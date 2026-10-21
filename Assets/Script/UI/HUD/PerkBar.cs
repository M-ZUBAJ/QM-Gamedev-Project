/*
First Date: 21/10/2026
Date: 21/10/2026
Author: Tony
Use by: On the perk row in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > Perks, with Perk1, Perk2, Perk3).
Unity Editor: Nothing to connect: it finds the player's PlayerPerks by itself, and its boxes by NAME:
              every child whose name starts with "Perk" is a box, with a "Title" text and a "Stats" text inside.
              One Box Per Perk off: the boxes you made (Perk1-3) keep their places; more boxes = duplicate Perk3.
              One Box Per Perk on:  Perk1 is copied for every perk, and the boxes share the whole row.
              In Edit mode every box stays visible so you can arrange them; they only hide while playing.
              Testing: Play, select the Player, PlayerPerks' ⋮ menu > "Test: Add Random Perk" (and "Test: Die" on
              PlayerHealth: every box disappears again).
Purpose: Shows the player's perks at the top of the screen: each box has the perk's name (e.g. "Fireball x2")
         and its stats right now (e.g. "Q with sword / 2.4 s cooldown"). Boxes are invisible until they have a
         perk and pop in when the player gets one, then stay for the whole game (until the player dies).
         Two ways (One Box Per Perk):
           Off = fixed boxes in fixed places (Perk1, Perk2, Perk3); with more perks than boxes the newest show.
           On  = one box per perk from the left, sharing the row: 1 perk = the whole row, 10 perks = 10 boxes.
Requirement: PerkBar.cs x-> PlayerPerks.cs
             PerkBar.cs <-x HUDBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PerkBar : MonoBehaviour
{
    // One box on screen.
    class Box
    {
        public GameObject go;
        public CanvasGroup group;   // hides the box without moving the others (alpha 0)
        public TMP_Text title;
        public TMP_Text stats;
    }

    // ===== Fields =====
    [Tooltip("Off: the boxes you made (Perk1, Perk2, Perk3) stay in their places and pop in as perks are gained. " +
             "On: a new box for every perk, from the left, all sharing the row: 1 perk fills the whole row, " +
             "10 perks = 10 boxes.")]
    [SerializeField] bool oneBoxPerPerk = false;

    [Tooltip("Title color (the perk's name).")]
    [SerializeField] Color titleColor = new Color(1f, 0.85f, 0.2f);   // gold

    [Tooltip("Stats color.")]
    [SerializeField] Color statsColor = Color.white;

    [Tooltip("Smallest text size when many boxes share the row (One Box Per Perk): long text shrinks down to this.")]
    [Min(6f)]
    [SerializeField] float minTextSize = 10f;

    // How often (seconds) the stats are rewritten, so numbers stay right, and how often to look for the player.
    const float RefreshEvery = 0.5f;

    readonly List<Box> boxes = new List<Box>();
    int madeByHand;        // how many boxes were in the Hierarchy before the game started (Perk1-3)
    GameObject template;   // the box copied for One Box Per Perk (the first one)
    PlayerPerks perks;     // found by itself (null until the player is there)
    float nextRefresh;

    public bool OneBoxPerPerk => oneBoxPerPerk;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Perk")) boxes.Add(MakeBox(child.gameObject));
        }
        madeByHand = boxes.Count;
        template = boxes.Count > 0 ? boxes[0].go : null;

        Refresh();   // invisible from the very first frame (no perks yet)
    }

    void OnDisable()
    {
        if (perks != null) perks.Changed -= Refresh;
        perks = null;
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + RefreshEvery;

        if (perks == null)
        {
            perks = PlayerPerks.Current;
            if (perks != null) perks.Changed += Refresh;   // pop in at once when a perk is gained
        }
        Refresh();
    }

    // ===== My methods =====

    // Remembers a box's parts. Long text shrinks to fit (boxes get narrow with One Box Per Perk).
    Box MakeBox(GameObject go)
    {
        Box box = new Box { go = go };
        box.group = go.GetComponent<CanvasGroup>();
        if (box.group == null) box.group = go.AddComponent<CanvasGroup>();
        box.group.blocksRaycasts = false;   // the HUD never blocks clicks

        Transform title = go.transform.Find("Title");
        Transform stats = go.transform.Find("Stats");
        box.title = title != null ? title.GetComponent<TMP_Text>() : null;
        box.stats = stats != null ? stats.GetComponent<TMP_Text>() : null;
        FitText(box.title);
        FitText(box.stats);
        return box;
    }

    void FitText(TMP_Text text)
    {
        if (text == null || text.enableAutoSizing) return;
        text.fontSizeMax = text.fontSize;   // never bigger than you set it
        text.fontSizeMin = Mathf.Min(minTextSize, text.fontSize);
        text.enableAutoSizing = true;
    }

    void Refresh()
    {
        IReadOnlyList<PlayerPerks.OwnedPerk> owned = perks != null ? perks.Owned : null;
        int count = owned != null ? owned.Count : 0;

        if (oneBoxPerPerk) ShowOnePerBox(owned, count);
        else ShowFixedBoxes(owned, count);
    }

    // Off: the hand-made boxes keep their places (a box without a perk is see-through, so the others don't move).
    // More perks than boxes: the newest ones are shown.
    void ShowFixedBoxes(IReadOnlyList<PlayerPerks.OwnedPerk> owned, int count)
    {
        int first = Mathf.Max(0, count - madeByHand);
        for (int i = 0; i < boxes.Count; i++)
        {
            Box box = boxes[i];
            bool handMade = i < madeByHand;
            if (box.go.activeSelf != handMade) box.go.SetActive(handMade);   // copies from One Box Per Perk: off
            if (!handMade) continue;

            int p = first + i;
            bool hasPerk = p < count;
            box.group.alpha = hasPerk ? 1f : 0f;
            if (hasPerk) Fill(box, owned[p]);
        }
    }

    // On: one box per perk from the left. Boxes without a perk are switched off, so the row's layout gives the
    // rest all the room: 1 perk = the whole row, 2 = half each...
    void ShowOnePerBox(IReadOnlyList<PlayerPerks.OwnedPerk> owned, int count)
    {
        while (boxes.Count < count && template != null)
        {
            GameObject copy = Instantiate(template, transform);
            copy.name = "Perk" + (boxes.Count + 1);
            boxes.Add(MakeBox(copy));
        }

        for (int i = 0; i < boxes.Count; i++)
        {
            Box box = boxes[i];
            bool hasPerk = i < count;
            if (box.go.activeSelf != hasPerk) box.go.SetActive(hasPerk);
            box.group.alpha = 1f;
            if (hasPerk) Fill(box, owned[i]);
        }
    }

    void Fill(Box box, PlayerPerks.OwnedPerk perk)
    {
        string title = perks.PerkName(perk);
        string stats = perks.PerkStats(perk);
        if (box.title != null)
        {
            if (box.title.text != title) box.title.text = title;
            box.title.color = titleColor;
        }
        if (box.stats != null)
        {
            if (box.stats.text != stats) box.stats.text = stats;
            box.stats.color = statsColor;
        }
    }
}
