/*
First Date: 7/11/2026
Date: 13/11/2026
Author: Tony
Use by: Put this on the title screen's menu canvas (Tools > UI > Build Main Title Scene makes TitleCanvas with it).
Unity Editor: Nothing to connect: it finds its pages and buttons by NAME (TitlePage, NewGamePage, SettingsPage, Title,
              Subtitle, Buttons, Footer, Version, PlayButton, SettingsButton, QuitButton, BackButton, StartButton...).
              Play Intro: Always / First Time Only (once per game session) / Never (handy while testing).
              Animation Speed: 1 = normal, 2 = everything twice as fast (testing), 0.5 = slow motion.
              Sounds: drag sound files in; empty slots are silent (buttons with their own sound keep it).
              Testing: open MainTitleScene and press Play. Click (or press any key) during the opening to skip it.
Purpose: The title screen:
           Opening:    fades in from black, the title letters fall in and shine, the line under it types itself,
                       the buttons rise in one by one, then the version and credits fade in.
           PLAY:       slides to the New Game page. START GAME there: a white flash, a shockwave ring, a burst of
                       sparks, the menu zooms away, then "DAY 1" and the game loads (ScreenFader) with the choices
                       made (NewGamePanel -> GameSetup).
           SETTINGS:   slides to the Settings page (key bindings, and tabs still to come).
           BACK / Esc: slides back. QUIT: the title shrinks away and the screen fades to black (hidden on WebGL,
                       where a web page can't close itself).
         The mouse is free here, and time runs normally (also when coming back from a paused game).
Requirement: TitleMenu.cs x-> UITween.cs, UISparks.cs, UISound.cs, ScreenFader.cs, TitleTextFX.cs, CascadeIn.cs, ButtonFX.cs, NewGamePanel.cs, GameSetup.cs
             TitleMenu.cs o-> KeyBindingMenu.cs
             TitleMenu.cs <-x TitleSceneBuilder.cs
*/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TitleMenu : MonoBehaviour
{
    public enum IntroMode
    {
        Always,          // every time the title screen opens
        FirstTimeOnly,   // only the first time in a game session
        Never,           // straight to the buttons (handy while testing)
    }

    // ===== Fields =====
    [Header("Opening")]
    [Tooltip("When the opening plays: Always, only the first time in a session, or Never.")]
    [SerializeField] IntroMode playIntro = IntroMode.Always;

    [Header("Animations")]
    [Range(0.25f, 4f)]
    [Tooltip("Speed of every menu animation: 1 = normal, 2 = twice as fast (testing), 0.5 = slow motion.")]
    [SerializeField] float animationSpeed = 1f;

    [Tooltip("Sparks fly out of clicked buttons.")]
    [SerializeField] bool sparks = true;

    [Header("Game")]
    [Tooltip("The scene START GAME loads (its file name).")]
    [SerializeField] string gameScene = "GameScene";

    [Header("Sounds (optional)")]
    [Tooltip("Mouse over a button.")]
    [SerializeField] AudioClip hoverSound;
    [Tooltip("A button is clicked.")]
    [SerializeField] AudioClip clickSound;
    [Tooltip("The page changes (a whoosh).")]
    [SerializeField] AudioClip pageSound;
    [Tooltip("START GAME (something big).")]
    [SerializeField] AudioClip startSound;

    const float SlideDistance = 260f;   // pixels a page slides out / in when the page changes
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

    static bool introSeen;   // First Time Only: the opening already played this session

    CanvasGroup pagesGroup;   // everything on the menu (zooms away on START GAME)
    CanvasGroup titlePage, newGamePage, settingsPage, currentPage;
    TitleTextFX title;
    TMP_Text subtitle;
    CascadeIn buttons;
    CanvasGroup footer;
    Image introBlack;
    RectTransform startButton;
    Image flash, shockwave;
    NewGamePanel newGame;
    bool introPlaying, busy;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        UITween.Speed = animationSpeed;
        UISparks.Enabled = sparks;

        pagesGroup = Find<CanvasGroup>("Pages");
        titlePage = Find<CanvasGroup>("TitlePage");
        newGamePage = Find<CanvasGroup>("NewGamePage");
        settingsPage = Find<CanvasGroup>("SettingsPage");
        title = Find<TitleTextFX>("Title");
        subtitle = Find<TMP_Text>("Subtitle");
        buttons = Find<CascadeIn>("Buttons");
        footer = Find<CanvasGroup>("Footer");
        introBlack = Find<Image>("IntroBlack");
        startButton = FindDeep(transform, "StartButton") as RectTransform;
        flash = startButton != null ? FindDeep(startButton, "Flash")?.GetComponent<Image>() : null;
        shockwave = Find<Image>("Shockwave");
        newGame = GetComponentInChildren<NewGamePanel>(true);

        Hook(FindDeep(transform, "PlayButton"), () => ShowPage(newGamePage, true));
        Hook(FindDeep(transform, "SettingsButton"), () => ShowPage(settingsPage, true));
        Hook(FindDeep(transform, "QuitButton"), () => { if (!busy) StartCoroutine(QuitGame()); });
        Hook(startButton, () => { if (!busy) StartCoroutine(StartGame()); });
        if (newGamePage != null) Hook(FindDeep(newGamePage.transform, "BackButton"), () => ShowPage(titlePage, false));
        if (settingsPage != null) Hook(FindDeep(settingsPage.transform, "BackButton"), () => ShowPage(titlePage, false));

        foreach (ButtonFX fx in GetComponentsInChildren<ButtonFX>(true)) fx.UseSoundsIfEmpty(hoverSound, clickSound);

        // The version shows what's typed in Footer > Version (e.g. "v1.1"), so changing it there is enough. Only an
        // empty text is filled in from Project Settings > Player > Version.
        TMP_Text version = Find<TMP_Text>("Version");
        if (version != null && string.IsNullOrWhiteSpace(version.text)) version.text = "v" + Application.version;

#if UNITY_WEBGL && !UNITY_EDITOR
        Transform quit = FindDeep(transform, "QuitButton");
        if (quit != null) quit.gameObject.SetActive(false);   // a web page can't close itself
#endif
    }

    void Start()
    {
        Time.timeScale = 1f;                      // never start frozen (e.g. after a paused game)
        Cursor.lockState = CursorLockMode.None;   // the menu needs the mouse
        Cursor.visible = true;

        // Only the title page shows at first.
        if (newGamePage != null) newGamePage.gameObject.SetActive(false);
        if (settingsPage != null) settingsPage.gameObject.SetActive(false);
        currentPage = titlePage;
        WriteSubtitle();

        bool play = playIntro == IntroMode.Always || (playIntro == IntroMode.FirstTimeOnly && !introSeen);
        if (play) StartCoroutine(Intro());
        else if (introBlack != null) introBlack.gameObject.SetActive(false);
    }

    void Update()
    {
        if (introPlaying && Input.anyKeyDown) { SkipIntro(); return; }

        // Esc goes back to the title page (unless the key menu is waiting for a key: there Esc cancels).
        if (!busy && currentPage != titlePage && Input.GetKeyDown(KeyCode.Escape) && !KeyBindingMenu.IsListening)
            ShowPage(titlePage, false);
    }

    void OnValidate()
    {
        if (Application.isPlaying) { UITween.Speed = animationSpeed; UISparks.Enabled = sparks; }   // change while playing
    }

    // Static values outlive Play sessions when domain reload is off: start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        introSeen = false;
        UITween.Speed = 1f;
    }

    // ===== My methods =====

    // ----- Opening -----

    IEnumerator Intro()
    {
        introPlaying = true;
        if (introBlack != null)
        {
            // It fades to see-through but stays on until the opening ends: it keeps blocking clicks, so the click
            // that skips the opening can't also press a (still invisible) button.
            introBlack.gameObject.SetActive(true);
            introBlack.color = Color.black;
            UITween.Run(this, 0.8f, UITween.Ease.InOutSine, t => introBlack.color = new Color(0f, 0f, 0f, 1f - t));
        }
        if (title != null) title.HideLetters();
        if (subtitle != null) subtitle.maxVisibleCharacters = 0;
        if (buttons != null) buttons.Hide();
        if (footer != null) footer.alpha = 0f;

        yield return UITween.Wait(0.4f);
        if (title != null) title.PlayIntro();      // the letters fall in

        yield return UITween.Wait(0.95f);
        if (title != null) title.Shine();          // a light sweeps over them

        yield return UITween.Wait(0.1f);
        if (subtitle != null)                      // the line under the title types itself
        {
            int letters = subtitle.text.Length;
            UITween.Run(this, 0.5f, UITween.Ease.Linear, t => subtitle.maxVisibleCharacters = Mathf.RoundToInt(letters * t));
        }

        yield return UITween.Wait(0.2f);
        if (buttons != null) buttons.Play();       // the buttons rise in one by one

        yield return UITween.Wait(0.6f);
        if (footer != null) UITween.Fade(this, footer, 1f, 0.4f);

        yield return UITween.Wait(0.4f);
        introPlaying = false;
        introSeen = true;
        if (introBlack != null) introBlack.gameObject.SetActive(false);
    }

    // Jumps to the end of the opening (a click or any key during it).
    void SkipIntro()
    {
        StopAllCoroutines();
        introPlaying = false;
        introSeen = true;
        if (introBlack != null) introBlack.gameObject.SetActive(false);
        if (title != null) title.FinishIntro();
        if (subtitle != null) subtitle.maxVisibleCharacters = 99999;
        if (buttons != null) buttons.Finish();
        if (footer != null) footer.alpha = 1f;
    }

    // "Survive 5 nights. Win on the morning of day 6." from the game's goal.
    void WriteSubtitle()
    {
        if (subtitle == null || newGame == null) return;
        int n = newGame.DaysToWin;
        subtitle.text = "Survive " + n + (n == 1 ? " night" : " nights") + ". Win on the morning of day " + (n + 1) + ".";
    }

    // ----- Pages -----

    // Slides the open page out and "page" in (forward = to the left, like turning a page; back = to the right).
    void ShowPage(CanvasGroup page, bool forward)
    {
        if (busy || introPlaying || page == null || page == currentPage) return;
        StartCoroutine(SwitchPage(currentPage, page, forward ? 1 : -1));
    }

    IEnumerator SwitchPage(CanvasGroup from, CanvasGroup to, int direction)
    {
        busy = true;
        UISound.Play(pageSound);
        if (to == titlePage) WriteSubtitle();

        RectTransform fromRect = (RectTransform)from.transform;
        RectTransform toRect = (RectTransform)to.transform;
        from.interactable = false;
        UITween.Run(this, 0.3f, UITween.Ease.InCubic, t =>
        {
            fromRect.anchoredPosition = new Vector2(-direction * SlideDistance * t, 0f);
            from.alpha = 1f - t;
        });

        yield return UITween.Wait(0.12f);
        to.alpha = 0f;
        to.interactable = false;
        toRect.anchoredPosition = new Vector2(direction * SlideDistance, 0f);
        to.gameObject.SetActive(true);   // its rows cascade in by themselves (CascadeIn)

        yield return UITween.Run(this, 0.4f, UITween.Ease.OutCubic, t =>
        {
            toRect.anchoredPosition = new Vector2(direction * SlideDistance * (1f - t), 0f);
            to.alpha = t;
        });

        from.gameObject.SetActive(false);
        fromRect.anchoredPosition = Vector2.zero;
        from.alpha = 1f;
        from.interactable = true;
        to.interactable = true;
        currentPage = to;
        busy = false;
    }

    // ----- START GAME and QUIT -----

    IEnumerator StartGame()
    {
        busy = true;
        if (pagesGroup != null) pagesGroup.blocksRaycasts = false;
        if (newGame != null) newGame.Commit();   // only the changed options
        GameSetup.Choose();
        UISound.Play(startSound);

        // A white flash on the button...
        if (flash != null)
        {
            flash.gameObject.SetActive(true);
            UITween.Run(this, 0.45f, UITween.Ease.OutCubic, t => flash.color = new Color(1f, 1f, 1f, 0.9f * (1f - t)),
                        () => flash.gameObject.SetActive(false));
        }

        // ...a ring of light spreading across the screen from it, and a big burst of sparks.
        if (shockwave != null && startButton != null)
        {
            RectTransform ring = shockwave.rectTransform;
            ring.position = startButton.TransformPoint(startButton.rect.center);
            shockwave.gameObject.SetActive(true);
            UITween.Run(this, 0.9f, UITween.Ease.OutCubic, t =>
            {
                ring.localScale = Vector3.one * Mathf.Lerp(0.2f, 14f, t);
                shockwave.color = new Color(Gold.r, Gold.g, Gold.b, 0.9f * (1f - t));
            }, () => shockwave.gameObject.SetActive(false));
        }
        UISparks.Burst(startButton, Gold, 34);

        // The whole menu zooms towards the player and fades away.
        yield return UITween.Wait(0.2f);
        if (pagesGroup != null)
        {
            RectTransform pages = (RectTransform)pagesGroup.transform;
            yield return UITween.Run(this, 0.55f, UITween.Ease.InCubic, t =>
            {
                pages.localScale = Vector3.one * (1f + 0.18f * t);
                pagesGroup.alpha = 1f - t;
            });
        }

        int n = newGame != null ? newGame.DaysToWin : 5;
        ScreenFader.LoadScene(gameScene, "DAY 1", "Survive " + n + (n == 1 ? " night" : " nights"));
    }

    IEnumerator QuitGame()
    {
        busy = true;
        if (pagesGroup != null) pagesGroup.blocksRaycasts = false;

        if (title != null)
        {
            Transform t = title.transform;
            UITween.Scale(this, t, Vector3.one * 0.6f, 0.6f, UITween.Ease.InCubic);
        }
        if (pagesGroup != null) UITween.Fade(this, pagesGroup, 0f, 0.6f, UITween.Ease.InCubic);

        yield return UITween.Wait(0.25f);
        ScreenFader.FadeOutAndQuit();
    }

    // ----- Finding things -----

    void Hook(Transform target, UnityEngine.Events.UnityAction action)
    {
        Button b = target != null ? target.GetComponent<Button>() : null;
        if (b != null) b.onClick.AddListener(action);
    }

    T Find<T>(string childName) where T : Component
    {
        Transform t = FindDeep(transform, childName);
        return t != null ? t.GetComponent<T>() : null;
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
