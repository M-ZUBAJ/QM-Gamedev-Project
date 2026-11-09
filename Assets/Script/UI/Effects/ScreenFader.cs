/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up. ScreenFader.LoadScene(...) / FadeOutAndQuit() make it by themselves (TitleMenu uses them).
Unity Editor: Nothing to set. The scene to load should be in File > Build Settings (Tools > UI > Build Main Title Scene
              adds MainTitleScene and GameScene there); while testing in the editor it also loads scenes that aren't.
Purpose: The cinematic change from the menu to the game:
           1. the screen fades to black,
           2. a big title card stamps in (e.g. "DAY 1", "Survive 5 nights"),
           3. the scene loads behind the black screen (a loading bar shows if it takes a while),
           4. the new scene fades in from black.
         It keeps itself alive through the scene change, so the new scene needs nothing for the fade-in.
         It also blocks clicks while it plays, so nothing is pressed twice.
Requirement: ScreenFader.cs x-> UITween.cs
             ScreenFader.cs <-x TitleMenu.cs
*/

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScreenFader : MonoBehaviour
{
    // ===== Fields =====
    const int SortOrder = 1000;        // drawn over every other canvas
    const float ToBlackTime = 0.45f;   // seconds to fade to black
    const float FromBlackTime = 0.9f;  // seconds for the new scene to fade in
    const float CardTime = 1.6f;       // the title card shows at least this long (real seconds)
    static readonly Color CardColor = new Color(1f, 0.85f, 0.35f);   // gold, like the menu

    static ScreenFader instance;   // the one playing right now (null = none)

    Image black;
    CanvasGroup card;
    TMP_Text cardTitle, cardSubtitle;
    CanvasGroup bar;
    Image barFill;

    // True while a fade is playing (e.g. so the menu ignores clicks).
    public static bool IsBusy => instance != null;

    // ===== Unity messages (Unity calls these) =====

    void OnDestroy()
    {
        if (instance == this) instance = null;   // static values outlive the scene, so clear them
    }

    // ===== My methods =====

    // Fades to black, shows the title card, loads the scene, then fades it in.
    public static void LoadScene(string sceneName, string title, string subtitle)
    {
        if (instance != null) return;   // already changing scene
        Create().StartCoroutine(instance.LoadRoutine(sceneName, title, subtitle));
    }

    // Fades to black, then closes the game (in the editor: stops playing).
    public static void FadeOutAndQuit()
    {
        if (instance != null) return;
        Create().StartCoroutine(instance.QuitRoutine());
    }

    IEnumerator LoadRoutine(string sceneName, string title, string subtitle)
    {
        yield return FadeBlack(0f, 1f, ToBlackTime);

        // The title card: the big text stamps in from bigger, then the small line fades in under it.
        cardTitle.text = title;
        cardSubtitle.text = subtitle;
        card.alpha = 1f;
        cardTitle.alpha = 0f;
        cardSubtitle.alpha = 0f;
        UITween.Run(this, 0.45f, UITween.Ease.OutBack, t =>
        {
            cardTitle.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.8f, 1f, t);
            cardTitle.alpha = Mathf.Clamp01(t * 2f);
        });
        UITween.Run(this, 0.5f, UITween.Ease.OutCubic, t => cardSubtitle.alpha = t, null, 0.4f);

        AsyncOperation load = StartLoading(sceneName);
        if (load == null)   // couldn't load: show the menu again
        {
            yield return UITween.Wait(0.8f);
            card.alpha = 0f;
            yield return FadeBlack(1f, 0f, ToBlackTime);
            Destroy(gameObject);
            yield break;
        }

        // Keep the card up for a moment, and until the scene is ready (Unity stops at 0.9 until it's allowed in).
        load.allowSceneActivation = false;
        float shown = 0f;
        while (shown < CardTime || load.progress < 0.9f)
        {
            shown += Time.unscaledDeltaTime;
            if (shown > CardTime)   // taking a while: show how far it got
            {
                bar.alpha = Mathf.MoveTowards(bar.alpha, 1f, Time.unscaledDeltaTime * 3f);
                barFill.fillAmount = load.progress / 0.9f;
            }
            yield return null;
        }

        barFill.fillAmount = 1f;
        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;
        yield return null;   // the new scene's first frame has run

        // The new scene fades in.
        UITween.Run(this, 0.35f, UITween.Ease.OutCubic, t => { card.alpha = 1f - t; bar.alpha = Mathf.Min(bar.alpha, 1f - t); });
        yield return FadeBlack(1f, 0f, FromBlackTime);
        Destroy(gameObject);
    }

    IEnumerator QuitRoutine()
    {
        yield return FadeBlack(0f, 1f, 0.6f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FadeBlack(float from, float to, float seconds)
    {
        black.raycastTarget = true;   // no clicks get through while fading
        yield return UITween.Run(this, seconds, UITween.Ease.InOutSine,
                                 t => black.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t)));
        black.raycastTarget = to > 0f;
    }

    // Starts loading the scene in the background. A scene that isn't in Build Settings still loads while testing
    // in the editor (in a built game every scene must be in Build Settings).
    static AsyncOperation StartLoading(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName)) return SceneManager.LoadSceneAsync(sceneName);

#if UNITY_EDITOR
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets(sceneName + " t:Scene"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != sceneName) continue;
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                path, new LoadSceneParameters(LoadSceneMode.Single));
        }
#endif
        Debug.LogError("ScreenFader: can't find a scene called \"" + sceneName + "\". Add it to File > Build Settings.");
        return null;
    }

    // ----- Making the fader (it builds its own canvas, so nothing needs setting up) -----

    static ScreenFader Create()
    {
        GameObject go = new GameObject("ScreenFader", typeof(RectTransform));
        DontDestroyOnLoad(go);   // lives through the scene change, for the fade-in

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();   // so the black screen can block clicks

        instance = go.AddComponent<ScreenFader>();
        instance.Build();
        return instance;
    }

    void Build()
    {
        black = MakeImage("Black", transform, new Color(0f, 0f, 0f, 0f));
        Stretch(black.rectTransform);

        GameObject cardGo = new GameObject("Card", typeof(RectTransform));
        cardGo.transform.SetParent(transform, false);
        Stretch((RectTransform)cardGo.transform);
        card = cardGo.AddComponent<CanvasGroup>();
        card.alpha = 0f;
        card.blocksRaycasts = false;

        cardTitle = MakeText("Title", cardGo.transform, 170f, CardColor, new Vector2(0f, 40f), new Vector2(1600f, 220f));
        cardTitle.fontStyle = FontStyles.Bold;
        cardTitle.characterSpacing = 12f;
        cardSubtitle = MakeText("Subtitle", cardGo.transform, 42f, new Color(0.85f, 0.85f, 0.9f),
                                new Vector2(0f, -90f), new Vector2(1400f, 70f));

        // The loading bar: a thin dark line low on the screen that fills with gold.
        GameObject barGo = new GameObject("LoadingBar", typeof(RectTransform));
        barGo.transform.SetParent(cardGo.transform, false);
        RectTransform barRect = (RectTransform)barGo.transform;
        barRect.anchoredPosition = new Vector2(0f, -260f);
        barRect.sizeDelta = new Vector2(520f, 8f);
        bar = barGo.AddComponent<CanvasGroup>();
        bar.alpha = 0f;
        Image barBack = MakeImage("Back", barGo.transform, new Color(1f, 1f, 1f, 0.15f));
        Stretch(barBack.rectTransform);
        barFill = MakeImage("Fill", barGo.transform, CardColor);
        Stretch(barFill.rectTransform);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillAmount = 0f;
    }

    static Image MakeImage(string objectName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text MakeText(string objectName, Transform parent, float size, Color color, Vector2 position, Vector2 box)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = box;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
