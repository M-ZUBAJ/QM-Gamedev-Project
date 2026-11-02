/*
First Date: 2/11/2026
Date: 2/11/2026
Author: Tony
Use by: Put this on the "Feed" object of NotificationCanvas (Tools > UI > Build Notifications makes it all):
        an object with a Vertical Layout Group, and a switched-off child named "Message" (a box with an "Icon"
        Image and a "Text" TextMeshPro text) that every message is copied from.
Unity Editor: GameRules > Notifications: Show Notifications (on / off) and Notification Side (Left / Right),
              also while playing. To change where the messages stack, move or resize the Feed in the prefab;
              Left / Right only flips it to the other edge of the screen.
              Testing: Play and harvest a tree (or pick an item up): "+3 Wood" shows on the side, then fades away.
Purpose: Shows short messages on the side of the screen for the player, newest at the bottom, each fading away
         after a few seconds (like the pickup messages in Valheim). Any script can show one:
           NotificationFeed.Show("Night is coming!");
           NotificationFeed.ShowGain("Coins", "Coins", 5, coinSprite);   // "+5 Coins"
         A gain with the same key while its message is still on screen adds up instead of making a new line:
         "+3 Wood" then 2 more Wood = "+5 Wood".
         Nothing shows while GameRules.ShowNotifications is off.
Requirement: NotificationFeed.cs x-> GameRules.cs, VerticalLayoutGroup (Unity)
             NotificationFeed.cs <-x GainNotifier.cs, NotificationUIBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
[DisallowMultipleComponent]
public class NotificationFeed : MonoBehaviour
{
    // One message on screen.
    class Message
    {
        public GameObject root;
        public CanvasGroup group;   // fades the whole message
        public TMP_Text text;
        public Image icon;          // can be missing
        public string key;          // gains with the same key add up; null = a plain message, never adds up
        public string name;         // e.g. "Wood"
        public int amount;          // e.g. 3
        public float age;           // seconds since it showed (or last grew)
    }

    // ===== Fields =====
    [Range(0.5f, 15f)]
    [Tooltip("How long a message stays (seconds) before it starts to fade away.")]
    [SerializeField] float showSeconds = 3f;

    [Range(0f, 3f)]
    [Tooltip("How long the fading takes (seconds). 0 = it just disappears.")]
    [SerializeField] float fadeSeconds = 0.6f;

    [Range(1, 12)]
    [Tooltip("Most messages on screen at once. One more = the oldest goes straight away.")]
    [SerializeField] int maxMessages = 5;

    [Tooltip("How a gain is written: {0} = the amount, {1} = the name. " +
             "\"+{0} {1}\" = \"+3 Wood\", \"+ {0} {1}\" = \"+ 3 Wood\".")]
    [SerializeField] string gainFormat = "+{0} {1}";

    [Tooltip("The message every new one is copied from (switched off). Empty = the child named \"Message\".")]
    [SerializeField] GameObject template;

    const float PopSeconds = 0.15f;   // a new (or growing) message starts a little bigger and shrinks back in this time
    const float PopSize = 0.15f;      // 0.15 = 15% bigger

    static NotificationFeed instance;   // the feed in the scene: every Show goes to it
    static bool warned;                 // the "no feed in the scene" note was already written to the Console

    readonly List<Message> messages = new List<Message>();   // on screen now, oldest first
    VerticalLayoutGroup column;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        column = GetComponent<VerticalLayoutGroup>();
        if (template == null)
        {
            Transform t = transform.Find("Message");
            if (t != null) template = t.gameObject;
        }
        if (template != null) template.SetActive(false);   // only its copies are shown
    }

    void OnEnable()
    {
        instance = this;
        GameRules.Changed += OnRulesChanged;
        ApplySide();
    }

    void OnDisable()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear them
        GameRules.Changed -= OnRulesChanged;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;   // real time: messages still go away while the game is paused

        for (int i = messages.Count - 1; i >= 0; i--)   // backwards, so removing one doesn't skip the next
        {
            Message m = messages[i];
            m.age += dt;
            if (m.age >= showSeconds + fadeSeconds) { Remove(i); continue; }

            // Full until showSeconds, then fading to nothing over fadeSeconds.
            float fade = fadeSeconds > 0f ? Mathf.Clamp01((m.age - showSeconds) / fadeSeconds) : 0f;
            m.group.alpha = 1f - fade;

            // The pop: a little bigger at first, normal size after PopSeconds.
            float pop = 1f + PopSize * Mathf.Clamp01(1f - m.age / PopSeconds);
            m.root.transform.localScale = new Vector3(pop, pop, 1f);
        }
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        instance = null;
        warned = false;
    }

    // ===== My methods =====

    // Shows a message, e.g. NotificationFeed.Show("Night is coming!"). icon = an optional picture in front of it.
    public static void Show(string text, Sprite icon = null)
    {
        NotificationFeed feed = Ready();
        if (feed == null) return;

        Message m = feed.NewMessage(icon);
        if (m != null) m.text.text = text;
    }

    // Shows a gain, e.g. ShowGain("Wood", "Wood", 3, woodIcon) = "+3 Wood".
    // key = what adds up: another gain with the same key while this one is on screen turns it into "+5 Wood".
    public static void ShowGain(string key, string name, int amount, Sprite icon = null)
    {
        if (amount <= 0) return;
        NotificationFeed feed = Ready();
        if (feed != null) feed.AddGain(key, name, amount, icon);
    }

    // The feed, if messages can show right now: the rule is on and there is a feed in the scene.
    static NotificationFeed Ready()
    {
        if (!GameRules.ShowNotifications) return null;

        if (instance == null && !warned)
        {
            warned = true;
            Debug.Log("NotificationFeed: there is no notification feed in the scene, so messages aren't shown. " +
                      "Add one with Tools > UI > Build Notifications.");
        }
        return instance;
    }

    // A gain line: the one already on screen for this key, grown, or a new one.
    void AddGain(string key, string name, int amount, Sprite icon)
    {
        Message m = FindByKey(key);
        if (m != null)
        {
            Restart(m);   // full again, pops, moves to the bottom (newest)
        }
        else
        {
            m = NewMessage(icon);
            if (m == null) return;
            m.key = key;
            m.name = name;
        }

        m.amount += amount;
        m.text.text = GainText(m.amount, m.name);
    }

    // "+3 Wood", written the way Gain Format says.
    string GainText(int amount, string itemName)
    {
        try
        {
            return string.Format(gainFormat, amount, itemName);
        }
        catch (System.FormatException)
        {
            return "+" + amount + " " + itemName;   // a typo in Gain Format (e.g. "{2}"): the normal way
        }
    }

    // A new line at the bottom (newest), copied from the template. Too many lines: the oldest goes.
    Message NewMessage(Sprite icon)
    {
        if (template == null)
        {
            Debug.LogWarning(name + ": no message template. Add a switched-off child named \"Message\" " +
                             "(Tools > UI > Build Notifications makes one).", this);
            return null;
        }

        GameObject go = Instantiate(template, transform);
        go.name = "Message (shown)";
        go.SetActive(true);

        Message m = new Message { root = go, text = go.GetComponentInChildren<TMP_Text>(true) };
        if (m.text == null)
        {
            Debug.LogWarning(name + ": the message template has no TextMeshPro text.", this);
            Destroy(go);
            return null;
        }

        m.group = go.GetComponent<CanvasGroup>();
        if (m.group == null) m.group = go.AddComponent<CanvasGroup>();
        m.group.blocksRaycasts = false;   // messages never block clicks

        Transform iconChild = go.transform.Find("Icon");
        m.icon = iconChild != null ? iconChild.GetComponent<Image>() : null;
        if (m.icon != null)
        {
            m.icon.sprite = icon;
            m.icon.gameObject.SetActive(icon != null);   // no picture: only the text
        }

        Restart(m);   // adds it to the list as the newest
        while (messages.Count > Mathf.Max(1, maxMessages)) Remove(0);
        return m;
    }

    // Back to the start: fully visible, popping, and the newest (at the bottom).
    void Restart(Message m)
    {
        m.age = 0f;
        m.group.alpha = 1f;
        m.root.transform.SetAsLastSibling();

        messages.Remove(m);   // (does nothing for a new message)
        messages.Add(m);
    }

    // The message on screen for this key, or null.
    Message FindByKey(string key)
    {
        if (key == null) return null;
        foreach (Message m in messages)
        {
            if (m.key == key) return m;
        }
        return null;
    }

    void Remove(int i)
    {
        Destroy(messages[i].root);
        messages.RemoveAt(i);
    }

    void OnRulesChanged()
    {
        if (!GameRules.ShowNotifications)   // switched off: the messages on screen go too
        {
            for (int i = messages.Count - 1; i >= 0; i--) Remove(i);
        }
        ApplySide();
    }

    // Puts the feed on the side GameRules says, keeping its distance from the screen edge and its height.
    void ApplySide()
    {
        bool right = GameRules.NotificationSide == NotificationSide.Right;
        float x = right ? 1f : 0f;
        RectTransform rt = (RectTransform)transform;

        rt.anchorMin = new Vector2(x, rt.anchorMin.y);
        rt.anchorMax = new Vector2(x, rt.anchorMax.y);
        rt.pivot = new Vector2(x, rt.pivot.y);
        float fromEdge = Mathf.Abs(rt.anchoredPosition.x);
        rt.anchoredPosition = new Vector2(right ? -fromEdge : fromEdge, rt.anchoredPosition.y);

        // The messages line up on that side too (the top / middle / bottom part of the alignment stays as set).
        if (column != null) column.childAlignment = OnSide(column.childAlignment, right);
    }

    // The same alignment row (upper, middle or lower), on the left or the right.
    static TextAnchor OnSide(TextAnchor alignment, bool right)
    {
        int row = (int)alignment / 3;   // TextAnchor goes UpperLeft, UpperCenter, UpperRight, MiddleLeft... 3 per row
        return (TextAnchor)(row * 3 + (right ? 2 : 0));
    }
}
