/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > UI > Build Pause Menu (it asks first, and saves your open scenes). It makes
              Assets/Prefabs/UI/Pause/PauseCanvas.prefab and puts it in every game scene in File > Build Settings
              (GameScene, ProceduralGenerateScene), saving them, then opens your scenes again. Run it again any time:
              the prefab is made again and the scenes keep their copy (one each). Guide: Docs/Pause Menu Setup.md
Purpose: The Esc pause menu, in one click:
           PauseCanvas (Canvas, Sort Order 40: over the HUD, chest panels and the victory page, under the screen fader;
                        PauseMenu)
             > Pause (switched off until Esc): a dark backdrop, PAUSED, < RESUME (top left), MAIN MENU (top right),
               the settings' KEY BINDINGS, CONTROLS and DISPLAY tabs (made by the title screen's own builder,
               TitleSceneBuilder.MakeSettingsTabs, so they look and work the same: KeyBindingMenu, ControlsMenu,
               DisplayMenu) and a hint at the bottom.
         An EventSystem is added to a scene that has none (the buttons need one).
Requirement: PauseMenuBuilder.cs x-> PauseMenu.cs, TitleSceneBuilder.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UIBuildHelper;    // NewUI, Stretch, CheckTMP, MakeFolders, MakeEventSystemIfMissing

public static class PauseMenuBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Pause Menu";
    const string PrefabFolder = "Assets/Prefabs/UI/Pause";
    const string PrefabPath = PrefabFolder + "/PauseCanvas.prefab";
    const int SortOrder = 40;   // HUD 0, notifications 5, chest panels 20 / 21, victory 30; the screen fader is 1000
    static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.78f);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Pause Menu", false, MenuOrder.BuildPauseMenu)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;

        List<string> scenes = GameScenes();
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        if (!EditorUtility.DisplayDialog(ToolName,
                "This makes the pause menu (Esc while playing):\n\n" +
                "- KEY BINDINGS (the same list as the title screen's Settings)\n" +
                "- CONTROLS: Mouse sensitivity (1 - 10) and Invert mouse\n" +
                "- RESUME and MAIN MENU; the game stops while it's open\n\n" +
                (exists ? "It replaces " + PrefabPath + ".\n" : "Saved as " + PrefabPath + ".\n") +
                "It's put in (and saves): " + (scenes.Count > 0 ? string.Join(", ", scenes.ConvertAll(Path.GetFileNameWithoutExtension)) : "no game scene in Build Settings") +
                ".\n\nContinue?", "Build", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;   // unsaved changes: save them, or stop

        SceneSetup[] before = EditorSceneManager.GetSceneManagerSetup();
        GameObject prefab = SavePrefab();
        List<string> done = new List<string>();
        foreach (string path in scenes) done.Add(PutInScene(path, prefab));
        if (before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);

        Debug.Log(ToolName + ": " + PrefabPath + " made; " + (done.Count > 0 ? string.Join(" ", done) : "no game scene in Build Settings " +
                  "(drag PauseCanvas into your scene)") + " Test: Play GameScene and press Esc.");
    }

    // ===== My methods =====

    // The game scenes: the ones in File > Build Settings with a player (PlayerController) in them.
    static List<string> GameScenes()
    {
        List<string> found = new List<string>();
        string[] scripts = AssetDatabase.FindAssets("PlayerController t:MonoScript");
        string guid = scripts.Length > 0 ? scripts[0] : null;
        if (string.IsNullOrEmpty(guid)) return found;
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.path) || !File.Exists(entry.path)) continue;
            if (File.ReadAllText(entry.path).Contains(guid)) found.Add(entry.path);
        }
        return found;
    }

    // Builds the pause menu and saves it as the prefab (the same file each time, so the scenes' copies stay linked).
    static GameObject SavePrefab()
    {
        MakeFolders(PrefabFolder);
        GameObject root = new GameObject("PauseCanvas", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        try
        {
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            TitleSceneBuilder.AddScaler(root);   // 1920 x 1080, like the title screen
            root.AddComponent<GraphicRaycaster>();

            // Everything shown while paused (PauseMenu switches it on and off).
            GameObject content = NewUI("Pause", root.transform);
            Stretch(content, 0f, 0f, 0f, 0f);
            content.AddComponent<CanvasGroup>();   // it fades in

            Image backdrop = NewUI("Backdrop", content.transform).AddComponent<Image>();
            Stretch(backdrop.gameObject, 0f, 0f, 0f, 0f);
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;   // nothing behind it can be clicked

            TextMeshProUGUI title = TitleSceneBuilder.MakeText(content.transform, "PageTitle", "PAUSED", 60f, Color.white,
                                                               TextAlignmentOptions.Center);
            TitleSceneBuilder.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(900f, 80f));
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 10f;

            Button resume = TitleSceneBuilder.MakeButton(content.transform, "ResumeButton", "< RESUME", new Vector2(240f, 64f), 28f, 8, false, false);
            TitleSceneBuilder.Place(resume.gameObject, new Vector2(0f, 1f), new Vector2(160f, -72f), new Vector2(240f, 64f));
            Button menu = TitleSceneBuilder.MakeButton(content.transform, "MainMenuButton", "MAIN MENU", new Vector2(250f, 64f), 26f, 8, false, false);
            TitleSceneBuilder.Place(menu.gameObject, new Vector2(1f, 1f), new Vector2(-165f, -72f), new Vector2(250f, 64f));

            TitleSceneBuilder.MakeSettingsTabs(content.transform, false);   // KEY BINDINGS, CONTROLS and DISPLAY, like the title screen

            TextMeshProUGUI hint = TitleSceneBuilder.MakeText(content.transform, "PauseHint", "Esc: back to the game",
                                                              22f, TitleSceneBuilder.Dim, TextAlignmentOptions.Center);
            TitleSceneBuilder.Place(hint.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(800f, 36f));

            root.AddComponent<PauseMenu>();
            content.SetActive(false);   // shown with Esc
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // Opens a game scene, keeps (or puts in) one copy of the prefab, an EventSystem, and saves it. Returns a report.
    static string PutInScene(string path, GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        bool kept = false;
        foreach (PauseMenu menu in Object.FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(menu.gameObject);
            bool ours = root != null && PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab;
            if (ours && !kept)
            {
                kept = true;
                continue;
            }
            Object.DestroyImmediate(root != null ? root : menu.gameObject);   // a second one, or one made by hand
        }
        if (!kept)
        {
            GameObject made = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            made.name = prefab.name;
        }
        MakeEventSystemIfMissing(ToolName);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return scene.name + (kept ? " (its pause menu updated, saved)." : " (pause menu added, saved).");
    }
}
