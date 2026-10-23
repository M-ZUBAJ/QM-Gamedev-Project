
using UnityEngine;

public class EquippedWeaponDisplay : MonoBehaviour
{
    public GameObject swordModel;
    public GameObject axeModel;
    public GameObject pickAxeModel;

    void Start()
    {
        UpdateWeaponDisplay();
    }

    void Update()
    {
        UpdateWeaponDisplay();
    }

    void UpdateWeaponDisplay()
    {
        if (PlayerInventory.Instance == null)
        {
            swordModel.SetActive(false);
            axeModel.SetActive(false);
            pickAxeModel.SetActive(false);
            return;
        }

        PlayerInventory.Slot item = PlayerInventory.Instance.ActiveItem;

        bool swordEquipped =
            item != null && !item.IsEmpty && item.id == "Sword";

        bool axeEquipped =
            item != null && !item.IsEmpty && item.id == "Axe";

        bool pickAxeEquipped =
            item != null && !item.IsEmpty && item.id == "PickAxe";

        swordModel.SetActive(swordEquipped);
        axeModel.SetActive(false);// should be axeEquipped
        pickAxeModel.SetActive(false); // should be pickAxeEquipped
    }
}
