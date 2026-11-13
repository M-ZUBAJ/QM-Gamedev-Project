/*
First Date: 8/10/2026
Date: 13/11/2026
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
Requirement: MenuOrder.cs <-x BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, DeathEffectBuilder.cs, EnemyBuilder.cs, FirstPersonHandsBuilder.cs, GameEndUIBuilder.cs, GrasslandTreeBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs, ItemTools.cs, NotificationUIBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, PunchEffectBuilder.cs, ScriptMapTool.cs, TierToolBuilder.cs, TitleSceneBuilder.cs, WildAnimalBuilder.cs, WorldSpawnSetup.cs
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
    // HUD (bars, crosshair, day / time / coin counters) and the "+3 Wood" messages
    public const int BuildHUD = 160;
    public const int BuildNotifications = 161;
    // --- line ---
    // Game end (victory screen)
    public const int BuildGameEndUI = 180;
    // --- line ---
    // Main title scene (title, new game, settings)
    public const int BuildTitleScene = 195;

    // ===== Tools > Building =====
    public const int MakeBuildingFromSelection = 200;
    public const int UpdateBuildMenuList = 201;

    // ===== Tools > World =====
    public const int UseGrasslandTrees = 225;   // the placeholder tree -> low-poly grassland trees (Broken Vector's pack)
    public const int MixTreesInScene = 226;     // random look, turn and size for the trees in the open scene
    public const int BuildWildAnimal = 227;     // an animal model (ithappy's deer) -> a huntable animal that runs from the player
    public const int SetUpWorldSpawning = 228;  // ObjectScatter spacing groups + forest patches, deer spawned out of the player's sight

    // ===== Tools > Player =====
    public const int AddCharacterModel = 250;   // the animated character on the Player's ball
    public const int AddFirstPersonHands = 251; // Minecraft-style hands on the camera for first person
    public const int SetUpPlayerAnimations = 252; // Mixamo clips, both controllers' timing, first-person arms, PunchCombo

    // ===== Tools > Enemies =====
    public const int BuildMonsterEnemy = 270;   // a monster model -> an animated enemy prefab (e.g. SlimeEnemy)

    // ===== Tools > Effects =====
    public const int BuildPunchWindBurst = 285; // the grey wind burst where a bare-handed punch hits something with health
    public const int BuildDeathParticles = 286; // creatures die into particles: harmless green, harmful red

    // ===== Tools > Items =====
    public const int MakeItemFromSelection = 300;
    public const int BuildTierTools = 301;      // 15 tools (pickaxe / axe / sword x Wood ... Diamond) from the Low Poly Tool Pack
    public const int RebuildToolModels = 302;   // only the 15 tool prefabs again (handle length, materials); Item Data untouched
    // --- line ---
    public const int ConvertTaggedObjectsToItems = 320;   // one-time move from tags, rarely needed

    // ===== Tools > Script Map =====
    public const int ScriptMapUpdateNow = 900;
    public const int ScriptMapAutoUpdate = 901;
}
