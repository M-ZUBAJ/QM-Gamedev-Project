/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then Tools > World > Set Up World Spawning, then save the scene. Ctrl + Z undoes it.
              Run it again any time: it only fills in settings that are still empty, and never changes your counts.
              Hand-made steps: Docs/World Spawning Setup.md
Purpose: Sets up how the world fills itself, in the open scene:
         - Sanyam's ObjectScatter: the tree rules get Spacing Group "Trees" (trees keep Min Spacing from every tree
           look), forest patches (Patch Size 120 m, Open Ground 0.35 = meadows between the forests) and sizes 0.85-1.2;
           rocks get sizes 0.7-1.3; every rule keeps Clearance 3 m from other kinds. So many trees still leave room for
           forest rocks and berry bushes.
         - Its animal rules (prefabs with WildAnimal, e.g. DeerNode) move to an AnimalSpawner, which keeps deer around
           the player and only spawns them where the player can't see.
         - PlayerSight on the Player (the line of sight the AnimalSpawner asks).
Requirement: WorldSpawnSetup.cs x-> ObjectScatter.cs, AnimalSpawner.cs, PlayerSight.cs, WildAnimal.cs, ResourceNode.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UIBuildHelper;   // AddToPlayer, FindPrefabPath

public static class WorldSpawnSetup
{
    // ===== Fields =====
    const string ToolName = "Set Up World Spawning";
    const string DefaultAnimalPath = "Assets/Prefabs/ResourceNodes/DeerNode.prefab";   // Tools > World > Build Wild Animal

    const string TreeGroup = "Trees";      // every tree look keeps the trees' Min Spacing from the others
    const float Clearance = 3f;            // meters from other kinds: a rock or bush not inside a tree trunk
    const float TreePatchSize = 120f;      // meters across one forest
    const float TreeOpenGround = 0.35f;    // about a third of the grass stays open meadow (room for deer, a view)
    static readonly Vector2 TreeSizes = new Vector2(0.85f, 1.2f);
    static readonly Vector2 RockSizes = new Vector2(0.7f, 1.3f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Set Up World Spawning", false, MenuOrder.SetUpWorldSpawning)]
    static void Setup()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }

        Undo.SetCurrentGroupName(ToolName);
        int undoGroup = Undo.GetCurrentGroup();
        List<string> report = new List<string>();

        List<ObjectScatter.ScatterRule> animalRules = new List<ObjectScatter.ScatterRule>();
        Terrain terrain = null;
        ObjectScatter[] scatters = Object.FindObjectsByType<ObjectScatter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ObjectScatter scatter in scatters)
        {
            report.Add(FixScatter(scatter, animalRules));
            if (terrain == null) terrain = scatter.terrain;
        }
        if (scatters.Length == 0) report.Add("no ObjectScatter in the open scene (open GameScene for the trees, rocks and bushes)");

        report.Add(SetUpAnimalSpawners(animalRules, terrain));

        AddToPlayer<PlayerSight>();
        report.Add(Object.FindFirstObjectByType<PlayerSight>() != null ? "the Player has PlayerSight (its line of sight)" :
                                                                       "no Player found: drag PlayerSight onto it");

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log(ToolName + ":\n- " + string.Join("\n- ", report) +
                  "\nSave the scene, then Play: the Console says what each rule spawned and how many deer came.");
    }

    // ===== My methods =====

    // Fills the new ObjectScatter settings on its rules and takes its animal rules out (they go to an AnimalSpawner).
    static string FixScatter(ObjectScatter scatter, List<ObjectScatter.ScatterRule> animalRules)
    {
        Undo.RecordObject(scatter, ToolName);
        int trees = 0, rocks = 0, moved = 0;
        for (int i = scatter.rules.Count - 1; i >= 0; i--)
        {
            ObjectScatter.ScatterRule rule = scatter.rules[i];
            if (rule.prefab != null && rule.prefab.GetComponent<WildAnimal>() != null)
            {
                animalRules.Insert(0, rule);
                scatter.rules.RemoveAt(i);
                moved++;
                continue;
            }

            if (rule.clearance <= 0f) rule.clearance = Clearance;
            ResourceNode node = rule.prefab != null ? rule.prefab.GetComponentInChildren<ResourceNode>(true) : null;
            if (node == null) continue;
            if (node.resourceType == ResourceNode.ResourceType.Tree)
            {
                if (string.IsNullOrEmpty(rule.spacingGroup)) rule.spacingGroup = TreeGroup;
                if (rule.patchSize <= 0f)
                {
                    rule.patchSize = TreePatchSize;
                    rule.openGround = TreeOpenGround;
                }
                SetSizes(rule, TreeSizes);
                trees++;
            }
            else if (node.resourceType == ResourceNode.ResourceType.Stone)
            {
                SetSizes(rule, RockSizes);
                rocks++;
            }
        }
        EditorUtility.SetDirty(scatter);
        return "ObjectScatter on \"" + scatter.name + "\": " + trees + " tree rules (group \"" + TreeGroup + "\", forest patches), " +
               rocks + " rock rules, Clearance " + Clearance + " m for every rule" +
               (moved > 0 ? ", " + moved + " animal rule(s) moved to an AnimalSpawner" : "");
    }

    // Only while the sizes are still empty (0) or the plain 1 to 1, so sizes you chose stay.
    static void SetSizes(ObjectScatter.ScatterRule rule, Vector2 sizes)
    {
        bool plain = rule.minSize <= 0f || rule.maxSize <= 0f || (rule.minSize == 1f && rule.maxSize == 1f);
        if (!plain) return;
        rule.minSize = sizes.x;
        rule.maxSize = sizes.y;
    }

    // One AnimalSpawner per animal: the animal rules taken from the ObjectScatter, else DeerNode. A spawner that already
    // spawns that animal is kept (only its terrain is filled in if empty).
    static string SetUpAnimalSpawners(List<ObjectScatter.ScatterRule> animalRules, Terrain terrain)
    {
        AnimalSpawner[] existing = Object.FindObjectsByType<AnimalSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (animalRules.Count == 0)
        {
            if (existing.Length > 0) return "AnimalSpawner already set up";
            GameObject deer = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(DefaultAnimalPath));
            if (deer == null) return "no animal to spawn: build DeerNode with Tools > World > Build Wild Animal, then run this again";
            animalRules.Add(new ObjectScatter.ScatterRule { name = deer.name, prefab = deer, terrainLayer = 0, minStrength = 0.6f, maxSlope = 20f });
        }

        List<string> done = new List<string>();
        foreach (ObjectScatter.ScatterRule rule in animalRules)
        {
            AnimalSpawner spawner = System.Array.Find(existing, s => s.AnimalPrefab == rule.prefab);
            bool made = spawner == null;
            if (made)
            {
                GameObject holder = new GameObject("AnimalSpawner - " + rule.prefab.name);
                Undo.RegisterCreatedObjectUndo(holder, ToolName);
                spawner = holder.AddComponent<AnimalSpawner>();
            }

            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("animalPrefab").objectReferenceValue = rule.prefab;
            SerializedProperty terrainField = so.FindProperty("terrain");
            if (terrainField.objectReferenceValue == null) terrainField.objectReferenceValue = terrain;
            if (made)
            {
                // The same ground as the old rule: its texture, how strongly painted, how steep.
                so.FindProperty("terrainLayer").intValue = rule.terrainLayer;
                so.FindProperty("minStrength").floatValue = rule.minStrength;
                so.FindProperty("maxSlope").floatValue = rule.maxSlope;
            }
            so.ApplyModifiedProperties();
            done.Add("\"" + spawner.name + "\" " + (made ? "made" : "kept") + " (" + rule.prefab.name + " around the player, out of sight)");
        }
        return string.Join(", ", done);
    }
}
