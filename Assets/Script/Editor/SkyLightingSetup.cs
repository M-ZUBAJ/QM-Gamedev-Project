/*
First Date: 19/11/2026
Date: 20/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then Tools > World > Set Up Sky Lighting. It takes a few seconds and saves the scene.
              Run it again after changing the sky (Window > Rendering > Lighting > Environment).
              Tools > World > Build New World Scene does the same for ProceduralGenerateScene by itself (SaveSkyLight).
              Test: open MainTitleScene, Play, START GAME: shadows look like when GameScene is played directly (soft,
              blue-grey), not pitch black. Hand-made steps: Docs/Sky Lighting Setup.md
Purpose: Saves the light from the sky for the open scene, so it isn't dark when another scene opens it.
         Shadows are lit by the light from the whole sky (ambient light), and shiny things reflect the sky. Unity
         works both out by itself only for the scene opened in the Editor, and only until it closes. A scene opened by
         another scene (the title screen's START GAME) or in a built game gets only what was saved ("generated") for
         it. GameScene had nothing saved, so from the title screen every shadow was pitch black.
         It is Unity's Generate Lighting with only the sky: baked and realtime Global Illumination are switched off
         for the scene first, because light baked into the ground can't follow the day / night cycle (the ground would
         stay lit at night) and baking it would take minutes.
         Files: Assets/Scenes/<scene> Lighting.lighting (the settings) and the folder Assets/Scenes/<scene>/ (Unity's
         LightingData.asset + the sky's reflection). Commit them with the scene.
Requirement: SkyLightingSetup.cs x-> MenuOrder.cs
             SkyLightingSetup.cs <-x NewWorldBuilder.cs
*/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkyLightingSetup
{
    // ===== Fields =====
    const string ToolName = "Set Up Sky Lighting";

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Set Up Sky Lighting", false, MenuOrder.SetUpSkyLighting)]
    static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first.", "OK");
            return;
        }
        if (Lightmapping.isRunning)
        {
            EditorUtility.DisplayDialog(ToolName, "Unity is already generating lighting (bar at the bottom right). " +
                                        "Wait for it, then try again.", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog(ToolName, "Save the scene first (Ctrl + S): Unity keeps the light from the sky in a " +
                                        "folder next to the scene file.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(ToolName,
                "Save the light from the sky for \"" + scene.name + "\"?\n\n" +
                "Without it, its shadows are pitch black when the title screen opens it (and in a built game).\n\n" +
                "This switches baked and realtime Global Illumination off for this scene (they don't suit the day / night " +
                "cycle), takes a few seconds and saves the scene.",
                "Save Sky Light", "Cancel"))
            return;

        string report = SaveSkyLight(scene);
        if (report == null)
        {
            EditorUtility.DisplayDialog(ToolName, "Unity couldn't generate the lighting. The Console says why.", "OK");
            return;
        }
        Debug.Log(ToolName + ": \"" + scene.name + "\" has its light from the sky saved (" + report + "). Commit them with " +
                  "the scene. Test: open MainTitleScene, Play, START GAME: the shadows are no longer pitch black.");
    }

    // ===== My methods =====

    // Saves a scene's light from the sky, with no questions asked (the menu above asks first; Build New World Scene
    // uses this directly): sky-only Lighting Settings, Generate Lighting, then the scene is saved. Returns where the
    // light was saved, or null if Unity couldn't generate it.
    public static string SaveSkyLight(Scene scene)
    {
        if (Lightmapping.isRunning || string.IsNullOrEmpty(scene.path)) return null;
        if (SceneManager.GetActiveScene() != scene) SceneManager.SetActiveScene(scene);   // Generate Lighting works on it

        string settingsPath = UseSkyOnlySettings(scene);

        // Unity's Generate Lighting (Window > Rendering > Lighting). With both Global Illumination systems off it only
        // works out the sky's ambient light and reflection, and saves them in Assets/Scenes/<scene>/.
        if (!Lightmapping.Bake()) return null;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);   // keeps the scene's link to the saved light

        LightingDataAsset saved = Lightmapping.lightingDataAsset;
        return (saved != null ? AssetDatabase.GetAssetPath(saved) : "no Lighting Data found: see the Console") +
               ", settings " + settingsPath;
    }

    // Gives the open scene Lighting Settings with only the sky (made as Assets/Scenes/<scene> Lighting.lighting if it
    // has none). Returns their file.
    static string UseSkyOnlySettings(Scene scene)
    {
        if (!Lightmapping.TryGetLightingSettings(out LightingSettings settings) || settings == null)
        {
            settings = new LightingSettings { name = scene.name + " Lighting" };
            string folder = Path.GetDirectoryName(scene.path).Replace('\\', '/');
            AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + scene.name + " Lighting.lighting"));
        }

        settings.bakedGI = false;        // light baked into the ground would stay there at night (and takes minutes to bake)
        settings.realtimeGI = false;     // Enlighten: slow to prepare, and not needed for this look
        settings.autoGenerate = false;   // Auto Generate keeps the light only while the scene is open in the Editor
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Lightmapping.lightingSettings = settings;   // the open scene uses them
        return AssetDatabase.GetAssetPath(settings);
    }
}
