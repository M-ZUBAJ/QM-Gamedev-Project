/*
First Date: 21/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Snow Content (Tools > World > Build New World Scene runs it too, and puts it all
              in the new world). Needs the rock shapes (Tools > World > Use Low Poly Rocks), the low-poly trees (Tools >
              World > Use Low Poly Grassland Trees) and the tier tools (Tools > Items > Build Tier Tools). Run it again
              any time: everything is refilled in place; the Diamond and Spruce Wood items, their prefabs and the two
              rock materials are only made if missing, so changes you make to them stay.
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
           - Spruce Wood (22/11/2026): the item (Assets/Config/Items/SpruceWood, "Spruce Wood", a raw material; Mined
             With = Iron, so its trees need an Iron Axe or better) and its prefab (Assets/Prefabs/Item/SpruceWood:
             Turki's Wood log in a darker spruce brown, copied from his Wood);
           - four snowy conifers, Prefab Variants of Turki's TreeNode in Assets/Prefabs/ResourceNodes/Trees (TreeNode Snow
             Pine, Tall Snow Pine, Snow Spruce, Tall Snow Spruce: the tree pack's pines and spruces in its winter "Cold"
             colors). Since 22/11/2026 they're the snow's spruce wood: they drop Spruce Wood instead of Wood (the same
             3 - 5), take 300 damage to chop down (a grassland tree 30) and need an Iron Axe or better (Spruce Wood's
             Mined With). And their falling (Tools > Effects > Build Tree Fall runs again: it closes the bottom of their
             trunks);
           - the tool recipes again (like Tools > Items > Update Tool Recipes, without asking): with the Diamond item the
             Diamond Pickaxe, Axe and Sword get theirs (the Gold one + 3 Plank + 6 Diamond, the sword 9) and join Turki's
             crafting list.
         Diamonds also make the diamond armor (Tools > Items > Build Plate Armor, which makes the Diamond item itself
         with MakeDiamond when it's missing), and Spruce Wood the Spruce Bow (Tools > Items > Build Bow And Arrows,
         which makes the Spruce Wood item itself with MakeSpruceWood when it's missing).
Requirement: SnowBuilder.cs x-> DesertBuilder.cs, RockCaveBuilder.cs, GrasslandTreeBuilder.cs, TreeFallBuilder.cs, RockBreakBuilder.cs, TierToolBuilder.cs, ItemData.cs, ToolStats.cs, UIBuildHelper.cs, MenuOrder.cs
             SnowBuilder.cs <-x ArmorBuilder.cs, BowBuilder.cs, NewWorldBuilder.cs, TrapBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UIBuildHelper;    // FindPrefabPath

public static class SnowBuilder
{
    // What the tool made (Build New World Scene puts it in the new world). Anything missing is null / empty.
    public class Parts
    {
        public ItemData diamond;
        public ItemData spruceWood;
        public GameObject diamondOre, snowStone;
        public List<GameObject> caves = new List<GameObject>();
        public List<GameObject> trees = new List<GameObject>();
        public string report;
    }

    // ===== Fields =====
    const string ToolName = "Build Snow Content";
    const string StonePath = "Assets/Prefabs/ResourceNodes/StoneNode.prefab";   // its material is the rock pack's grey color sheet
    const string ItemFolder = "Assets/Config/Items";
    const string ItemPrefabFolder = "Assets/Prefabs/Item";
    public const string SpruceWoodId = "SpruceWood";       // the file name = the Item Id (recipes find it by this)
    const string SpruceWoodName = "Spruce Wood";           // what the player sees

    static readonly Color DiamondCube = new Color(0.55f, 0.95f, 1f);       // the Diamond item's cube: ice blue
    static readonly Color DiamondTint = new Color(0.72f, 1f, 1.25f);       // the grey color sheet -> shiny blue-white
    static readonly Color SnowRockTint = new Color(1.12f, 1.18f, 1.28f);   // the grey color sheet -> frosty light grey (over 1 = lighter)
    static readonly Color SpruceColor = new Color(0.36f, 0.24f, 0.13f);    // Spruce Wood's log: darker than Wood's (0.52, 0.33, 0.22)

    const float DiamondHealth = 50f;            // Turki's rocks have 30, the desert's gold 40
    const int DiamondMin = 1, DiamondMax = 3;   // diamonds per rock: fewer than gold (2 - 4), they're the best
    const float SpruceHealth = 300f;            // the snow's trees: 10 times a grassland tree (Turki's TreeNode: 30)
    const ToolTier SpruceAxe = ToolTier.Iron;   // the weakest axe that chops them (Spruce Wood's Mined With)

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
        parts.spruceWood = MakeSpruceWood(report);
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
        parts.trees = GrasslandTreeBuilder.BuildSnowTrees(parts.spruceWood, SpruceHealth, out string trees);
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

    // The Spruce Wood item and its prefab: copies of Turki's Wood item and prefab (a short log), made once, then kept
    // right (linked to each other, the log in spruce brown, picked up as Spruce Wood, Mined With = Iron while it's
    // still None: ToolRules then lets only an Iron Axe or better chop a tree dropping it). Each run only changes what's
    // still wrong. The snow's trees drop it; Tools > Items > Build Bow And Arrows makes it too when it's missing (the
    // Spruce Bow's recipe needs it).
    public static ItemData MakeSpruceWood(List<string> report)
    {
        ItemData wood = FindItem("Wood");
        string prefabPath = FindPrefabPath(ItemPrefabFolder + "/" + SpruceWoodId + ".prefab");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null && wood != null && wood.Prefab != null)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(wood.Prefab), prefabPath);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        ItemData item = FindItem(SpruceWoodId);
        bool made = item == null;
        if (made)
        {
            string itemPath = ItemFolder + "/" + SpruceWoodId + ".asset";
            if (wood != null) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(wood), itemPath);
            else AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ItemData>(), itemPath);
            item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
        }
        if (item == null)
        {
            report.Add("the Spruce Wood item couldn't be made");
            return null;
        }

        SerializedObject so = new SerializedObject(item);   // the names in quotes must match ItemData's fields
        SerializedProperty link = so.FindProperty("prefab");
        if (prefab != null && (made || link.objectReferenceValue == null || link.objectReferenceValue == (wood != null ? wood.Prefab : null)))
            link.objectReferenceValue = prefab;
        if (made) so.FindProperty("displayName").stringValue = SpruceWoodName;
        SerializedProperty needs = so.FindProperty("minedWith");   // the axe its trees need (set while None, so a change by hand stays)
        bool axeSet = needs.enumValueIndex == (int)ToolTier.None;
        if (axeSet) needs.enumValueIndex = (int)SpruceAxe;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (prefab != null) DesertBuilder.PaintItemPrefab(AssetDatabase.GetAssetPath(prefab), item, SpruceColor);
        report.Add(made ? SpruceWoodName + " item made (" + AssetDatabase.GetAssetPath(item) + ", Turki's Wood in spruce brown, " +
                          "its trees need an " + SpruceAxe + " Axe or better)"
                        : SpruceWoodName + " item kept" + (axeSet ? " (its trees now need an " + SpruceAxe + " Axe or better)" : ""));
        return item;
    }

    // An Item Data by its exact file name (e.g. "Wood"), wherever it is; null if there's none.
    static ItemData FindItem(string id)
    {
        foreach (string guid in AssetDatabase.FindAssets(id + " t:ItemData", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == id) return AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }
        return null;
    }

    // The rock pack's grey color sheet (Turki's StoneNode wears it since Tools > World > Use Low Poly Rocks).
    static Material GreySheet()
    {
        GameObject stone = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(StonePath));
        MeshRenderer look = stone != null ? stone.GetComponentInChildren<MeshRenderer>(true) : null;
        return look != null ? look.sharedMaterial : null;
    }
}
