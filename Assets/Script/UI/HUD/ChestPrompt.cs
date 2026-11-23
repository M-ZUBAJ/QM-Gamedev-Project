/*
First Date: 19/11/2026
Date: 23/11/2026
Author: Tony
Use by: The prompt under the crosshair in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > ChestPrompt > Pill >
        Key > KeyText, Label, CoinIcon, Price).
Unity Editor: Nothing to connect (the tool fills the fields). Testing: Play and look at a chest from close by (where
              E opens it): "E  Open common box  (coin) 2" shows under the crosshair (the box's name: common, rare, epic
              or legendary). The price is red while you have fewer coins than that. Look at a berry bush from close by:
              "E  Pick berries  x4", or on a picked one "E  Berries grow back in  0:42" (counting down to the next).
Purpose: Tells the player what E does while the crosshair is on something E is for, within its reach:
         - one of Marko's perk chests: what it costs (the same test as the chest's E key and its yellow outline,
           InteractOutline). Looking inside is free: Marko's chest only takes the coins when a perk is picked. Not
           shown once the chest is used.
         - a berry bush (Tony's BerryBush, since 23/11/2026): how many berries E picks, or how long until the next one
           grows (no coin there).
         Hidden while a menu is open (e.g. the perk panel itself) and while the player is dead.
Requirement: ChestPrompt.cs x-> PerkChest.cs, InteractOutline.cs, MenuState.cs, KeyBindings.cs, CanvasGroup (Unity)
             ChestPrompt.cs o-> PlayerCoins.cs, PlayerHealth.cs, ChestTier.cs, BerryBush.cs
             ChestPrompt.cs <-x HUDBuilder.cs
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class ChestPrompt : MonoBehaviour
{
    // ===== Fields =====
    [Header("Texts (the tool fills these)")]
    [Tooltip("The key's letter, e.g. E (read from the chest).")]
    [SerializeField] TMP_Text keyText;

    [Tooltip("What the key does.")]
    [SerializeField] TMP_Text label;

    [Tooltip("The chest's price.")]
    [SerializeField] TMP_Text price;

    [Header("Look")]
    [Tooltip("The words next to the key.")]
    [SerializeField] string openText = "Open chest";

    [Tooltip("The price's color while the player has enough coins.")]
    [SerializeField] Color canPayColor = new Color(1f, 0.85f, 0.2f);

    [Tooltip("The price's color while the player has fewer coins than that.")]
    [SerializeField] Color cantPayColor = new Color(1f, 0.38f, 0.32f);

    [Tooltip("The words next to the key on a berry bush with berries (then how many: x4).")]
    [SerializeField] string pickText = "Pick berries";

    [Tooltip("The words on a berry bush with none yet (then the time until the next one grows: 0:42).")]
    [SerializeField] string growingText = "Berries grow back in";

    [Tooltip("The countdown's color on a berry bush with none yet.")]
    [SerializeField] Color waitColor = new Color(0.82f, 0.84f, 0.88f);

    [Tooltip("Seconds to fade in and out.")]
    [Range(0f, 0.5f)]
    [SerializeField] float fadeSeconds = 0.1f;

    const float Reach = 8f;   // meters the camera looks for a chest; each chest's own Interaction Range decides

    CanvasGroup group;
    PlayerHealth playerHealth;   // no prompt while dead (can be missing)
    GameObject coinIcon;         // the coin before the price: hidden on a berry bush (found by name)
    PerkChest shownChest;        // what the texts say right now, so they're only rewritten when something changes
    int shownCost = -1;
    bool shownCanPay;
    BerryBush shownBush;
    int shownBerries = -1;
    int shownSeconds = -1;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;   // never in the way of clicks
        group.interactable = false;
        Transform coin = FindPart(transform, "CoinIcon");
        coinIcon = coin != null ? coin.gameObject : null;
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    // LateUpdate: after the player and the camera have moved this frame.
    void LateUpdate()
    {
        LookedAt(out PerkChest chest, out BerryBush bush);
        if (chest != null) Write(chest);
        else if (bush != null) Write(bush);

        float target = chest != null || bush != null ? 1f : 0f;
        group.alpha = fadeSeconds > 0f ? Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / fadeSeconds) : target;
    }

    // ===== My methods =====

    // The chest or berry bush the crosshair is on, within its reach (both null when it's on neither).
    void LookedAt(out PerkChest chest, out BerryBush bush)
    {
        chest = null;
        bush = null;
        if (MenuState.AnyOpen || (playerHealth != null && playerHealth.IsDead)) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!InteractOutline.FirstHit(ray, Reach, out RaycastHit hit)) return;

        chest = hit.collider.GetComponentInParent<PerkChest>();
        if (chest != null && (chest.IsOpened || hit.distance > chest.InteractionRange)) chest = null;
        if (chest != null) return;
        bush = hit.collider.GetComponentInParent<BerryBush>();   // the same reach as its E key (Turki's Collect Range)
        if (bush != null && hit.distance > bush.PickRange) bush = null;
    }

    // Fills in the key, the words and the price (red if the player can't pay it yet).
    void Write(PerkChest chest)
    {
        int cost = chest.OpeningCost;
        bool canPay = PlayerCoins.Instance == null || PlayerCoins.Instance.Coins >= cost;
        if (chest == shownChest && cost == shownCost && canPay == shownCanPay) return;   // nothing changed

        shownChest = chest;
        shownCost = cost;
        shownCanPay = canPay;
        shownBush = null;   // a bush looked at next is written again
        if (coinIcon != null) coinIcon.SetActive(true);
        if (keyText != null) keyText.text = chest.InteractKey.ToString();
        ChestTier tier = chest.Tier;   // a loot box says which kind it is: "Open rare box"
        if (label != null) label.text = tier != null ? "Open " + tier.BoxName.ToLowerInvariant() : openText;
        if (price != null)
        {
            price.text = cost.ToString();
            price.color = canPay ? canPayColor : cantPayColor;
        }
    }

    // A berry bush: "E  Pick berries  x4", or how long until its next berry grows ("E  Berries grow back in  0:42").
    void Write(BerryBush bush)
    {
        int count = bush.Berries;
        int seconds = count > 0 ? 0 : Mathf.CeilToInt(bush.SecondsToNextBerry);
        if (bush == shownBush && count == shownBerries && seconds == shownSeconds) return;   // nothing changed

        shownBush = bush;
        shownBerries = count;
        shownSeconds = seconds;
        shownChest = null;   // a chest looked at next is written again
        if (coinIcon != null) coinIcon.SetActive(false);   // berries are free
        if (keyText != null) keyText.text = KeyBindings.Interact.ToString();
        if (label != null) label.text = count > 0 ? pickText : growingText;
        if (price != null)
        {
            price.text = count > 0 ? "x" + count : seconds / 60 + ":" + (seconds % 60).ToString("00");
            price.color = count > 0 ? canPayColor : waitColor;
        }
    }

    // A child at any depth with this name (null = none).
    static Transform FindPart(Transform parent, string partName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == partName) return child;
            Transform deeper = FindPart(child, partName);
            if (deeper != null) return deeper;
        }
        return null;
    }
}
