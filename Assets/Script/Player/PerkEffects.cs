/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: The Player (next to PlayerPerks). Tools > World > Build Loot Chests puts it there. If the Player has none (e.g.
        the scene wasn't saved after that tool), one is added to it the first time a perk needs it.
Unity Editor: Critical Multiplier (2 = double damage), Sprint Stamina Saving, Last Stand's flash. How big each perk is
              (+30 %, +1 coin...) is set on its Perk Data (Assets/Config/Perks > Value), not here.
              Testing: Play, click this component's ⋮ > "Test: Every New Perk Once" (or buy them from the loot boxes),
              then look at the HUD's perk icons and the inventory's Info tab.
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
           Last Stand       once a life, a killing blow leaves 1 health and  PlayerHealth asks TryLastStand
                            a few seconds of safety (Value = seconds)
           Glide            hold Space in the air to float down, tap Space  switches PlayerGlide on
                            twice in the air to dive
         When the player dies every perk is lost: PlayerPerks calls ResetEffects, which undoes all of it.
         Describe(kind) is each perk's line on the HUD and the Info tab.
Requirement: PerkEffects.cs x-> PerkData.cs, PlayerPerks.cs, ResourceNode.cs
             PerkEffects.cs o-> PlayerStamina.cs, PlayerJump.cs, PlayerController.cs, PlayerGlide.cs, PlayerInventory.cs, NotificationFeed.cs
             PerkEffects.cs <-x LootChestBuilder.cs, PerkManager.cs, PlayerCombat.cs, ResourceNode.cs
             PerkEffects.cs <-o ItemConsumer.cs, MonsterBounty.cs, PlayerHealth.cs, PlayerInfoPage.cs, PlayerPerks.cs, PunchEffect.cs
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

    // What the perks add up to in this life (each pick adds its Perk Data's Value).
    float secondWind;      // 0.3 = stamina recharges 30 % faster
    float critChance;      // 0.05 = 5 % of hits are critical
    float springLegs;      // 0.25 = jumps 25 % higher
    float gourmet;         // 0.1 = food does 10 % more
    float sprinter;        // 0.15 = sprints 15 % faster
    int sprinterPicks;     // each Sprinter also saves stamina
    float brawler;         // 0.1 = punches push 10 % farther
    int bountyCoins;       // extra coins per monster killed
    int lumberjack;        // extra wood per tree
    int prospector;        // extra stone / ore per rock
    int lastStands;        // Last Stand saves left in this life
    float shieldSeconds;   // safe seconds after a save
    bool glide;            // Glide picked

    static PerkEffects current;

    PlayerStamina stamina;        // each can be missing: its perks then do nothing
    PlayerJump jump;
    PlayerController controller;
    PlayerGlide glider;

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

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        Push();   // everything at its normal value (and a Glide put on the Player by hand stays off until the perk)
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

    // PlayerHealth asks this when a hit would kill the player. True = Last Stand saves them (once a life): it flashes,
    // says so, and "shield" is how many seconds nothing can hurt them.
    public static bool TryLastStand(out float shield)
    {
        shield = 0f;
        if (current == null || current.lastStands <= 0) return false;

        current.lastStands--;
        shield = current.shieldSeconds;
        current.StartFlash();
        NotificationFeed.Show("Last Stand! You held on with 1 health.", null);
        return true;
    }

    // Loses every new perk and undoes them (PlayerPerks calls this when the player dies).
    public void ResetEffects()
    {
        secondWind = critChance = springLegs = gourmet = sprinter = brawler = shieldSeconds = 0f;
        sprinterPicks = bountyCoins = lumberjack = prospector = lastStands = 0;
        glide = false;
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
            case ChestPerkType.LastStand:
                lastStands++;
                shieldSeconds = Mathf.Max(shieldSeconds, value > 0f ? value : defaultShieldSeconds);
                break;
            case ChestPerkType.Glide:          glide = true; break;
            default:
                Debug.LogWarning("PerkEffects doesn't know the perk kind " + perk.perkType + " (" + perk.displayName + ").");
                return;
        }
        Push();
    }

    // A count perk's Value as a whole number (at least 1, so a Value left at 0 still gives one).
    static int Whole(float value) => Mathf.Max(1, Mathf.RoundToInt(value));

    // Hands the totals to the scripts that use them. After a reset everything goes back to normal (x1, Glide off).
    void Push()
    {
        if (stamina == null) stamina = GetComponent<PlayerStamina>();
        if (jump == null) jump = GetComponent<PlayerJump>();
        if (controller == null) controller = GetComponent<PlayerController>();
        if (glider == null) glider = GetComponent<PlayerGlide>();

        if (stamina != null)
        {
            stamina.SetRegenBoost(this, 1f + secondWind);
            stamina.SetSprintCost(Mathf.Pow(1f - sprintStaminaSaving, sprinterPicks));
        }
        if (jump != null) jump.SetJumpBoost(1f + springLegs);
        if (controller != null) controller.SetSprintBoost(1f + sprinter);
        if (glide && glider == null) glider = gameObject.AddComponent<PlayerGlide>();
        if (glider != null) glider.enabled = glide;
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
            case ChestPerkType.LastStand:      return lastStands > 0 ? "Ready: survive\none killing blow" : "Used in this life";
            case ChestPerkType.Glide:          return "Hold Space: float down\nSpace twice: dive";
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
