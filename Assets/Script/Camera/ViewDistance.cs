/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: The Main Camera (Tools > World > Build New World Scene puts it on the new world's). One per scene.
Unity Editor: Distance = how far the player can see, in meters (100). Lower = faster on slow computers and in the web
              (WebGL) build; higher = see further. It can be changed while playing.
              The scene's fog must be on (Window > Rendering > Lighting > Environment > Other Settings > Fog, mode
              Linear), or a built game may leave the mist out: Build New World Scene does that for the new world.
              Guide: Docs/View Distance Setup.md
              Testing: Play and look around: the world fades into a mist at Distance, and the mist turns orange at
              sunrise / sunset and dark blue at night. Untick this component to compare (Game view > Stats shows the
              frame rate).
Purpose: How far the player can see (the line of sight, like Minecraft's Render Distance), to keep the frame rate up,
         most of all in the web build:
         - nothing further than Distance is drawn (the camera's Far Clip Plane; shadows end there too): fewer trees,
           rocks, grass and shadows to draw,
         - a mist thickens from Clear Share x Distance to Distance, so things fade in and out instead of popping,
         - the same mist lies low on the sky all round, so the far ground melts into the sky instead of a hard edge,
         - the mist's color follows the time of day (the scene's DaylightCycle): Day Color, Sunset Color at sunrise
           and sunset, Night Color at night,
         - the terrain's grass is drawn only up to Distance,
         - PlayerSight (what the spawners think the player can see) sees exactly Distance far, so deer appear just
           past the mist, never in view.
         Untick it (or remove it) and everything is back as before.
Requirement: ViewDistance.cs x-> Camera (Unity)
             ViewDistance.cs o-> DaylightCycle.cs, Terrain (Unity)
             ViewDistance.cs <-x NewWorldBuilder.cs
             ViewDistance.cs <-o PlayerSight.cs, WorldGenerator.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;            // ShadowCastingMode, LightProbeUsage, ReflectionProbeUsage
using UnityEngine.SceneManagement;      // SceneManager

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class ViewDistance : MonoBehaviour
{
    // ===== Fields =====
    [Header("How far")]
    [Tooltip("How far the player can see, in meters. Nothing further is drawn: lower = a higher frame rate (most of all " +
             "in the web build), higher = see further. Minecraft's smallest Render Distance (2 chunks) is 32 m.")]
    [Range(10f, 500f)]
    [SerializeField] float distance = 100f;

    [Tooltip("How much of that is clear before the mist starts: 0.4 = clear for the first 40% (40 m of 100), then the " +
             "mist thickens until nothing shows at Distance.")]
    [Range(0f, 0.95f)]
    [SerializeField] float clearShare = 0.4f;

    [Tooltip("How high up the sky the mist reaches: 0 = only below the horizon, 0.35 = about 20 degrees up, " +
             "1 = all the way up (no sky at all).")]
    [Range(0f, 1f)]
    [SerializeField] float mistOnSky = 0.35f;

    [Header("Mist color (follows the DaylightCycle)")]
    [Tooltip("The mist in the day.")]
    [SerializeField] Color dayColor = new Color(0.7f, 0.8f, 0.9f);

    [Tooltip("The mist at sunrise and sunset.")]
    [SerializeField] Color sunsetColor = new Color(0.9f, 0.62f, 0.45f);

    [Tooltip("The mist at night.")]
    [SerializeField] Color nightColor = new Color(0.05f, 0.07f, 0.13f);

    const string MistShaderName = "ViewDistanceMist";   // Assets/Effects/ViewDistance/Resources (Resources: builds keep it)
    const float MistDepth = 0.98f;   // the sky's mist sits just inside where drawing stops (this share of Distance)
    const float MistOversize = 1.15f;   // a little bigger than the screen, so its edges never show
    static readonly int MistTopId = Shader.PropertyToID("_MistTop");

    static ViewDistance current;
    Camera cam;
    GameObject mist;                 // the sky's mist: a flat card in front of the camera, at the far end
    Material mistMaterial;
    Mesh mistMesh;
    readonly List<Terrain> terrains = new List<Terrain>();
    readonly Dictionary<Terrain, float> grassBefore = new Dictionary<Terrain, float>();   // each terrain's own grass distance

    // How things were before, given back when this is switched off.
    bool changed;
    float farBefore;
    bool fogBefore;
    FogMode fogModeBefore;
    Color fogColorBefore;
    float fogStartBefore, fogEndBefore;

    // The one in the scene (null if none or switched off).
    public static ViewDistance Current => current != null && current.isActiveAndEnabled ? current : null;

    // How far the player can see right now, in meters: Distance while a ViewDistance is on, else no limit.
    // e.g. PlayerSight uses it as its range.
    public static float Limit => Current != null ? Current.distance : float.PositiveInfinity;

    public float Distance => distance;

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        current = this;
        cam = GetComponent<Camera>();
        Remember();
        MakeMist();
        Apply();
    }

    void OnDisable()
    {
        if (current == this) current = null;
        if (mist != null) mist.SetActive(false);
        GiveBack();
    }

    void OnDestroy()
    {
        if (mist != null) Destroy(mist);
        if (mistMaterial != null) Destroy(mistMaterial);
        if (mistMesh != null) Destroy(mistMesh);
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        current = null;
    }

    // Every frame (after the camera moved): the distance can be changed while playing, the day's color changes, and
    // the screen's shape can change (window resized).
    void LateUpdate()
    {
        Apply();
    }

    // ===== My methods =====

    // Changes how far the player can see (e.g. from a settings menu), in meters.
    public void SetDistance(float meters)
    {
        distance = Mathf.Clamp(meters, 10f, 500f);
        if (isActiveAndEnabled) Apply();
    }

    void Apply()
    {
        if (cam == null) return;
        cam.farClipPlane = distance;   // also where shadows end (Unity never draws them past the far clip plane)

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = distance * clearShare;
        RenderSettings.fogEndDistance = distance;
        RenderSettings.fogColor = MistColor();

        LimitGrass();
        PlaceMist();
    }

    // The mist's color now: day, sunset or night, from how high the sun is (like the sky itself).
    Color MistColor()
    {
        DaylightCycle day = DaylightCycle.Current;
        if (day == null) return dayColor;

        float height = day.SunHeight;
        float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, -0.25f, height));   // the same as the night sky
        Color color = Color.Lerp(dayColor, nightColor, night);
        float sunset = Mathf.Clamp01(1f - Mathf.Abs(height - 0.02f) / 0.18f);              // the sun near the horizon
        color = Color.Lerp(color, sunsetColor, sunset * sunset);
        color *= Mathf.Lerp(0.3f, 1f, day.SkyDip);   // darker for the moment the sky fades out (the sun turns into the moon)
        color.a = 1f;
        return color;
    }

    // The terrain's grass is drawn no further than Distance (it's hidden by the mist there anyway).
    void LimitGrass()
    {
        Terrain.GetActiveTerrains(terrains);
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null) continue;
            if (!grassBefore.ContainsKey(terrain)) grassBefore[terrain] = terrain.detailObjectDistance;
            float wanted = Mathf.Min(grassBefore[terrain], distance);
            if (!Mathf.Approximately(terrain.detailObjectDistance, wanted)) terrain.detailObjectDistance = wanted;
        }
    }

    // The sky's mist: a card filling the screen just inside where drawing stops, so it covers only the sky and the empty
    // space past the last drawn hill. It's a child of the camera, so it turns with it.
    void MakeMist()
    {
        if (mist != null)
        {
            mist.SetActive(true);
            return;
        }
        Shader shader = Resources.Load<Shader>(MistShaderName);
        if (shader == null)
        {
            Debug.LogWarning(name + ": ViewDistance's mist shader (Assets/Effects/ViewDistance/Resources/" + MistShaderName +
                             ".shader) is missing: the far ground ends in a hard edge against the sky.", this);
            return;
        }
        mistMaterial = new Material(shader) { name = "View Distance Mist" };
        mistMesh = new Mesh { name = "View Distance Mist" };
        mistMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
        };
        mistMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mistMesh.RecalculateBounds();

        mist = new GameObject("View Distance Mist");
        mist.layer = DrawnLayer();
        mist.transform.SetParent(transform, false);
        mist.AddComponent<MeshFilter>().sharedMesh = mistMesh;
        MeshRenderer renderer = mist.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mistMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    // The card's place and size: at the far end, a bit bigger than what the camera sees there.
    void PlaceMist()
    {
        if (mist == null) return;
        float depth = distance * MistDepth;
        float tall = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * MistOversize;
        mist.transform.localPosition = new Vector3(0f, 0f, depth);
        mist.transform.localRotation = Quaternion.identity;
        mist.transform.localScale = new Vector3(tall * cam.aspect, tall, 1f);
        mistMaterial.SetFloat(MistTopId, mistOnSky);
    }

    // A layer this camera draws (Default if it can), so the card shows.
    int DrawnLayer()
    {
        if ((cam.cullingMask & 1) != 0) return 0;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((cam.cullingMask & (1 << layer)) != 0) return layer;
        }
        return 0;
    }

    void Remember()
    {
        if (changed) return;
        changed = true;
        farBefore = cam.farClipPlane;
        fogBefore = RenderSettings.fog;
        fogModeBefore = RenderSettings.fogMode;
        fogColorBefore = RenderSettings.fogColor;
        fogStartBefore = RenderSettings.fogStartDistance;
        fogEndBefore = RenderSettings.fogEndDistance;
    }

    // Everything back as it was. The scene's fog only while this scene is still the active one (when another scene is
    // loading, the fog settings already belong to that one).
    void GiveBack()
    {
        if (!changed) return;
        changed = false;
        if (cam != null) cam.farClipPlane = farBefore;
        foreach (KeyValuePair<Terrain, float> pair in grassBefore)
        {
            if (pair.Key != null) pair.Key.detailObjectDistance = pair.Value;
        }
        grassBefore.Clear();

        if (SceneManager.GetActiveScene() != gameObject.scene) return;
        RenderSettings.fog = fogBefore;
        RenderSettings.fogMode = fogModeBefore;
        RenderSettings.fogColor = fogColorBefore;
        RenderSettings.fogStartDistance = fogStartBefore;
        RenderSettings.fogEndDistance = fogEndBefore;
    }
}
