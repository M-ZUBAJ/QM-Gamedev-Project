/*
First Date: 19/11/2026
Date: 21/11/2026
Author: Tony
Use by: The inventory's Info page (InventoryCanvas > InventoryWindow > Pages > InfoPage). Tools > UI > Build Inventory UI
        (or Tools > UI > Add Inventory Scripts for an inventory built before) makes the page and puts this on it.
Unity Editor: Nothing to connect: it finds the player's scripts by itself, and its parts by NAME:
                HealthRow, HungerRow, StaminaRow, ArmorRow, SpeedRow, SprintRow, JumpRow, FireballRow, CoinsRow:
                  each with a "Value" text, a "Bonus" text and, for Health / Hunger / Stamina, a "Bar" with a "Fill"
                  (ArmorRow: Tools > Items > Build Plate Armor adds it to an Info tab made before the armor)
                PerkList: the perk rows go in here, copied from its switched-off "PerkTemplate" (Icon, Name, Stats)
                NoPerks: the text shown while the player has no perk
              Testing: Play, press Tab and click Info. Open a chest and take a perk: its stat shows the bonus in green
              and the perk joins the list. Wear all six copper armor pieces: the Armor and Coins rows turn gold.
Purpose: The player's status, updated 4 times a second while the Info tab is open:
           Health, Hunger, Stamina    now / max, with a bar
           Armor                      how much the worn armor takes off every hit, and its set ("Iron set 4/6")
           Walk / Sprint speed        meters per second
           Jump height, Fireball (locked, or its cooldown), Coins
         What perks added shows in green next to the stat, e.g. "+44 from perks" (max health above its start),
         "+15% from perks" (walk speed, sprint, jump), "+30% recharge from perks" (Second Wind), "+1 per kill from
         perks" (Bounty Hunter), and on the right the perks the player has, with their icons, names and stats
         (PlayerPerks).
         An armor set bonus shows in gold, on a gold-tinted row, so it stands out from the perks' green: with the whole
         copper set on, Coins says "+1 per kill (Copper Set)" and Armor says "Copper Set bonus" (PlayerArmor); the
         gold set's cheaper chests show there too ("Gold Set bonus, chests -10%").
Requirement: PlayerInfoPage.cs x-> ArmorStats.cs
             PlayerInfoPage.cs o-> PlayerHealth.cs, PlayerHunger.cs, PlayerStamina.cs, PlayerController.cs, PlayerJump.cs, PlayerFireball.cs, PlayerCoins.cs, PlayerPerks.cs, PlayerArmor.cs, PerkEffects.cs
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
        public Image back;          // the row's rounded background (tinted while an armor set bonus improves the stat)
        public Color backColor;     // its usual color
    }

    // ===== Fields =====
    [Tooltip("The color of what perks added, e.g. \"+44 from perks\".")]
    [SerializeField] Color bonusColor = new Color(0.1f, 0.58f, 0.27f);

    [Tooltip("The color of a hint that isn't a bonus, e.g. how to unlock the fireball.")]
    [SerializeField] Color hintColor = new Color(0.42f, 0.43f, 0.48f);

    [Tooltip("The color of an armor set bonus, e.g. \"+1 per kill (Copper Set)\" while the whole copper set is worn: a " +
             "strong gold, so it stands out from the green perk bonuses.")]
    [SerializeField] Color setBonusColor = new Color(0.8f, 0.5f, 0.02f);

    [Tooltip("The background of a row an armor set bonus improves (Stamina and Armor while the whole iron set is worn), " +
             "so it catches the eye.")]
    [SerializeField] Color setBonusRowColor = new Color(1f, 0.93f, 0.72f);

    const float RefreshEvery = 0.25f;   // seconds: 4 times a second, so the bars follow while the page is open

    Row health, hunger, stamina, armorRow, walk, sprint, jump, fireball, coins;
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
        armorRow = FindRow("ArmorRow");   // an Info tab made before the armor has none (Build Plate Armor adds it)
        walk = FindRow("SpeedRow");
        sprint = FindRow("SprintRow");
        jump = FindRow("JumpRow");
        fireball = FindRow("FireballRow");
        coins = FindRow("CoinsRow");
        FitBonus(stamina);   // a perk bonus and a set bonus together can be long
        FitBonus(armorRow);
        FitBonus(coins);     // Bounty Hunter + the armor set bonus

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
        // The Second Wind perk: stamina recharges faster (green, like the other perks).
        float regen = PerkEffects.StaminaRegenBonus;
        string staminaFromPerks = regen > 0.001f ? "+" + Mathf.RoundToInt(regen * 100f) + "% recharge from perks" : "";
        if (playerStamina != null) ShowAmount(stamina, playerStamina.Stamina, playerStamina.MaxStamina, playerStamina.StartMaxStamina, staminaFromPerks);
        else ShowMissing(stamina);

        // The armor's set bonus (the whole copper set: +1 coin per monster killed), in gold on the rows it improves.
        PlayerArmor armor = PlayerArmor.Current;
        PlayerArmor.SetBonus setBonus = armor != null ? armor.ActiveBonus : null;
        ShowArmor(armor, setBonus);

        if (controller != null)
        {
            ShowSpeed(walk, controller.Speed, controller.StartSpeed);
            ShowSpeed(sprint, controller.Speed * controller.SprintMultiplier, controller.StartSpeed * controller.BaseSprintMultiplier);
        }
        else
        {
            ShowMissing(walk);
            ShowMissing(sprint);
        }

        if (playerJump != null)
        {
            float higher = playerJump.BaseJumpHeight > 0f ? (playerJump.JumpHeight / playerJump.BaseJumpHeight - 1f) * 100f : 0f;
            Show(jump, playerJump.JumpHeight.ToString("0.00") + " m", higher >= 0.5f ? "+" + Mathf.RoundToInt(higher) + "% from perks" : "", true);
        }
        else ShowMissing(jump);

        if (fireballSkill == null) ShowMissing(fireball);
        else if (!fireballSkill.HasFireballPerk) Show(fireball, "Locked", "a chest perk unlocks it", false);
        else Show(fireball, fireballSkill.CurrentCooldown.ToString("0.0") + " s cooldown",
                  "x" + fireballSkill.PerkStacks + " from perks", true);

        // Extra coins per monster killed: the armor set bonus in gold, the Bounty Hunter perk in green.
        int fromSet = setBonus != null ? setBonus.coinsPerKill : 0;
        int fromPerks = PerkEffects.BountyCoins;
        string perKill = fromPerks > 0 ? "+" + fromPerks + " per kill from perks" : "";
        if (fromSet > 0) perKill = (perKill.Length > 0 ? perKill + "  " : "") + SetBonusText("+" + fromSet + " per kill (" + setBonus.set + " Set)");
        if (PlayerCoins.Instance != null) Show(coins, PlayerCoins.Instance.Coins.ToString(), perKill, true);
        else ShowMissing(coins);
        Highlight(coins, fromSet > 0 && PlayerCoins.Instance != null);

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

    // Now / max with a bar, and how much perks raised the max above its start (then "more", e.g. a set bonus).
    void ShowAmount(Row row, float now, float max, float startMax, string more = "")
    {
        if (row == null) return;
        float extra = max - startMax;
        string fromPerks = extra >= 0.5f ? "+" + Mathf.RoundToInt(extra) + " from perks" : "";
        string both = string.IsNullOrEmpty(fromPerks) ? more : string.IsNullOrEmpty(more) ? fromPerks : fromPerks + "  " + more;
        Show(row, Mathf.CeilToInt(now) + " / " + Mathf.RoundToInt(max), both, true);
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

    // The Armor row: how much the worn armor takes off every hit, then its set: "Iron Set bonus" in gold (and the row
    // tinted) once the whole set is worn (the gold set adds its cheaper chests: "Gold Set bonus, chests -10%"), else how
    // many of its pieces are on ("Iron set 4/6").
    void ShowArmor(PlayerArmor armor, PlayerArmor.SetBonus setBonus)
    {
        if (armorRow == null) return;
        Highlight(armorRow, setBonus != null);
        if (armor == null)
        {
            ShowMissing(armorRow);
            return;
        }

        float percent = armor.Protection * 100f;
        string value = ArmorStats.Percent(percent);
        string set = armor.MostWornSet(out int count);
        string discount = setBonus != null && setBonus.chestDiscount > 0f
            ? ", chests -" + Mathf.RoundToInt(setBonus.chestDiscount * 100f) + "%" : "";   // the gold set's cheaper chests
        if (setBonus != null) Show(armorRow, value, SetBonusText(setBonus.set + " Set bonus" + discount), true);
        else if (set != null) Show(armorRow, value, set + " set " + count + "/" + armor.SetSize, false);
        else Show(armorRow, value, percent > 0f ? "less damage per hit" : "none worn", false);
    }

    // Bold text in the set bonus color, for inside a row's Bonus text (the rest of it keeps its own color).
    string SetBonusText(string text)
    {
        return "<b><color=#" + ColorUtility.ToHtmlStringRGB(setBonusColor) + ">" + text + "</color></b>";
    }

    // Tints a row's background while an armor set bonus improves its stat, and puts it back after.
    void Highlight(Row row, bool on)
    {
        if (row == null || row.back == null) return;
        Color wanted = on ? setBonusRowColor : row.backColor;
        if (row.back.color != wanted) row.back.color = wanted;
    }

    // A long Bonus text shrinks to fit the row instead of running off its edge.
    static void FitBonus(Row row)
    {
        if (row == null || row.bonus == null || row.bonus.enableAutoSizing) return;
        row.bonus.fontSizeMax = row.bonus.fontSize;
        row.bonus.fontSizeMin = 12f;
        row.bonus.enableAutoSizing = true;
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
        Image back = row.GetComponent<Image>();
        return new Row
        {
            value = value != null ? value.GetComponent<TMP_Text>() : null,
            bonus = bonus != null ? bonus.GetComponent<TMP_Text>() : null,
            fill = fill as RectTransform,
            back = back,
            backColor = back != null ? back.color : Color.white,
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
