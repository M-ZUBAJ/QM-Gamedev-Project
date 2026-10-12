/*
First Date: 11/10/2026
Date: 11/10/2026
Author: Tony
Use by: Not a script you put on an object. It is the "Consumable" section inside every Item Data asset (Assets/Config/Items).
Unity Editor: Select an Item Data file (e.g. Assets/Config/Items/Apple) > set Item Type = Consumable > the Consumable
              section appears: type how much it changes each stat. (Item Type = Material hides it: can't be eaten.)
              Positive = gives that much (Health 20 = heals 20). Negative = takes it away (Hunger -10 = makes you hungrier).
              Mix as many as you like, e.g. a meal: Health 20, Hunger 20. A bad mushroom: Health -15, Hunger 5.
              Testing: put the item in a hotbar slot, select it (1 - 8) and right click (the Consume key).
              The player needs ItemConsumer. Full steps: Docs/Item Setup.md
Purpose: What eating / drinking an item does to the player's stats (Health, Hunger, Stamina).
         ItemConsumer reads it and changes the stats. Stats never go above their max or below 0.
Requirement: Consumable.cs <-x ItemConsumer.cs, ItemData.cs, ItemDataEditor.cs
*/

using UnityEngine;

// [Serializable] lets Unity show this inside Item Data as a fold-out section and save it there.
[System.Serializable]
public class Consumable
{
    // ===== Fields =====
    [Tooltip("Health given when eaten (all stats max at 100 by default). Negative = damage. 0 = no change.")]
    public float health = 0f;

    [Tooltip("Food given when eaten (fills the hunger bar). Negative = makes the player hungrier. 0 = no change.")]
    public float hunger = 0f;

    [Tooltip("Stamina given when eaten. Negative = tires the player. 0 = no change.")]
    public float stamina = 0f;

    // True if eating it changes anything. All 0 = not consumable (a material like Wood or Stone).
    public bool IsConsumable => health != 0f || hunger != 0f || stamina != 0f;
}
