/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Nothing to click. Make, delete or move a Building Data and the build menu (B) lists it by itself.
              (Tools > Building > Update Build Menu List still does the same by hand.)
Purpose: Keeps the build menu's Buildings list up to date without anyone remembering to click a menu:
         - when Unity finishes importing files and a Building Data was made, deleted or moved,
         - and once each time scripts finish compiling (catches anything made while this tool didn't exist).
         It only saves the build menu prefab when the list really changed.
Requirement: BuildMenuAutoList.cs x-> BuildMenuUIBuilder.cs, BuildingData.cs
*/

using UnityEditor;

public class BuildMenuAutoList : AssetPostprocessor
{
    // ===== Unity messages (Unity calls these) =====

    // Unity calls this after it imports, deletes or moves files.
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (TouchesBuildingData(imported) || TouchesBuildingData(moved) || AnyAssetFile(deleted))
        {
            Schedule();
        }
    }

    // Unity calls this every time it finishes compiling scripts.
    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsCompiled()
    {
        Schedule();
    }

    // ===== My methods =====

    // Waits until Unity is idle: prefabs can't be opened and saved in the middle of an import.
    static void Schedule()
    {
        EditorApplication.delayCall -= Run;   // never queue it twice
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        BuildMenuUIBuilder.UpdateListNow(false);   // quiet: no pop-ups, a Console line only when it changed
    }

    static bool TouchesBuildingData(string[] paths)
    {
        foreach (string path in paths)
        {
            if (path.EndsWith(".asset") && AssetDatabase.LoadAssetAtPath<BuildingData>(path) != null) return true;
        }
        return false;
    }

    // A deleted file can't be loaded any more, so any deleted .asset might have been a Building Data.
    static bool AnyAssetFile(string[] paths)
    {
        foreach (string path in paths)
        {
            if (path.EndsWith(".asset")) return true;
        }
        return false;
    }
}
