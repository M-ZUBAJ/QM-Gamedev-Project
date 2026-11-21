/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: The inventory's Gear page (InventoryCanvas > InventoryWindow > Pages > GearPage). Tools > UI > Build Gear Page
        puts it there.
Unity Editor: Nothing to connect: it finds Armor > Total (the protection line), Armor > SetBonus (the set line) and the
              pop-up (ArmorPicker) by itself. Bonus Color = the set line once the whole set is worn.
              Testing: Play, Tab > Gear: the line under the slots says how much the worn armor protects; the set line
              next to the leggings counts the set's pieces ("Copper set 4/6") and turns gold with all six.
Purpose: Looks after the whole Gear page: the line under the armor slots ("Hits do 10% less damage", or how to start
         when nothing is worn), the set line ("Copper set 4/6" and what all six give, or "COPPER SET BONUS: stamina
         recharges 50% faster" in gold once the whole set is worn, and under it the better set being worn toward,
         e.g. "Iron 3/6: +100% stamina recharge"), and closes the pop-up when the page is left
         (another tab, or the inventory closes). The character on the left is GearPreview, each slot is an ArmorSlotUI.
         Say shows a short message on the bottom line for a moment (e.g. "Iron Boots goes in the Boots slot.").
Requirement: GearPage.cs x-> PlayerArmor.cs, ArmorPicker.cs, ArmorStats.cs
             GearPage.cs <-x ArmorSlotUI.cs, GearPageBuilder.cs
*/

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GearPage : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The set line's color once the whole set is worn and its bonus is on: a strong gold that's easy to read " +
             "on the light page.")]
    [SerializeField] Color bonusColor = new Color(0.8f, 0.5f, 0.02f);

    [Tooltip("The set line's color before that (how many pieces are worn, and what all of them would give).")]
    [SerializeField] Color hintColor = new Color(0.42f, 0.43f, 0.48f);

    TMP_Text total;        // Armor > Total
    TMP_Text setLine;      // Armor > SetBonus (a page built before the set bonus has none: then it's just not shown)
    ArmorPicker picker;
    Color totalColor;      // its usual color (a message is shown in Say Color)
    string said;           // the message showing now (null = none)
    float sayUntil;        // when it goes (unscaled seconds)

    const float SaySeconds = 2.5f;
    static readonly Color SayColor = new Color(0.8f, 0.15f, 0.12f);

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        total = Text("Armor/Total");
        setLine = Text("Armor/SetBonus");
        if (total != null) totalColor = total.color;
        picker = GetComponentInChildren<ArmorPicker>(true);
    }

    void Update()
    {
        if (said == null || Time.unscaledTime < sayUntil) return;
        said = null;   // the message has had its moment: back to the protection line
        if (total != null) total.color = totalColor;
        Refresh();
    }

    void OnEnable()
    {
        PlayerArmor.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerArmor.Changed -= Refresh;
        if (picker != null) picker.Close();   // a pop-up shouldn't still be open the next time the page shows
        if (said != null && total != null) total.color = totalColor;
        said = null;
    }

    // ===== My methods =====

    // Shows a short message (red) on the line under the slots for a moment, then the protection line comes back.
    public void Say(string message)
    {
        if (total == null || string.IsNullOrEmpty(message)) return;
        said = message;
        sayUntil = Time.unscaledTime + SaySeconds;   // unscaled: menus may pause the game
        total.text = message;
        total.color = SayColor;
    }

    void Refresh()
    {
        PlayerArmor armor = PlayerArmor.Current;
        ShowSet(armor);
        if (total == null || said != null) return;   // a message is showing: the line updates when it goes

        float percent = armor != null ? armor.Protection * 100f : 0f;
        total.text = percent > 0f
            ? "Hits do " + ArmorStats.Percent(percent) + " less damage"
            : "No armor yet: click a slot to put some on";
    }

    // The set line: "Copper set 4/6" and what all six give (grey), or the set's bonus in gold once it's all worn (a
    // mix of Copper and Iron pieces counts as Copper), with the better set being worn toward under it, small and grey
    // ("Iron 3/6: +100% stamina recharge"). Empty while no worn piece belongs to a set.
    void ShowSet(PlayerArmor armor)
    {
        if (setLine == null) return;
        int count = 0;
        string set = armor != null ? armor.MostWornSet(out count) : null;
        if (set == null)
        {
            setLine.text = "";
            return;
        }

        int size = armor.SetSize;
        PlayerArmor.SetBonus bonus = armor.BonusFor(set);
        if (count >= size && bonus != null)
        {
            string text = "<b>" + set.ToUpper() + " SET BONUS</b>\n" + FirstUpper(bonus.Describe());
            string better = armor.BetterSetInProgress(out int betterCount);
            PlayerArmor.SetBonus next = better != null ? armor.BonusFor(better) : null;
            if (next != null)
            {
                text += "\n<size=85%><color=#" + ColorUtility.ToHtmlStringRGB(hintColor) + ">" + better + " " + betterCount +
                        "/" + size + ": " + next.Short() + "</color></size>";
            }
            setLine.text = text;
            setLine.color = bonusColor;
            return;
        }
        setLine.text = set + " set " + count + "/" + size + (bonus != null ? "\nAll " + size + ": " + bonus.Describe() : "");
        setLine.color = hintColor;
    }

    static string FirstUpper(string text)
    {
        return string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text.Substring(1);
    }

    // A text on this page by its path (e.g. "Armor/Total"), or null.
    TMP_Text Text(string path)
    {
        Transform part = transform.Find(path);
        return part != null ? part.GetComponent<TMP_Text>() : null;
    }
}
