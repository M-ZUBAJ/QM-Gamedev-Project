/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Snow Content (Tools > World > Build New World Scene runs it too, and puts it all
              in the new world). Needs the rock shapes (Tools > World > Use Low Poly Rocks), the low-poly trees (Tools >
              World > Use Low Poly Grassland Trees) and the tier tools (Tools > Items > Build Tier Tools). Run it again
              any time: everything is refilled in place; the Diamond item, its prefab and the two rock materials are
              only made if missing, so changes you make to them stay.
              Guide: Docs/Snow Setup.md
Purpose: Everything the new world's snow needs, in one click:
           - Diamond: the item (Assets/Config/Items/Diamond, a raw material; Mined With = Gold, so its rock needs a Gold
             Pickaxe or better) and its prefab (Assets/Prefabs/Item/Diamond: a small ice-blue cube, copied from Turki's
             Iron like the desert's Gold);
           - DiamondNode (Assets/Prefabs/ResourceNodes/Rocks/DiamondNode): a Prefab Variant of Turki's IronNode in shiny
             blue-white, harder (50 health), dropping 1 - 3 Diamonds;
           - StoneNode Snow (.../Rocks/StoneNode Snow): a Prefab Variant of his StoneNode in frosty light grey, the same
             health and Stone drops, for the snowfields;
           - their materials, Assets/Materials/Rocks/Diamond Ore.mat and Snow Rock.mat (the rock pack's grey color sheet,
             tinted), and their rock-break dust in their own colors (Tools > Effects > Build Rock Break runs again);
           - three snow caves of frosty rock (Assets/Prefabs/World/Caves/Snow Cave Small, Snow Cave, Snow Cave Big, from
             RockCaveBuilder): the World Generator puts diamonds and stone in them;
           - four snowy conifers, Prefab Variants of Turki's TreeNode in Assets/Prefabs/ResourceNodes/Trees (TreeNode Snow
             Pine, Tall Snow Pine, Snow Spruce, Tall Snow Spruce: the tree pack's pines and spruces in its winter "Cold"
             colors; they give Wood like every tree), and their falling (Tools > Effects > Build Tree Fall runs again: it
             closes the bottom of their trunks);
           - the tool recipes again (like Tools > Items > Update Tool Recipes, without asking): with the Diamond item the
             Diamond Pickaxe, Axe and Sword get theirs (the Gold one + 3 Plank + 6 Diamond, the sword 9) and join Turki's
             crafting list.
         Diamonds also make the diamond armor (Tools > Items > Build Plate Armor, which makes the Diamond item itself
         with MakeDiamond when it's missing).
Requirement: SnowBuilder.cs x-> DesertBuilder.cs, RockCaveBuilder.cs, GrasslandTreeBuilder.cs, TreeFallBuilder.cs, RockBreakBuilder.cs, TierToolBuilder.cs, ItemData.cs, ToolStats.cs, UIBuildHelper.cs, MenuOrder.cs
             SnowBuilder.cs <-x ArmorBuilder.cs, NewWorldBuilder.cs
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UIBuildHelper;    // FindPrefabPath

public static class SnowBuilder
{
    // What the tool made (Build New World Scene puts it in the new world). Anything missing is null / empty.
    public class Parts
    {
        public ItemData diamond;
        public GameObject diamondOre, snowStone;
        public List<GameObject> caves = new List<GameObject>();
        public List<GameObject> trees = new List<GameObject>();
        public string report;
    }

    // ===== Fields =====
    const string ToolName = "Build Snow Content";
    const string StonePath = "Assets/Prefabs/ResourceNodes/StoneNode.prefab";   // its material is the rock pack's grey color sheet

    static readonly Color DiamondCube = new Color(0.55f, 0.95f, 1f);       // the Diamond item's cube: ice blue
    static readonly Color DiamondTint = new Color(0.72f, 1f, 1.25f);       // the grey color sheet -> shiny blue-white
    static readonly Color SnowRockTint = new Color(1.12f, 1.18f, 1.28f);   // the grey color sheet -> frosty light grey (over 1 = lighter)

    const float DiamondHealth = 50f;            // Turki's rocks have 30, the desert's gold 40
    const int DiamondMin = 1, DiamondMax = 3;   // diamonds per rock: fewer than gold (2 - 4), they're the best

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build Snow Content", false, MenuOrder.BuildSnowContent)]
    static void FromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        Parts parts = BuildAll();
        Debug.Log(ToolName + ": " + parts.report + ". Build New World Scene puts them in the new world.");
    }

    // ===== My methods =====

    // Builds (refills) everything. Returns what it made, with a report for the Console.
    public static Parts BuildAll()
    {
        Parts parts = new Parts();
        List<string> report = new List<string>();

        parts.diamond = MakeDiamond(report);
        Material grey = GreySheet();
        Material diamondLook = DesertBuilder.RockMaterial("Diamond Ore", grey, DiamondTint, 0.3f, 0.85f);
        Material snowRock = DesertBuilder.RockMaterial("Snow Rock", grey, SnowRockTint, 0f, 0.35f);
        parts.diamondOre = DesertBuilder.MakeVariant("IronNode", "DiamondNode", diamondLook, parts.diamond, DiamondHealth,
                                                     DiamondMin, DiamondMax, report);
        parts.snowStone = DesertBuilder.MakeVariant("StoneNode", "StoneNode Snow", snowRock, null, 0f, 0, 0, report);   // StoneNode's health and drops
        AssetDatabase.SaveAssets();
        report.Add("rock break: " + RockBreakBuilder.Build());

        parts.caves = RockCaveBuilder.BuildSnow(snowRock, out string caves);
        report.Add(caves);
        parts.trees = GrasslandTreeBuilder.BuildSnowTrees(out string trees);
        report.Add(trees);
        if (parts.trees.Count > 0)
        {
            string fall = TreeFallBuilder.BuildAll();   // TreeFall comes from TreeNode; this closes the new trunks
            report.Add("tree fall: " + (fall ?? "the WoodChips prefab couldn't be saved"));
        }
        report.Add("tool recipes: " + TierToolBuilder.ApplyRecipes());
        AssetDatabase.SaveAssets();

        parts.report = string.Join("; ", report);
        return parts;
    }

    // The Diamond item and its prefab (a copy of Turki's Iron, made once, then kept right; Mined With = Gold).
    // Tools > Items > Build Plate Armor makes it too when it's missing, so the diamond armor gets its recipes.
    public static ItemData MakeDiamond(List<string> report) => DesertBuilder.MakeOreItem("Diamond", DiamondCube, ToolTier.Gold, report);

    // The rock pack's grey color sheet (Turki's StoneNode wears it since Tools > World > Use Low Poly Rocks).
    static Material GreySheet()
    {
        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(StonePath));
        MeshRenderer look = stone != null ? stone.GetComponentInChildren<MeshRenderer>(true) : null;
        return look != null ? look.sharedMaterial : null;
    }
}
