/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Effects > Build Death Particles (any scene open). Run it again any time: the particle prefab
              is refilled in place, and your own picture or material changes are kept. Tools > World > Build Wild Animal
              and Tools > Enemies > Build Monster Enemy also call it for the creatures they make.
              Hand-made steps: Docs/Death Effect Setup.md
Purpose: Makes the death particles and gives them to the creatures:
         - Assets/Effects/DeathParticles: a soft glowing dot picture (DeathDot.png) and its see-through material.
         - Assets/Prefabs/Effects/DeathParticles.prefab: white particles that float up, swirl a little and fade.
           DeathEffect colors them (green / red) and decides where they appear.
         - DeathEffect on every prefab with Tony's WildAnimal or EnemyAnimator (DeerNode, SlimeEnemy). For one with a
           death animation it times the dissolve to that animation (it starts at 30 % and ends with it) and ticks
           Read/Write on its model, so the particles can come off the body's real surface.
Requirement: DeathEffectBuilder.cs x-> DeathEffect.cs, WildAnimal.cs, EnemyAnimator.cs, UIBuildHelper.cs, MenuOrder.cs
             DeathEffectBuilder.cs <-x EnemyBuilder.cs, WildAnimalBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders

public static class DeathEffectBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Death Particles";
    const string ArtFolder = "Assets/Effects/DeathParticles";                     // the picture and its material
    const string DefaultPrefabPath = "Assets/Prefabs/Effects/DeathParticles.prefab";
    const string CreatureFolder = "Assets/Prefabs";                               // where it looks for creatures
    const int PictureSize = 64;          // pixels; a small soft dot needs no more
    const float DissolveFrom = 0.3f;     // share of the death animation before the body starts turning into particles

    // Model files to tick Read/Write on, once no prefab is open (re-importing a model while its prefab is open is risky).
    static readonly HashSet<string> needReadable = new HashSet<string>();

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Effects/Build Death Particles", false, MenuOrder.BuildDeathParticles)]
    static void Build()
    {
        if (BuildParticles() == null)
        {
            EditorUtility.DisplayDialog(ToolName, "Couldn't save " + DefaultPrefabPath + ". See the Console for why.", "OK");
            return;
        }

        List<string> done = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { CreatureFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || !IsCreature(prefab)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                done.Add(AddTo(root));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        string readable = MakeMeshesReadable();
        AssetDatabase.SaveAssets();

        Debug.Log(ToolName + ": particles saved in " + FindPrefabPath(DefaultPrefabPath) + ". " +
                  (done.Count > 0 ? "DeathEffect on: " + string.Join("; ", done) + "." : "No creature found (build one with Tools > World > Build Wild Animal or Tools > Enemies > Build Monster Enemy).") +
                  readable + " Test: Play and kill a deer (green) or a slime (red).");
    }

    // ===== My methods =====

    // A creature made by Tony's tools: an animal (WildAnimal) or an animated enemy (EnemyAnimator).
    static bool IsCreature(GameObject prefab)
    {
        return prefab.GetComponent<WildAnimal>() != null || prefab.GetComponent<EnemyAnimator>() != null;
    }

    // Puts a DeathEffect with the particles on a creature (root = its top object, in an opened prefab or one being
    // built), timed to its death animation if it has one. Returns a short report. After saving and closing the prefab,
    // call MakeMeshesReadable().
    public static string AddTo(GameObject root)
    {
        ParticleSystem particles = LoadParticles();
        if (particles == null) particles = BuildParticles();

        DeathEffect effect = root.GetComponent<DeathEffect>();
        if (effect == null) effect = root.AddComponent<DeathEffect>();
        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("particles").objectReferenceValue = particles;

        string report = root.name + " (" + (effect.IsHarmful ? "red" : "green");
        float? length = root.GetComponent<EnemyAnimator>() != null ? DeathAnimationLength(root.GetComponentInChildren<Animator>(true)) : null;
        if (length.HasValue)
        {
            so.FindProperty("dissolveStart").floatValue = length.Value * DissolveFrom;
            so.FindProperty("dissolveSeconds").floatValue = Mathf.Max(0.3f, length.Value * (1f - DissolveFrom));
            WantReadable(root);
            report += ", turns into particles during its " + length.Value.ToString("0.0") + " s death animation";
        }
        else
        {
            report += ", bursts when it dies";
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return report + ")";
    }

    // Ticks Read/Write on the models AddTo asked for, so DeathEffect may read their meshes while playing (the particles
    // come off the real surface). Returns a sentence for the Console ("" if nothing changed).
    public static string MakeMeshesReadable()
    {
        List<string> changed = new List<string>();
        foreach (string path in needReadable)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || importer.isReadable) continue;
            importer.isReadable = true;
            importer.SaveAndReimport();
            changed.Add(Path.GetFileName(path));
        }
        needReadable.Clear();
        return changed.Count > 0 ? " Read/Write ticked on " + string.Join(", ", changed) + " (so the particles come off the body's surface)." : "";
    }

    static void WantReadable(GameObject root)
    {
        SkinnedMeshRenderer best = null;
        foreach (SkinnedMeshRenderer skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (skin.sharedMesh == null) continue;
            if (best == null || skin.sharedMesh.vertexCount > best.sharedMesh.vertexCount) best = skin;
        }
        if (best == null || best.sharedMesh.isReadable) return;
        string path = AssetDatabase.GetAssetPath(best.sharedMesh);
        if (AssetImporter.GetAtPath(path) is ModelImporter) needReadable.Add(path);
    }

    // Seconds the death state plays (its clip length / its speed): a state named "Die" or "Death". null = none.
    static float? DeathAnimationLength(Animator animator)
    {
        AnimatorController controller = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
        if (controller == null) return null;
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            foreach (ChildAnimatorState child in layer.stateMachine.states)
            {
                string name = child.state.name.ToLower();
                if (!name.Contains("die") && !name.Contains("death")) continue;
                if (child.state.motion is AnimationClip clip && clip.length > 0f)
                {
                    return clip.length / Mathf.Max(0.05f, Mathf.Abs(child.state.speed));
                }
            }
        }
        return null;
    }

    static ParticleSystem LoadParticles()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(DefaultPrefabPath));
        return prefab != null ? prefab.GetComponent<ParticleSystem>() : null;
    }

    // ----- The particles -----

    // Makes or refills DeathParticles.prefab (in place, so creatures' links to it stay). Returns its ParticleSystem.
    static ParticleSystem BuildParticles()
    {
        Material material = MakeMaterial();
        string path = FindPrefabPath(DefaultPrefabPath);
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("DeathParticles");
        try
        {
            ParticleSystem ps = root.GetComponent<ParticleSystem>();
            if (ps == null) ps = root.AddComponent<ParticleSystem>();
            Fill(ps, material);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            return saved != null ? saved.GetComponent<ParticleSystem>() : null;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // White dots that float up a little, swirl and fade. They make nothing by themselves: DeathEffect calls Emit.
    static void Fill(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // Unity only lets Duration change while stopped
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = true;               // never "finishes", so particles can be added whenever DeathEffect wants
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);   // seconds each dot lives
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);      // m/s, out of the body's surface
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);     // meters
        main.startColor = Color.white;  // DeathEffect sets green or red
        main.gravityModifier = -0.12f;  // a little lighter than air: they drift up like sparks
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // they stay where they came off, while the body shrinks
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 3000;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;   // DeathEffect switches it to the body's mesh for the slime
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;   // air slows them down
        slow.enabled = true;
        slow.limit = 100f;
        slow.dampen = 0f;
        slow.drag = 1.5f;
        slow.multiplyDragByParticleSize = false;
        slow.multiplyDragByParticleVelocity = false;

        ParticleSystem.NoiseModule swirl = ps.noise;   // a gentle wobble, so they don't fly in straight lines
        swirl.enabled = true;
        swirl.strength = 0.35f;
        swirl.frequency = 1.2f;
        swirl.scrollSpeed = 0.5f;
        swirl.damping = true;
        swirl.quality = ParticleSystemNoiseQuality.Medium;

        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);

        AnimationCurve grow = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.15f));
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, grow);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 0.06f;   // share of the screen: dots right in front of the camera stay small
    }

    // The see-through material with the dot picture. Made once: an existing one is kept (only a missing picture is put back).
    static Material MakeMaterial()
    {
        MakeFolders(ArtFolder);
        Texture2D texture = SavePicture();
        string path = ArtFolder + "/DeathParticle.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(ParticleShader()) { name = "DeathParticle", mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.mainTexture == null)
        {
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    // Unity's simple see-through particle shader (Built-in render pipeline, works in WebGL): picture x particle color,
    // no lighting, so green and red look the same by day and by night.
    static Shader ParticleShader()
    {
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");
        if (shader == null) shader = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat").shader;
        return shader;
    }

    // A soft round dot with a bright middle, saved as a PNG (only if the file isn't there yet, so your own art is kept).
    static Texture2D SavePicture()
    {
        string path = ArtFolder + "/DeathDot.png";
        if (!File.Exists(path))
        {
            int n = PictureSize;
            Texture2D drawn = new Texture2D(n, n, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f;
                    float v = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);                  // 0 in the middle, 1 at the edge
                    float alpha = Mathf.Clamp01(Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f) * 1.4f);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * alpha));
                }
            }
            drawn.SetPixels32(pixels);
            drawn.Apply();
            File.WriteAllBytes(path, drawn.EncodeToPNG());
            Object.DestroyImmediate(drawn);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = PictureSize;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
