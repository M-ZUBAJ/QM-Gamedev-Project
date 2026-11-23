/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Berry Bushes (needs the Yughues Free Bushes 2018 pack, in Assets/Imported3D).
              Run it again any time: the bush is rebuilt in place, Turki's ResourceNode settings and your BerryBush
              numbers stay, and no scene changes. Guide: Docs/Berry Bush Setup.md
Purpose: Turns Turki's placeholder berry bush (a green ball with three red balls) into a real-looking one, in one click:
           - Assets/Prefabs/ResourceNodes/BerryBush/Berry Cluster.asset: three small red berries in one mesh (made here,
             refilled each run) and Assets/Materials/BerryBush/Berry.mat (shiny red, only made if missing);
           - each look in Looks, read from the pack (P_Bush01: green leaves in rows, like an elderberry; P_Bush05:
             twigs with small round leaves, like a currant bush): its 4 levels of detail and material, a Body box
             around the thick part of its leaves, and spots for berries: rays toward the bush from all around and
             from above stop at the first leaf that is really there (solid in the leaf picture, not its see-through
             edge), and the spots farthest apart are kept;
           - Assets/Prefabs/ResourceNodes/BerryNode.prefab rebuilt in place (scenes and ObjectScatter rules keep their
             link to it): the old balls go, Model > LOD0-3 (the leaves) and Berries > Berry 1..N come in, with a LOD
             Group (berries only drawn on the 3 nearest levels: farther they're too small to see), a Box Collider
             instead of the Sphere Collider, Tony's BerryBush (filled in; the first time Fewest / Most Berries =
             Turki's Drop Min / Max) and StandOnGround (so it stands on the ground whatever height the scatter rule
             spawns it at: the rules still say 0.6 m, made for the old ball). Turki's ResourceNode stays as it is.
Requirement: BerryBushBuilder.cs x-> BerryBush.cs, ResourceNode.cs, StandOnGround.cs, ShapeColor.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode
using static UIBuildHelper;    // MakeFolders

public static class BerryBushBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Berry Bushes";
    const string DefaultBushPath = "Assets/Prefabs/ResourceNodes/BerryNode.prefab";   // Turki's berry bush
    const string MeshFolder = "Assets/Prefabs/ResourceNodes/BerryBush";
    const string ClusterPath = MeshFolder + "/Berry Cluster.asset";
    const string MaterialFolder = "Assets/Materials/BerryBush";
    const string BerryMaterialPath = MaterialFolder + "/Berry.mat";

    // The Yughues bushes a berry bush can wear: the name shown in BerryBush's Looks, and the pack's prefab. The pack's
    // other three aren't used: P_Bush02 has orange autumn leaves, P_Bush03 purple flowers, P_Bush04 leaves like bamboo.
    static readonly (string name, string prefab)[] Looks =
    {
        ("Leafy", "P_Bush01"),    // green leaves in rows, like an elderberry
        ("Twiggy", "P_Bush05"),   // twigs with small round leaves, like a currant bush
    };

    const int Levels = 4;                  // levels of detail: near to far
    const int BerryLevels = 3;             // the berries are drawn on the 3 nearest (farther they're too small to see)
    static readonly float[] DefaultHeights = { 0.35f, 0.15f, 0.05f, 0.01f };   // screen share where each level ends

    const float BerryRadius = 0.035f;      // meters: 7 cm berries (real ones are smaller, but they'd vanish a few steps away)
    static readonly Color BerryRed = new Color(0.72f, 0.04f, 0.09f);
    const int SpotsPerLook = 12;           // berry spots kept for each look (a bush uses 3 - 5 of them, in its own order)
    const float SpotGap = 0.16f;           // meters between two berry clusters at least
    const int SideRays = 700;              // rays toward the bush from all around ...
    const int TopRays = 250;               // ... and from above, looking for leaves to put berries on
    const int MostParts = 12;              // berries made at most for one bush

    // One look read from the pack, with its Body box and berry spots worked out.
    class Found
    {
        public string name;
        public Mesh[] meshes;
        public Material material;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float[] heights;
        public Bounds box;
        public List<BerryBush.Spot> spots;
    }

    // What a leaf picture lets through: its pixels (their see-through value) and the material's Alpha Cutoff.
    class Leaf
    {
        public Color32[] pixels;
        public int width;
        public int height;
        public byte cutoff = 128;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;

        // True where the leaf picture is solid at this UV (no picture = solid everywhere).
        public bool SolidAt(Vector2 uv)
        {
            if (pixels == null) return true;
            uv = Vector2.Scale(uv, tiling) + offset;
            int x = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(uv.x, 1f) * width), 0, width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(uv.y, 1f) * height), 0, height - 1);
            return pixels[y * width + x].a >= cutoff;
        }
    }

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build Berry Bushes", false, MenuOrder.BuildBerryBushes)]
    static void FromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        string path = BushPath();
        if (path == null)
        {
            EditorUtility.DisplayDialog(ToolName, "No berry bush found: Turki's BerryNode prefab (a ResourceNode with " +
                                        "Collect With Key ticked) in Assets/Prefabs.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(ToolName,
                "Rebuild " + path + " as a Yughues bush with red berries?\n\n" +
                "Turki's ResourceNode settings stay (Drop Item, health). The green ball and its red balls are replaced. " +
                "Scenes don't change: they keep their link to the bush.", "Rebuild", "Cancel")) return;

        string report = Build(path);
        Debug.Log(ToolName + ": " + report);
        EditorUtility.DisplayDialog(ToolName, report + "\n\nTest: Play, find a berry bush on the grass, look at it from " +
                                    "close by and press E.", "OK");
    }

    // ===== My methods =====

    // Rebuilds the berry bush prefab at "path". Returns what it did.
    public static string Build(string path)
    {
        List<Found> looks = new List<Found>();
        List<string> missing = new List<string>();
        for (int i = 0; i < Looks.Length; i++)
        {
            Found look = ReadLook(Looks[i].name, Looks[i].prefab, i, out string problem);
            if (look != null) looks.Add(look);
            else missing.Add(problem);
        }
        if (looks.Count == 0)
            return "nothing done: " + string.Join(", ", missing) + " not found. Import Yughues Free Bushes 2018 (Asset Store) first.";

        MakeFolders(MeshFolder);
        MakeFolders(MaterialFolder);
        Mesh cluster = BuildCluster();
        Material red = BerryMaterial();
        string bush = Rebuild(path, looks, cluster, red);
        AssetDatabase.SaveAssets();

        List<string> made = new List<string>();
        foreach (Found look in looks) made.Add(look.name + ": " + look.spots.Count + " berry spots");
        return bush + "; looks " + string.Join(", ", made) +
               (missing.Count > 0 ? "; NOT FOUND: " + string.Join(", ", missing) : "") +
               "; berries: " + ClusterPath + " + " + BerryMaterialPath + ".";
    }

    // ----- Finding things -----

    // Turki's berry bush: BerryNode, else the first prefab in Assets/Prefabs with a ResourceNode picked with a key.
    static string BushPath()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBushPath) != null) return DefaultBushPath;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ResourceNode node = prefab != null ? prefab.GetComponent<ResourceNode>() : null;
            if (node != null && new SerializedObject(node).FindProperty("collectWithKey").boolValue) return path;
        }
        return null;
    }

    // The pack prefab with exactly this name, wherever the pack is (null = not imported).
    static string FindPackPrefab(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return path;
        }
        return null;
    }

    // The first part with a mesh among these renderers (null = none).
    static MeshRenderer FirstMesh(Renderer[] renderers)
    {
        foreach (Renderer look in renderers)
        {
            if (!(look is MeshRenderer part)) continue;
            MeshFilter shape = part.GetComponent<MeshFilter>();
            if (shape != null && shape.sharedMesh != null) return part;
        }
        return null;
    }

    // ----- Reading a look -----

    // One look from the pack: its levels of detail (one mesh each), material and placing, then its Body box and berry
    // spots from its nearest level. Null (and its name in "problem") if it can't be used.
    static Found ReadLook(string name, string prefabName, int index, out string problem)
    {
        problem = prefabName;
        string path = FindPackPrefab(prefabName);
        GameObject pack = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
        if (pack == null) return null;

        List<MeshRenderer> parts = new List<MeshRenderer>();
        List<float> heights = new List<float>();
        LODGroup group = pack.GetComponent<LODGroup>();
        if (group != null)
        {
            foreach (LOD lod in group.GetLODs())
            {
                MeshRenderer part = FirstMesh(lod.renderers);
                if (part == null || parts.Count >= Levels) continue;
                parts.Add(part);
                heights.Add(lod.screenRelativeTransitionHeight);
            }
        }
        if (parts.Count == 0)   // no LOD Group: its first mesh is every level
        {
            MeshRenderer only = FirstMesh(pack.GetComponentsInChildren<MeshRenderer>(true));
            if (only != null) parts.Add(only);
        }
        if (parts.Count == 0)
        {
            problem = prefabName + " (no mesh in it)";
            return null;
        }

        Found look = new Found { name = name + " (" + prefabName + ")", heights = heights.ToArray() };
        look.meshes = new Mesh[parts.Count];
        for (int i = 0; i < parts.Count; i++) look.meshes[i] = parts[i].GetComponent<MeshFilter>().sharedMesh;
        look.material = parts[0].sharedMaterial;
        Matrix4x4 place = pack.transform.worldToLocalMatrix * parts[0].transform.localToWorldMatrix;   // the leaves in the bush's space
        look.position = place.GetColumn(3);
        look.rotation = place.rotation;
        look.scale = place.lossyScale;

        Mesh near = look.meshes[0];
        Vector3[] corners = near.vertices;   // read here in the Editor (the pack's models allow it anyway)
        if (corners.Length == 0)
        {
            problem = prefabName + " (its mesh can't be read)";
            return null;
        }
        for (int i = 0; i < corners.Length; i++) corners[i] = place.MultiplyPoint3x4(corners[i]);
        look.box = BodyBox(corners);
        look.spots = BerrySpots(corners, near.triangles, near.uv, LeafOf(look.material), index);
        problem = null;
        return look;
    }

    // The Body box: across, the middle 90 % of the leaves' corners (so the player bumps into the bush, not into thin
    // air beside a lone leaf); up, from the ground to 85 % of the bush's height. Not "up to most corners": the pack's
    // bushes have most of their leaves low down (half of P_Bush01's corners are under 0.3 m, its top at 1.38 m), and
    // a box ending at 0.8 m let the crosshair (1 m up in first person) pass over the bush.
    static Bounds BodyBox(Vector3[] corners)
    {
        int n = corners.Length;
        float[] xs = new float[n];
        float[] ys = new float[n];
        float[] zs = new float[n];
        for (int i = 0; i < n; i++)
        {
            xs[i] = corners[i].x;
            ys[i] = corners[i].y;
            zs[i] = corners[i].z;
        }
        System.Array.Sort(xs);
        System.Array.Sort(ys);
        System.Array.Sort(zs);
        Bounds box = new Bounds();
        float bottom = Mathf.Max(0f, ys[0]);
        box.SetMinMax(new Vector3(Share(xs, 0.05f), bottom, Share(zs, 0.05f)),
                      new Vector3(Share(xs, 0.95f), bottom + (ys[n - 1] - bottom) * 0.85f, Share(zs, 0.95f)));
        return box;
    }

    // The value this share of the way along a sorted list (0.5 = the middle one).
    static float Share(float[] sorted, float share)
    {
        return sorted[Mathf.Clamp(Mathf.RoundToInt(share * (sorted.Length - 1)), 0, sorted.Length - 1)];
    }

    // The leaf picture's see-through values, read through the graphics card (works whatever the picture's import
    // settings, without changing them), and the material's Alpha Cutoff.
    static Leaf LeafOf(Material material)
    {
        Leaf leaf = new Leaf();
        if (material == null) return leaf;
        if (material.HasProperty("_Cutoff"))
            leaf.cutoff = (byte)Mathf.Clamp(Mathf.RoundToInt(material.GetFloat("_Cutoff") * 255f), 1, 255);
        Texture picture = material.mainTexture;
        if (picture == null) return leaf;
        leaf.tiling = material.mainTextureScale;
        leaf.offset = material.mainTextureOffset;

        RenderTexture target = RenderTexture.GetTemporary(picture.width, picture.height, 0, RenderTextureFormat.ARGB32,
                                                          RenderTextureReadWrite.Linear);
        RenderTexture before = RenderTexture.active;
        Texture2D copy = new Texture2D(picture.width, picture.height, TextureFormat.RGBA32, false, true);
        try
        {
            Graphics.Blit(picture, target);
            RenderTexture.active = target;
            copy.ReadPixels(new Rect(0, 0, picture.width, picture.height), 0, 0);
            copy.Apply(false);
            leaf.pixels = copy.GetPixels32();
            leaf.width = picture.width;
            leaf.height = picture.height;
        }
        finally
        {
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(copy);
        }
        return leaf;
    }

    // ----- Berry spots -----

    // Spots for berry clusters: rays toward the bush from all around (and some from above) stop at the first leaf that
    // is really there; not too low (among the stems) and not deep inside (hidden by other leaves). Then the spots
    // farthest apart are kept, up to Spots Per Look. Each one faces back along its ray, out of the bush.
    static List<BerryBush.Spot> BerrySpots(Vector3[] corners, int[] triangles, Vector2[] uvs, Leaf leaf, int index)
    {
        Bounds all = new Bounds(corners[0], Vector3.zero);
        foreach (Vector3 corner in corners) all.Encapsulate(corner);
        Vector3 middle = all.center;
        float bottom = all.min.y;
        float height = all.size.y;
        float reach = Mathf.Max(all.extents.x, all.extents.z);
        float radius = (all.extents.x + all.extents.z) * 0.5f;
        System.Random rng = new System.Random(1000 + index * 7919);   // the same spots on every run

        List<Vector3> points = new List<Vector3>();
        List<Vector3> outward = new List<Vector3>();
        for (int i = 0; i < SideRays + TopRays; i++)
        {
            bool side = i < SideRays;
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            Vector3 from;
            Vector3 way;
            if (side)
            {
                float y = bottom + height * Mathf.Lerp(0.3f, 0.9f, (float)rng.NextDouble());
                Vector3 across = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                from = new Vector3(middle.x, y, middle.z) + across * (reach * 2f + 0.5f);
                Vector3 to = new Vector3(middle.x, y + height * Mathf.Lerp(-0.12f, 0.12f, (float)rng.NextDouble()), middle.z);
                way = (to - from).normalized;
            }
            else
            {
                float away = radius * 0.8f * Mathf.Sqrt((float)rng.NextDouble());
                from = new Vector3(middle.x + Mathf.Cos(angle) * away, all.max.y + 0.5f, middle.z + Mathf.Sin(angle) * away);
                way = new Vector3(Mathf.Lerp(-0.2f, 0.2f, (float)rng.NextDouble()), -1f,
                                  Mathf.Lerp(-0.2f, 0.2f, (float)rng.NextDouble())).normalized;
            }
            if (!FirstLeaf(from, way, corners, triangles, uvs, leaf, out Vector3 point)) continue;
            if (point.y < bottom + height * 0.28f) continue;                    // down among the stems
            float flat = new Vector2(point.x - middle.x, point.z - middle.z).magnitude;
            if (side && flat < radius * 0.4f) continue;                          // deep inside: other leaves hide it
            if (!side && point.y < bottom + height * 0.55f) continue;            // seen from above through a gap: too deep
            points.Add(point);
            outward.Add(-way);
        }

        // Each next spot is the one farthest from all the spots kept so far (at least Spot Gap away).
        List<BerryBush.Spot> spots = new List<BerryBush.Spot>();
        List<Vector3> kept = new List<Vector3>();
        int pick = points.Count > 0 ? rng.Next(points.Count) : -1;
        while (pick >= 0 && spots.Count < SpotsPerLook)
        {
            kept.Add(points[pick]);
            spots.Add(new BerryBush.Spot
            {
                position = points[pick] + outward[pick] * (BerryRadius * 0.6f),   // its back on the leaf
                turn = Facing(outward[pick], rng),
            });

            pick = -1;
            float best = SpotGap;
            for (int i = 0; i < points.Count; i++)
            {
                float nearest = float.MaxValue;
                foreach (Vector3 spot in kept) nearest = Mathf.Min(nearest, Vector3.Distance(points[i], spot));
                if (nearest <= best) continue;
                best = nearest;
                pick = i;
            }
        }
        return spots;
    }

    // The first solid leaf along a ray: the nearest face it passes through where the leaf picture isn't see-through.
    static bool FirstLeaf(Vector3 from, Vector3 way, Vector3[] corners, int[] triangles, Vector2[] uvs, Leaf leaf, out Vector3 point)
    {
        point = default;
        List<(float distance, int face, float u, float v)> hits = new List<(float, int, float, float)>();
        for (int f = 0; f + 2 < triangles.Length; f += 3)
        {
            if (Crosses(from, way, corners[triangles[f]], corners[triangles[f + 1]], corners[triangles[f + 2]],
                        out float distance, out float u, out float v))
                hits.Add((distance, f, u, v));
        }
        hits.Sort((a, b) => a.distance.CompareTo(b.distance));

        bool hasUV = uvs != null && uvs.Length == corners.Length;
        foreach ((float distance, int face, float u, float v) in hits)
        {
            if (hasUV)
            {
                Vector2 uv = uvs[triangles[face]] * (1f - u - v) + uvs[triangles[face + 1]] * u + uvs[triangles[face + 2]] * v;
                if (!leaf.SolidAt(uv)) continue;   // a see-through part of the leaf picture: the ray goes on
            }
            point = from + way * distance;
            return true;
        }
        return false;
    }

    // Where a ray crosses a triangle, either side (leaves are drawn on both): Möller-Trumbore. Distance = along the ray,
    // u / v = how far toward the second / third corner.
    static bool Crosses(Vector3 from, Vector3 way, Vector3 a, Vector3 b, Vector3 c, out float distance, out float u, out float v)
    {
        distance = u = v = 0f;
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 p = Vector3.Cross(way, ac);
        float det = Vector3.Dot(ab, p);
        if (Mathf.Abs(det) < 1e-10f) return false;   // the ray runs along the face
        float inverse = 1f / det;
        Vector3 s = from - a;
        u = Vector3.Dot(s, p) * inverse;
        if (u < 0f || u > 1f) return false;
        Vector3 q = Vector3.Cross(s, ab);
        v = Vector3.Dot(way, q) * inverse;
        if (v < 0f || u + v > 1f) return false;
        distance = Vector3.Dot(ac, q) * inverse;
        return distance > 1e-5f;
    }

    // A berry cluster's turn: facing out of the bush (its +Z), spun at random around that.
    static Quaternion Facing(Vector3 outward, System.Random rng)
    {
        Vector3 up = Mathf.Abs(outward.y) > 0.95f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(outward, up) * Quaternion.Euler(0f, 0f, (float)(rng.NextDouble() * 360.0));
    }

    // ----- The berries -----

    // Three small red berries in one mesh: a little bunch of balls of slightly different sizes, facing +Z (out of the
    // bush) with their backs on the leaf. Refilled in place, so the bush's links to it stay.
    static Mesh BuildCluster()
    {
        List<Vector3> corners = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> faces = new List<int>();
        float r = BerryRadius;
        AddBall(corners, normals, faces, new Vector3(0f, 1.1f * r, -0.1f * r), r);
        AddBall(corners, normals, faces, new Vector3(-0.95f * r, -0.55f * r, 0f), r * 0.92f);
        AddBall(corners, normals, faces, new Vector3(0.95f * r, -0.5f * r, -0.15f * r), r * 0.85f);

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ClusterPath);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh();
        else mesh.Clear();
        mesh.name = "Berry Cluster";
        mesh.SetVertices(corners);
        mesh.SetNormals(normals);
        mesh.SetTriangles(faces, 0);
        mesh.RecalculateBounds();
        if (isNew) AssetDatabase.CreateAsset(mesh, ClusterPath);
        else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    // A ball of 80 faces (an icosahedron with each face split in four), round enough for a 7 cm berry.
    static void AddBall(List<Vector3> corners, List<Vector3> normals, List<int> faces, Vector3 centre, float radius)
    {
        float g = (1f + Mathf.Sqrt(5f)) * 0.5f;   // the golden ratio: the icosahedron's 12 corners
        List<Vector3> directions = new List<Vector3>
        {
            new Vector3(-1f, g, 0f), new Vector3(1f, g, 0f), new Vector3(-1f, -g, 0f), new Vector3(1f, -g, 0f),
            new Vector3(0f, -1f, g), new Vector3(0f, 1f, g), new Vector3(0f, -1f, -g), new Vector3(0f, 1f, -g),
            new Vector3(g, 0f, -1f), new Vector3(g, 0f, 1f), new Vector3(-g, 0f, -1f), new Vector3(-g, 0f, 1f),
        };
        int[] big =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        Dictionary<long, int> middles = new Dictionary<long, int>();
        int Middle(int a, int b)   // the corner halfway along an edge (made once, shared by both faces)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (middles.TryGetValue(key, out int m)) return m;
            m = directions.Count;
            directions.Add((directions[a] + directions[b]) * 0.5f);
            middles[key] = m;
            return m;
        }
        List<int> small = new List<int>();
        for (int i = 0; i < big.Length; i += 3)
        {
            int a = big[i], b = big[i + 1], c = big[i + 2];
            int ab = Middle(a, b), bc = Middle(b, c), ca = Middle(c, a);
            small.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
        }

        int start = corners.Count;
        foreach (Vector3 direction in directions)
        {
            Vector3 n = direction.normalized;
            corners.Add(centre + n * radius);
            normals.Add(n);
        }
        for (int i = 0; i < small.Count; i += 3)
        {
            int a = start + small[i], b = start + small[i + 1], c = start + small[i + 2];
            // Unity draws a face on the side where Cross(b - a, c - a) points: that must be away from the middle.
            Vector3 middleOfFace = (corners[a] + corners[b] + corners[c]) / 3f - centre;
            if (Vector3.Dot(Vector3.Cross(corners[b] - corners[a], corners[c] - corners[a]), middleOfFace) < 0f) (b, c) = (c, b);
            faces.Add(a);
            faces.Add(b);
            faces.Add(c);
        }
    }

    // Shiny red, drawn many at a time (GPU instancing). Only made if missing, so changes to it stay.
    static Material BerryMaterial()
    {
        Material red = AssetDatabase.LoadAssetAtPath<Material>(BerryMaterialPath);
        if (red != null) return red;
        red = new Material(Shader.Find("Standard")) { name = "Berry", color = BerryRed, enableInstancing = true };
        red.SetFloat("_Glossiness", 0.75f);
        red.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(red, BerryMaterialPath);
        return red;
    }

    // ----- The bush -----

    // The bush prefab rebuilt in place: its file and top object stay, so every link to it does (the scenes' bush, the
    // ObjectScatter rules, the new world). Returns what it did.
    static string Rebuild(string path, List<Found> looks, Mesh cluster, Material red)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ResourceNode node = root.GetComponent<ResourceNode>();
            if (node == null) return path + " has no ResourceNode, so it was left alone";
            BerryBush bush = root.GetComponent<BerryBush>();
            bool firstTime = bush == null;
            int most = firstTime ? new SerializedObject(node).FindProperty("dropMax").intValue      // Turki's Drop Max
                                 : new SerializedObject(bush).FindProperty("mostBerries").intValue; // your number
            most = Mathf.Clamp(most, 1, MostParts);

            // 1. The old look goes: Turki's green ball and its red balls (ShapeColor first: it needs the renderer).
            foreach (ShapeColor paint in root.GetComponentsInChildren<ShapeColor>(true)) Object.DestroyImmediate(paint);
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            MeshRenderer ball = root.GetComponent<MeshRenderer>();
            if (ball != null) Object.DestroyImmediate(ball);
            MeshFilter ballShape = root.GetComponent<MeshFilter>();
            if (ballShape != null) Object.DestroyImmediate(ballShape);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;   // was (1, 1.2, 1): the ball was stretched up

            // 2. The Body: a box, added before the old collider goes (ResourceNode needs one at all times).
            Found first = looks[0];
            BoxCollider body = root.GetComponent<BoxCollider>();
            if (body == null) body = root.AddComponent<BoxCollider>();
            foreach (Collider other in root.GetComponents<Collider>())
            {
                if (other != body) Object.DestroyImmediate(other);
            }
            body.center = first.box.center;
            body.size = first.box.size;

            // 3. The leaves: one part per level of detail, showing the first look (each bush picks its own when it appears).
            Transform model = NewChild("Model", root.transform);
            MeshFilter[] leaves = new MeshFilter[Levels];
            for (int i = 0; i < Levels; i++)
            {
                Transform part = NewChild("LOD" + i, model);
                part.localPosition = first.position;
                part.localRotation = first.rotation;
                part.localScale = first.scale;
                leaves[i] = part.gameObject.AddComponent<MeshFilter>();
                leaves[i].sharedMesh = first.meshes[Mathf.Min(i, first.meshes.Length - 1)];
                part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = first.material;
            }

            // 4. The berries: as many as Most Berries, shown on the first look's first spots.
            Transform bunch = NewChild("Berries", root.transform);
            MeshRenderer[] berries = new MeshRenderer[most];
            for (int i = 0; i < most; i++)
            {
                Transform berry = NewChild("Berry " + (i + 1), bunch);
                if (i < first.spots.Count)
                {
                    berry.localPosition = first.spots[i].position;
                    berry.localRotation = first.spots[i].turn;
                }
                berry.gameObject.AddComponent<MeshFilter>().sharedMesh = cluster;
                berries[i] = berry.gameObject.AddComponent<MeshRenderer>();
                berries[i].sharedMaterial = red;
                berries[i].shadowCastingMode = ShadowCastingMode.Off;   // too small for a shadow worth drawing
            }

            // 5. The LOD Group: near leaves to far leaves; the berries only on the 3 nearest levels.
            LODGroup levels = root.GetComponent<LODGroup>();
            if (levels == null) levels = root.AddComponent<LODGroup>();
            LOD[] lods = new LOD[Levels];
            for (int i = 0; i < Levels; i++)
            {
                List<Renderer> drawn = new List<Renderer> { leaves[i].GetComponent<MeshRenderer>() };
                if (i < BerryLevels) drawn.AddRange(berries);
                float screenShare = i < first.heights.Length ? first.heights[i] : DefaultHeights[i];
                lods[i] = new LOD(screenShare, drawn.ToArray());
            }
            levels.fadeMode = LODFadeMode.None;
            levels.SetLODs(lods);
            levels.RecalculateBounds();

            // 6. Tony's BerryBush, filled in (the first time, its berry numbers are Turki's Drop Min / Max).
            if (firstTime) bush = root.AddComponent<BerryBush>();
            Fill(bush, looks, leaves, berries, body, firstTime ? node : null);

            // 7. StandOnGround: it stands on the ground whatever height the scatter rule spawns it at (0.6 m up, made
            //    for the old ball). Only set the first time, so your changes stay.
            if (root.GetComponent<StandOnGround>() == null)
            {
                SerializedObject feet = new SerializedObject(root.AddComponent<StandOnGround>());   // names must match StandOnGround's fields
                feet.FindProperty("footOffset").floatValue = 0f;   // the bush's pivot is at the foot of its stems
                feet.FindProperty("sink").floatValue = 0.04f;
                feet.FindProperty("maxTilt").floatValue = 10f;
                feet.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return Path.GetFileNameWithoutExtension(path) + " rebuilt (" + most + " berries at most" +
                   (firstTime ? ", BerryBush and StandOnGround added" : ", your BerryBush numbers kept") + ")";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // BerryBush's fields, through SerializedObject (the names in quotes must match BerryBush's fields).
    static void Fill(BerryBush bush, List<Found> looks, MeshFilter[] leaves, MeshRenderer[] berries, BoxCollider body,
                     ResourceNode countsFrom)
    {
        SerializedObject so = new SerializedObject(bush);
        if (countsFrom != null)   // the first time: as many berries as the old bush gave
        {
            SerializedObject node = new SerializedObject(countsFrom);
            so.FindProperty("fewestBerries").intValue = Mathf.Min(node.FindProperty("dropMin").intValue, berries.Length);
            so.FindProperty("mostBerries").intValue = berries.Length;
        }

        SerializedProperty list = so.FindProperty("looks");
        list.arraySize = looks.Count;
        for (int i = 0; i < looks.Count; i++)
        {
            Found found = looks[i];
            SerializedProperty look = list.GetArrayElementAtIndex(i);
            look.FindPropertyRelative("name").stringValue = found.name;
            SerializedProperty meshes = look.FindPropertyRelative("meshes");
            meshes.arraySize = found.meshes.Length;
            for (int m = 0; m < found.meshes.Length; m++) meshes.GetArrayElementAtIndex(m).objectReferenceValue = found.meshes[m];
            look.FindPropertyRelative("material").objectReferenceValue = found.material;
            look.FindPropertyRelative("leavesPosition").vector3Value = found.position;
            look.FindPropertyRelative("leavesRotation").quaternionValue = found.rotation;
            look.FindPropertyRelative("leavesScale").vector3Value = found.scale;
            look.FindPropertyRelative("bodyCenter").vector3Value = found.box.center;
            look.FindPropertyRelative("bodySize").vector3Value = found.box.size;
            SerializedProperty spots = look.FindPropertyRelative("spots");
            spots.arraySize = found.spots.Count;
            for (int s = 0; s < found.spots.Count; s++)
            {
                SerializedProperty spot = spots.GetArrayElementAtIndex(s);
                spot.FindPropertyRelative("position").vector3Value = found.spots[s].position;
                spot.FindPropertyRelative("turn").quaternionValue = found.spots[s].turn;
            }
        }

        SerializedProperty leafParts = so.FindProperty("leafParts");
        leafParts.arraySize = leaves.Length;
        for (int i = 0; i < leaves.Length; i++) leafParts.GetArrayElementAtIndex(i).objectReferenceValue = leaves[i];
        SerializedProperty berryParts = so.FindProperty("berryParts");
        berryParts.arraySize = berries.Length;
        for (int i = 0; i < berries.Length; i++) berryParts.GetArrayElementAtIndex(i).objectReferenceValue = berries[i];
        so.FindProperty("body").objectReferenceValue = body;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A new empty child object.
    static Transform NewChild(string name, Transform parent)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
}
