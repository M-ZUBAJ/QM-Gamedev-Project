/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Menu Tools > UI > Build Main Title Scene. It asks to save the open scene first, and asks before replacing
              an existing MainTitleScene. It makes, everything connected:
                Assets/Scenes/MainTitleScene.unity (first in File > Build Settings, GameScene second)
                Assets/Prefabs/UI/Title/TitleCanvas.prefab (the menu)
                Assets/Config/NewGame/NewGameDefaults.asset (the game as it is now, read from GameScene)
                Assets/UI/Title/*.png + TitleText.mat (the soft shapes and the glowing title; existing files are kept)
              It builds a brand new scene, so Undo doesn't apply: run it again to rebuild.
              To change texts, sizes or colors for every future build, edit Fields below.
              Needs TMP Essentials (it tells you if they are missing). Guide: Docs/Main Title Setup.md
Purpose: Builds the whole title screen in one click:
           Main Camera, EventSystem, KeyBindings (with GameScene's default keys)
           BackgroundCanvas (TitleBackground: the cycling sky and stars) + Embers (a Particle System rising in front)
           TitleCanvas (TitleMenu) > Vignette, Pages > TitlePage / NewGamePage (NewGamePanel) / SettingsPage,
                                     FX > Shockwave, IntroBlack
Requirement: TitleSceneBuilder.cs x-> UIBuildHelper.cs, MenuOrder.cs, NewGameDefaultsSync.cs, NewGameDefaults.cs, UIFXSprites.cs, UITween.cs, TitleMenu.cs, TitleBackground.cs, TitleTextFX.cs, ButtonFX.cs, CascadeIn.cs, PulseFX.cs, TabFX.cs, TabGroup.cs, ChoiceRow.cs, StepperRow.cs, TogglePop.cs, DropdownUnfold.cs, NewGamePanel.cs, KeyBindingMenu.cs, IncomingMessage.cs, KeyBindings.cs
*/

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UIBuildHelper;   // NewUI, AddText, NewCanvas... can be called directly

public static class TitleSceneBuilder
{
    // ===== Fields =====
    const string ToolName = "Build Main Title Scene";
    const string ScenePath = "Assets/Scenes/MainTitleScene.unity";
    static string PrefabPath => FindPrefabPath("Assets/Prefabs/UI/Title/TitleCanvas.prefab");
    const string ArtFolder = "Assets/UI/Title";

    const string GameTitle = "UNKNOWN SURVIVAL";
    const string Credits = "Tony  ·  Turki  ·  Marko  ·  Sanyam";

    // The look: dark see-through boxes, white text, gold for what's picked (the same gold as the coins).
    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    static readonly Color DarkText = new Color(0.12f, 0.09f, 0.04f);
    static readonly Color BoxColor = new Color(0.07f, 0.08f, 0.11f, 0.9f);
    static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color KeyColor = new Color(0.18f, 0.2f, 0.26f, 1f);
    static readonly Color SoftWhite = new Color(0.86f, 0.87f, 0.92f);
    static readonly Color Dim = new Color(0.62f, 0.64f, 0.7f);
    static readonly Color[] DifficultyColors =
    {
        new Color(0.36f, 0.85f, 0.46f),   // Peaceful: green
        new Color(0.38f, 0.66f, 1f),      // Easy: blue
        new Color(1f, 0.8f, 0.25f),       // Normal: gold
        new Color(1f, 0.36f, 0.3f),       // Hard: red
    };

    // Sizes (pixels on a 1920 x 1080 screen).
    const float RowLabelX = 40f;       // where row names start (the changed-dot sits left of them)
    const float ControlX = 380f;       // where the row's buttons / dropdowns start
    const float RowHeight = 50f;

    // The art for this build (made or loaded in MakeArt).
    static Sprite gradientSprite, vignetteSprite, glowSprite, ringSprite;
    static Texture2D stripesTexture;
    static Material titleMaterial;

    // Unity's built-in UI pictures.
    static Sprite Builtin(string file) => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + file);

    // ===== Menu (Unity calls this) =====

    [MenuItem("Tools/UI/Build Main Title Scene", false, MenuOrder.BuildTitleScene)]
    static void Build()
    {
        if (!CheckTMP(ToolName)) return;
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(ToolName,
                "MainTitleScene already exists. Build a new one and replace it (and TitleCanvas.prefab)?",
                "Replace", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;   // the open scene closes next

        ReadDefaults(out string keyBindingsJson);   // while GameScene can still be opened

        // Changing scene unloads every asset nothing holds on to, and a C# variable still pointing at one then
        // counts as empty (that once saved an empty Defaults link). So the art is made, and the defaults loaded,
        // AFTER the scene change.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeArt();
        NewGameDefaults defaults = AssetDatabase.LoadAssetAtPath<NewGameDefaults>(NewGameDefaultsSync.DefaultsPath);
        Camera cam = MakeCamera();
        MakeEventSystemIfMissing(ToolName);
        MakeKeyBindings(keyBindingsJson);
        MakeBackground(cam);
        MakeEmbers(cam);
        GameObject canvas = MakeMenu(defaults);

        MakeFolders(FolderOf(PrefabPath));
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, PrefabPath, InteractionMode.AutomatedAction);
        MakeFolders(FolderOf(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        PutInBuildSettings();

        Selection.activeGameObject = canvas;
        Debug.Log("Title screen built: " + ScenePath + " (first in Build Settings). Press Play to see it. " +
                  "New Game defaults = GameScene as it is now (" + NewGameDefaultsSync.DefaultsPath + ").");
    }

    // ===== My methods =====

    // ----- Files: art, defaults, build settings -----

    static void MakeArt()
    {
        MakeFolders(ArtFolder);
        gradientSprite = SaveShape("SkyGradient", UIFXSprites.Shape.SkyGradient, false);
        vignetteSprite = SaveShape("Vignette", UIFXSprites.Shape.Vignette, false);
        glowSprite = SaveShape("Glow", UIFXSprites.Shape.SoftDot, false);
        ringSprite = SaveShape("Ring", UIFXSprites.Shape.Ring, false);
        Sprite stripes = SaveShape("Stripes", UIFXSprites.Shape.Stripes, true);
        stripesTexture = stripes != null ? stripes.texture : null;
        titleMaterial = MakeTitleMaterial();
    }

    // Saves a drawn shape as a PNG sprite (only if the file isn't there yet, so your own art is kept).
    static Sprite SaveShape(string fileName, UIFXSprites.Shape shape, bool repeat)
    {
        string path = ArtFolder + "/" + fileName + ".png";
        if (!File.Exists(path))
        {
            Texture2D texture = UIFXSprites.Draw(shape);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // A copy of the title font's material with a soft dark shadow and a warm gold glow around the letters.
    static Material MakeTitleMaterial()
    {
        string path = ArtFolder + "/TitleText.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null || font.material == null) return null;

        Material m = new Material(font.material);
        m.EnableKeyword("UNDERLAY_ON");                       // the shadow
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.7f));
        m.SetFloat("_UnderlayOffsetX", 0.5f);
        m.SetFloat("_UnderlayOffsetY", -0.5f);
        m.SetFloat("_UnderlayDilate", 0.2f);
        m.SetFloat("_UnderlaySoftness", 0.5f);
        m.EnableKeyword("GLOW_ON");                           // the glow
        m.SetColor("_GlowColor", new Color(1f, 0.68f, 0.2f, 0.6f));
        m.SetFloat("_GlowOffset", 0f);
        m.SetFloat("_GlowInner", 0.05f);
        m.SetFloat("_GlowOuter", 0.35f);
        m.SetFloat("_GlowPower", 0.6f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Makes / updates the New Game defaults asset = GameScene as it is now (GameScene is opened for a moment if it
    // isn't open), and reads GameScene's keys.
    static void ReadDefaults(out string keyBindingsJson)
    {
        keyBindingsJson = null;
        string path = NewGameDefaultsSync.DefaultsPath;
        MakeFolders(FolderOf(path));
        NewGameDefaults defaults = AssetDatabase.LoadAssetAtPath<NewGameDefaults>(path);
        if (defaults == null)
        {
            defaults = ScriptableObject.CreateInstance<NewGameDefaults>();
            AssetDatabase.CreateAsset(defaults, path);
        }

        string gamePath = NewGameDefaultsSync.GameScenePath;
        Scene game = SceneManager.GetSceneByPath(gamePath);
        bool openedHere = false;
        if (!game.isLoaded)
        {
            if (!File.Exists(gamePath))
            {
                Debug.LogWarning(ToolName + ": " + gamePath + " not found, so the New Game defaults are the script's.");
                return;
            }
            game = EditorSceneManager.OpenScene(gamePath, OpenSceneMode.Additive);
            openedHere = true;
        }

        NewGameDefaultsSync.ReadFrom(game, defaults);
        keyBindingsJson = NewGameDefaultsSync.KeyBindingsJson(game);
        if (openedHere) EditorSceneManager.CloseScene(game, true);
        AssetDatabase.SaveAssets();
    }

    // MainTitleScene first (the built game starts there), then GameScene, then any others as they were.
    static void PutInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

        string gamePath = NewGameDefaultsSync.GameScenePath;
        if (!scenes.Exists(s => s.path == gamePath) && File.Exists(gamePath))
            scenes.Insert(1, new EditorBuildSettingsScene(gamePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ----- Scene objects -----

    static Camera MakeCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();   // to hear the menu sounds
        return cam;
    }

    // The same default keys as the game, so RESET ALL on the title screen gives the game's keys.
    static void MakeKeyBindings(string json)
    {
        KeyBindings keys = new GameObject("KeyBindings").AddComponent<KeyBindings>();
        if (!string.IsNullOrEmpty(json)) EditorJsonUtility.FromJsonOverwrite(json, keys);
    }

    // A canvas far behind everything (the embers fly in front of it): the sky gradient and the stars.
    static void MakeBackground(Camera cam)
    {
        GameObject canvasGo = NewUI("BackgroundCanvas", null);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100f;
        canvas.sortingOrder = -10;
        AddScaler(canvasGo);

        Image bottom = AddImage(NewUI("SkyBottom", canvasGo.transform), null, new Color(0.06f, 0.08f, 0.18f));
        Stretch(bottom.gameObject, 0f, 0f, 0f, 0f);
        Image top = AddImage(NewUI("SkyTop", canvasGo.transform), gradientSprite, new Color(0.03f, 0.04f, 0.11f));
        Stretch(top.gameObject, 0f, 0f, 0f, 0f);
        Stretch(NewUI("Stars", canvasGo.transform), 0f, 0f, 0f, 0f);

        TitleBackground background = canvasGo.AddComponent<TitleBackground>();
        Set(background, "starSprite", glowSprite);
    }

    // Glowing embers drifting up from below, between the sky and the menu.
    static void MakeEmbers(Camera cam)
    {
        GameObject go = new GameObject("Embers");
        go.transform.position = cam.transform.position + new Vector3(0f, -5.5f, 12f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;                 // already full when the scene opens
        main.useUnscaledTime = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f), new Color(1f, 0.45f, 0.15f));
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 16f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // emit upward
        shape.scale = new Vector3(26f, 4f, 0.5f);      // wide across the screen

        ParticleSystem.NoiseModule noise = ps.noise;   // drifting, like real embers
        noise.enabled = true;
        noise.strength = 0.45f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.25f;

        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient alpha = new Gradient();
        alpha.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                              new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(alpha);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
    }

    // ----- The menu -----

    static GameObject MakeMenu(NewGameDefaults defaults)
    {
        GameObject canvas = NewCanvas("TitleCanvas", 0);

        Image vignette = AddImage(NewUI("Vignette", canvas.transform), vignetteSprite, Color.black);
        Stretch(vignette.gameObject, 0f, 0f, 0f, 0f);

        GameObject pages = NewUI("Pages", canvas.transform);
        Stretch(pages, 0f, 0f, 0f, 0f);
        pages.AddComponent<CanvasGroup>();
        MakeTitlePage(pages.transform, defaults);
        MakeNewGamePage(pages.transform, defaults);
        MakeSettingsPage(pages.transform);

        GameObject fx = NewUI("FX", canvas.transform);
        Stretch(fx, 0f, 0f, 0f, 0f);
        Image ring = AddImage(NewUI("Shockwave", fx.transform), ringSprite, Gold);
        ring.rectTransform.sizeDelta = new Vector2(220f, 220f);
        ring.gameObject.SetActive(false);

        Image black = AddImage(NewUI("IntroBlack", canvas.transform), null, Color.black);
        Stretch(black.gameObject, 0f, 0f, 0f, 0f);
        black.raycastTarget = true;          // no clicks during the opening (a click skips it instead)
        black.gameObject.SetActive(false);   // TitleMenu shows it when the opening plays

        canvas.AddComponent<TitleMenu>();
        return canvas;
    }

    static void MakeTitlePage(Transform parent, NewGameDefaults d)
    {
        RectTransform page = MakePage("TitlePage", parent);

        TextMeshProUGUI title = MakeText(page, "Title", GameTitle, 132f, Color.white, TextAlignmentOptions.Center);
        Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1760f, 190f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 14f;
        if (titleMaterial != null) title.fontSharedMaterial = titleMaterial;
        title.gameObject.AddComponent<TitleTextFX>();

        int n = d.DaysToWin;
        TextMeshProUGUI subtitle = MakeText(page, "Subtitle",
            "Survive " + n + " nights. Win on the morning of day " + (n + 1) + ".", 36f, SoftWhite, TextAlignmentOptions.Center);
        Place(subtitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(1400f, 60f));

        GameObject column = NewUI("Buttons", page);
        Place(column, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(440f, 340f));
        VerticalLayoutGroup v = column.AddComponent<VerticalLayoutGroup>();
        v.spacing = 24f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        CascadeIn cascade = column.AddComponent<CascadeIn>();
        SetCascade(cascade, new Vector2(0f, -70f), 0.1f, 0.55f, UITween.Ease.OutBack);

        MakeButton(column.transform, "PlayButton", "PLAY", new Vector2(420f, 92f), 42f);
        MakeButton(column.transform, "SettingsButton", "SETTINGS", new Vector2(420f, 92f), 42f);
        MakeButton(column.transform, "QuitButton", "QUIT", new Vector2(420f, 92f), 42f);

        GameObject footer = NewUI("Footer", page);
        RectTransform f = (RectTransform)footer.transform;
        f.anchorMin = new Vector2(0f, 0f);
        f.anchorMax = new Vector2(1f, 0f);
        f.pivot = new Vector2(0.5f, 0f);
        f.anchoredPosition = new Vector2(0f, 24f);
        f.sizeDelta = new Vector2(-80f, 50f);   // 40 pixels in from each side
        footer.AddComponent<CanvasGroup>();
        TextMeshProUGUI version = MakeText(footer.transform, "Version", "v" + Application.version, 26f, Dim, TextAlignmentOptions.Left);
        Stretch(version.gameObject, 0f, 0f, 0f, 0f);
        TextMeshProUGUI credits = MakeText(footer.transform, "Credits", Credits, 26f, Dim, TextAlignmentOptions.Right);
        Stretch(credits.gameObject, 0f, 0f, 0f, 0f);
    }

    static void MakeNewGamePage(Transform parent, NewGameDefaults d)
    {
        RectTransform page = MakePage("NewGamePage", parent);
        MakeHeader(page, "NEW GAME", true);

        GameObject panel = NewUI("Panel", page);
        Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 690f));
        AddBox(panel).color = PanelColor;

        GameObject rows = NewUI("Rows", panel.transform);
        Stretch(rows, 30f, 22f, 30f, 22f);
        VerticalLayoutGroup v = rows.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        SetCascade(rows.AddComponent<CascadeIn>(), new Vector2(50f, 0f), 0.035f, 0.4f, UITween.Ease.OutCubic);

        Section(rows.transform, "GOAL");
        MakeStepperRow(rows.transform, "DaysRow", "Days to win", d.DaysToWin);
        MakeDropdownRow(rows.transform, "WinAtRow", "Win at");
        Section(rows.transform, "WORLD");
        MakeChoiceRow(rows.transform, "DayLengthRow", "Day length", new[] { "Short", "Normal", "Long" }, 150f, null, true);
        MakeDropdownRow(rows.transform, "StartTimeRow", "Start time");
        Section(rows.transform, "DIFFICULTY");
        MakeChoiceRow(rows.transform, "DifficultyRow", null, new[] { "Peaceful", "Easy", "Normal", "Hard" }, 170f,
                      DifficultyColors, false);
        MakeInfoRow(rows.transform, "DifficultyInfo", "");
        Section(rows.transform, "RULES");
        MakeChoiceRow(rows.transform, "DeathRow", "When I die", new[] { "Keep items", "Drop items", "Lose items" }, 190f,
                      null, false);
        MakeToggleRow(rows.transform, "PickupRow", "Pick up dropped items by walking over them");
        MakeToggleRow(rows.transform, "MessagesRow", "Show \"+3 Wood\" messages");

        TextMeshProUGUI summary = MakeText(page, "Summary", "Everything is set to the game as it is now.", 30f, SoftWhite,
                                       TextAlignmentOptions.Center);
        Place(summary.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(1500f, 44f));

        // START GAME: a pulsing gold glow behind it, and a Flash inside it for the click.
        Image glow = AddImage(NewUI("StartGlow", page), glowSprite, new Color(Gold.r, Gold.g, Gold.b, 0.55f));
        Place(glow.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(760f, 230f));
        PulseFX pulse = glow.gameObject.AddComponent<PulseFX>();
        Set(pulse, "speed", 0.8f);
        Set(pulse, "growBy", 0.08f);
        Set(pulse, "lowestAlpha", 0.35f);

        Button start = MakeButton(page, "StartButton", "START GAME", new Vector2(480f, 96f), 44f, 18, false);
        Place(start.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(480f, 96f));
        Image flash = AddBox(NewUI("Flash", start.transform));
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.raycastTarget = false;
        Stretch(flash.gameObject, 0f, 0f, 0f, 0f);
        flash.gameObject.SetActive(false);

        NewGamePanel panelScript = page.gameObject.AddComponent<NewGamePanel>();
        Set(panelScript, "defaults", d);
        page.gameObject.SetActive(false);   // TitleMenu slides it in
    }

    static void MakeSettingsPage(Transform parent)
    {
        RectTransform page = MakePage("SettingsPage", parent);
        MakeHeader(page, "SETTINGS", false);

        // The tab bar: four tab buttons side by side, and the gold underline that glides under the open one.
        GameObject bar = NewUI("Tabs", page);
        Place(bar, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1200f, 64f));
        ((RectTransform)bar.transform).pivot = new Vector2(0.5f, 1f);
        HorizontalLayoutGroup h = bar.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = true;

        string[] tabNames = { "KEY BINDINGS", "CONTROLS", "DISPLAY", "AUDIO" };
        string[] tabObjects = { "KeyBindingsTab", "ControlsTab", "DisplayTab", "AudioTab" };
        Button[] tabButtons = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++) tabButtons[i] = MakeTab(bar.transform, tabObjects[i], tabNames[i]);

        Image underline = AddImage(NewUI("Underline", bar.transform), null, Gold);
        underline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        Place(underline.gameObject, new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(290f, 6f));

        GameObject holder = NewUI("TabPages", page);
        Place(holder, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1200f, 700f));
        GameObject[] tabPages =
        {
            MakeKeyBindingsPage(holder.transform),
            MakeIncomingPage(holder.transform, "ControlsPage", "Mouse sensitivity, invert mouse and more."),
            MakeIncomingPage(holder.transform, "DisplayPage", "Fullscreen, quality and more."),
            MakeIncomingPage(holder.transform, "AudioPage", "Music and sound volume."),
        };

        TabGroup tabs = bar.AddComponent<TabGroup>();
        SerializedObject so = new SerializedObject(tabs);
        SerializedProperty list = so.FindProperty("tabs");
        list.arraySize = tabButtons.Length;
        for (int i = 0; i < tabButtons.Length; i++)
        {
            SerializedProperty tab = list.GetArrayElementAtIndex(i);
            tab.FindPropertyRelative("button").objectReferenceValue = tabButtons[i];
            tab.FindPropertyRelative("page").objectReferenceValue = tabPages[i];
        }
        so.FindProperty("selectedColor").colorValue = new Color(0.86f, 0.58f, 0.16f);   // amber: white text still reads
        so.FindProperty("normalColor").colorValue = BoxColor;
        so.ApplyModifiedPropertiesWithoutUndo();
        bar.AddComponent<TabFX>();

        page.gameObject.SetActive(false);   // TitleMenu slides it in
    }

    static GameObject MakeKeyBindingsPage(Transform parent)
    {
        GameObject page = NewUI("KeyBindingsPage", parent);
        Stretch(page, 0f, 0f, 0f, 0f);

        // The scrolling list.
        GameObject scroll = NewUI("Scroll", page.transform);
        Stretch(scroll, 0f, 0f, 0f, 70f);
        AddBox(scroll).color = PanelColor;
        ScrollRect sr = scroll.AddComponent<ScrollRect>();

        GameObject viewport = NewUI("Viewport", scroll.transform);
        Stretch(viewport, 10f, 10f, 28f, 10f);
        viewport.AddComponent<RectMask2D>();

        GameObject content = NewUI("Content", viewport.transform);
        RectTransform c = (RectTransform)content.transform;
        c.anchorMin = new Vector2(0f, 1f);
        c.anchorMax = new Vector2(1f, 1f);
        c.pivot = new Vector2(0.5f, 1f);
        c.sizeDelta = Vector2.zero;
        VerticalLayoutGroup v = content.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(10, 10, 8, 8);
        v.spacing = 4f;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetCascade(content.AddComponent<CascadeIn>(), new Vector2(50f, 0f), 0.02f, 0.35f, UITween.Ease.OutCubic);

        Scrollbar scrollbar = MakeScrollbar(scroll.transform);
        sr.content = c;
        sr.viewport = (RectTransform)viewport.transform;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 35f;
        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // The templates KeyBindingMenu copies (switched off, outside the list).
        GameObject templates = NewUI("Templates", page.transform);
        templates.SetActive(false);
        MakeHeaderTemplate(templates.transform);
        MakeRowTemplate(templates.transform);
        MakeHotbarTemplate(templates.transform);

        TextMeshProUGUI hint = MakeText(page.transform, "Hint", "Click a key, then press the new one (Esc = cancel).", 24f,
                                    SoftWhite, TextAlignmentOptions.Left);
        Place(hint.gameObject, new Vector2(0f, 0f), new Vector2(10f, 14f), new Vector2(880f, 44f));
        ((RectTransform)hint.transform).pivot = new Vector2(0f, 0f);

        Button reset = MakeButton(page.transform, "ResetAllButton", "RESET ALL", new Vector2(240f, 56f), 24f, 10, false, false);
        Place(reset.gameObject, new Vector2(1f, 0f), new Vector2(-126f, 34f), new Vector2(240f, 56f));

        page.AddComponent<KeyBindingMenu>();
        return page;
    }

    static GameObject MakeIncomingPage(Transform parent, string objectName, string note)
    {
        GameObject page = NewUI(objectName, parent);
        Stretch(page, 0f, 0f, 0f, 0f);
        AddBox(page).color = PanelColor;

        TextMeshProUGUI message = MakeText(page.transform, "Message", "INCOMING", 80f, Gold, TextAlignmentOptions.Center);
        Place(message.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1000f, 120f));
        message.fontStyle = FontStyles.Bold;
        message.characterSpacing = 12f;
        if (titleMaterial != null) message.fontSharedMaterial = titleMaterial;
        PulseFX pulse = message.gameObject.AddComponent<PulseFX>();
        Set(pulse, "speed", 0.6f);
        Set(pulse, "growBy", 0.04f);
        Set(pulse, "lowestAlpha", 0.55f);

        TextMeshProUGUI noteText = MakeText(page.transform, "Note", note + " Coming soon.", 28f, SoftWhite, TextAlignmentOptions.Center);
        Place(noteText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1000f, 50f));

        GameObject stripesGo = NewUI("Stripes", page.transform);
        Place(stripesGo, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(560f, 22f));
        RawImage stripes = stripesGo.AddComponent<RawImage>();
        stripes.texture = stripesTexture;
        stripes.color = new Color(Gold.r, Gold.g, Gold.b, 0.6f);
        stripes.raycastTarget = false;

        page.AddComponent<IncomingMessage>();
        page.SetActive(false);   // TabGroup opens it
        return page;
    }

    // ----- Pieces -----

    // A full-screen page that can fade (TitleMenu slides pages in and out).
    static RectTransform MakePage(string objectName, Transform parent)
    {
        GameObject page = NewUI(objectName, parent);
        Stretch(page, 0f, 0f, 0f, 0f);
        page.AddComponent<CanvasGroup>();
        return (RectTransform)page.transform;
    }

    // The top of a page: BACK on the left, the page's name in the middle, DEFAULTS on the right (New Game only).
    static void MakeHeader(Transform page, string title, bool withDefaults)
    {
        Button back = MakeButton(page, "BackButton", "< BACK", new Vector2(210f, 64f), 28f, 8, false, false);
        Place(back.gameObject, new Vector2(0f, 1f), new Vector2(155f, -72f), new Vector2(210f, 64f));

        TextMeshProUGUI name = MakeText(page, "PageTitle", title, 60f, Color.white, TextAlignmentOptions.Center);
        Place(name.gameObject, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(900f, 80f));
        name.fontStyle = FontStyles.Bold;
        name.characterSpacing = 10f;

        if (!withDefaults) return;
        Button defaults = MakeButton(page, "DefaultsButton", "DEFAULTS", new Vector2(230f, 64f), 26f, 8, false, false);
        Place(defaults.gameObject, new Vector2(1f, 1f), new Vector2(-165f, -72f), new Vector2(230f, 64f));
    }

    // A menu button: dark box, bold white text, a hidden ">" Arrow, and ButtonFX for the hover / click effects.
    static Button MakeButton(Transform parent, string objectName, string label, Vector2 size, float fontSize,
                             int sparks = 14, bool dimOthers = true, bool arrow = true)
    {
        GameObject go = NewUI(objectName, parent);
        ((RectTransform)go.transform).sizeDelta = size;
        LayoutElement le = go.AddComponent<LayoutElement>();   // its size inside a layout group
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        Image box = AddBox(go);
        box.color = BoxColor;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = box;

        TextMeshProUGUI text = MakeText(go.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center);
        Stretch(text.gameObject, 0f, 0f, 0f, 0f);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 4f;

        if (arrow)
        {
            TextMeshProUGUI a = MakeText(go.transform, "Arrow", ">", fontSize, DarkText, TextAlignmentOptions.Center);
            Place(a.gameObject, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(40f, size.y));
            a.fontStyle = FontStyles.Bold;
            a.gameObject.SetActive(false);   // ButtonFX shows it on hover
        }

        ButtonFX fx = go.AddComponent<ButtonFX>();
        Set(fx, "sparks", sparks);
        Set(fx, "dimOthers", dimOthers);
        return button;
    }

    // A settings tab button (TabGroup colors its box: amber when open, dark when not).
    static Button MakeTab(Transform parent, string objectName, string label)
    {
        GameObject go = NewUI(objectName, parent);
        Image box = AddBox(go);
        box.color = BoxColor;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = box;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);   // a little brighter under the mouse
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = MakeText(go.transform, "Label", label, 26f, Color.white, TextAlignmentOptions.Center);
        Stretch(text.gameObject, 0f, 0f, 0f, 0f);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 4f;
        return button;
    }

    // A row in the New Game list: the gold changed-dot, the row's name, and room for its control at ControlX.
    static RectTransform MakeRow(Transform parent, string objectName, string label, float height = RowHeight)
    {
        GameObject row = NewUI(objectName, parent);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = height;

        Image dot = AddImage(NewUI("Dot", row.transform), Builtin("Knob.psd"), Gold);
        Place(dot.gameObject, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(14f, 14f));

        if (label != null)
        {
            TextMeshProUGUI name = MakeText(row.transform, "Label", label, 26f, Color.white, TextAlignmentOptions.Left);
            Place(name.gameObject, new Vector2(0f, 0.5f), new Vector2(RowLabelX + 165f, 0f), new Vector2(330f, height));
        }
        return (RectTransform)row.transform;
    }

    static void Section(Transform parent, string title)
    {
        GameObject row = NewUI(title.Substring(0, 1) + title.Substring(1).ToLower() + "Header", parent);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = 34f;
        TextMeshProUGUI text = MakeText(row.transform, "Title", title, 22f, Gold, TextAlignmentOptions.BottomLeft);
        Stretch(text.gameObject, 14f, 0f, 0f, 2f);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 6f;
    }

    // Days to win: [ - ]  5  [ + ]
    static void MakeStepperRow(Transform parent, string objectName, string label, int value)
    {
        RectTransform row = MakeRow(parent, objectName, label);

        Button minus = MakeButton(row, "Minus", "-", new Vector2(56f, 44f), 34f, 6, false, false);
        Place(minus.gameObject, new Vector2(0f, 0.5f), new Vector2(ControlX + 28f, 0f), new Vector2(56f, 44f));

        GameObject box = NewUI("ValueBox", row);
        Place(box, new Vector2(0f, 0.5f), new Vector2(ControlX + 116f, 0f), new Vector2(100f, 44f));
        AddBox(box).color = KeyColor;
        box.AddComponent<RectMask2D>();   // the rolling numbers stay inside
        TextMeshProUGUI number = MakeText(box.transform, "Value", value.ToString(), 32f, Gold, TextAlignmentOptions.Center);
        Stretch(number.gameObject, 0f, 0f, 0f, 0f);
        number.fontStyle = FontStyles.Bold;

        Button plus = MakeButton(row, "Plus", "+", new Vector2(56f, 44f), 34f, 6, false, false);
        Place(plus.gameObject, new Vector2(0f, 0.5f), new Vector2(ControlX + 204f, 0f), new Vector2(56f, 44f));

        StepperRow stepper = row.gameObject.AddComponent<StepperRow>();
        Set(stepper, "min", 1);
        Set(stepper, "max", 30);
        Set(stepper, "value", value);
    }

    // Win at / Start time: a TextMeshPro dropdown (NewGamePanel fills in its options).
    static void MakeDropdownRow(Transform parent, string objectName, string label)
    {
        RectTransform row = MakeRow(parent, objectName, label);

        TMP_DefaultControls.Resources res = new TMP_DefaultControls.Resources
        {
            standard = Builtin("UISprite.psd"),
            background = Builtin("Background.psd"),
            inputField = Builtin("InputFieldBackground.psd"),
            knob = Builtin("Knob.psd"),
            checkmark = Builtin("Checkmark.psd"),
            dropdown = Builtin("DropdownArrow.psd"),
            mask = Builtin("UIMask.psd"),
        };
        GameObject go = TMP_DefaultControls.CreateDropdown(res);
        go.name = "Dropdown";
        go.transform.SetParent(row, false);
        SetLayer(go);
        Place(go, new Vector2(0f, 0.5f), new Vector2(ControlX + 220f, 0f), new Vector2(440f, 44f));

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
        go.GetComponent<Image>().color = KeyColor;
        dropdown.captionText.fontSize = 26f;
        dropdown.captionText.color = Color.white;
        dropdown.itemText.fontSize = 24f;
        dropdown.itemText.color = Color.white;
        Transform arrowT = go.transform.Find("Arrow");
        if (arrowT != null) arrowT.GetComponent<Image>().color = Gold;

        RectTransform template = dropdown.template;
        template.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.11f, 0.97f);
        template.sizeDelta = new Vector2(template.sizeDelta.x, 300f);
        Transform item = template.Find("Viewport/Content/Item");
        if (item != null)
        {
            Toggle toggle = item.GetComponent<Toggle>();
            ColorBlock colors = toggle.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.pressedColor = new Color(Gold.r, Gold.g, Gold.b, 0.6f);
            toggle.colors = colors;
            Transform check = item.Find("Item Checkmark");
            if (check != null) check.GetComponent<Image>().color = Gold;
        }

        go.AddComponent<DropdownUnfold>();
    }

    // Day length / Difficulty / When I die: option buttons side by side, a gold Highlight gliding behind the picked one.
    static void MakeChoiceRow(Transform parent, string objectName, string label, string[] options, float optionWidth,
                              Color[] colors, bool withInfo)
    {
        RectTransform row = MakeRow(parent, objectName, label);
        float left = label != null ? ControlX : RowLabelX;
        float width = options.Length * optionWidth + (options.Length - 1) * 8f;

        GameObject holder = NewUI("Options", row);
        Place(holder, new Vector2(0f, 0.5f), new Vector2(left + width / 2f, 0f), new Vector2(width, 44f));
        HorizontalLayoutGroup h = holder.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        Image highlight = AddBox(NewUI("Highlight", holder.transform));
        highlight.color = colors != null && colors.Length > 0 ? colors[0] : Gold;
        highlight.raycastTarget = false;
        highlight.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        highlight.rectTransform.sizeDelta = new Vector2(optionWidth, 44f);

        foreach (string option in options)
        {
            GameObject go = NewUI(option.Replace(" ", ""), holder.transform);
            go.AddComponent<LayoutElement>().preferredWidth = optionWidth;
            Image box = AddBox(go);
            box.color = Color.white;   // the Button's colors below give the real (see-through) look
            Button button = go.AddComponent<Button>();
            button.targetGraphic = box;
            ColorBlock cb = button.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0.07f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
            cb.pressedColor = new Color(1f, 1f, 1f, 0.3f);
            cb.selectedColor = cb.normalColor;
            button.colors = cb;

            TextMeshProUGUI text = MakeText(go.transform, "Label", option, 24f, Color.white, TextAlignmentOptions.Center);
            Stretch(text.gameObject, 0f, 0f, 0f, 0f);
            text.fontStyle = FontStyles.Bold;
        }

        if (withInfo)
        {
            TextMeshProUGUI info = MakeText(row, "Info", "", 26f, Gold, TextAlignmentOptions.Left);
            Place(info.gameObject, new Vector2(0f, 0.5f), new Vector2(left + width + 20f + 80f, 0f), new Vector2(160f, 44f));
        }

        ChoiceRow choice = row.gameObject.AddComponent<ChoiceRow>();
        if (colors != null)
        {
            SerializedObject so = new SerializedObject(choice);
            SerializedProperty list = so.FindProperty("optionColors");
            list.arraySize = colors.Length;
            for (int i = 0; i < colors.Length; i++) list.GetArrayElementAtIndex(i).colorValue = colors[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // The line under Difficulty ("4 enemies every night, normal hunger").
    static void MakeInfoRow(Transform parent, string objectName, string text)
    {
        GameObject row = NewUI(objectName, parent);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = 30f;
        TextMeshProUGUI t = row.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = 24f;
        t.color = Dim;
        t.fontStyle = FontStyles.Italic;
        t.alignment = TextAlignmentOptions.Left;
        t.margin = new Vector4(RowLabelX, 0f, 0f, 0f);
        t.raycastTarget = false;
    }

    // [x] Pick up dropped items...: a tick box with the row's text right of it.
    static void MakeToggleRow(Transform parent, string objectName, string label)
    {
        RectTransform row = MakeRow(parent, objectName, null, 46f);

        GameObject go = NewUI("Toggle", row);
        Place(go, new Vector2(0f, 0.5f), new Vector2(RowLabelX + 18f, 0f), new Vector2(36f, 36f));
        Image box = AddBox(go);
        box.color = KeyColor;
        Image tick = AddImage(NewUI("Checkmark", go.transform), Builtin("Checkmark.psd"), Gold);
        Stretch(tick.gameObject, 2f, 2f, 2f, 2f);

        Toggle toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = tick;
        toggle.isOn = false;
        go.AddComponent<TogglePop>();

        TextMeshProUGUI text = MakeText(row, "Label", label, 26f, Color.white, TextAlignmentOptions.Left);
        Place(text.gameObject, new Vector2(0f, 0.5f), new Vector2(RowLabelX + 60f + 400f, 0f), new Vector2(800f, 46f));
    }

    // ----- Key binding templates -----

    static void MakeHeaderTemplate(Transform parent)
    {
        GameObject go = NewUI("HeaderTemplate", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 40f;
        TextMeshProUGUI text = MakeText(go.transform, "Text", "GROUP", 22f, Gold, TextAlignmentOptions.BottomLeft);
        Stretch(text.gameObject, 16f, 0f, 0f, 4f);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 6f;
    }

    static void MakeRowTemplate(Transform parent)
    {
        GameObject go = NewUI("RowTemplate", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 50f;
        AddImage(go, null, new Color(1f, 1f, 1f, 0f));   // see-through: flashes red when the row shakes

        TextMeshProUGUI label = MakeText(go.transform, "Label", "Action", 26f, Color.white, TextAlignmentOptions.Left);
        Place(label.gameObject, new Vector2(0f, 0.5f), new Vector2(30f + 190f, 0f), new Vector2(380f, 50f));
        MakeKeyCap(go.transform, "Key", 440f + 125f, 250f);
        MakeKeyCap(go.transform, "Key2", 710f + 125f, 250f);
    }

    static void MakeHotbarTemplate(Transform parent)
    {
        GameObject go = NewUI("HotbarRowTemplate", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 50f;
        AddImage(go, null, new Color(1f, 1f, 1f, 0f));

        TextMeshProUGUI label = MakeText(go.transform, "Label", "Hotbar", 26f, Color.white, TextAlignmentOptions.Left);
        Place(label.gameObject, new Vector2(0f, 0.5f), new Vector2(30f + 190f, 0f), new Vector2(380f, 50f));
        for (int i = 0; i < 8; i++) MakeKeyCap(go.transform, "Key" + (i + 1), 440f + 31f + i * 72f, 62f);
    }

    // A key box: shows a key's name, clickable to change it.
    static void MakeKeyCap(Transform parent, string objectName, float centerX, float width)
    {
        GameObject go = NewUI(objectName, parent);
        Place(go, new Vector2(0f, 0.5f), new Vector2(centerX, 0f), new Vector2(width, 42f));
        Image box = AddBox(go);
        box.color = Color.white;   // the Button's colors give the real look
        Button button = go.AddComponent<Button>();
        button.targetGraphic = box;
        ColorBlock cb = button.colors;
        cb.normalColor = KeyColor;
        cb.highlightedColor = new Color(0.3f, 0.33f, 0.42f, 1f);
        cb.pressedColor = new Color(Gold.r * 0.8f, Gold.g * 0.8f, Gold.b * 0.8f, 1f);
        cb.selectedColor = KeyColor;
        cb.disabledColor = new Color(KeyColor.r, KeyColor.g, KeyColor.b, 0.6f);
        button.colors = cb;

        TextMeshProUGUI text = MakeText(go.transform, "Text", "-", 24f, Color.white, TextAlignmentOptions.Center);
        Stretch(text.gameObject, 4f, 0f, 4f, 0f);
    }

    static Scrollbar MakeScrollbar(Transform parent)
    {
        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = Builtin("UISprite.psd"),
            background = Builtin("Background.psd"),
            knob = Builtin("Knob.psd"),
        };
        GameObject go = DefaultControls.CreateScrollbar(res);
        go.name = "Scrollbar";
        go.transform.SetParent(parent, false);
        SetLayer(go);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(14f, -20f);
        rt.anchoredPosition = new Vector2(-6f, 0f);

        Scrollbar bar = go.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        if (bar.handleRect != null) bar.handleRect.GetComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.8f);
        return bar;
    }

    // ----- Small helpers -----

    static TextMeshProUGUI MakeText(Transform parent, string objectName, string text, float size, Color color,
                                TextAlignmentOptions alignment)
    {
        TextMeshProUGUI t = AddText(NewUI(objectName, parent), text, size, alignment);
        t.color = color;
        t.enableWordWrapping = false;
        return t;
    }

    static Image AddImage(GameObject go, Sprite sprite, Color color)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Anchors a UI object to one point of its parent (0,0 = bottom left, 1,1 = top right) at this position and size.
    static void Place(GameObject go, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void AddScaler(GameObject canvasGo)
    {
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    static void SetCascade(CascadeIn cascade, Vector2 offset, float stagger, float duration, UITween.Ease ease)
    {
        SerializedObject so = new SerializedObject(cascade);
        so.FindProperty("offset").vector2Value = offset;
        so.FindProperty("stagger").floatValue = stagger;
        so.FindProperty("duration").floatValue = duration;
        so.FindProperty("ease").enumValueIndex = (int)ease;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Sets one Inspector field by its name (the names in quotes must match the field names in the script).
    static void Set(Object target, string field, object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning(ToolName + ": no field \"" + field + "\" on " + target.GetType().Name); return; }

        switch (value)
        {
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case bool b: p.boolValue = b; break;
            case Object o: p.objectReferenceValue = o; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLayer(GameObject go)
    {
        go.layer = UILayer;
        foreach (Transform child in go.transform) SetLayer(child.gameObject);
    }
}
