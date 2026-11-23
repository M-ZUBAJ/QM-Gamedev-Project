/*
First Date: 8/10/2026
Date: 23/11/2026
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
Requirement: MenuOrder.cs <-x ArmorBuilder.cs, BerryBushBuilder.cs, BowBuilder.cs, BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, ChestSetup.cs, CraftingMenuBuilder.cs, DeathEffectBuilder.cs, DeathScreenBuilder.cs, DesertBuilder.cs, EnemyBuilder.cs, FirstPersonHandsBuilder.cs, GameEndUIBuilder.cs, GearPageBuilder.cs, GrasslandTreeBuilder.cs, HUDBuilder.cs, HumanEnemyBuilder.cs, InventoryUIBuilder.cs, ItemTools.cs, LootChestBuilder.cs, NewWorldBuilder.cs, NightMonsterBuilder.cs, NotificationUIBuilder.cs, PauseMenuBuilder.cs, PerkPanelBuilder.cs, PerkRollBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, PunchEffectBuilder.cs, RockBreakBuilder.cs, RockBuilder.cs, RockCaveBuilder.cs, ScriptMapTool.cs, SkyLightingSetup.cs, SnowBuilder.cs, TierToolBuilder.cs, TitleSceneBuilder.cs, TrapBuilder.cs, TreeFallBuilder.cs, WildAnimalBuilder.cs, WorldBarrierBuilder.cs, WorldSpawnSetup.cs
*/

public static class MenuOrder
{
    // ===== Tools > UI =====
    // Inventory
    public const int BuildInventoryUI = 100;
    public const int AddInventoryScripts = 101;
    public const int BuildGearPage = 103;       // the inventory's Gear page: the character on the left, the armor slots on the right
    public const int BuildCraftingMenu = 102;   // Turki's crafting list: rows with pictures, a scroll list, the tier tools
    // --- line ---
    // Build menu (B)
    public const int BuildBuildMenuUI = 120;
    // --- line ---
    // Build mode (while placing)
    public const int BuildBuildModeUI = 140;
    public const int AddBuildModeScripts = 141;
    // --- line ---
    // HUD (bars, crosshair, day / time / coin counters), the "+3 Wood" messages and the chests' perk panel
    public const int BuildHUD = 160;
    public const int BuildNotifications = 161;
    public const int BuildPerkPanel = 162;      // the chests' "CHOOSE YOUR REWARD" panel (Marko's PerkSelectionUI + Tony's PerkPanelInfo)
    public const int BuildPerkRollPopup = 163;  // the new chest system's slot-machine pop-up (PerkRollUI: BUY / REROLL)
    // --- line ---
    // Game end (victory screen, death page), pause menu
    public const int BuildGameEndUI = 180;      // the victory page: a trophy, coins left / earned / spent, all stats like the Info tab (MINIMISE), MAIN MENU
    public const int UpgradeDeathScreen = 181;  // the GAME OVER page: days survived, SHOW ALL STATS (like the Info tab), MAIN MENU
    public const int BuildPauseMenu = 182;      // the Esc pause menu: key bindings, mouse sensitivity / invert, resume, main menu
    // --- line ---
    // Main title scene (title, new game, settings)
    public const int BuildTitleScene = 195;

    // ===== Tools > Building =====
    public const int MakeBuildingFromSelection = 200;
    public const int UpdateBuildMenuList = 201;
    public const int BuildTraps = 202;          // the Iron Trap you build with B (Traps tab): snaps shut on a monster, hurts it and holds it still

    // ===== Tools > World =====
    public const int UseGrasslandTrees = 225;   // the placeholder tree -> low-poly grassland trees (Broken Vector's pack)
    public const int MixTreesInScene = 226;     // random look, turn and size for the trees in the open scene
    public const int BuildWildAnimal = 227;     // an animal model (ithappy's deer) -> a huntable animal that runs from the player
    public const int SetUpWorldSpawning = 228;  // ObjectScatter spacing groups + forest patches, deer spawned out of the player's sight
    public const int SetUpChests = 229;         // chests stand on the floor (StandOnGround) and glow yellow when you can open them (InteractOutline)
    public const int SetUpSkyLighting = 230;    // saves the scene's light from the sky, so shadows aren't pitch black when the title screen opens it
    public const int UseLowPolyRocks = 231;     // the placeholder cube rocks -> low-poly rocks (Broken Vector's pack) that break into pieces
    public const int BuildRockCaves = 232;      // walk-in caves made of low-poly rocks, with spots for ores (for the new world)
    public const int BuildNewWorldScene = 233;  // ProceduralGenerateScene: GameScene's game with a new random world every game
    public const int AddWorldBarrier = 234;     // a glowing barrier around the open scene's map (the old world; the new world is endless, no barrier)
    public const int BuildDesertContent = 235;  // the new world's desert: gold ore, hard sandstone rock, sandstone caves (and the turtle, a night monster)
    public const int BuildLootChests = 236;     // common / rare / epic loot boxes: prices, new perks, where they spawn
    public const int BuildSnowContent = 237;    // the new world's snow: diamond ore, frosty rocks, icy caves, snowy pines, diamond tools
    public const int FixGrassInBuilds = 239;    // the new world's grass drawn right in a built game (Unity 2023.1 bug with terrain made while playing)
    public const int BuildBerryBushes = 238;    // Turki's berry bush -> a Yughues bush with red berries you pick with E (they grow back)

    // ===== Tools > Player =====
    public const int AddCharacterModel = 250;   // the animated character on the Player's ball
    public const int AddFirstPersonHands = 251; // Minecraft-style hands on the camera for first person
    public const int SetUpPlayerAnimations = 252; // Mixamo clips, both controllers' timing, first-person arms, PunchCombo

    // ===== Tools > Enemies =====
    public const int BuildMonsterEnemy = 270;   // a monster model -> an animated enemy prefab (e.g. SlimeEnemy)
    public const int SetUpNightMonsters = 271;  // the turtle as night 2's new monster (its shell), spawning out of sight
    public const int BuildHumanEnemies = 272;   // the new world's human enemies (Polytope bodies, random gear by tier) and their spawner

    // ===== Tools > Effects =====
    public const int BuildPunchWindBurst = 285; // the grey wind burst where a bare-handed punch hits something with health
    public const int BuildDeathParticles = 286; // creatures die into particles: harmless green, harmful red
    public const int BuildTreeFall = 287;       // a chopped tree falls over and breaks into brown wood chips
    public const int BuildRockBreak = 288;      // a broken rock bursts into chunks of its own stone, gravel and dust

    // ===== Tools > Items =====
    public const int MakeItemFromSelection = 300;
    public const int BuildTierTools = 301;      // 15 tools (pickaxe / axe / sword x Wood ... Diamond) from the Low Poly Tool Pack
    public const int RebuildToolModels = 302;   // only the 15 tool prefabs again (handle length, materials); Item Data untouched
    public const int UpdateToolRecipes = 303;   // only the tools' recipes again (the crafting chain Wooden > Stone > Iron)
    public const int BuildPlateArmor = 304;     // Blink's plate armor as 6 items to craft and wear (+ the Gear page)
    public const int BuildBowAndArrows = 305;   // GameLair's bow as the Wooden Bow, 5 tiers of arrows, the flying arrow, the draw ring
    // --- line ---
    public const int ConvertTaggedObjectsToItems = 320;   // one-time move from tags, rarely needed

    // ===== Tools > Script Map =====
    public const int ScriptMapUpdateNow = 900;
    public const int ScriptMapAutoUpdate = 901;
}
