/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: The inventory's Info page (InventoryCanvas > InventoryWindow > Pages > InfoPage). Tools > UI > Build Inventory UI
        (or Tools > UI > Add Inventory Scripts for an inventory built before) makes the page and puts this on it.
Unity Editor: Nothing to connect: it finds the player's scripts by itself, and its parts by NAME:
                HealthRow, HungerRow, StaminaRow, SpeedRow, SprintRow, JumpRow, FireballRow, CoinsRow:
                  each with a "Value" text, a "Bonus" text and, for Health / Hunger / Stamina, a "Bar" with a "Fill"
                PerkList: the perk rows go in here, copied from its switched-off "PerkTemplate" (Icon, Name, Stats)
                NoPerks: the text shown while the player has no perk
              Testing: Play, press Tab and click Info. Open a chest and take a perk: its stat shows the bonus in green
              and the perk joins the list.
Purpose: The player's status, updated 4 times a second while the Info tab is open:
           Health, Hunger, Stamina    now / max, with a bar
           Walk / Sprint speed        meters per second
           Jump height, Fireball (locked, or its cooldown), Coins
         What perks added shows in green next to the stat, e.g. "+44 from perks" (max health above its start) or
         "+15% from perks" (walk speed), and on the right the perks the player has, with their icons, names and
         stats (PlayerPerks).
Requirement: PlayerInfoPage.cs o-> PlayerHealth.cs, PlayerHunger.cs, PlayerStamina.cs, PlayerController.cs, PlayerJump.cs, PlayerFireball.cs, PlayerCoins.cs, PlayerPerks.cs
             PlayerInfoPage.cs <-x InventoryUIBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;   // Image

[DisallowMultipleComponent]
public class PlayerInfoPage : MonoBehaviour
{
    // One stat row's parts.
    class Row
    {
        public TMP_Text value;      // e.g. "85 / 144"
        public TMP_Text bonus;      // e.g. "+44 from perks"
        public RectTransform fill;  // the bar's fill (only Health / Hunger / Stamina): its right edge = how full
    }

    // ===== Fields =====
    [Tooltip("The color of what perks added, e.g. \"+44 from perks\".")]
    [SerializeField] Color bonusColor = new Color(0.1f, 0.58f, 0.27f);

    [Tooltip("The color of a hint that isn't a bonus, e.g. how to unlock the fireball.")]
    [SerializeField] Color hintColor = new Color(0.42f, 0.43f, 0.48f);

    const float RefreshEvery = 0.25f;   // seconds: 4 times a second, so the bars follow while the page is open

    Row health, hunger, stamina, walk, sprint, jump, fireball, coins;
    Transform perkList;
    GameObject perkTemplate;
    GameObject noPerks;
    readonly List<GameObject> perkRows = new List<GameObject>();

    // The player's scripts (each can be missing: its row then shows "-").
    PlayerHealth playerHealth;
    PlayerHunger playerHunger;
    PlayerStamina playerStamina;
    PlayerController controller;
    PlayerJump playerJump;
    PlayerFireball fireballSkill;   // Marko's
    PlayerPerks perks;
    float nextRefresh;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        health = FindRow("HealthRow");
        hunger = FindRow("HungerRow");
        stamina = FindRow("StaminaRow");
        walk = FindRow("SpeedRow");
        sprint = FindRow("SprintRow");
        jump = FindRow("JumpRow");
        fireball = FindRow("FireballRow");
        coins = FindRow("CoinsRow");

        perkList = FindDeep(transform, "PerkList");
        Transform template = FindDeep(transform, "PerkTemplate");
        perkTemplate = template != null ? template.gameObject : null;
        if (perkTemplate != null) perkTemplate.SetActive(false);
        Transform none = FindDeep(transform, "NoPerks");
        noPerks = none != null ? none.gameObject : null;
    }

    // The tab opened: up to date at once.
    void OnEnable()
    {
        nextRefresh = 0f;
        Refresh();
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + RefreshEvery;
        Refresh();
    }

    // ===== My methods =====

    void Refresh()
    {
        FindPlayer();

        if (playerHealth != null) ShowAmount(health, playerHealth.Health, playerHealth.MaxHealth, playerHealth.StartMaxHealth);
        else ShowMissing(health);
        if (playerHunger != null) ShowAmount(hunger, playerHunger.Hunger, playerHunger.MaxHunger, playerHunger.MaxHunger);
        else ShowMissing(hunger);
        if (playerStamina != null) ShowAmount(stamina, playerStamina.Stamina, playerStamina.MaxStamina, playerStamina.StartMaxStamina);
        else ShowMissing(stamina);

        if (controller != null)
        {
            ShowSpeed(walk, controller.Speed, controller.StartSpeed);
            ShowSpeed(sprint, controller.Speed * controller.SprintMultiplier, controller.StartSpeed * controller.SprintMultiplier);
        }
        else
        {
            ShowMissing(walk);
            ShowMissing(sprint);
        }

        if (playerJump != null) Show(jump, playerJump.JumpHeight.ToString("0.00") + " m", "", false);
        else ShowMissing(jump);

        if (fireballSkill == null) ShowMissing(fireball);
        else if (!fireballSkill.HasFireballPerk) Show(fireball, "Locked", "a chest perk unlocks it", false);
        else Show(fireball, fireballSkill.CurrentCooldown.ToString("0.0") + " s cooldown",
                  "x" + fireballSkill.PerkStacks + " from perks", true);

        if (PlayerCoins.Instance != null) Show(coins, PlayerCoins.Instance.Coins.ToString(), "", false);
        else ShowMissing(coins);

        ShowPerks();
    }

    // The player's scripts, found once (and again later if the player wasn't there yet).
    void FindPlayer()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        GameObject player = playerHealth.gameObject;
        if (playerHunger == null) playerHunger = player.GetComponent<PlayerHunger>();
        if (playerStamina == null) playerStamina = player.GetComponent<PlayerStamina>();
        if (controller == null) controller = player.GetComponent<PlayerController>();
        if (playerJump == null) playerJump = player.GetComponent<PlayerJump>();
        if (fireballSkill == null) fireballSkill = player.GetComponent<PlayerFireball>();
        if (perks == null) perks = PlayerPerks.Current;
    }

    // Now / max with a bar, and how much perks raised the max above its start.
    void ShowAmount(Row row, float now, float max, float startMax)
    {
        if (row == null) return;
        float extra = max - startMax;
        Show(row, Mathf.CeilToInt(now) + " / " + Mathf.RoundToInt(max),
             extra >= 0.5f ? "+" + Mathf.RoundToInt(extra) + " from perks" : "", true);
        if (row.fill != null)
        {
            float full = max > 0f ? Mathf.Clamp01(now / max) : 0f;
            row.fill.anchorMax = new Vector2(full, 1f);   // the fill reaches this far along the bar
        }
    }

    // A speed in meters per second, and how many percent perks added to it.
    void ShowSpeed(Row row, float speed, float startSpeed)
    {
        float percent = startSpeed > 0f ? (speed / startSpeed - 1f) * 100f : 0f;
        Show(row, speed.ToString("0.0") + " m/s", percent >= 0.5f ? "+" + Mathf.RoundToInt(percent) + "% from perks" : "", true);
    }

    void ShowMissing(Row row)
    {
        Show(row, "-", "", false);
    }

    // Writes a row: its value, and the text next to it (green if it's what perks added, grey for a hint).
    void Show(Row row, string value, string extra, bool isBonus)
    {
        if (row == null) return;
        SetText(row.value, value);
        if (row.bonus == null) return;
        SetText(row.bonus, extra);
        row.bonus.color = isBonus ? bonusColor : hintColor;
    }

    // One row per perk the player has, copied from PerkTemplate (icon, name with x2, stats).
    void ShowPerks()
    {
        IReadOnlyList<PlayerPerks.OwnedPerk> owned = perks != null ? perks.Owned : null;
        int count = owned != null ? owned.Count : 0;
        if (noPerks != null && noPerks.activeSelf != (count == 0)) noPerks.SetActive(count == 0);
        if (perkTemplate == null || perkList == null) return;

        while (perkRows.Count < count)
        {
            GameObject row = Instantiate(perkTemplate, perkList);
            row.name = "Perk" + (perkRows.Count + 1);
            perkRows.Add(row);
        }
        for (int i = 0; i < perkRows.Count; i++)
        {
            bool hasPerk = i < count;
            if (perkRows[i].activeSelf != hasPerk) perkRows[i].SetActive(hasPerk);
            if (hasPerk) FillPerk(perkRows[i].transform, owned[i]);
        }
    }

    void FillPerk(Transform row, PlayerPerks.OwnedPerk perk)
    {
        Transform iconPart = row.Find("Icon");
        Image icon = iconPart != null ? iconPart.GetComponent<Image>() : null;
        if (icon != null)
        {
            Sprite picture = perks.IconOf(perk.type);
            icon.sprite = picture;
            icon.enabled = picture != null;
        }
        Transform name = row.Find("Name");
        Transform stats = row.Find("Stats");
        if (name != null) SetText(name.GetComponent<TMP_Text>(), perks.PerkName(perk));
        if (stats != null) SetText(stats.GetComponent<TMP_Text>(), perks.PerkStats(perk).Replace("\n", "  ·  "));
    }

    // The row with this name and its Value, Bonus and Bar > Fill, or null if there's no such row.
    Row FindRow(string rowName)
    {
        Transform row = FindDeep(transform, rowName);
        if (row == null) return null;

        Transform value = row.Find("Value");
        Transform bonus = row.Find("Bonus");
        Transform fill = row.Find("Bar/Fill");
        return new Row
        {
            value = value != null ? value.GetComponent<TMP_Text>() : null,
            bonus = bonus != null ? bonus.GetComponent<TMP_Text>() : null,
            fill = fill as RectTransform,
        };
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text != null && text.text != value) text.text = value;
    }

    // A child (or grandchild...) with this name, or null.
    static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
