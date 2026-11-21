/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > World > Build Desert Content (Tools > World > Build New World Scene runs it too, and puts it
              all in the new world). Needs the rock shapes (Tools > World > Use Low Poly Rocks) and RPG Monster DUO PBR
              Polyart (for the turtle). Run it again any time: everything is refilled in place; the Gold item, the
              Gold item's prefab and the two rock materials are only made if missing, so changes you make to them stay.
              Guide: Docs/Desert Setup.md
Purpose: Everything the new world's desert needs, in one click:
           - Gold: the item (Assets/Config/Items/Gold, a raw material; Mined With = Iron, so its rock needs an Iron
             Pickaxe or better) and its prefab (Assets/Prefabs/Item/Gold: a small gold cube, like Turki's Iron and
             Copper, copied from his Iron);
           - GoldNode (Assets/Prefabs/ResourceNodes/Rocks/GoldNode): a Prefab Variant of Turki's IronNode in shiny gold,
             a little harder (40 health), dropping 2 - 4 Gold;
           - SandstoneNode (.../Rocks/SandstoneNode): a Prefab Variant of his StoneNode in warm desert sandstone, 2.5
             times as hard (75 health instead of 30), dropping 4 - 6 Stone;
           - their materials, Assets/Materials/Rocks/Gold Ore.mat and Sandstone.mat (the rock pack's sand color sheet,
             tinted), and their rock-break dust in their own colors (Tools > Effects > Build Rock Break runs again);
           - two desert caves of sandstone (Assets/Prefabs/World/Caves/Desert Cave Small and Desert Cave, from
             RockCaveBuilder): the World Generator puts gold and sandstone in them;
           - the turtle, Assets/Prefabs/Enemy/TurtleShellEnemy (NightMonsterBuilder.BuildTurtle): since 21/11/2026 a
             night monster from night 2, with its shell (ShellDefense), no longer the desert's day monster. Tools >
             Enemies > Set Up Night Monsters puts it in the scenes' night spawner.
         MakeOreItem, RockMaterial and MakeVariant are shared with SnowBuilder (the snow's Diamond, DiamondNode and
         StoneNode Snow); MakeGold with ArmorBuilder (the gold armor's recipes need the Gold item). Since 21/11/2026 an ore item copied from Iron also gets its own Mined With when it still has
         the one the copy brought from Iron (Stone): Gold had kept it, so a Stone Pickaxe mined gold.
Requirement: DesertBuilder.cs x-> ItemData.cs, ToolStats.cs, ItemPickup.cs, ShapeColor.cs, ResourceNode.cs, NightMonsterBuilder.cs, RockCaveBuilder.cs, RockBreakBuilder.cs, UIBuildHelper.cs, MenuOrder.cs
             DesertBuilder.cs <-x ArmorBuilder.cs, NewWorldBuilder.cs, SnowBuilder.cs
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UIBuildHelper;    // FindPrefabPath, FolderOf, MakeFolders

public static class DesertBuilder
{
    // What the tool made (Build New World Scene puts it in the new world). Anything missing is null / empty.
    public class Parts
    {
        public ItemData gold;
        public GameObject goldOre, sandstone, monster;
        public List<GameObject> caves = new List<GameObject>();
        public string report;
    }

    // ===== Fields =====
    const string ToolName = "Build Desert Content";
    const string ItemFolder = "Assets/Config/Items";
    const string ItemPrefabFolder = "Assets/Prefabs/Item";
    const string NodeFolder = "Assets/Prefabs/ResourceNodes";
    const string RocksFolder = NodeFolder + "/Rocks";
    const string DesertStonePath = RocksFolder + "/StoneNode Desert.prefab";   // its material is the pack's sand color sheet
    const string MaterialFolder = "Assets/Materials/Rocks";

    static readonly Color GoldCube = new Color(1f, 0.78f, 0.22f);         // the Gold item's cube
    static readonly Color GoldTint = new Color(1f, 0.9f, 0.45f);          // the sand color sheet -> gold
    static readonly Color SandstoneTint = new Color(1f, 0.8f, 0.62f);     // the sand color sheet -> warm sandstone

    const float GoldHealth = 40f;        // Turki's rocks have 30
    const float SandstoneHealth = 75f;   // 2.5 times a stone rock: "harder to break"

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/World/Build Desert Content", false, MenuOrder.BuildDesertContent)]
    static void FromMenu()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(ToolName, "Stop Play first: changes made while playing are lost.", "OK");
            return;
        }
        Parts parts = BuildAll();
        Debug.Log(ToolName + ": " + parts.report + " Build New World Scene puts them in the new world.");
    }

    // ===== My methods =====

    // Builds (refills) everything. Returns what it made, with a report for the Console.
    public static Parts BuildAll()
    {
        Parts parts = new Parts();
        List<string> report = new List<string>();

        parts.gold = MakeGold(report);
        Material sand = SandSheet();
        Material goldLook = RockMaterial("Gold Ore", sand, GoldTint, 0.45f, 0.65f);
        Material sandstoneLook = RockMaterial("Sandstone", sand, SandstoneTint, 0f, 0.12f);
        parts.goldOre = MakeVariant("IronNode", "GoldNode", goldLook, parts.gold, GoldHealth, 2, 4, report);
        parts.sandstone = MakeVariant("StoneNode", "SandstoneNode", sandstoneLook, null, SandstoneHealth, 4, 6, report);
        AssetDatabase.SaveAssets();
        report.Add("rock break: " + RockBreakBuilder.Build());

        parts.caves = RockCaveBuilder.BuildDesert(sandstoneLook, out string caves);
        report.Add(caves);
        parts.monster = MakeMonster(report);
        AssetDatabase.SaveAssets();

        parts.report = string.Join("; ", report);
        return parts;
    }

    // The Gold item and its prefab (made once, then kept right). Tools > Items > Build Plate Armor makes it too when
    // it's missing, so the gold armor gets its recipes.
    public static ItemData MakeGold(List<string> report) => MakeOreItem("Gold", GoldCube, ToolTier.Iron, report);

    // An ore's item and its prefab (e.g. Gold): copies of Turki's Iron item and prefab (a small colored cube), made
    // once, then linked to each other, colored and given the pickaxe its rock needs (Mined With). Each run only changes
    // what's still wrong. SnowBuilder makes the Diamond with it.
    public static ItemData MakeOreItem(string id, Color cube, ToolTier minedWith, List<string> report)
    {
        ItemData iron = FindItem("Iron");
        string prefabPath = FindPrefabPath(ItemPrefabFolder + "/" + id + ".prefab");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null && iron != null && iron.Prefab != null)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(iron.Prefab), prefabPath);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        ItemData item = FindItem(id);
        bool made = item == null;
        if (made)
        {
            string itemPath = ItemFolder + "/" + id + ".asset";
            if (iron != null) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(iron), itemPath);
            else AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ItemData>(), itemPath);
            item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
        }
        if (item == null)
        {
            report.Add("the " + id + " item couldn't be made");
            return null;
        }

        SerializedObject so = new SerializedObject(item);   // the names in quotes must match ItemData's fields
        SerializedProperty link = so.FindProperty("prefab");
        if (prefab != null && (made || link.objectReferenceValue == null || link.objectReferenceValue == (iron != null ? iron.Prefab : null)))
            link.objectReferenceValue = prefab;
        if (made) so.FindProperty("displayName").stringValue = "";   // = the file name, e.g. Gold
        // Mined With: set while it's None, or still the one the copy brought from Iron (Stone). Before 21/11/2026 only
        // None was set, so Gold kept Iron's Stone and a Stone Pickaxe mined gold.
        SerializedProperty needs = so.FindProperty("minedWith");
        int copied = iron != null && iron != item ? (int)iron.MinedWith : (int)ToolTier.None;
        bool fixedNow = needs.enumValueIndex != (int)minedWith && (needs.enumValueIndex == (int)ToolTier.None || needs.enumValueIndex == copied);
        if (fixedNow) needs.enumValueIndex = (int)minedWith;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (prefab != null) PaintItemPrefab(AssetDatabase.GetAssetPath(prefab), item, cube);
        string tier = minedWith.ToString();
        string pickaxe = "needs " + ("AEIOU".IndexOf(tier[0]) >= 0 ? "an " : "a ") + tier + " Pickaxe";   // "an Iron", "a Gold"
        report.Add(made ? id + " item made (" + AssetDatabase.GetAssetPath(item) + ", " + pickaxe + ")"
                        : id + " item kept" + (fixedNow ? " (now " + pickaxe + ")" : ""));
        return item;
    }

    // An ore item's prefab: its cube colored (its ShapeColor), picked up as that item (its ItemPickup), its top object
    // named after it.
    static void PaintItemPrefab(string path, ItemData item, Color cube)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = item.name;
            foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (script == null) continue;
                SerializedObject so = new SerializedObject(script);
                if (script is ShapeColor) so.FindProperty("color").colorValue = cube;   // the cube's color
                if (script is ItemPickup) so.FindProperty("data").objectReferenceValue = item;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The rock pack's sand color sheet (StoneNode Desert's material), else StoneNode's grey one.
    static Material SandSheet()
    {
        foreach (string path in new[] { FindPrefabPath(DesertStonePath), FindPrefabPath(NodeFolder + "/StoneNode.prefab") })
        {
            GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshRenderer look = rock != null ? rock.GetComponentInChildren<MeshRenderer>(true) : null;
            if (look != null && look.sharedMaterial != null) return look.sharedMaterial;
        }
        return null;
    }

    // A copy of a color sheet material, tinted, with its own shine (metallic, smoothness), in Assets/Materials/Rocks.
    // Made once, so your changes stay. (SnowBuilder uses it too.)
    public static Material RockMaterial(string fileName, Material from, Color tint, float metallic, float smoothness)
    {
        MakeFolders(MaterialFolder);
        string path = MaterialFolder + "/" + fileName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null || from == null) return material;

        material = new Material(from) { name = fileName };
        if (material.HasProperty("_Color")) material.color = from.color * tint;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // A Prefab Variant of one of Turki's rocks (made once in a preview scene, then refilled in place every run): its
    // material, health and drops changed; its shapes, ground standing and breaking come from the rock it's made from.
    // drop null = the rock's own drop; health 0 = its own health; max 0 = its own drop amounts. (SnowBuilder uses it too.)
    public static GameObject MakeVariant(string from, string name, Material material, ItemData drop, float health, int min, int max,
                                         List<string> report)
    {
        GameObject parent = AssetDatabase.LoadAssetAtPath<GameObject>(FindPrefabPath(NodeFolder + "/" + from + ".prefab"));
        if (parent == null)
        {
            report.Add("no " + from + " prefab, so no " + name);
            return null;
        }
        MakeFolders(RocksFolder);
        string path = RocksFolder + "/" + name + ".prefab";
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        Scene preview = existed ? default : EditorSceneManager.NewPreviewScene();
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : (GameObject)PrefabUtility.InstantiatePrefab(parent, preview);
        try
        {
            root.name = name;
            MeshRenderer look = root.GetComponentInChildren<MeshRenderer>(true);
            if (look != null && material != null) look.sharedMaterials = new[] { material };
            ResourceNode node = root.GetComponent<ResourceNode>();
            if (node != null)
            {
                SerializedObject so = new SerializedObject(node);   // the names in quotes must match Turki's fields
                if (drop != null) so.FindProperty("dropItem").objectReferenceValue = drop;
                if (health > 0f) so.FindProperty("maxHealth").floatValue = health;
                if (max > 0)
                {
                    so.FindProperty("dropMin").intValue = min;
                    so.FindProperty("dropMax").intValue = max;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);   // a copy of a prefab saved as a new one = a Prefab Variant
            report.Add(name + (existed ? " refilled" : " made") + " (" + (health > 0f ? health + " health" : from + "'s health") +
                       ", " + (max > 0 ? min + " - " + max + " " : from + "'s amount of ") + (drop != null ? drop.name : "its own drop") + ")");
            return saved;
        }
        finally
        {
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    // The turtle, as a night monster with its shell (since 21/11/2026 it's no longer the desert's day monster):
    // NightMonsterBuilder builds it the same way Tools > Enemies > Set Up Night Monsters does.
    static GameObject MakeMonster(List<string> report)
    {
        return NightMonsterBuilder.BuildTurtle(report);
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
}
