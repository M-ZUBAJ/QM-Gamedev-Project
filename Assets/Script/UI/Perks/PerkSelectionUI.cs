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

        selectionPanel.SetActive(true);
        IsOpen = true;

        return true;
    }

    void SelectPerk(PerkData perk)
    {
        if (selectionCallback == null) return;

        Action<PerkData> callback = selectionCallback;

        Hide();

        callback(perk);
    }

    public void Hide()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        IsOpen = false;
        selectionCallback = null;
    }
}
