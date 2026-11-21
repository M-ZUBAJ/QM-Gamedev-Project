/*
First Date: 19/11/2026
Date: 21/11/2026
Author: Tony
Use by: The perk panel's canvas (PerkSelectCanvas), next to Marko's PerkSelectionUI. Tools > UI > Build Perk Panel puts
        it there and connects it.
Unity Editor: Nothing to connect by hand (the tool fills the fields). Testing: Play, look at a chest and press E: the
              panel shows the chest's price and your coins. With too few coins the CHOOSE buttons are faded, say how
              many coins they need, and can't be clicked.
Purpose: The extras on Marko's perk panel ("CHOOSE YOUR REWARD"):
           - a loot box's name as the title, in its color ("RARE BOX": Tony's ChestTier on the chest);
           - the price of the chest that is open (PerkChest.Showing) and how many coins the player has;
           - while the player can't pay, the three CHOOSE buttons can't be clicked and say "NEED 2 COINS", and a red
             line says how many are missing (Marko's chest takes the coins when a perk is picked);
           - the close hint names the chest's key ("E: close, or walk away");
           - the REROLL button, only for a box that allows it (the epic box in the old chest system, every box in the
             new one) and only once the player has the Reroll perk (ChestTier.CanReroll): "REROLL  10 COINS" shows new
             perks for coins (each reroll at the same box costs twice as much); faded while you can't pay, and it says
             so when the box has no other perk to roll into;
           - the window pops in when the panel opens.
Requirement: PerkPanelInfo.cs x-> PerkSelectionUI.cs, PerkChest.cs, PlayerCoins.cs, UITween.cs
             PerkPanelInfo.cs o-> ChestTier.cs
             PerkPanelInfo.cs <-x PerkPanelBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PerkPanelInfo : MonoBehaviour
{
    // ===== Fields =====
    [Header("Texts (the tool fills these)")]
    [Tooltip("The open chest's price.")]
    [SerializeField] TMP_Text priceText;

    [Tooltip("How many coins the player has.")]
    [SerializeField] TMP_Text coinsText;

    [Tooltip("A red line, shown only while the player can't pay.")]
    [SerializeField] TMP_Text needText;

    [Tooltip("How to close the panel (it names the chest's key).")]
    [SerializeField] TMP_Text hintText;

    [Tooltip("The panel's title (a loot box's name goes there, in its color).")]
    [SerializeField] TMP_Text titleText;

    [Tooltip("The line under the title (a loot box's color).")]
    [SerializeField] Image titleLine;

    [Header("Reroll (a box that allows it)")]
    [Tooltip("The REROLL button: shown only for a box whose ChestTier allows rerolls.")]
    [SerializeField] Button rerollButton;

    [Tooltip("The REROLL button's text (it shows the price).")]
    [SerializeField] TMP_Text rerollLabel;

    [Header("Choose buttons (one per perk card)")]
    [SerializeField] Button[] chooseButtons = new Button[3];
    [SerializeField] TMP_Text[] chooseLabels = new TMP_Text[3];

    [Header("Look")]
    [Tooltip("The CHOOSE buttons' text while the player can pay.")]
    [SerializeField] string chooseText = "CHOOSE";

    [Tooltip("The price's color while the player can pay.")]
    [SerializeField] Color canPayColor = new Color(1f, 0.85f, 0.2f);

    [Tooltip("The price's color while the player can't pay.")]
    [SerializeField] Color cantPayColor = new Color(1f, 0.38f, 0.32f);

    [Tooltip("The box that pops in when the panel opens. Empty = no pop.")]
    [SerializeField] RectTransform window;

    [Tooltip("Seconds the pop takes.")]
    [Range(0f, 1f)]
    [SerializeField] float popSeconds = 0.25f;

    bool wasOpen;
    PerkChest shownChest;   // what the texts say right now, so they're only rewritten when something changes
    int shownCost = -1;
    int shownCoins = -1;
    int shownReroll = -2;
    bool shownRerollChanges;
    string defaultTitle;    // the title as built ("CHOOSE YOUR REWARD"), for a chest that isn't a loot box
    Color defaultTitleColor = Color.white;
    Color defaultLineColor = Color.white;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (titleText != null)
        {
            defaultTitle = titleText.text;
            defaultTitleColor = titleText.color;
        }
        if (titleLine != null) defaultLineColor = titleLine.color;
        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(Reroll);
            rerollButton.gameObject.SetActive(false);
        }
    }

    // LateUpdate: after the chest opened the panel this frame, so the first picture already shows the right numbers.
    void LateUpdate()
    {
        PerkSelectionUI ui = PerkSelectionUI.Instance;
        bool open = ui != null && ui.IsOpen;
        if (open && !wasOpen)
        {
            shownCost = shownCoins = -1;   // write everything again
            PopIn();
        }
        wasOpen = open;
        if (open) Write();
    }

    // ===== My methods =====

    void Write()
    {
        PerkChest chest = PerkChest.Showing;
        int cost = chest != null ? chest.OpeningCost : 0;
        int coins = PlayerCoins.Instance != null ? PlayerCoins.Instance.Coins : 0;
        int reroll = chest != null && chest.RerollAllowed ? chest.RerollCost : -1;   // -1 = no reroll at this chest
        bool rerollChanges = chest != null && chest.RerollWouldChange;
        if (chest == shownChest && cost == shownCost && coins == shownCoins && reroll == shownReroll &&
            rerollChanges == shownRerollChanges) return;   // nothing changed

        shownChest = chest;
        shownCost = cost;
        shownCoins = coins;
        shownReroll = reroll;
        shownRerollChanges = rerollChanges;
        bool canPay = coins >= cost;
        WriteTitle(chest != null ? chest.Tier : null);
        WriteReroll(reroll, rerollChanges, coins);

        if (priceText != null)
        {
            priceText.text = cost + (cost == 1 ? " coin" : " coins");
            priceText.color = canPay ? canPayColor : cantPayColor;
        }
        if (coinsText != null) coinsText.text = coins.ToString();
        if (needText != null)
        {
            needText.gameObject.SetActive(!canPay);
            int missing = cost - coins;
            needText.text = "Not enough coins: " + missing + (missing == 1 ? " more coin" : " more coins") + " needed.";
        }
        if (hintText != null && chest != null) hintText.text = chest.InteractKey + ": close, or walk away";

        for (int i = 0; i < chooseButtons.Length; i++)
        {
            if (chooseButtons[i] != null) chooseButtons[i].interactable = canPay;
            if (i < chooseLabels.Length && chooseLabels[i] != null)
                chooseLabels[i].text = canPay ? chooseText : "NEED " + cost + (cost == 1 ? " COIN" : " COINS");
        }
    }

    // A loot box's name and color as the title ("RARE BOX"); any other chest keeps the built title.
    void WriteTitle(ChestTier tier)
    {
        Color color = tier != null ? tier.Color : defaultTitleColor;
        if (titleText != null)
        {
            titleText.text = tier != null ? tier.BoxName.ToUpperInvariant() : defaultTitle;
            titleText.color = color;
        }
        if (titleLine != null)
            titleLine.color = tier != null ? new Color(color.r, color.g, color.b, defaultLineColor.a) : defaultLineColor;
    }

    // The REROLL button: hidden at a chest without rerolls; its price; faded while it can't be used.
    void WriteReroll(int price, bool wouldChange, int coins)
    {
        if (rerollButton == null) return;
        rerollButton.gameObject.SetActive(price >= 0);
        if (price < 0) return;
        rerollButton.interactable = wouldChange && coins >= price;
        if (rerollLabel != null)
            rerollLabel.text = wouldChange ? "REROLL  " + price + (price == 1 ? " COIN" : " COINS") : "NOTHING NEW TO ROLL";
    }

    // The REROLL button was clicked: Marko's chest pays and shows new perks (it says no if it can't).
    void Reroll()
    {
        PerkChest chest = PerkChest.Showing;
        if (chest != null && chest.TryReroll()) shownCost = -1;   // write everything again
    }

    // The window grows in from a little smaller, with a small bounce.
    void PopIn()
    {
        if (window == null || popSeconds <= 0f) return;
        window.localScale = Vector3.one * 0.9f;
        UITween.Scale(this, window, Vector3.one, popSeconds, UITween.Ease.OutBack);
    }
}
