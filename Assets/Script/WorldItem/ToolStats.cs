/*
First Date: 12/11/2026
Date: 22/11/2026
Author: Tony
Use by: Not a script you put on an object. It is the "Tool" section inside an Item Data asset whose Item Type is Tool
        (e.g. Assets/Config/Items/Tools/StonePickaxe), and the lists of tool kinds and tiers.
Unity Editor: Select a tool's Item Data > Item Type = Tool > the Tool section: Kind (Pickaxe / Axe / Sword / Bow) and
              Tier (Wood ... Diamond). Tools > Items > Build Tier Tools makes all 15 tools, Tools > Items > Build Bow And
              Arrows the Wooden Bow and the Spruce Bow. An ore's Item Data uses ToolTier too: its Mined With (the weakest
              pickaxe that can mine it).
Purpose: What a tool is: its kind (what it's good at) and its tier (how strong). ToolRules turns these into damage and
         into which rocks it can mine:
         - Pickaxe: rock and ores. A higher tier mines harder ores (Wood: stone, Stone: copper and iron, Iron: gold,
           Gold: diamond).
         - Axe: chops trees faster. A higher tier chops harder trees (Iron: the snow's spruce).
         - Sword: hits enemies and animals harder.
         - Bow: shoots arrows (BowShooter: hold the attack button to draw, let go to shoot); it doesn't hit up close.
         Each tier is 25 % stronger than the one before at the tool's own job. Not a bow: how strong a bow is comes from
         its own Bow section (BowStats: its damage, draw time and arrow speed); its tier only sorts the crafting list.
Requirement: ToolStats.cs <-x BowBuilder.cs, CraftingMenuBuilder.cs, DesertBuilder.cs, FireballProjectile.cs, ItemData.cs, ItemDataEditor.cs, PlayerCombat.cs, SnowBuilder.cs, TierToolBuilder.cs, ToolInHand.cs, ToolRules.cs
*/

using UnityEngine;

// What a tool is for. New kinds (a hoe, a shovel...) get a new line here, and then show in the dropdown.
public enum ToolKind
{
    Pickaxe,   // rock and ores
    Axe,       // trees
    Sword,     // enemies and animals
    Bow,       // shoots arrows (BowShooter), held in the left hand; it doesn't hit up close
}

// How strong a tool is, weakest first. None = no tool (a bare hand), and for an ore's Mined With: "anything can mine it".
public enum ToolTier
{
    None,
    Wood,
    Stone,
    Iron,      // copper counts as this tier too: "copper or iron can mine gold"
    Gold,
    Diamond,
}

// [Serializable] lets Unity show this inside Item Data as a fold-out section and save it there.
[System.Serializable]
public class ToolStats
{
    // ===== Fields =====
    [Tooltip("What it's good at: Pickaxe = rock and ores, Axe = trees, Sword = enemies and animals, Bow = shoots arrows.")]
    public ToolKind kind = ToolKind.Pickaxe;

    [Tooltip("How strong it is. Each tier is 25 % stronger than the one before at its own job, and a pickaxe's tier " +
             "decides which ores it can mine (an axe's: which trees, the snow's spruce needs Iron). A bow's strength is its Bow " +
             "section instead (its tier only sorts the crafting list).")]
    public ToolTier tier = ToolTier.Wood;
}
