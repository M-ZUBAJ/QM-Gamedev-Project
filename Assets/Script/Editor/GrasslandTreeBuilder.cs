/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Needs Broken Vector's free "Low Poly Tree Pack" (Assets/Imported3D/BrokenVector/LowPolyTreePack).
              Open GameScene, then menu Tools > World > Use Low Poly Grassland Trees. It asks first, then:
              - Turki's Assets/Prefabs/ResourceNodes/TreeNode.prefab becomes a low-poly Round Oak instead of its
                cylinder and ball, with the same drops, health and gathering. It's changed inside the same file, so
                every TreeNode already placed changes too.
              - 5 more looks, as Prefab Variants of TreeNode, in Assets/Prefabs/ResourceNodes/Trees (TreeNode Oak,
                Broadleaf, Tall Broadleaf, Birch, Birch Cluster). Place any of them in the world.
              - The trees in the open scene are stood on the ground (TreeNode's pivot is now the foot of its trunk),
                and it offers to mix their looks.
              - Sanyam's ObjectScatter (it spawns the trees when the game starts): its tree rules get Y Offset 0 (it
                was 2, for the old pivot, which made the new trees float), and it offers to spawn all 6 looks.
              The trees' heights are in Looks below: change them and run the tool again.
              Tools > World > Mix Trees In Scene: after placing more trees, gives each one a random look, turn and
              size and stands it on the ground. Undo (Ctrl + Z) works for the scene changes, not for the prefabs.
              Hand-made steps: Docs/Tree Setup.md
Purpose: Swaps the placeholder tree (brown cylinder + green ball) for low-poly trees that suit a grassland:
         round oaks, broadleaf trees and birches in the pack's green (Normal) colours. The pack's pines, palms,
         willows, acacias and dead tree are left out (they belong to other biomes).
         Each tree: the ResourceNode and its Capsule Collider on the top object (the collider wraps the trunk), and
         a Model child showing the pack's mesh, with the foot of its trunk on the top object's position.
Requirement: GrasslandTreeBuilder.cs x-> UIBuildHelper.cs, ShapeColor.cs, ResourceNode.cs, ObjectScatter.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;   // Scene
using static UIBuildHelper;          // FindPrefabPath, FolderOf, MakeFolders

public static class GrasslandTreeBuilder
{
    // ===== Fields =====
    const string ToolName = "Grassland Trees";
    // Turki's tree. If it's moved, the tool still finds it by name (FindPrefabPath).
    static string TreeNodePath => FindPrefabPath("Assets/Prefabs/ResourceNodes/TreeNode.prefab");
    const string VariantFolder = "Assets/Prefabs/ResourceNodes/Trees";
    const string ModelName = "Model";
    const float SinkIntoGround = 0.25f;   // meters the trunk goes into the ground, so it doesn't float on a slope

    // The Capsule Collider around the trunk: 1.5 times as thick as the trunk looks, but within these (meters).
    const float MinTrunkCollider = 0.3f;
    const float MaxTrunkCollider = 0.9f;

    // Mix Trees In Scene: each tree gets a random size in this range (1 = as built) and a random turn.
    const float MinSize = 0.9f;
    const float MaxSize = 1.15f;

    // One tree look: our name for it, the pack's prefab it shows, how tall it is (meters, ground to top: the whole
    // tree is scaled to it) and its share of the trees the ObjectScatter spawns (percent).
    struct Look
    {
        public string name;
        public string packTree;
        public float height;
        public int share;

        public Look(string name, string packTree, float height, int share)
        {
            this.name = name;
            this.packTree = packTree;
            this.height = height;
            this.share = share;
        }
    }

    // The grassland trees. The first one becomes TreeNode itself; the others become Prefab Variants of it.
    // The old placeholder tree was 5.65 m tall; the player's character is 1.8 m.
    static readonly Look[] Looks =
    {
        new Look("Round Oak", "Tree Type0 06", 9f, 30),        // a round crown on a thin trunk, like the old ball on a stick
        new Look("Oak", "Tree Type0 05", 9.5f, 20),            // a fuller crown of leaf clumps
        new Look("Broadleaf", "Tree Type4 03", 11f, 15),       // an open crown of leaf clumps on spreading branches
        new Look("Tall Broadleaf", "Tree Type4 04", 13f, 10),  // the tallest
        new Look("Birch", "Tree Type5 03", 11f, 15),           // white trunk, light green leaves
        new Look("Birch Cluster", "Tree Type5 04", 10f, 10),   // three birch trunks growing together
    };

    // The foot of a mesh's trunk (the middle of its lowest part), how thick the trunk is and how tall the tree is,
    // in the mesh's own units.
    struct Trunk
    {
        public Vector3 foot;
        public float radius;
        public float height;
    }

    // ===== Menu (Unity calls these) =====

    [MenuItem("Tools/World/Use Low Poly Grassland Trees", false, MenuOrder.UseGrasslandTrees)]
    static void Build()
    {
        string treeNodePath = TreeNodePath;
        GameObject treeNode = AssetDatabase.LoadAssetAtPath<GameObject>(treeNodePath);
        if (treeNode == null)
        {
            Dialog("Couldn't find TreeNode.prefab (Turki's tree, in Assets/Prefabs/ResourceNodes).");
            return;
        }
        if (PackTree(Looks[0].packTree) == null)
        {
            Dialog("Broken Vector's Low Poly Tree Pack isn't in the project.\n\nImport it (Window > Package Manager > " +
                   "My Assets > Low Poly Tree Pack > Import), then run this again.");
            return;
        }

        bool again = treeNode.transform.Find(ModelName) != null;
        string question = again
            ? "TreeNode is already a low-poly tree. Build TreeNode and its " + (Looks.Length - 1) + " other looks again?\n" +
              "(Changes made to those prefabs are lost.)"
            : "This changes Turki's TreeNode prefab: its cylinder and ball become a low-poly " + Looks[0].name + ", with the " +
              "same drops, health and gathering. Every TreeNode already placed changes with it.\n\nIt also makes " +
              (Looks.Length - 1) + " more tree looks in " + VariantFolder + ".";
        if (!EditorUtility.DisplayDialog(ToolName, question, again ? "Build again" : "Change TreeNode", "Cancel")) return;

        if (!BuildTreeNode(treeNodePath, Looks[0]))
        {
            Dialog("Couldn't change TreeNode. See the Console for why.");
            return;
        }
        treeNode = AssetDatabase.LoadAssetAtPath<GameObject>(treeNodePath);   // the saved one

        int looks = 1;
        for (int i = 1; i < Looks.Length; i++)
        {
            if (BuildVariant(treeNode, Looks[i]) != null) looks++;
        }
        AssetDatabase.SaveAssets();

        List<GameObject> trees = TreesInScene();
        bool mix = trees.Count > 0 && EditorUtility.DisplayDialog(ToolName,
            "The open scene has " + trees.Count + " tree(s). Give each one a random look, turn and size?\n" +
            "(They're stood on the ground either way. Ctrl + Z undoes it.)", "Mix them", "Keep their look");
        int grounded = Place(trees, mix);
        string spawners = FixSpawners();

        Selection.activeObject = treeNode;
        EditorGUIUtility.PingObject(treeNode);   // flashes it in the Project window
        Debug.Log("Grassland trees built: TreeNode is now a " + Looks[0].name + ", plus " + (looks - 1) + " more looks in " +
                  VariantFolder + ". " + Report(trees.Count, grounded, mix) + spawners + " Test: Play and punch a tree: " +
                  "it still gives Wood (and sometimes Apples).");
    }

    [MenuItem("Tools/World/Mix Trees In Scene", false, MenuOrder.MixTreesInScene)]
    static void Mix()
    {
        GameObject treeNode = AssetDatabase.LoadAssetAtPath<GameObject>(TreeNodePath);
        if (treeNode == null || treeNode.transform.Find(ModelName) == null)
        {
            Dialog("Run Tools > World > Use Low Poly Grassland Trees first: it makes the tree looks to mix.");
            return;
        }
        List<GameObject> trees = TreesInScene();
        if (trees.Count == 0)
        {
            Dialog("There are no trees in the open scene. Drag TreeNode (Assets/Prefabs/ResourceNodes) or one of the looks " +
                   "in " + VariantFolder + " into the scene, then run this again.");
            return;
        }
        int grounded = Place(trees, true);
        Debug.Log(ToolName + ": " + Report(trees.Count, grounded, true));
    }

    // ===== My methods =====

    // ----- The prefabs -----

    // Turki's TreeNode: its old look taken off and the first look put on, IN PLACE (same file, same top object), so
    // every placed TreeNode and every look made from it keeps its link.
    static bool BuildTreeNode(string path, Look look)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // The old look: a brown cylinder on the top object (coloured by ShapeColor) and a green ball (Leaves).
            ShapeColor paint = root.GetComponent<ShapeColor>();
            if (paint != null) Object.DestroyImmediate(paint);   // first, because it needs the Mesh Renderer
            MeshRenderer cylinder = root.GetComponent<MeshRenderer>();
            if (cylinder != null) Object.DestroyImmediate(cylinder);
            MeshFilter cylinderShape = root.GetComponent<MeshFilter>();
            if (cylinderShape != null) Object.DestroyImmediate(cylinderShape);
            Transform leaves = root.transform.Find("Leaves");
            if (leaves != null) Object.DestroyImmediate(leaves.gameObject);

            // The top object now stands at the foot of the trunk, unstretched (it was 2 times taller for the cylinder).
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            Transform model = root.transform.Find(ModelName);
            if (model == null)
            {
                model = new GameObject(ModelName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
                model.SetParent(root.transform, false);
            }
            if (!Dress(root, model, look)) return false;
            return PrefabUtility.SaveAsPrefabAsset(root, path) != null;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // One more look: a Prefab Variant of TreeNode (it keeps TreeNode's drops and health, and follows Turki's changes to
    // them) showing another tree. An existing one is refilled IN PLACE, so links to it keep working.
    static GameObject BuildVariant(GameObject treeNode, Look look)
    {
        string path = VariantPath(look);
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;

        Scene hidden = default;
        GameObject root;
        if (existed)
        {
            root = PrefabUtility.LoadPrefabContents(path);
        }
        else
        {
            hidden = EditorSceneManager.NewPreviewScene();   // a hidden scene, so the open scene isn't touched
            root = (GameObject)PrefabUtility.InstantiatePrefab(treeNode, hidden);
        }

        try
        {
            Transform model = root.transform.Find(ModelName);
            if (model == null || !Dress(root, model, look)) return null;
            return PrefabUtility.SaveAsPrefabAsset(root, path);   // a TreeNode copy saved as its own file = a Prefab Variant
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else EditorSceneManager.ClosePreviewScene(hidden);
        }
    }

    // Gives a tree its look: the pack tree's mesh and material on Model, with the foot of its trunk on the top object,
    // and the top object's Capsule Collider around the trunk. False = the pack has no such tree.
    static bool Dress(GameObject root, Transform model, Look look)
    {
        GameObject packTree = PackTree(look.packTree);
        MeshFilter shape = packTree != null ? packTree.GetComponentInChildren<MeshFilter>() : null;
        MeshRenderer paint = packTree != null ? packTree.GetComponentInChildren<MeshRenderer>() : null;
        if (shape == null || shape.sharedMesh == null || paint == null)
        {
            Debug.LogWarning(ToolName + ": the pack has no tree called \"" + look.packTree + "\", so " + look.name + " was skipped.");
            return false;
        }

        MeshFilter filter = model.GetComponent<MeshFilter>();
        if (filter == null) filter = model.gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = model.gameObject.AddComponent<MeshRenderer>();
        filter.sharedMesh = shape.sharedMesh;
        renderer.sharedMaterials = paint.sharedMaterials;   // the pack's "Normal" colours: summer green

        Trunk trunk = Measure(shape.sharedMesh);
        float size = trunk.height > 0f ? look.height / trunk.height : 1f;   // e.g. 9 m wanted / 4.9 m as imported = 1.84
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one * size;
        model.localPosition = -trunk.foot * size + Vector3.down * SinkIntoGround;

        // The collider: the trunk, a bit thicker than it looks (so punches don't slip past), up to the middle of the
        // crown. No collider on the leaves: the player can walk and jump under them.
        CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = root.AddComponent<CapsuleCollider>();
        capsule.direction = 1;   // standing up (Y)
        capsule.radius = Mathf.Clamp(trunk.radius * size * 1.5f, MinTrunkCollider, MaxTrunkCollider);
        capsule.height = Mathf.Max(capsule.radius * 2f, look.height * 0.75f);
        capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
        return true;
    }

    // Where the foot of the trunk is and how thick it is, from the mesh's corners (vertices). If they can't be read,
    // a guess: the trunk stands on the mesh's middle and is 15 cm thick.
    static Trunk Measure(Mesh mesh)
    {
        Bounds box = mesh.bounds;
        Trunk trunk = new Trunk { foot = new Vector3(0f, box.min.y, 0f), radius = 0.15f, height = box.size.y };
        Vector3[] corners = mesh.vertices;
        if (corners.Length == 0 || box.size.y <= 0f) return trunk;

        // The foot: the middle of the lowest 8 % of the tree (only the trunk is that low).
        float low = box.min.y + box.size.y * 0.08f;
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (Vector3 c in corners)
        {
            if (c.y > low) continue;
            sum += c;
            count++;
        }
        if (count > 0)
        {
            trunk.foot.x = sum.x / count;
            trunk.foot.z = sum.z / count;
        }

        // The thickness: the widest the trunk gets between 5 % and 20 % of the height (above the roots, below the leaves).
        float from = box.min.y + box.size.y * 0.05f;
        float to = box.min.y + box.size.y * 0.2f;
        float widest = 0f;
        foreach (Vector3 c in corners)
        {
            if (c.y < from || c.y > to) continue;
            widest = Mathf.Max(widest, new Vector2(c.x - trunk.foot.x, c.z - trunk.foot.z).magnitude);
        }
        if (widest > 0f) trunk.radius = widest;
        return trunk;
    }

    // A tree prefab of Broken Vector's pack by its name (e.g. "Tree Type0 06"); null if it isn't in the project.
    static GameObject PackTree(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    // Where a look's prefab goes, e.g. Assets/Prefabs/ResourceNodes/Trees/TreeNode Birch.prefab (found by name if moved).
    static string VariantPath(Look look)
    {
        return FindPrefabPath(VariantFolder + "/TreeNode " + look.name + ".prefab");
    }

    // ----- The scene -----

    // The top object of every tree in the open scene made from TreeNode or one of its looks.
    static List<GameObject> TreesInScene()
    {
        HashSet<string> treePaths = new HashSet<string> { TreeNodePath };
        for (int i = 1; i < Looks.Length; i++) treePaths.Add(VariantPath(Looks[i]));

        List<GameObject> trees = new List<GameObject>();
        foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(node.gameObject);
            if (root == null || trees.Contains(root)) continue;
            if (treePaths.Contains(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root))) trees.Add(root);
        }
        return trees;
    }

    // Stands each tree on the ground under it. mix = first give it a random look (TreeNode or one of its variants),
    // turn and size. One Ctrl + Z undoes it all. Returns how many found ground.
    static int Place(List<GameObject> trees, bool mix)
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ToolName);

        List<GameObject> looks = new List<GameObject> { AssetDatabase.LoadAssetAtPath<GameObject>(TreeNodePath) };
        for (int i = 1; i < Looks.Length; i++)
        {
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(Looks[i]));
            if (variant != null) looks.Add(variant);
        }

        int grounded = 0;
        foreach (GameObject tree in trees)
        {
            if (mix)
            {
                GameObject look = looks[Random.Range(0, looks.Count)];
                // A tree inside another prefab (e.g. part of a map prefab) can't change prefab here: it only gets turned.
                if (look != PrefabUtility.GetCorrespondingObjectFromSource(tree) && PrefabUtility.IsOutermostPrefabInstanceRoot(tree))
                {
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(tree, look, InteractionMode.UserAction);
                }
                if (tree == null) continue;

                Undo.RecordObject(tree.transform, ToolName);
                tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * Random.Range(MinSize, MaxSize);
            }
            if (Ground(tree)) grounded++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        return grounded;
    }

    // Moves the tree up or down onto the ground: the lowest ground under its trunk, so no side of it floats.
    static bool Ground(GameObject tree)
    {
        Physics.SyncTransforms();   // so the rays see where everything is now
        Transform t = tree.transform;
        CapsuleCollider capsule = tree.GetComponent<CapsuleCollider>();
        float radius = capsule != null ? capsule.radius * t.lossyScale.x : 0.3f;

        float? lowest = null;
        foreach (Vector3 side in new[] { Vector3.zero, Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            float? y = GroundBelow(t.position + side * radius);
            if (y.HasValue && (!lowest.HasValue || y.Value < lowest.Value)) lowest = y;
        }
        if (!lowest.HasValue) return false;

        Undo.RecordObject(t, ToolName);
        t.position = new Vector3(t.position.x, lowest.Value, t.position.z);
        return true;
    }

    // The height of the first ground under this point, looking down from 2.5 m above it (a tree placed with TreeNode's
    // old pivot stands 2 m above its ground). Trees, rocks, triggers and moving things (player, enemies) aren't ground.
    static float? GroundBelow(Vector3 point)
    {
        float? ground = null;
        float nearest = float.MaxValue;
        foreach (RaycastHit hit in Physics.RaycastAll(point + Vector3.up * 2.5f, Vector3.down, 30f,
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

    // ----- The spawner -----

    // Sanyam's ObjectScatter spawns trees when the game starts, at the ground height + each rule's Y Offset. Its tree
    // rule had Y Offset 2, because the old tree's pivot was the middle of its 4 m trunk: the new trees (pivot at the
    // foot of the trunk) floated 2 m up. This sets every tree rule's Y Offset to 0 and offers to share a TreeNode
    // rule's count out over all the looks: one rule per look, same settings, count by each look's share.
    // Returns a sentence for the Console ("" if no ObjectScatter spawns trees).
    static string FixSpawners()
    {
        GameObject[] looks = new GameObject[Looks.Length];
        looks[0] = AssetDatabase.LoadAssetAtPath<GameObject>(TreeNodePath);
        for (int i = 1; i < Looks.Length; i++) looks[i] = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(Looks[i]));

        string report = "";
        foreach (ObjectScatter scatter in Object.FindObjectsByType<ObjectScatter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            List<ObjectScatter.ScatterRule> rules = scatter.rules;
            int treeRules = 0;
            int single = -1;   // the rule spawning plain TreeNodes, if it's the only tree rule
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].prefab == null || System.Array.IndexOf(looks, rules[i].prefab) < 0) continue;
                treeRules++;
                single = rules[i].prefab == looks[0] ? i : -1;
            }
            if (treeRules == 0) continue;

            bool canShare = treeRules == 1 && single >= 0 && System.Array.IndexOf(looks, null) < 0;
            int choice = 2;   // 0 = fix + all looks, 1 = cancel, 2 = only fix the height
            if (canShare)
            {
                choice = EditorUtility.DisplayDialogComplex(ToolName,
                    "Sanyam's ObjectScatter (on \"" + scatter.name + "\") spawns " + rules[single].count + " TreeNodes when the game " +
                    "starts, at the ground + Y Offset " + rules[single].yOffset + " (made for the old tree). Set Y Offset to 0 so " +
                    "they stand on the ground, and spawn all " + Looks.Length + " looks instead of only the " + Looks[0].name +
                    "?\n\n" + ShareList(rules[single].count) + "\n\n(Ctrl + Z undoes it. Save the scene to keep it.)",
                    "Fix + all " + Looks.Length + " looks", "Cancel", "Only fix the height");
            }
            if (choice == 1)
            {
                report += " ObjectScatter on \"" + scatter.name + "\" was left as it was: its trees float until its tree " +
                          "rules' Y Offset is 0.";
                continue;
            }

            Undo.RecordObject(scatter, ToolName);
            foreach (ObjectScatter.ScatterRule rule in rules)
            {
                if (rule.prefab != null && System.Array.IndexOf(looks, rule.prefab) >= 0) rule.yOffset = 0f;
            }
            if (choice == 0)
            {
                ObjectScatter.ScatterRule old = rules[single];
                rules.RemoveAt(single);
                rules.InsertRange(single, ShareOut(old, looks));
            }
            EditorUtility.SetDirty(scatter);
            EditorSceneManager.MarkSceneDirty(scatter.gameObject.scene);
            report += " ObjectScatter on \"" + scatter.name + "\": tree Y Offset set to 0" +
                      (choice == 0 ? ", and it now spawns all " + Looks.Length + " looks." : ".");
        }
        return report;
    }

    // One rule per look, copying the old rule's settings, its count shared out by each look's share (they add up to it).
    static List<ObjectScatter.ScatterRule> ShareOut(ObjectScatter.ScatterRule old, GameObject[] looks)
    {
        List<ObjectScatter.ScatterRule> result = new List<ObjectScatter.ScatterRule>();
        int[] counts = Counts(old.count);
        for (int i = 0; i < Looks.Length; i++)
        {
            result.Add(new ObjectScatter.ScatterRule
            {
                name = "Trees: " + Looks[i].name,
                prefab = looks[i],
                count = counts[i],
                yOffset = 0f,
                terrainLayer = old.terrainLayer,
                minStrength = old.minStrength,
                maxSlope = old.maxSlope,
                minSpacing = old.minSpacing,
                spacingGroup = "Trees",            // every look keeps Min Spacing from the other looks (one forest)
                clearance = old.clearance,
                patchSize = old.patchSize,
                openGround = old.openGround,
                minSize = old.minSize,
                maxSize = old.maxSize,
            });
        }
        return result;
    }

    // A total shared out by the looks' shares (percent), rounded so they still add up to the total.
    static int[] Counts(int total)
    {
        int shares = 0;
        foreach (Look look in Looks) shares += look.share;
        int[] counts = new int[Looks.Length];
        int given = 0;
        for (int i = 0; i < Looks.Length; i++)
        {
            counts[i] = Mathf.RoundToInt(total * (float)Looks[i].share / Mathf.Max(1, shares));
            given += counts[i];
        }
        counts[0] += total - given;   // what rounding left over goes to the first look
        return counts;
    }

    // "60 Round Oak, 40 Oak, 30 Broadleaf, ..."
    static string ShareList(int total)
    {
        int[] counts = Counts(total);
        List<string> parts = new List<string>();
        for (int i = 0; i < Looks.Length; i++) parts.Add(counts[i] + " " + Looks[i].name);
        return string.Join(", ", parts);
    }

    // "2 tree(s) in the scene mixed and stood on the ground. Save the scene to keep it."
    static string Report(int trees, int grounded, bool mixed)
    {
        if (trees == 0) return "No trees in the open scene to stand on the ground.";
        string text = trees + " tree(s) in the scene " + (mixed ? "mixed and " : "") + "stood on the ground. Save the scene to keep it.";
        if (grounded < trees) text += " " + (trees - grounded) + " found no ground under them and stayed where they were.";
        return text;
    }

    static void Dialog(string text)
    {
        EditorUtility.DisplayDialog(ToolName, text, "OK");
    }
}
