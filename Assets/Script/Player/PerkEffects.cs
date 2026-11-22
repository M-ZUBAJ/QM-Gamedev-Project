/*
First Date: 21/11/2026
Date: 22/11/2026
Author: Tony
Use by: The Player (next to PlayerPerks). Tools > World > Build Loot Chests puts it there. If the Player has none (e.g.
        the scene wasn't saved after that tool), one is added to it the first time a perk needs it.
Unity Editor: Critical Multiplier (2 = double damage), Sprint Stamina Saving, Last Stand's flash, Night Power's message.
              How big each perk is (+30 %, +1 coin...) is set on its Perk Data (Assets/Config/Perks > Value), not here.
              Testing: Play, click this component's ⋮ > "Test: Every New Perk Once" (or buy them from the loot boxes),
              then look at the HUD's perk icons and the inventory's Info tab. Night Power: drag the sun's Time Of Day
              (DaylightCycle on the Directional Light) past 0.77 to make it night. Keen Eye: walk up to a tree: the
              crosshair turns yellow once a punch reaches it.
Purpose: Does what the loot boxes' new perks do. Marko's PerkManager hands every perk that isn't one of his four
         (health, stamina, speed, fireball) to Apply, and each pick adds its Perk Data's Value:
           Second Wind      stamina recharges faster (+0.3 = 30 %)          PlayerStamina.SetRegenBoost
           Critical Strike  a chance (0.05 = 5 %) that a hit does double     Marko's PlayerCombat asks RollCritical;
                            damage; its wind burst turns red                 PunchEffect paints the burst
           Spring Legs      jump higher (+0.25 = 25 %)                      PlayerJump.SetJumpBoost
           Bounty Hunter    +1 coin for every monster killed                MonsterBounty asks BountyCoins
           Gourmet          food does more (+0.1 = 10 %; bad food not worse) ItemConsumer asks FoodMultiplier
           Lumberjack       +1 wood from every tree                         Turki's ResourceNode asks ExtraDrop
           Prospector       +1 stone or ore from every rock                 Turki's ResourceNode asks ExtraDrop
           Sprinter         sprint faster (+0.15 = 15 %), using less stamina PlayerController.SetSprintBoost,
                                                                            PlayerStamina.SetSprintCost
           Brawler          punches push monsters back farther (+0.1 = 10 %) PunchEffect asks KnockbackMultiplier
           Attack Speed     attacks come faster (+0.1 = 10 %)               Marko's PlayerCombat asks
                                                                            AttackSpeedMultiplier
           Last Stand       a killing blow leaves 1 health and a few seconds PlayerHealth asks TryLastStand
                            of safety (Value = seconds). Then it's used up:
                            it leaves the HUD and a box may offer it again
           Glide            hold Space in the air to float down, tap Space  switches PlayerGlide on
                            twice in the air to dive
           Vampire          heal a share (0.05 = 5 %) of the damage you      PunchEffect and Marko's FireballProjectile
                            really do to monsters and animals                hurt through WithVampire / Hurt
           Thorns           a monster that hits you takes a share (0.1 =     Marko's EnemyAttack calls Reflect
                            10 %) of that hit back
           Night Power      at night: max health, max stamina, walking      PlayerHealth / PlayerStamina.SetMaxBoost,
                            speed and melee damage +0.1 = 10 % (the         PlayerController.SetMoveBoost, Marko's
                            fireball stays the same)                        PlayerCombat asks DamageMultiplier
           Reroll           the REROLL button works at loot boxes (one at   ChestTier asks RerollUnlocked
                            a time; without it no box can reroll)
           Keen Eye         the crosshair turns yellow when the attack can  Crosshair asks KeenEye
                            reach what it points at (one at a time)
           Long Reach       the attack reaches farther (+0.1 = 10 %)        Marko's PlayerCombat asks ReachMultiplier
           Arrow Recovery   arrows stay in monsters and animals to be taken ArrowProjectile asks ArrowRecovery
                            back, a dead one drops them, and missed arrows
                            stay on the ground to be picked up (one at a
                            time)
         When the player dies every perk is lost: PlayerPerks calls ResetEffects, which undoes all of it.
         Describe(kind) is each perk's line on the HUD and the Info tab.
Requirement: PerkEffects.cs x-> PerkData.cs, PlayerPerks.cs, ResourceNode.cs, DamageNumbers.cs
             PerkEffects.cs o-> PlayerHealth.cs, PlayerStamina.cs, PlayerJump.cs, PlayerController.cs, PlayerGlide.cs, PlayerInventory.cs, NotificationFeed.cs, EnemyCombat.cs, DaylightCycle.cs, PlayerCombat.cs
             PerkEffects.cs <-x ArrowProjectile.cs, ChestTier.cs, EnemyAttack.cs, FireballProjectile.cs, LootChestBuilder.cs, PerkManager.cs, PlayerCombat.cs, ResourceNode.cs
             PerkEffects.cs <-o BowShooter.cs, Crosshair.cs, ItemConsumer.cs, MonsterBounty.cs, PlayerHealth.cs, PlayerInfoPage.cs, PlayerPerks.cs, PunchEffect.cs
*/

using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PerkEffects : MonoBehaviour
{
    // ===== Fields =====
    [Header("Critical Strike")]
    [Tooltip("How much a critical hit multiplies the damage: 2 = double.")]
    [Range(1f, 5f)]
    [SerializeField] float criticalMultiplier = 2f;

    [Header("Sprinter")]
    [Tooltip("Each Sprinter perk makes sprinting use this share less stamina (0.25 = a quarter less; two perks: 44 % less).")]
    [Range(0f, 0.9f)]
    [SerializeField] float sprintStaminaSaving = 0.25f;

    [Header("Last Stand")]
    [Tooltip("Seconds of safety after Last Stand saves the player, used when its Perk Data's Value is 0.")]
    [Range(0f, 10f)]
    [SerializeField] float defaultShieldSeconds = 2f;

    [Tooltip("The flash over the whole screen when Last Stand saves the player.")]
    [SerializeField] Color flashColor = new Color(1f, 0.8f, 0.25f, 0.55f);

    [Tooltip("Seconds the flash takes to fade away.")]
    [Range(0.1f, 3f)]
    [SerializeField] float flashSeconds = 0.9f;

    [Header("Night Power")]
    [Tooltip("On: a message (bottom left) says when Night Power wakes up at night and when it rests at dawn.")]
    [SerializeField] bool tellNightPower = true;

    // What the perks add up to in this life (each pick adds its Perk Data's Value).
    float secondWind;      // 0.3 = stamina recharges 30 % faster
    float critChance;      // 0.05 = 5 % of hits are critical
    float springLegs;      // 0.25 = jumps 25 % higher
    float gourmet;         // 0.1 = food does 10 % more
    float sprinter;        // 0.15 = sprints 15 % faster
    int sprinterPicks;     // each Sprinter also saves stamina
    float brawler;         // 0.1 = punches push 10 % farther
    float attackSpeed;     // 0.1 = attacks come 10 % faster
    int bountyCoins;       // extra coins per monster killed
    int lumberjack;        // extra wood per tree
    int prospector;        // extra stone / ore per rock
    int lastStands;        // Last Stand saves left in this life
    float shieldSeconds;   // safe seconds after a save
    bool glide;            // Glide picked
    float vampire;         // 0.05 = heal 5 % of the damage done
    float thorns;          // 0.1 = a monster takes 10 % of its hit back
    float nightPower;      // 0.1 = +10 % health, stamina, speed and melee damage at night
    bool nightActive;      // Night Power is working right now (it's night)
    bool rerollUnlocked;   // Reroll picked: the REROLL button works at loot boxes
    bool keenEye;          // Keen Eye picked: the crosshair turns yellow when the attack can reach
    float longReach;       // 0.1 = the attack reaches 10 % farther
    bool arrowRecovery;    // Arrow Recovery picked: arrows can be had back (ArrowProjectile)

    static PerkEffects current;

    PlayerHealth health;          // each can be missing: its perks then do nothing
    PlayerStamina stamina;
    PlayerJump jump;
    PlayerController controller;
    PlayerGlide glider;
    PlayerCombat combat;          // Marko's attack (Long Reach's line shows how far it reaches)

    // The player's perk effects. If the Player has none yet, one is added to it (the object with PlayerInventory) the
    // first time something asks. Null only when there's no player at all.
    public static PerkEffects Current
    {
        get
        {
            if (current != null) return current;
            current = FindFirstObjectByType<PerkEffects>();
            if (current == null && PlayerInventory.Instance != null)
                current = PlayerInventory.Instance.gameObject.AddComponent<PerkEffects>();
            return current;
        }
    }

    // The questions other scripts ask. They never add a PerkEffects: no perks yet = no effect.
    public static bool RollCritical() => current != null && current.critChance > 0f && Random.value < current.critChance;
    public static float CriticalChance => current != null ? current.critChance : 0f;
    public static float CriticalMultiplier => current != null ? current.criticalMultiplier : 2f;
    public static float FoodMultiplier => current != null ? 1f + current.gourmet : 1f;
    public static float KnockbackMultiplier => current != null ? 1f + current.brawler : 1f;
    public static int BountyCoins => current != null ? current.bountyCoins : 0;
    public static float StaminaRegenBonus => current != null ? current.secondWind : 0f;   // 0.3 = +30 %
    public static float AttackSpeedMultiplier => current != null ? 1f + current.attackSpeed : 1f;   // 1.1 = 10 % faster
    public static float DamageMultiplier => current != null && current.nightActive ? 1f + current.nightPower : 1f;   // Night Power
    public static bool NightPowerOn => current != null && current.nightActive;
    public static bool RerollUnlocked => current != null && current.rerollUnlocked;         // Reroll: ChestTier lets REROLL work
    public static bool KeenEye => current != null && current.keenEye;                       // Keen Eye: the crosshair shows the reach
    public static float ReachMultiplier => current != null ? 1f + current.longReach : 1f;   // Long Reach: 1.1 = 10 % farther
    public static bool ArrowRecovery => current != null && current.arrowRecovery;           // Arrow Recovery: arrows can be had back

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        Push();   // everything at its normal value (and a Glide put on the Player by hand stays off until the perk)
    }

    // Night Power wakes up when night starts and rests at dawn. Checked every frame, so it also follows a death at night
    // (the clock goes back to the start time) and dragging the sun's Time Of Day in the Inspector.
    void Update()
    {
        bool night = nightPower > 0f && ItIsNight;
        if (night == nightActive) return;

        nightActive = night;
        Push();
        if (tellNightPower)
        {
            NotificationFeed.Show(night ? "Night Power: +" + Percent(nightPower) + " health, stamina, speed and melee damage until dawn."
                                        : "Dawn: Night Power rests until night.", null);
        }
    }

    void OnDestroy()
    {
        if (current == this) current = null;   // static values outlive the scene, so clear them
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        current = null;
    }

    // ===== My methods =====

    // Marko's PerkManager calls this for every perk that isn't one of his four.
    public static void Apply(PerkData perk)
    {
        if (perk == null) return;
        PerkEffects effects = Current;
        if (effects != null) effects.Add(perk);
        else Debug.LogWarning("PerkEffects: there's no Player to give " + perk.displayName + " to.");
    }

    // Extra items from a broken tree or rock (Turki's ResourceNode asks it): Lumberjack / Prospector. Berries picked with
    // the key (pickedByKey) and animals get nothing extra.
    public static int ExtraDrop(ResourceNode.ResourceType type, bool pickedByKey)
    {
        if (current == null || pickedByKey) return 0;
        switch (type)
        {
            case ResourceNode.ResourceType.Tree:  return current.lumberjack;
            case ResourceNode.ResourceType.Stone: return current.prospector;
            default:                              return 0;
        }
    }

    // Hurts a target now (hurt, e.g. () => enemy.TakeDamage(10)). With the Vampire perk the player heals a share of the
    // damage it really did: a dodged hit heals nothing, a monster with 5 health left hit for 20 heals as for 5.
    // Marko's FireballProjectile uses it.
    // The damage it did pops up as a number over the target (DamageNumbers, if the player has damage numbers on).
    public static void Hurt(Component target, System.Action hurt)
    {
        System.Action withHeal = WithVampire(target, hurt);
        float before = DamageNumbers.HealthOf(target, out bool creature);
        Vector3 top = DamageNumbers.TopOf(target);   // before the hurt: a target it kills may lose its collider
        withHeal?.Invoke();
        DamageNumbers.ShowDealt(target, before, creature, top, false);
    }

    // The same hurt, wrapped so that running it (now or later) heals the player: PunchEffect runs it when the fist or
    // tool lands. Without the Vampire perk it's the hurt, unchanged. Only monsters (EnemyCombat) and animals heal, not
    // trees or rocks.
    public static System.Action WithVampire(Component target, System.Action hurt)
    {
        if (hurt == null || target == null || current == null || current.vampire <= 0f) return hurt;
        return () =>
        {
            float before = HealthOf(target);
            hurt();
            if (before <= 0f || current == null) return;   // not a monster or an animal, or it was already dead
            float after = Mathf.Max(0f, HealthOf(target));   // gone (-1) or below 0 = all of its health was taken
            current.Heal((before - after) * current.vampire);
        };
    }

    // A target's health right now: a monster's (EnemyCombat) or an animal's (ResourceNode of type Animal). -1 = neither.
    static float HealthOf(Component target)
    {
        if (target == null) return -1f;

        EnemyCombat enemy = target as EnemyCombat;
        if (enemy == null) enemy = target.GetComponentInParent<EnemyCombat>();
        if (enemy != null) return enemy.currentHealth;

        ResourceNode node = target as ResourceNode;
        if (node == null) node = target.GetComponentInParent<ResourceNode>();
        if (node != null && node.resourceType == ResourceNode.ResourceType.Animal) return node.currentHealth;
        return -1f;
    }

    // Thorns: Marko's EnemyAttack calls this right after an enemy hit the player (damage = the hit, before armor). That
    // enemy takes Thorns' share of it back (its own dodge chance still counts).
    public static void Reflect(Component attacker, float damage)
    {
        if (current == null || current.thorns <= 0f || attacker == null || damage <= 0f) return;
        EnemyCombat enemy = attacker.GetComponentInParent<EnemyCombat>();
        if (enemy == null || enemy.currentHealth <= 0f) return;
        float before = enemy.currentHealth;
        Vector3 top = DamageNumbers.TopOf(enemy);
        enemy.TakeDamage(damage * current.thorns);
        DamageNumbers.ShowDealt(enemy, before, true, top, false);   // the thorns' damage, as a number
    }

    // PlayerHealth asks this when a hit would kill the player. True = Last Stand saves them: it flashes, says so, and
    // "shield" is how many seconds nothing can hurt them. Then that Last Stand is used up: it leaves the HUD and a loot
    // box may offer it again (PlayerPerks.Lose).
    public static bool TryLastStand(out float shield)
    {
        shield = 0f;
        if (current == null || current.lastStands <= 0) return false;

        current.lastStands--;
        shield = current.shieldSeconds;
        current.StartFlash();
        NotificationFeed.Show("Last Stand! You held on with 1 health.", null);
        PlayerPerks.Lose(ChestPerkType.LastStand);
        return true;
    }

    // Loses every new perk and undoes them (PlayerPerks calls this when the player dies).
    public void ResetEffects()
    {
        secondWind = critChance = springLegs = gourmet = sprinter = brawler = attackSpeed = shieldSeconds = 0f;
        vampire = thorns = nightPower = longReach = 0f;
        sprinterPicks = bountyCoins = lumberjack = prospector = lastStands = 0;
        glide = rerollUnlocked = keenEye = arrowRecovery = false;
        nightActive = false;   // switched off here, so no "Dawn" message for a perk that was lost
        Push();
    }

    // One perk picked: adds its Value to its kind's total.
    void Add(PerkData perk)
    {
        float value = perk.value;
        switch (perk.perkType)
        {
            case ChestPerkType.SecondWind:     secondWind += value; break;
            case ChestPerkType.CriticalStrike: critChance += value; break;
            case ChestPerkType.SpringLegs:     springLegs += value; break;
            case ChestPerkType.BountyHunter:   bountyCoins += Whole(value); break;
            case ChestPerkType.Gourmet:        gourmet += value; break;
            case ChestPerkType.Lumberjack:     lumberjack += Whole(value); break;
            case ChestPerkType.Prospector:     prospector += Whole(value); break;
            case ChestPerkType.Sprinter:       sprinter += value; sprinterPicks++; break;
            case ChestPerkType.Brawler:        brawler += value; break;
            case ChestPerkType.AttackSpeed:    attackSpeed += value; break;
            case ChestPerkType.LastStand:
                lastStands++;
                shieldSeconds = Mathf.Max(shieldSeconds, value > 0f ? value : defaultShieldSeconds);
                break;
            case ChestPerkType.Glide:          glide = true; break;
            case ChestPerkType.Vampire:        vampire += value; break;
            case ChestPerkType.Thorns:         thorns += value; break;
            case ChestPerkType.NightPower:     nightPower += value; break;   // Update switches it on when it's night
            case ChestPerkType.Reroll:         rerollUnlocked = true; break;
            case ChestPerkType.KeenEye:        keenEye = true; break;
            case ChestPerkType.LongReach:      longReach += value; break;
            case ChestPerkType.ArrowRecovery:  arrowRecovery = true; break;
            default:
                Debug.LogWarning("PerkEffects doesn't know the perk kind " + perk.perkType + " (" + perk.displayName + ").");
                return;
        }
        Push();
    }

    // A count perk's Value as a whole number (at least 1, so a Value left at 0 still gives one).
    static int Whole(float value) => Mathf.Max(1, Mathf.RoundToInt(value));

    // Is it night now (the sun's DaylightCycle)? No sun = never night.
    static bool ItIsNight => DaylightCycle.Current != null && DaylightCycle.Current.IsNight;

    // Hands the totals to the scripts that use them. After a reset everything goes back to normal (x1, Glide off).
    void Push()
    {
        if (health == null) health = GetComponent<PlayerHealth>();
        if (stamina == null) stamina = GetComponent<PlayerStamina>();
        if (jump == null) jump = GetComponent<PlayerJump>();
        if (controller == null) controller = GetComponent<PlayerController>();
        if (glider == null) glider = GetComponent<PlayerGlide>();
        if (combat == null) combat = GetComponent<PlayerCombat>();

        float night = nightActive ? 1f + nightPower : 1f;   // Night Power: 1.1 at night with one pick, else normal

        if (health != null) health.SetMaxBoost(night);
        if (stamina != null)
        {
            stamina.SetRegenBoost(this, 1f + secondWind);
            stamina.SetSprintCost(Mathf.Pow(1f - sprintStaminaSaving, sprinterPicks));
            stamina.SetMaxBoost(night);
        }
        if (jump != null) jump.SetJumpBoost(1f + springLegs);
        if (controller != null)
        {
            controller.SetSprintBoost(1f + sprinter);
            controller.SetMoveBoost(night);
        }
        if (glide && glider == null) glider = gameObject.AddComponent<PlayerGlide>();
        if (glider != null) glider.enabled = glide;
    }

    // Vampire: heals the player (never a dead one: that would bring them back without respawning).
    void Heal(float amount)
    {
        if (amount <= 0f) return;
        if (health == null) health = GetComponent<PlayerHealth>();
        if (health != null && !health.IsDead) health.Heal(amount);
    }

    // A perk's line for the HUD and the Info tab (two lines at most), e.g. "+30% stamina recharge".
    public string Describe(ChestPerkType kind)
    {
        switch (kind)
        {
            case ChestPerkType.SecondWind:     return "+" + Percent(secondWind) + " stamina recharge";
            case ChestPerkType.CriticalStrike: return Percent(critChance) + " chance\nx" + criticalMultiplier.ToString("0.#", CultureInfo.InvariantCulture) + " damage";
            case ChestPerkType.SpringLegs:     return "+" + Percent(springLegs) + " jump" + (jump != null ? "\nJump " + jump.JumpHeight.ToString("0.00", CultureInfo.InvariantCulture) + " m" : "");
            case ChestPerkType.BountyHunter:   return "+" + bountyCoins + (bountyCoins == 1 ? " coin" : " coins") + " per monster";
            case ChestPerkType.Gourmet:        return "+" + Percent(gourmet) + " from food";
            case ChestPerkType.Lumberjack:     return "+" + lumberjack + " wood per tree";
            case ChestPerkType.Prospector:     return "+" + prospector + " per rock";
            case ChestPerkType.Sprinter:
                return "+" + Percent(sprinter) + " sprint speed\n-" + Percent(1f - Mathf.Pow(1f - sprintStaminaSaving, sprinterPicks)) + " sprint stamina";
            case ChestPerkType.Brawler:        return "+" + Percent(brawler) + " knockback";
            case ChestPerkType.AttackSpeed:    return "+" + Percent(attackSpeed) + " attack speed";
            case ChestPerkType.LastStand:      return lastStands > 0 ? "Ready: survive\none killing blow" : "Used up";
            case ChestPerkType.Glide:          return "Hold Space: float down\nSpace twice: dive";
            case ChestPerkType.Vampire:        return "Heal " + Percent(vampire) + " of\nthe damage you do";
            case ChestPerkType.Thorns:         return Percent(thorns) + " of each hit\ngoes back to the monster";
            case ChestPerkType.NightPower:
                return "+" + Percent(nightPower) + " to all stats\n" + (nightActive ? "ON (it's night)" : "at night");
            case ChestPerkType.Reroll:         return "REROLL works\nat loot boxes";
            case ChestPerkType.KeenEye:        return "Crosshair turns\nyellow in reach";
            case ChestPerkType.LongReach:
                return "+" + Percent(longReach) + " attack reach" +
                       (combat != null ? "\nReach " + combat.AttackReach.ToString("0.0#", CultureInfo.InvariantCulture) + " m" : "");
            case ChestPerkType.ArrowRecovery:  return "Arrows stay to be\npicked up again";
            default:                           return "";
        }
    }

    // 0.0625 -> "6.25%".
    static string Percent(float share)
    {
        return (share * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";
    }

    // A gold flash over the whole screen that fades away (its own small canvas, under the Player, so it never stays).
    void StartFlash()
    {
        if (flashSeconds > 0f && flashColor.a > 0f) StartCoroutine(Flashing());
    }

    IEnumerator Flashing()
    {
        GameObject holder = new GameObject("Last Stand Flash");
        holder.transform.SetParent(transform, false);
        Canvas canvas = holder.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;   // over the HUD and the menus, under the screen fader (1000)

        Image image = new GameObject("Flash").AddComponent<Image>();
        image.transform.SetParent(holder.transform, false);
        image.raycastTarget = false;   // never blocks a click
        RectTransform area = image.rectTransform;
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;

        for (float t = 0f; t < flashSeconds; t += Time.unscaledDeltaTime)
        {
            Color c = flashColor;
            c.a *= 1f - t / flashSeconds;
            image.color = c;
            yield return null;
        }
        Destroy(holder);
    }

    // Click the component's ⋮ menu in the Inspector while playing: gives one of each new perk the game has loaded (the
    // loot boxes' lists), as if it was bought. Marko's four aren't given (his PerkManager does those).
    [ContextMenu("Test: Every New Perk Once")]
    void TestEveryPerk()
    {
        if (!Application.isPlaying) return;
        foreach (PerkData perk in Resources.FindObjectsOfTypeAll<PerkData>())
        {
            if (perk == null || perk.perkType < ChestPerkType.SecondWind || PlayerPerks.TimesPicked(perk) > 0) continue;
            Add(perk);
            PlayerPerks.Record(perk);
        }
    }
}
