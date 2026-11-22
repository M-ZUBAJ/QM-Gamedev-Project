/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to put on an object: it sets itself up when the game starts (only in a finished build).
Unity Editor: Nothing to set. In the Editor every message still shows in the Console as before. To see the plain messages
              in a build too, tick File > Build Settings > Development Build.
Purpose: Keeps a finished build (the web game) fast: plain Debug.Log messages ("TreeNode took 25 damage", "Chest(Clone)
         costs 2 coins to open": one for every hit and every chest that appears) aren't written there any more; warnings
         and errors still are. In the web game each message went to the browser's console with its stack trace, which
         takes a long time for every one, so hits and new land appearing made the game stutter.
Requirement: None
*/

using UnityEngine;

public static class BuildLogs
{
    // ===== My methods =====

    // Unity calls this once when the game starts, before the first scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void QuietPlainLogs()
    {
#if !UNITY_EDITOR
        if (!Debug.isDebugBuild) Debug.unityLogger.filterLogType = LogType.Warning;   // warnings, errors and exceptions only
#endif
    }
}
