/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Needs ithappy's free "Animals FREE" pack (Assets/Imported3D/ithappy/Animals_FREE).
              Open GameScene, then menu Tools > World > Build Wild Animal.
              Animal: the one selected in the Project window (an ithappy animal prefab or model, e.g. Horse_001), or
              the Deer when nothing is selected. It makes (and asks before replacing them):
              - Assets/Prefabs/ResourceNodes/<Name>Node.prefab (e.g. DeerNode): a huntable animal with WildAnimal,
                the meat drop and health copied from Turki's AnimalNode (the sheep);
              - Assets/Animation/Animals/<Name>Node.controller: idle / walk / run, picked by WildAnimal's Speed;
              - Assets/Materials/Animals/<material> (Built-in).mat: the pack's materials are made for URP, which our
                Built-in project can't draw (pink), so a Standard copy with the same picture (made only if missing).
              Then it offers to replace the sheep in the open scene: the ObjectScatter's AnimalNode rule (the 40 sheep
              spawned when the game starts) and the AnimalNodes placed by hand. Undo (Ctrl + Z) works for the scene
              changes, not for the files. Hand-made steps: Docs/Animal Setup.md
Purpose: Turns an animal model into a wild animal the player can hunt, in one click:
         <Name>Node   kinematic Rigidbody (it moves itself) + Box Collider (what punches hit) + Turki's ResourceNode
                      (drops meat when killed) + Tony's WildAnimal (grazes, wanders, runs away from the player)
           > Model    the pack's model at its own size, feet on the ground, with the new controller and material
Requirement: WildAnimalBuilder.cs x-> UIBuildHelper.cs, AnimatorBuildHelper.cs, MaterialBuildHelper.cs, MeshBounds.cs, WildAnimal.cs, ResourceNode.cs, ObjectScatter.cs, DeathEffectBuilder.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AnimatorBuildHelper;   // EmptyController
using static UIBuildHelper;         // FindPrefabPath, FolderOf, MakeFolders

public static class WildAnimalBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Wild Animal";
    const string DefaultAnimal = "Deer_001";   // ithappy's deer, used when nothing is selected
    const string PrefabFolder = "Assets/Prefabs/ResourceNodes";
    const string ControllerFolder = "Assets/Animation/Animals";
    const string MaterialFolder = "Assets/Materials/Animals";
    // Turki's sheep. If it's moved, the tool still finds it by name (FindPrefabPath).
    static string SheepPath => FindPrefabPath("Assets/Prefabs/ResourceNodes/AnimalNode.prefab");
    const float Size = 1f;   // 1 = the pack's own size (its animals are sized to each other: the deer is about 2 m tall)

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build Wild Animal", false, MenuOrder.BuildWildAnimal)]
    static void Build()
    {
        GameObject animal = ChosenAnimal();
        if (animal == null)
        {
            Dialog("No animal found.\n\nImport ithappy's \"Animals FREE\" (Window > Package Manager > My Assets), or select an " +
                   "animal prefab or model (with a Skinned Mesh Renderer) in the Project window, then run this again.");
            return;
        }

        SkinnedMeshRenderer skin = animal.GetComponentInChildren<SkinnedMeshRenderer>(true);
        string modelPath = AssetDatabase.GetAssetPath(skin.sharedMesh);   // the model file (.fbx) the mesh comes from
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            Dialog(animal.name + "'s mesh doesn't come from a model file, so its animations can't be found.");
            return;
        }

        string name = AnimalName(animal) + "Node";   // "Deer_001" -> "DeerNode"
        string prefabPath = FindPrefabPath(PrefabFolder + "/" + name + ".prefab");
        string controllerPath = ControllerFolder + "/" + name + ".controller";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null ||
                      AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null;
        if (exists && !EditorUtility.DisplayDialog(ToolName,
                name + " already exists. Build it again and replace it?\n(Changes made to its prefab or controller are lost.)",
                "Replace", "Cancel"))
        {
            return;
        }

        Dictionary<string, AnimationClip> clips = ClipsIn(modelPath);
        AnimationClip idle = Pick(clips, "idle");
        AnimationClip walk = Pick(clips, "walk");
        AnimationClip run = Pick(clips, "run");
        AnimatorController controller = BuildController(controllerPath, idle, walk, run);
        Material[] materials = MaterialBuildHelper.DrawableCopies(skin.sharedMaterials, MaterialFolder, 0.1f);   // matte, like the low-poly world

        GameObject prefab = BuildPrefab(name, prefabPath, model, materials, controller, out float height);
        if (prefab == null)
        {
            Dialog("Couldn't save " + prefabPath + ". See the Console for why.");
            return;
        }
        if (idle == null || walk == null || run == null)
        {
            Debug.LogWarning(ToolName + ": " + model.name + " has no " + (idle == null ? "idle " : "") + (walk == null ? "walk " : "") +
                             (run == null ? "run " : "") + "animation, so that part plays nothing. Drag a clip into the Move " +
                             "blend tree in the Animator window.");
        }

        string scene = ReplaceSheep(prefab);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);   // flashes it in the Project window
        Debug.Log(name + " built: " + prefabPath + " (" + height.ToString("0.0") + " m tall)." + scene + " Test: Play and walk up " +
                  "to a " + AnimalName(animal).ToLower() + ": it runs away. Hit it until it falls: \"+N Meat\".");
    }

    // ===== My methods =====

    // ----- Choosing the animal -----

    // The animal to use: a prefab or model with a Skinned Mesh Renderer selected in the Project window, else the Deer.
    static GameObject ChosenAnimal()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected != null && EditorUtility.IsPersistent(selected)                    // a file, not a scene object
            && selected.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
            && selected.GetComponent<ResourceNode>() == null)                           // not one of our animals already
        {
            return selected;
        }

        foreach (string guid in AssetDatabase.FindAssets(DefaultAnimal + " t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != DefaultAnimal) continue;
            GameObject deer = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (deer != null && deer.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) return deer;
        }
        return null;
    }

    // "Deer_001" -> "Deer", "Horse_001" -> "Horse".
    static string AnimalName(GameObject animal)
    {
        string name = Regex.Replace(animal.name, @"[_\s]*\d+$", "").Replace(" ", "");
        return name.Length > 0 ? name : "Animal";
    }

    // ----- Animations -----

    // Every animation inside the model file, by name (e.g. "Deer_001_walk").
    static Dictionary<string, AnimationClip> ClipsIn(string modelPath)
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (part is AnimationClip clip && !clip.name.StartsWith("__preview__")) clips[clip.name] = clip;
        }
        return clips;
    }

    // The clip for a word: a name ending in "_word" first (Tiger_001_idle, not Tiger_001_idle_rare), else any name with it.
    static AnimationClip Pick(Dictionary<string, AnimationClip> clips, string word)
    {
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            if (pair.Key.ToLower().EndsWith("_" + word)) return pair.Value;
        }
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            if (pair.Key.ToLower().Contains(word)) return pair.Value;
        }
        return null;
    }

    // One state, Move: a blend of idle (Speed 0), walk (1) and run (2), played at AnimSpeed. Made inside the same file
    // if it exists (so the prefab stays linked to it).
    static AnimatorController BuildController(string path, AnimationClip idle, AnimationClip walk, AnimationClip run)
    {
        MakeFolders(FolderOf(path));
        AnimatorController c = EmptyController(path);
        c.AddParameter(WildAnimal.SpeedName, AnimatorControllerParameterType.Float);
        c.AddParameter(new AnimatorControllerParameter
        {
            name = WildAnimal.AnimSpeedName,
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f,
        });

        AnimatorState move = c.CreateBlendTreeInController("Move", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = WildAnimal.SpeedName;
        tree.useAutomaticThresholds = false;
        if (idle != null) tree.AddChild(idle, 0f);
        if (walk != null) tree.AddChild(walk, 1f);
        if (run != null) tree.AddChild(run, 2f);
        move.speedParameter = WildAnimal.AnimSpeedName;
        move.speedParameterActive = true;
        c.layers[0].stateMachine.defaultState = move;

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return c;
    }

    // ----- The prefab -----

    // An existing prefab is emptied and refilled IN PLACE: its top object stays the same, so links to it (the
    // ObjectScatter's rule, placed animals) keep working. A brand-new object saved over it could break those links.
    static GameObject BuildPrefab(string name, string path, GameObject model, Material[] materials,
                                  AnimatorController controller, out float height)
    {
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        try
        {
            if (existed) Empty(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the world's middle, so measuring is simple
            root.transform.localScale = Vector3.one;

            GameObject look = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
            look.name = "Model";
            look.transform.SetParent(root.transform, false);
            look.transform.localPosition = Vector3.zero;
            look.transform.localRotation = Quaternion.identity;
            look.transform.localScale = Vector3.one * Size;

            Animator animator = look.GetComponentInChildren<Animator>(true);
            if (animator == null) animator = look.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;                                   // WildAnimal moves it
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;    // animals out of sight don't move their bones
            foreach (SkinnedMeshRenderer r in look.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.sharedMaterials.Length == materials.Length) r.sharedMaterials = materials;
            }

            // Feet on the ground (the top object), centered over it, measured from the real mesh.
            Bounds seen = MeshBounds.Of(look);
            Vector3 shift = new Vector3(-seen.center.x, -seen.min.y, -seen.center.z);
            look.transform.localPosition = shift;
            seen.center += shift;
            height = seen.size.y;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;   // WildAnimal moves it; physics never pushes it or tips it over
            body.useGravity = false;

            BoxCollider box = root.AddComponent<BoxCollider>();   // what the player's punches and the animal's own looking-ahead use
            box.center = new Vector3(0f, seen.size.y * 0.5f, 0f);
            box.size = new Vector3(seen.size.x * 0.8f, seen.size.y, seen.size.z * 0.85f);

            ResourceNode node = root.AddComponent<ResourceNode>();
            CopySheepDrops(node);
            root.AddComponent<WildAnimal>();
            DeathEffectBuilder.AddTo(root);   // bursts into green particles when it's killed
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);   // only the saved prefab is kept
            else Object.DestroyImmediate(root);
        }
    }

    // The sheep's ResourceNode settings (meat drop, health, Animal type), so hunting gives the same as before.
    // No sheep: Animal type and 100 health, and Meat if there's an Item Data called Meat.
    static void CopySheepDrops(ResourceNode node)
    {
        GameObject sheep = AssetDatabase.LoadAssetAtPath<GameObject>(SheepPath);
        ResourceNode sheepNode = sheep != null ? sheep.GetComponent<ResourceNode>() : null;
        if (sheepNode != null)
        {
            EditorUtility.CopySerialized(sheepNode, node);
            return;
        }

        node.resourceType = ResourceNode.ResourceType.Animal;
        node.maxHealth = 100f;
        foreach (string guid in AssetDatabase.FindAssets("Meat t:ItemData"))
        {
            Object meat = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            if (meat == null || meat.name != "Meat") continue;
            SerializedObject so = new SerializedObject(node);
            so.FindProperty("dropItem").objectReferenceValue = meat;
            so.ApplyModifiedPropertiesWithoutUndo();
            break;
        }
    }

    // Removes everything from an opened prefab except its top object's Transform: children first, then scripts, then
    // the rest (a script may need the collider, so it goes first).
    static void Empty(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in root.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(script);
        foreach (Component part in root.GetComponents<Component>())
        {
            if (part != null && !(part is Transform)) Object.DestroyImmediate(part);
        }
    }

    // ----- The scene -----

    // Offers to swap the sheep in the open scene for the new animal: the ObjectScatter rules spawning AnimalNode (their
    // Y Offset becomes 0: the sheep's pivot was the middle of its body, the new animal's is its feet) and the AnimalNodes
    // placed by hand (put on the ground under them). One Ctrl + Z undoes it all. Returns a sentence for the Console.
    static string ReplaceSheep(GameObject animal)
    {
        GameObject sheep = AssetDatabase.LoadAssetAtPath<GameObject>(SheepPath);
        if (sheep == null) return "";

        List<ObjectScatter.ScatterRule> rules = new List<ObjectScatter.ScatterRule>();
        List<ObjectScatter> scatters = new List<ObjectScatter>();
        int spawned = 0;
        foreach (ObjectScatter scatter in Object.FindObjectsByType<ObjectScatter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (ObjectScatter.ScatterRule rule in scatter.rules)
            {
                if (rule.prefab != sheep) continue;
                rules.Add(rule);
                spawned += rule.count;
                if (!scatters.Contains(scatter)) scatters.Add(scatter);
            }
        }
        List<GameObject> placed = new List<GameObject>();
        foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(node.gameObject);
            if (root != null && !placed.Contains(root) && PrefabUtility.IsOutermostPrefabInstanceRoot(root) &&
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == SheepPath)
            {
                placed.Add(root);
            }
        }
        if (rules.Count == 0 && placed.Count == 0) return " No sheep (AnimalNode) in the open scene to replace.";

        string list = (spawned > 0 ? "\n- " + spawned + " spawned by the ObjectScatter when the game starts" : "") +
                      (placed.Count > 0 ? "\n- " + placed.Count + " placed by hand" : "");
        if (!EditorUtility.DisplayDialog(ToolName, "Replace the sheep (AnimalNode) in the open scene with " + animal.name + "?" +
                list + "\n\n(Ctrl + Z undoes it. Save the scene to keep it.)", "Replace them", "Keep the sheep"))
        {
            return " The sheep in the scene were kept.";
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ToolName);
        foreach (ObjectScatter scatter in scatters) Undo.RecordObject(scatter, ToolName);
        foreach (ObjectScatter.ScatterRule rule in rules)
        {
            rule.prefab = animal;
            rule.yOffset = 0f;
        }
        foreach (ObjectScatter scatter in scatters)
        {
            EditorUtility.SetDirty(scatter);
            EditorSceneManager.MarkSceneDirty(scatter.gameObject.scene);
        }

        Physics.SyncTransforms();   // so the ground rays see where everything is now
        foreach (GameObject old in placed)
        {
            Transform from = old.transform;
            GameObject fresh = (GameObject)PrefabUtility.InstantiatePrefab(animal, old.scene);
            Undo.RegisterCreatedObjectUndo(fresh, ToolName);
            fresh.transform.SetParent(from.parent, false);
            fresh.transform.SetPositionAndRotation(OnGround(from.position), Quaternion.Euler(0f, from.eulerAngles.y, 0f));
            fresh.transform.SetSiblingIndex(from.GetSiblingIndex());
            Undo.DestroyObjectImmediate(old);
        }
        Undo.CollapseUndoOperations(undoGroup);

        return (spawned > 0 ? " The ObjectScatter now spawns " + spawned + " " + animal.name + " (Y Offset 0)." : "") +
               (placed.Count > 0 ? " " + placed.Count + " placed sheep replaced." : "") + " Save the scene to keep it.";
    }

    // The ground under a point (looking down from 3 m above it: a sheep's pivot was 0.5 m up). Nothing found: the point.
    static Vector3 OnGround(Vector3 point)
    {
        float nearest = float.MaxValue;
        Vector3 ground = point;
        foreach (RaycastHit hit in Physics.RaycastAll(point + Vector3.up * 3f, Vector3.down, 30f,
                                                      Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<ResourceNode>() != null) continue;   // the sheep itself, trees, rocks
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) continue;         // the player, enemies
            if (hit.distance >= nearest) continue;
            nearest = hit.distance;
            ground = hit.point;
        }
        return ground;
    }

    static void Dialog(string text)
    {
        EditorUtility.DisplayDialog(ToolName, text, "OK");
    }
}
