/*
First Date: 21/11/2026
Date: 23/11/2026
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
         MonsterKilled tells anyone listening that the player killed a monster (PlayerStats counts them for the
         victory and death pages). A human enemy (it has HumanBrain) fires HumanKilled instead (since 23/11/2026): the
         end pages count it under "Humans killed". Its extra coins are the same as a monster's.
Requirement: MonsterBounty.cs o-> PlayerArmor.cs, PerkEffects.cs, PlayerCoins.cs, HumanBrain.cs
             MonsterBounty.cs <-x EnemyCombat.cs, PlayerStats.cs
*/

using UnityEngine;

public static class MonsterBounty
{
    // ===== Fields =====

    // Fired every time the player kills a monster (e.g. PlayerStats counts them). Listeners must unsubscribe in
    // OnDisable: this class is static, so it outlives the scene.
    public static event System.Action MonsterKilled;

    // Fired instead of MonsterKilled when the one killed was a human enemy (HumanBrain). Same rules for listeners.
    public static event System.Action HumanKilled;

    // ===== Unity messages (Unity calls these) =====

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        MonsterKilled = null;
        HumanKilled = null;
    }

    // ===== My methods =====

    // A monster (or a human enemy) was killed: gives the extra coins (if any) and returns how many. "dead" = the one
    // that died (Marko's EnemyCombat passes itself), so a human counts as a human; null = a monster.
    public static int Killed(GameObject dead = null)
    {
        PlayerArmor armor = PlayerArmor.Current;
        int coins = (armor != null ? armor.CoinsPerKill : 0) + PerkEffects.BountyCoins;
        if (coins > 0 && PlayerCoins.Instance != null) PlayerCoins.Instance.AddCoins(coins);
        if (dead != null && dead.GetComponent<HumanBrain>() != null) HumanKilled?.Invoke();
        else MonsterKilled?.Invoke();
        return coins;
    }
}
