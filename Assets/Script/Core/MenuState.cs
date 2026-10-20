/*
First Date: 5/10/2026
Date: 6/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component).
        Menus tell it when they open and close:  MenuState.SetOpen(this, true / false)
        Other scripts ask:                        if (MenuState.AnyOpen) ...
Purpose: One place to check "is any menu open?" (menus free the cursor and pause aiming).
         It doesn't know any menu by name, so it can be copied to another project on its own,
         and a new menu only has to call SetOpen; every script that asks follows by itself.
         KeysCaptured = a menu is reading EVERY key right now (e.g. the key binding menu waiting for a new key),
         so gameplay must ignore the keyboard.
Requirement: MenuState.cs <-x BlueprintPlacer.cs, BuildMenu.cs, CameraController.cs, CraftingMenu.cs, DeathScreen.cs, HeldItemView.cs, HotbarSelector.cs, InventoryWindow.cs, ItemConsumer.cs, ItemDropper.cs, ItemGrabber.cs, ObjectLevitator.cs, PlayerController.cs, PlayerJump.cs, VictoryScreen.cs
*/

using System.Collections.Generic;

public static class MenuState
{
    // ===== Fields =====
    static readonly HashSet<object> openMenus = new HashSet<object>();   // the menus open right now
    static readonly HashSet<object> keyCatchers = new HashSet<object>(); // menus reading every key right now

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

    // Static values outlive the scene (and Play sessions if domain reload is off): start clean every time.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        openMenus.Clear();
        keyCatchers.Clear();
    }
}
