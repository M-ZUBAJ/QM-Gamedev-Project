/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up: it makes itself when the game starts (a hidden object kept through every scene).
Unity Editor: Nothing to set. The F11 key can be changed on the Key Bindings page ("Fullscreen"). Testing: in a build,
              press Alt + Enter or F11: the game fills the screen, press again (or Esc in the browser) to leave it. (In
              the editor's Game view nothing happens: Unity can't make it fullscreen.)
Purpose: Alt + Enter or F11 switch fullscreen on and off anywhere in the game: the title screen, the game, the menus
         (DisplaySettings.ToggleFullscreen). Not while the key list waits for a new key (that key may be what the player
         wants to set). A Windows / Mac build already does Alt + Enter by itself (Player Settings > Allow Fullscreen
         Switch), so there it only adds F11; the web build gets both.
Requirement: FullscreenKeys.cs x-> DisplaySettings.cs, KeyBindings.cs, KeyBindingMenu.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class FullscreenKeys : MonoBehaviour
{
    // ===== Unity messages (Unity calls these) =====

    // Makes the one hidden FullscreenKeys when the game starts; it lives on through every scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void MakeOne()
    {
        if (FindFirstObjectByType<FullscreenKeys>() != null) return;
        GameObject go = new GameObject("FullscreenKeys");
        DontDestroyOnLoad(go);
        go.AddComponent<FullscreenKeys>();
    }

    void Update()
    {
        if (KeyBindingMenu.IsListening) return;   // the key list is waiting for a key: F11 / Alt + Enter may be the new key

        bool altEnter = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
                        (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
#if UNITY_STANDALONE
        altEnter = false;   // a Windows / Mac build switches on Alt + Enter by itself: doing it here too would undo it
#endif
        if (altEnter || Input.GetKeyDown(KeyBindings.Fullscreen)) DisplaySettings.ToggleFullscreen();
    }
}
