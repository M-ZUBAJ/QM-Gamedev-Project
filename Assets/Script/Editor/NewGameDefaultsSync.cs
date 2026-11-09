/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. Every time GameScene is saved, the New Game page's defaults
              (Assets/Config/NewGame/NewGameDefaults.asset) are read again from it, and the Console says so when
              something changed. Tools > UI > Build Main Title Scene also uses it to make that asset.
Purpose: Keeps "the game as it is now" up to date for the New Game page: copies from GameScene the days to win and
         win time (VictoryScreen), day length, start time and parts of the day (DaylightCycle), enemies per night
         (EnemySpawner), hunger speed (PlayerHunger) and the rules (GameRules) into NewGameDefaults.
         Also, after each compile, fills an EMPTY Defaults on the New Game page (TitleCanvas.prefab > NewGamePanel)
         with that asset: without it the page can't start and shows "Option A". It only saves the prefab then.
Requirement: NewGameDefaultsSync.cs x-> NewGameDefaults.cs, NewGamePanel.cs, VictoryScreen.cs, DaylightCycle.cs, EnemySpawner.cs, PlayerHunger.cs, GameRules.cs, KeyBindings.cs
             NewGameDefaultsSync.cs <-x TitleSceneBuilder.cs
*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]   // Unity runs the static constructor below when the editor starts and after each compile
public static class NewGameDefaultsSync
{
    // ===== Fields =====
    public const string DefaultsPath = "Assets/Config/NewGame/NewGameDefaults.asset";
    public const string GameScenePath = "Assets/Scenes/GameScene.unity";

    // ===== Unity messages (Unity calls these) =====

    static NewGameDefaultsSync()
    {
        EditorSceneManager.sceneSaved -= OnSceneSaved;
        EditorSceneManager.sceneSaved += OnSceneSaved;
        EditorApplication.delayCall += FillEmptyLinks;   // once the editor has finished loading
    }

    // ===== My methods =====

    // A New Game page with an empty Defaults can't start (its dropdowns show "Option A"): link the asset.
    // Looks first (cheap), and only opens and saves a TitleCanvas prefab whose link is really empty.
    static void FillEmptyLinks()
    {
        NewGameDefaults defaults = AssetDatabase.LoadAssetAtPath<NewGameDefaults>(DefaultsPath);
        if (defaults == null) return;

        foreach (string guid in AssetDatabase.FindAssets("TitleCanvas t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || !HasEmptyLink(asset)) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (NewGamePanel panel in root.GetComponentsInChildren<NewGamePanel>(true))
            {
                SerializedObject so = new SerializedObject(panel);
                SerializedProperty link = so.FindProperty("defaults");
                if (link == null || link.objectReferenceValue != null) continue;
                link.objectReferenceValue = defaults;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("New Game page fixed: its empty Defaults now uses " + DefaultsPath + " (" + path + ").");
        }
    }

    static bool HasEmptyLink(GameObject prefab)
    {
        foreach (NewGamePanel panel in prefab.GetComponentsInChildren<NewGamePanel>(true))
        {
            SerializedProperty link = new SerializedObject(panel).FindProperty("defaults");
            if (link != null && link.objectReferenceValue == null) return true;
        }
        return false;
    }

    static void OnSceneSaved(Scene scene)
    {
        if (scene.path != GameScenePath) return;

        NewGameDefaults defaults = AssetDatabase.LoadAssetAtPath<NewGameDefaults>(DefaultsPath);
        if (defaults == null) return;   // the title scene wasn't built yet: nothing to keep up to date

        if (ReadFrom(scene, defaults)) Debug.Log("New Game defaults updated from GameScene (" + DefaultsPath + ").");
    }

    // Copies how the game is set up in this (open) scene into the defaults. Returns true if anything changed.
    public static bool ReadFrom(Scene scene, NewGameDefaults defaults)
    {
        SerializedObject d = new SerializedObject(defaults);

        Copy(Find<VictoryScreen>(scene), d, "daysToWin", "winTime");
        DaylightCycle sun = Find<DaylightCycle>(scene);
        Copy(sun, d, "dayLengthMinutes", "dawnStart", "morningStart", "afternoonStart", "duskStart", "nightStart");
        CopyAs(sun, "timeOfDay", d, "startTime");
        Copy(Find<EnemySpawner>(scene), d, "spawnAtNight", "enemiesPerNight");
        CopyAs(Find<PlayerHunger>(scene), "drainPerSecond", d, "hungerDrainPerSecond");
        Copy(Find<GameRules>(scene), d, "keepInventory", "clearItemOnDead", "autoPickupDrops", "showNotifications");

        if (!d.ApplyModifiedPropertiesWithoutUndo()) return false;

        d.Update();
        d.FindProperty("readFrom").stringValue = scene.name + ", " + System.DateTime.Now.ToString("d/M/yyyy HH:mm");
        d.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(defaults);
        AssetDatabase.SaveAssetIfDirty(defaults);
        return true;
    }

    // The scene's KeyBindings as text (so the title scene can start with the same default keys), or null.
    public static string KeyBindingsJson(Scene scene)
    {
        KeyBindings keys = Find<KeyBindings>(scene);
        return keys != null ? EditorJsonUtility.ToJson(keys) : null;
    }

    // The first T anywhere in the scene (also on switched-off objects), or null.
    static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    // Copies fields that have the same name in the scene's script and in the defaults.
    static void Copy(Component source, SerializedObject target, params string[] names)
    {
        foreach (string n in names) CopyAs(source, n, target, n);
    }

    // Copies one field (a number, tick box or dropdown) from the scene's script into the defaults.
    static void CopyAs(Component source, string sourceName, SerializedObject target, string targetName)
    {
        if (source == null) return;
        SerializedProperty from = new SerializedObject(source).FindProperty(sourceName);
        SerializedProperty to = target.FindProperty(targetName);
        if (from == null || to == null) return;

        switch (to.propertyType)
        {
            case SerializedPropertyType.Integer: to.intValue = from.intValue; break;
            case SerializedPropertyType.Float: to.floatValue = from.floatValue; break;
            case SerializedPropertyType.Boolean: to.boolValue = from.boolValue; break;
            case SerializedPropertyType.Enum: to.enumValueIndex = from.enumValueIndex; break;
        }
    }
}
