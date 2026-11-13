/*
First Date: 12/11/2026
Date: 12/11/2026
Author: Tony
Use by: Not a script you put on an object. It is the "Tool" section inside an Item Data asset whose Item Type is Tool
        (e.g. Assets/Config/Items/Tools/StonePickaxe), and the lists of tool kinds and tiers.
Unity Editor: Select a tool's Item Data > Item Type = Tool > the Tool section: Kind (Pickaxe / Axe / Sword) and Tier
              (Wood ... Diamond). Tools > Items > Build Tier Tools makes all 15 tools. An ore's Item Data uses ToolTier
              too: its Mined With (the weakest pickaxe that can mine it).
Purpose: What a tool is: its kind (what it's good at) and its tier (how strong). ToolRules turns these into damage and
         into which rocks it can mine:
         - Pickaxe: rock and ores. A higher tier mines harder ores (Wood: stone, Stone: copper and iron, Iron: gold,
           Gold: diamond).
         - Axe: chops trees faster.
         - Sword: hits enemies and animals harder.
         Each tier is 25 % stronger than the one before at the tool's own job.
Requirement: ToolStats.cs <-x ItemData.cs, PlayerCombat.cs, TierToolBuilder.cs, ToolInHand.cs, ToolRules.cs
*/

using UnityEngine;

// What a tool is for. New kinds (a hoe, a shovel...) get a new line here, and then show in the dropdown.
public enum ToolKind
{
    Pickaxe,   // rock and ores
    Axe,       // trees
    Sword,     // enemies and animals
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
    [Tooltip("What it's good at: Pickaxe = rock and ores, Axe = trees, Sword = enemies and animals.")]
    public ToolKind kind = ToolKind.Pickaxe;

    [Tooltip("How strong it is. Each tier is 25 % stronger than the one before at its own job, and a pickaxe's tier " +
             "decides which ores it can mine.")]
    public ToolTier tier = ToolTier.Wood;
}
