/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: A perk chest prefab, next to Marko's PerkChest: Assets/Prefabs/WorldItems/Chest (the common box), Rare Chest,
        Epic Chest and Legendary Chest. Tools > World > Build Loot Chests puts it on all four and fills it in.
Unity Editor: Tier, Box Name and Color (the prompt under the crosshair, the perk panel's title and the burst), Start Price
              and how it grows (Growth Add: + Grows By each time; Multiply: x Grows By), Can Reroll (the old system's
              REROLL button), Reroll Needs Perk (on: REROLL only works once the player has the Reroll perk).
              New chest system (GameRules > New Chests, picked on the New Game page): Random In New System (a slot-machine
              pop-up rolls one perk instead of choose 1 of 3), Reroll In New System, New System Perks (empty = the
              PerkChest's Available Perks), New System Only (the legendary box: gone in the old system).
              Vanish Particles, Burst Count, Burst Speed: the burst in Color when a perk is bought and the box disappears.
              Testing: Play, buy a perk from a common box (2 coins): the next common box asks 3. Die: back to 2.
              Wear the whole gold armor set: every box is 10 % cheaper (a 10-coin box asks 9; the panel shows the old
              price crossed out).
Purpose: A loot box's kind, price and how it works in each chest system. Every box of one kind costs the same, and each
         perk bought from that kind makes the next one dearer:
           Common     2, 3, 4, 5...       (+1)
           Rare       10, 15, 23, 34...   (x1.5, rounded)
           Epic       100, 200, 400...    (x2)
           Legendary  150, 300, 600...    (x2, the new system only)
         The prices start again when the player dies (PlayerPerks calls ResetPrices: the perks are lost too) and when a
         new game starts (a scene loads).
         The whole gold armor set takes 10 % off every price (PlayerArmor.ChestDiscount), rerolls too: rounded down, so
         even a cheap box gets cheaper (2 -> 1, 10 -> 9, 100 -> 90), never under 1 coin. FullPrice = before the discount.
         Two chest systems (GameRules > New Chests):
           Old: every box lets you choose 1 of 3 from its PerkChest's Available Perks; REROLL only where Can Reroll is
                on (the epic box).
           New: a box with Random In New System (common, epic) rolls ONE random perk in a pop-up (PerkRollUI: BUY or
                REROLL); the others (rare, legendary) let you choose 1 of 3. Every box can REROLL. A box with New System
                Perks offers those (the common and rare boxes: the old common and rare perks together).
         Reroll: in both systems the REROLL button only works once the player has the Reroll perk (the epic box offers
         it, and the legendary box in the new system; PerkEffects.RerollUnlocked), while Reroll Needs Perk is on. The
         first reroll at a box costs a tenth of its price, each next reroll at that same box twice as much; another box
         starts again at a tenth.
         When a perk is bought (Marko's PerkChest calls Bought) the box bursts into particles in its color (common gold,
         rare blue, epic purple, legendary orange) and disappears.
Requirement: ChestTier.cs x-> GameRules.cs, PerkData.cs, PerkEffects.cs
             ChestTier.cs o-> PlayerArmor.cs
             ChestTier.cs <-x LootChestBuilder.cs, PerkChest.cs, PlayerPerks.cs
             ChestTier.cs <-o ChestPrompt.cs, PerkPanelInfo.cs, PerkRollUI.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The kinds of loot box, cheapest first. New kinds go at the end (the saved boxes keep their kind).
public enum LootTier
{
    Common,
    Rare,
    Epic,
    Legendary,
}

// How a price grows after each perk bought: add a number, or multiply by one.
public enum PriceGrowth
{
    Add,
    Multiply,
}

[DisallowMultipleComponent]
public class ChestTier : MonoBehaviour
{
    // ===== Fields =====
    [Header("Kind")]
    [Tooltip("Which kind of box this is. Every box of a kind shares one price.")]
    [SerializeField] LootTier tier = LootTier.Common;

    [Tooltip("Its name in the prompt under the crosshair (\"Open rare box\") and on the perk panel.")]
    [SerializeField] string boxName = "Common box";

    [Tooltip("Its color: the perk panel's title and the burst when a perk from it is bought.")]
    [SerializeField] Color color = new Color(1f, 0.78f, 0.25f);

    [Header("Price")]
    [Tooltip("Coins the first perk of this kind costs.")]
    [Min(0)]
    [SerializeField] int startPrice = 2;

    [Tooltip("Add: each perk bought adds Grows By to the price. Multiply: each perk bought multiplies it by Grows By.")]
    [SerializeField] PriceGrowth growth = PriceGrowth.Add;

    [Tooltip("+1 (Add) for the common box, x1.5 / x2 (Multiply) for the rare / epic and legendary box.")]
    [Min(0f)]
    [SerializeField] float growBy = 1f;

    [Header("Reroll (the REROLL button)")]
    [Tooltip("Old chest system: on = the perk panel shows a REROLL button for this box (new perks for coins).")]
    [SerializeField] bool canReroll;

    [Tooltip("On: REROLL (in both chest systems) only works once the player has the Reroll perk, from an epic or " +
             "legendary box. Off: it works from the start.")]
    [SerializeField] bool rerollNeedsPerk = true;

    [Tooltip("The first reroll at a box costs this share of its price (0.1 = a tenth: 10 coins at a 100-coin box).")]
    [Range(0.01f, 1f)]
    [SerializeField] float rerollShare = 0.1f;

    [Tooltip("Each further reroll at the same box costs this many times the last one (2 = double).")]
    [Min(1f)]
    [SerializeField] float rerollGrowth = 2f;

    [Header("New chest system (GameRules > New Chests)")]
    [Tooltip("On: in the new system this box rolls ONE random perk in a pop-up (PerkRollUI: BUY or REROLL) instead of " +
             "letting you choose 1 of 3 (the common and epic boxes).")]
    [SerializeField] bool randomInNewSystem;

    [Tooltip("On: the REROLL button works at this box in the new system (every box, as built).")]
    [SerializeField] bool rerollInNewSystem = true;

    [Tooltip("The perks this box offers in the new system (the common and rare boxes: the old common and rare perks " +
             "together). Empty = the same as its PerkChest's Available Perks.")]
    [SerializeField] List<PerkData> newSystemPerks = new List<PerkData>();

    [Tooltip("On: this box is only in the new system (the legendary box). In the old system it removes itself as the " +
             "game starts.")]
    [SerializeField] bool newSystemOnly;

    [Header("Burst when a perk is bought")]
    [Tooltip("The particles that burst in Color where the box was: Assets/Prefabs/Effects/DeathParticles.prefab " +
             "(Tools > Effects > Build Death Particles). Empty = no burst.")]
    [SerializeField] ParticleSystem vanishParticles;

    [Tooltip("How many particles (a bigger box: more).")]
    [Range(0, 500)]
    [SerializeField] int burstCount = 140;

    [Tooltip("How fast they fly out, in meters per second.")]
    [Range(0f, 8f)]
    [SerializeField] float burstSpeed = 2.6f;

    [Tooltip("Which Build Loot Chests settings this box has (the tool brings an older box up to date once).")]
    [SerializeField, HideInInspector] int toolVersion;

    // Perks bought from each kind of box in this life (shared by every box: a static value outlives one chest).
    static readonly int[] bought = new int[System.Enum.GetValues(typeof(LootTier)).Length];

    int rerolls;   // rerolls done at this box (another box starts again at 0)

    public LootTier Tier => tier;
    public string BoxName => boxName;
    public Color Color => color;

    // Which chest system the game uses (the New Game page picks it): true = the new one.
    public static bool NewSystem => GameRules.NewChests;

    // This box rolls one random perk in the pop-up (new system), instead of choose 1 of 3.
    public bool RandomRoll => NewSystem && randomInNewSystem;

    // The REROLL button works here: in the new system every box as built, in the old one only Can Reroll (epic); in both
    // only once the player has the Reroll perk (while Reroll Needs Perk is on).
    public bool CanReroll => (NewSystem ? rerollInNewSystem : canReroll) && (!rerollNeedsPerk || PerkEffects.RerollUnlocked);

    public bool NewSystemOnly => newSystemOnly;

    // What a perk from this box costs now: with the worn armor's discount taken off (the whole gold set: 10 % off).
    public int Price => Discounted(FullPrice);

    // What it would cost without any discount (the perk panel shows it crossed out while there is one).
    public int FullPrice => PriceAfter(BoughtOf(tier));

    // The worn armor's chest discount now: 0.1 = 10 % off (the whole gold set), 0 = none.
    public static float Discount
    {
        get
        {
            PlayerArmor armor = PlayerArmor.Current;
            return armor != null ? armor.ChestDiscount : 0f;
        }
    }

    // What the next reroll at this box costs (at least 1 coin).
    public int RerollPrice => Mathf.Max(1, Round(Price * rerollShare * Mathf.Pow(rerollGrowth, rerolls)));

    // ===== Unity messages (Unity calls these) =====

    // The legendary box is only in the new system: in the old one it goes away before anyone sees it.
    // (Start, not Awake: the New Game page's choice is put into GameRules after the scene's objects wake up.)
    void Start()
    {
        if (newSystemOnly && !NewSystem)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so the prices start fresh every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        ResetPrices();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // A new game (the title screen's START GAME, Play Again) starts with the start prices too: the prices are static, so
    // they would otherwise carry over from the last game.
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetPrices();
    }

    // ===== My methods =====

    // Marko's PerkChest calls this when a perk from this box was paid for: the next box of this kind costs more, and
    // this one bursts into particles in its color (it disappears right after).
    public void Bought()
    {
        bought[(int)tier]++;
        Burst();
    }

    // Marko's PerkChest calls this after a reroll at this box: the next one here costs more.
    public void Rerolled()
    {
        rerolls++;
    }

    // The perks this box offers now: its New System Perks in the new system (when it has any), else "available" (its
    // PerkChest's Available Perks).
    public List<PerkData> PerksFor(List<PerkData> available)
    {
        return NewSystem && newSystemPerks != null && newSystemPerks.Count > 0 ? newSystemPerks : available;
    }

    // How many perks were bought from this kind of box in this life.
    public static int BoughtOf(LootTier kind)
    {
        int i = (int)kind;
        return i >= 0 && i < bought.Length ? bought[i] : 0;
    }

    // Every kind back to its start price (PlayerPerks calls it when the player dies and loses the perks).
    public static void ResetPrices()
    {
        System.Array.Clear(bought, 0, bought.Length);
    }

    // A price with the armor's discount taken off, rounded down so even a cheap box gets cheaper (2 -> 1, 10 -> 9), but
    // never under 1 coin (a free box stays free).
    static int Discounted(int price)
    {
        float discount = Discount;
        if (discount <= 0f || price <= 1) return price;
        return Mathf.Max(1, (int)System.Math.Floor(price * (1.0 - discount) + 0.0001));   // + a hair: 10 x 0.9 is 9, not 8.99999
    }

    // The price after "count" perks bought.
    int PriceAfter(int count)
    {
        float price = growth == PriceGrowth.Add ? startPrice + growBy * count : startPrice * Mathf.Pow(growBy, count);
        return Round(price);
    }

    // 22.5 -> 23 (Unity's RoundToInt would give 22: it rounds halves to the even number).
    static int Round(float value)
    {
        return (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    // The box bursts into particles in its color (deep to light), all over where it stood, flying out and up.
    // A copy of Vanish Particles, not a child: it stays while the box is removed, then removes itself.
    void Burst()
    {
        if (vanishParticles == null || burstCount <= 0) return;

        Bounds box = WorldBox();
        ParticleSystem ps = Instantiate(vanishParticles, box.center, Quaternion.identity);
        ParticleSystem.MainModule main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.45f));
        ps.Play();   // its own emission is off: only Emit makes particles

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < burstCount; i++)
        {
            Vector3 point = new Vector3(Random.Range(box.min.x, box.max.x), Random.Range(box.min.y, box.max.y),
                                        Random.Range(box.min.z, box.max.z));
            Vector3 outward = point - box.center;
            outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Random.onUnitSphere;
            emit.position = point;
            emit.velocity = (outward * Random.Range(0.4f, 1f) + Vector3.up * 0.5f) * burstSpeed;
            emit.startSize = Random.Range(0.06f, 0.15f);   // a little bigger than a creature's dots: the box is solid
            ps.Emit(emit, 1);
        }
        Destroy(ps.gameObject, Mathf.Max(2f, main.startLifetime.constantMax + 0.5f));
    }

    // The box's space in the world: its collider (it fits the box), else its pictures together, else a 1 m cube.
    Bounds WorldBox()
    {
        Collider body = GetComponent<Collider>();
        if (body != null && body.enabled) return body.bounds;

        Bounds box = new Bounds(transform.position + Vector3.up * 0.5f, Vector3.one);
        bool found = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!found) box = r.bounds;
            else box.Encapsulate(r.bounds);
            found = true;
        }
        return box;
    }
}
