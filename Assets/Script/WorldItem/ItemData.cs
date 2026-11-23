/*
First Date: 7/10/2026
Date: 22/11/2026
Author: Tony
Use by: This is an ASSET (a config file in the Project window, kept in Assets/Config/Items), not a script you put on an object.
Unity Editor: Easiest: select an item object (in the scene or a prefab) > menu Tools > Items > Make Item From Selection.
              By hand: Assets/Config/Items > right click > Create > Items > Item Data, name the file after the item (e.g. "Wood"),
              drag the item's prefab into Prefab, choose Pickup. Then put ItemPickup on the prefab and drag this file into its "Data".
              Icon: choose ONE: Capture (a photo of the prefab), Static (a PNG sprite) or Animated (an .aseprite file).
              Item Type: Material (Wood, Stone: can't be eaten), Consumable (food / drink: right click to eat) or Tool
              (a pickaxe, axe or sword: held in the hand, left click swings it; a bow: hold left click, let go to shoot)
              or Armor (worn from the inventory's Gear tab; its Armor section, see ArmorStats.cs, says where it's worn
              and how much it protects) or Ammo (an arrow: a bow shoots it; its Ammo section, see AmmoStats.cs, says how
              hard it hits).
              Choosing Consumable shows its Health / Hunger / Stamina fields (see Consumable.cs), Tool shows its Kind
              and Tier (see ToolStats.cs) and, for a bow (Kind Bow), its Bow section (Damage, Draw Seconds, Arrow Speed:
              see BowStats.cs), Material shows Mined With (which pickaxe a rock dropping it needs, or which axe a tree:
              Spruce Wood = Iron).
              Both types can be used in crafting and building recipes.
              Full steps: Docs/Item Setup.md
Purpose: One kind of item (a material like Wood or Stone, or food like an Apple): what it looks like,
         how the player picks it up, what eating it does (Consumable type only), and its crafting recipe
         (what it is made from, how many, and where: Crafting page, Furnace...;
         for the crafting system later; empty recipe = can't be crafted, a raw material).
         The FILE is the item: its file name is the Item Id the inventory stacks by, recipes point to this file,
         and every object in the world with ItemPickup pointing here is this item. No tags, no typed ids.
         A tool whose recipe uses up another tool is an upgrade of it (UpgradedFrom: a Stone Axe of a Wooden Axe), and
         so is armor whose recipe uses up a piece for the same slot (an Iron Helmet of a Copper Helmet).
         Armor is worn, one piece per slot (PlayerArmor), from the inventory's Gear tab.
Requirement: ItemData.cs <-> Recipe.cs
             ItemData.cs x-> Consumable.cs, ToolStats.cs, BowStats.cs, ArmorStats.cs, AmmoStats.cs, IconCapture.cs, IconSource.cs, PlayerInventory.cs
             ItemData.cs <-x ArmorBuilder.cs, ArmorLook.cs, ArmorPicker.cs, ArmorSlotUI.cs, ArmorTestKit.cs, ArrowProjectile.cs, AutoCollector.cs, BerryBush.cs, BowBuilder.cs, BowHUD.cs, BowShooter.cs, CraftingMenu.cs, CraftingMenuBuilder.cs, CraftingRow.cs, CraftingSetPanels.cs, CraftingTabs.cs, Crosshair.cs, DesertBuilder.cs, GrasslandTreeBuilder.cs, HeldItemView.cs, ItemConsumer.cs, ItemDataEditor.cs, ItemIconRules.cs, ItemPickup.cs, ItemTools.cs, PlayerArmor.cs, PlayerInventory.cs, PlayerStats.cs, ResourceNode.cs, SnowBuilder.cs, TierToolBuilder.cs, ToolInHand.cs, ToolRules.cs, ToolTestKit.cs
             ItemData.cs <-o BuffDisplay.cs, FoodBuff.cs
*/

using System.Collections.Generic;
using UnityEngine;

// How an item is picked up. New ways of picking up get a new line here, and then show in the dropdown.
public enum PickupType
{
    Pickup,       // aim with the crosshair and press Grab (F)
    AutoPickup,   // collected as soon as the player touches it (F works too)
}

// What kind of item it is. New kinds (a tool, armor...) get a new line here, and then show in the dropdown.
// Every kind can be used in crafting and building recipes.
public enum ItemType
{
    Material,     // used to make things (Wood, Stone). Can't be eaten.
    Consumable,   // food / drink: right click to eat, changes the player's stats (Apple, Bread)
    Tool,         // a pickaxe, axe or sword: held in the hand, left click swings it (see its Tool section)
    Armor,        // worn on the body (helmet, chestplate...): put on from the inventory's Gear tab (see its Armor section)
    Ammo,         // an arrow: a bow in the hand shoots it (see its Ammo section); the strongest one carried goes first
}

// Where an item is made. New places (an anvil, a cooking pot...) get a new line here, and then show in the dropdown.
public enum CraftStation
{
    Crafting,   // the Crafting page of the inventory (no building needed)
    Furnace,    // a Furnace building (smelting, cooking)
}

// CreateAssetMenu adds this to the right click > Create menu in the Project window.
[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    // ===== Fields =====
    [Tooltip("Name shown to the player. Leave empty to use the file name.")]
    [SerializeField] string displayName = "";

    [Tooltip("What the item looks like in the world (a prefab with ItemPickup pointing back to this file). " +
             "Used when the item is dropped, for its icon and for recipes.")]
    [SerializeField] GameObject prefab;

    [Tooltip("How the player picks it up.")]
    [SerializeField] PickupType pickup = PickupType.Pickup;

    [Tooltip("The inventory picture. Capture = a photo of the prefab, Static = a PNG, Animated = an .aseprite file.")]
    [SerializeField] IconSource icon = new IconSource();

    [Tooltip("Material = used to make things, can't be eaten. Consumable = food / drink, right click to eat. " +
             "Tool = held and swung (a bow shoots). Armor = worn (the Gear tab). Ammo = an arrow (a bow shoots it). " +
             "Every type can be used in recipes.")]
    [SerializeField] ItemType itemType = ItemType.Material;

    // Only shown when Item Type = Consumable (Editor/ItemDataEditor.cs hides it otherwise).
    [Header("Consumable (food / drink)")]
    [Tooltip("What eating it does to the player's stats. Positive = gives, negative = takes away.")]
    [SerializeField] Consumable consumable = new Consumable();

    // Only shown when Item Type = Tool (Editor/ItemDataEditor.cs hides it otherwise).
    [Header("Tool (pickaxe, axe, sword)")]
    [Tooltip("What kind of tool it is and how strong (see ToolStats.cs). It shows in the player's hand and left click swings it.")]
    [SerializeField] ToolStats tool = new ToolStats();

    // Only shown for a bow: Item Type = Tool and Tool > Kind = Bow (Editor/ItemDataEditor.cs hides it otherwise).
    [Header("Bow (a Tool of Kind Bow)")]
    [Tooltip("What its arrows get from it: extra damage, how fast it draws and how fast its arrows fly (see BowStats.cs).")]
    [SerializeField] BowStats bow = new BowStats();

    // Only shown when Item Type = Armor (Editor/ItemDataEditor.cs hides it otherwise).
    [Header("Armor (helmet, chestplate...)")]
    [Tooltip("Where it's worn, how much it protects, and which part of the character it shows (see ArmorStats.cs).")]
    [SerializeField] ArmorStats armor = new ArmorStats();

    // Only shown when Item Type = Ammo (Editor/ItemDataEditor.cs hides it otherwise).
    [Header("Ammo (arrows)")]
    [Tooltip("How hard it hits and how its head looks (see AmmoStats.cs). A bow in the hand shoots it.")]
    [SerializeField] AmmoStats ammo = new AmmoStats();

    // Only shown when Item Type = Material.
    [Header("Mining")]   // (rock and, since 22/11/2026, trees: see the tooltip)
    [Tooltip("The weakest tool that can gather a rock or a tree that drops this item: a pickaxe for a rock, an axe for a " +
             "tree. None = anything can, even a bare hand (Stone, Wood). Copper and Iron: Stone. Gold: Iron. Diamond: " +
             "Gold. Spruce Wood: Iron (the snow's spruce needs an Iron Axe or better).")]
    [SerializeField] ToolTier minedWith = ToolTier.None;

    [Header("Crafting (for the crafting system, coming later)")]
    [Tooltip("What crafting this item costs. Click + for each material, drag in its Item Data and set the amount. " +
             "Empty = can't be crafted (a raw material found in the world, like Wood).")]
    [SerializeField] List<Ingredient> recipe = new List<Ingredient>();

    [Min(1)]
    [Tooltip("How many of this item one craft makes (e.g. 1 Wood -> 4 Planks).")]
    [SerializeField] int craftAmount = 1;

    [Tooltip("Where it is made: Crafting = the inventory's Crafting page, Furnace = at a Furnace. " +
             "Only matters when it has a recipe.")]
    [SerializeField] CraftStation craftedAt = CraftStation.Crafting;

    public string Id => name;   // the file name: same file = same item = stacks together
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public GameObject Prefab => prefab;
    public PickupType Pickup => pickup;
    public IconSource Icon => icon;   // where the picture comes from (Capture / Static / Animated)
    public ItemType Type => itemType;
    public Consumable Consumable => consumable;
    // Can be eaten: Consumable type with at least one stat set (a Material never can, whatever its numbers say).
    public bool IsConsumable => itemType == ItemType.Consumable && consumable != null && consumable.IsConsumable;
    public bool IsTool => itemType == ItemType.Tool && tool != null;   // held in the hand and swung (ToolRules, ToolInHand)
    public ToolStats Tool => tool;               // its kind and tier (only means something when IsTool)
    public bool IsBow => IsTool && tool.kind == ToolKind.Bow && bow != null;   // a bow: BowShooter shoots it (BowStats)
    public BowStats Bow => bow;                  // its damage, draw time and arrow speed (only means something when IsBow)
    public bool IsArmor => itemType == ItemType.Armor && armor != null;   // worn on the body (PlayerArmor, the Gear tab)
    public ArmorStats Armor => armor;            // its slot, defense and look (only means something when IsArmor)
    public bool IsAmmo => itemType == ItemType.Ammo && ammo != null;   // an arrow: a bow shoots it (BowShooter)
    public AmmoStats Ammo => ammo;               // its damage and head look (only means something when IsAmmo)
    public ToolTier MinedWith => minedWith;      // the weakest pickaxe (rock) or axe (tree) for one that drops it (None = anything)
    public IReadOnlyList<Ingredient> Recipe => recipe;
    public int CraftAmount => craftAmount;
    public CraftStation CraftedAt => craftedAt;
    public bool IsCraftable => recipe.Exists(ing => ing != null && ing.item != null);
    // The tool or armor piece this one is an upgrade of: the one in its recipe, which crafting uses up (a Stone Axe's
    // Wooden Axe, an Iron Helmet's Copper Helmet: armor for the same slot). null = not an upgrade. Crafting puts
    // the new item in the slot the old one was in (PlayerInventory.AddInto), and a worn piece is upgraded where it's
    // worn (PlayerArmor.LendForUpgrade).
    public ItemData UpgradedFrom
    {
        get
        {
            if (IsTool) return recipe.Find(ing => ing != null && ing.item != null && ing.item.IsTool)?.item;
            if (IsArmor) return recipe.Find(ing => ing != null && ing.item != null && ing.item.IsArmor && ing.item.Armor.slot == armor.slot)?.item;
            return null;
        }
    }

    // ===== Unity messages (Unity calls these) =====

    // Unity calls this when a value is changed in the Inspector: read the .aseprite frames if Icon is Animated.
    void OnValidate()
    {
#if UNITY_EDITOR
        if (icon == null) icon = new IconSource();
        if (consumable == null) consumable = new Consumable();
        if (tool == null) tool = new ToolStats();
        if (bow == null) bow = new BowStats();
        if (armor == null) armor = new ArmorStats();
        if (ammo == null) ammo = new AmmoStats();
        icon.EditorRefresh(this);
#endif
    }

    // ===== My methods =====

    // The picture to show (for an animated icon: its first frame; UIAnimatedImage.ShowIcon plays the rest).
    // Capture = a photo of the prefab (taken once, then remembered).
    public Sprite GetIcon()
    {
        if (!icon.IsCapture) return icon.Still;
        return prefab != null ? IconCapture.GetIcon(Id, prefab) : null;
    }

    // The recipe checks, ready for the crafting system (shared with Building Data, see Recipe).
    // No recipe = uncraftable: CanCraft and TakeRecipeFrom are always false (an empty recipe is NOT free here).
    public bool CanCraft(PlayerInventory inv) => IsCraftable && global::Recipe.CanAfford(recipe, inv);
    public bool TakeRecipeFrom(PlayerInventory inv) => IsCraftable && global::Recipe.TakeFrom(recipe, inv);

    // Why it can't be crafted right now, e.g. "3 Stone", or "Can't be crafted" with no recipe. null = can craft.
    public string Missing(PlayerInventory inv)
    {
        if (!IsCraftable) return "Can't be crafted";
        return global::Recipe.Missing(recipe, inv);
    }
}
