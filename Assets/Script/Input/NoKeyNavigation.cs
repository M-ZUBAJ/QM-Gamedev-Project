/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component): it switches itself on in every game scene (a scene with a
        PlayerController: GameScene, ProceduralGenerateScene).
Unity Editor: Nothing to set. Testing: Play GameScene, press Tab, open Crafting and click the list's scroll bar (left or
              right click, on the bar or beside the handle), then walk with W A S D: the list stays where it is. Click
              CRAFT, then jump with Space: nothing is crafted again. While playing, the scene's EventSystem shows Send
              Navigation Events unticked (it goes back to ticked when you stop: nothing is saved).
Purpose: Unity's EventSystem lets the keyboard work the menus ("navigation"): the arrow keys and W A S D (the Input
         Manager's Horizontal and Vertical) move the last clicked scroll bar or slider, or move the highlight to the
         next button, Space and Enter (Submit) press the last clicked button again, and Esc (Cancel) closes a dropdown.
         In the game those keys walk, run and jump, and the player can walk with the inventory open, so a scroll bar
         you clicked scrolled while you walked (and Space after CRAFT crafted again). In every game scene this turns
         that off for every menu, now and later: they're used with the mouse there (the mouse wheel still scrolls).
         The title screen keeps it (its dropdowns use the arrow keys and Esc, and nothing walks there).
Requirement: NoKeyNavigation.cs x-> EventSystem (Unity)
             NoKeyNavigation.cs o-> PlayerController.cs
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class NoKeyNavigation
{
    // ===== My methods =====

    // Unity calls this when a scene has loaded: its objects woke up (Awake), but haven't started (Start).
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TurnOffInGame();
    }

    // In a game scene (one with a player), every EventSystem stops sending the keyboard to the menus.
    static void TurnOffInGame()
    {
        if (Object.FindFirstObjectByType<PlayerController>() == null) return;   // no player: the title screen keeps it

        foreach (EventSystem events in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            events.sendNavigationEvents = false;   // the Inspector's "Send Navigation Events"
    }

    // Static values outlive Play sessions when domain reload is off: listen for scenes again every time (only once).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Once more after the first scene: with Edit > Project Settings > Editor > Enter Play Mode Options' "Reload Scene"
    // off, Play doesn't load the open scene again, so no sceneLoaded comes for it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterFirstScene()
    {
        TurnOffInGame();
    }
}
