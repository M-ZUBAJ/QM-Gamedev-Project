/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: Put this on the DISPLAY page (Tools > UI > Build Main Title Scene does it on the title screen's Settings page,
        Tools > UI > Build Pause Menu in the Esc pause menu). It finds by NAME: "FullscreenRow", "DamageRow" and
        "DamageCreaturesRow" (each with a Toggle inside) and, optionally, "LockNote" and "Note" (texts in the
        DamageCreaturesRow), "DamagePreview", "ResetDisplayButton" and a "Hint" text.
Unity Editor: Nothing to connect. Testing: Play, open DISPLAY: untick Show damage numbers, and Only monsters and animals
              greys out and can't be ticked; tick it again and hit a tree: a number pops up (tick Only monsters and
              animals: trees show none, slimes do).
Purpose: The display settings (DisplaySettings, saved on the computer):
           - Fullscreen (also Alt + Enter or F11). In the web build the browser fills the screen and Esc leaves it; the
             tick follows what the screen really does. The tick asks for it as soon as the mouse button goes down: the
             browser only allows fullscreen while it handles the player's click, and Unity passes the request on with
             the next mouse or key event (a normal tick changes when the button comes up: it needed a second click);
           - Show damage numbers, and inside its panel, under it, Only monsters and animals: it belongs to Show damage
             numbers, so while that's off it's greyed out, locked (can't be ticked) and says why;
           - RESET: damage numbers back on, for every hit.
Requirement: DisplayMenu.cs x-> DisplaySettings.cs, Toggle (Unity), EventTrigger (Unity)
             DisplayMenu.cs <-x TitleSceneBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DisplayMenu : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("How see-through the locked child setting is (Only monsters and animals while Show damage numbers is off).")]
    [Range(0f, 1f)]
    [SerializeField] float lockedAlpha = 0.35f;

    const string FullscreenName = "FullscreenRow";
    const string DamageName = "DamageRow";
    const string CreaturesName = "DamageCreaturesRow";
    const string NormalHint = "Changes work at once. Damage numbers are saved on this computer.";

    Toggle fullscreen, damage, creatures;
    CanvasGroup creaturesFade;   // the child row: greyed out while locked
    GameObject lockNote, note;   // "Turn on Show damage numbers first" / what the child does
    CanvasGroup previewFade;     // the sample numbers dim while damage numbers are off
    TMP_Text hint;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        fullscreen = ToggleIn(FullscreenName);
        damage = ToggleIn(DamageName);
        creatures = ToggleIn(CreaturesName);
        Transform creaturesRow = FindDeep(transform, CreaturesName);
        if (creaturesRow != null)
        {
            creaturesFade = creaturesRow.GetComponent<CanvasGroup>();
            if (creaturesFade == null) creaturesFade = creaturesRow.gameObject.AddComponent<CanvasGroup>();
            lockNote = FindDeep(creaturesRow, "LockNote")?.gameObject;
            note = FindDeep(creaturesRow, "Note")?.gameObject;
        }
        Transform preview = FindDeep(transform, "DamagePreview");
        if (preview != null)
        {
            previewFade = preview.GetComponent<CanvasGroup>();
            if (previewFade == null) previewFade = preview.gameObject.AddComponent<CanvasGroup>();
        }
        hint = FindDeep(transform, "Hint")?.GetComponent<TMP_Text>();

        if (fullscreen != null)
        {
            fullscreen.onValueChanged.AddListener(DisplaySettings.SetFullscreen);
            AskOnPress(fullscreen);
        }
        if (damage != null) damage.onValueChanged.AddListener(DisplaySettings.SetShowDamage);
        if (creatures != null) creatures.onValueChanged.AddListener(DisplaySettings.SetDamageOnlyCreatures);
        Button reset = FindDeep(transform, "ResetDisplayButton")?.GetComponent<Button>();
        if (reset != null) reset.onClick.AddListener(ResetAll);
    }

    void OnEnable()
    {
        DisplaySettings.Changed += Refresh;
        Refresh();
        if (hint != null) hint.text = NormalHint;
    }

    // The page closed: the choices are written to the computer.
    void OnDisable()
    {
        DisplaySettings.Changed -= Refresh;
        DisplaySettings.Save();
    }

#if !UNITY_EDITOR
    // The screen can change by itself (Alt + Enter, F11, the browser's Esc, or a refused request): the tick follows it.
    // (Not in the editor: its Game view never goes fullscreen, so the tick would always jump back.)
    void Update()
    {
        if (fullscreen != null && fullscreen.isOn != Screen.fullScreen) fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
    }
#endif

    // ===== My methods =====

    // Shows the saved choices, and locks the child setting while its parent is off.
    void Refresh()
    {
        bool showDamage = DisplaySettings.ShowDamage;
#if !UNITY_EDITOR
        if (fullscreen != null) fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
#endif
        if (damage != null) damage.SetIsOnWithoutNotify(showDamage);
        if (creatures != null)
        {
            creatures.SetIsOnWithoutNotify(DisplaySettings.DamageOnlyCreatures);
            creatures.interactable = showDamage;   // locked: it can't be ticked or unticked
        }
        if (creaturesFade != null)
        {
            creaturesFade.alpha = showDamage ? 1f : lockedAlpha;
            creaturesFade.interactable = showDamage;
        }
        if (lockNote != null) lockNote.SetActive(!showDamage);   // why it's locked
        if (note != null) note.SetActive(showDamage);
        if (previewFade != null) previewFade.alpha = showDamage ? 1f : lockedAlpha;
    }

    void ResetAll()
    {
        DisplaySettings.ResetToDefaults();
        if (hint != null) hint.text = "Damage numbers back on, for every hit.";
    }

    // The web build: the browser only switches to fullscreen while it handles the player's click or key, and Unity
    // passes the request on with the next mouse or key event. A tick box changes when the mouse button comes UP, after
    // that click's events, so it took a second click. So the fullscreen tick asks already when the button goes DOWN:
    // the browser then switches when it comes up, in the same click. (The tick's own change asks for the same again:
    // nothing more happens. Elsewhere it simply switches a moment earlier.)
    static void AskOnPress(Toggle toggle)
    {
        EventTrigger trigger = toggle.GetComponent<EventTrigger>();
        if (trigger == null) trigger = toggle.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry press = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        press.callback.AddListener(data =>
        {
            PointerEventData pointer = data as PointerEventData;
            if (toggle.IsInteractable() && (pointer == null || pointer.button == PointerEventData.InputButton.Left))
                DisplaySettings.SetFullscreen(!toggle.isOn);
        });
        trigger.triggers.Add(press);
    }

    Toggle ToggleIn(string rowName)
    {
        Transform row = FindDeep(transform, rowName);
        return row != null ? row.GetComponentInChildren<Toggle>(true) : null;
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
