/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Rock Caves (Tools > World > Build New World Scene runs it too). Needs the rock
              shapes from Tools > World > Use Low Poly Rocks (Assets/Prefabs/ResourceNodes/Rocks/Shapes).
              The caves are refilled in place every time, the same each time (each design has its own seed), so the
              new world's links to them stay. To change one: open its prefab and move its rocks by hand (then don't run
              the tool again), or change the Designs table below.
              Guide: Docs/New World Setup.md
Purpose: Three walk-in rock caves for the new world (Assets/Prefabs/World/Caves: Rock Cave Small, Rock Cave, Rock Cave
         Big), made from the low-poly rock pack so they match the world:
           - Walls: a ring of big rocks around a round floor, overlapping so there are no holes, with a gap at the
             front (+Z, about 5 m wide);
           - the entrance: a tall rock either side and one across the top (about 3.6 m high inside);
           - Roof: flat rocks over the whole ring, a little below the tops of the walls (dim inside: it blocks the sun);
           - a few smaller rocks around the outside, so it looks like a rocky mound;
           - Ore Spots: empty markers (OreSpot) on the floor, where WorldGenerator puts Stone, Copper and Iron rocks.
         Every rock has the rock material and a Mesh Collider of its own shape, so the walls and roof are solid.
         The floor is the terrain: WorldGenerator flattens the ground under each cave.
Requirement: RockCaveBuilder.cs x-> UIBuildHelper.cs, MenuOrder.cs
             RockCaveBuilder.cs <-x NewWorldBuilder.cs
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UIBuildHelper;    // MakeFolders

public static class RockCaveBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Rock Caves";
    const string CaveFolder = "Assets/Prefabs/World/Caves";
    const string ShapeFolder = "Assets/Prefabs/ResourceNodes/Rocks/Shapes";   // made by Use Low Poly Rocks
    const string StonePath = "Assets/Prefabs/ResourceNodes/StoneNode.prefab";  // its material is the rocks' color sheet
    public const string OreSpotName = "OreSpot";   // WorldGenerator looks for children named this

    // The shapes' pivot is SinkShare of their height above their bottom (Use Low Poly Rocks), so standing on the floor
    // they're sunk that much: a rock "height" tall shows only 85 % of it.
    const float SinkShare = 0.15f;
    const float EntranceHalfWidth = 2.6f;   // meters: the gap in the walls is about 5 m wide
    const float EntranceHeight = 3.6f;      // meters from the floor to the bottom of the rock across the top

    // One cave: its name, the floor's radius and the walls' height (meters), and its own seed (the same cave every run).
    struct Design
    {
        public string name;
        public float radius, wallHeight;
        public int seed;
    }

    static readonly Design[] Designs =
    {
        new Design { name = "Rock Cave Small", radius = 6.5f, wallHeight = 5f,   seed = 11 },
        new Design { name = "Rock Cave",       radius = 7.5f, wallHeight = 5.5f, seed = 23 },
        new Design { name = "Rock Cave Big",   radius = 8.5f, wallHeight = 6f,   seed = 37 },
    };

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build Rock Caves", false, MenuOrder.BuildRockCaves)]
    static void FromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        List<GameObject> caves = BuildAll(out string report);
        if (caves.Count == 0) EditorUtility.DisplayDialog(ToolName, report, "OK");
        Debug.Log(ToolName + ": " + report);
    }

    // ===== My methods =====

    // Builds (refills) the three caves. Returns them; "report" says what happened, for the Console.
    public static List<GameObject> BuildAll(out string report)
    {
        List<GameObject> built = new List<GameObject>();
        List<Mesh> shapes = Shapes();
        if (shapes.Count == 0)
        {
            report = "no rock shapes in " + ShapeFolder + ". Run Tools > World > Use Low Poly Rocks first.";
            return built;
        }
        Material rock = RockMaterial();

        MakeFolders(CaveFolder);
        foreach (Design design in Designs)
        {
            GameObject cave = BuildCave(design, shapes, rock);
            if (cave != null) built.Add(cave);
        }
        AssetDatabase.SaveAssets();
        report = built.Count + " caves in " + CaveFolder + " (" + string.Join(", ", built.Select(c => c.name)) + ")";
        return built;
    }

    // The rock shapes made by Use Low Poly Rocks, in name order (so a design always picks the same ones).
    static List<Mesh> Shapes()
    {
        List<Mesh> shapes = new List<Mesh>();
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { ShapeFolder }))
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
            if (mesh != null) shapes.Add(mesh);
        }
        return shapes.OrderBy(m => m.name).ToList();
    }

    // The stone rocks' material (the pack's Grey color sheet), or Unity's default if there's no StoneNode.
    static Material RockMaterial()
    {
        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(StonePath));
        MeshRenderer renderer = stone != null ? stone.GetComponentInChildren<MeshRenderer>(true) : null;
        if (renderer != null && renderer.sharedMaterial != null) return renderer.sharedMaterial;
        return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
    }

    // One cave, refilled IN PLACE if its prefab exists (so the new world's link to it stays).
    static GameObject BuildCave(Design d, List<Mesh> shapes, Material material)
    {
        string path = CaveFolder + "/" + d.name + ".prefab";
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(d.name);
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            System.Random random = new System.Random(d.seed);
            Transform walls = Group(root, "Walls");
            Transform roof = Group(root, "Roof");
            Transform outside = Group(root, "Outside");
            Transform spots = Group(root, "Ore Spots");

            MakeWalls(walls, d, shapes, material, random);
            MakeEntrance(walls, d, shapes, material, random);
            MakeRoof(roof, d, shapes, material, random);
            MakeOutside(outside, d, shapes, material, random);
            MakeOreSpots(spots, d);

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // The ring of wall rocks, from just right of the entrance all the way round to just left of it. Each rock is wider
    // than the step between them, so they overlap (no gaps to see through or squeeze through).
    static void MakeWalls(Transform parent, Design d, List<Mesh> shapes, Material material, System.Random random)
    {
        float gap = Mathf.Asin(Mathf.Clamp01(EntranceHalfWidth / d.radius)) * Mathf.Rad2Deg + 4f;   // half the gap, degrees
        float arc = (360f - gap * 2f) * Mathf.Deg2Rad * d.radius;
        int count = Mathf.Max(6, Mathf.CeilToInt(arc / 3.2f));
        float step = arc / count;

        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Lerp(gap, 360f - gap, (i + 0.5f) / count);
            float depth = 3.2f + Next(random) * 0.8f;
            float height = d.wallHeight * (1f + Next(random) * 0.25f) / (1f - SinkShare);
            Vector3 size = new Vector3(step * 1.7f, height, depth);
            Rock(parent, "Wall Rock " + (i + 1), Pick(shapes, random), material, Around(angle, d.radius + depth * 0.4f),
                 Facing(angle) * Quaternion.Euler(0f, (Next(random) - 0.5f) * 20f, 0f), size);
        }
    }

    // A tall rock either side of the gap, and one lying across the top of it.
    static void MakeEntrance(Transform parent, Design d, List<Mesh> shapes, Material material, System.Random random)
    {
        float side = Mathf.Asin(Mathf.Clamp01((EntranceHalfWidth + 1.4f) / d.radius)) * Mathf.Rad2Deg;
        float pillarHeight = d.wallHeight * 1.15f / (1f - SinkShare);
        foreach (float angle in new[] { side, -side })
        {
            Rock(parent, angle > 0f ? "Entrance Right" : "Entrance Left", Pick(shapes, random), material,
                 Around(angle, d.radius + 1.2f), Facing(angle), new Vector3(3.4f, pillarHeight, 4f));
        }

        float lintelHeight = 2.4f;
        Vector3 at = new Vector3(0f, EntranceHeight + lintelHeight * SinkShare, d.radius + 1f);
        Rock(parent, "Entrance Top", Pick(shapes, random), material, at, Quaternion.identity,
             new Vector3(EntranceHalfWidth * 2f + 4f, lintelHeight, 3.6f));
    }

    // Flat rocks over the whole ring, resting a little below the tops of the walls: one in the middle and four around.
    static void MakeRoof(Transform parent, Design d, List<Mesh> shapes, Material material, System.Random random)
    {
        float r = d.radius;
        float thick = 2.4f;
        float y = d.wallHeight - 0.6f;   // the slabs' pivot: overlapping the walls' tops
        Vector2[] places = { Vector2.zero, new Vector2(0.45f, 0.45f), new Vector2(-0.45f, 0.45f), new Vector2(0.45f, -0.45f), new Vector2(-0.45f, -0.45f) };
        for (int i = 0; i < places.Length; i++)
        {
            float width = r * (i == 0 ? 1.5f : 1.35f);
            Vector3 at = new Vector3(places[i].x * r, y + (i == 0 ? 0.4f : 0f), places[i].y * r);
            Rock(parent, "Roof Rock " + (i + 1), Pick(shapes, random), material, at,
                 Quaternion.Euler(0f, Next(random) * 360f, 0f), new Vector3(width, thick, width));
        }
    }

    // Smaller rocks around the outside (not in front of the entrance), so the cave looks like a rocky mound.
    static void MakeOutside(Transform parent, Design d, List<Mesh> shapes, Material material, System.Random random)
    {
        int count = 7;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Lerp(50f, 310f, (i + 0.5f) / count) + (Next(random) - 0.5f) * 15f;
            float size = 2f + Next(random) * 1.5f;
            Rock(parent, "Outside Rock " + (i + 1), Pick(shapes, random), material, Around(angle, d.radius + 4.2f),
                 Quaternion.Euler(0f, Next(random) * 360f, 0f), new Vector3(size * 1.3f, size, size));
        }
    }

    // Where the ores go: six around the inside (not near the entrance) and one at the back.
    static void MakeOreSpots(Transform parent, Design d)
    {
        float[] angles = { 70f, 115f, 160f, 200f, 245f, 290f };
        for (int i = 0; i < angles.Length; i++)
        {
            Transform spot = new GameObject(OreSpotName + " " + (i + 1)).transform;
            spot.SetParent(parent, false);
            spot.localPosition = Around(angles[i], d.radius * 0.62f);
        }
        Transform back = new GameObject(OreSpotName + " " + (angles.Length + 1)).transform;
        back.SetParent(parent, false);
        back.localPosition = new Vector3(0f, 0f, -d.radius * 0.25f);
    }

    // A rock: the shape stretched to "size" (meters: width, height, depth), with the rock material and a solid
    // collider of its own shape.
    static void Rock(Transform parent, string name, Mesh shape, Material material, Vector3 at, Quaternion turn, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = turn;
        Vector3 own = shape.bounds.size;
        go.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, own.x), size.y / Mathf.Max(0.01f, own.y),
                                              size.z / Mathf.Max(0.01f, own.z));
        go.AddComponent<MeshFilter>().sharedMesh = shape;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        go.AddComponent<MeshCollider>().sharedMesh = shape;
    }

    static Transform Group(GameObject root, string name)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(root.transform, false);
        return group;
    }

    // A point on the floor at this angle (degrees from the front, +Z, turning right) and distance from the middle.
    static Vector3 Around(float angle, float distance)
    {
        float a = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * distance;
    }

    // Turned to face the middle from that angle (the rock's width runs along the wall).
    static Quaternion Facing(float angle)
    {
        return Quaternion.LookRotation(-Around(angle, 1f));
    }

    static Mesh Pick(List<Mesh> shapes, System.Random random) => shapes[random.Next(shapes.Count)];

    static float Next(System.Random random) => (float)random.NextDouble();
}
