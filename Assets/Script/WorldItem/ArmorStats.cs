/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: Not a script you put on an object. It is the "Armor" section inside an Item Data asset whose Item Type is Armor
        (e.g. Assets/Config/Items/Armor/IronHelmet), and the list of armor slots.
Unity Editor: Select an armor's Item Data > Item Type = Armor > the Armor section: Slot, Protection, Set, Piece, Hides
              and Look. Tools > Items > Build Plate Armor makes the 6 pieces of the Copper and Iron sets with these
              filled in.
Purpose: What a piece of armor is:
         - Slot: where it's worn (one piece per slot: Helmet, Shoulders, Chest, Gloves, Legs, Boots).
         - Protection: how much of every hit it takes off, in percent (PlayerArmor adds every worn piece up: the
           copper set is 1.5 % a piece and 2.5 % for the chestplate, 10 % for all six; the iron set 16 %).
         - Set: the armor set it belongs to (e.g. Copper). A piece of one set in every slot gives that set's bonus
           (PlayerArmor's Set Bonuses: the copper set's stamina comes back 50 % faster, the iron set's 100 %).
         - Piece: the part of the character's model it switches on when worn (Blink's character comes with its armor
           already inside, switched off, e.g. "PlateSet1_Helmet").
         - Hides: parts of the character it switches off while worn, so they don't poke through (e.g. the hair under
           a helmet, the starter shirt under a chestplate).
         - Look: the material it's drawn with while worn (ArmorLook), on the ground and in its icon: its set's colors
           (Copper = copper, Iron = steel grey). Both sets share Blink's plate shapes, so the color tells them apart.
Requirement: ArmorStats.cs <-x ArmorBuilder.cs, ArmorLook.cs, ArmorPicker.cs, ArmorSlotUI.cs, CraftingSetPanels.cs, GearPage.cs, GearPageBuilder.cs, IconArt.cs, ItemData.cs, PlayerArmor.cs, PlayerInfoPage.cs
*/

using UnityEngine;
using UnityEngine.Serialization;   // FormerlySerializedAs

// Where a piece of armor is worn, top to bottom. One piece per slot. New slots (a cape, a ring...) get a new line here
// at the end, and then show in the dropdown and on the Gear page.
public enum ArmorSlot
{
    Helmet,
    Shoulders,
    Chest,
    Gloves,
    Legs,
    Boots,
}

// [Serializable] lets Unity show this inside Item Data as a fold-out section and save it there.
[System.Serializable]
public class ArmorStats
{
    // ===== Fields =====
    [Tooltip("Where it's worn. The player wears one piece per slot.")]
    public ArmorSlot slot = ArmorSlot.Chest;

    [Tooltip("How much of every hit it takes off, in percent. PlayerArmor adds up every worn piece (at most 80 %). " +
             "The iron set: 1.5 each, 2.5 for the chestplate = 10 % for the whole set.")]
    [Min(0f)]
    [FormerlySerializedAs("defense")]   // it was whole Defense points before (each 4 %)
    public float protection = 1.5f;

    [Tooltip("The armor set it belongs to (e.g. Iron). A piece of the same set in every slot gives that set's bonus " +
             "(PlayerArmor > Set Bonuses). Empty = no set.")]
    public string set = "";

    [Tooltip("The name of the part of the character's model this switches on when worn (e.g. PlateSet1_Helmet). " +
             "Blink's character has its armor inside it, switched off.")]
    public string piece = "";

    [Tooltip("Names of parts of the character switched off while this is worn, so they don't poke through " +
             "(e.g. Hair under a helmet, Starter_Chest under a chestplate).")]
    public string[] hides = new string[0];

    [Tooltip("The material it's drawn with while worn (and its prefab's, so on the ground and in its icon): its set's " +
             "colors. Tools > Items > Build Plate Armor makes one per set in Assets/Materials/Armor. Empty = the " +
             "character's own (Blink's).")]
    public Material look;

    // ===== My methods =====

    // The name the player sees for a slot: Helmet, Shoulders, Chestplate, Gloves, Leggings, Boots.
    public static string SlotName(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Chest: return "Chestplate";
            case ArmorSlot.Legs:  return "Leggings";
            default:              return slot.ToString();
        }
    }

    // A percentage the way the Gear and Info pages write it: 1.5 -> "1.5%", 10 -> "10%". Always with a dot, whatever
    // the computer's language (some write 1,5).
    public static string Percent(float percent)
    {
        return percent.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
