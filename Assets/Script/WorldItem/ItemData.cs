/*
First Date: 7/10/2026
Date: 11/10/2026
Author: Tony
Use by: This is an ASSET (a config file in the Project window, kept in Assets/Config/Items), not a script you put on an object.
Unity Editor: Easiest: select an item object (in the scene or a prefab) > menu Tools > Items > Make Item From Selection.
              By hand: Assets/Config/Items > right click > Create > Items > Item Data, name the file after the item (e.g. "Wood"),
              drag the item's prefab into Prefab, choose Pickup. Then put ItemPickup on the prefab and drag this file into its "Data".
              Icon: choose ONE: Capture (a photo of the prefab), Static (a PNG sprite) or Animated (an .aseprite file).
              Item Type: Material (Wood, Stone: can't be eaten) or Consumable (food / drink: right click to eat).
              Choosing Consumable shows its Health / Hunger / Stamina fields (see Consumable.cs).
              Both types can be used in crafting and building recipes.
              Full steps: Docs/Item Setup.md
Purpose: One kind of item (a material like Wood or Stone, or food like an Apple): what it looks like,
         how the player picks it up, what eating it does (Consumable type only), and its crafting recipe
         (what it is made from, how many, and where: Crafting page, Furnace...;
         for the crafting system later; empty recipe = can't be crafted, a raw material).
         The FILE is the item: its file name is the Item Id the inventory stacks by, recipes point to this file,
         and every object in the world with ItemPickup pointing here is this item. No tags, no typed ids.
Requirement: ItemData.cs <-> Recipe.cs
             ItemData.cs x-> Consumable.cs, IconCapture.cs, IconSource.cs, PlayerInventory.cs
             ItemData.cs <-x AutoCollector.cs, CraftingMenu.cs, Crosshair.cs, ItemConsumer.cs, ItemDataEditor.cs, ItemPickup.cs, ItemTools.cs, PlayerInventory.cs, PlayerStats.cs, ResourceNode.cs
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
             "Both can be used in recipes.")]
    [SerializeField] ItemType itemType = ItemType.Material;

    // Only shown when Item Type = Consumable (Editor/ItemDataEditor.cs hides it otherwise).
    [Header("Consumable (food / drink)")]
    [Tooltip("What eating it does to the player's stats. Positive = gives, negative = takes away.")]
    [SerializeField] Consumable consumable = new Consumable();

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
    public IReadOnlyList<Ingredient> Recipe => recipe;
    public int CraftAmount => craftAmount;
    public CraftStation CraftedAt => craftedAt;
    public bool IsCraftable => recipe.Exists(ing => ing != null && ing.item != null);

    // ===== Unity messages (Unity calls these) =====

    // Unity calls this when a value is changed in the Inspector: read the .aseprite frames if Icon is Animated.
    void OnValidate()
    {
#if UNITY_EDITOR
        if (icon == null) icon = new IconSource();
        if (consumable == null) consumable = new Consumable();
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
