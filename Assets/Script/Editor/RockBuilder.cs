/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: 1. Import Broken Vector's free Low Poly Rock Pack (Assets/Imported3D/BrokenVector/LowPolyRockPack).
              2. Open GameScene, then menu Tools > World > Use Low Poly Rocks, and save the scene afterwards.
              Run it again any time: everything is refilled in place (Ctrl + Z undoes the scene part).
              Guide: Docs/Rock Setup.md
Purpose: Turns the placeholder rocks (Turki's StoneNode, CopperNode and IronNode: grey, copper and blue-grey cubes) into
         the pack's low-poly rocks, and makes them break into pieces:
           - Assets/Prefabs/ResourceNodes/Rocks/Shapes/<rock> Shape.asset: the pack's rounder rocks (not the flat slabs
             or the tiniest ones), all about 1.3 m big, with the pivot in the middle of the bottom, raised 15 % of the
             rock's height so the rock sits a little into the ground;
           - each rock prefab rebuilt in place (so the scene's and ObjectScatter's links stay): its Mesh Filter shows a
             shape (RockLook picks a random one when the game runs), a Mesh Collider in that shape instead of the box,
             StandOnGround (on the floor, tilted on slopes) and its stone: StoneNode the pack's grey, CopperNode
             Copper Ore.mat (the pack's orange, warmer), IronNode Iron Ore.mat (the pack's grey, dark blue-grey); the
             two ore materials are made in Assets/Materials/Rocks only if missing. Turki's ResourceNode is untouched;
           - Assets/Prefabs/ResourceNodes/Rocks/StoneNode Desert.prefab: a StoneNode in the pack's sand color;
           - Tools > Effects > Build Rock Break (the debris, and RockBreak on every rock);
           - in the open scene (asks first): Sanyam's ObjectScatter rule with "Desert" in its name that spawns
             StoneNodes spawns StoneNode Desert instead, and the rocks placed by hand are put on the ground (their
             pivot used to be the middle of the cube).
Requirement: RockBuilder.cs x-> RockLook.cs, StandOnGround.cs, RockBreakBuilder.cs, MeshAssetHelper.cs, ObjectScatter.cs, ResourceNode.cs, ShapeColor.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIBuildHelper;    // FindPrefabPath, MakeFolders

public static class RockBuilder
{
    // ===== Fields =====
    const string ToolName = "Use Low Poly Rocks";
    const string NodeFolder = "Assets/Prefabs/ResourceNodes";
    const string RocksFolder = NodeFolder + "/Rocks";
    const string ShapeFolder = RocksFolder + "/Shapes";
    const string DesertPath = RocksFolder + "/StoneNode Desert.prefab";
    const string MaterialFolder = "Assets/Materials/Rocks";

    const float TargetSize = 1.3f;       // meters: the cube root of width x height x depth (the old cube: 1.5 x 1 x 1.5 = 1.31)
    const float SinkShare = 0.15f;       // share of a rock's height that goes into the ground
    const float MinHeightShare = 0.35f;  // height / width: lower = a flat slab (the pack's Type4), not a rock to mine
    const float MaxHeightShare = 1.3f;   // higher = a thin spire
    const int MinTriangles = 80;         // fewer = one of the tiniest rocks: too plain when made 1.3 m big
    static readonly Color CopperTint = new Color(1f, 0.86f, 0.72f);   // the pack's orange, a bit warmer
    static readonly Color IronTint = new Color(0.55f, 0.58f, 0.72f);  // the pack's grey, darker and blue

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Use Low Poly Rocks", false, MenuOrder.UseLowPolyRocks)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }

        List<(string name, Mesh mesh, string path)> packRocks = PackRocks();
        Dictionary<string, Material> stones = packRocks.Count > 0 ? PackMaterials(packRocks[0].path) : new Dictionary<string, Material>();
        if (packRocks.Count == 0 || !stones.ContainsKey("Grey"))
        {
            EditorUtility.DisplayDialog(ToolName, "Broken Vector's Low Poly Rock Pack wasn't found (models called \"Rock Type1 01\" " +
                "and so on, with a Materials folder next to them). Import it from the Asset Store first: Docs/Rock Setup.md.", "OK");
            return;
        }

        List<Mesh> shapes = MakeShapes(packRocks);
        if (shapes.Count == 0)
        {
            EditorUtility.DisplayDialog(ToolName, "None of the pack's rocks could be read (the Console says why).", "OK");
            return;
        }

        Material grey = stones["Grey"];
        Material copper = OreMaterial("Copper Ore", stones.TryGetValue("Orange", out Material orange) ? orange : grey, CopperTint);
        Material iron = OreMaterial("Iron Ore", grey, IronTint);
        List<string> done = new List<string>();
        Dress("StoneNode", grey, shapes, done);
        Dress("CopperNode", copper, shapes, done);
        Dress("IronNode", iron, shapes, done);
        if (stones.TryGetValue("Sand", out Material sand)) MakeDesertLook(sand, done);
        AssetDatabase.SaveAssets();

        string breaks = RockBreakBuilder.Build();
        string scene = FixScene();
        Debug.Log(ToolName + ": " + shapes.Count + " rock shapes in " + ShapeFolder + "; rebuilt " +
                  (done.Count > 0 ? string.Join(", ", done) : "no rock prefab") + ". Rock break: " + breaks + scene +
                  " Test: Play, look at a few rocks and break one.");
    }

    // ===== My methods =====

    // The pack's rock models: (file name, its mesh, its path), sorted by name.
    static List<(string name, Mesh mesh, string path)> PackRocks()
    {
        List<(string, Mesh, string)> rocks = new List<(string, Mesh, string)>();
        foreach (string guid in AssetDatabase.FindAssets("Rock Type t:Model", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith("Rock Type")) continue;
            Mesh mesh = MeshAssetHelper.MeshIn(path);
            if (mesh != null) rocks.Add((fileName, mesh, path));
        }
        rocks.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return rocks;
    }

    // The pack's materials by name (Grey, Sand, Orange, Green), from the Materials folder next to its Models folder.
    static Dictionary<string, Material> PackMaterials(string modelPath)
    {
        Dictionary<string, Material> stones = new Dictionary<string, Material>();
        string models = Path.GetDirectoryName(modelPath).Replace('\\', '/');
        string folder = Path.GetDirectoryName(models).Replace('\\', '/') + "/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) return stones;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) stones[Path.GetFileNameWithoutExtension(path)] = material;
        }
        return stones;
    }

    // The rocks to mine: the pack's rounder rocks, each copied all about the same size, with its pivot where it meets
    // the ground (the middle of its bottom, raised SinkShare of its height).
    static List<Mesh> MakeShapes(List<(string name, Mesh mesh, string path)> packRocks)
    {
        MakeFolders(ShapeFolder);
        List<Mesh> shapes = new List<Mesh>();
        foreach ((string name, Mesh mesh, string _) in packRocks)
        {
            Bounds box = mesh.bounds;
            float width = Mathf.Max(box.size.x, box.size.z);
            if (width <= 0f || box.size.y <= 0f) continue;
            float heightShare = box.size.y / width;
            int triangles = 0;
            for (int s = 0; s < mesh.subMeshCount; s++) triangles += (int)mesh.GetIndexCount(s) / 3;
            if (heightShare < MinHeightShare || heightShare > MaxHeightShare || triangles < MinTriangles) continue;

            float size = Mathf.Pow(box.size.x * box.size.y * box.size.z, 1f / 3f);
            Vector3 pivot = new Vector3(box.center.x, box.min.y + box.size.y * SinkShare, box.center.z);
            Mesh shape = MeshAssetHelper.SaveCopy(mesh, pivot, TargetSize / size, ShapeFolder + "/" + name + " Shape.asset");
            if (shape != null) shapes.Add(shape);
        }
        return shapes;
    }

    // A copy of a pack material for an ore, tinted. Made once: an existing one is kept, so your changes stay.
    static Material OreMaterial(string fileName, Material from, Color tint)
    {
        MakeFolders(MaterialFolder);
        string path = MaterialFolder + "/" + fileName + ".mat";
        Material ore = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ore != null) return ore;

        ore = new Material(from) { name = fileName };
        if (ore.HasProperty("_Color")) ore.color = from.color * tint;
        AssetDatabase.CreateAsset(ore, path);
        return ore;
    }

    // One rock prefab rebuilt in place: the cube's look and box go, the rock's shape, stone and ground-standing come.
    static void Dress(string prefabName, Material stone, List<Mesh> shapes, List<string> done)
    {
        string path = FindPrefabPath(NodeFolder + "/" + prefabName + ".prefab");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning(ToolName + ": there's no " + prefabName + " prefab, so it was skipped.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ShapeColor tint = root.GetComponent<ShapeColor>();
            if (tint != null) Object.DestroyImmediate(tint);   // the cube's color: the rock's material has its own colors
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;           // the size is in the shape now (ObjectScatter still sizes each rock)

            Need<MeshFilter>(root).sharedMesh = shapes[0];
            Need<MeshRenderer>(root).sharedMaterials = new[] { stone };
            MeshCollider body = Need<MeshCollider>(root);
            body.convex = false;
            body.sharedMesh = shapes[0];
            // Only now the box goes: Turki's ResourceNode needs a collider, and the Mesh Collider is it from now on.
            foreach (BoxCollider box in root.GetComponents<BoxCollider>()) Object.DestroyImmediate(box);

            SerializedObject look = new SerializedObject(Need<RockLook>(root));   // the names in quotes must match the scripts' fields
            SerializedProperty list = look.FindProperty("shapes");
            list.ClearArray();
            foreach (Mesh shape in shapes)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shape;
            }
            look.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject stand = new SerializedObject(Need<StandOnGround>(root));
            stand.FindProperty("footOffset").floatValue = 0f;   // the pivot is where the rock meets the ground
            stand.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            done.Add(prefabName);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // StoneNode in the pack's sand color, for the desert: a Prefab Variant, so everything else comes from StoneNode.
    static void MakeDesertLook(Material sand, List<string> done)
    {
        GameObject stoneNode = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(NodeFolder + "/StoneNode.prefab"));
        if (stoneNode == null) return;
        MakeFolders(RocksFolder);

        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(DesertPath) != null;
        Scene preview = existed ? default : EditorSceneManager.NewPreviewScene();
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(DesertPath)
                                  : (GameObject)PrefabUtility.InstantiatePrefab(stoneNode, preview);
        try
        {
            root.GetComponent<MeshRenderer>().sharedMaterials = new[] { sand };
            PrefabUtility.SaveAsPrefabAsset(root, DesertPath);   // a StoneNode copy saved as a new prefab = a Prefab Variant
            done.Add("StoneNode Desert");
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    // ----- The open scene -----

    // Asks, then: Sanyam's desert rock rule spawns the sand rocks, and the rocks placed by hand stand on the ground.
    // One Ctrl + Z undoes it. Returns a sentence for the Console ("" if there's nothing to do).
    static string FixScene()
    {
        GameObject stoneNode = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(NodeFolder + "/StoneNode.prefab"));
        GameObject desert = AssetDatabase.LoadAssetAtPath<GameObject>(DesertPath);

        List<(ObjectScatter scatter, ObjectScatter.ScatterRule rule)> desertRules = new List<(ObjectScatter, ObjectScatter.ScatterRule)>();
        if (stoneNode != null && desert != null)
        {
            foreach (ObjectScatter scatter in Object.FindObjectsByType<ObjectScatter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (ObjectScatter.ScatterRule rule in scatter.rules)
                {
                    if (rule.prefab == stoneNode && rule.name != null && rule.name.ToLowerInvariant().Contains("desert"))
                        desertRules.Add((scatter, rule));
                }
            }
        }
        List<GameObject> placed = PlacedRocks();
        if (desertRules.Count == 0 && placed.Count == 0) return "";

        string question = "In the open scene:\n";
        if (desertRules.Count > 0)
            question += "\n- Sanyam's ObjectScatter rule \"" + desertRules[0].rule.name + "\" spawns StoneNode Desert (sand-colored) " +
                        "instead of the grey StoneNode.";
        if (placed.Count > 0)
            question += "\n- The " + placed.Count + " rocks placed by hand are put on the ground (their pivot used to be the middle " +
                        "of the cube, so they would float).";
        question += "\n\n(Ctrl + Z undoes it. Save the scene to keep it.)";
        if (!EditorUtility.DisplayDialog(ToolName, question, "Do it", "Leave the scene"))
            return " The open scene was left as it was.";

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ToolName);
        foreach ((ObjectScatter scatter, ObjectScatter.ScatterRule rule) in desertRules)
        {
            Undo.RecordObject(scatter, ToolName);
            rule.prefab = desert;
            EditorUtility.SetDirty(scatter);
            EditorSceneManager.MarkSceneDirty(scatter.gameObject.scene);
        }
        int grounded = 0;
        foreach (GameObject rock in placed)
        {
            if (!Ground(rock)) continue;
            grounded++;
            EditorSceneManager.MarkSceneDirty(rock.scene);
        }
        Undo.CollapseUndoOperations(undoGroup);

        return (desertRules.Count > 0 ? " Desert rule \"" + desertRules[0].rule.name + "\" now spawns StoneNode Desert." : "") +
               (placed.Count > 0 ? " " + grounded + " of " + placed.Count + " placed rocks put on the ground." : "") +
               " Save the scene.";
    }

    // The rocks placed in the open scenes (instances of the rock prefabs, not ones inside other prefabs).
    static List<GameObject> PlacedRocks()
    {
        HashSet<string> rockPaths = new HashSet<string> { DesertPath };
        foreach (string rock in new[] { "StoneNode", "CopperNode", "IronNode" })
            rockPaths.Add(FindPrefabPath(NodeFolder + "/" + rock + ".prefab"));

        List<GameObject> rocks = new List<GameObject>();
        foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(node.gameObject);
            if (root == null || rocks.Contains(root) || !PrefabUtility.IsOutermostPrefabInstanceRoot(root)) continue;
            if (rockPaths.Contains(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root))) rocks.Add(root);
        }
        return rocks;
    }

    // Moves a rock up or down onto the ground right under it, then lets its StandOnGround tilt it to the slope.
    static bool Ground(GameObject rock)
    {
        Physics.SyncTransforms();   // so the rays see where everything is now
        Transform t = rock.transform;
        float? ground = GroundBelow(t.position);
        if (!ground.HasValue) return false;

        Undo.RecordObject(t, ToolName);
        t.position = new Vector3(t.position.x, ground.Value, t.position.z);
        StandOnGround stand = rock.GetComponent<StandOnGround>();
        if (stand != null) stand.Settle();
        return true;
    }

    // The height of the first ground under this point, looking down from 3 m above it. Rocks, trees, triggers and
    // moving things (player, enemies) aren't ground.
    static float? GroundBelow(Vector3 point)
    {
        float? ground = null;
        float nearest = float.MaxValue;
        foreach (RaycastHit hit in Physics.RaycastAll(point + Vector3.up * 3f, Vector3.down, 30f,
                                                      Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<ResourceNode>() != null) continue;
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) continue;
            if (hit.distance < nearest)
            {
                nearest = hit.distance;
                ground = hit.point.y;
            }
        }
        return ground;
    }

    // The component, added if it isn't there.
    static T Need<T>(GameObject target) where T : Component
    {
        T found = target.GetComponent<T>();
        return found != null ? found : target.AddComponent<T>();
    }
}
