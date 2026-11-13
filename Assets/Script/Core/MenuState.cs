/*
First Date: 5/10/2026
Date: 10/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component).
        Menus tell it when they open and close:  MenuState.SetOpen(this, true / false)
        Other scripts ask:                        if (MenuState.AnyOpen) ...
        Close every menu at once:                 MenuState.CloseAll()   (a menu listens to CloseAllRequested)
Purpose: One place to check "is any menu open?" (menus free the cursor and pause aiming).
         It doesn't know any menu by name, so it can be copied to another project on its own,
         and a new menu only has to call SetOpen; every script that asks follows by itself.
         KeysCaptured = a menu is reading EVERY key right now (e.g. the key binding menu waiting for a new key),
         so gameplay must ignore the keyboard.
         CloseAll() asks every menu to close itself (PlayerRespawn and DeathScreen call it when the player dies,
         so only the death page is left on screen).
Requirement: MenuState.cs <-x BlueprintPlacer.cs, BuildMenu.cs, CameraController.cs, CraftingMenu.cs, DeathScreen.cs, HeldItemView.cs, HotbarSelector.cs, InventoryWindow.cs, ItemConsumer.cs, ItemDropper.cs, ItemGrabber.cs, ObjectLevitator.cs, PerkSelectionUI.cs, PlayerCombat.cs, PlayerController.cs, PlayerJump.cs, PlayerRespawn.cs, VictoryScreen.cs
*/

using System.Collections.Generic;

public static class MenuState
{
    // ===== Fields =====
    static readonly HashSet<object> openMenus = new HashSet<object>();   // the menus open right now
    static readonly HashSet<object> keyCatchers = new HashSet<object>(); // menus reading every key right now

    // Fired by CloseAll(). Each menu that listens closes itself, the same way its own key or button closes it
    // (subscribe in OnEnable, unsubscribe in OnDisable).
    public static event System.Action CloseAllRequested;

    // ===== My methods =====

    public static bool AnyOpen => openMenus.Count > 0;

    public static bool KeysCaptured => keyCatchers.Count > 0;

    // True if a menu OTHER than this one is open (e.g. "don't open on top of another menu").
    public static bool OthersOpen(object menu)
    {
        return openMenus.Count > (openMenus.Contains(menu) ? 1 : 0);
    }

    // A menu calls this when it opens (true) and closes (false), and with false in OnDestroy.
    public static void SetOpen(object menu, bool open)
    {
        if (open) openMenus.Add(menu);
        else openMenus.Remove(menu);
    }

    // A menu that reads every key (like the key binding menu) calls this when it opens / closes.
    public static void SetKeysCaptured(object menu, bool captured)
    {
        if (captured) keyCatchers.Add(menu);
        else keyCatchers.Remove(menu);
    }

    // Asks every open menu to close, e.g. when the player dies (the death page should be the only thing on screen).
    public static void CloseAll()
    {
        CloseAllRequested?.Invoke();
    }

    // Static values outlive the scene (and Play sessions if domain reload is off): start clean every time.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        openMenus.Clear();
        keyCatchers.Clear();
        CloseAllRequested = null;
    }
}
