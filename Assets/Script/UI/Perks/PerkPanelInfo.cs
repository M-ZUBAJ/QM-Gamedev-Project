/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: The perk panel's canvas (PerkSelectCanvas), next to Marko's PerkSelectionUI. Tools > UI > Build Perk Panel puts
        it there and connects it.
Unity Editor: Nothing to connect by hand (the tool fills the fields). Testing: Play, look at a chest and press E: the
              panel shows the chest's price and your coins. With too few coins the CHOOSE buttons are faded, say how
              many coins they need, and can't be clicked.
Purpose: The extras on Marko's perk panel ("CHOOSE YOUR REWARD"):
           - the price of the chest that is open (PerkChest.Showing) and how many coins the player has;
           - while the player can't pay, the three CHOOSE buttons can't be clicked and say "NEED 2 COINS", and a red
             line says how many are missing (Marko's chest takes the coins when a perk is picked);
           - the close hint names the chest's key ("E: close, or walk away");
           - the window pops in when the panel opens.
Requirement: PerkPanelInfo.cs x-> PerkSelectionUI.cs, PerkChest.cs, PlayerCoins.cs, UITween.cs
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

    // ===== Unity messages (Unity calls these) =====

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
        if (chest == shownChest && cost == shownCost && coins == shownCoins) return;   // nothing changed

        shownChest = chest;
        shownCost = cost;
        shownCoins = coins;
        bool canPay = coins >= cost;

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

    // The window grows in from a little smaller, with a small bounce.
    void PopIn()
    {
        if (window == null || popSeconds <= 0f) return;
        window.localScale = Vector3.one * 0.9f;
        UITween.Scale(this, window, Vector3.one, popSeconds, UITween.Ease.OutBack);
    }
}
