using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquippedWeaponDisplay : MonoBehaviour
{
    public GameObject swordModel;

    void Start()
    {
        UpdateWeaponDisplay();
    }

    void Update()
    {
        UpdateWeaponDisplay();
    }

    // checks if the currently selected hotbar item and turns the sword model on or off
    // depending on if it is equipped.
    void UpdateWeaponDisplay()
    {
        if (PlayerInventory.Instance == null)
        {
            swordModel.SetActive(false);
            return;
        }

        PlayerInventory.Slot item = PlayerInventory.Instance.ActiveItem;

        bool swordEquipped =
            item != null &&
            !item.IsEmpty &&
            item.id == "Sword"; // must be exact name 

        swordModel.SetActive(swordEquipped);
    }
}