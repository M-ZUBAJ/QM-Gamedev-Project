/*
First Date: 8/10/2026
Date: 20/10/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: To reorder the Tools menu, change the numbers below and save. Unity updates the menu after it compiles
              (if it doesn't, restart Unity: it sometimes remembers the old order).
Purpose: The order of every item in our Tools menu, in ONE place.
         Rules of Unity's menus:
         - Smaller number = higher up in its menu.
         - Numbers more than 10 apart = a separator line between them (a new group).
         - A submenu (UI, Building, Items...) sits where its smallest number is.
         Each tool's [MenuItem] uses one of these numbers, e.g. [MenuItem("Tools/UI/Build Inventory UI", false, MenuOrder.BuildInventoryUI)].
Requirement: MenuOrder.cs <-x BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, GameEndUIBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs, ItemTools.cs, ScriptMapTool.cs
*/

public static class MenuOrder
{
    // ===== Tools > UI =====
    // Inventory
    public const int BuildInventoryUI = 100;
    public const int AddInventoryScripts = 101;
    // --- line ---
    // Build menu (B)
    public const int BuildBuildMenuUI = 120;
    // --- line ---
    // Build mode (while placing)
    public const int BuildBuildModeUI = 140;
    public const int AddBuildModeScripts = 141;
    // --- line ---
    // HUD (bars, crosshair, day / time / coin counters)
    public const int BuildHUD = 160;
    // --- line ---
    // Game end (victory screen)
    public const int BuildGameEndUI = 180;

    // ===== Tools > Building =====
    public const int MakeBuildingFromSelection = 200;
    public const int UpdateBuildMenuList = 201;

    // ===== Tools > Items =====
    public const int MakeItemFromSelection = 300;
    // --- line ---
    public const int ConvertTaggedObjectsToItems = 320;   // one-time move from tags, rarely needed

    // ===== Tools > Script Map =====
    public const int ScriptMapUpdateNow = 900;
    public const int ScriptMapAutoUpdate = 901;
}
