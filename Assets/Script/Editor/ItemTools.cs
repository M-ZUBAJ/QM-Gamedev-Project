/*
First Date: 7/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Tools > Items > Make Item From Selection
                Select item objects (in the scene, or prefabs in the Project window), then click it. For each one it makes
                (or reuses) Assets/Items/<Name>.asset (Item Data), makes sure there is a prefab (new ones go in
                Assets/Prefabs/Items), puts ItemPickup on it pointing to the Item Data, and points recipes to it.
                Then click the new Item Data and choose its Pickup (Pickup or AutoPickup).
              Tools > Items > Convert Tagged Objects To Items
                One-time switch from tags: every object in the open scene and every prefab tagged "PickUp" or
                "AutoPickUp" becomes an item as above (AutoPickUp tag = AutoPickup), is untagged,
                and then the two tags are deleted from the project. Save the scene afterwards (Ctrl + S).
              Full steps: Docs/Item Setup.md
Purpose: Makes item config files (Item Data) and wires them up, so nobody has to do it by hand.
Requirement: ItemTools.cs x-> ItemData.cs, ItemPickup.cs, BuildingData.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;   // InternalEditorUtility.RemoveTag
using UnityEngine;

public static class ItemTools
{
    // ===== Fields =====
    const string ItemFolder = "Assets/Items";             // the Item Data config files
    const string PrefabFolder = "Assets/Prefabs/Items";   // prefabs made from scene objects
    static readonly string[] OldTags = { "PickUp", "AutoPickUp" };

    // ===== Menu (Unity calls these) =====

    [MenuItem("Tools/Items/Make Item From Selection", false, MenuOrder.MakeItemFromSelection)]
    static void MakeFromSelection()
    {
        GameObject[] picked = Selection.gameObjects;
        if (picked.Length == 0)
        {
            EditorUtility.DisplayDialog("Make Item", "Select the item object(s) first (in the scene or the Project window).", "OK");
            return;
        }

        List<ItemData> made = new List<ItemData>();
        foreach (GameObject go in picked)
        {
            ItemData data = MakeItem(go, null);
            if (data != null && !made.Contains(data)) made.Add(data);
        }

        AssetDatabase.SaveAssets();
        Selection.objects = made.ToArray();   // shows the new Item Data, ready to choose its Pickup
        Debug.Log("Items ready: " + string.Join(", ", made.Select(d => d.name)) +
                  ". Choose each one's Pickup in the Inspector, then save the scene (Ctrl + S).");
    }

    [MenuItem("Tools/Items/Convert Tagged Objects To Items", false, MenuOrder.ConvertTaggedObjectsToItems)]
    static void ConvertTagged()
    {
        if (!EditorUtility.DisplayDialog("Convert Tagged Objects To Items",
                "Every object in the open scene and every prefab tagged \"PickUp\" or \"AutoPickUp\" becomes an item " +
                "with an Item Data config file, and the two tags are deleted from the project.\n\n" +
                "Save the scene afterwards (Ctrl + S).", "Convert", "Cancel"))
        {
            return;
        }

        List<string> done = new List<string>();

        // Prefabs first, so scene copies of them find their prefab already converted.
        foreach (string path in AllPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            PickupType? type = PickupFromTag(prefab.tag);
            if (type == null) continue;

            ItemData data = MakeItem(prefab, type);
            if (data != null) done.Add(data.name + " (prefab)");
        }

        // Then every tagged object in the open scene.
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PickupType? type = PickupFromTag(SafeTag(go));
            if (type == null) continue;

            ItemData data = MakeItem(go, type);
            if (data != null) done.Add(data.name);
        }

        // Nothing uses the tags any more: remove anything still tagged, then the tags themselves.
        UntagEverything();
        foreach (string tag in OldTags) InternalEditorUtility.RemoveTag(tag);

        AssetDatabase.SaveAssets();
        Debug.Log("Converted " + done.Count + " tagged object(s) to items" +
                  (done.Count > 0 ? ": " + string.Join(", ", done.Distinct()) : "") +
                  ". Tags \"PickUp\" and \"AutoPickUp\" are deleted. Save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    // Makes (or reuses) the Item Data for this object, its prefab, and ItemPickup pointing to it.
    // pickup: what to set the Item Data's Pickup to (null = leave it as it is).
    static ItemData MakeItem(GameObject go, PickupType? pickup)
    {
        if (go == null) return null;
        MakeFolders(ItemFolder);

        string id = ItemPickup.CleanName(go.name);   // "Wood (2)" -> "Wood"
        string dataPath = ItemFolder + "/" + id + ".asset";

        ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        // The prefab: the selected prefab itself, the prefab a scene copy came from, the one the Item Data
        // already has, or a new one saved from the scene object.
        GameObject prefab = PrefabOf(go);
        if (prefab == null) prefab = data.Prefab;
        if (prefab == null && go.scene.IsValid())
        {
            MakeFolders(PrefabFolder);
            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, PrefabFolder + "/" + id + ".prefab",
                                                               InteractionMode.UserAction);
        }

        // Fill in the Item Data (only what is empty, plus the Pickup if given).
        SerializedObject dataSo = new SerializedObject(data);   // the names in quotes must match ItemData's fields
        if (dataSo.FindProperty("prefab").objectReferenceValue == null) dataSo.FindProperty("prefab").objectReferenceValue = prefab;
        if (pickup != null) dataSo.FindProperty("pickup").enumValueIndex = (int)pickup.Value;
        dataSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);

        if (prefab != null) SetUpPrefab(prefab, data);
        if (go.scene.IsValid()) SetUpSceneObject(go, data);

        PointRecipesTo(prefab, data);
        return data;
    }

    // The prefab file behind this object: itself if it is one, the source of a scene copy, or null.
    static GameObject PrefabOf(GameObject go)
    {
        if (!go.scene.IsValid()) return go.transform.root.gameObject;   // a prefab in the Project window
        if (!PrefabUtility.IsPartOfPrefabInstance(go)) return null;      // a plain scene object

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        return PrefabUtility.GetCorrespondingObjectFromSource(root);
    }

    // Opens the prefab file and puts ItemPickup (pointing to the Item Data) on its root, untagged.
    static void SetUpPrefab(GameObject prefab, ItemData data)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        ItemPickup pickup = root.GetComponent<ItemPickup>();
        if (pickup == null) pickup = root.AddComponent<ItemPickup>();
        SetData(pickup, data);
        if (IsOldTag(root.tag)) root.tag = "Untagged";

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // A scene object: same as the prefab, but with Undo (Ctrl + Z works).
    static void SetUpSceneObject(GameObject go, ItemData data)
    {
        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup == null) pickup = Undo.AddComponent<ItemPickup>(go);

        Undo.RecordObject(pickup, "Make Item");
        SetData(pickup, data);

        if (IsOldTag(SafeTag(go)))
        {
            Undo.RecordObject(go, "Make Item");
            go.tag = "Untagged";
        }
    }

    static void SetData(ItemPickup pickup, ItemData data)
    {
        SerializedObject so = new SerializedObject(pickup);   // "data" must match the field name in ItemPickup
        SerializedProperty prop = so.FindProperty("data");
        if (prop.objectReferenceValue == data) return;      // already right (no needless prefab override)
        prop.objectReferenceValue = data;
        so.ApplyModifiedProperties();
    }

    // Recipes used to point to the item's prefab (its ItemPickup). Point those lines to the Item Data instead.
    static void PointRecipesTo(GameObject prefab, ItemData data)
    {
        if (prefab == null) return;

        foreach (string guid in AssetDatabase.FindAssets("t:BuildingData"))
        {
            BuildingData building = AssetDatabase.LoadAssetAtPath<BuildingData>(AssetDatabase.GUIDToAssetPath(guid));
            SerializedObject so = new SerializedObject(building);
            SerializedProperty recipe = so.FindProperty("recipe");   // must match the field name in BuildingData
            bool changed = false;

            for (int i = 0; i < recipe.arraySize; i++)
            {
                SerializedProperty item = recipe.GetArrayElementAtIndex(i).FindPropertyRelative("item");
                Object old = item.objectReferenceValue;
                GameObject oldObject = old is Component c ? c.gameObject : old as GameObject;
                if (oldObject == null || oldObject.transform.root.gameObject != prefab) continue;

                item.objectReferenceValue = data;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(building);
                Debug.Log(building.name + ": recipe now uses " + data.name + ".");
            }
        }
    }

    // Sets every object still tagged PickUp / AutoPickUp (scene and prefabs) back to Untagged.
    static void UntagEverything()
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!IsOldTag(SafeTag(go))) continue;
            Undo.RecordObject(go, "Untag");
            go.tag = "Untagged";
        }

        foreach (string path in AllPrefabPaths())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab.GetComponentsInChildren<Transform>(true).Any(t => IsOldTag(t.tag))) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsOldTag(t.tag)) t.gameObject.tag = "Untagged";
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Every prefab file in Assets (not plugins).
    static IEnumerable<string> AllPrefabPaths()
    {
        return AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Where(p => !p.StartsWith("Assets/Plugins"));
    }

    static PickupType? PickupFromTag(string tag)
    {
        if (tag == "AutoPickUp") return PickupType.AutoPickup;
        if (tag == "PickUp") return PickupType.Pickup;
        return null;
    }

    static bool IsOldTag(string tag)
    {
        return OldTags.Contains(tag);
    }

    // An object's tag, or "" if its tag was already deleted (reading it would log an error).
    static string SafeTag(GameObject go)
    {
        try { return go.tag; }
        catch { return ""; }
    }

    // Makes every missing folder in a path like "Assets/Prefabs/Items".
    static void MakeFolders(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
