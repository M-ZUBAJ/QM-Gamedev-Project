/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to put on an object: Marko's EnemyCombat calls it when a monster dies.
Unity Editor: The numbers are set elsewhere: the armor sets' Coins Per Kill on the Player's PlayerArmor (Set Bonuses:
              copper 1, iron 2) and the Bounty Hunter Perk Data's Value (Assets/Config/Perks).
              Testing: Play with the whole copper set on and kill a slime: "+1 Coins" comes after Marko's coins.
Purpose: The extra coins for killing a monster, on top of the coins Marko's EnemyCombat gives:
           - the armor set bonus: the whole copper set +1, the whole iron set +2, the whole gold set +4
             (PlayerArmor.CoinsPerKill);
           - the rare box's Bounty Hunter perk: +1 for each one picked (PerkEffects.BountyCoins).
         Only killing counts: night monsters that burn away at dawn give nothing.
Requirement: MonsterBounty.cs o-> PlayerArmor.cs, PerkEffects.cs, PlayerCoins.cs
             MonsterBounty.cs <-x EnemyCombat.cs
*/

using UnityEngine;

public static class MonsterBounty
{
    // ===== My methods =====

    // A monster was killed: gives the extra coins (if any) and returns how many.
    public static int Killed()
    {
        PlayerArmor armor = PlayerArmor.Current;
        int coins = (armor != null ? armor.CoinsPerKill : 0) + PerkEffects.BountyCoins;
        if (coins > 0 && PlayerCoins.Instance != null) PlayerCoins.Instance.AddCoins(coins);
        return coins;
    }
}
