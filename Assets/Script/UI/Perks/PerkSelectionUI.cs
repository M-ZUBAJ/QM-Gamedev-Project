using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkSelectionUI : MonoBehaviour
{
    public static PerkSelectionUI Instance { get; private set; }

    [Header("Selection Panel")]
    [SerializeField] GameObject selectionPanel;

    [Header("Perk Buttons")]
    [SerializeField] Button[] perkButtons = new Button[3];

    [SerializeField] Image[] perkIcons = new Image[3];

    [SerializeField] TMP_Text[] perkNames = new TMP_Text[3];

    [SerializeField] TMP_Text[] perkDescriptions = new TMP_Text[3];

    Action<PerkData> selectionCallback;

    // Remember the cursor state before opening the perk menu.
    CursorLockMode previousCursorLockState;
    bool previousCursorVisible;
    bool cursorStateSaved;


    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one PerkSelectionUI exists!");
            return;
        }

        Instance = this;

        Hide();
    }

    public bool Show(List<PerkData> perks, Action<PerkData> onSelected)
    {
        if (selectionPanel == null)
        {
            Debug.LogError("Perk Selection Panel has not been assigned.");
            return false;
        }

        // [Tony] replaced (21/11/2026): 1 to 3 perks (a loot box may have fewer left to offer); the cards without a
        // perk hide. Log: Docs/TonyEditMate/Marko/21-11-2026 Loot Boxes And New Perks.md
        // Original:
        // if (perks == null || perks.Count != 3)
        // {
        //     Debug.LogError("Exactly three perks are required.");
        //     return false;
        // }
        if (perks == null || perks.Count < 1 || perks.Count > 3)                      // [Tony] replaced
        {                                                                             // [Tony] replaced
            Debug.LogError("One to three perks are required.");                       // [Tony] replaced
            return false;                                                             // [Tony] replaced
        }                                                                             // [Tony] replaced

        if (perkButtons.Length != 3 ||
            perkIcons.Length != 3 ||
            perkNames.Length != 3 ||
            perkDescriptions.Length != 3)
        {
            Debug.LogError("Perk UI arrays must each contain three elements.");
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (perkButtons[i] == null ||
                perkNames[i] == null ||
                perkDescriptions[i] == null ||
                (i < perks.Count && perks[i] == null))   // [Tony] edited (21/11/2026): only the cards that get a perk (was: perks[i] == null)
            {
                Debug.LogError("A perk button or its data is missing.");
                return false;
            }
        }

        selectionCallback = onSelected;

        // [Tony] replaced (21/11/2026): your loop below moved into FillCards (unchanged inside), which also hides the
        // cards without a perk, so Refresh (Tony's REROLL button) can fill the cards again. Original:
        // for (int i = 0; i < 3; i++)
        // {
        //     PerkData perk = perks[i];
        //
        //     perkNames[i].text = perk.displayName;
        //     perkDescriptions[i].text = perk.description;
        //
        //     if (perkIcons[i] != null && perk.icon != null)
        //         perkIcons[i].sprite = perk.icon;
        //
        //     perkButtons[i].onClick.RemoveAllListeners();
        //
        //     perkButtons[i].onClick.AddListener(
        //         () => SelectPerk(perk)
        //     );
        // }
        FillCards(perks);                                                             // [Tony] replaced

        // Remember the current mouse settings.
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        cursorStateSaved = true;

        // Unlock and show the mouse.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Display the perk selection screen.
        selectionPanel.SetActive(true);
        IsOpen = true;

        // Register this menu with the game's menu system.
        MenuState.SetOpen(this, true);

        return true;
    }

    // [Tony] added (21/11/2026): new perks on the cards while the panel stays open (Tony's REROLL button on the epic
    // box). Like Show, but the mouse and the menu state stay as they are. False = not open, or not 1 to 3 perks.
    public bool Refresh(List<PerkData> perks)                                         // [Tony] added
    {                                                                                 // [Tony] added
        if (!IsOpen || perks == null || perks.Count < 1 || perks.Count > 3 || perks.Contains(null)) return false;   // [Tony] added
        FillCards(perks);                                                             // [Tony] added
        return true;                                                                  // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added: your card-filling loop from Show (the same steps), for 1 to 3 perks; a card without a perk hides.
    void FillCards(List<PerkData> perks)                                              // [Tony] added
    {                                                                                 // [Tony] added
        for (int i = 0; i < 3; i++)                                                   // [Tony] added
        {                                                                             // [Tony] added
            bool used = i < perks.Count;                                              // [Tony] added
            CardOf(i).SetActive(used);                                                // [Tony] added
            if (!used) continue;                                                      // [Tony] added

            PerkData perk = perks[i];                                                 // [Tony] added (your lines from here)
            perkNames[i].text = perk.displayName;                                     // [Tony] added
            perkDescriptions[i].text = perk.description;                              // [Tony] added
            if (perkIcons[i] != null && perk.icon != null)                            // [Tony] added
                perkIcons[i].sprite = perk.icon;                                      // [Tony] added
            perkButtons[i].onClick.RemoveAllListeners();                              // [Tony] added
            perkButtons[i].onClick.AddListener(() => SelectPerk(perk));               // [Tony] added
        }                                                                             // [Tony] added
    }                                                                                 // [Tony] added

    // [Tony] added: a perk's whole card = its button's parent (Tony's panel: PerkOption1..3), unless the buttons share
    // one parent (then only the button).
    GameObject CardOf(int i)                                                          // [Tony] added
    {                                                                                 // [Tony] added
        Transform card = perkButtons[i].transform.parent;                             // [Tony] added
        for (int j = 0; j < perkButtons.Length && card != null; j++)                  // [Tony] added
        {                                                                             // [Tony] added
            if (j != i && perkButtons[j] != null && perkButtons[j].transform.parent == card) card = null;   // [Tony] added
        }                                                                             // [Tony] added
        return card != null ? card.gameObject : perkButtons[i].gameObject;            // [Tony] added
    }                                                                                 // [Tony] added

    void SelectPerk(PerkData perk)
    {
        Debug.Log("Perk button clicked: " + perk.displayName);

        if (selectionCallback == null)
        {
            Debug.LogError("Perk selection callback is missing!");
            return;
        }

        Action<PerkData> callback = selectionCallback;

        Debug.Log("Closing perk selection panel.");

        Hide();

        Debug.Log("Applying selected perk.");

        callback(perk);
    }

    public void Hide()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        IsOpen = false;
        selectionCallback = null;

        // Tell the game that this menu has closed.
        MenuState.SetOpen(this, false);

        // Restore the previous mouse settings.
        if (cursorStateSaved)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;

            cursorStateSaved = false;
        }
    }

    public void TestButtonClick()
    {
        Debug.Log("SUCCESS: Unity detected the perk button click!");
    }


    // [Tony] added (10/11/2026): close when every menu is told to close (MenuState.CloseAll), e.g. the player died,
    // the same as if it was hidden normally (no perk is chosen). To undo: delete the lines marked [Tony].
    // Log: Docs/TonyEditMate/Marko/10-11-2026 Coins Reset On Death, Perk Menu Closes, Punch.md
    void OnEnable()                                                                   // [Tony] added
    {                                                                                 // [Tony] added
        MenuState.CloseAllRequested += CloseNow;                                      // [Tony] added
    }                                                                                 // [Tony] added

    void OnDisable()                                                                  // [Tony] added
    {                                                                                 // [Tony] added
        MenuState.CloseAllRequested -= CloseNow;                                      // [Tony] added
    }                                                                                 // [Tony] added

    void CloseNow()                                                                   // [Tony] added
    {                                                                                 // [Tony] added
        if (IsOpen) Hide();                                                           // [Tony] added
    }                                                                                 // [Tony] added

    // This prevents the menu from remaining registered as open if its GameObject is destroyed
    void OnDestroy()
    {
        MenuState.SetOpen(this, false);
    }

}
