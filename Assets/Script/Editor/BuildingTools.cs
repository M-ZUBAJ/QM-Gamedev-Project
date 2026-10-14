/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Tools > Building > Make Building From Selection
                Select the finished building (its top object, in the scene) or its prefab (in the Project window),
                then click it. For each one it:
                - makes sure there is a prefab (new ones go in Assets/Prefabs/Building),
                - reuses the Building Data that already uses that prefab, or the one with the same name (searched in
                  the WHOLE project), or makes a new one in Assets/Config/Building,
                - sets the Building Data's Prefab.
                The build menu (B) lists it by itself (BuildMenuAutoList). Then click the Building Data and set its
                Display Name, Description, Icon and Recipe.
              Full steps: Docs/Build Menu Setup.md
Purpose: Makes building config files (Building Data) and wires them to their prefab, so nobody has to do it by hand.
Requirement: BuildingTools.cs x-> BuildingData.cs, ItemPickup.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildingTools
{
    // ===== Fields =====
    const string DataFolder = "Assets/Config/Building";     // where NEW Building Data files go (existing ones are found anywhere)
    const string PrefabFolder = "Assets/Prefabs/Building";  // prefabs made from scene objects

    // ===== Menu (Unity calls these) =====

    [MenuItem("Tools/Building/Make Building From Selection", false, MenuOrder.MakeBuildingFromSelection)]
    static void MakeFromSelection()
    {
        GameObject[] picked = Selection.gameObjects;
        if (picked.Length == 0)
        {
            EditorUtility.DisplayDialog("Make Building",
                "Select the building first: its top object in the scene, or its prefab in the Project window.", "OK");
            return;
        }

        List<BuildingData> made = new List<BuildingData>();
        foreach (GameObject go in picked)
        {
            BuildingData data = MakeBuilding(go);
            if (data != null && !made.Contains(data)) made.Add(data);
        }

        AssetDatabase.SaveAssets();
        Selection.objects = made.ToArray();   // shows the Building Data, ready to fill in its recipe
        Debug.Log("Buildings ready: " + string.Join(", ", made.Select(d => d.name)) +
                  ". Set each one's Recipe (and Icon, Description) in the Inspector, then save the scene (Ctrl + S).");
    }

    // ===== My methods =====

    // Makes (or reuses) the prefab and the Building Data for this object, and links them.
    static BuildingData MakeBuilding(GameObject go)
    {
        if (go == null) return null;
        string buildingName = ItemPickup.CleanName(go.name);   // "Furnace (2)" -> "Furnace"

        // The prefab: the selected prefab itself, the prefab a scene copy came from, or a new one saved from the scene object.
        GameObject prefab = PrefabOf(go);
        if (prefab == null)
        {
            UIBuildHelper.MakeFolders(PrefabFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath(PrefabFolder + "/" + buildingName + ".prefab");   // never overwrite
            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.UserAction);
            if (prefab == null) return null;
        }

        // Reuse a Building Data that already points to this prefab, or has this name; otherwise make one.
        BuildingData data = FindBuildingData(prefab, buildingName);
        if (data == null)
        {
            UIBuildHelper.MakeFolders(DataFolder);
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(DataFolder + "/" + buildingName + ".asset"));
        }

        // Fill in the Prefab only if it is empty (never replace one the developer chose).
        SerializedObject so = new SerializedObject(data);   // "prefab" must match the field name in BuildingData
        SerializedProperty prop = so.FindProperty("prefab");
        if (prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
        else if (prop.objectReferenceValue != prefab)
        {
            Debug.LogWarning("Building Data \"" + data.name + "\" already uses another prefab (" +
                             prop.objectReferenceValue.name + "). Left it as it is.", data);
        }

        return data;
    }

    // The prefab file behind this object: itself if it is one, the source of a scene copy, or null (a plain scene object).
    static GameObject PrefabOf(GameObject go)
    {
        if (!go.scene.IsValid()) return go.transform.root.gameObject;   // a prefab in the Project window
        if (!PrefabUtility.IsPartOfPrefabInstance(go)) return null;      // a plain scene object

        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        return PrefabUtility.GetCorrespondingObjectFromSource(root);
    }

    // First the Building Data whose Prefab is this prefab (its file name may differ, e.g. "Crafting Table"),
    // then the one whose file name is exactly "buildingName". Any folder. null = none.
    static BuildingData FindBuildingData(GameObject prefab, string buildingName)
    {
        BuildingData sameName = null;
        foreach (string guid in AssetDatabase.FindAssets("t:BuildingData"))
        {
            BuildingData found = AssetDatabase.LoadAssetAtPath<BuildingData>(AssetDatabase.GUIDToAssetPath(guid));
            if (found == null) continue;
            if (found.Prefab == prefab) return found;
            if (found.name == buildingName) sameName = found;
        }
        return sameName;
    }
}
