/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Needs Abduo Bricks' free "Low Poly Tool Pack" (Assets/Imported3D/Abduo Bricks/Low Poly Tool Pack).
              Open GameScene, then menu Tools > Items > Build Tier Tools. It asks first, then:
              - makes 15 tools: a pickaxe, axe and sword in Wood, Stone, Iron, Gold and Diamond
                (Assets/Config/Items/Tools/<Id>.asset + Assets/Prefabs/Item/Tools/<Id>.prefab, e.g. StonePickaxe), with
                Standard copies of the pack's URP materials in Assets/Materials/Tools. Wood, Stone and Iron tools get
                recipes (Gold and Diamond wait for their ores). Running it again rebuilds them in the same files.
              - ores need pickaxes: Copper's and Iron's Mined With = Stone (and Gold = Iron, Diamond = Gold if they exist);
              - PlayerCharacter.prefab gets ToolInHand, so the body and the first-person hands hold the selected tool;
                the Player gets ToolTestKit (the game rule Tool Test), and its PunchEffect's swing timing is measured;
              - "Build + replace Marko's tools" also: swaps Sword / Axe / PickAxe in Turki's crafting list for Iron Sword /
                Stone Axe / Stone Pickaxe (same recipes), takes Marko's GiveSwordForTesting, EquippedWeaponDisplay and his
                three weapon models out of the scene, and moves the old Sword / Axe / PickAxe items and the
                Assets/Prefabs/Weapons prefabs to the trash (your computer's bin; git has them too).
              Undo (Ctrl + Z) works for the scene changes, not for files. Save the scene afterwards.
              Tools > Items > Rebuild Tool Models rebuilds only the 15 tool prefabs (e.g. after changing HandleShare
              below): names, recipes, ores and the scene stay as they are.
              Hand-made steps: Docs/Tool Setup.md
Purpose: Replaces Marko's three placeholder tools with 15 tiered tools from the Low Poly Tool Pack, in one click.
         Each tool:
         <Id>       Box Collider (aim at it and press F to pick it up) + ItemPickup (this tool, 1)
           > Model  the pack's tool (handle + head, pivot at the bottom of the grip), leaning like an icon. ToolInHand
                    copies this child into the hand. Axe and pickaxe handles are shortened (HandleShare 0.7): the
                    handle sits in a squeezed "Handle Length" object, the head beside it keeps its shape.
         Its Item Data: Item Type Tool, Kind + Tier, icon = a photo of the prefab, and the recipe.
Requirement: TierToolBuilder.cs x-> UIBuildHelper.cs, MaterialBuildHelper.cs, MeshBounds.cs, PunchEffectBuilder.cs, ItemData.cs, ItemPickup.cs, ToolStats.cs, ToolInHand.cs, ToolTestKit.cs, PunchEffect.cs, FirstPersonHands.cs, PlayerAnimator.cs, CraftingMenu.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UIBuildHelper;   // AddToPlayer, FindPrefabPath, FolderOf, MakeFolders

public static class TierToolBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Tier Tools";
    const string ItemFolder = "Assets/Config/Items/Tools";
    const string PrefabFolder = "Assets/Prefabs/Item/Tools";
    const string MaterialFolder = "Assets/Materials/Tools";
    const string ModelName = "Model";   // ToolInHand copies the child with this name into the hand

    // Axe and pickaxe handles are this share of the pack's length: 0.7 = 30 % shorter (the head moves down with the
    // handle's top; the fist still holds near the bottom). 1 = the pack's length. Swords keep their size.
    // Change it, then run Tools > Items > Rebuild Tool Models.
    const float HandleShare = 0.7f;

    static readonly ToolTier[] Tiers = { ToolTier.Wood, ToolTier.Stone, ToolTier.Iron, ToolTier.Gold, ToolTier.Diamond };
    static readonly ToolKind[] Kinds = { ToolKind.Pickaxe, ToolKind.Axe, ToolKind.Sword };

    // Ores and the weakest pickaxe that mines them (set only while an ore's Mined With is still None).
    static readonly (string ore, ToolTier tier)[] Ores =
    {
        ("Copper", ToolTier.Stone), ("Iron", ToolTier.Stone), ("Gold", ToolTier.Iron), ("Diamond", ToolTier.Gold),
    };

    // Marko's old tools and what replaces each in Turki's crafting list (the new one has the same recipe).
    static readonly (string old, ToolKind kind, ToolTier tier)[] OldTools =
    {
        ("Sword", ToolKind.Sword, ToolTier.Iron),        // was 2 Plank + 1 Iron
        ("Axe", ToolKind.Axe, ToolTier.Stone),           // was 2 Plank + 1 Stone
        ("PickAxe", ToolKind.Pickaxe, ToolTier.Stone),   // was 2 Plank + 2 Stone
    };
    static readonly string[] OldScripts = { "GiveSwordForTesting", "EquippedWeaponDisplay" };   // by name: they may be deleted later
    const string OldPrefabFolder = "Assets/Prefabs/Weapons";

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Items/Build Tier Tools", false, MenuOrder.BuildTierTools)]
    static void Build()
    {
        if (PackPrefab(ToolKind.Pickaxe, ToolTier.Wood) == null)
        {
            Dialog("Abduo Bricks' Low Poly Tool Pack isn't in the project.\n\nImport it (Window > Package Manager > My Assets > " +
                   "Low Poly Tool Pack > Import), then run this again.");
            return;
        }

        bool sceneReady = Object.FindFirstObjectByType<PlayerInventory>() != null;
        int choice = EditorUtility.DisplayDialogComplex(ToolName,
            "Make 15 tools (pickaxe, axe and sword in Wood, Stone, Iron, Gold and Diamond) from the Low Poly Tool Pack, " +
            "make copper and iron need a Stone pickaxe, and let the player hold the selected tool?\n\n" +
            "\"Build + replace Marko's tools\" also:\n" +
            "- swaps Sword / Axe / PickAxe in Turki's crafting list for Iron Sword / Stone Axe / Stone Pickaxe (same recipes);\n" +
            "- takes GiveSwordForTesting, EquippedWeaponDisplay and the three weapon models out of the open scene;\n" +
            "- moves Assets/Config/Items/Sword, Axe, PickAxe and Assets/Prefabs/Weapons/Sword, Axe, PickAxe to the trash." +
            (sceneReady ? "" : "\n\n(Open GameScene first to replace Marko's tools: the scene parts need it.)"),
            "Build + replace Marko's tools", "Cancel", "Only build the tools");
        if (choice == 1) return;
        bool replace = choice == 0 && sceneReady;

        List<ItemData> tools = new List<ItemData>();
        foreach (ToolTier tier in Tiers)
        {
            foreach (ToolKind kind in Kinds)
            {
                ItemData tool = BuildTool(kind, tier);
                if (tool != null) tools.Add(tool);
            }
        }
        AssetDatabase.SaveAssets();

        List<string> report = new List<string> { tools.Count + " tools built in " + ItemFolder };
        report.Add(SetOres());
        report.Add(SetUpCharacter());
        report.Add(SetUpPlayer(tools));
        if (replace)
        {
            report.Add(SwapCraftingList());
            report.Add(ClearOldToolsFromScene());
            report.Add(TrashOldFiles());
        }
        AssetDatabase.SaveAssets();

        Debug.Log(ToolName + ": " + string.Join(". ", report.FindAll(line => !string.IsNullOrEmpty(line))) + ". Save the scene to keep " +
                  "it. Test: tick GameRules > Tool Test, Play, and select a tool in the hotbar.");
    }

    // Only the 15 tool prefabs again (their look: handle length, materials, box), in the same files. The Item Data
    // (names, recipes), ores, scene and Player are left alone, unlike Build Tier Tools.
    [MenuItem("Tools/Items/Rebuild Tool Models", false, MenuOrder.RebuildToolModels)]
    static void RebuildModels()
    {
        if (PackPrefab(ToolKind.Pickaxe, ToolTier.Wood) == null)
        {
            Dialog("Abduo Bricks' Low Poly Tool Pack isn't in the project, so the tools can't be rebuilt.");
            return;
        }

        List<string> done = new List<string>();
        foreach (ToolTier tier in Tiers)
        {
            foreach (ToolKind kind in Kinds)
            {
                string id = Id(kind, tier);
                GameObject pack = PackPrefab(kind, tier);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemFolder + "/" + id + ".asset");
                if (item == null) item = FindByName<ItemData>(id, "ItemData");
                if (pack == null || item == null || item.Prefab == null) continue;   // never built: Build Tier Tools makes it
                if (BuildPrefab(id, pack, item, kind) != null) done.Add(id);
            }
        }
        AssetDatabase.SaveAssets();

        if (done.Count == 0) Dialog("No tools to rebuild yet. Run Tools > Items > Build Tier Tools first.");
        else Debug.Log("Rebuild Tool Models: " + done.Count + " tools rebuilt (axe and pickaxe handles at " +
                       Mathf.RoundToInt(HandleShare * 100f) + " % of the pack's length). Play and select a tool to see it.");
    }

    // ===== My methods =====

    // ----- The tools -----

    // One tool: its Item Data (made or reused), its prefab (built in place) and the links between them.
    static ItemData BuildTool(ToolKind kind, ToolTier tier)
    {
        GameObject pack = PackPrefab(kind, tier);
        string id = Id(kind, tier);
        if (pack == null)
        {
            Debug.LogWarning(ToolName + ": the pack has no " + kind + "_" + tier + ", so " + id + " was skipped.");
            return null;
        }

        ItemData item = FindOrMakeItem(id);
        GameObject prefab = BuildPrefab(id, pack, item, kind);
        if (prefab == null)
        {
            Debug.LogWarning(ToolName + ": couldn't save " + id + "'s prefab. See the Console for why.");
            return item;
        }
        FillItem(item, kind, tier, prefab);
        return item;
    }

    // "StonePickaxe", "WoodenSword"... (the Item Id = the Item Data's file name).
    static string Id(ToolKind kind, ToolTier tier)
    {
        return ToolRules.TierName(tier) + kind;
    }

    // The pack's prefab for a tool, e.g. "Pickaxe_Wood" (found by name, wherever the pack is).
    static GameObject PackPrefab(ToolKind kind, ToolTier tier)
    {
        return FindByName<GameObject>(kind + "_" + tier, "Prefab");
    }

    static ItemData FindOrMakeItem(string id)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemFolder + "/" + id + ".asset");
        if (item == null) item = FindByName<ItemData>(id, "ItemData");   // moved somewhere else
        if (item != null) return item;

        MakeFolders(ItemFolder);
        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, ItemFolder + "/" + id + ".asset");
        return item;
    }

    // The tool's prefab, emptied and refilled IN PLACE if it exists (so links to it keep working).
    static GameObject BuildPrefab(string id, GameObject pack, ItemData item, ToolKind kind)
    {
        string path = FindPrefabPath(PrefabFolder + "/" + id + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(id);
        try
        {
            if (existed) Empty(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // the world's middle, so measuring is simple
            root.transform.localScale = Vector3.one;

            GameObject model = MakeModel(pack, kind, root.transform);   // the pack's tool, axe and pickaxe handles shortened
            model.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);   // leaning, like a tool icon (ToolInHand turns its own copy)
            foreach (Renderer part in model.GetComponentsInChildren<Renderer>(true))
            {
                part.sharedMaterials = MaterialBuildHelper.DrawableCopies(part.sharedMaterials, MaterialFolder);   // URP -> Standard
            }

            // Centered on the top object, with a box around it to aim at (F picks it up).
            Bounds seen = MeshBounds.Of(model);
            model.transform.localPosition = -seen.center;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = seen.size;

            ItemPickup pickup = root.AddComponent<ItemPickup>();
            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("data").objectReferenceValue = item;
            so.FindProperty("amount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);   // only the saved prefab is kept
            else Object.DestroyImmediate(root);
        }
    }

    // The pack's tool under a "Model" object, its handle shortened to HandleShareOf(kind):
    //   Model
    //     > Handle Length    squeezed along the handle (scale y = the share)
    //         > the pack's tool, unpacked (its handle)
    //     > ToolHead         the head, taken out of the squeeze so it keeps its shape, moved down with the handle's top
    // It's measured upright, before Model leans: the handle stands along +y with its bottom at y 0 (the pack's pivot).
    static GameObject MakeModel(GameObject pack, ToolKind kind, Transform parent)
    {
        GameObject model = new GameObject(ModelName);
        model.transform.SetParent(parent, false);
        GameObject length = new GameObject("Handle Length");
        length.transform.SetParent(model.transform, false);

        GameObject tool = (GameObject)PrefabUtility.InstantiatePrefab(pack, parent.gameObject.scene);
        PrefabUtility.UnpackPrefabInstance(tool, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);   // so its head can move out
        tool.transform.SetParent(length.transform, false);

        float share = HandleShareOf(kind);
        if (Mathf.Approximately(share, 1f)) return model;   // a sword keeps the pack's size

        List<Transform> heads = new List<Transform>();
        foreach (Transform part in tool.GetComponentsInChildren<Transform>(true))
        {
            bool head = part != tool.transform && part.name.ToLower().Contains("head");
            if (head && !heads.Exists(found => part.IsChildOf(found))) heads.Add(part);
        }
        if (heads.Count == 0)
        {
            Debug.LogWarning(ToolName + ": " + pack.name + " has no part named ...Head, so its handle keeps its full length.");
            return model;
        }

        foreach (Transform head in heads)
        {
            float middle = MeshBounds.Of(head.gameObject).center.y;   // the head's middle, in meters up from the handle's bottom
            head.SetParent(model.transform, true);                    // out of the squeeze, staying where it is
            head.position += Vector3.up * middle * (share - 1f);      // down as far as that part of the handle shrinks
        }
        length.transform.localScale = new Vector3(1f, share, 1f);
        return model;
    }

    static float HandleShareOf(ToolKind kind)
    {
        return kind == ToolKind.Sword ? 1f : HandleShare;
    }

    // Removes everything from an opened prefab except its top object's Transform: children, scripts, then the rest.
    static void Empty(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in root.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(script);
        foreach (Component part in root.GetComponents<Component>())
        {
            if (part != null && !(part is Transform)) Object.DestroyImmediate(part);
        }
    }

    // The Item Data's settings: name, prefab, Pickup, a photo icon, Item Type Tool with Kind + Tier, and the recipe.
    static void FillItem(ItemData item, ToolKind kind, ToolTier tier, GameObject prefab)
    {
        SerializedObject so = new SerializedObject(item);
        so.FindProperty("displayName").stringValue = ToolRules.TierName(tier) + " " + kind;   // "Stone Pickaxe"
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("pickup").enumValueIndex = (int)PickupType.Pickup;
        so.FindProperty("icon.mode").enumValueIndex = (int)IconMode.Capture;
        so.FindProperty("itemType").enumValueIndex = (int)ItemType.Tool;
        so.FindProperty("tool.kind").enumValueIndex = (int)kind;
        so.FindProperty("tool.tier").enumValueIndex = (int)tier;
        so.FindProperty("minedWith").enumValueIndex = (int)ToolTier.None;
        so.FindProperty("craftAmount").intValue = 1;
        so.FindProperty("craftedAt").enumValueIndex = (int)CraftStation.Crafting;

        SerializedProperty recipe = so.FindProperty("recipe");
        recipe.ClearArray();
        foreach ((ItemData ingredient, int amount) in RecipeFor(kind, tier))
        {
            recipe.InsertArrayElementAtIndex(recipe.arraySize);
            SerializedProperty entry = recipe.GetArrayElementAtIndex(recipe.arraySize - 1);
            entry.FindPropertyRelative("item").objectReferenceValue = ingredient;
            entry.FindPropertyRelative("amount").intValue = amount;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    // What a tool costs: Planks for the handle and the tier's material. Stone and Iron are Marko's old prices (Axe =
    // 2 Plank + 1 Stone, PickAxe = 2 Plank + 2 Stone, Sword = 2 Plank + 1 Iron). Gold and Diamond: no recipe yet.
    static List<(ItemData, int)> RecipeFor(ToolKind kind, ToolTier tier)
    {
        List<(ItemData, int)> recipe = new List<(ItemData, int)>();
        ItemData plank = FindByName<ItemData>("Plank", "ItemData");
        if (plank == null) return recipe;

        if (tier == ToolTier.Wood)
        {
            recipe.Add((plank, kind == ToolKind.Pickaxe ? 4 : kind == ToolKind.Axe ? 3 : 2));
            return recipe;
        }
        string materialName = tier == ToolTier.Stone ? "Stone" : tier == ToolTier.Iron ? "Iron" : null;
        ItemData material = materialName != null ? FindByName<ItemData>(materialName, "ItemData") : null;
        if (material == null) return recipe;   // Gold, Diamond (or a missing material): can't be crafted yet

        recipe.Add((plank, 2));
        recipe.Add((material, kind == ToolKind.Pickaxe ? 2 : 1));
        return recipe;
    }

    // ----- Ores, the character and the Player -----

    // Copper and Iron need a Stone pickaxe (Gold: Iron, Diamond: Gold), only where Mined With is still None.
    static string SetOres()
    {
        List<string> done = new List<string>();
        foreach ((string oreName, ToolTier tier) in Ores)
        {
            ItemData ore = FindByName<ItemData>(oreName, "ItemData");
            if (ore == null) continue;
            SerializedObject so = new SerializedObject(ore);
            SerializedProperty minedWith = so.FindProperty("minedWith");
            if (minedWith.enumValueIndex != (int)ToolTier.None) continue;   // already chosen: kept
            minedWith.enumValueIndex = (int)tier;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ore);
            done.Add(oreName + " needs a " + ToolRules.TierName(tier) + " Pickaxe");
        }
        return done.Count > 0 ? string.Join(", ", done) : "";
    }

    // ToolInHand on the player's character prefab: the Character in the scene and the first-person hands (a Prefab
    // Variant of it) both get it. The hands also stop hiding for Marko's old tool names.
    static string SetUpCharacter()
    {
        string path = FindPrefabPath("Assets/Prefabs/Player/PlayerCharacter.prefab");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            return "no PlayerCharacter.prefab yet: run Tools > Player > Add Character Model, then this again, to see tools in the hands";
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            GameObject holder = animator != null ? animator.gameObject : root;
            if (holder.GetComponent<ToolInHand>() == null)
            {
                holder.AddComponent<ToolInHand>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        foreach (FirstPersonHands hands in Object.FindObjectsByType<FirstPersonHands>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject so = new SerializedObject(hands);
            SerializedProperty hide = so.FindProperty("hideWhileHolding");
            if (hide == null || hide.arraySize == 0) continue;
            hide.ClearArray();   // the hands always show now, holding the tool
            so.ApplyModifiedProperties();
        }
        return "the player's character holds the selected tool (ToolInHand)";
    }

    // ToolTestKit with every tool on the Player, and PunchEffect's sword / axe and pickaxe timing from the animations.
    static string SetUpPlayer(List<ItemData> tools)
    {
        PlayerInventory player = Object.FindFirstObjectByType<PlayerInventory>();
        if (player == null) return "no Player in the open scene: open GameScene and run this again for the Tool Test rule";

        AddToPlayer<ToolTestKit>();
        SerializedObject kit = new SerializedObject(player.GetComponent<ToolTestKit>());
        SerializedProperty list = kit.FindProperty("tools");
        list.ClearArray();
        foreach (ItemData tool in tools)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = tool;
        }
        kit.ApplyModifiedProperties();   // recorded for Undo

        string timing = "";
        PunchEffect effect = player.GetComponent<PunchEffect>();
        if (effect != null)
        {
            float? swing = PunchEffectBuilder.MeasureAction(PlayerAction.Attack, out string swingHow);
            float? mine = PunchEffectBuilder.MeasureAction(PlayerAction.Mine, out string mineHow);
            SerializedObject so = new SerializedObject(effect);
            if (swing.HasValue) so.FindProperty("swingDelay").floatValue = swing.Value;
            if (mine.HasValue) so.FindProperty("mineDelay").floatValue = mine.Value;
            so.ApplyModifiedProperties();
            timing = ", Swing Delay " + (swing.HasValue ? swing.Value.ToString("0.00") + " s (" + swingHow + ")" : "kept (" + swingHow + ")") +
                     ", Mine Delay " + (mine.HasValue ? mine.Value.ToString("0.00") + " s (" + mineHow + ")" : "kept (" + mineHow + ")");
        }
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        return "the Player has ToolTestKit (game rule Tool Test)" + timing;
    }

    // ----- Replacing Marko's tools -----

    // Turki's crafting list (CraftingMenu's Craftables): each old tool becomes its new one, in the same place.
    static string SwapCraftingList()
    {
        int swapped = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<CraftingMenu>(true) == null) continue;
            if (PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Model) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (CraftingMenu menu in root.GetComponentsInChildren<CraftingMenu>(true))
                {
                    SerializedObject so = new SerializedObject(menu);
                    SerializedProperty list = so.FindProperty("craftables");
                    for (int i = 0; list != null && i < list.arraySize; i++)
                    {
                        Object entry = list.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (entry == null) continue;
                        foreach ((string old, ToolKind kind, ToolTier tier) in OldTools)
                        {
                            if (entry.name != old || !(entry is ItemData)) continue;
                            ItemData replacement = AssetDatabase.LoadAssetAtPath<ItemData>(ItemFolder + "/" + Id(kind, tier) + ".asset");
                            if (replacement == null) break;
                            list.GetArrayElementAtIndex(i).objectReferenceValue = replacement;
                            changed = true;
                            swapped++;
                            break;
                        }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        return swapped > 0 ? "Turki's crafting list: " + swapped + " old tools swapped for the new ones" : "";
    }

    // Marko's test giver and weapon display (found by name, so this tool still compiles once they're deleted), and his
    // three weapon models (instances of the prefabs in Assets/Prefabs/Weapons, e.g. under the Main Camera).
    static string ClearOldToolsFromScene()
    {
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ToolName);
        int scripts = 0;
        int models = 0;

        List<MonoBehaviour> oldScripts = new List<MonoBehaviour>();
        foreach (MonoBehaviour script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (script != null && System.Array.IndexOf(OldScripts, script.GetType().Name) >= 0) oldScripts.Add(script);
        }
        foreach (MonoBehaviour script in oldScripts)
        {
            Undo.DestroyObjectImmediate(script);
            scripts++;
        }

        List<GameObject> oldModels = new List<GameObject>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject go = t.gameObject;
            if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (IsOldPrefab(path)) oldModels.Add(go);
        }
        foreach (GameObject go in oldModels)
        {
            Undo.DestroyObjectImmediate(go);
            models++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (scripts + models > 0) EditorSceneManager.MarkAllScenesDirty();
        return scripts + models > 0 ? "scene: " + scripts + " of Marko's tool scripts and " + models + " weapon models removed" : "";
    }

    // The old Sword / Axe / PickAxe Item Data and Marko's weapon prefabs, to the trash (recoverable from the bin).
    static string TrashOldFiles()
    {
        List<string> paths = new List<string>();
        foreach (var (old, _, _) in OldTools)
        {
            ItemData item = FindByName<ItemData>(old, "ItemData");
            if (item != null) paths.Add(AssetDatabase.GetAssetPath(item));
            string prefab = OldPrefabFolder + "/" + old + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) != null) paths.Add(prefab);
        }
        int trashed = 0;
        foreach (string path in paths)
        {
            if (AssetDatabase.MoveAssetToTrash(path)) trashed++;
        }
        return trashed > 0 ? trashed + " old tool files moved to the trash" : "";
    }

    static bool IsOldPrefab(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith(OldPrefabFolder + "/")) return false;
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        foreach (var (old, _, _) in OldTools)
        {
            if (name == old) return true;
        }
        return false;
    }

    // ----- Small helpers -----

    // An asset by its exact file name (e.g. "Plank"), of this type ("ItemData", "Prefab"); null if there's none.
    static T FindByName<T>(string name, string type) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:" + type, new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
        }
        return null;
    }

    static void Dialog(string text)
    {
        EditorUtility.DisplayDialog(ToolName, text, "OK");
    }
}
