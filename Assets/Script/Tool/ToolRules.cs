/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: Nothing to set up: a set of rules other scripts ask (it isn't put on an object).
Unity Editor: Nothing to click. The numbers live in three places:
              - Tier Bonus below (1.25 = each tier hits 25 % harder than the one before);
              - Marko's damage numbers on the Player's PlayerCombat (Hand Damage, Sword Damage, Axe Tree Damage,
                Pick Axe Stone Damage...), used as they are for a wooden tool;
              - each ore's Item Data > Mined With (the weakest pickaxe that can mine it).
Purpose: How tools work, in one place. Marko's PlayerCombat asks it on every attack (its [Tony] lines):
         - The swing a click makes: nothing (or a non-tool, like wood or food) in the hand = Punch, a sword or an axe
           = Attack, a pickaxe = Mine.
         - Which rocks a tool can mine: copper and iron need a Stone pickaxe, gold an Iron one, diamond a Gold one
           (the ore's Mined With). Hands, axes and swords only break rock that needs nothing (Stone).
         - The tier bonus: a tool hits 25 % harder per tier at its own job (sword: enemies and animals, axe: trees,
           pickaxe: rock and ores): Wood = Marko's number, Stone x1.25, Iron x1.56, Gold x1.95, Diamond x2.44.
Requirement: ToolRules.cs x-> ToolStats.cs, ItemData.cs, PlayerInventory.cs, ResourceNode.cs, PlayerAnimator.cs, NotificationFeed.cs
             ToolRules.cs <-x PlayerCombat.cs, PlayerFireball.cs, PunchCombo.cs, TierToolBuilder.cs
*/

using UnityEngine;

public static class ToolRules
{
    // ===== Fields =====
    public const float TierBonus = 1.25f;   // each tier hits this many times harder than the one before, at its own job
    const float MessageGap = 1.5f;          // seconds between two "Needs a ... Pickaxe" messages, so clicking doesn't spam

    static float nextMessage;               // when the next "Needs a ... Pickaxe" message may show

    // ===== My methods =====

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        nextMessage = 0f;
    }

    // The tool in this inventory slot. null = nothing there, or not a tool (a click punches).
    public static ToolStats ToolIn(PlayerInventory.Slot slot)
    {
        if (slot == null || slot.IsEmpty || slot.data == null || !slot.data.IsTool) return null;
        return slot.data.Tool;
    }

    // True if the slot holds a sword of any tier (e.g. Marko's fireball needs one).
    public static bool IsSword(PlayerInventory.Slot slot)
    {
        ToolStats tool = ToolIn(slot);
        return tool != null && tool.kind == ToolKind.Sword;
    }

    // The animation a click plays: Punch (no tool), Mine (pickaxe) or Attack (sword, axe). Mine and Attack both show
    // Mixamo's BotAxeAttackDownward; they stay two actions so each can have its own clip and hit timing later.
    public static PlayerAction SwingFor(ToolStats tool)
    {
        if (tool == null) return PlayerAction.Punch;
        return tool.kind == ToolKind.Pickaxe ? PlayerAction.Mine : PlayerAction.Attack;
    }

    // The name Marko's PlayerCombat checks to pick his damage numbers: "Sword", "Axe", "PickAxe", or "" (a hand).
    public static string WeaponName(ToolStats tool)
    {
        if (tool == null) return "";
        switch (tool.kind)
        {
            case ToolKind.Sword: return "Sword";
            case ToolKind.Axe: return "Axe";
            default: return "PickAxe";
        }
    }

    // How many times harder this tool hits than Marko's number: TierBonus for each tier above Wood, but only at its
    // own job (a diamond pickaxe isn't a better weapon). No tool, another job or a wooden tool = 1.
    public static float Boost(ToolStats tool, ToolKind job)
    {
        if (tool == null || tool.kind != job || tool.tier <= ToolTier.Wood) return 1f;
        return Mathf.Pow(TierBonus, (int)tool.tier - (int)ToolTier.Wood);
    }

    // Whose job a resource is: trees = the axe's, animals = the sword's, rock and ores = the pickaxe's.
    public static ToolKind JobFor(ResourceNode node)
    {
        switch (node.resourceType)
        {
            case ResourceNode.ResourceType.Tree: return ToolKind.Axe;
            case ResourceNode.ResourceType.Animal: return ToolKind.Sword;
            default: return ToolKind.Pickaxe;
        }
    }

    // The weakest pickaxe that can mine this (None = anything can, even a bare hand). Only rock can need one: it's
    // the Mined With of the item the rock drops (copper and iron: Stone, gold: Iron, diamond: Gold).
    public static ToolTier NeededFor(ResourceNode node)
    {
        if (node == null || node.resourceType != ResourceNode.ResourceType.Stone) return ToolTier.None;
        ItemData drop = node.DropItem;
        return drop != null ? drop.MinedWith : ToolTier.None;
    }

    // Can this tool (null = a bare hand) break this resource at all?
    public static bool CanMine(ToolStats tool, ResourceNode node)
    {
        ToolTier need = NeededFor(node);
        if (need == ToolTier.None) return true;
        return tool != null && tool.kind == ToolKind.Pickaxe && tool.tier >= need;
    }

    // Tells the player which pickaxe a rock needs (not more often than every MessageGap seconds).
    public static void TellCantMine(ResourceNode node)
    {
        if (Time.time < nextMessage) return;
        nextMessage = Time.time + MessageGap;
        NotificationFeed.Show("Needs a " + TierName(NeededFor(node)) + " Pickaxe or better");
    }

    // A tier as the tools' names say it: "Wooden", "Stone", "Iron", "Gold", "Diamond".
    public static string TierName(ToolTier tier)
    {
        return tier == ToolTier.Wood ? "Wooden" : tier.ToString();
    }
}
