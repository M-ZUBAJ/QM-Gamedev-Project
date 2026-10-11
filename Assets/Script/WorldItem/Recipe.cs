/*
First Date: 7/10/2026
Date: 11/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component).
Unity Editor: You never add this yourself. It is the "Recipe" list you fill in on an Item Data or a Building Data:
              click + for each material, drag in its Item Data (Assets/Config/Items) and set the amount.
Purpose: Everything about recipes in ONE place, shared by every config that costs materials
         (Building Data: what a building costs, Item Data: what crafting an item costs).
         - Ingredient: one recipe line = which material (Item Data) and how many.
         - Recipe.CanAfford / Missing / TakeFrom: checks and pays a recipe from the player's inventory.
         The same material twice in one recipe is added up (2 Stone + 3 Stone = 5 Stone).
Requirement: Recipe.cs <-> ItemData.cs
             Recipe.cs x-> PlayerInventory.cs
             Recipe.cs <-x BuildingButton.cs, BuildingData.cs
*/

using System.Collections.Generic;
using UnityEngine;

// One line of a recipe: which material and how many.
[System.Serializable]
public class Ingredient
{
    [Tooltip("The material: drag in its Item Data config file (from Assets/Config/Items, e.g. Wood).")]
    public ItemData item;

    [Min(1)]
    [Tooltip("How many of it are needed.")]
    public int amount = 1;

    public string ItemId => item != null ? item.Id : "";
    public string DisplayName => item != null ? item.DisplayName : "(no item)";
}

public static class Recipe
{
    // ===== My methods =====

    // True if the inventory has every material of the recipe. An empty recipe is free.
    public static bool CanAfford(IReadOnlyList<Ingredient> recipe, PlayerInventory inv)
    {
        return Missing(recipe, inv) == null;
    }

    // What is still missing, e.g. "3 Stone, 1 Wood", or null if there is enough of everything.
    public static string Missing(IReadOnlyList<Ingredient> recipe, PlayerInventory inv)
    {
        List<string> missing = new List<string>();
        foreach (KeyValuePair<string, int> need in TotalNeeded(recipe))
        {
            int have = inv != null ? inv.Count(need.Key) : 0;
            if (have < need.Value) missing.Add((need.Value - have) + " " + NameOf(recipe, need.Key));
        }
        return missing.Count == 0 ? null : string.Join(", ", missing);
    }

    // Takes every material out of the inventory. Takes nothing (and returns false) if anything is missing.
    public static bool TakeFrom(IReadOnlyList<Ingredient> recipe, PlayerInventory inv)
    {
        if (!CanAfford(recipe, inv)) return false;
        if (inv == null) return true;   // only an empty (free) recipe gets here without an inventory

        foreach (KeyValuePair<string, int> need in TotalNeeded(recipe))
        {
            inv.Remove(need.Key, need.Value);
        }
        return true;
    }

    // Adds up the recipe per Item Id, in case the same material is in it twice (e.g. 2 Stone + 3 Stone = 5 Stone).
    static Dictionary<string, int> TotalNeeded(IReadOnlyList<Ingredient> recipe)
    {
        Dictionary<string, int> total = new Dictionary<string, int>();
        if (recipe == null) return total;

        foreach (Ingredient ing in recipe)
        {
            if (ing == null || ing.item == null) continue;   // an empty recipe line costs nothing
            total.TryGetValue(ing.ItemId, out int sum);
            total[ing.ItemId] = sum + Mathf.Max(1, ing.amount);
        }
        return total;
    }

    static string NameOf(IReadOnlyList<Ingredient> recipe, string itemId)
    {
        foreach (Ingredient ing in recipe)
        {
            if (ing != null && ing.item != null && ing.ItemId == itemId) return ing.DisplayName;
        }
        return itemId;
    }
}
