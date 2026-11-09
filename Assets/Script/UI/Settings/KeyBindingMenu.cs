/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Put this on the Key Bindings page (Tools > UI > Build Main Title Scene does it: SettingsPage > TabPages >
        KeyBindingsPage). It needs, by NAME: "Content" (the scroll list's content, with a Vertical Layout Group),
        and switched-off templates "HeaderTemplate" (a text), "RowTemplate" (a "Label" text and the Buttons "Key"
        and "Key2", each with a text) and "HotbarRowTemplate" (a "Label" and the Buttons "Key1" ... "Key8").
        Optional: a "Hint" text and a "ResetAllButton".
Unity Editor: Nothing to connect. Which keys are listed is the Rows table in this script.
              Testing: Play, open Settings, click a key, press another key. The choice is saved (KeyBindings), so it
              also works in the game.
Purpose: The key binding list. Click a key box, then press the new key (Esc = cancel):
           - the box pulses gold while it waits, then pops when the new key is set;
           - if another row (used at the same time) had that key, the two swap, and that row shakes red so you see
             what moved. Normal play and building are separate: left click can place buildings AND do other things;
           - keys hard-coded in teammates' scripts (Attack, Fireball) are shown greyed: they can't change yet, and
             choosing one of those keys for something else gives a warning shake.
         RESET ALL puts every key back to its default, the boxes flipping over in a wave down the list.
Requirement: KeyBindingMenu.cs x-> KeyBindings.cs, UITween.cs
             KeyBindingMenu.cs <-x TitleSceneBuilder.cs
             KeyBindingMenu.cs <-o TitleMenu.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class KeyBindingMenu : MonoBehaviour
{
    // When a key is used: keys only clash with keys used at the same time.
    enum When { Playing, Building }

    // One row of the list. keys = what its key box changes (several = they share one key on purpose),
    // second = the second key box (null = none). No keys and no fixed key = the hotbar row (8 boxes).
    struct Row
    {
        public string group, label, fixedKey;
        public When when;
        public KeyAction[] keys, second;

        public Row(string group, string label, When when, KeyAction[] keys, KeyAction[] second = null, string fixedKey = null)
        {
            this.group = group;
            this.label = label;
            this.when = when;
            this.keys = keys;
            this.second = second;
            this.fixedKey = fixedKey;
        }
    }

    // One clickable key box.
    class Slot
    {
        public KeyAction[] actions;
        public When when;
        public string name;          // for the hint, e.g. "Jump" or "Move forward (2nd key)"
        public Button button;
        public TMP_Text text;
        public RectTransform row;    // the whole row (it shakes)
        public Image rowImage;       // the row's background (it flashes red)
    }

    // ===== Fields =====
    // The list, top to bottom. A new key = a new line here (and a KeyAction in KeyBindings).
    static readonly Row[] Rows =
    {
        new Row("MOVEMENT", "Move forward", When.Playing, new[] { KeyAction.MoveForward }, new[] { KeyAction.MoveForwardAlt }),
        new Row("MOVEMENT", "Move back", When.Playing, new[] { KeyAction.MoveBack }, new[] { KeyAction.MoveBackAlt }),
        new Row("MOVEMENT", "Move left", When.Playing, new[] { KeyAction.MoveLeft }, new[] { KeyAction.MoveLeftAlt }),
        new Row("MOVEMENT", "Move right", When.Playing, new[] { KeyAction.MoveRight }, new[] { KeyAction.MoveRightAlt }),
        new Row("MOVEMENT", "Jump", When.Playing, new[] { KeyAction.Jump }),
        new Row("MOVEMENT", "Sprint", When.Playing, new[] { KeyAction.Sprint }),
        new Row("ITEMS", "Grab item", When.Playing, new[] { KeyAction.Grab }),
        new Row("ITEMS", "Eat / Levitate", When.Playing, new[] { KeyAction.Consume, KeyAction.Levitate, KeyAction.Interact }),
        new Row("ITEMS", "Inventory", When.Playing, new[] { KeyAction.Inventory }),
        new Row("ITEMS", "Crafting", When.Playing, new[] { KeyAction.Crafting }),
        new Row("ITEMS", "Hotbar 1 - 8", When.Playing, null),
        new Row("BUILDING", "Build menu", When.Playing, new[] { KeyAction.Build }),
        new Row("BUILDING", "Place building", When.Building, new[] { KeyAction.PlaceBuilding }),
        new Row("BUILDING", "Cancel building", When.Building, new[] { KeyAction.CancelBuilding }),
        new Row("BUILDING", "Snap / Surface", When.Building, new[] { KeyAction.AutoPlace }),
        new Row("CAMERA", "Change view", When.Playing, new[] { KeyAction.ToggleView }),
        new Row("CAMERA", "Free the mouse", When.Playing, new[] { KeyAction.UnlockCursor }),
        new Row("FIXED (can't change yet)", "Attack", When.Playing, null, null, "Left click"),
        new Row("FIXED (can't change yet)", "Fireball", When.Playing, null, null, "Q"),
    };

    // Keys read straight from the keyboard in teammates' scripts (Marko's PlayerCombat / PlayerFireball), with their use.
    static readonly KeyCode[] FixedKeys = { KeyCode.Mouse0, KeyCode.Q };
    static readonly string[] FixedUses = { "Attack", "Fireball" };

    const string NormalHint = "Click a key, then press the new one (Esc = cancel).";
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    static readonly Color Warning = new Color(1f, 0.3f, 0.25f, 0.45f);
    static readonly Color Greyed = new Color(1f, 1f, 1f, 0.4f);   // rows that can't change

    static KeyCode[] allKeys;              // every key Unity knows, to find the one pressed
    static KeyBindingMenu listeningMenu;   // the menu waiting for a key right now (null = none)

    readonly List<Slot> slots = new List<Slot>();
    TMP_Text hint;
    Slot listening;        // the box waiting for a key (null = none)
    int listenFrame;       // the frame it started waiting (that click must not count as the new key)
    float pulse;

    // True while a key box waits for a key (e.g. so Esc cancels it instead of leaving the page).
    public static bool IsListening => listeningMenu != null;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        if (allKeys == null) allKeys = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));
        hint = FindDeep(transform, "Hint")?.GetComponent<TMP_Text>();
        Button reset = FindDeep(transform, "ResetAllButton")?.GetComponent<Button>();
        if (reset != null) reset.onClick.AddListener(ResetAll);
        BuildList();
    }

    void OnEnable()
    {
        KeyBindings.Changed += Refresh;
        Refresh();
        ShowHint(NormalHint, false);
    }

    void OnDisable()
    {
        KeyBindings.Changed -= Refresh;
        StopListening();
    }

    void Update()
    {
        if (listening == null) return;

        // The waiting box pulses gold.
        pulse += UITween.DeltaTime;
        float wave = 0.5f + 0.5f * Mathf.Sin(pulse * 9f);
        listening.text.color = Color.Lerp(Color.white, Gold, wave);
        listening.text.transform.localScale = Vector3.one * (1f + 0.06f * wave);

        if (Time.frameCount == listenFrame) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { StopListening(); ShowHint(NormalHint, true); return; }

        foreach (KeyCode key in allKeys)
        {
            if (key == KeyCode.None || !Input.GetKeyDown(key)) continue;
            Assign(listening, key);
            break;
        }
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        listeningMenu = null;
    }

    // ===== My methods =====

    // Makes one row (or header) per entry of the Rows table, from the templates.
    void BuildList()
    {
        Transform content = FindDeep(transform, "Content");
        Transform headerTemplate = FindDeep(transform, "HeaderTemplate");
        Transform rowTemplate = FindDeep(transform, "RowTemplate");
        Transform hotbarTemplate = FindDeep(transform, "HotbarRowTemplate");
        if (content == null || headerTemplate == null || rowTemplate == null || hotbarTemplate == null)
        {
            Debug.LogWarning("KeyBindingMenu: missing Content or a template. Run Tools > UI > Build Main Title Scene.", this);
            return;
        }

        string group = null;
        foreach (Row row in Rows)
        {
            if (row.group != group)
            {
                group = row.group;
                Transform header = Clone(headerTemplate, content);
                header.GetComponentInChildren<TMP_Text>(true).text = group;
            }

            if (row.keys == null && row.fixedKey == null)   // the hotbar: 8 small boxes
            {
                Transform line = Clone(hotbarTemplate, content);
                SetLabel(line, row.label);
                for (int i = 0; i < KeyBindings.HotbarKeyCount; i++)
                    AddSlot(line, "Key" + (i + 1), new[] { KeyAction.Hotbar1 + i }, row.when, "Hotbar " + (i + 1));
                continue;
            }

            Transform rowObject = Clone(rowTemplate, content);
            SetLabel(rowObject, row.label);

            if (row.fixedKey != null)   // can't change: greyed text and a box that can't be clicked
            {
                Transform key = FindDeep(rowObject, "Key");
                key.GetComponent<Button>().interactable = false;
                TMP_Text keyText = key.GetComponentInChildren<TMP_Text>(true);
                keyText.text = row.fixedKey;
                keyText.color = Greyed;
                FindDeep(rowObject, "Label").GetComponent<TMP_Text>().color = Greyed;
                FindDeep(rowObject, "Key2").gameObject.SetActive(false);
                continue;
            }

            AddSlot(rowObject, "Key", row.keys, row.when, row.label);
            if (row.second != null) AddSlot(rowObject, "Key2", row.second, row.when, row.label + " (2nd key)");
            else FindDeep(rowObject, "Key2").gameObject.SetActive(false);
        }
    }

    void AddSlot(Transform row, string buttonName, KeyAction[] actions, When when, string slotName)
    {
        Transform t = FindDeep(row, buttonName);
        if (t == null) return;

        Slot slot = new Slot
        {
            actions = actions,
            when = when,
            name = slotName,
            button = t.GetComponent<Button>(),
            text = t.GetComponentInChildren<TMP_Text>(true),
            row = (RectTransform)row,
            rowImage = row.GetComponent<Image>(),
        };
        slot.button.onClick.AddListener(() => StartListening(slot));
        slots.Add(slot);
    }

    // Writes every key's name in its box.
    void Refresh()
    {
        foreach (Slot s in slots)
        {
            if (s != listening) s.text.text = KeyName(KeyBindings.Get(s.actions[0]));
        }
    }

    void StartListening(Slot slot)
    {
        StopListening();
        listening = slot;
        listeningMenu = this;
        listenFrame = Time.frameCount;
        pulse = 0f;
        slot.text.text = "press a key...";
        ShowHint("Press the new key for " + slot.name + " (Esc = cancel).", true);
    }

    void StopListening()
    {
        if (listening != null)
        {
            listening.text.color = Color.white;
            listening.text.transform.localScale = Vector3.one;
            listening.text.text = KeyName(KeyBindings.Get(listening.actions[0]));
        }
        listening = null;
        if (listeningMenu == this) listeningMenu = null;
    }

    // Gives the box its new key. Another box used at the same time with that key gets this box's old key (a swap).
    void Assign(Slot slot, KeyCode key)
    {
        KeyCode old = KeyBindings.Get(slot.actions[0]);
        StopListening();
        if (key == old) { Pop(slot); ShowHint(NormalHint, true); return; }

        string note = slot.name + ": " + KeyName(key);
        foreach (Slot other in slots)
        {
            if (other == slot || other.when != slot.when || KeyBindings.Get(other.actions[0]) != key) continue;

            KeyBindings.SetSeveral(other.actions, old);   // the swap
            Flip(other, 0f);
            Shake(other);
            note = KeyName(key) + " was used by " + other.name + ", so " + other.name + " now uses " + KeyName(old) + ".";
        }

        int fixedIndex = System.Array.IndexOf(FixedKeys, key);
        if (slot.when == When.Playing && fixedIndex >= 0)
        {
            Shake(slot);
            note = KeyName(key) + " also does " + FixedUses[fixedIndex] + " (that one can't change yet).";
        }

        KeyBindings.SetSeveral(slot.actions, key);
        Pop(slot);
        ShowHint(note, true);
    }

    // Every key back to its default, the boxes flipping over in a wave down the list.
    void ResetAll()
    {
        StopListening();
        KeyBindings.ResetToDefaults();
        for (int i = 0; i < slots.Count; i++) Flip(slots[i], i * 0.025f);
        ShowHint("Every key is back to its default.", true);
    }

    // ----- Little animations -----

    // The new key pops: bigger and gold, then back to normal.
    void Pop(Slot slot)
    {
        Transform t = slot.text.transform;
        UITween.Run(this, 0.4f, UITween.Ease.OutBack, k =>
        {
            t.localScale = Vector3.one * Mathf.LerpUnclamped(1.4f, 1f, k);
            slot.text.color = Color.Lerp(Gold, Color.white, Mathf.Clamp01(k));
        });
    }

    // The box flips over like a card (it shrinks to a line and comes back).
    void Flip(Slot slot, float delay)
    {
        Transform t = slot.button.transform;
        UITween.Run(this, 0.13f, UITween.Ease.InCubic, k => t.localScale = new Vector3(1f - k, 1f, 1f), () =>
            UITween.Run(this, 0.22f, UITween.Ease.OutBack, k => t.localScale = new Vector3(k, 1f, 1f)), delay);
    }

    // The row shakes sideways and flashes red (something moved, or a warning).
    void Shake(Slot slot)
    {
        RectTransform row = slot.row;
        float home = row.anchoredPosition.x;
        Color normal = slot.rowImage != null ? slot.rowImage.color : Color.clear;
        UITween.Run(this, 0.45f, UITween.Ease.Linear, k =>
        {
            row.anchoredPosition = new Vector2(home + Mathf.Sin(k * Mathf.PI * 6f) * 14f * (1f - k), row.anchoredPosition.y);
            if (slot.rowImage != null) slot.rowImage.color = Color.Lerp(Warning, normal, k);
        });
    }

    // The line under the list (fades over to the new text).
    void ShowHint(string text, bool animate)
    {
        if (hint == null || hint.text == text) return;
        if (!animate || !isActiveAndEnabled) { hint.text = text; hint.alpha = 1f; return; }

        UITween.Run(this, 0.1f, UITween.Ease.Linear, k => hint.alpha = 1f - k, () =>
        {
            hint.text = text;
            UITween.Run(this, 0.2f, UITween.Ease.OutCubic, k => hint.alpha = k);
        });
    }

    // ----- Helpers -----

    // A short, friendly name for a key: Mouse0 -> "Left click", Alpha1 -> "1", LeftShift -> "Left Shift".
    public static string KeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None: return "-";
            case KeyCode.Mouse0: return "Left click";
            case KeyCode.Mouse1: return "Right click";
            case KeyCode.Mouse2: return "Middle click";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Return: return "Enter";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.BackQuote: return "`";
            case KeyCode.Minus: return "-";
            case KeyCode.Equals: return "=";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.Semicolon: return ";";
            case KeyCode.Quote: return "'";
            case KeyCode.Comma: return ",";
            case KeyCode.Period: return ".";
            case KeyCode.Slash: return "/";
            case KeyCode.Backslash: return "\\";
        }

        string name = key.ToString();
        if (name.StartsWith("Alpha")) return name.Substring(5);                // Alpha1 -> 1
        if (name.StartsWith("Keypad")) return "Num " + name.Substring(6);      // Keypad1 -> Num 1
        if (name.StartsWith("Mouse")) return "Mouse " + name.Substring(5);     // Mouse3 -> Mouse 3

        // "LeftShift" -> "Left Shift", "UpArrow" -> "Up Arrow": a space before each capital inside the word.
        System.Text.StringBuilder spaced = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) spaced.Append(' ');
            spaced.Append(name[i]);
        }
        return spaced.ToString();
    }

    static Transform Clone(Transform template, Transform parent)
    {
        Transform copy = Instantiate(template, parent);
        copy.name = template.name.Replace("Template", "");
        copy.gameObject.SetActive(true);
        return copy;
    }

    static void SetLabel(Transform row, string text)
    {
        Transform label = FindDeep(row, "Label");
        if (label != null) label.GetComponent<TMP_Text>().text = text;
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
