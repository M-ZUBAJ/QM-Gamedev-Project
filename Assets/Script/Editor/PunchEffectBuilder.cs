/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > Effects > Build Punch Wind Burst.
              It makes Assets/Prefabs/Effects/PunchWindBurst.prefab (asks before replacing it) and its pictures and
              materials in Assets/Effects/PunchWindBurst (only the missing ones, so your own art is kept), then puts
              PunchEffect on the Player and fills in its Burst Prefab and Impact Delay. Undo (Ctrl + Z) works for the
              Player change. To see the burst without playing: open the prefab and click Play in the Particle Effect box
              (bottom right of the Scene view). Hand-made steps: Docs/Punch Effect Setup.md
Purpose: Makes the grey "wind explosion" that a bare-handed punch makes when it hits something with health:
         PunchWindBurst   plays its parts together (makes no particles itself)
           > Puff         grey dust clouds bursting out and slowing down, like air pushed out by the fist
           > Streaks      fast light-grey wind lines shooting out around the hit
           > Ring         one shock ring that grows and fades
           > Dust         a few small dark specks that fall
         Impact Delay = when the first-person punch has the fist furthest forward: the tool plays the punch
         animation on a hidden copy of the hands and watches the right hand, so the burst pops when the fist lands.
Requirement: PunchEffectBuilder.cs x-> UIBuildHelper.cs, PunchEffect.cs, PlayerAnimator.cs, MenuOrder.cs
             PunchEffectBuilder.cs <-x PlayerAnimationSetup.cs, TierToolBuilder.cs
*/

using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;   // AnimationClipPlayable, AnimationPlayableOutput
using UnityEngine.Playables;    // PlayableGraph
using UnityEngine.Rendering;    // ShadowCastingMode
using static UIBuildHelper;     // AddToPlayer, FindPrefabPath, FolderOf, MakeFolders

public static class PunchEffectBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Punch Wind Burst";
    // Where the prefab goes. If you move it, the tool still finds it by name (FindPrefabPath).
    static string PrefabPath => FindPrefabPath("Assets/Prefabs/Effects/PunchWindBurst.prefab");
    const string ArtFolder = "Assets/Effects/PunchWindBurst";   // the drawn pictures and their materials
    const int PictureSize = 128;   // pixels; soft shapes this small on screen don't need more

    // The punch to measure: the first-person one (Tools > Player > Add First Person Hands), else the third-person one.
    const string HandsController = "Assets/Animation/Player/FirstPersonHands.controller";
    const string HandsModel = "Assets/Prefabs/Player/FirstPersonHands.prefab";
    const string BodyController = "Assets/Animation/Player/PlayerCharacter.controller";
    const string BodyModel = "Assets/Prefabs/Player/PlayerCharacter.prefab";
    const float DefaultDelay = 0.25f;   // seconds, used when there's no punch animation to measure

    // The greys (1 = white, 0 = black). The 4th number (alpha) = how solid (1 = not see-through at all).
    static readonly Color PuffLight = new Color(0.9f, 0.9f, 0.9f, 0.85f);
    static readonly Color PuffDark = new Color(0.66f, 0.66f, 0.68f, 0.85f);
    static readonly Color StreakLight = new Color(0.97f, 0.97f, 0.97f, 0.95f);
    static readonly Color StreakDark = new Color(0.8f, 0.8f, 0.82f, 0.9f);
    static readonly Color RingColor = new Color(0.92f, 0.92f, 0.92f, 0.8f);
    static readonly Color DustLight = new Color(0.6f, 0.6f, 0.6f, 1f);
    static readonly Color DustDark = new Color(0.38f, 0.38f, 0.4f, 1f);

    // The soft cloud picture is a few round blobs together: x, y = middle, z = radius (the picture goes from -1 to 1).
    static readonly Vector3[] PuffBlobs =
    {
        new Vector3(0f, 0f, 0.62f), new Vector3(-0.34f, 0.16f, 0.42f), new Vector3(0.3f, 0.24f, 0.4f),
        new Vector3(0.06f, -0.32f, 0.4f), new Vector3(-0.24f, -0.24f, 0.34f), new Vector3(0.32f, -0.16f, 0.36f),
    };

    enum Picture { Puff, Ring, Dot }

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Effects/Build Punch Wind Burst", false, MenuOrder.BuildPunchWindBurst)]
    static void Build()
    {
        string path = PrefabPath;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null && !EditorUtility.DisplayDialog(ToolName,
                "PunchWindBurst already exists. Build it again and replace it?\n(Changes made to the prefab are lost. " +
                "The pictures and materials in " + ArtFolder + " are kept.)", "Replace", "Cancel"))
        {
            return;
        }

        Material puff = MakeMaterial("WindPuff", Picture.Puff);
        Material ring = MakeMaterial("WindRing", Picture.Ring);
        Material dot = MakeMaterial("WindDot", Picture.Dot);
        GameObject prefab = BuildPrefab(path, puff, ring, dot);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(ToolName, "Couldn't save " + path + ". See the Console for why.", "OK");
            return;
        }

        AssetDatabase.SaveAssets();   // writes the materials to disk now

        float? measured = MeasurePunch(out string how);
        float delay = measured ?? DefaultDelay;
        string timing = "Impact Delay = " + delay.ToString("0.00") + " s (" + how + ")";

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        if (PutOnPlayer(prefab, delay))
        {
            Debug.Log("PunchWindBurst built: " + path + ". " + timing + ". Test: Play and punch an enemy, tree or stone " +
                      "with an empty hand, or click the ⋮ on the Player's PunchEffect > Test: Burst In Front. Save the scene to keep it.");
        }
        else
        {
            Debug.LogWarning("PunchWindBurst built: " + path + ", but there's no Player (PlayerInventory) in the open scene. " +
                             "Open GameScene and run this again, or drag PunchEffect onto the Player, the prefab into its " +
                             "Burst Prefab, and set its " + timing + ".");
        }
    }

    // ===== My methods =====

    // ----- The prefab -----

    // An existing prefab is emptied and refilled IN PLACE: its top object stays the same, so links to it (the
    // Player's Burst Prefab) keep working. A brand-new object saved over it could break those links.
    static GameObject BuildPrefab(string path, Material puff, Material ring, Material dot)
    {
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("PunchWindBurst");
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            // The top: a Particle System without particles, so the parts play together and the whole burst can be
            // previewed by selecting it.
            ParticleSystem group = root.GetComponent<ParticleSystem>();
            if (group == null) group = root.AddComponent<ParticleSystem>();
            Prepare(group, 0.6f);
            ParticleSystem.EmissionModule emission = group.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = group.shape;
            shape.enabled = false;
            root.GetComponent<ParticleSystemRenderer>().enabled = false;

            MakePuff(root.transform, puff);
            MakeStreaks(root.transform, dot);
            MakeRing(root.transform, ring);
            MakeDust(root.transform, dot);
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);   // only the saved prefab is kept
            else Object.DestroyImmediate(root);
        }
    }

    // Grey dust clouds: burst out of the hit, slow down fast (drag), grow, turn and fade. Drawn furthest back.
    static void MakePuff(Transform parent, Material material)
    {
        ParticleSystem ps = NewPart("Puff", parent, material, 0.6f);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4f);      // meters per second at the start
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);    // meters
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);   // every cloud turned differently
        main.startColor = new ParticleSystem.MinMaxGradient(PuffLight, PuffDark);
        main.gravityModifier = -0.1f;   // drifts up a little, like dust in the air

        Burst(ps, 12, 16);
        Shape(ps, 75f, 0.1f, 0.6f);   // mostly sideways: from the front that's what makes it look like a burst
        Drag(ps, 6f);
        Grow(ps, Curve(0f, 0.45f, 0.25f, 0.9f, 1f, 1.25f));
        Fade(ps, 0f, 0f, 0.08f, 1f, 0.55f, 0.65f, 1f, 0f);

        ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-2f, 2f);   // radians per second, a slow swirl either way

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortMode = ParticleSystemSortMode.Distance;   // nearer clouds drawn over farther ones
        renderer.sortingFudge = 2f;
        renderer.maxParticleSize = 0.35f;   // a cloud right in front of the camera covers at most a third of the screen
    }

    // Wind lines: a round spot stretched by its speed, so fast = a long line. They shoot out sideways and a bit toward
    // the player, slow down, get shorter and thinner, and are gone in a fifth of a second.
    static void MakeStreaks(Transform parent, Material material)
    {
        ParticleSystem ps = NewPart("Streaks", parent, material, 0.3f);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);   // the line's thickness
        main.startColor = new ParticleSystem.MinMaxGradient(StreakLight, StreakDark);

        Burst(ps, 10, 14);
        Shape(ps, 75f, 0.1f, 0f);   // from the rim of a small circle, so every line flies outward
        Drag(ps, 9f);
        Grow(ps, Curve(0f, 1f, 1f, 0.3f));
        Fade(ps, 0f, 1f, 0.6f, 0.8f, 1f, 0f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.07f;   // longer the faster it goes (10 m/s = 0.7 m long)
        renderer.lengthScale = 2.5f;      // never shorter than 2.5 times its thickness
        renderer.cameraVelocityScale = 0f;
        renderer.sortingFudge = 0f;
    }

    // One shock ring at the hit: grows fast, then slower, and fades. Always faces the camera.
    static void MakeRing(Transform parent, Material material)
    {
        ParticleSystem ps = NewPart("Ring", parent, material, 0.3f);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.25f;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startColor = RingColor;

        Burst(ps, 1, 1);
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;   // starts right at the hit
        Grow(ps, Curve(0f, 0.25f, 0.4f, 0.9f, 1f, 1.2f));   // 0.25 m wide -> 1.2 m wide
        Fade(ps, 0f, 0.9f, 0.35f, 0.7f, 1f, 0f);

        ps.GetComponent<ParticleSystemRenderer>().sortingFudge = 1f;
    }

    // A few small dark specks knocked off the target: they fly out and fall. Drawn in front of the rest.
    static void MakeDust(Transform parent, Material material)
    {
        ParticleSystem ps = NewPart("Dust", parent, material, 0.3f);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(DustLight, DustDark);
        main.gravityModifier = 1.5f;   // falls a bit faster than normal, so it feels heavy

        Burst(ps, 6, 10);
        Shape(ps, 70f, 0.05f, 1f);
        Drag(ps, 3f);
        Fade(ps, 0f, 1f, 0.7f, 1f, 1f, 0f);

        ps.GetComponent<ParticleSystemRenderer>().sortingFudge = -1f;
    }

    // A new part under the top: its own object with a Particle System that plays once when the burst appears.
    static ParticleSystem NewPart(string name, Transform parent, Material material, float duration)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        Prepare(ps, duration);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;   // wind casts no shadow
        renderer.receiveShadows = false;
        return ps;
    }

    // Settings every part shares: plays once by itself when made, and everything (size, spread, speed) follows the
    // burst's scale, so PunchEffect's Size can make it bigger or smaller.
    static void Prepare(ParticleSystem ps, float duration)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // Unity only lets Duration change while stopped
        ParticleSystem.MainModule main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;   // the burst doesn't move after it appears
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 50;
    }

    // All particles at once when it starts, a random number from min to max.
    static void Burst(ParticleSystem ps, int min, int max)
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)min, (short)max) });
    }

    // A cone pointing out of the hit surface (the burst's front). angle = how wide it opens (90 = flat along the
    // surface), radius = size of its starting circle, thickness 0 = start on its rim, 1 = anywhere inside it.
    static void Shape(ParticleSystem ps, float angle, float radius, float thickness)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
        shape.radiusThickness = thickness;
    }

    // Air slowing the particles down: bigger = they stop sooner (6 = most of the speed gone in a third of a second).
    static void Drag(ParticleSystem ps, float drag)
    {
        ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;
        slow.enabled = true;
        slow.limit = 100f;    // no speed limit, only the drag below
        slow.dampen = 0f;
        slow.drag = drag;
        slow.multiplyDragByParticleSize = false;
        slow.multiplyDragByParticleVelocity = false;
    }

    // Size over the particle's life (1 = its start size).
    static void Grow(ParticleSystem ps, AnimationCurve curve)
    {
        ParticleSystem.SizeOverLifetimeModule grow = ps.sizeOverLifetime;
        grow.enabled = true;
        grow.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    // See-through-ness over the particle's life: pairs of (time 0..1, alpha 0..1).
    static void Fade(ParticleSystem ps, params float[] timeAlpha)
    {
        GradientAlphaKey[] keys = new GradientAlphaKey[timeAlpha.Length / 2];
        for (int i = 0; i < keys.Length; i++) keys[i] = new GradientAlphaKey(timeAlpha[i * 2 + 1], timeAlpha[i * 2]);

        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, keys);
        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        fade.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    // A smooth curve through pairs of (time 0..1, value).
    static AnimationCurve Curve(params float[] timeValue)
    {
        Keyframe[] keys = new Keyframe[timeValue.Length / 2];
        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(timeValue[i * 2], timeValue[i * 2 + 1]);
        AnimationCurve curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0f);
        return curve;
    }

    // ----- Pictures and materials -----

    // A see-through particle material showing one drawn picture. Made once: an existing one is kept as it is (only a
    // missing picture is put back), so changes made to it stay.
    static Material MakeMaterial(string name, Picture picture)
    {
        MakeFolders(ArtFolder);
        Texture2D texture = SavePicture(name, picture);
        string path = ArtFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(ParticleShader()) { name = name, mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.mainTexture == null)
        {
            material.mainTexture = texture;   // its picture was deleted: put the drawn one back
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    // Unity's simple see-through particle shader (Built-in render pipeline, works in WebGL): picture x particle color,
    // no lighting, so the grey looks the same in every light.
    static Shader ParticleShader()
    {
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");
        if (shader == null) shader = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat").shader;
        return shader;
    }

    // Saves a drawn picture as a PNG (only if the file isn't there yet, so your own art is kept).
    static Texture2D SavePicture(string name, Picture picture)
    {
        string path = ArtFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D drawn = Draw(picture);
            File.WriteAllBytes(path, drawn.EncodeToPNG());
            Object.DestroyImmediate(drawn);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;   // no dark rims around the soft edges
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;          // stays smooth when small and far away
            importer.maxTextureSize = PictureSize;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // White (lit) pixels with see-through-ness making the shape. The particle color tints them grey.
    static Texture2D Draw(Picture picture)
    {
        int n = PictureSize;
        Texture2D texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;   // -1 (left) to 1 (right)
                float v = (y + 0.5f) / n * 2f - 1f;   // -1 (bottom) to 1 (top)
                float r = Mathf.Sqrt(u * u + v * v);  // 0 in the middle, 1 at the edge
                float light = 1f;
                float alpha = picture == Picture.Puff ? Puff(u, v, out light)
                            : picture == Picture.Ring ? Ring(r)
                            : Dot(r);
                alpha *= Mathf.Clamp01((1f - r) / 0.08f);   // always clear at the edge, so no square shows

                byte l = (byte)Mathf.RoundToInt(255f * light);
                pixels[y * n + x] = new Color32(l, l, l, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // A soft cloud: round blobs together, thicker and thinner patches (noise), lit from the top left.
    static float Puff(float u, float v, out float light)
    {
        float alpha = 0f;
        foreach (Vector3 blob in PuffBlobs)
        {
            float d = Mathf.Sqrt((u - blob.x) * (u - blob.x) + (v - blob.y) * (v - blob.y)) / blob.z;
            alpha = Mathf.Max(alpha, Mathf.SmoothStep(0f, 1f, 1f - d));
        }
        float noise = 0.6f * Mathf.PerlinNoise(u * 3f + 11f, v * 3f + 7f) + 0.4f * Mathf.PerlinNoise(u * 8f + 31f, v * 8f + 17f);
        light = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(0.5f + 0.45f * (v - u)));
        return alpha * Mathf.Lerp(0.55f, 1f, noise);
    }

    // A thin soft ring with a faint haze inside, like a shock wave in the air.
    static float Ring(float r)
    {
        float ring = Mathf.Exp(-Mathf.Pow((r - 0.76f) / 0.07f, 2f));
        float haze = r < 0.76f ? 0.18f * Mathf.Pow(r / 0.76f, 3f) : 0f;
        return Mathf.Max(ring, haze);
    }

    // A round soft spot, solid in the middle. Stretched by speed it becomes a wind line.
    static float Dot(float r)
    {
        return Mathf.Pow(Mathf.Clamp01(1f - r), 1.5f);
    }

    // ----- When the fist lands -----

    // Seconds from the click until the punch has the fist furthest forward (null = couldn't tell; how = what happened,
    // for the Console).
    static float? MeasurePunch(out string how)
    {
        return MeasureAction(PlayerAction.Punch, out how);
    }

    // Seconds from the click until an action's swing lands (null = couldn't tell; how = what happened, for the Console).
    // It plays the action's animation, frame by frame, on a hidden copy of the hands (else the body) and watches the
    // punching hand (the left one for PunchLeft and HeavyHook, else the right): a punch or hook lands when the hand is
    // furthest forward, a tool swing (Attack, Mine: BotAxeAttackDownward chops down) when it's lowest (only in the first
    // 70 %, so a looping clip like Blink's MiningLoop counts its first swing). Tools > Items > Build Tier Tools and Set Up Player Animations use it too.
    internal static float? MeasureAction(PlayerAction action, out string how)
    {
        string state = action.ToString();
        if (!FindAction(HandsController, HandsModel, state, out AnimationClip clip, out float speed, out GameObject model) &&
            !FindAction(BodyController, BodyModel, state, out clip, out speed, out model))
        {
            how = "no " + state + " animation to measure: run Tools > Player > Add First Person Hands, then this again";
            return null;
        }
        how = clip.name + " couldn't be played here to measure it: set its delay by hand";
        bool lowest = action == PlayerAction.Mine || action == PlayerAction.Attack;   // every tool chops downward
        bool highest = false;   // (no action lands at its highest point now; the uppercut was swapped for the left hook)
        bool leftHand = action == PlayerAction.PunchLeft || action == PlayerAction.HeavyHook;
        float searchTo = clip.length * (lowest ? 0.7f : 1f);

        GameObject copy = Object.Instantiate(model);
        copy.hideFlags = HideFlags.HideAndDontSave;   // never shown, never saved
        PlayableGraph graph = PlayableGraph.Create("Measure " + state);
        try
        {
            Animator animator = copy.GetComponentInChildren<Animator>(true);
            Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(leftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand) : null;
            if (hand == null) return null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // it's hidden; animate it anyway

            AnimationClipPlayable swing = AnimationClipPlayable.Create(graph, clip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, state, animator);
            output.SetSourcePlayable(swing);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            Transform body = animator.transform;
            float bestTime = 0f;
            float best = float.MinValue;
            float worst = float.MaxValue;
            for (float t = 0f; t <= searchTo; t += 1f / 60f)   // 60 looks per second of the clip
            {
                swing.SetTime(t);
                graph.Evaluate();
                Vector3 fromBody = hand.position - body.position;
                // Meters in front of the body, or (for a tool swing) how low the hand is: bigger = better either way.
                float score = lowest ? -Vector3.Dot(fromBody, body.up)
                            : highest ? Vector3.Dot(fromBody, body.up)
                            : Vector3.Dot(fromBody, body.forward);
                if (score > best)
                {
                    best = score;
                    bestTime = t;
                }
                worst = Mathf.Min(worst, score);
            }
            if (best - worst < 0.05f) return null;   // the hand didn't move: the clip couldn't be played here

            how = (lowest ? "when the hand is lowest in " : highest ? "when the hand is highest in " : "when the hand is furthest forward in ") + clip.name;
            return bestTime / speed;   // the state plays faster than the clip (Speed 2 = half the time)
        }
        catch (System.Exception e)
        {
            how = "couldn't measure " + clip.name + ": " + e.Message + ". Set its delay by hand";
            return null;
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(copy);
        }
    }

    // A state's clip and speed in a controller, and the character model it plays on (false = not there).
    static bool FindAction(string controllerPath, string modelPath, string state, out AnimationClip clip, out float speed,
                           out GameObject model)
    {
        clip = null;
        speed = 1f;
        model = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(modelPath));
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (model == null || controller == null) return false;

        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            foreach (ChildAnimatorState child in layer.stateMachine.states)
            {
                if (child.state.name == state && child.state.motion is AnimationClip found)
                {
                    clip = found;
                    speed = Mathf.Max(0.01f, child.state.speed);
                    return true;
                }
            }
        }
        return false;
    }

    // ----- The Player -----

    // Puts PunchEffect on the Player (if it isn't there) and gives it the prefab and the delay. False = no Player.
    static bool PutOnPlayer(GameObject prefab, float delay)
    {
        AddToPlayer<PunchEffect>();
        PunchEffect effect = Object.FindFirstObjectByType<PunchEffect>();
        if (effect == null) return false;

        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("burstPrefab").objectReferenceValue = prefab;
        so.FindProperty("impactDelay").floatValue = delay;
        so.ApplyModifiedProperties();   // recorded for Undo
        EditorSceneManager.MarkSceneDirty(effect.gameObject.scene);
        return true;
    }
}
