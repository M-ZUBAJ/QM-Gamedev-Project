/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Open GameScene, then menu Tools > Items > Build Plate Armor (it asks first), and save the scene afterwards.
              Run it again any time: the items, their prefabs and the Gear page are refilled in place, and recipes you
              changed stay. Guide: Docs/Armor Setup.md
Purpose: Blink's plate armor (the PlateSet1 parts already inside the player's character, switched off) as four
         armor sets, 24 items the player can craft, wear and see, in one click. Copper comes first, Iron is its upgrade,
         Gold Iron's and Diamond Gold's. How much a whole set protects follows Tony's formula a[n] = 5/2 n^2 - 5/2 n + 10
         percent for the n-th set (copper 10 %, iron 15 %, gold 25 %, diamond 40 %), and each set splits it like
         copper: the chestplate a quarter, every other piece 15 % (SetTotal, SlotShares):
           - Copper: Assets/Config/Items/Armor/Copper<Slot>.asset: Copper Helmet, Copper Shoulders, Copper Chestplate,
             Copper Gloves, Copper Leggings, Copper Boots. Protection 1.5 % each and 2.5 % for the chestplate (10 % off
             every hit for all six); all six: +1 coin for every monster killed; a recipe of Copper (5 / 4 / 8 / 3 / 7 / 4);
             copper colored;
           - Iron: Iron<Slot>.asset ("Iron Helmet"...): 2.25 % each, 3.75 % for the chestplate (15 % for all six; 16 %
             until 21/11/2026); all six: +2 coins for every monster killed, and its pieces count as Copper (a mix of both
             still gives the Copper bonus); each crafted from its Copper piece + Iron (4 / 3 / 6 / 2 / 5 / 3); steel grey;
           - Gold: Gold<Slot>.asset ("Gold Helmet"...): 3.75 % each, 6.25 % for the chestplate (25 % for all six); all
             six: +4 coins for every monster killed and 10 % off every chest, and its pieces count as Iron; each crafted
             from its Iron piece + Gold (4 / 3 / 6 / 2 / 5 / 3); bright gold with red leather and crimson cloth;
           - Diamond (21/11/2026): Diamond<Slot>.asset ("Diamond Helmet"...): 6 % each, 10 % for the chestplate (40 % for
             all six); all six: +8 coins for every monster killed and 10 % off every chest, and its pieces count as Gold;
             each crafted from its Gold piece + Diamond (4 / 3 / 6 / 2 / 5 / 3); ice blue with sapphire trims, dark
             slate leather and silver cloth, glossy;
           - the Gold and Diamond items (from Tools > World > Build Desert / Snow Content) are made here too when one is
             missing, the same way those tools make them (a copy of Turki's Iron), so every set gets its recipes;
           - files from before the tiers were swapped (20/11/2026: Iron came first, and Reinforced Iron = an Iron piece
             + Copper) are renamed by tier: Iron... -> Copper..., ReinforcedIron... -> Iron..., with Iron and Copper
             swapped in their recipes, so everything linked to them stays linked;
           - every item: Item Type Armor (the slot, Protection, Set, the character's part to show and what it hides,
             Look = its set's material), a photo icon, made at the Crafting page; a recipe only for a new item or one
             without a recipe (so a recipe you changed stays);
           - each set's look: Assets/Materials/Armor/<Set> Armor.mat with <Set> Armor Palette.png = Blink's palette
             picture with the four strips the plate parts use repainted in the set's colors (plates, trims, leather
             straps, cloth; their light-to-dark shading kept) and a little metal shine. Made only if missing: change the
             colors by hand, or delete the two files to get the tool's colors again;
           - Assets/Prefabs/Item/Armor/<Id>.prefab (+ Meshes/Iron<Slot>.asset, shared by every set): the piece frozen
             in the character's pose, in its set's colors, for its icon and for when it's dropped;
           - ArmorLook on Assets/Prefabs/Player/PlayerCharacter, so the body, the first-person hands and the Gear page's
             preview show the worn pieces, with the skin under the clothes (so nothing flickers): Blink's rows (the
             skin each piece covers from every side, switched off) and trimmed copies of the skin a piece covers only
             partly (Assets/Prefabs/Player/SkinUnderClothes: the arms under the chestplate and the hands in the gloves,
             without the faces under them; for the body and for the first-person hands);
           - PlayerArmor on the Player (the worn armor: it takes its share off every hit) with every set's bonus, and
             the armor in the Player's ArmorTestKit (GameRules > Armor Test gives it; it's taken out of the Tool
             Test kit, which had it before);
           - the armor in Turki's crafting list, after the tools, set by set, each set in one panel (Tools > UI > Build
             Crafting Menu's sorting and set panels);
           - Tools > UI > Build Gear Page: the inventory's Gear page (the character on the left, the slots on the right);
           - the Armor row on the inventory's Info tab (how much the armor protects, the set bonus in gold).
Requirement: ArmorBuilder.cs x-> ItemData.cs, ArmorStats.cs, IconSource.cs, ItemPickup.cs, ArmorLook.cs, FirstPersonHands.cs, PlayerArmor.cs, PlayerInventory.cs, ArmorTestKit.cs, ToolTestKit.cs, MeshAssetHelper.cs, CraftingMenuBuilder.cs, GearPageBuilder.cs, InventoryUIBuilder.cs, DesertBuilder.cs, SnowBuilder.cs, UIBuildHelper.cs, MenuOrder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders, CheckTMP, AddToPlayer

public static class ArmorBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Plate Armor";
    const string ItemFolder = "Assets/Config/Items/Armor";
    const string PrefabFolder = "Assets/Prefabs/Item/Armor";
    const string MeshFolder = PrefabFolder + "/Meshes";
    const string CharacterPath = "Assets/Prefabs/Player/PlayerCharacter.prefab";
    const string HandsPath = "Assets/Prefabs/Player/FirstPersonHands.prefab";   // the first-person hands (found by name if moved)
    const string SkinFolder = "Assets/Prefabs/Player/SkinUnderClothes";         // the trimmed skin copies
    const string BodyName = "Body";   // Blink's skin parts are under Mesh > Body (Arms, Chest, Feet, Hands, Head, Legs, Neck)
    const string ModelName = "Model";

    // The skin under the clothes, as shares of the character's height (0.0006 = about 1 mm on a 1.8 m body). Skin lies
    // ON a piece where its corners are within OnSkin of the piece's surface: two surfaces that close flicker. A skin
    // part gets a trimmed copy when at least MinOnShare (1 %) of it lies on the piece, and the copy leaves out every
    // face within CutSkin (about 3 mm, so also where an animation moves the two a little apart).
    const float OnSkin = 0.0006f;
    const float CutSkin = 0.0017f;
    const float MinOnShare = 0.01f;

    // One slot's piece of Blink's plate armor (both sets use the same parts): where it's worn, the character's part it
    // shows and what it hides on the character while worn.
    struct Slot
    {
        public ArmorSlot slot;
        public string part;
        public string[] hides;
    }

    static readonly Slot[] Slots =
    {
        new Slot { slot = ArmorSlot.Helmet,    part = "PlateSet1_Helmet",    hides = new[] { "Hair" } },
        new Slot { slot = ArmorSlot.Shoulders, part = "PlateSet1_Shoulders", hides = new string[0] },
        new Slot { slot = ArmorSlot.Chest,     part = "PlateSet1_Chest",     hides = new[] { "Starter_Chest" } },
        new Slot { slot = ArmorSlot.Gloves,    part = "PlateSet1_Gloves",    hides = new string[0] },
        new Slot { slot = ArmorSlot.Legs,      part = "PlateSet1_Pants",     hides = new[] { "Starter_Pants" } },
        new Slot { slot = ArmorSlot.Boots,     part = "PlateSet1_Boots",     hides = new[] { "Starter_Boots" } },
    };

    // A strip of Blink's palette picture (8 pixels wide, half the picture high; each part of a model takes its colors
    // from one) and the colors it gets: its darkest row becomes Dark, its lightest Light, the rows between in between.
    struct Strip
    {
        public int x;          // its left edge, in pixels of the 256 x 256 picture
        public bool top;       // in the picture's top half (else the bottom half)
        public Color dark, light;
    }

    // The four strips the plate parts take their colors from (measured from their corners' texture spots, 20/11/2026):
    // the plates (bottom half), the darker trims (bottom), the leather straps (top) and the cloth under the chestplate
    // (bottom). They mostly use the middle of each strip.
    const int PlatesX = 64, TrimsX = 104, StrapsX = 96, ClothX = 120;

    // Ready-made colors for a set: the plates, trims, leather straps and cloth (each strip's darkest and lightest color).
    // Declared before Sets, which uses them (Unity fills these in from top to bottom).
    static readonly Strip[] SteelColors =
    {
        new Strip { x = PlatesX, dark = new Color(0.25f, 0.27f, 0.3f), light = new Color(0.92f, 0.94f, 0.97f) },             // steel grey
        new Strip { x = TrimsX, dark = new Color(0.1f, 0.1f, 0.11f), light = new Color(0.55f, 0.56f, 0.58f) },              // dark iron
        new Strip { x = StrapsX, top = true, dark = new Color(0.12f, 0.07f, 0.04f), light = new Color(0.75f, 0.55f, 0.38f) }, // brown leather
        new Strip { x = ClothX, dark = new Color(0.03f, 0.04f, 0.1f), light = new Color(0.35f, 0.45f, 0.85f) },             // navy cloth
    };

    static readonly Strip[] CopperColors =
    {
        new Strip { x = PlatesX, dark = new Color(0.3f, 0.12f, 0.05f), light = new Color(1f, 0.72f, 0.45f) },               // copper
        new Strip { x = TrimsX, dark = new Color(0.12f, 0.07f, 0.04f), light = new Color(0.7f, 0.45f, 0.25f) },             // dark bronze
        new Strip { x = StrapsX, top = true, dark = new Color(0.08f, 0.05f, 0.03f), light = new Color(0.55f, 0.38f, 0.25f) }, // dark leather
        new Strip { x = ClothX, dark = new Color(0.02f, 0.08f, 0.07f), light = new Color(0.25f, 0.85f, 0.72f) },            // teal (copper's patina)
    };

    static readonly Strip[] GoldColors =
    {
        new Strip { x = PlatesX, dark = new Color(0.38f, 0.24f, 0.03f), light = new Color(1f, 0.93f, 0.55f) },              // bright gold
        new Strip { x = TrimsX, dark = new Color(0.16f, 0.1f, 0.02f), light = new Color(0.72f, 0.52f, 0.15f) },             // dark gold
        new Strip { x = StrapsX, top = true, dark = new Color(0.12f, 0.03f, 0.02f), light = new Color(0.7f, 0.25f, 0.18f) },  // red leather
        new Strip { x = ClothX, dark = new Color(0.12f, 0.01f, 0.03f), light = new Color(0.85f, 0.12f, 0.2f) },             // crimson cloth
    };

    static readonly Strip[] DiamondColors =
    {
        new Strip { x = PlatesX, dark = new Color(0.1f, 0.3f, 0.4f), light = new Color(0.8f, 0.98f, 1f) },                  // ice blue
        new Strip { x = TrimsX, dark = new Color(0.03f, 0.08f, 0.18f), light = new Color(0.28f, 0.52f, 0.85f) },            // sapphire
        new Strip { x = StrapsX, top = true, dark = new Color(0.05f, 0.05f, 0.07f), light = new Color(0.38f, 0.4f, 0.48f) },  // dark slate leather
        new Strip { x = ClothX, dark = new Color(0.12f, 0.14f, 0.18f), light = new Color(0.92f, 0.95f, 1f) },               // silver cloth
    };

    // How much a whole set protects, in percent (Tony, 21/11/2026): a[n] = 5/2 x n^2 - 5/2 x n + 10 for the n-th set,
    // so each set adds 5 % more than the step before it: copper (n = 1) 10 %, iron (2) 15 %, gold (3) 25 %, diamond
    // (4) 40 %.
    static float SetTotal(int n) => 2.5f * n * n - 2.5f * n + 10f;

    // Each slot's share of its set's protection, as copper's first numbers had it (1.5 % a piece and 2.5 % for the
    // chestplate, of 10 %): the chestplate a quarter, every other piece 15 %. Helmet, Shoulders, Chest, Gloves, Legs,
    // Boots. Declared before Sets, which uses it (Unity fills these in from top to bottom).
    static readonly float[] SlotShares = { 0.15f, 0.15f, 0.25f, 0.15f, 0.15f, 0.15f };

    // A set's protection spread over its six pieces, each rounded to a hundredth: Spread(15) = 2.25, 2.25, 3.75, 2.25,
    // 2.25, 2.25.
    static float[] Spread(float total)
    {
        float[] pieces = new float[SlotShares.Length];
        for (int i = 0; i < pieces.Length; i++) pieces[i] = Mathf.Round(total * SlotShares[i] * 100f) / 100f;
        return pieces;
    }

    // One armor set: its name (Armor > Set and PlayerArmor's Set Bonuses), the start of its pieces' Item Ids, each
    // slot's Protection (percent) and cost, what the cost is (an item's Id), the set whose piece for the same slot each
    // recipe also uses up (an upgrade), its colors and shine. Build = false: only its look (material and palette
    // picture) is made, no items yet, so its colors can be seen and changed before the set is added.
    class ArmorSet
    {
        public string name, idStart, costItem, upgradeOf;
        public bool build = true;
        public float[] protection;                      // Helmet, Shoulders, Chest, Gloves, Legs, Boots
        public int[] cost;                              // the same order
        public Strip[] palette;
        public float metallic, smoothness;
    }

    // Weakest first: an upgrade's recipe needs the set it upgrades to be made before it.
    static readonly ArmorSet[] Sets =
    {
        new ArmorSet
        {
            name = "Copper", idStart = "Copper", costItem = "Copper",
            protection = Spread(SetTotal(1)),                             // 10 % for all six: 1.5 each, 2.5 for the chestplate
            cost = new[] { 5, 4, 8, 3, 7, 4 },
            palette = CopperColors, metallic = 0.6f, smoothness = 0.55f,
        },
        new ArmorSet
        {
            name = "Iron", idStart = "Iron", costItem = "Iron", upgradeOf = "Copper",
            protection = Spread(SetTotal(2)),                             // 15 %: 2.25 each, 3.75 for the chestplate (was 16 %)
            cost = new[] { 4, 3, 6, 2, 5, 3 },                            // Iron, on top of the Copper piece
            palette = SteelColors, metallic = 0.5f, smoothness = 0.5f,
        },
        // Gold (made since 21/11/2026; the Gold item comes from Tools > World > Build Desert Content): its bonus is in
        // PlayerArmor's DefaultBonuses (+4 coins per kill, 10 % off every chest, counts as Iron).
        new ArmorSet
        {
            name = "Gold", idStart = "Gold", costItem = "Gold", upgradeOf = "Iron",
            protection = Spread(SetTotal(3)),                             // 25 %: 3.75 each, 6.25 for the chestplate
            cost = new[] { 4, 3, 6, 2, 5, 3 },                            // Gold, on top of the Iron piece
            palette = GoldColors, metallic = 0.75f, smoothness = 0.7f,
        },
        // Diamond (21/11/2026, Tony: "a diamond armor, +8 coins set bonus and -10 % discount"; the Diamond item comes
        // from the snow caves, Tools > World > Build Snow Content): its bonus is in PlayerArmor's DefaultBonuses (+8
        // coins per kill, 10 % off every chest, counts as Gold).
        new ArmorSet
        {
            name = "Diamond", idStart = "Diamond", costItem = "Diamond", upgradeOf = "Gold",
            protection = Spread(SetTotal(4)),                             // 40 %: 6 each, 10 for the chestplate
            cost = new[] { 4, 3, 6, 2, 5, 3 },                            // Diamond, on top of the Gold piece
            palette = DiamondColors, metallic = 0.35f, smoothness = 0.85f, // less metal, more gloss: a gem's shine
        },
    };

    const string LookFolder = "Assets/Materials/Armor";   // each set's material and palette picture

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/Items/Build Plate Armor", false, MenuOrder.BuildPlateArmor)]
    static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        if (!CheckTMP(ToolName)) return;

        string characterPath = FindPrefabPath(CharacterPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(characterPath) == null)
        {
            EditorUtility.DisplayDialog(ToolName, "There's no PlayerCharacter prefab yet. Run Tools > Player > Add Character Model first.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog(ToolName,
                "This makes Blink's plate armor wearable, as four sets of 6 pieces (helmet, shoulders, chestplate, gloves, " +
                "leggings, boots) in " + ItemFolder + ":\n\n" +
                "- Copper (copper colored): 1.5 % protection each, 2.5 % for the chestplate (10 % for all six); all six = " +
                "+1 coin for every monster killed; recipes of Copper\n" +
                "- Iron (steel grey): 15 % for all six; all six = +2 coins for every monster killed, and it counts as " +
                "Copper; each piece = its Copper piece + Iron (a worn Copper piece can be upgraded where it's worn)\n" +
                "- Gold (gold): 25 % for all six; all six = +4 coins for every monster killed and 10 % off every chest, and " +
                "it counts as Iron; each piece = its Iron piece + Gold\n" +
                "- Diamond (ice blue): 40 % for all six; all six = +8 coins for every monster killed and 10 % off every chest, " +
                "and it counts as Gold; each piece = its Gold piece + Diamond (the Diamond item is made too if missing)\n" +
                "- armor files from when Iron came first (and Reinforced Iron) renamed by tier: Iron to Copper, " +
                "Reinforced Iron to Iron\n" +
                "- each set's colors in " + LookFolder + " (only if missing)\n" +
                "- ArmorLook on the PlayerCharacter prefab, PlayerArmor on the Player\n" +
                "- the skin under the clothes hidden or trimmed, so the shoulders, gloves and boots don't flicker\n" +
                "- the armor in Turki's crafting list (each set in one panel) and in the Armor Test kit (GameRules > Armor Test)\n" +
                "- the inventory's Gear page, built again: the character on the left, the armor slots on the right\n" +
                "- the Armor row on the inventory's Info tab (the set bonus in gold)\n\n" +
                "Open GameScene first, and save it afterwards. Continue?", "Build", "Cancel"))
            return;

        int renamed = RenameOldFiles();   // the old Iron... -> Copper..., ReinforcedIron... -> Iron...
        List<ItemData> items = new List<ItemData>();
        List<string> missing = new List<string>();
        List<string> noCost = new List<string>();
        List<string> looksMade = new List<string>();
        List<string> oresMade = new List<string>();
        List<ArmorLook.SkinUnder> skin;
        List<string> trimmed = new List<string>();
        float height;
        GameObject source = PrefabUtility.LoadPrefabContents(characterPath);   // to freeze each piece from (never saved)
        try
        {
            Dictionary<string, Material> looks = MakeLooks(source, looksMade);
            Dictionary<ArmorSlot, Mesh> frozen = new Dictionary<ArmorSlot, Mesh>();   // each slot's piece, frozen once for both sets
            Dictionary<string, ItemData> made = new Dictionary<string, ItemData>();   // by Item Id, for the upgrade recipes
            foreach (ArmorSet set in Sets)
            {
                if (!set.build) continue;   // only its look for now (MakeLooks)
                ItemData cost = CostItem(set, oresMade);
                if (cost == null) noCost.Add(set.name + " (no " + set.costItem + " item)");
                looks.TryGetValue(set.name, out Material setLook);
                for (int s = 0; s < Slots.Length; s++)
                {
                    Slot slot = Slots[s];
                    SkinnedMeshRenderer part = FindPart(source, slot.part);
                    if (part == null)
                    {
                        if (!missing.Contains(slot.part)) missing.Add(slot.part);
                        continue;
                    }
                    if (!frozen.TryGetValue(slot.slot, out Mesh mesh)) frozen[slot.slot] = mesh = FreezeMesh(MeshName(slot), part);

                    string id = IdOf(set, slot);
                    ItemData item = FindOrMakeItem(id);
                    GameObject prefab = BuildPrefab(id, mesh, setLook != null ? new[] { setLook } : part.sharedMaterials, item);
                    ItemData lower = null;
                    if (set.upgradeOf != null) made.TryGetValue(IdOf(SetNamed(set.upgradeOf), slot), out lower);
                    FillItem(item, set, s, prefab, setLook, cost, lower);
                    made[id] = item;
                    items.Add(item);
                }
            }
            skin = SkinRows(source);
            height = AddTrims(source, skin, 0f, trimmed);   // the body's skin
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
        }
        AddFirstPersonTrims(skin, height, trimmed);           // the first-person hands' own arms and hands
        skin.RemoveAll(row => (row.hides == null || row.hides.Length == 0) && row.trims.Count == 0);   // rows that do nothing
        AssetDatabase.SaveAssets();

        string look = SetUpArmorLook(characterPath, skin);
        string player = SetUpPlayer(items);
        string crafting = CraftingMenuBuilder.SortList();
        string gear = GearPageBuilder.BuildPage();
        string info = InventoryUIBuilder.AddArmorRowToCanvas();
        AssetDatabase.SaveAssets();

        Debug.Log(ToolName + ": " + items.Count + " armor items (Copper, Iron, Gold, Diamond) in " + ItemFolder +
                  (oresMade.Count > 0 ? " (recipe items: " + string.Join(", ", oresMade) + ")" : "") +
                  (renamed > 0 ? " (" + renamed + " old pieces renamed by tier: Iron to Copper, Reinforced Iron to Iron, " +
                                 "with Iron and Copper swapped in their recipes)" : "") +
                  (missing.Count > 0 ? " (not found on the character: " + string.Join(", ", missing) + ")" : "") +
                  (noCost.Count > 0 ? " (no recipes for " + string.Join(", ", noCost) + ")" : "") +
                  "; set colors: " + (looksMade.Count > 0 ? string.Join(", ", looksMade) : "kept as they were") +
                  "; " + look + " (skin under the clothes: " +
                  (trimmed.Count > 0 ? "trimmed copies " + string.Join(", ", trimmed) : "no trimmed copies needed") + ");" +
                  player + " Crafting: " + crafting + " Gear page: " + gear + " Info tab: " + info + "." +
                  " Save the scene, then Play: craft or get the armor (GameRules > Armor Test), Tab > Gear.");
        string clashes = Clashes(skin);
        if (clashes != null) Debug.LogWarning(ToolName + ": " + clashes);
    }

    // ===== My methods =====

    // The tiers were swapped (20/11/2026, Tony: iron is the higher tier). Before, Iron came first and Reinforced Iron
    // was its upgrade (an Iron piece + Copper); now Copper comes first and Iron is its upgrade (a Copper piece + Iron).
    // Each old file keeps its tier and gets the new name, and a rename keeps everything that links to it (Turki's
    // crafting list, the upgrade recipes, the prefabs):
    //   - an old first-tier piece Iron<Slot> (its recipe uses up no armor piece) becomes Copper<Slot>, prefab too;
    //   - an old upgrade ReinforcedIron<Slot> becomes Iron<Slot>, prefab too (its recipe's piece is the Copper one now);
    //   - each renamed piece's recipe gets Iron and Copper swapped (amounts and anything you changed stay);
    //   - Reinforced Iron Armor.mat and its palette picture (copper colors) become Copper Armor.
    // Nothing happens once they're renamed, or where a file with the new name is there already. Returns how many
    // pieces were renamed.
    static int RenameOldFiles()
    {
        ItemData iron = FindItem("Iron"), copper = FindItem("Copper");   // the materials the recipes swap
        int renamed = 0;
        foreach (Slot slot in Slots)
        {
            string name = ArmorStats.SlotName(slot.slot);
            ItemData oldFirst = FindItem("Iron" + name);
            if (oldFirst != null && oldFirst.UpgradedFrom == null && RenamePiece(oldFirst, "Iron" + name, "Copper" + name))
            {
                SwapInRecipe(oldFirst, iron, copper);
                renamed++;
            }
            ItemData oldUpgrade = FindItem("ReinforcedIron" + name);
            if (oldUpgrade != null && RenamePiece(oldUpgrade, "ReinforcedIron" + name, "Iron" + name))
            {
                SwapInRecipe(oldUpgrade, iron, copper);
                renamed++;
            }
        }
        Rename(LookFolder + "/Reinforced Iron Armor.mat", "Copper Armor");
        Rename(LookFolder + "/Reinforced Iron Armor Palette.png", "Copper Armor Palette");
        if (renamed > 0) AssetDatabase.SaveAssets();
        return renamed;
    }

    // Renames a piece's Item Data and its prefab (Assets/Prefabs/Item/Armor/<oldId>) to newId, only while no Item Data
    // has that name. True = renamed.
    static bool RenamePiece(ItemData item, string oldId, string newId)
    {
        if (FindItem(newId) != null) return false;   // the new one is there already: both stay
        if (Rename(AssetDatabase.GetAssetPath(item), newId) == 0) return false;
        Rename(FindPrefabPath(PrefabFolder + "/" + oldId + ".prefab"), newId);
        return true;
    }

    // Iron <-> Copper in a piece's recipe (the materials swap with the tiers); amounts and other lines stay.
    static void SwapInRecipe(ItemData item, ItemData iron, ItemData copper)
    {
        if (iron == null || copper == null) return;
        SerializedObject so = new SerializedObject(item);
        SerializedProperty recipe = so.FindProperty("recipe");   // must match ItemData's field
        for (int i = 0; i < recipe.arraySize; i++)
        {
            SerializedProperty what = recipe.GetArrayElementAtIndex(i).FindPropertyRelative("item");
            if (what.objectReferenceValue == iron) what.objectReferenceValue = copper;
            else if (what.objectReferenceValue == copper) what.objectReferenceValue = iron;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    // A piece's Item Id: the set's start + the slot's name (CopperHelmet, IronChestplate, CopperLeggings).
    static string IdOf(ArmorSet set, Slot slot) => set.idStart + ArmorStats.SlotName(slot.slot);

    // The frozen piece's file name, shared by every set (they use the same part). It keeps the name it got when Iron
    // was the first set (IronHelmet), so changing the order of Sets makes no new files.
    static string MeshName(Slot slot) => "Iron" + ArmorStats.SlotName(slot.slot);

    static ArmorSet SetNamed(string name) => System.Array.Find(Sets, set => set.name == name);

    // Renames the file at "path" to "newName" (same folder and extension) if it's there and that name is free. 1 = done.
    static int Rename(string path, string newName)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.LoadMainAssetAtPath(path) == null) return 0;
        string target = FolderOf(path) + "/" + newName + Path.GetExtension(path);
        if (AssetDatabase.LoadMainAssetAtPath(target) != null) return 0;   // the new file is there already: both stay
        string problem = AssetDatabase.RenameAsset(path, newName);
        if (string.IsNullOrEmpty(problem)) return 1;
        Debug.LogWarning(ToolName + ": couldn't rename " + path + " to " + newName + ": " + problem);
        return 0;
    }

    // The character's part with this name (e.g. PlateSet1_Helmet), switched on or off, or null.
    static SkinnedMeshRenderer FindPart(GameObject character, string partName)
    {
        foreach (SkinnedMeshRenderer part in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (part.name == partName) return part;
        }
        return null;
    }

    // The item a set's recipe costs (e.g. Diamond). Gold and Diamond come from the desert's and the snow's tools; when
    // one of them is missing it's made here the same way (a copy of Turki's Iron), so the set gets its recipes whichever
    // tool runs first. "made" gets what those tools reported.
    static ItemData CostItem(ArmorSet set, List<string> made)
    {
        ItemData cost = FindItem(set.costItem);
        if (cost != null) return cost;
        if (set.costItem == "Gold") return DesertBuilder.MakeGold(made);
        if (set.costItem == "Diamond") return SnowBuilder.MakeDiamond(made);
        return null;
    }

    static ItemData FindItem(string id)
    {
        foreach (string guid in AssetDatabase.FindAssets(id + " t:ItemData", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == id) return AssetDatabase.LoadAssetAtPath<ItemData>(path);
        }
        return null;
    }

    static ItemData FindOrMakeItem(string id)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemFolder + "/" + id + ".asset");
        if (item == null) item = FindItem(id);   // moved somewhere else
        if (item != null) return item;

        MakeFolders(ItemFolder);
        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, ItemFolder + "/" + id + ".asset");
        return item;
    }

    // The piece's item prefab, emptied and refilled IN PLACE if it exists: a box to aim at (F picks it up), ItemPickup,
    // and Model = the frozen piece in its set's material (so its icon and the dropped piece have the set's colors).
    static GameObject BuildPrefab(string id, Mesh mesh, Material[] materials, ItemData item)
    {
        if (mesh == null) return null;

        string path = FindPrefabPath(PrefabFolder + "/" + id + ".prefab");
        MakeFolders(FolderOf(path));
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(id);
        try
        {
            if (existed) Empty(root);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            GameObject model = new GameObject(ModelName);
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterials = materials;

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;

            SerializedObject so = new SerializedObject(root.AddComponent<ItemPickup>());   // the names in quotes must match ItemPickup's fields
            so.FindProperty("data").objectReferenceValue = item;
            so.FindProperty("amount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
        }
    }

    // The piece frozen in the character's saved pose, turned and sized as it is on the character, with its middle at
    // (0, 0, 0). Saved (refilled) as Meshes/<name>.asset.
    static Mesh FreezeMesh(string name, SkinnedMeshRenderer part)
    {
        Mesh posed = PosedMesh(part);
        MakeFolders(MeshFolder);
        Mesh saved = MeshAssetHelper.SaveCopy(posed, posed.bounds.center, 1f, MeshFolder + "/" + name + ".asset");
        Object.DestroyImmediate(posed);
        return saved;
    }

    // A new mesh of the part as it stands in the character's saved pose, in the character's space (destroy it when done).
    static Mesh PosedMesh(SkinnedMeshRenderer part)
    {
        part.gameObject.SetActive(true);   // only in an opened copy, which is never saved
        Mesh posed = new Mesh();
        part.BakeMesh(posed, true);        // true = without the part's own scale: its matrix below adds it (else it'd be twice)

        // Into the character's space: everything the part's object does (move, turn, size) relative to the character.
        Matrix4x4 toCharacter = part.transform.root.worldToLocalMatrix * part.transform.localToWorldMatrix;
        Vector3[] corners = posed.vertices;
        Vector3[] normals = posed.normals;
        Vector4[] tangents = posed.tangents;
        for (int i = 0; i < corners.Length; i++) corners[i] = toCharacter.MultiplyPoint3x4(corners[i]);
        for (int i = 0; i < normals.Length; i++) normals[i] = toCharacter.MultiplyVector(normals[i]).normalized;
        for (int i = 0; i < tangents.Length; i++)
        {
            Vector3 along = toCharacter.MultiplyVector(tangents[i]).normalized;
            tangents[i] = new Vector4(along.x, along.y, along.z, tangents[i].w);
        }
        posed.vertices = corners;
        if (normals.Length == corners.Length) posed.normals = normals;
        if (tangents.Length == corners.Length) posed.tangents = tangents;
        posed.RecalculateBounds();
        return posed;
    }

    // Removes everything from an opened prefab except its top object's Transform: children, scripts, then the rest.
    static void Empty(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (MonoBehaviour script in root.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(script);
        foreach (Component part in root.GetComponents<Component>())
        {
            if (part != null && !(part is Transform)) Object.DestroyImmediate(part);
        }
    }

    // The Item Data's settings: name ("Iron Helmet"), prefab, Pickup, a photo icon, Item Type Armor with its Armor
    // section (slot, Protection, Set, Piece, Hides, Look), and a recipe only when it has none (so a recipe you changed
    // stays): the set's cost (Copper), or for an upgrade the lower set's piece + the cost (Copper Helmet + 4 Iron).
    // No cost item (e.g. no Copper in the project) = no recipe.
    static void FillItem(ItemData item, ArmorSet set, int slotIndex, GameObject prefab, Material look, ItemData cost, ItemData lower)
    {
        Slot slot = Slots[slotIndex];
        SerializedObject so = new SerializedObject(item);   // the names in quotes must match ItemData's fields
        so.FindProperty("displayName").stringValue = set.name + " " + ArmorStats.SlotName(slot.slot);
        if (prefab != null) so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("pickup").enumValueIndex = (int)PickupType.Pickup;
        so.FindProperty("icon.mode").enumValueIndex = (int)IconMode.Capture;
        so.FindProperty("itemType").enumValueIndex = (int)ItemType.Armor;
        so.FindProperty("armor.slot").enumValueIndex = (int)slot.slot;
        so.FindProperty("armor.protection").floatValue = set.protection[slotIndex];
        so.FindProperty("armor.set").stringValue = set.name;
        so.FindProperty("armor.piece").stringValue = slot.part;
        so.FindProperty("armor.look").objectReferenceValue = look;
        SerializedProperty hides = so.FindProperty("armor.hides");
        hides.ClearArray();
        foreach (string covered in slot.hides)
        {
            hides.InsertArrayElementAtIndex(hides.arraySize);
            hides.GetArrayElementAtIndex(hides.arraySize - 1).stringValue = covered;
        }
        so.FindProperty("craftAmount").intValue = 1;
        so.FindProperty("craftedAt").enumValueIndex = (int)CraftStation.Crafting;

        SerializedProperty recipe = so.FindProperty("recipe");
        bool upgrade = set.upgradeOf != null;
        if (recipe.arraySize == 0 && cost != null && (!upgrade || lower != null))
        {
            if (upgrade) AddLine(recipe, lower, 1);   // the piece it's made from, used up
            AddLine(recipe, cost, set.cost[slotIndex]);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    static void AddLine(SerializedProperty recipe, ItemData item, int amount)
    {
        recipe.InsertArrayElementAtIndex(recipe.arraySize);
        SerializedProperty line = recipe.GetArrayElementAtIndex(recipe.arraySize - 1);
        line.FindPropertyRelative("item").objectReferenceValue = item;
        line.FindPropertyRelative("amount").intValue = amount;
    }

    // Each set's look: a copy of the plate parts' own material (Blink's Armor) with a palette picture of its own and a
    // little metal shine. Made only if missing, so colors changed by hand stay (delete the two files to get the tool's
    // colors again). "made" gets the new files. Returns each set's material by its name (empty if the character's armor
    // material or its picture can't be found: the sets then keep Blink's colors).
    static Dictionary<string, Material> MakeLooks(GameObject character, List<string> made)
    {
        Dictionary<string, Material> looks = new Dictionary<string, Material>();
        SkinnedMeshRenderer anyPart = FindPart(character, Slots[(int)ArmorSlot.Chest].part);
        Material original = anyPart != null ? anyPart.sharedMaterial : null;
        Texture2D palette = original != null ? original.mainTexture as Texture2D : null;
        string palettePath = palette != null ? AssetDatabase.GetAssetPath(palette) : null;
        if (string.IsNullOrEmpty(palettePath)) return looks;

        MakeFolders(LookFolder);
        foreach (ArmorSet set in Sets)
        {
            string materialPath = LookFolder + "/" + set.name + " Armor.mat";
            Material look = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (look == null)
            {
                Texture2D picture = MakePalette(palettePath, set, LookFolder + "/" + set.name + " Armor Palette.png");
                if (picture == null) continue;
                look = new Material(original) { name = set.name + " Armor" };
                look.mainTexture = picture;
                look.color = Color.white;
                look.SetFloat("_Metallic", set.metallic);
                look.SetFloat("_Glossiness", set.smoothness);
                AssetDatabase.CreateAsset(look, materialPath);
                made.Add(materialPath);
            }
            looks[set.name] = look;
        }
        return looks;
    }

    // A copy of Blink's palette picture (read from its file: the picture itself may not be readable) with the set's
    // strips repainted, saved as a PNG. An existing one is kept.
    static Texture2D MakePalette(string sourcePath, ArmorSet set, string path)
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        Texture2D picture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!picture.LoadImage(File.ReadAllBytes(sourcePath)))
        {
            Object.DestroyImmediate(picture);
            return null;
        }
        int width = picture.width, half = picture.height / 2;
        int stripWidth = Mathf.Max(1, width / 32);   // 8 pixels on Blink's 256 x 256 picture
        Color32[] pixels = picture.GetPixels32();    // row 0 = the picture's BOTTOM row
        foreach (Strip strip in set.palette)
        {
            Repaint(pixels, width, strip.x * width / 256, stripWidth, strip.top ? half : 0, half, strip);
        }
        picture.SetPixels32(pixels);
        File.WriteAllBytes(path, picture.EncodeToPNG());
        Object.DestroyImmediate(picture);

        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;   // the exact colors
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;                                            // like Blink's own picture
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // One strip in new colors: each pixel by how light it is within the strip (its darkest = Dark, its lightest = Light),
    // so the strip keeps its light-to-dark shading in the new color.
    static void Repaint(Color32[] pixels, int width, int x0, int stripWidth, int y0, int rows, Strip strip)
    {
        float low = float.MaxValue, high = float.MinValue;
        for (int y = y0; y < y0 + rows; y++)
        {
            float light = Light(pixels[y * width + x0 + stripWidth / 2]);
            low = Mathf.Min(low, light);
            high = Mathf.Max(high, light);
        }
        for (int y = y0; y < y0 + rows; y++)
        {
            for (int x = x0; x < x0 + stripWidth; x++)
            {
                Color32 old = pixels[y * width + x];
                float t = high > low ? Mathf.InverseLerp(low, high, Light(old)) : 0.5f;
                Color32 paint = Color.Lerp(strip.dark, strip.light, t);
                paint.a = old.a;
                pixels[y * width + x] = paint;
            }
        }
    }

    // How light a color looks (0 - 255).
    static float Light(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

    // ArmorLook on the PlayerCharacter prefab's top object (the Animator's), so every copy of the character has it, with
    // its Skin Under Clothes rows. Returns a few words for the Console.
    static string SetUpArmorLook(string characterPath, List<ArmorLook.SkinUnder> rows)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(characterPath);
        try
        {
            ArmorLook look = root.GetComponent<ArmorLook>();
            bool added = look == null;
            if (added) look = root.AddComponent<ArmorLook>();
            WriteSkinRows(look, rows);
            PrefabUtility.SaveAsPrefabAsset(root, characterPath);
            return "ArmorLook " + (added ? "added to" : "updated on") + " PlayerCharacter";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ArmorLook's Skin Under Clothes rows to keep: each clothing part and its Hides as set on the prefab (Blink's when
    // there's no ArmorLook yet), Blink's rows it's missing, and an empty row for each armor piece without one (a trimmed
    // copy needs a row). The Trims are always made again (AddTrims).
    static List<ArmorLook.SkinUnder> SkinRows(GameObject character)
    {
        List<ArmorLook.SkinUnder> rows = new List<ArmorLook.SkinUnder>();
        ArmorLook look = character.GetComponentInChildren<ArmorLook>(true);
        if (look != null)
        {
            foreach (ArmorLook.SkinUnder row in look.SkinUnderClothes)
            {
                if (row == null || string.IsNullOrEmpty(row.clothing) || HasRow(rows, row.clothing)) continue;
                rows.Add(new ArmorLook.SkinUnder(row.clothing, row.hides != null ? (string[])row.hides.Clone() : new string[0]));
            }
        }
        foreach (ArmorLook.SkinUnder row in ArmorLook.BlinkSkin())
        {
            if (!HasRow(rows, row.clothing)) rows.Add(row);
        }
        foreach (Slot slot in Slots)
        {
            if (!HasRow(rows, slot.part)) rows.Add(new ArmorLook.SkinUnder(slot.part));
        }
        return rows;
    }

    static bool HasRow(List<ArmorLook.SkinUnder> rows, string clothing) => rows.Exists(row => row.clothing == clothing);

    // Trimmed copies (ArmorLook.TrimmedSkin) for every row's clothing: each skin part it doesn't switch off, where at
    // least MinOnShare of it lies on the clothing. On the first-person hands (they have FirstPersonHands) only the parts
    // that copy draws: its arms, hands and gloves. height = the character's height (0 = measure it on this one's skin);
    // returns it, so the first-person hands (only arms) can use the body's.
    static float AddTrims(GameObject character, List<ArmorLook.SkinUnder> rows, float height, List<string> made)
    {
        Transform body = FindDeep(character.transform, BodyName);
        if (body == null) return height;
        FirstPersonHands hands = character.GetComponentInChildren<FirstPersonHands>(true);

        Dictionary<SkinnedMeshRenderer, Mesh> skins = new Dictionary<SkinnedMeshRenderer, Mesh>();   // each skin part and its posed copy
        try
        {
            foreach (SkinnedMeshRenderer part in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (part.sharedMesh != null && (hands == null || hands.Draws(part.name))) skins[part] = PosedMesh(part);
            }
            if (height <= 0f) height = Height(skins.Values);

            foreach (ArmorLook.SkinUnder row in rows)
            {
                SkinnedMeshRenderer cloth = FindPart(character, row.clothing);
                if (cloth == null || cloth.sharedMesh == null || (hands != null && !hands.Draws(cloth.name))) continue;
                Mesh clothPosed = PosedMesh(cloth);
                Vector3[] clothCorners = clothPosed.vertices;
                int[] clothFaces = clothPosed.triangles;
                Object.DestroyImmediate(clothPosed);

                foreach (KeyValuePair<SkinnedMeshRenderer, Mesh> skin in skins)
                {
                    if (row.hides != null && System.Array.IndexOf(row.hides, skin.Key.name) >= 0) continue;   // switched off anyway
                    if (row.trims.Exists(trim => trim.full == skin.Key.sharedMesh)) continue;                  // that mesh is done
                    Mesh trimmed = Trim(skin.Key.sharedMesh, skin.Value, clothCorners, clothFaces, row.clothing, height, out float share);
                    if (trimmed == null) continue;
                    row.trims.Add(new ArmorLook.TrimmedSkin { part = skin.Key.name, full = skin.Key.sharedMesh, trimmed = trimmed });
                    made.Add(trimmed.name + " (" + Mathf.RoundToInt(share * 100f) + " % of it lay on it)");
                }
            }
        }
        finally
        {
            foreach (Mesh posed in skins.Values) Object.DestroyImmediate(posed);
        }
        return height;
    }

    // The first-person hands' own trimmed copies, added to the same rows: that copy's arms and hands have meshes of their
    // own (FirstPersonArmsHelper), and ArmorLook only uses a copy on a part that has the mesh it was made from.
    static void AddFirstPersonTrims(List<ArmorLook.SkinUnder> rows, float height, List<string> made)
    {
        string path = FindPrefabPath(HandsPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;   // no first-person hands yet
        GameObject copy = PrefabUtility.LoadPrefabContents(path);             // never saved
        try
        {
            AddTrims(copy, rows, height, made);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(copy);
        }
    }

    // Lowest to highest corner of these posed meshes (the character's height), or 1.8 m if there are none.
    static float Height(IEnumerable<Mesh> posed)
    {
        float low = float.MaxValue, high = float.MinValue;
        foreach (Mesh mesh in posed)
        {
            foreach (Vector3 corner in mesh.vertices)
            {
                low = Mathf.Min(low, corner.y);
                high = Mathf.Max(high, corner.y);
            }
        }
        return high > low ? high - low : 1.8f;
    }

    // A copy of the skin's mesh without its faces lying under the clothing (all three corners within CutSkin of the
    // clothing's surface), saved as SkinFolder/<mesh> under <clothing>.asset. Null = less than MinOnShare of the skin
    // lies ON the clothing (within OnSkin), so the two don't flicker and no copy is needed. share = how much of it does.
    static Mesh Trim(Mesh mesh, Mesh posed, Vector3[] cloth, int[] clothFaces, string clothing, float height, out float share)
    {
        float on = height * OnSkin, cut = height * CutSkin;
        Vector3[] corners = posed.vertices;
        float[] distance = DistanceTo(cloth, clothFaces, corners, cut);

        float all = 0f, lying = 0f;   // the skin's whole surface, and the part of it lying on the clothing
        int cutFaces = 0;
        List<int>[] kept = new List<int>[posed.subMeshCount];
        for (int sub = 0; sub < kept.Length; sub++)
        {
            kept[sub] = new List<int>();
            int[] faces = posed.GetTriangles(sub);
            for (int i = 0; i + 2 < faces.Length; i += 3)
            {
                int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                float area = Vector3.Cross(corners[b] - corners[a], corners[c] - corners[a]).magnitude;   // twice its area: only shares count
                float farthest = Mathf.Max(distance[a], Mathf.Max(distance[b], distance[c]));
                all += area;
                if (farthest <= on) lying += area;
                if (farthest <= cut)
                {
                    cutFaces++;
                    continue;
                }
                kept[sub].Add(a);
                kept[sub].Add(b);
                kept[sub].Add(c);
            }
        }
        share = all > 0f ? lying / all : 0f;
        if (share < MinOnShare || cutFaces == 0) return null;

        MakeFolders(SkinFolder);
        return MeshAssetHelper.SaveSkinnedCopy(mesh, kept, SkinFolder + "/" + mesh.name + " under " + clothing + ".asset");
    }

    // How far each corner is from the clothing's surface (its faces); farther than "limit" = float.MaxValue.
    static float[] DistanceTo(Vector3[] cloth, int[] faces, Vector3[] corners, float limit)
    {
        int count = faces.Length / 3;
        Bounds[] near = new Bounds[count];   // each clothing face's box, grown by "limit" on every side (a quick first check)
        for (int f = 0; f < count; f++)
        {
            Bounds box = new Bounds(cloth[faces[f * 3]], Vector3.zero);
            box.Encapsulate(cloth[faces[f * 3 + 1]]);
            box.Encapsulate(cloth[faces[f * 3 + 2]]);
            box.Expand(limit * 2f);
            near[f] = box;
        }

        float[] distance = new float[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 corner = corners[i];
            float best = float.MaxValue;
            for (int f = 0; f < count; f++)
            {
                if (!near[f].Contains(corner)) continue;
                Vector3 closest = ClosestOnFace(corner, cloth[faces[f * 3]], cloth[faces[f * 3 + 1]], cloth[faces[f * 3 + 2]]);
                best = Mathf.Min(best, Vector3.Distance(corner, closest));
            }
            distance[i] = best <= limit ? best : float.MaxValue;
        }
        return distance;
    }

    // The point of the face (a, b, c) closest to p: inside it, on one of its edges or at a corner (Ericson's way, from
    // "Real-Time Collision Detection").
    static Vector3 ClosestOnFace(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));

        float sum = va + vb + vc;
        if (sum <= 0f) return a;   // a flat face with no area
        return a + ab * (vb / sum) + ac * (vc / sum);
    }

    static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // Writes the rows into ArmorLook's Skin Under Clothes list (the names in quotes must match ArmorLook's fields).
    static void WriteSkinRows(ArmorLook look, List<ArmorLook.SkinUnder> rows)
    {
        SerializedObject so = new SerializedObject(look);
        SerializedProperty list = so.FindProperty("skinUnderClothes");
        list.ClearArray();
        for (int r = 0; r < rows.Count; r++)
        {
            list.InsertArrayElementAtIndex(r);
            SerializedProperty row = list.GetArrayElementAtIndex(r);
            row.FindPropertyRelative("clothing").stringValue = rows[r].clothing;

            SerializedProperty hides = row.FindPropertyRelative("hides");
            hides.ClearArray();
            foreach (string part in rows[r].hides ?? new string[0])
            {
                hides.InsertArrayElementAtIndex(hides.arraySize);
                hides.GetArrayElementAtIndex(hides.arraySize - 1).stringValue = part;
            }

            SerializedProperty trims = row.FindPropertyRelative("trims");
            trims.ClearArray();
            foreach (ArmorLook.TrimmedSkin trim in rows[r].trims)
            {
                trims.InsertArrayElementAtIndex(trims.arraySize);
                SerializedProperty line = trims.GetArrayElementAtIndex(trims.arraySize - 1);
                line.FindPropertyRelative("part").stringValue = trim.part;
                line.FindPropertyRelative("full").objectReferenceValue = trim.full;
                line.FindPropertyRelative("trimmed").objectReferenceValue = trim.trimmed;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A body part trimmed under two pieces uses only one copy at a time (the later row's) while both show. A sentence
    // naming such parts, or null.
    static string Clashes(List<ArmorLook.SkinUnder> rows)
    {
        Dictionary<Mesh, List<string>> under = new Dictionary<Mesh, List<string>>();
        foreach (ArmorLook.SkinUnder row in rows)
        {
            foreach (ArmorLook.TrimmedSkin trim in row.trims)
            {
                if (!under.TryGetValue(trim.full, out List<string> clothes)) under[trim.full] = clothes = new List<string>();
                clothes.Add(row.clothing);
            }
        }
        List<string> clashes = new List<string>();
        foreach (KeyValuePair<Mesh, List<string>> part in under)
        {
            if (part.Value.Count > 1) clashes.Add(part.Key.name + " (under " + string.Join(" and ", part.Value) + ")");
        }
        if (clashes.Count == 0) return null;
        return "these skin parts lie on more than one piece: " + string.Join(", ", clashes) + ". While both pieces show, " +
               "only the later one's trimmed copy is used, so a little flicker may stay there.";
    }

    // PlayerArmor on the Player in the open scene with every set's bonus, and the armor in its ArmorTestKit (Ctrl + Z
    // undoes it). Returns a sentence for the Console.
    static string SetUpPlayer(List<ItemData> items)
    {
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();
        if (inventory == null) return " No Player in the open scene, so PlayerArmor and the Armor Test kit weren't saved on it. " +
                                      "Both add themselves when a game starts, so the armor and GameRules > Armor Test still " +
                                      "work; open GameScene and run it again to keep them in the scene.";

        AddToPlayer<PlayerArmor>();
        string bonusReport = AddSetBonuses(inventory.GetComponent<PlayerArmor>());

        // The armor's own test rule (GameRules > Armor Test): every piece in ArmorTestKit's list. The Tool Test kit had
        // the armor before (20/11/2026): it goes out of that list, so Tool Test gives only tools.
        AddToPlayer<ArmorTestKit>();
        int added = 0, movedOut = 0;
        ArmorTestKit armorKit = inventory.GetComponent<ArmorTestKit>();
        if (armorKit != null)
        {
            SerializedObject so = new SerializedObject(armorKit);
            SerializedProperty list = so.FindProperty("armor");   // must match ArmorTestKit's field
            foreach (ItemData item in items)
            {
                bool there = false;
                for (int i = 0; i < list.arraySize && !there; i++) there = list.GetArrayElementAtIndex(i).objectReferenceValue == item;
                if (there) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                added++;
            }
            so.ApplyModifiedProperties();   // with Undo
        }
        ToolTestKit toolKit = inventory.GetComponent<ToolTestKit>();
        if (toolKit != null)
        {
            SerializedObject so = new SerializedObject(toolKit);
            SerializedProperty list = so.FindProperty("tools");   // must match ToolTestKit's field
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                ItemData item = list.GetArrayElementAtIndex(i).objectReferenceValue as ItemData;
                if (item == null || !item.IsArmor) continue;
                list.GetArrayElementAtIndex(i).objectReferenceValue = null;   // a reference is emptied before it's removed
                list.DeleteArrayElementAtIndex(i);
                movedOut++;
            }
            so.ApplyModifiedProperties();   // with Undo
        }
        EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);
        string kitReport = (added > 0 ? " " + added + " armor items in the Armor Test kit." : "") +
                           (movedOut > 0 ? " " + movedOut + " armor items moved out of the Tool Test kit." : "");
        return " PlayerArmor on the Player." + bonusReport + kitReport;
    }

    // The set bonuses PlayerArmor's list is missing (e.g. Gold: +4 coins per kill and 10 % off every chest), added at
    // its end. A list saved before 21/11/2026 (stamina bonuses, Iron first then Reinforced Iron) is replaced first by
    // today's: Copper +1, Iron +2 (counts as Copper) and Gold +4 coins per kill. Other bonuses already there are left as
    // they are, except a color that was never set (a list saved before sets had colors) gets the set's own.
    static string AddSetBonuses(PlayerArmor armor)
    {
        if (armor == null) return "";
        Undo.RecordObject(armor, ToolName);
        bool replaced = armor.UpdateOldBonuses();
        SerializedObject so = new SerializedObject(armor);   // made after the change above, so it reads the new list
        SerializedProperty list = so.FindProperty("setBonuses");   // the names in quotes must match PlayerArmor's fields
        List<string> added = new List<string>();
        foreach (PlayerArmor.SetBonus bonus in PlayerArmor.DefaultBonuses())
        {
            bool there = false;
            for (int i = 0; i < list.arraySize && !there; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                there = string.Equals(entry.FindPropertyRelative("set").stringValue, bonus.set, System.StringComparison.OrdinalIgnoreCase);
                SerializedProperty color = entry.FindPropertyRelative("color");
                if (there && color.colorValue.a <= 0f) color.colorValue = bonus.color;
            }
            if (there) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty fresh = list.GetArrayElementAtIndex(list.arraySize - 1);
            fresh.FindPropertyRelative("set").stringValue = bonus.set;
            fresh.FindPropertyRelative("coinsPerKill").intValue = bonus.coinsPerKill;
            fresh.FindPropertyRelative("countsAs").stringValue = bonus.countsAs;
            fresh.FindPropertyRelative("color").colorValue = bonus.color;
            fresh.FindPropertyRelative("chestDiscount").floatValue = bonus.chestDiscount;
            added.Add(bonus.set);
        }
        so.ApplyModifiedProperties();   // with Undo
        return (replaced ? " The old set bonuses (stamina, or Iron first) were replaced by the coin bonuses." : "") +
               (added.Count > 0 ? " Set bonuses added: " + string.Join(", ", added) + "." : "");
    }
}
