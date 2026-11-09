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

        if (perks == null || perks.Count != 3)
        {
            Debug.LogError("Exactly three perks are required.");
            return false;
        }

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
                perks[i] == null)
            {
                Debug.LogError("A perk button or its data is missing.");
                return false;
            }
        }

        selectionCallback = onSelected;

        for (int i = 0; i < 3; i++)
        {
            PerkData perk = perks[i];

            perkNames[i].text = perk.displayName;
            perkDescriptions[i].text = perk.description;

            if (perkIcons[i] != null && perk.icon != null)
                perkIcons[i].sprite = perk.icon;

            perkButtons[i].onClick.RemoveAllListeners();

            perkButtons[i].onClick.AddListener(
                () => SelectPerk(perk)
            );
        }

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


    // This prevents the menu from remaining registered as open if its GameObject is destroyed
    void OnDestroy()
    {
        MenuState.SetOpen(this, false);
    }

}
