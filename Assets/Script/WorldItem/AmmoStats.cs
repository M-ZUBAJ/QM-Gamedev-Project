/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: Not a script you put on an object. It is the "Ammo" section inside an Item Data asset whose Item Type is Ammo
        (e.g. Assets/Config/Items/Arrows/IronArrow).
Unity Editor: Select an arrow's Item Data > Item Type = Ammo > the Ammo section: Damage and Head. Tools > Items > Build Bow
              And Arrows makes the five arrows (Stone, Copper, Iron, Gold, Diamond) and sets both (it sets them again
              each time it runs).
Purpose: What an arrow is: how hard it hits (Damage, from a fully drawn Wooden Bow: half drawn = half) and how its head
         looks (Head: the material the arrowhead gets in flight, stuck in things and on the bow string). A bow in the
         hand (BowShooter) shoots the arrow with the most Damage the player carries.
Requirement: AmmoStats.cs <-x BowBuilder.cs, BowShooter.cs, CraftingMenuBuilder.cs, ItemData.cs
             AmmoStats.cs <-o ToolInHand.cs
*/

using UnityEngine;

// [Serializable] lets Unity show this inside Item Data as a fold-out section and save it there.
[System.Serializable]
public class AmmoStats
{
    // ===== Fields =====
    [Tooltip("How much one arrow hurts a monster or an animal, shot from a fully drawn Wooden Bow (half drawn = half). " +
             "Stone 20, Copper 25, Iron 30, Gold 40, Diamond 50 (a wooden sword hits for 25).")]
    [Min(0f)]
    public float damage = 20f;

    [Tooltip("The arrowhead's material: the head gets it in flight, stuck in things and on the bow string. Empty = the " +
             "arrow model's own colors.")]
    public Material head;
}
