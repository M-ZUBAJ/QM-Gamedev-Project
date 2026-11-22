/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to put on an object: Marko's PerkChest asks it which perks it may offer.
Unity Editor: Set the rules on each Perk Data (Assets/Config/Perks), under "Offered when": Requires (only offered once
              the player has picked that perk in this life) and Max Picks (1 = one at a time, 0 = no limit).
              Testing: Play and open common boxes: Precision only shows up after an epic box gave Critical Strike,
              Spring Legs only after Glide, and Long Reach (rare boxes too) only after Keen Eye. An epic box doesn't offer
              Last Stand, Critical Strike, Glide, Reroll or Keen Eye while you have it (Max Picks 1); Last Stand comes
              back once it has saved you (PlayerPerks.Lose).
Purpose: Which perks a loot box may offer right now, and picking a few of them at random:
           - CanOffer(perk): its Requires perk was picked in this life, and it was picked fewer times than Max Picks;
           - Pick(pool, count, shown): up to "count" different perks, ones not shown before first (so a reroll shows
             new perks when there are any), in a random order.
         What was picked comes from PlayerPerks, which forgets it all when the player dies (perks are lost then).
Requirement: PerkRules.cs x-> PerkData.cs, PlayerPerks.cs
             PerkRules.cs <-x PerkChest.cs
             PerkRules.cs <-o PerkRollUI.cs
*/

using System.Collections.Generic;
using UnityEngine;

public static class PerkRules
{
    // ===== My methods =====

    // May a chest offer this perk now?
    public static bool CanOffer(PerkData perk)
    {
        if (perk == null) return false;
        if (perk.requires != null && PlayerPerks.TimesPicked(perk.requires) == 0) return false;   // needs another perk first
        return perk.maxPicks <= 0 || PlayerPerks.TimesPicked(perk) < perk.maxPicks;
    }

    // Up to "count" different perks from the pool: ones not in "shown" first, then (if there aren't enough new ones)
    // ones that were shown before. The result is shuffled, so a new perk can land on any card.
    public static List<PerkData> Pick(List<PerkData> pool, int count, List<PerkData> shown = null)
    {
        List<PerkData> fresh = new List<PerkData>();
        List<PerkData> seen = new List<PerkData>();
        if (pool != null)
        {
            foreach (PerkData perk in pool)
            {
                if (perk == null || fresh.Contains(perk) || seen.Contains(perk)) continue;   // each perk once
                if (shown != null && shown.Contains(perk)) seen.Add(perk);
                else fresh.Add(perk);
            }
        }

        List<PerkData> picked = new List<PerkData>();
        TakeRandom(fresh, picked, count);
        TakeRandom(seen, picked, count);
        for (int i = picked.Count - 1; i > 0; i--)   // shuffle
        {
            int j = Random.Range(0, i + 1);
            (picked[i], picked[j]) = (picked[j], picked[i]);
        }
        return picked;
    }

    // Moves random perks from "from" to "into" until "into" holds "count" (or "from" is empty).
    static void TakeRandom(List<PerkData> from, List<PerkData> into, int count)
    {
        while (into.Count < count && from.Count > 0)
        {
            int i = Random.Range(0, from.Count);
            into.Add(from[i]);
            from.RemoveAt(i);
        }
    }
}
