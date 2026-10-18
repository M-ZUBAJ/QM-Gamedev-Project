/*
First Date: 18/10/2026
Date: 18/10/2026
Author: Tony
Use by: Put this on the coin counter in the HUD (Tools > UI > Build HUD makes it: HUDCanvas > TopRight > CoinCounter),
        an object with two children: "Icon" (an Image) and "Text" (a TextMeshPro text).
Unity Editor: Choose the Look; only the fields that look needs are shown (CoinCounterEditor):
                Auto Icon     = a gold coin drawn by this script (no art needed)
                Static Icon   = drag a PNG (imported as Sprite) into Static Icon
                Animated Icon = drag an .aseprite file into Aseprite (frames and timing are read by themselves)
                Text Name     = no picture, a word instead, e.g. "Coins: 12"
              Text Color works with every look. Nothing else to connect: it finds Marko's PlayerCoins by itself.
              Testing: Play and defeat an enemy (it gives 1 - 3 coins), or select the Player and call
              PlayerCoins.AddCoins from a test script; the number goes up.
Purpose: Shows how many coins the player has (Marko's PlayerCoins.Instance.Coins), and redraws whenever the total
         changes (PlayerCoins.CoinsChanged). Waits quietly if the player isn't there yet.
Requirement: CoinCounter.cs x-> PlayerCoins.cs, AsepriteFrames.cs
             CoinCounter.cs <-x CoinCounterEditor.cs, HUDBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;   // Image

// How the coin is shown. New looks get a new line here, and then show in the dropdown.
public enum CoinLook
{
    AutoIcon,       // a gold coin drawn by the script
    StaticIcon,     // a still picture (PNG imported as Sprite)
    AnimatedIcon,   // an .aseprite animation
    TextName,       // a word instead of a picture, e.g. "Coins: 12"
}

[ExecuteAlways]   // also in Edit mode, so changing the Look shows straight away
[DisallowMultipleComponent]
public class CoinCounter : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("How the coin is shown: a drawn coin, a picture, an animation, or a word.")]
    [SerializeField] CoinLook look = CoinLook.AutoIcon;

    [Tooltip("The coin picture (a PNG imported as Sprite).")]
    [SerializeField] Sprite staticIcon;

    [Tooltip("The coin animation (.aseprite file).")]
    [SerializeField] Object aseprite;

    [Tooltip("Optional: the Aseprite tag (animation name) to play when the file has several. Empty = the first one.")]
    [SerializeField] string asepriteTag = "";

    [Tooltip("The word shown before the number, e.g. \"Coins\" -> \"Coins: 12\".")]
    [SerializeField] string textName = "Coins";

    [Tooltip("Color of the number (and the word).")]
    [SerializeField] Color textColor = new Color(1f, 0.85f, 0.2f);   // gold

    [Tooltip("The picture. Empty = the child named \"Icon\".")]
    [SerializeField] Image icon;

    [Tooltip("The number. Empty = the child named \"Text\".")]
    [SerializeField] TMP_Text text;

    // Read from the .aseprite file in the editor and saved with the scene / prefab, so they work in a built game too.
    [SerializeField, HideInInspector] Sprite[] frames = new Sprite[0];
    [SerializeField, HideInInspector] float[] durations = new float[0];

    const float SearchInterval = 0.5f;   // seconds between looks for the player while PlayerCoins is missing
    const float FallbackFps = 10f;       // animation speed when the file has no timing

    static Sprite autoCoin;              // the drawn coin, made once and shared

    PlayerCoins coins;                   // Marko's script on the player (null until found)
    float nextSearch;
    bool dirty = true;                   // something changed: redraw at the next Update
    int frame;
    float frameTimer;

    public CoinLook Look => look;

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        FindChildren();
        dirty = true;
    }

    void OnDisable()
    {
        if (coins != null) coins.CoinsChanged -= OnCoinsChanged;
        coins = null;
    }

    void Update()
    {
        if (Application.isPlaying && coins == null) FindCoins();
        if (look == CoinLook.AnimatedIcon) Animate();
        if (dirty) Redraw();
    }

    // Unity calls this when a value is changed in the Inspector.
    // Only reads the file and marks "redraw": changing objects isn't allowed in here, Update does it.
    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) ReadAseprite();
#endif
        dirty = true;
    }

    // ===== My methods =====

    void FindChildren()
    {
        if (icon == null)
        {
            Transform t = transform.Find("Icon");
            if (t != null) icon = t.GetComponent<Image>();
        }
        if (text == null)
        {
            Transform t = transform.Find("Text");
            text = t != null ? t.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);
        }
    }

    // Looks for Marko's PlayerCoins, at most twice a second, until it's there (e.g. the player spawns later).
    void FindCoins()
    {
        if (Time.unscaledTime < nextSearch) return;
        nextSearch = Time.unscaledTime + SearchInterval;

        coins = PlayerCoins.Instance;
        if (coins == null) return;

        coins.CoinsChanged += OnCoinsChanged;   // redraw whenever the total changes
        dirty = true;
    }

    void OnCoinsChanged(int total)
    {
        dirty = true;
    }

    void Redraw()
    {
        dirty = false;
        if (text == null || icon == null) FindChildren();

        int total = coins != null ? coins.Coins : 0;
        bool word = look == CoinLook.TextName;

        if (text != null)
        {
            text.text = word ? textName + ": " + total : total.ToString();
            text.color = textColor;
        }

        if (icon == null) return;
        if (icon.gameObject.activeSelf == word) icon.gameObject.SetActive(!word);   // no picture for Text Name
        if (word) return;

        icon.color = Color.white;   // the picture's own colors
        icon.preserveAspect = true;
        if (look == CoinLook.AutoIcon)
        {
            // The drawn coin only exists while playing (it's made in code and must never be saved into the scene),
            // so in Edit mode a plain gold square stands in for it.
            icon.sprite = Application.isPlaying ? AutoCoin() : null;
            if (!Application.isPlaying) icon.color = new Color(1f, 0.8f, 0.15f);
        }
        else if (look == CoinLook.StaticIcon) icon.sprite = staticIcon;
        else icon.sprite = frames.Length > 0 ? frames[Mathf.Min(frame, frames.Length - 1)] : null;
    }

    // Steps through the animation frames (real time, so it moves even when the game is paused).
    void Animate()
    {
        if (frames.Length < 2 || icon == null) return;

        frameTimer += Application.isPlaying ? Time.unscaledDeltaTime : 0f;   // still in Edit mode: just the first frame
        float d = frame < durations.Length && durations[frame] > 0f ? durations[frame] : 1f / FallbackFps;
        if (frameTimer < d) return;

        frameTimer -= d;
        frame = (frame + 1) % frames.Length;
        icon.sprite = frames[frame];
    }

    // A small pixel-art gold coin (16 x 16), drawn once: dark rim, gold face, light shine in the top left.
    static Sprite AutoCoin()
    {
        if (autoCoin != null) return autoCoin;

        const int size = 16;
        Color rim = new Color(0.62f, 0.4f, 0.05f);
        Color face = new Color(1f, 0.8f, 0.15f);
        Color shine = new Color(1f, 0.96f, 0.6f);
        Color clear = new Color(0f, 0f, 0f, 0f);

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;   // sharp pixels, no blur
        tex.hideFlags = HideFlags.DontSave;  // made in code: never saved into the scene

        Vector2 middle = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), middle);
                Color c = clear;
                if (d <= 7.5f) c = rim;                               // the outer ring
                if (d <= 6.2f) c = face;                              // the gold inside
                if (d <= 6.2f && x - y < -4f && d > 3.5f) c = shine;  // a shine on the top left edge
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        autoCoin = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        autoCoin.hideFlags = HideFlags.DontSave;
        return autoCoin;
    }

#if UNITY_EDITOR
    // Fills frames and durations from the .aseprite file (see AsepriteFrames).
    void ReadAseprite()
    {
        if (look != CoinLook.AnimatedIcon || aseprite == null)
        {
            frames = new Sprite[0];
            durations = new float[0];
            return;
        }

        if (!AsepriteFrames.IsAseprite(aseprite))
        {
            Debug.LogWarning(name + ": Aseprite must be an .aseprite file.", this);
            aseprite = null;
            return;
        }

        AsepriteFrames.Read(aseprite, asepriteTag, out frames, out durations);
        frame = 0;
    }
#endif
}
