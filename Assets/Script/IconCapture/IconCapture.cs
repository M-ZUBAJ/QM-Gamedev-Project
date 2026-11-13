/*
First Date: 30/9/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up. Other scripts call IconCapture.GetIcon(itemId, someObject)
        and IconCapture.MakeModel(itemId).
Unity Editor: Nothing to set. To make every icon brighter or darker, change the STUDIO_ numbers in Fields below.
Purpose: Takes a "photo" of a 3D object and turns it into a Sprite for UI icons.
         Icons are remembered by Item Id, so the same item always uses the same icon
         (whether the first photo came from an object in the scene or from a prefab file, e.g. a recipe's material).
         Colors from ShapeColor are kept in both cases.
         Photos are taken in a "studio" with its own fixed lighting (one key light and a soft even light), so an icon
         looks the same at noon and at midnight: for the moment of the photo the world's lights (the sun at night),
         its dim night ambient light and its fog are switched off, then put straight back.
         It also keeps the look-only copy it photographed (meshes and colors, no scripts or physics),
         so MakeModel can give a fresh copy later, e.g. for the item in the player's hand (HeldItemView),
         even though the real item was destroyed when it was picked up.
Requirement: IconCapture.cs o-> ShapeColor.cs
             IconCapture.cs <-x BuildingData.cs, HeldItemView.cs, ItemData.cs, ItemIconRules.cs, ItemPickup.cs
             IconCapture.cs <-o ToolInHand.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // AmbientMode, SphericalHarmonicsL2, LightProbeUsage

public static class IconCapture
{
    // What the world's lighting was before a photo, to put it back right after.
    struct WorldLighting
    {
        public AmbientMode mode;
        public Color ambient;
        public float ambientIntensity;
        public SphericalHarmonicsL2 ambientProbe;
        public bool fog;
    }

    // ===== Fields =====
    const int ICON_SIZE = 128;       // icon width and height in pixels
    const int ICON_LAYER = 31;       // an unused layer, so the icon camera only sees the copy
    const float CAM_FOV = 30f;       // a narrow view keeps the item's edges from stretching

    static readonly Vector3 STUDIO_POS = new Vector3(0f, -1000f, 0f);           // far below the game world
    static readonly Quaternion VIEW_ANGLE = Quaternion.Euler(25f, -35f, 0f);    // three-quarter view

    // The studio's fixed lighting (the same whatever time it is in the game):
    // a key light shining from the top left of the picture, and a soft even light so the shadow side isn't black.
    static readonly Quaternion STUDIO_LIGHT_ANGLE = VIEW_ANGLE * Quaternion.Euler(30f, 30f, 0f);
    static readonly Color STUDIO_LIGHT_COLOR = new Color(1f, 0.97f, 0.92f);    // a little warm, like daylight
    const float STUDIO_LIGHT_INTENSITY = 1.1f;                                // about the midday sun (1.2)
    static readonly Color STUDIO_AMBIENT = new Color(0.55f, 0.55f, 0.58f);    // the soft even light (1 = full white)

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();   // look-only copies
    static readonly List<Light> switchedOff = new List<Light>();   // world lights turned off for the photo
    static Camera cam;
    static Light studioLight;
    static Transform modelHolder;   // switched off: the kept copies under it are never drawn

    // ===== My methods =====

    // Returns the icon for this item. A photo is only taken the first time an Item Id is seen.
    public static Sprite GetIcon(string itemId, GameObject source)
    {
        if (cache.TryGetValue(itemId, out Sprite icon)) return icon;

        icon = Capture(itemId, source);
        cache[itemId] = icon;
        return icon;
    }

    // A new look-only copy of this item (meshes and colors, no scripts, colliders or physics), or null if
    // this Item Id was never photographed (seen) this play session. The caller places it and destroys it when done.
    public static GameObject MakeModel(string itemId)
    {
        if (!models.TryGetValue(itemId, out GameObject kept) || kept == null) return null;

        GameObject model = Object.Instantiate(kept);   // no parent = in the scene, switched on
        model.name = itemId + " (Model)";

        // Instantiate does NOT copy color overrides (MaterialPropertyBlocks, e.g. from ShapeColor),
        // so the copy would show the plain material (white). Copy them part by part (same order in both).
        Renderer[] from = kept.GetComponentsInChildren<Renderer>(true);
        Renderer[] to = model.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        for (int i = 0; i < from.Length && i < to.Length; i++)
        {
            if (!from[i].HasPropertyBlock()) continue;
            from[i].GetPropertyBlock(block);
            to[i].SetPropertyBlock(block);
        }

        return model;
    }

    static Sprite Capture(string itemId, GameObject source)
    {
        if (cam == null) cam = CreateCamera();
        if (studioLight == null) studioLight = CreateStudioLight();

        GameObject copy = BuildVisualCopy(source);
        Bounds bounds = GetBounds(copy);

        // Aim at the item's center, then back off until the whole item fits in the view.
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        float dist = radius / Mathf.Sin(CAM_FOV * 0.5f * Mathf.Deg2Rad);
        cam.transform.rotation = VIEW_ANGLE;
        cam.transform.position = bounds.center - VIEW_ANGLE * Vector3.forward * dist;
        cam.nearClipPlane = Mathf.Max(0.01f, dist - radius * 2f);
        cam.farClipPlane = dist + radius * 2f;

        // Render into a temporary image (in the studio's own light), then copy it into a texture we can keep.
        RenderTexture rt = RenderTexture.GetTemporary(ICON_SIZE, ICON_SIZE, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        // (Only while playing: in Edit mode switching the scene's lights would mark the scene as changed.)
        bool studio = Application.isPlaying;
        UseWorldProbes(copy, false);
        WorldLighting world = studio ? StudioLightingOn() : default;
        try
        {
            cam.Render();   // happens right now, inside this line: the game's own view never sees the studio lighting
        }
        finally
        {
            if (studio) StudioLightingOff(world);   // the world's lighting is back before anything else draws
            UseWorldProbes(copy, true);
        }

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, ICON_SIZE, ICON_SIZE), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        // Clean up everything except the finished texture.
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        // The copy must leave the studio NOW: several icons can be taken in the same frame (e.g. the build menu:
        // a building, then its materials), and a copy still standing there would show up in the next photo.
        KeepModel(itemId, copy);

        return Sprite.Create(tex, new Rect(0, 0, ICON_SIZE, ICON_SIZE), new Vector2(0.5f, 0.5f));
    }

    // Stores the photographed copy under the switched-off holder (which hides it at once), for MakeModel.
    static void KeepModel(string itemId, GameObject copy)
    {
        if (models.TryGetValue(itemId, out GameObject old) && old != null)
        {
            Object.DestroyImmediate(copy);   // already have one (DestroyImmediate: gone before the next photo)
            return;
        }

        if (modelHolder == null)
        {
            GameObject h = new GameObject("ItemModels (hidden)");
            h.SetActive(false);              // switched off: nothing under it is drawn
            Object.DontDestroyOnLoad(h);     // keep the models when a new scene loads
            modelHolder = h.transform;
        }

        copy.name = itemId;
        copy.transform.SetParent(modelHolder, true);   // true = keep its size
        models[itemId] = copy;
    }

    // Makes a copy with only meshes and materials: no scripts, colliders or physics.
    static GameObject BuildVisualCopy(GameObject source)
    {
        GameObject root = new GameObject("IconCopy");
        root.transform.position = STUDIO_POS;
        root.transform.localScale = source.transform.lossyScale;   // keep the item's proportions

        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (MeshRenderer src in source.GetComponentsInChildren<MeshRenderer>())
        {
            MeshFilter srcFilter = src.GetComponent<MeshFilter>();
            if (srcFilter == null || srcFilter.sharedMesh == null) continue;   // nothing to draw

            // Where this piece sits compared to the item's root (ignoring the item's own rotation).
            Matrix4x4 rel = source.transform.worldToLocalMatrix * src.transform.localToWorldMatrix;

            GameObject piece = new GameObject(src.name);
            piece.layer = ICON_LAYER;
            piece.transform.SetParent(root.transform, false);
            piece.transform.localPosition = rel.GetColumn(3);
            piece.transform.localRotation = rel.rotation;
            piece.transform.localScale = rel.lossyScale;

            piece.AddComponent<MeshFilter>().sharedMesh = srcFilter.sharedMesh;
            MeshRenderer rend = piece.AddComponent<MeshRenderer>();
            rend.sharedMaterials = src.sharedMaterials;

            // Keep colors set by scripts like ShapeColor (they override the material's color on this shape only).
            block.Clear();
            if (src.HasPropertyBlock()) src.GetPropertyBlock(block);

            // A prefab FILE never ran ShapeColor, so it has no override yet: read the color from ShapeColor itself.
            // (Without this, an icon taken from a prefab, e.g. a recipe's material, shows the plain white material.)
            ShapeColor shapeColor = src.GetComponent<ShapeColor>();
            if (shapeColor != null)
            {
                block.SetColor("_Color", shapeColor.Color);       // Built-in Render Pipeline
                block.SetColor("_BaseColor", shapeColor.Color);   // URP
            }

            if (!block.isEmpty) rend.SetPropertyBlock(block);
        }

        return root;
    }

    static Bounds GetBounds(GameObject obj)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            bounds.Encapsulate(rends[i].bounds);
        }
        return bounds;
    }

    // ----- Studio lighting: the same light for every photo -----

    // Switches to the studio's light for one photo: only the studio light, a fixed soft even light, no fog.
    // Returns the world's lighting, to give back to StudioLightingOff.
    static WorldLighting StudioLightingOn()
    {
        WorldLighting world = new WorldLighting
        {
            mode = RenderSettings.ambientMode,
            ambient = RenderSettings.ambientLight,
            ambientIntensity = RenderSettings.ambientIntensity,
            ambientProbe = RenderSettings.ambientProbe,
            fog = RenderSettings.fog,
        };

        // Every world light off (e.g. the sun, dim at night), so only the studio light shines on the item.
        switchedOff.Clear();
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light == studioLight || !light.enabled) continue;
            light.enabled = false;
            switchedOff.Add(light);
        }

        // The soft even light ("ambient") the same every time, whatever the sky looks like now.
        SphericalHarmonicsL2 even = new SphericalHarmonicsL2();
        even.AddAmbientLight(STUDIO_AMBIENT);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = STUDIO_AMBIENT;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.ambientProbe = even;
        RenderSettings.fog = false;

        studioLight.enabled = true;
        return world;
    }

    // Puts the world's lighting back exactly as it was.
    static void StudioLightingOff(WorldLighting world)
    {
        studioLight.enabled = false;
        foreach (Light light in switchedOff)
        {
            if (light != null) light.enabled = true;
        }
        switchedOff.Clear();

        RenderSettings.ambientMode = world.mode;
        RenderSettings.ambientLight = world.ambient;
        RenderSettings.ambientIntensity = world.ambientIntensity;
        RenderSettings.ambientProbe = world.ambientProbe;
        RenderSettings.fog = world.fog;
    }

    // Light probes (baked light stored in the world) would light the copy by where it stands: off for the photo,
    // so the studio's own soft light is used. Back on afterwards (the kept copy becomes the item in the player's hand).
    static void UseWorldProbes(GameObject copy, bool use)
    {
        foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true))
        {
            r.lightProbeUsage = use ? LightProbeUsage.BlendProbes : LightProbeUsage.Off;
            r.reflectionProbeUsage = use ? ReflectionProbeUsage.BlendProbes : ReflectionProbeUsage.Off;
        }
    }

    // The studio's key light: lights only the icon copy (its layer), never the world, and is only on during a photo.
    static Light CreateStudioLight()
    {
        GameObject go = new GameObject("IconLight");
        Object.DontDestroyOnLoad(go);   // keep it when scenes change
        go.transform.rotation = STUDIO_LIGHT_ANGLE;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = STUDIO_LIGHT_COLOR;
        light.intensity = STUDIO_LIGHT_INTENSITY;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << ICON_LAYER;
        light.enabled = false;
        return light;
    }

    static Camera CreateCamera()
    {
        GameObject go = new GameObject("IconCamera");
        Object.DontDestroyOnLoad(go);   // keep it when scenes change

        Camera c = go.AddComponent<Camera>();
        c.enabled = false;                                // only renders when Capture calls Render()
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0f, 0f, 0f, 0f);    // transparent background
        c.cullingMask = 1 << ICON_LAYER;                  // sees only the icon copy
        c.fieldOfView = CAM_FOV;
        return c;
    }
}
