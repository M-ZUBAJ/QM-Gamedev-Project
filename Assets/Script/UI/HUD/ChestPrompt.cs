/*
First Date: 19/11/2026
Date: 21/11/2026
Author: Tony
Use by: The prompt under the crosshair in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > ChestPrompt > Pill >
        Key > KeyText, Label, CoinIcon, Price).
Unity Editor: Nothing to connect (the tool fills the fields). Testing: Play and look at a chest from close by (where
              E opens it): "E  Open chest  (coin) 2" shows under the crosshair. The price is red while you have fewer
              coins than that.
Purpose: Tells the player what E does and what the chest costs while the crosshair is on one of Marko's perk chests
         within its reach: the same test as the chest's E key and its yellow outline (InteractOutline). Hidden while a
         menu is open (e.g. the perk panel itself), while the player is dead, and once the chest is used.
         Looking inside is free: Marko's chest only takes the coins when a perk is picked.
Requirement: ChestPrompt.cs x-> PerkChest.cs, InteractOutline.cs, MenuState.cs, CanvasGroup (Unity)
             ChestPrompt.cs o-> PlayerCoins.cs, PlayerHealth.cs, ChestTier.cs
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

    [Tooltip("Seconds to fade in and out.")]
    [Range(0f, 0.5f)]
    [SerializeField] float fadeSeconds = 0.1f;

    const float Reach = 8f;   // meters the camera looks for a chest; each chest's own Interaction Range decides

    CanvasGroup group;
    PlayerHealth playerHealth;   // no prompt while dead (can be missing)
    PerkChest shownChest;        // what the texts say right now, so they're only rewritten when something changes
    int shownCost = -1;
    bool shownCanPay;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;   // never in the way of clicks
        group.interactable = false;
    }

    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    // LateUpdate: after the player and the camera have moved this frame.
    void LateUpdate()
    {
        PerkChest chest = LookedAtChest();
        if (chest != null) Write(chest);

        float target = chest != null ? 1f : 0f;
        group.alpha = fadeSeconds > 0f ? Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / fadeSeconds) : target;
    }

    // ===== My methods =====

    // The chest the crosshair is on, within its reach, or null.
    PerkChest LookedAtChest()
    {
        if (MenuState.AnyOpen || (playerHealth != null && playerHealth.IsDead)) return null;
        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!InteractOutline.FirstHit(ray, Reach, out RaycastHit hit)) return null;

        PerkChest chest = hit.collider.GetComponentInParent<PerkChest>();
        if (chest == null || chest.IsOpened || hit.distance > chest.InteractionRange) return null;
        return chest;
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
        if (keyText != null) keyText.text = chest.InteractKey.ToString();
        ChestTier tier = chest.Tier;   // a loot box says which kind it is: "Open rare box"
        if (label != null) label.text = tier != null ? "Open " + tier.BoxName.ToLowerInvariant() : openText;
        if (price != null)
        {
            price.text = cost.ToString();
            price.color = canPay ? canPayColor : cantPayColor;
        }
    }
}
