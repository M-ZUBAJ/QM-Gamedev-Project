/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Effects > Build Rock Break (Tools > World > Use Low Poly Rocks runs it too). Run it again any
              time: it refills the debris in place and only adds RockBreak where it's missing; Dust Color is measured
              again from each rock's material. Nothing in the scene changes. Guide: Docs/Rock Setup.md
Purpose: Rocks that break into pieces, in one click:
           - Assets/Effects/RockBreak/: RockDust.png + RockGravel.png (drawn here) and their particle materials, lit by
             the sun so they're darker at night (only if missing); Chunk 1-3.asset: small rock meshes for the chunks
             (the Low Poly Rock Pack's smallest rocks, else a cube), refilled each run;
           - Assets/Prefabs/Effects/RockDebris.prefab (refilled in place, so the rocks' links stay): the Chunks (3D
             pieces that tumble, bounce off the ground and shrink away; each break gives them the broken rock's own
             material), with Gravel and Dust inside. They make nothing by themselves: RockBreak emits them;
           - RockBreak on every rock prefab in Assets/Prefabs (a ResourceNode of type Stone), Debris filled in and Dust
             Color = the average color of the rock's material, a bit lighter. Prefabs first, then their Prefab Variants.
Requirement: RockBreakBuilder.cs x-> RockBreak.cs, ResourceNode.cs, MeshAssetHelper.cs, UIBuildHelper.cs, MenuOrder.cs
             RockBreakBuilder.cs <-x RockBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders

public static class RockBreakBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Rock Break";
    const string ArtFolder = "Assets/Effects/RockBreak";                          // the pictures, materials and chunk meshes
    const string DefaultPrefabPath = "Assets/Prefabs/Effects/RockDebris.prefab";
    const string RockFolder = "Assets/Prefabs";                                    // where it looks for rocks
    static readonly string[] ChunkSources = { "Rock Type1 04", "Rock Type2 04", "Rock Type6 04" };   // the pack's smallest rocks
    static readonly Color DefaultDust = new Color(0.66f, 0.65f, 0.62f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Effects/Build Rock Break", false, MenuOrder.BuildRockBreak)]
    static void FromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        Debug.Log(ToolName + ": " + Build() + " Test: Play and break a rock.");
    }

    // ===== My methods =====

    // Builds the debris and puts RockBreak on every rock prefab. Returns what it did, for the Console.
    public static string Build()
    {
        ParticleSystem debris = BuildDebris();
        if (debris == null) return "the RockDebris prefab couldn't be saved (the Console says why).";

        List<string> done = new List<string>();
        foreach (string path in RockPrefabs()) done.Add(AddTo(path, debris));
        AssetDatabase.SaveAssets();
        return FindPrefabPath(DefaultPrefabPath) + " made; RockBreak on " +
               (done.Count > 0 ? string.Join(", ", done) : "no rock prefab (a ResourceNode of type Stone in " + RockFolder + ")") + ".";
    }

    // ----- The rock prefabs -----

    // Every rock prefab (Turki's ResourceNode of type Stone), the ones that aren't Prefab Variants first: a variant gets
    // RockBreak from the prefab it's made from, so that one has to have it first.
    static List<string> RockPrefabs()
    {
        List<string> regular = new List<string>();
        List<string> variants = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { RockFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ResourceNode node = prefab != null ? prefab.GetComponent<ResourceNode>() : null;
            if (node == null || node.resourceType != ResourceNode.ResourceType.Stone) continue;
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant) variants.Add(path);
            else regular.Add(path);
        }
        regular.AddRange(variants);
        return regular;
    }

    // RockBreak on one rock prefab (kept if it's there), Debris and Dust Color filled in.
    static string AddTo(string path, ParticleSystem debris)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            RockBreak rockBreak = root.GetComponent<RockBreak>();
            bool added = rockBreak == null;
            if (added) rockBreak = root.AddComponent<RockBreak>();
            SerializedObject so = new SerializedObject(rockBreak);   // the names in quotes must match RockBreak's fields
            so.FindProperty("debris").objectReferenceValue = debris;
            so.FindProperty("dustColor").colorValue = DustColorOf(root);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return Path.GetFileNameWithoutExtension(path) + (added ? "" : " (had it)");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The dust's color for a rock: its material's average color, a bit lighter (dust is paler than the stone).
    static Color DustColorOf(GameObject rock)
    {
        MeshRenderer look = rock.GetComponentInChildren<MeshRenderer>();
        Material stone = look != null ? look.sharedMaterial : null;
        if (stone == null) return DefaultDust;
        Color dust = Color.Lerp(AverageColor(stone), Color.white, 0.2f);
        dust.a = 1f;
        return dust;
    }

    // A material's average color: its picture's pixels (read from the file) times its Color.
    static Color AverageColor(Material material)
    {
        Color tint = material.HasProperty("_Color") ? material.color : Color.white;
        Texture2D picture = material.mainTexture as Texture2D;
        string path = picture != null ? AssetDatabase.GetAssetPath(picture) : "";
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return tint;

        Texture2D copy = new Texture2D(2, 2);
        try
        {
            if (!copy.LoadImage(File.ReadAllBytes(path))) return tint;
            double r = 0, g = 0, b = 0;
            int count = 0;
            foreach (Color32 pixel in copy.GetPixels32())
            {
                if (pixel.a < 128) continue;   // see-through pixels aren't the stone's color
                r += pixel.r;
                g += pixel.g;
                b += pixel.b;
                count++;
            }
            if (count == 0) return tint;
            return new Color((float)(r / count / 255.0), (float)(g / count / 255.0), (float)(b / count / 255.0)) * tint;
        }
        finally
        {
            Object.DestroyImmediate(copy);
        }
    }

    // ----- The debris -----

    // Makes or refills RockDebris.prefab (in place, so the rocks' links to it stay). Returns its root ParticleSystem.
    static ParticleSystem BuildDebris()
    {
        MakeFolders(ArtFolder);
        Material dust = MakeMaterial("RockDust", SavePicture("RockDust", 64, false));
        Material gravel = MakeMaterial("RockGravel", SavePicture("RockGravel", 32, true));
        Mesh[] chunkShapes = MakeChunkShapes();

        string path = FindPrefabPath(DefaultPrefabPath);
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("RockDebris");
        try
        {
            FillChunks(Need<ParticleSystem>(root), chunkShapes, DefaultStone());
            FillGravel(Need<ParticleSystem>(Child(root, "Gravel")), gravel);
            FillDust(Need<ParticleSystem>(Child(root, "Dust")), dust);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            return saved != null ? saved.GetComponent<ParticleSystem>() : null;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // The chunks: small 3D rocks that fly, tumble, bounce off the ground, slow down and shrink away at the end.
    // RockBreak gives each chunk its place, speed and size, and the renderer the broken rock's material.
    static void FillChunks(ParticleSystem ps, Mesh[] shapes, Material stone)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // Unity only lets Duration change while stopped
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = true;               // never "finishes", so pieces can be added whenever a rock breaks
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);          // seconds each chunk lives
        main.startSpeed = 0f;                                                      // RockBreak gives each its speed
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);              // meters (RockBreak sets each)
        main.startRotation3D = true;                                               // turned every way, not only flat
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = 1.6f;    // stone is heavy: it falls fast
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // they stay where they flew
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 500;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;           // RockBreak places every chunk itself

        // Tumbling, a random way for each chunk, slowing down to a stop as it settles.
        ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;
        spin.enabled = true;
        spin.separateAxes = true;
        AnimationCurve slowLeft = new AnimationCurve(new Keyframe(0f, -1f), new Keyframe(1f, 0f));
        AnimationCurve slowRight = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        spin.x = new ParticleSystem.MinMaxCurve(7f, slowLeft, slowRight);   // radians per second at the start
        spin.y = new ParticleSystem.MinMaxCurve(7f, slowLeft, slowRight);
        spin.z = new ParticleSystem.MinMaxCurve(7f, slowLeft, slowRight);

        // They bounce off the ground (and anything else solid there).
        ParticleSystem.CollisionModule bounce = ps.collision;
        bounce.enabled = true;
        bounce.type = ParticleSystemCollisionType.World;
        bounce.mode = ParticleSystemCollisionMode.Collision3D;
        bounce.dampen = 0.45f;          // loses almost half its speed at each bounce
        bounce.bounce = 0.3f;           // bounces back up a little
        bounce.lifetimeLoss = 0f;
        bounce.quality = ParticleSystemCollisionQuality.High;   // exact, fine for a few dozen chunks
        bounce.radiusScale = 0.5f;
        bounce.collidesWith = ~(1 << 2);   // everything but the "Ignore Raycast" layer

        ParticleSystem.LimitVelocityOverLifetimeModule air = ps.limitVelocityOverLifetime;   // air slows them a little
        air.enabled = true;
        air.limit = 100f;
        air.dampen = 0f;
        air.drag = 0.3f;
        air.multiplyDragByParticleSize = false;
        air.multiplyDragByParticleVelocity = false;

        AnimationCurve shrink = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f));
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;   // full size, then shrinks away at the end
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.SetMeshes(shapes);                          // each chunk is one of these, at random
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.sharedMaterial = stone;                     // RockBreak swaps in the broken rock's material
        renderer.enableGPUInstancing = false;                // the rocks' Standard materials can't draw instanced particles
        renderer.shadowCastingMode = ShadowCastingMode.On;   // little shadows on the ground
        renderer.receiveShadows = true;
    }

    // Gravel: small specks that fly fast, bounce once or twice and fade. RockBreak colors each.
    static void FillGravel(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 1000;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.CollisionModule bounce = ps.collision;
        bounce.enabled = true;
        bounce.type = ParticleSystemCollisionType.World;
        bounce.mode = ParticleSystemCollisionMode.Collision3D;
        bounce.dampen = 0.5f;
        bounce.bounce = 0.2f;
        bounce.lifetimeLoss = 0f;
        bounce.quality = ParticleSystemCollisionQuality.Medium;   // cheaper: only the ground and other things that don't move
        bounce.collidesWith = ~(1 << 2);

        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 0.05f;   // share of the screen: specks right in front of the camera stay small
    }

    // Dust: soft puffs that roll out, grow, rise a little and fade. RockBreak colors each (see-through).
    static void FillDust(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = -0.03f;   // dust drifts up a little
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 300;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.LimitVelocityOverLifetimeModule air = ps.limitVelocityOverLifetime;   // it stops quickly
        air.enabled = true;
        air.limit = 100f;
        air.dampen = 0f;
        air.drag = 2.5f;
        air.multiplyDragByParticleSize = false;
        air.multiplyDragByParticleVelocity = false;

        ParticleSystem.RotationOverLifetimeModule swirl = ps.rotationOverLifetime;
        swirl.enabled = true;
        swirl.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

        AnimationCurve grow = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.5f));
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, grow);

        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 0.6f;
    }

    // The chunk meshes: the pack's smallest rocks, each centred on its middle and 1 m big (Start Size = meters).
    // Without the pack: Unity's cube.
    static Mesh[] MakeChunkShapes()
    {
        List<Mesh> shapes = new List<Mesh>();
        foreach (string source in ChunkSources)
        {
            Mesh mesh = null;
            foreach (string guid in AssetDatabase.FindAssets(source + " t:Model", new[] { "Assets" }))
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(modelPath) == source) mesh = MeshAssetHelper.MeshIn(modelPath);
                if (mesh != null) break;
            }
            if (mesh == null) continue;

            Bounds box = mesh.bounds;
            float biggest = Mathf.Max(box.size.x, box.size.y, box.size.z);
            if (biggest <= 0f) continue;
            Mesh chunk = MeshAssetHelper.SaveCopy(mesh, box.center, 1f / biggest, ArtFolder + "/Chunk " + (shapes.Count + 1) + ".asset");
            if (chunk != null) shapes.Add(chunk);
        }
        if (shapes.Count == 0) shapes.Add(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
        return shapes.ToArray();
    }

    // The pack's grey stone, shown in the prefab until a rock breaks (RockBreak swaps in the rock's own). Else Unity's default.
    static Material DefaultStone()
    {
        foreach (string guid in AssetDatabase.FindAssets("Grey t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("LowPolyRockPack") && Path.GetFileNameWithoutExtension(path) == "Grey")
                return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
    }

    // A see-through particle material with a picture, lit by the sun (darker at night). Made once: an existing one is
    // kept (only a missing picture is put back).
    static Material MakeMaterial(string fileName, Texture2D picture)
    {
        string path = ArtFolder + "/" + fileName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/VertexLit Blended");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat").shader;
            material = new Material(shader) { name = fileName, mainTexture = picture };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.mainTexture == null)
        {
            material.mainTexture = picture;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    // A white picture (each piece gets tinted), saved as a PNG only if the file isn't there yet (your own art is kept).
    // hard = a gravel speck with a lumpy edge; else a soft round puff of dust.
    static Texture2D SavePicture(string fileName, int size, bool hard)
    {
        string path = ArtFolder + "/" + fileName + ".png";
        if (!File.Exists(path))
        {
            Texture2D drawn = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;   // -1 .. 1
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);   // 0 in the middle, 1 at the edge
                    float angle = Mathf.Atan2(dy, dx);
                    float alpha;
                    byte shade = 255;
                    if (hard)
                    {
                        float edge = 0.72f + 0.14f * Mathf.Sin(angle * 3f + 1f) + 0.08f * Mathf.Sin(angle * 5f);   // lumpy
                        alpha = Mathf.Clamp01((edge - d) * size * 0.5f);       // a sharp edge, one pixel soft
                        shade = (byte)(Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(0.5f + (dy - dx) * 0.4f)) * 255f);   // lit from the top left
                    }
                    else
                    {
                        float puff = 1f - Mathf.Clamp01(d);
                        float wobble = 0.85f + 0.15f * Mathf.Sin(angle * 4f + d * 6f);   // not a perfect circle
                        alpha = puff * puff * (3f - 2f * puff) * wobble;
                    }
                    pixels[y * size + x] = new Color32(shade, shade, shade, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(alpha)));
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
            importer.maxTextureSize = size;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // A child object with this name (made if it isn't there), sitting right on its parent.
    static GameObject Child(GameObject parent, string childName)
    {
        Transform found = parent.transform.Find(childName);
        if (found != null) return found.gameObject;
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    // The component, added if it isn't there.
    static T Need<T>(GameObject target) where T : Component
    {
        T found = target.GetComponent<T>();
        return found != null ? found : target.AddComponent<T>();
    }
}
