using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponUpgradeSystem : MonoBehaviour
{
    [Header("Upgrade Settings")]

    [Min(1)]
    [SerializeField] int killsPerUpgrade = 5;

    [Min(0f)]
    [SerializeField] float damageIncreasePercent = 2f;

    [Min(0f)]
    [SerializeField] float maxTotalBonusPercent = 20f;


    [System.Serializable]
    class WeaponProgress
    {
        public string weaponId;
        public int kills;
        public float bonusPercent;
    }

    [Header("Weapon Progress")]
    [SerializeField] List<WeaponProgress> weapons = new List<WeaponProgress>();


    public static WeaponUpgradeSystem Instance { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("There is already a WeaponUpgradeSystem in the scene.");
            return;
        }

        Instance = this;
    }


    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    WeaponProgress FindWeapon(string weaponId, bool create)
    {
        foreach (WeaponProgress weapon in weapons)
        {
            if (weapon.weaponId == weaponId)
                return weapon;
        }

        if (!create)
            return null;

        WeaponProgress newWeapon = new WeaponProgress();
        newWeapon.weaponId = weaponId;
        weapons.Add(newWeapon);

        return newWeapon;
    }


    public float GetDamageMultiplier(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId))
            return 1f;

        WeaponProgress weapon = FindWeapon(weaponId, false);

        if (weapon == null)
            return 1f;

        return 1f + weapon.bonusPercent / 100f;
    }


    public void RecordKill(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId))
            return;

        if (killsPerUpgrade < 1 || damageIncreasePercent <= 0f || maxTotalBonusPercent <= 0f)
            return;

        WeaponProgress weapon = FindWeapon(weaponId, true);

        // Stop counting once this weapon reaches its maximum bonus.
        if (weapon.bonusPercent >= maxTotalBonusPercent)
            return;

        weapon.kills++;

        if (weapon.kills >= killsPerUpgrade)
        {
            weapon.kills = 0;

            // Remember the previous bonus so we can display the actual increase.
            float previousBonus = weapon.bonusPercent;

            weapon.bonusPercent = Mathf.Min(
                weapon.bonusPercent + damageIncreasePercent,
                maxTotalBonusPercent
            );

            float actualIncrease = weapon.bonusPercent - previousBonus;

            // Find the weapon's display name in the inventory.
            string weaponName = weaponId;

            PlayerInventory inventory = PlayerInventory.Instance;

            if (inventory != null)
            {
                foreach (PlayerInventory.Slot slot in inventory.Slots)
                {
                    if (slot != null && !slot.IsEmpty && slot.id == weaponId)
                    {
                        weaponName = slot.ShownName;
                        break;
                    }
                }
            }

            // Show the upgrade notification using the existing UI system.
            string message = killsPerUpgrade + " kills! " +
                            weaponName + " damage increased by +" +
                            actualIncrease.ToString("0.##") +
                            "% (total +" +
                            weapon.bonusPercent.ToString("0.##") + "%).";

            NotificationFeed.Show(message);

            Debug.Log(message);
        }
    }
}
