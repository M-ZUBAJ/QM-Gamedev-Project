/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: PauseCanvas (Tools > UI > Build Pause Menu makes Assets/Prefabs/UI/Pause/PauseCanvas.prefab and puts it in the game
        scenes). It finds by NAME: "Pause" (everything shown while paused, switched off otherwise), "ResumeButton"
        and "MainMenuButton".
Unity Editor: Main Menu Scene = where MAIN MENU goes. The key is KeyBindings' Unlock Cursor (Esc; "Pause menu" on the
              Key Bindings page). Whether time stops while it's open is GameRules > Pause Game (on; the New Game page's
              "Pause menu (Esc) pauses the game"), read each time it opens.
              Testing: Play GameScene, press Esc: the game stops and the menu opens. Change a key or the mouse
              sensitivity, then Esc or RESUME: back to the game with the new settings.
Purpose: The pause menu, opened with Esc while playing: KEY BINDINGS (the same list as the title screen's Settings) and
         CONTROLS (Mouse sensitivity 1 - 10, Invert mouse), RESUME and MAIN MENU. While it's open the game stops
         (Time.timeScale 0, while GameRules > Pause Game is on: the default), the mouse is free and the game's own keys
         don't work (MenuState).
         It doesn't open on top of another menu (inventory, crafting, build menu, a chest, the death page...), nor in
         the frame Esc closes one of them. Esc closes it again, except while a key box is waiting for a key (Esc then
         only cancels that). In the web build the browser itself frees the mouse on Esc: losing the mouse while playing
         opens the menu too.
Requirement: PauseMenu.cs x-> MenuState.cs, GameRules.cs, KeyBindings.cs, KeyBindingMenu.cs, MouseSettings.cs, ScreenFader.cs, UITween.cs, Canvas (Unity)
             PauseMenu.cs <-x PauseMenuBuilder.cs
             PauseMenu.cs <-o CameraController.cs
*/

using UnityEngine;
using UnityEngine.EventSystems;   // EventSystem: which UI object is selected
using UnityEngine.UI;             // Button

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class PauseMenu : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The scene MAIN MENU goes back to: the title screen (it must be in File > Build Settings).")]
    [SerializeField] string mainMenuScene = "MainTitleScene";

    const string ContentName = "Pause";
    const string ResumeName = "ResumeButton";
    const string MainMenuName = "MainMenuButton";

    static PauseMenu current;

    GameObject content;
    CanvasGroup fade;
    Button resumeButton, mainMenuButton;
    bool open;
    bool leaving;               // MAIN MENU was clicked: the title screen is loading
    bool timeStopped;           // this opening stopped time (GameRules > Pause Game was on when it opened)
    float timeScaleBefore = 1f;
    bool menuOpenLastFrame;     // another menu was open last frame (Esc may have just closed it)
    bool listeningLastFrame;    // a key box was waiting for a key last frame (Esc may have just cancelled it)
#if UNITY_WEBGL && !UNITY_EDITOR
    bool lockedLastFrame;       // the mouse was locked (aiming) last frame (the web build: see Update)
#endif

    // True while the scene has a pause menu: Esc opens it (CameraController then leaves Esc alone).
    public static bool Active => current != null && current.isActiveAndEnabled;

    // True while the pause menu is open.
    public static bool IsOpen => current != null && current.open;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        Transform part = FindDeep(transform, ContentName);
        content = part != null ? part.gameObject : null;
        fade = content != null ? content.GetComponent<CanvasGroup>() : null;
        resumeButton = FindButton(ResumeName);
        mainMenuButton = FindButton(MainMenuName);
        if (resumeButton != null) resumeButton.onClick.AddListener(() => Close(true));
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (content == null) Debug.LogWarning("PauseMenu: no child named \"" + ContentName + "\". Run Tools > UI > Build Pause Menu.", this);
        else content.SetActive(false);
    }

    void OnEnable()
    {
        MenuState.CloseAllRequested += CloseForOthers;   // e.g. the player died, or won
    }

    void OnDisable()
    {
        MenuState.CloseAllRequested -= CloseForOthers;
        if (open) Close(false);
    }

    void OnDestroy()
    {
        if (current == this) current = null;   // static values outlive the scene, so clear them
        if (open && timeStopped) Time.timeScale = 1f;
        MenuState.SetOpen(this, false);
        MenuState.SetKeysCaptured(this, false);
    }

    void Update()
    {
        if (content == null || leaving) return;
        bool pausePressed = Input.GetKeyDown(KeyBindings.UnlockCursor);

        if (open)
        {
            // Esc while a key box waits (or just stopped waiting) only cancels that key, it doesn't close the menu.
            if (pausePressed && !KeyBindingMenu.IsListening && !listeningLastFrame) Close(true);
            return;
        }

        if (pausePressed && CanOpen()) Open();
#if UNITY_WEBGL && !UNITY_EDITOR
        // The browser frees the mouse itself on Esc (the game may never see the key): losing the mouse while playing
        // opens the menu too.
        else if (lockedLastFrame && Cursor.lockState != CursorLockMode.Locked && CanOpen()) Open();
#endif
    }

    void LateUpdate()
    {
        menuOpenLastFrame = MenuState.AnyOpen;
        listeningLastFrame = KeyBindingMenu.IsListening;
#if UNITY_WEBGL && !UNITY_EDITOR
        lockedLastFrame = Cursor.lockState == CursorLockMode.Locked;
#endif
    }

    // ===== My methods =====

    // Not on top of another menu, and not in the frame Esc closed one (the inventory, a chest's panel...).
    bool CanOpen() => !MenuState.AnyOpen && !menuOpenLastFrame;

    void Open()
    {
        open = true;
        content.SetActive(true);
        MenuState.SetOpen(this, true);          // the camera, building, levitating... pause
        MenuState.SetKeysCaptured(this, true);  // moving, jumping, eating, the hotbar... stop
        Cursor.lockState = CursorLockMode.None; // the mouse is free for the menu
        Cursor.visible = true;
        timeStopped = GameRules.PauseGame;      // the New Game page's "Pause menu (Esc) pauses the game" (on by default)
        if (timeStopped)
        {
            timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;                // everything that moves with time stops
        }
        SetButtons(true);

        // A quick fade and grow in (menu time: it plays while the game is stopped).
        if (fade != null)
        {
            fade.alpha = 0f;
            UITween.Fade(this, fade, 1f, 0.15f);
        }
        content.transform.localScale = Vector3.one * 0.97f;
        UITween.Scale(this, content.transform, Vector3.one, 0.22f);
    }

    // Closes the menu and lets the game go on. lockCursor = back to aiming (not when the death or victory page takes over).
    void Close(bool lockCursor)
    {
        if (!open) return;
        open = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);   // Space / Enter can't press a button later
        if (content != null) content.SetActive(false);   // the pages close: the mouse settings are written to the computer
        MenuState.SetOpen(this, false);
        MenuState.SetKeysCaptured(this, false);
        if (timeStopped) Time.timeScale = timeScaleBefore;
        timeStopped = false;
        MouseSettings.Save();
        if (!lockCursor) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Another menu asked every menu to close (MenuState.CloseAll: the player died or won): it decides the mouse.
    void CloseForOthers()
    {
        Close(false);
    }

    // Back to the title screen: time runs again, the screen fades to black and the title scene loads.
    void GoToMainMenu()
    {
        if (!open || leaving) return;
        leaving = true;
        SetButtons(false);
        MouseSettings.Save();
        Time.timeScale = 1f;   // the title screen must not start frozen
        ScreenFader.LoadScene(mainMenuScene, "", "");
    }

    void SetButtons(bool on)
    {
        if (resumeButton != null) resumeButton.interactable = on;
        if (mainMenuButton != null) mainMenuButton.interactable = on;
    }

    Button FindButton(string childName)
    {
        Transform child = FindDeep(transform, childName);
        return child != null ? child.GetComponent<Button>() : null;
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
