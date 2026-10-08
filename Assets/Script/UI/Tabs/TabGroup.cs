/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: Drag this script onto the object that holds the tab buttons (e.g. TabBar).
Unity Editor: Tabs: set Size to the number of tabs, then for each one drag in its Button and its Page.
              Element 0 = InventoryTab + InventoryPage, 1 = GearTab + GearPage, 2 = CraftingTab + CraftingPage.
              Testing: press Play, open the window and click the tabs.
              Full steps: Docs/Inventory Setup.md
Purpose: Tabs like a web browser: clicking a tab button shows its page and hides the others.
         The chosen tab's button gets the Selected Color so you can see which page is open.
         Works for any window with tabs, not only the inventory.
Requirement: TabGroup.cs <-x InventoryUIBuilder.cs
*/

using UnityEngine;
using UnityEngine.UI;   // Button, Graphic

[DisallowMultipleComponent]
public class TabGroup : MonoBehaviour
{
    // One tab: the button you click and the page it shows.
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject page;
    }

    // ===== Fields =====
    [Tooltip("Every tab, in order. Each one is a button and the page it opens.")]
    [SerializeField] Tab[] tabs;

    [Min(0)]
    [Tooltip("Which tab is open the first time (0 = the first one).")]
    [SerializeField] int startTab = 0;

    [Tooltip("Ticked: reopening the window shows the tab you had open last. Unticked: always the Start Tab.")]
    [SerializeField] bool rememberLast = true;

    [Header("Colors")]
    [Tooltip("Button color of the open tab.")]
    [SerializeField] Color selectedColor = new Color(1f, 0.85f, 0.4f);

    [Tooltip("Button color of the other tabs.")]
    [SerializeField] Color normalColor = Color.white;

    int current = -1;   // the open tab (-1 = none yet)

    public int Current => current;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        // Tell every button which tab it opens. "index" is copied per button, so each click remembers its own number.
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => Select(index));
        }
    }

    // Runs every time the window opens.
    void OnEnable()
    {
        Select(rememberLast && current >= 0 ? current : startTab);
    }

    // ===== My methods =====

    // Shows tab i and hides the rest. Also usable from a button's On Click ().
    public void Select(int i)
    {
        if (tabs == null || tabs.Length == 0) return;
        current = Mathf.Clamp(i, 0, tabs.Length - 1);

        for (int j = 0; j < tabs.Length; j++)
        {
            bool open = j == current;
            if (tabs[j].page != null) tabs[j].page.SetActive(open);
            if (tabs[j].button != null && tabs[j].button.targetGraphic != null)
                tabs[j].button.targetGraphic.color = open ? selectedColor : normalColor;
        }
    }
}
