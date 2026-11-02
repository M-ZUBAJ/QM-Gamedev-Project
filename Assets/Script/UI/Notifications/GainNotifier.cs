/*
First Date: 2/11/2026
Date: 2/11/2026
Author: Tony
Use by: Put this on NotificationCanvas (Tools > UI > Build Notifications adds it), or on any one object in the scene.
Unity Editor: Tick which gains get a message (Items, Coins). Nothing to connect: it finds the player's inventory and
              Marko's PlayerCoins by itself, and takes the coin picture from the HUD's coin counter.
              Testing: Play and harvest a tree: "+3 Wood". Defeat an enemy: "+2 Coins".
Purpose: Tells the player what they just got, through NotificationFeed:
           Items: "+3 Wood" with the item's icon whenever items come into the inventory
                  (PlayerInventory.ItemAdded: harvesting, picking up, crafting...).
           Coins: "+5 Coins" whenever the coin total goes up (Marko's PlayerCoins.CoinsChanged, e.g. an enemy was
                  defeated). Spending coins shows nothing.
         The item icon follows GameRules' Item Icons rules, like the inventory slots (ItemIconRules).
Requirement: GainNotifier.cs x-> NotificationFeed.cs, PlayerInventory.cs, ItemIconRules.cs
             GainNotifier.cs o-> PlayerCoins.cs, CoinCounter.cs
             GainNotifier.cs <-x NotificationUIBuilder.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class GainNotifier : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Show \"+3 Wood\" when items come into the inventory (harvesting, picking up, crafting...).")]
    [SerializeField] bool items = true;

    [Tooltip("Show \"+5 Coins\" when the player gets coins (e.g. defeating an enemy).")]
    [SerializeField] bool coins = true;

    [Tooltip("The word after the coin amount.")]
    [SerializeField] string coinName = "Coins";

    [Tooltip("The coin picture in the message. Empty = the same coin as the HUD's coin counter (none without one).")]
    [SerializeField] Sprite coinIcon;

    const string CoinKey = "Coins";      // every coin gain adds up into one line
    const float SearchInterval = 0.5f;   // seconds between looks for PlayerCoins while it's missing

    PlayerCoins playerCoins;   // Marko's script on the player (null until found)
    int lastCoins;             // the total last time, to work out how many just came in
    float nextSearch;
    CoinCounter counter;       // the HUD's coin counter, for its picture (can be missing)

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        PlayerInventory.ItemAdded += OnItemAdded;
    }

    void OnDisable()
    {
        PlayerInventory.ItemAdded -= OnItemAdded;
        if (playerCoins != null) playerCoins.CoinsChanged -= OnCoinsChanged;
        playerCoins = null;
    }

    void Update()
    {
        if (playerCoins == null) FindCoins();
    }

    // ===== My methods =====

    // Items came into the inventory: "+3 Wood", with the item's picture (as the inventory shows it).
    void OnItemAdded(PlayerInventory.Slot slot, int amount)
    {
        if (!items) return;

        ItemIconRules.Resolve(slot.data, slot.icon, out Sprite icon, out _);   // Label style = no picture
        string itemName = string.IsNullOrEmpty(slot.name) ? slot.id : slot.name;
        NotificationFeed.ShowGain("Item " + slot.id, itemName, amount, icon);
    }

    // Looks for Marko's PlayerCoins, at most twice a second, until it's there (e.g. the player spawns later).
    void FindCoins()
    {
        if (Time.unscaledTime < nextSearch) return;
        nextSearch = Time.unscaledTime + SearchInterval;

        playerCoins = PlayerCoins.Instance;
        if (playerCoins == null) return;

        lastCoins = playerCoins.Coins;   // coins the player already has don't count as new
        playerCoins.CoinsChanged += OnCoinsChanged;
    }

    // The coin total changed: more than before = "+5 Coins". Less (spent) = nothing.
    void OnCoinsChanged(int total)
    {
        int gained = total - lastCoins;
        lastCoins = total;
        if (coins && gained > 0) NotificationFeed.ShowGain(CoinKey, coinName, gained, CoinIcon());
    }

    // The coin picture: the one set here, else whatever the HUD's coin counter shows.
    Sprite CoinIcon()
    {
        if (coinIcon != null) return coinIcon;
        if (counter == null) counter = FindFirstObjectByType<CoinCounter>();
        return counter != null ? counter.ShownSprite : null;
    }
}
