/*
First Date: 12/11/2026
Date: 22/11/2026
Author: Tony
Use by: The Player (next to PlayerInventory). Tools > Items > Build Tier Tools puts it there with all 15 tools.
Unity Editor: Turn the rule on: select the GameRules object > Testing > Tool Test. Ticking it while playing gives the
              tools at once. From the title screen: New Game > Testing > Start with all tools.
Purpose: For testing tools: while the game rule Tool Test is on, the player gets one of each tool (pickaxe, axe and
         sword in wood, stone, iron, gold and diamond: 15) in the inventory, when the game starts or the moment the
         rule is turned on. Tools the player already has aren't given twice, and turning the rule off takes nothing away.
         Armor has its own rule since 20/11/2026: Armor Test (ArmorTestKit); Tools > Items > Build Plate Armor moves
         the armor it had put in this list over there.
         Arrows (22/11/2026, Tools > Items > Build Bow And Arrows fills Arrows): each kind is topped up to Arrows Each
         (32), so a bow in the tools can shoot at once.
Requirement: ToolTestKit.cs x-> PlayerInventory.cs, ItemData.cs, GameRules.cs
             ToolTestKit.cs <-x ArmorBuilder.cs, BowBuilder.cs, TierToolBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ToolTestKit : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The items given while the game rule Tool Test is on: the tools (Tools > Items > Build Tier Tools). The " +
             "armor is given by Armor Test (ArmorTestKit).")]
    [SerializeField] List<ItemData> tools = new List<ItemData>();

    [Tooltip("The arrows given while Tool Test is on (Tools > Items > Build Bow And Arrows fills it): each kind is topped " +
             "up to Arrows Each.")]
    [SerializeField] List<ItemData> arrows = new List<ItemData>();

    [Tooltip("How many of each arrow the test gives (a player who has more keeps them).")]
    [Min(1)]
    [SerializeField] int arrowsEach = 32;

    bool wasOn;     // the rule's value last time we looked, so turning it on (not just being on) gives the tools
    bool started;   // the New Game page sets the rule before Start: the tools are given in Start, once everything is ready

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        GameRules.Changed += OnRulesChanged;
    }

    void OnDisable()
    {
        GameRules.Changed -= OnRulesChanged;
    }

    void Start()
    {
        started = true;
        OnRulesChanged();   // the rule may be on from the start
    }

    // ===== My methods =====

    void OnRulesChanged()
    {
        if (!started) return;   // Start gives them
        bool on = GameRules.ToolTest;
        if (on && !wasOn) Give();
        wasOn = on;
    }

    // One of each tool the player doesn't have yet, and each arrow topped up to Arrows Each.
    public void Give()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null) return;

        foreach (ItemData tool in tools)
        {
            if (tool == null || inventory.Count(tool.Id) > 0) continue;
            if (!inventory.Add(tool.Id, tool.DisplayName, tool.GetIcon(), 1, tool))
            {
                Debug.Log("Tool Test: the inventory is full, so not every test tool fit.");
                return;
            }
        }
        foreach (ItemData arrow in arrows)
        {
            int missing = arrow != null ? arrowsEach - inventory.Count(arrow.Id) : 0;
            if (missing <= 0) continue;
            if (!inventory.Add(arrow.Id, arrow.DisplayName, arrow.GetIcon(), missing, arrow))
            {
                Debug.Log("Tool Test: the inventory is full, so not every test arrow fit.");
                return;
            }
        }
    }
}
