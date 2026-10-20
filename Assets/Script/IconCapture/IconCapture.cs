/*
First Date: 30/9/2026
Date: 7/10/2026
Author: Tony
Use by: Nothing to set up. Other scripts call IconCapture.GetIcon(itemId, someObject)
        and IconCapture.MakeModel(itemId).
Purpose: Takes a "photo" of a 3D object and turns it into a Sprite for UI icons.
         Icons are remembered by Item Id, so the same item always uses the same icon
         (whether the first photo came from an object in the scene or from a prefab file, e.g. a recipe's material).
         Colors from ShapeColor are kept in both cases.
         It also keeps the look-only copy it photographed (meshes and colors, no scripts or physics),
         so MakeModel can give a fresh copy later, e.g. for the item in the player's hand (HeldItemView),
         even though the real item was destroyed when it was picked up.
Requirement: IconCapture.cs o-> ShapeColor.cs
             IconCapture.cs <-x BuildingData.cs, HeldItemView.cs, ItemData.cs, ItemIconRules.cs, ItemPickup.cs
*/

using System.Collections.Generic;
using UnityEngine;

public static class IconCapture
{
    // ===== Fields =====
    const int ICON_SIZE = 128;       // icon width and height in pixels
    const int ICON_LAYER = 31;       // an unused layer, so the icon camera only sees the copy
    const float CAM_FOV = 30f;       // a narrow view keeps the item's edges from stretching

    static readonly Vector3 STUDIO_POS = new Vector3(0f, -1000f, 0f);           // far below the game world
    static readonly Quaternion VIEW_ANGLE = Quaternion.Euler(25f, -35f, 0f);    // three-quarter view

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();   // look-only copies
    static Camera cam;
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

        GameObject copy = BuildVisualCopy(source);
        Bounds bounds = GetBounds(copy);

        // Aim at the item's center, then back off until the whole item fits in the view.
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        float dist = radius / Mathf.Sin(CAM_FOV * 0.5f * Mathf.Deg2Rad);
        cam.transform.rotation = VIEW_ANGLE;
        cam.transform.position = bounds.center - VIEW_ANGLE * Vector3.forward * dist;
        cam.nearClipPlane = Mathf.Max(0.01f, dist - radius * 2f);
        cam.farClipPlane = dist + radius * 2f;

        // Render into a temporary image, then copy it into a texture we can keep.
        RenderTexture rt = RenderTexture.GetTemporary(ICON_SIZE, ICON_SIZE, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

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
