/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Set Up Chests, with GameScene open (so the chests placed in it are fixed too), then
              save the scene. Run it again any time, e.g. after the chest model changes: it measures again and keeps
              outline settings you changed. Hand-made steps: Docs/Interact Outline Setup.md
Purpose: Chests that stand on the floor and glow yellow when you can open them:
         - every prefab in Assets/Prefabs with Marko's PerkChest (the Chest prefab) gets StandOnGround, with Foot
           Offset measured from its model (how far the model's bottom is below the pivot), and InteractOutline: yellow,
           shown while you look at the chest from within its Interaction Range, the same as the E key needs;
         - the chests placed in the open scene are put on the floor right now (Ctrl + Z undoes it), so the Scene view
           shows them right too. Spawned chests (ObjectScatter) settle by themselves when the game starts.
Requirement: ChestSetup.cs x-> StandOnGround.cs, InteractOutline.cs, PerkChest.cs, MeshBounds.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ChestSetup
{
    // ===== Fields =====
    const string ToolName = "Set Up Chests";
    const string PrefabFolder = "Assets/Prefabs";                     // where it looks for chest prefabs
    static readonly Color Yellow = new Color(1f, 0.82f, 0.1f);        // the outline of things you can open

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Set Up Chests", false, MenuOrder.SetUpChests)]
    static void Setup()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }

        List<string> done = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<PerkChest>() != null) done.Add(SetUpPrefab(path));
        }
        AssetDatabase.SaveAssets();
        if (done.Count == 0)
        {
            EditorUtility.DisplayDialog(ToolName, "No chest prefab found (a prefab in " + PrefabFolder + " with PerkChest on it).", "OK");
            return;
        }

        int settled = SettlePlacedChests();
        Debug.Log(ToolName + ": " + string.Join("; ", done) + ". " +
                  (settled > 0 ? settled + " chests placed in the open scene were put on the floor (save the scene). " : "") +
                  "Spawned chests settle by themselves. Test: Play, look at a chest from close by: a yellow outline glows.");
    }

    // ===== My methods =====

    // One chest prefab: StandOnGround with its Foot Offset measured, and InteractOutline (made yellow only when new,
    // so your own color and range stay on later runs). Returns a short report.
    static string SetUpPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // How far the model's bottom is below the pivot (real mesh corners, so an animated mesh measures right).
            Bounds seen = MeshBounds.Of(root);
            float scaleY = Mathf.Max(0.0001f, root.transform.lossyScale.y);
            float foot = Mathf.Max(0f, (root.transform.position.y - seen.min.y) / scaleY);

            StandOnGround ground = root.GetComponent<StandOnGround>();
            if (ground == null) ground = root.AddComponent<StandOnGround>();
            SerializedObject groundSettings = new SerializedObject(ground);
            groundSettings.FindProperty("footOffset").floatValue = foot;
            groundSettings.ApplyModifiedPropertiesWithoutUndo();

            float range = 3f;
            PerkChest chest = root.GetComponent<PerkChest>();
            SerializedProperty chestRange = new SerializedObject(chest).FindProperty("interactionRange");
            if (chestRange != null) range = chestRange.floatValue;   // Marko's E reach (read only)

            InteractOutline outline = root.GetComponent<InteractOutline>();
            bool added = outline == null;
            if (added) outline = root.AddComponent<InteractOutline>();
            SerializedObject outlineSettings = new SerializedObject(outline);
            if (added) outlineSettings.FindProperty("color").colorValue = Yellow;
            outlineSettings.FindProperty("showWhen").enumValueIndex = (int)OutlineShowWhen.LookAt;
            outlineSettings.FindProperty("range").floatValue = range;   // shows exactly when E opens it
            outlineSettings.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return root.name + ": model " + seen.size.y.ToString("0.00") + " m tall, its bottom " + foot.ToString("0.00") +
                   " m below its pivot, yellow outline within " + range + " m";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Puts every chest placed in the open scene on the floor now. Returns how many moved.
    static int SettlePlacedChests()
    {
        int count = 0;
        foreach (StandOnGround ground in Object.FindObjectsByType<StandOnGround>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (ground.GetComponent<PerkChest>() == null) continue;
            Undo.RecordObject(ground.transform, ToolName);
            if (!ground.Settle()) continue;
            EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
            count++;
        }
        return count;
    }
}
