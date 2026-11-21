/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: A perk chest prefab, next to Marko's PerkChest: Assets/Prefabs/WorldItems/Chest (the common box), Rare Chest and
        Epic Chest. Tools > World > Build Loot Chests puts it on all three and fills it in.
Unity Editor: Tier, Box Name and Color (the prompt under the crosshair and the perk panel's title), Start Price and how it
              grows (Growth Add: + Grows By each time; Multiply: x Grows By), Can Reroll (the panel's REROLL button).
              Testing: Play, buy a perk from a common box (2 coins): the next common box asks 3. Die: back to 2.
Purpose: A loot box's kind and price. Every box of one kind costs the same, and each perk bought from that kind makes
         the next one dearer:
           Common  2, 3, 4, 5...       (+1)
           Rare    10, 15, 23, 34...   (x1.5, rounded)
           Epic    100, 200, 400...    (x2)
         The prices start again when the player dies (PlayerPerks calls ResetPrices: the perks are lost too) and when a
         new game starts (a scene loads).
         Reroll (the epic box): the first reroll at a box costs a tenth of its price, each next reroll at that same box
         twice as much; another box starts again at a tenth.
Requirement: ChestTier.cs <-x LootChestBuilder.cs, PerkChest.cs, PlayerPerks.cs
             ChestTier.cs <-o ChestPrompt.cs, PerkPanelInfo.cs
*/

using UnityEngine;
using UnityEngine.SceneManagement;

// The kinds of loot box, cheapest first. New kinds go at the end (e.g. a legendary box later).
public enum LootTier
{
    Common,
    Rare,
    Epic,
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

    [Tooltip("Its color on the perk panel's title.")]
    [SerializeField] Color color = new Color(0.86f, 0.87f, 0.92f);

    [Header("Price")]
    [Tooltip("Coins the first perk of this kind costs.")]
    [Min(0)]
    [SerializeField] int startPrice = 2;

    [Tooltip("Add: each perk bought adds Grows By to the price. Multiply: each perk bought multiplies it by Grows By.")]
    [SerializeField] PriceGrowth growth = PriceGrowth.Add;

    [Tooltip("+1 (Add) for the common box, x1.5 / x2 (Multiply) for the rare / epic box.")]
    [Min(0f)]
    [SerializeField] float growBy = 1f;

    [Header("Reroll (the perk panel's REROLL button)")]
    [Tooltip("On: the perk panel shows a REROLL button for this box (new perks for coins).")]
    [SerializeField] bool canReroll;

    [Tooltip("The first reroll at a box costs this share of its price (0.1 = a tenth: 10 coins at a 100-coin box).")]
    [Range(0.01f, 1f)]
    [SerializeField] float rerollShare = 0.1f;

    [Tooltip("Each further reroll at the same box costs this many times the last one (2 = double).")]
    [Min(1f)]
    [SerializeField] float rerollGrowth = 2f;

    // Perks bought from each kind of box in this life (shared by every box: a static value outlives one chest).
    static readonly int[] bought = new int[System.Enum.GetValues(typeof(LootTier)).Length];

    int rerolls;   // rerolls done at this box (another box starts again at 0)

    public LootTier Tier => tier;
    public string BoxName => boxName;
    public Color Color => color;
    public bool CanReroll => canReroll;

    // What a perk from this box costs now.
    public int Price => PriceAfter(BoughtOf(tier));

    // What the next reroll at this box costs (at least 1 coin).
    public int RerollPrice => Mathf.Max(1, Round(Price * rerollShare * Mathf.Pow(rerollGrowth, rerolls)));

    // ===== Unity messages (Unity calls these) =====

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

    // Marko's PerkChest calls this when a perk from this box was paid for: the next box of this kind costs more.
    public void Bought()
    {
        bought[(int)tier]++;
    }

    // Marko's PerkChest calls this after a reroll at this box: the next one here costs more.
    public void Rerolled()
    {
        rerolls++;
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
}
