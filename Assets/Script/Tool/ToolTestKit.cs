/*
First Date: 12/11/2026
Date: 19/11/2026
Author: Tony
Use by: The Player (next to PlayerInventory). Tools > Items > Build Tier Tools puts it there with all 15 tools.
Unity Editor: Turn the rule on: select the GameRules object > Testing > Tool Test. Ticking it while playing gives the
              tools at once. From the title screen: New Game > Testing > Start with all tools.
Purpose: For testing tools: while the game rule Tool Test is on, the player gets one of each tool (pickaxe, axe and
         sword in wood, stone, iron, gold and diamond: 15) in the inventory, when the game starts or the moment the
         rule is turned on. Tools the player already has aren't given twice, and turning the rule off takes nothing away.
Requirement: ToolTestKit.cs x-> PlayerInventory.cs, ItemData.cs, GameRules.cs
             ToolTestKit.cs <-x TierToolBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ToolTestKit : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The tools given while the game rule Tool Test is on. Tools > Items > Build Tier Tools fills this list.")]
    [SerializeField] List<ItemData> tools = new List<ItemData>();

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

    // One of each tool the player doesn't have yet.
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
    }
}
