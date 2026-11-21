/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: The perk roll pop-up's canvas (PerkRollCanvas). Tools > UI > Build Perk Roll Popup builds it, puts it in the open
        scene and connects everything (Tools > World > Build Loot Chests puts it in every game scene too).
Unity Editor: Roll Seconds (how long the slot-machine roll lasts), Fast Step / Slow Step (how quickly the pictures change
              at the start / end of the roll), Slide Distance, Sparks. The parts are filled in by the tool.
              Testing: new chest system (GameRules > New Chests ticked), Play, look at a common box and press E: the
              pictures roll for 2.5 s (SKIP, E, Space or a click stops it), then BUY or REROLL. E again, or walking away,
              closes it without paying; opening that box again shows the same perk.
Purpose: The new chest system's random boxes (the common and epic box: ChestTier > Random In New System). Marko's
         PerkChest opens it with the one perk it rolled:
           - the box's perks flick past like a slot machine, slowing down, and land on the rolled one (a pop, a glow and
             sparks in the box's color);
           - SKIP (or E / Space / a click) jumps straight to the result;
           - the result stays: the perk's name and what it does, the price and your coins, BUY (pays, and the box bursts
             into particles: PerkChest.BuyRolled) and REROLL (pays a tenth of the price, doubling at the same box, and
             rolls again: PerkChest.TryReroll; only once the player has the Reroll perk, else BUY sits in the middle
             alone), faded while you can't pay;
           - E or walking away closes it (Marko's PerkChest), nothing is paid; so does the player dying (MenuState).
         While it's open the mouse is free and the game knows a menu is open (no attacks, no outline, no jumping).
Requirement: PerkRollUI.cs x-> PerkChest.cs, PerkData.cs, PlayerCoins.cs, MenuState.cs, UITween.cs
             PerkRollUI.cs o-> ChestTier.cs, PerkRules.cs, UISparks.cs
             PerkRollUI.cs <-x PerkChest.cs, PerkRollBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PerkRollUI : MonoBehaviour
{
    // ===== Fields =====
    [Header("Parts (the tool fills these)")]
    [Tooltip("The dark backdrop with everything on it: switched on while the pop-up shows.")]
    [SerializeField] GameObject panel;

    [Tooltip("The box that pops in when it opens.")]
    [SerializeField] RectTransform window;

    [Tooltip("The box's name (\"COMMON BOX\"), in its color.")]
    [SerializeField] TMP_Text titleText;

    [Tooltip("The line under the title (the box's color).")]
    [SerializeField] Image titleLine;

    [Tooltip("The frame the pictures roll in (the sparks fly from it).")]
    [SerializeField] RectTransform reel;

    [Tooltip("The perk's picture.")]
    [SerializeField] Image icon;

    [Tooltip("The soft light behind the picture (the box's color; it flashes when the roll lands).")]
    [SerializeField] Image glow;

    [SerializeField] TMP_Text perkName;
    [SerializeField] TMP_Text perkDescription;

    [Tooltip("The box's price.")]
    [SerializeField] TMP_Text priceText;

    [Tooltip("How many coins the player has.")]
    [SerializeField] TMP_Text coinsText;

    [Tooltip("A red line, shown only while the player can't pay.")]
    [SerializeField] TMP_Text needText;

    [Tooltip("What the keys do (it names the chest's key).")]
    [SerializeField] TMP_Text hintText;

    [SerializeField] Button buyButton;
    [SerializeField] TMP_Text buyLabel;
    [SerializeField] Button rerollButton;
    [SerializeField] TMP_Text rerollLabel;
    [SerializeField] Button skipButton;

    [Header("Roll")]
    [Tooltip("Seconds the roll lasts before it lands on the perk.")]
    [Range(0.5f, 6f)]
    [SerializeField] float rollSeconds = 2.5f;

    [Tooltip("Seconds between pictures at the start of the roll (fast).")]
    [Range(0.02f, 0.3f)]
    [SerializeField] float fastStep = 0.05f;

    [Tooltip("Seconds between pictures at the end of the roll (slow).")]
    [Range(0.1f, 1f)]
    [SerializeField] float slowStep = 0.34f;

    [Tooltip("How far (pixels) each new picture slides down into the frame.")]
    [Range(0f, 200f)]
    [SerializeField] float slideDistance = 70f;

    [Tooltip("Sparks when the roll lands on the perk (0 = none).")]
    [Range(0, 60)]
    [SerializeField] int sparks = 26;

    [Header("Look")]
    [Tooltip("The perk names' color while rolling.")]
    [SerializeField] Color rollingNameColor = new Color(0.62f, 0.64f, 0.7f);

    [Tooltip("The rolled perk's name color.")]
    [SerializeField] Color resultNameColor = new Color(1f, 0.82f, 0.3f);

    [Tooltip("The price's color while the player can pay.")]
    [SerializeField] Color canPayColor = new Color(1f, 0.85f, 0.2f);

    [Tooltip("The price's color while the player can't pay.")]
    [SerializeField] Color cantPayColor = new Color(1f, 0.38f, 0.32f);

    [Tooltip("Seconds the window's pop takes when it opens (0 = no pop).")]
    [Range(0f, 1f)]
    [SerializeField] float popSeconds = 0.25f;

    PerkChest chest;            // the chest it shows (null = closed)
    PerkData result;            // the perk the chest rolled
    readonly List<PerkData> faces = new List<PerkData>();   // the pictures that roll past
    int face;                   // which of them shows
    bool rolling;
    float rollTime;             // seconds rolled so far
    float nextStep;             // seconds until the next picture
    float slide;                // 1 = the picture just came in (at the top), 0 = in its place
    float slideSeconds;
    float landedAt = -10f;      // unscaled time the roll landed
    float lastPress = -20f;     // unscaled time of the last mouse press (BUY / REROLL need a press after the landing)
    int startedFrame = -1;      // the frame it opened (the E that opened it mustn't also skip)
    int skippedFrame = -1;      // the frame it landed (the E that skipped mustn't also close it)
    Vector2 buyHome;            // BUY's place next to REROLL (it moves to the middle when there's no REROLL)
    Color lineAlpha = Color.white;

    // What the texts say now, so they're only rewritten when something changes.
    int shownPrice = -1, shownCoins = -1, shownReroll = -2;
    bool shownChanges, shownRolling;

    // The mouse as it was before opening (like Marko's perk panel), given back on closing.
    CursorLockMode previousLock;
    bool previousVisible;
    bool cursorSaved;

    public static PerkRollUI Instance { get; private set; }
    public bool IsOpen { get; private set; }
    public bool IsRolling => rolling;

    // Is the pop-up open for this chest?
    public static bool IsOpenFor(PerkChest c) => c != null && Instance != null && Instance.IsOpen && Instance.chest == c;

    // True while E must not close it: still rolling (E skips instead), or E just stopped the roll this frame.
    public static bool BlocksClose => Instance != null && Instance.IsOpen &&
                                      (Instance.rolling || Instance.skippedFrame == Time.frameCount);

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(name + ": there is more than one PerkRollUI in the scene; only the first one is used.");
            return;
        }
        Instance = this;

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(Buy);
            buyHome = ((RectTransform)buyButton.transform).anchoredPosition;
        }
        if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
        if (skipButton != null) skipButton.onClick.AddListener(Skip);
        if (titleLine != null) lineAlpha = titleLine.color;
        if (panel != null) panel.SetActive(false);
        if (panel == null) Debug.LogWarning(name + ": PerkRollUI has no Panel. Run Tools > UI > Build Perk Roll Popup.");
    }

    // Every menu closes when the game asks (the player died, the game was won), the same as E.
    void OnEnable()
    {
        MenuState.CloseAllRequested += Close;
    }

    void OnDisable()
    {
        MenuState.CloseAllRequested -= Close;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;   // static values outlive the scene, so clear it
        MenuState.SetOpen(this, false);
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Instance = null;
    }

    void Update()
    {
        if (!IsOpen) return;
        if (chest == null)   // the chest is gone (removed or picked up by something else)
        {
            Close();
            return;
        }

        if (Input.GetMouseButtonDown(0)) lastPress = Time.unscaledTime;
        if (rolling)
        {
            bool skip = Time.frameCount != startedFrame &&
                        (Input.GetKeyDown(chest.InteractKey) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0));
            if (skip) Land(true);
            else Roll(Time.unscaledDeltaTime);
        }
        Slide(Time.unscaledDeltaTime);
        Write();
    }

    // ===== My methods =====

    // Marko's PerkChest calls this with the perk it rolled and the box's perks (the pictures that roll past).
    // animate false = straight to the result (opening a box again). Called again while open = a new roll (REROLL).
    public bool Open(PerkChest from, PerkData perk, List<PerkData> boxPerks, bool animate)
    {
        if (panel == null || from == null || perk == null) return false;

        chest = from;
        result = perk;
        FillFaces(boxPerks);

        if (!IsOpen)
        {
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            cursorSaved = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            panel.SetActive(true);
            IsOpen = true;
            MenuState.SetOpen(this, true);
            PopIn();
        }

        startedFrame = Time.frameCount;
        WriteTitle();
        if (animate && rollSeconds > 0f && faces.Count > 1) StartRoll();
        else Land(false);
        return true;
    }

    // Closes it (nothing is paid). Marko's PerkChest calls it for E and walking away; BUY closes it before the perk is
    // given (like Marko's panel).
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        rolling = false;
        chest = null;
        if (panel != null) panel.SetActive(false);
        MenuState.SetOpen(this, false);

        if (cursorSaved)
        {
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            cursorSaved = false;
        }
    }

    // The pictures that roll past: the box's perks the player may get (each once, shuffled), the rolled one among them.
    void FillFaces(List<PerkData> boxPerks)
    {
        faces.Clear();
        if (boxPerks != null)
        {
            foreach (PerkData perk in boxPerks)
            {
                if (perk != null && !faces.Contains(perk) && PerkRules.CanOffer(perk)) faces.Add(perk);
            }
        }
        if (!faces.Contains(result)) faces.Add(result);

        for (int i = faces.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (faces[i], faces[j]) = (faces[j], faces[i]);
        }
    }

    void StartRoll()
    {
        rolling = true;
        rollTime = 0f;
        nextStep = 0f;   // the first picture at once
        face = Random.Range(0, faces.Count);
        if (perkDescription != null) perkDescription.text = "";
        if (glow != null) glow.color = WithAlpha(BoxColor(), 0.2f);
        ShowButtons();
        shownPrice = -1;   // write everything again
    }

    // The pictures flick past, slower and slower (each step a little longer), then the roll lands.
    void Roll(float seconds)
    {
        rollTime += seconds;
        if (rollTime >= rollSeconds)
        {
            Land(true);
            return;
        }

        nextStep -= seconds;
        if (nextStep > 0f) return;

        float k = rollTime / rollSeconds;
        float step = Mathf.Lerp(fastStep, slowStep, k * k);   // slows down more and more toward the end
        nextStep = step;
        face = (face + 1) % faces.Count;
        ShowFace(faces[face], rollingNameColor);
        slide = 1f;
        slideSeconds = Mathf.Min(0.09f, step * 0.8f);
    }

    // Each new picture slides down into its place.
    void Slide(float seconds)
    {
        if (icon == null) return;
        if (slide > 0f) slide = slideSeconds > 0f ? Mathf.Max(0f, slide - seconds / slideSeconds) : 0f;
        icon.rectTransform.anchoredPosition = new Vector2(0f, slide * slideDistance);
    }

    // Lands on the rolled perk: its name and what it does, BUY and REROLL. celebrate = a pop, a glow flash and sparks
    // (a roll that just ended; not when a box is opened again).
    void Land(bool celebrate)
    {
        rolling = false;
        skippedFrame = Time.frameCount;
        landedAt = Time.unscaledTime;
        slide = 0f;
        ShowFace(result, resultNameColor);
        if (perkDescription != null) perkDescription.text = result.description;
        ShowButtons();
        shownPrice = -1;   // write everything again

        Color color = BoxColor();
        if (glow != null) glow.color = WithAlpha(color, 0.35f);
        if (!celebrate) return;

        if (icon != null)
        {
            icon.transform.localScale = Vector3.one * 1.35f;
            UITween.Scale(this, icon.transform, Vector3.one, 0.35f, UITween.Ease.OutBack);
        }
        if (glow != null)
        {
            glow.color = WithAlpha(color, 0.95f);
            UITween.Color(this, glow, WithAlpha(color, 0.35f), 0.7f);
        }
        if (sparks > 0 && reel != null) UISparks.Burst(reel, color, sparks);
    }

    void ShowFace(PerkData perk, Color nameColor)
    {
        if (icon != null)
        {
            icon.sprite = perk.icon;
            icon.enabled = perk.icon != null;
        }
        if (perkName != null)
        {
            perkName.text = perk.displayName;
            perkName.color = nameColor;
        }
    }

    // SKIP while rolling; BUY and REROLL (if this box can reroll) after it landed.
    void ShowButtons()
    {
        bool canReroll = chest != null && chest.RerollAllowed;
        if (skipButton != null) skipButton.gameObject.SetActive(rolling);
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!rolling);
            ((RectTransform)buyButton.transform).anchoredPosition = canReroll ? buyHome : new Vector2(0f, buyHome.y);
        }
        if (rerollButton != null) rerollButton.gameObject.SetActive(!rolling && canReroll);
        if (hintText != null && chest != null)
        {
            hintText.text = rolling ? "SKIP, " + chest.InteractKey + ", Space or a click: see it now"
                                    : chest.InteractKey + ": close, or walk away";
        }
    }

    // The price, your coins and the buttons' words (only rewritten when something changed).
    void Write()
    {
        int price = chest.OpeningCost;
        int coins = PlayerCoins.Instance != null ? PlayerCoins.Instance.Coins : 0;
        int reroll = chest.RerollAllowed ? chest.RerollCost : -1;   // -1 = no reroll at this box
        bool changes = reroll >= 0 && chest.RerollWouldChange;
        if (price == shownPrice && coins == shownCoins && reroll == shownReroll && changes == shownChanges &&
            rolling == shownRolling) return;

        shownPrice = price;
        shownCoins = coins;
        shownReroll = reroll;
        shownChanges = changes;
        shownRolling = rolling;
        bool canPay = coins >= price;

        if (priceText != null)
        {
            priceText.text = price + (price == 1 ? " coin" : " coins");
            priceText.color = canPay ? canPayColor : cantPayColor;
        }
        if (coinsText != null) coinsText.text = coins.ToString();
        if (needText != null)
        {
            needText.gameObject.SetActive(!canPay && !rolling);
            int missing = price - coins;
            needText.text = "Not enough coins: " + missing + (missing == 1 ? " more coin" : " more coins") + " needed.";
        }
        if (buyButton != null) buyButton.interactable = canPay;
        if (buyLabel != null) buyLabel.text = price <= 0 ? "TAKE IT" : (canPay ? "BUY  " : "NEED ") + CoinWords(price);
        if (reroll >= 0)
        {
            if (rerollButton != null) rerollButton.interactable = changes && coins >= reroll;
            if (rerollLabel != null) rerollLabel.text = changes ? "REROLL  " + CoinWords(reroll) : "NOTHING NEW TO ROLL";
        }
    }

    // BUY: Marko's chest pays and gives the perk (and bursts). Only a press made after the roll landed counts, so the
    // click that skipped the roll can't buy by accident.
    void Buy()
    {
        if (!IsOpen || rolling || lastPress <= landedAt || chest == null || !chest.CanBuyRolled) return;
        if (PlayerCoins.Instance == null || PlayerCoins.Instance.Coins < chest.OpeningCost) return;

        PerkChest buying = chest;
        Close();
        buying.BuyRolled();
    }

    // REROLL: Marko's chest pays and rolls again (it calls Open, which plays the roll), or does nothing if it can't.
    void Reroll()
    {
        if (!IsOpen || rolling || lastPress <= landedAt || chest == null) return;
        chest.TryReroll();
    }

    void Skip()
    {
        if (IsOpen && rolling) Land(true);
    }

    // The box's name and color as the title ("EPIC BOX").
    void WriteTitle()
    {
        ChestTier tier = chest != null ? chest.Tier : null;
        Color color = BoxColor();
        if (titleText != null)
        {
            titleText.text = tier != null ? tier.BoxName.ToUpperInvariant() : "LUCKY ROLL";
            titleText.color = color;
        }
        if (titleLine != null) titleLine.color = WithAlpha(color, lineAlpha.a);
    }

    // The box's color (Tony's ChestTier), else gold.
    Color BoxColor()
    {
        return chest != null && chest.Tier != null ? chest.Tier.Color : resultNameColor;
    }

    static Color WithAlpha(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

    // 1 -> "1 COIN", 12 -> "12 COINS".
    static string CoinWords(int coins) => coins + (coins == 1 ? " COIN" : " COINS");

    // The window grows in from a little smaller, with a small bounce.
    void PopIn()
    {
        if (window == null || popSeconds <= 0f) return;
        window.localScale = Vector3.one * 0.9f;
        UITween.Scale(this, window, Vector3.one, popSeconds, UITween.Ease.OutBack);
    }
}
