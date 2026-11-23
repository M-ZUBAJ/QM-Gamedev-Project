/*
First Date: 7/10/2026
Date: 23/11/2026
Author: Tony
Use by: Drag this script onto BuildMenuCanvas (NOT onto BuildWindow itself: a hidden object can't listen for keys).
        Tools > UI > Build Build Menu UI makes all of it for you.
Unity Editor: Window = BuildWindow. List = the object the building buttons go in. Button Prefab = BuildingButton prefab.
              Buildings = every Building Data (config file) to show, in order.
              Tools > Building > Update Build Menu List fills Buildings with every Building Data in the project.
              Found by name, all optional: BuildWindow > Tabs (one button per tab: "All", and one named after each
              Build Category: "Building", "Traps") and BuildWindow > Info (the info box: InfoTitle, InfoText, InfoStats).
              Testing: Play, press B (the Build key, set in KeyBindings), click a tab, rest the mouse on a building for
              1 second (its info box shows), click it, place it with left click (the next one follows: keep placing),
              right click = back to this menu, B or Esc = stop.
              Full steps: Docs/Build Menu Setup.md
Purpose: The build menu. B opens / closes it (Esc also closes it). It shows one button per building,
         with its picture, name and recipe, and since 23/11/2026:
         - tabs: All, Building, Traps (each Building Data's Category); the picked tab is gold, an empty one faded;
           the pick stays while the game runs;
         - an info box when the mouse rests on a building for Info Delay seconds (1): its name, its Description and
           the numbers its prefab gives (IBuildingInfo, e.g. the iron trap's damage and stun time), next to the
           mouse and kept inside the window.
         Clicking a building closes the menu and starts placing it (BlueprintPlacer: build mode, which carries on
         with the same building after each one is placed). Right click while placing opens this menu again
         (BlueprintPlacer.BackToMenuRequested); B while placing stops building.
         It also closes by itself when every menu is told to close (MenuState.CloseAll, e.g. the player died).
Requirement: BuildMenu.cs x-> BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, KeyBindings.cs, MenuState.cs
             BuildMenu.cs <-x BuildMenuUIBuilder.cs, GearPageBuilder.cs, TrapBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BuildMenu : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The object to show and hide: BuildWindow.")]
    [SerializeField] GameObject window;

    [Tooltip("Where the building buttons are made (an object with a Grid Layout Group, e.g. BuildWindow/List).")]
    [SerializeField] Transform list;

    [Tooltip("The BuildingButton prefab: one is made per building.")]
    [SerializeField] BuildingButton buttonPrefab;

    [Tooltip("Every building in the menu (Building Data config files), in order. " +
             "Tools > Building > Update Build Menu List fills this for you.")]
    [SerializeField] List<BuildingData> buildings = new List<BuildingData>();

    [Header("Tabs (BuildWindow > Tabs)")]
    [Tooltip("The picked tab's color.")]
    [SerializeField] Color pickedTabColor = new Color(1f, 0.82f, 0.35f);

    [Tooltip("The other tabs' color.")]
    [SerializeField] Color tabColor = Color.white;

    [Tooltip("How see-through a tab with nothing in it is (1 = solid).")]
    [Range(0f, 1f)]
    [SerializeField] float emptyTabAlpha = 0.45f;

    [Header("Info box (BuildWindow > Info)")]
    [Tooltip("Seconds the mouse must rest on a building before its info box shows.")]
    [Range(0f, 5f)]
    [SerializeField] float infoDelay = 1f;

    [Tooltip("Where the info box sits from the mouse: right and down (it flips near the window's edges).")]
    [SerializeField] Vector2 infoOffset = new Vector2(24f, -24f);

    bool isOpen;
    readonly List<BuildingButton> buttons = new List<BuildingButton>();
    readonly List<(Button button, BuildCategory? category)> tabs = new List<(Button, BuildCategory?)>();
    BuildCategory? picked;     // the tab shown (null = All)
    RectTransform info;        // the info box (can be missing)
    TMP_Text infoTitle;
    TMP_Text infoText;
    TMP_Text infoStats;
    BuildingButton hovered;    // the building the mouse rests on
    float hoverSince;          // unscaled time it got there
    bool infoShown;

    public bool IsOpen => isOpen;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        MakeButtons();
        FindTabs();
        FindInfo();
        Pick(picked);
        SetOpen(false);   // always start closed, even if the window was left visible in the editor
    }

    void Update()
    {
        // Another menu has the cursor, or a menu is reading every key (e.g. rebinding): not for us.
        if (MenuState.OthersOpen(this) || MenuState.KeysCaptured) return;

        if (Input.GetKeyDown(KeyBindings.Build))
        {
            if (BlueprintPlacer.IsPlacing) BlueprintPlacer.CancelPlacing();   // B while building = stop building
            else SetOpen(!isOpen);
        }
        else if (isOpen && Input.GetKeyDown(KeyBindings.UnlockCursor))
        {
            SetOpen(false);   // Esc closes it too
        }

        if (isOpen) UpdateInfo();
    }

    void OnEnable()
    {
        MenuState.CloseAllRequested += CloseNow;              // e.g. the player died: close with every other menu
        BlueprintPlacer.BackToMenuRequested += OpenAgain;     // right click while building: pick something else
    }

    void OnDisable()
    {
        MenuState.CloseAllRequested -= CloseNow;
        BlueprintPlacer.BackToMenuRequested -= OpenAgain;
    }

    void OnDestroy()
    {
        MenuState.SetOpen(this, false);   // static values outlive the scene, so clear them
    }

    // ===== My methods =====

    void CloseNow()
    {
        if (isOpen) SetOpen(false);
    }

    void OpenAgain()
    {
        if (!isOpen && !MenuState.OthersOpen(this)) SetOpen(true);
    }

    // Also usable from a Close button: On Click () > BuildMenuCanvas > BuildMenu.SetOpen (ticked = open).
    public void SetOpen(bool open)
    {
        isOpen = open;
        if (window != null) window.SetActive(open);
        if (!open)
        {
            hovered = null;
            HideInfo();
        }

        MenuState.SetOpen(this, open);   // the camera, levitating, the hotbar... all pause while a menu is open

        // Open: free cursor to click. Closed: hidden cursor so the mouse turns the camera.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    // One button per building, made once when the game starts.
    void MakeButtons()
    {
        if (list == null || buttonPrefab == null)
        {
            Debug.LogWarning("BuildMenu: drag in List and Button Prefab (or use Tools > UI > Build Build Menu UI).");
            return;
        }

        foreach (BuildingData data in buildings)
        {
            if (data == null || data.Prefab == null) continue;   // an empty line, or a config without a prefab

            BuildingButton button = Instantiate(buttonPrefab, list);
            button.Setup(data, Choose, Hover, Leave);
            buttons.Add(button);
        }
    }

    // A building button was clicked: close the menu and start placing it (build mode).
    void Choose(BuildingData data)
    {
        SetOpen(false);
        BlueprintPlacer.Begin(data);
    }

    // ----- Tabs -----

    // The tab buttons in BuildWindow > Tabs: "All" shows everything, the others are named after a Build Category.
    void FindTabs()
    {
        Transform row = window != null ? window.transform.Find("Tabs") : null;
        if (row == null) return;
        foreach (Transform child in row)
        {
            Button tab = child.GetComponent<Button>();
            if (tab == null) continue;
            BuildCategory? category = null;
            if (child.name != "All")
            {
                if (!System.Enum.TryParse(child.name, out BuildCategory found)) continue;   // not a category name
                category = found;
            }
            tabs.Add((tab, category));
            tab.onClick.AddListener(() => Pick(category));
        }
    }

    // Shows only this tab's buildings (null = all of them) and colors the tabs: the picked one gold, empty ones faded.
    void Pick(BuildCategory? category)
    {
        picked = category;
        hovered = null;
        HideInfo();
        foreach (BuildingButton button in buttons)
            button.gameObject.SetActive(category == null || button.Data.Category == category);

        foreach ((Button tab, BuildCategory? tabCategory) in tabs)
        {
            bool isPicked = tabCategory == category;
            if (tab.targetGraphic != null) tab.targetGraphic.color = isPicked ? pickedTabColor : tabColor;
            TMP_Text label = tab.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.fontStyle = isPicked ? FontStyles.Bold : FontStyles.Normal;
            CanvasGroup look = tab.GetComponent<CanvasGroup>();
            if (look != null) look.alpha = tabCategory == null || HasAny(tabCategory.Value) ? 1f : emptyTabAlpha;
        }
    }

    bool HasAny(BuildCategory category)
    {
        foreach (BuildingButton button in buttons)
        {
            if (button.Data.Category == category) return true;
        }
        return false;
    }

    // ----- Info box -----

    void FindInfo()
    {
        Transform box = window != null ? window.transform.Find("Info") : null;
        if (box == null) return;
        info = (RectTransform)box;
        infoTitle = FindText(box, "InfoTitle");
        infoText = FindText(box, "InfoText");
        infoStats = FindText(box, "InfoStats");
        box.gameObject.SetActive(false);
    }

    // The mouse came onto a building button: its info box shows after Info Delay seconds.
    void Hover(BuildingButton button)
    {
        hovered = button;
        hoverSince = Time.unscaledTime;
        HideInfo();
    }

    void Leave(BuildingButton button)
    {
        if (hovered != button) return;
        hovered = null;
        HideInfo();
    }

    void UpdateInfo()
    {
        if (info == null || hovered == null) return;
        if (!infoShown && Time.unscaledTime - hoverSince >= infoDelay) ShowInfo(hovered.Data);
        if (infoShown) PlaceInfo();
    }

    // Name, description and the numbers its prefab gives (e.g. "Damage 20").
    void ShowInfo(BuildingData data)
    {
        if (infoTitle != null) infoTitle.text = data.DisplayName;
        if (infoText != null)
        {
            infoText.text = data.Description;
            infoText.gameObject.SetActive(!string.IsNullOrEmpty(data.Description));
        }
        string stats = data.InfoLines();
        if (infoStats != null)
        {
            infoStats.text = stats;
            infoStats.gameObject.SetActive(!string.IsNullOrEmpty(stats));
        }
        info.gameObject.SetActive(true);
        info.SetAsLastSibling();   // drawn on top of the buttons
        infoShown = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(info);   // its size fits the new words before it's placed
        PlaceInfo();
    }

    void HideInfo()
    {
        infoShown = false;
        if (info != null) info.gameObject.SetActive(false);
    }

    // Next to the mouse, kept inside the window: it flips to the left near the right edge, and up near the bottom.
    void PlaceInfo()
    {
        RectTransform area = (RectTransform)window.transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, Input.mousePosition, null, out Vector2 mouse)) return;
        Vector2 size = info.rect.size;
        Rect inside = area.rect;
        bool left = mouse.x + infoOffset.x + size.x > inside.xMax;
        bool up = mouse.y + infoOffset.y - size.y < inside.yMin;
        info.anchorMin = info.anchorMax = new Vector2(0.5f, 0.5f);
        info.pivot = new Vector2(left ? 1f : 0f, up ? 0f : 1f);
        info.anchoredPosition = mouse + new Vector2(left ? -infoOffset.x : infoOffset.x, up ? -infoOffset.y : infoOffset.y);
    }

    // A TextMeshPro text with this name anywhere under "parent", or null.
    static TMP_Text FindText(Transform parent, string childName)
    {
        foreach (TMP_Text text in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == childName) return text;
        }
        return null;
    }
}
