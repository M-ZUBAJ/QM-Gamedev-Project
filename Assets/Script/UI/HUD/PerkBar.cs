/*
First Date: 21/10/2026
Date: 19/11/2026
Author: Tony
Use by: On the perk row in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > Perks, with Perk1, Perk2, Perk3, plus
        the icon row HUDCanvas > PerkIcons with PerkIcon1).
Unity Editor: Nothing to connect: it finds the player's PlayerPerks by itself, and its boxes by NAME:
              every child whose name starts with "Perk" is a box, with a "Title" text and a "Stats" text inside.
              One Box Per Perk off: the boxes you made (Perk1-3) keep their places; more boxes = duplicate Perk3.
              One Box Per Perk on:  Perk1 is copied for every perk, and the boxes share the whole row.
              GameRules > Perk Icons on: the boxes hide and Icon Row shows one tile per perk instead. Its first child
              whose name starts with "PerkIcon" is copied for every perk; a tile has an "Icon" image, a "Count" text
              (x2) and a "Label" text (the perk's name, only for a perk without a picture). Empty Icon Row = the
              sibling named "PerkIcons" (Build HUD saves it switched off; this switches it on while playing).
              In Edit mode the boxes stay visible so you can arrange them; they only hide while playing.
              Testing: Play, select the Player, PlayerPerks' ⋮ menu > "Test: Add Random Perk" (and "Test: Die" on
              PlayerHealth: everything disappears again). Tick GameRules > Perk Icons on and off while playing.
Purpose: Shows the player's perks at the top of the screen, as text or as icons (GameRules > Perk Icons):
           Text:  each box has the perk's name (e.g. "Fireball x2") and its stats right now (e.g. "Q with sword /
                  2.4 s cooldown"). Boxes are invisible until they have a perk, then stay for the whole game (until
                  the player dies). Two ways (One Box Per Perk):
                    Off = fixed boxes in fixed places (Perk1, Perk2, Perk3); with more perks than boxes the newest show.
                    On  = one box per perk from the left, sharing the row: 1 perk = the whole row, 10 perks = 10 boxes.
           Icons: one small tile per perk, centered at the top: its picture (PlayerPerks.IconOf, e.g. a heart for
                  Max Health) and "x2" when the player has it more than once. The inventory's Info tab has the details.
Requirement: PerkBar.cs x-> PlayerPerks.cs, GameRules.cs
             PerkBar.cs <-x HUDBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;   // Image, Graphic

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

    // One icon tile on screen.
    class Tile
    {
        public GameObject go;
        public Image icon;
        public TMP_Text count;      // "x2"
        public TMP_Text label;      // the perk's name, only when it has no picture
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

    [Header("Icons (GameRules > Perk Icons)")]
    [Tooltip("The row of perk icons shown instead of these boxes while GameRules > Perk Icons is on. " +
             "Empty = the sibling named \"PerkIcons\".")]
    [SerializeField] RectTransform iconRow;

    [Tooltip("Color of the x2 on an icon.")]
    [SerializeField] Color countColor = new Color(1f, 0.85f, 0.2f);   // gold

    // How often (seconds) the stats are rewritten, so numbers stay right, and how often to look for the player.
    const float RefreshEvery = 0.5f;

    readonly List<Box> boxes = new List<Box>();
    readonly List<Tile> tiles = new List<Tile>();
    int madeByHand;          // how many boxes were in the Hierarchy before the game started (Perk1-3)
    GameObject template;     // the box copied for One Box Per Perk (the first one)
    GameObject tileTemplate; // the icon tile copied for every perk (the first one)
    PlayerPerks perks;       // found by itself (null until the player is there)
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

        if (iconRow == null && transform.parent != null) iconRow = transform.parent.Find("PerkIcons") as RectTransform;
        if (iconRow != null)
        {
            foreach (Transform child in iconRow)
            {
                if (child.name.StartsWith("PerkIcon")) tiles.Add(MakeTile(child.gameObject));
            }
        }
        tileTemplate = tiles.Count > 0 ? tiles[0].go : null;

        Refresh();   // invisible from the very first frame (no perks yet)
    }

    void OnEnable()
    {
        GameRules.Changed += Refresh;   // Perk Icons ticked or unticked: switch at once
    }

    void OnDisable()
    {
        GameRules.Changed -= Refresh;
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

    // Remembers an icon tile's parts.
    Tile MakeTile(GameObject go)
    {
        Tile tile = new Tile { go = go };
        Transform icon = go.transform.Find("Icon");
        Transform count = go.transform.Find("Count");
        Transform label = go.transform.Find("Label");
        tile.icon = icon != null ? icon.GetComponent<Image>() : null;
        tile.count = count != null ? count.GetComponent<TMP_Text>() : null;
        tile.label = label != null ? label.GetComponent<TMP_Text>() : null;
        foreach (Graphic part in go.GetComponentsInChildren<Graphic>(true)) part.raycastTarget = false;   // never blocks clicks
        return tile;
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
        bool icons = iconRow != null && GameRules.PerkIcons;

        if (iconRow != null && iconRow.gameObject.activeSelf != icons) iconRow.gameObject.SetActive(icons);
        if (icons)
        {
            HideBoxes();
            ShowTiles(owned, count);
        }
        else if (oneBoxPerPerk) ShowOnePerBox(owned, count);
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

    // Icons on: every text box see-through (they keep their places for when the rule is turned off again).
    void HideBoxes()
    {
        foreach (Box box in boxes) box.group.alpha = 0f;
    }

    // One tile per perk, in the order they were gained. Tiles without a perk are switched off, so the row stays
    // centered on the ones that show.
    void ShowTiles(IReadOnlyList<PlayerPerks.OwnedPerk> owned, int count)
    {
        while (tiles.Count < count && tileTemplate != null)
        {
            GameObject copy = Instantiate(tileTemplate, iconRow);
            copy.name = "PerkIcon" + (tiles.Count + 1);
            tiles.Add(MakeTile(copy));
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            bool hasPerk = i < count;
            if (tiles[i].go.activeSelf != hasPerk) tiles[i].go.SetActive(hasPerk);
            if (hasPerk) FillTile(tiles[i], owned[i]);
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

    // The perk's picture and its stacks (x2). A perk without a picture shows its name instead.
    void FillTile(Tile tile, PlayerPerks.OwnedPerk perk)
    {
        Sprite picture = perks.IconOf(perk.type);
        if (tile.icon != null)
        {
            if (tile.icon.sprite != picture) tile.icon.sprite = picture;
            tile.icon.enabled = picture != null;
        }
        if (tile.label != null)
        {
            bool showName = picture == null;
            if (tile.label.gameObject.activeSelf != showName) tile.label.gameObject.SetActive(showName);
            if (showName) tile.label.text = PlayerPerks.NameOf(perk.type);
        }
        if (tile.count != null)
        {
            string stacks = perk.stacks > 1 ? "x" + perk.stacks : "";
            if (tile.count.text != stacks) tile.count.text = stacks;
            tile.count.color = countColor;
        }
    }
}
