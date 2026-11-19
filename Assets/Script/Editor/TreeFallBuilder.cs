/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > Effects > Build Tree Fall. Run it again any time (and after adding a tree look): it refills
              the chips and the closed trunks in place and only adds TreeFall where it's missing. Nothing in the scene
              changes (the trees there are copies of the prefabs). Guide: Docs/Tree Fall Setup.md
Purpose: A falling tree and brown wood chips when a tree is chopped down, in one click:
           - Assets/Effects/WoodChips/WoodChip.png + WoodChip.mat (only if missing): a small splinter picture and a
             see-through particle material that the sun lights (darker at night);
           - Assets/Prefabs/Effects/WoodChips.prefab (refilled in place, so the trees' links stay): splinters that
             tumble, fall and fade; they make nothing by themselves (FallingTree emits them, each a random brown);
           - Assets/Effects/TreeFall/CutWood.png + CutWood.mat (only if missing): light, fresh-cut wood with rings;
           - Assets/Effects/TreeFall/Closed Trunks/<model> - <mesh> Closed.asset (refilled in place): each tree mesh
             with the bottom of its trunk closed. The pack's trunks are open tubes there (hidden in the ground while
             the tree stands); a falling tree turned that hole toward the player, who saw straight through the trunk;
           - TreeFall on every tree prefab in Assets/Prefabs (a ResourceNode of type Tree that isn't picked with a key,
             so not a berry bush): Turki's TreeNode first, its looks get it from there; Chips, Cut Wood and Closed
             Meshes (each prefab's own meshes) filled in.
Requirement: TreeFallBuilder.cs x-> TreeFall.cs, ResourceNode.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders

public static class TreeFallBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Tree Fall";
    const string ArtFolder = "Assets/Effects/WoodChips";                         // the picture and its material
    const string DefaultPrefabPath = "Assets/Prefabs/Effects/WoodChips.prefab";
    const string TreeFolder = "Assets/Prefabs";                                   // where it looks for trees
    const int PictureSize = 32;   // pixels; a small splinter needs no more
    const string CutFolder = "Assets/Effects/TreeFall";                          // the cut wood and the closed trunks
    const string MeshFolder = CutFolder + "/Closed Trunks";
    const int RingsSize = 64;          // pixels for the tree rings
    const float BottomShare = 0.1f;    // a hole in the lowest tenth of a tree is the bottom of its trunk

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Effects/Build Tree Fall", false, MenuOrder.BuildTreeFall)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }

        ParticleSystem chips = BuildChips();
        if (chips == null)
        {
            EditorUtility.DisplayDialog(ToolName, "The WoodChips prefab couldn't be saved. The Console says why.", "OK");
            return;
        }

        Material cutWood = MakeCutWood();
        Dictionary<Mesh, Mesh> closedOnce = new Dictionary<Mesh, Mesh>();   // each mesh closed once, even if trees share it
        List<string> trees = TreePrefabs();
        List<string> done = new List<string>();
        foreach (string path in trees) done.Add(AddTo(path, chips, cutWood, closedOnce));
        AssetDatabase.SaveAssets();

        int closedCount = 0;
        foreach (Mesh closed in closedOnce.Values)
        {
            if (closed != null) closedCount++;
        }
        Debug.Log(ToolName + ": " + FindPrefabPath(DefaultPrefabPath) + " made; TreeFall on " +
                  (done.Count > 0 ? string.Join(", ", done) : "no tree prefab (a ResourceNode of type Tree in " + TreeFolder + ")") +
                  "; " + closedCount + " trunk mesh(es) closed at the bottom in " + MeshFolder + ". Test: Play and chop a tree down.");
    }

    // ===== My methods =====

    // Every tree prefab (not a berry bush), the ones that aren't Prefab Variants first: a variant gets TreeFall from
    // the prefab it's made from, so that one has to have it first.
    static List<string> TreePrefabs()
    {
        List<string> regular = new List<string>();
        List<string> variants = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { TreeFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || !IsTree(prefab)) continue;
            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant) variants.Add(path);
            else regular.Add(path);
        }
        regular.AddRange(variants);
        return regular;
    }

    // A tree: Turki's ResourceNode of type Tree that isn't picked with a key (his berry bush is type Tree too).
    static bool IsTree(GameObject prefab)
    {
        ResourceNode node = prefab.GetComponent<ResourceNode>();
        if (node == null || node.resourceType != ResourceNode.ResourceType.Tree) return false;
        SerializedProperty byKey = new SerializedObject(node).FindProperty("collectWithKey");
        return byKey == null || !byKey.boolValue;
    }

    // TreeFall on one tree prefab (kept if it's there, e.g. from the prefab a look is made from), with Chips, Cut Wood
    // and Closed Meshes (the closed version of each of its meshes) filled in.
    static string AddTo(string path, ParticleSystem chips, Material cutWood, Dictionary<Mesh, Mesh> closedOnce)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            TreeFall fall = root.GetComponent<TreeFall>();
            bool added = fall == null;
            if (added) fall = root.AddComponent<TreeFall>();
            SerializedObject so = new SerializedObject(fall);   // the names in quotes must match TreeFall's fields
            so.FindProperty("chips").objectReferenceValue = chips;
            so.FindProperty("cutWood").objectReferenceValue = cutWood;

            SerializedProperty list = so.FindProperty("closedMeshes");
            list.ClearArray();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh open = filter.sharedMesh;
                if (open == null) continue;
                if (!closedOnce.TryGetValue(open, out Mesh closed))
                {
                    closed = ClosedTrunk(open);
                    closedOnce[open] = closed;
                }
                if (closed == null) continue;   // nothing open at the bottom
                list.InsertArrayElementAtIndex(list.arraySize);
                SerializedProperty pair = list.GetArrayElementAtIndex(list.arraySize - 1);
                pair.FindPropertyRelative("open").objectReferenceValue = open;
                pair.FindPropertyRelative("closed").objectReferenceValue = closed;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return Path.GetFileNameWithoutExtension(path) + (added ? "" : " (had it)");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ----- The chips -----

    // Makes or refills WoodChips.prefab (in place, so the trees' links to it stay). Returns its ParticleSystem.
    static ParticleSystem BuildChips()
    {
        Material material = MakeMaterial();
        string path = FindPrefabPath(DefaultPrefabPath);
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("WoodChips");
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

    // Splinters that tumble, fall back down and fade. They make nothing by themselves: FallingTree calls Emit, with
    // each chip's own speed and brown.
    static void Fill(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // Unity only lets Duration change while stopped
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = true;               // never "finishes", so chips can be added whenever FallingTree wants
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);         // seconds each chip lives
        main.startSpeed = 0f;                                                     // FallingTree gives each its speed
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);           // meters
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);  // any angle (in radians)
        main.startColor = Color.white;  // FallingTree gives each chip a brown
        main.gravityModifier = 1.1f;    // chips fall back down
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // they stay where they flew, the tree moves on
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 4000;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;           // FallingTree places every chip itself

        ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;   // air slows them a little
        slow.enabled = true;
        slow.limit = 100f;
        slow.dampen = 0f;
        slow.drag = 0.8f;
        slow.multiplyDragByParticleSize = false;
        slow.multiplyDragByParticleVelocity = false;

        ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;   // they tumble as they fly
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);   // radians per second

        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);

        AnimationCurve shrink = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.6f));
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.maxParticleSize = 0.08f;   // share of the screen: chips right in front of the camera stay small
    }

    // The see-through material with the splinter picture. Made once: an existing one is kept (only a missing picture
    // is put back).
    static Material MakeMaterial()
    {
        MakeFolders(ArtFolder);
        Texture2D texture = SavePicture();
        string path = ArtFolder + "/WoodChip.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(ParticleShader()) { name = "WoodChip", mainTexture = texture };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.mainTexture == null)
        {
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    // A see-through particle shader the sun lights (so chips are darker at night, like the tree), from Unity's
    // Built-in shaders; the plain unlit one if it's missing.
    static Shader ParticleShader()
    {
        Shader shader = Shader.Find("Legacy Shaders/Particles/VertexLit Blended");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat").shader;
        return shader;
    }

    // A small splinter (a long thin box with soft ends), white so each chip can be tinted, saved as a PNG (only if
    // the file isn't there yet, so your own art is kept).
    static Texture2D SavePicture()
    {
        string path = ArtFolder + "/WoodChip.png";
        if (!File.Exists(path))
        {
            int n = PictureSize;
            Texture2D drawn = new Texture2D(n, n, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f);   // 0 in the middle, 1 at the edge
                    float v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                    float across = Mathf.Clamp01((0.42f - v) / 0.12f);   // a strip about 40 % as tall as the picture
                    float along = Mathf.Clamp01((0.9f - u) / 0.2f);      // soft left and right ends
                    float alpha = across * along;
                    byte shade = (byte)(Mathf.Lerp(0.85f, 1f, 1f - v) * 255f);   // a lighter middle, like fresh wood
                    pixels[y * n + x] = new Color32(shade, shade, shade, (byte)Mathf.RoundToInt(255f * alpha));
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

    // ----- The closed trunk -----

    // The tree mesh with every hole at its bottom closed. The pack's trunks are open tubes there: a standing tree hides
    // that end in the ground, but a falling one turns it toward the player, who saw straight through the trunk. Each
    // hole gets a flat face in a new sub-mesh, so it can have its own material (Cut Wood). Saved in MeshFolder and
    // refilled in place (the trees' links to it stay). Null = nothing open at the bottom, or the mesh can't be read.
    static Mesh ClosedTrunk(Mesh open)
    {
        Vector3[] corners = open.vertices;
        if (corners.Length == 0)
        {
            Debug.LogWarning(ToolName + ": the corners of " + open.name + " can't be read, so its trunk stays open.");
            return null;
        }
        List<List<Vector3>> holes = BottomHoles(open, corners);
        if (holes.Count == 0) return null;

        // Everything the mesh already has...
        int count = corners.Length;
        Vector3[] ownNormals = open.normals;
        Vector2[] ownUVs = open.uv;
        Color32[] ownColors = open.colors32;
        Vector4[] ownTangents = open.tangents;
        List<Vector3> vertices = new List<Vector3>(corners);
        List<Vector2> uvs = new List<Vector2>(ownUVs.Length == count ? ownUVs : new Vector2[count]);
        List<Vector3> normals = ownNormals.Length == count ? new List<Vector3>(ownNormals) : null;     // null = it has none
        List<Color32> colors = ownColors.Length == count ? new List<Color32>(ownColors) : null;
        List<Vector4> tangents = ownTangents.Length == count ? new List<Vector4>(ownTangents) : null;
        List<int> cut = new List<int>();

        // ...and a flat face across each hole: a fan of triangles from its middle, facing out of the trunk.
        foreach (List<Vector3> hole in holes)
        {
            Vector3 middle = Vector3.zero;
            foreach (Vector3 corner in hole) middle += corner;
            middle /= hole.Count;

            // The hole's corners go around the way the trunk's own faces use those edges, so a face closing it uses
            // them the other way round (corner i + 1, then i) to face the same way: outward. Unity shows a triangle
            // from the side Cross(b - a, c - a) points to; at the bottom of a trunk that has to be down.
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < hole.Count; i++)
            {
                Vector3 a = hole[(i + 1) % hole.Count];
                normal += Vector3.Cross(hole[i] - a, middle - a);
            }
            bool flip = normal.y > 0f;   // the mesh goes around the other way: turn the face over
            if (flip) normal = -normal;
            normal = normal.sqrMagnitude > 0f ? normal.normalized : Vector3.down;

            // The tree rings' picture lies flat on the face, its middle in the middle of the hole.
            Vector3 across = Vector3.Cross(normal, Vector3.forward);
            if (across.sqrMagnitude < 0.01f) across = Vector3.Cross(normal, Vector3.right);
            across.Normalize();
            Vector3 along = Vector3.Cross(normal, across);
            float radius = 0f;
            foreach (Vector3 corner in hole) radius = Mathf.Max(radius, (corner - middle).magnitude);
            if (radius <= 0f) radius = 1f;

            int first = vertices.Count;   // the middle, then the hole's corners (copies: they need their own normal)
            List<Vector3> fan = new List<Vector3>(hole.Count + 1) { middle };
            fan.AddRange(hole);
            foreach (Vector3 corner in fan)
            {
                Vector3 offset = corner - middle;
                vertices.Add(corner);
                uvs.Add(new Vector2(0.5f + Vector3.Dot(offset, across) * 0.5f / radius,
                                    0.5f + Vector3.Dot(offset, along) * 0.5f / radius));
                if (normals != null) normals.Add(normal);
                if (colors != null) colors.Add(new Color32(255, 255, 255, 255));
                if (tangents != null) tangents.Add(new Vector4(across.x, across.y, across.z, 1f));
            }
            for (int i = 0; i < hole.Count; i++)
            {
                int a = first + 1 + i;
                int b = first + 1 + (i + 1) % hole.Count;
                if (flip) cut.AddRange(new[] { a, b, first });
                else cut.AddRange(new[] { b, a, first });
            }
        }

        // Saved: a new file, or the old one refilled (Clear keeps the file, so the trees' links to it stay).
        MakeFolders(MeshFolder);
        string model = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(open));
        string fileName = string.IsNullOrEmpty(model) || model == open.name ? open.name : model + " - " + open.name;
        foreach (char bad in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(bad, '_');
        string path = MeshFolder + "/" + fileName + " Closed.asset";
        Mesh closed = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = closed == null;
        if (isNew) closed = new Mesh();
        else closed.Clear();

        closed.name = fileName + " Closed";
        closed.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        closed.SetVertices(vertices);
        closed.SetUVs(0, uvs);
        if (normals != null) closed.SetNormals(normals);
        if (colors != null) closed.SetColors(colors);
        if (tangents != null) closed.SetTangents(tangents);
        int own = open.subMeshCount;
        closed.subMeshCount = own + 1;   // its own parts, then the cut
        for (int s = 0; s < own; s++) closed.SetTriangles(open.GetTriangles(s), s, false);
        closed.SetTriangles(cut, own, false);
        if (normals == null) closed.RecalculateNormals();
        closed.RecalculateBounds();

        if (isNew) AssetDatabase.CreateAsset(closed, path);
        else EditorUtility.SetDirty(closed);
        return closed;
    }

    // The holes in a mesh's lowest tenth (BottomShare): loops of edges that only one triangle uses. A low-poly mesh has
    // its corners copied for every face (for the flat look), so corners in the same spot count as one.
    static List<List<Vector3>> BottomHoles(Mesh mesh, Vector3[] corners)
    {
        Dictionary<Vector3Int, int> spotAt = new Dictionary<Vector3Int, int>();
        List<Vector3> spots = new List<Vector3>();
        int[] spotOf = new int[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3Int key = Vector3Int.RoundToInt(corners[i] * 10000f);   // the same tenth of a millimetre
            if (!spotAt.TryGetValue(key, out int spot))
            {
                spot = spots.Count;
                spots.Add(corners[i]);
                spotAt[key] = spot;
            }
            spotOf[i] = spot;
        }

        // How many triangles use each edge, and every edge in the direction its triangle goes around.
        Dictionary<long, int> uses = new Dictionary<long, int>();
        List<Vector2Int> edges = new List<Vector2Int>();
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] triangles = mesh.GetTriangles(sub);
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                for (int k = 0; k < 3; k++)
                {
                    int from = spotOf[triangles[t + k]];
                    int to = spotOf[triangles[t + (k + 1) % 3]];
                    if (from == to) continue;   // a squashed triangle
                    long key = EdgeKey(from, to);
                    uses.TryGetValue(key, out int n);
                    uses[key] = n + 1;
                    edges.Add(new Vector2Int(from, to));
                }
            }
        }
        Dictionary<int, int> next = new Dictionary<int, int>();   // the edges only one triangle uses: from -> to
        foreach (Vector2Int edge in edges)
        {
            if (uses[EdgeKey(edge.x, edge.y)] == 1) next[edge.x] = edge.y;
        }

        // Walk around each loop of those edges and keep the ones at the bottom.
        float top = mesh.bounds.min.y + mesh.bounds.size.y * BottomShare;
        List<List<Vector3>> holes = new List<List<Vector3>>();
        HashSet<int> walked = new HashSet<int>();
        foreach (int start in next.Keys)
        {
            if (walked.Contains(start)) continue;
            List<Vector3> loop = new List<Vector3>();
            bool low = true;
            int at = start;
            while (walked.Add(at) && next.TryGetValue(at, out int to))
            {
                loop.Add(spots[at]);
                if (spots[at].y > top) low = false;
                at = to;
            }
            if (at == start && loop.Count >= 3 && low) holes.Add(loop);   // went all the way around, low enough
        }
        return holes;
    }

    // One number for an edge between two corners, the same whichever way round.
    static long EdgeKey(int a, int b)
    {
        return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
    }

    // ----- The cut wood -----

    // Light, fresh-cut wood with tree rings, for the closed end of a falling trunk. Made once: an existing material is
    // kept (only a missing picture is put back).
    static Material MakeCutWood()
    {
        MakeFolders(CutFolder);
        Texture2D rings = SaveRings();
        string path = CutFolder + "/CutWood.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");   // lit like the trees
            if (shader == null) shader = Shader.Find("Diffuse");
            material = new Material(shader) { name = "CutWood", mainTexture = rings };
            material.SetFloat("_Glossiness", 0.1f);   // dull, like wood (0 = matte, 1 = mirror)
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.mainTexture == null)
        {
            material.mainTexture = rings;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    // Tree rings drawn as a PNG (only if the file isn't there yet, so your own art is kept): light wood, a few darker
    // rings that aren't perfectly round, a little darker near the bark, and a dark dot in the middle.
    static Texture2D SaveRings()
    {
        string path = CutFolder + "/CutWood.png";
        if (!File.Exists(path))
        {
            int n = RingsSize;
            Color light = new Color(0.86f, 0.7f, 0.48f);   // fresh wood
            Color ring = new Color(0.68f, 0.5f, 0.31f);    // the rings
            Color pith = new Color(0.5f, 0.34f, 0.2f);     // the dot in the middle
            Texture2D drawn = new Texture2D(n, n, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;   // -1 .. 1
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);   // 0 in the middle, 1 at the edge
                    float angle = Mathf.Atan2(dy, dx);
                    float wobble = 0.025f * Mathf.Sin(angle * 5f) + 0.015f * Mathf.Sin(angle * 3f + 1.3f);
                    float place = Mathf.Repeat((d + wobble) * 4.5f, 1f);              // 4 or 5 rings to the edge
                    float onRing = Mathf.Clamp01(1f - Mathf.Abs(place - 0.5f) / 0.14f);   // 1 on a ring, 0 between
                    Color c = Color.Lerp(light, ring, Mathf.SmoothStep(0f, 1f, onRing) * 0.85f);
                    c = Color.Lerp(c, ring, Mathf.Clamp01((d - 0.8f) / 0.2f) * 0.5f);   // near the bark
                    c = Color.Lerp(c, pith, Mathf.Clamp01((0.09f - d) / 0.04f));      // the middle
                    pixels[y * n + x] = c;
                }
            }
            drawn.SetPixels32(pixels);
            drawn.Apply();
            File.WriteAllBytes(path, drawn.EncodeToPNG());
            Object.DestroyImmediate(drawn);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = RingsSize;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
