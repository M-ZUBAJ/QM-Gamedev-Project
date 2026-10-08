/*
First Date: 7/10/2026
Date: 8/10/2026
Author: Tony
Use by: Drag this script onto BuildMenuCanvas (NOT onto BuildWindow itself: a hidden object can't listen for keys).
        Tools > UI > Build Build Menu UI makes all of it for you.
Unity Editor: Window = BuildWindow. List = the object the building buttons go in. Button Prefab = BuildingButton prefab.
              Buildings = every Building Data (config file) to show, in order.
              Tools > Building > Update Build Menu List fills Buildings with every Building Data in the project.
              Testing: Play, press B (the Build key, set in KeyBindings), click a building, place it with left click.
              Full steps: Docs/Build Menu Setup.md
Purpose: The build menu. B opens / closes it (Esc also closes it). It shows one button per building,
         with its picture, name and recipe. Clicking one closes the menu and starts placing it (BlueprintPlacer).
         Pressing B while placing stops placing.
Requirement: BuildMenu.cs x-> BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, KeyBindings.cs, MenuState.cs
             BuildMenu.cs <-x BuildMenuUIBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

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

    bool isOpen;

    public bool IsOpen => isOpen;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        MakeButtons();
        SetOpen(false);   // always start closed, even if the window was left visible in the editor
    }

    void Update()
    {
        // Another menu has the cursor, or a menu is reading every key (e.g. rebinding): not for us.
        if (MenuState.OthersOpen(this) || MenuState.KeysCaptured) return;

        if (Input.GetKeyDown(KeyBindings.Build))
        {
            if (BlueprintPlacer.IsPlacing) BlueprintPlacer.CancelPlacing();   // B while placing = stop placing
            else SetOpen(!isOpen);
        }
        else if (isOpen && Input.GetKeyDown(KeyBindings.UnlockCursor))
        {
            SetOpen(false);   // Esc closes it too
        }
    }

    void OnDestroy()
    {
        MenuState.SetOpen(this, false);   // static values outlive the scene, so clear them
    }

    // ===== My methods =====

    // Also usable from a Close button: On Click () > BuildMenuCanvas > BuildMenu.SetOpen (ticked = open).
    public void SetOpen(bool open)
    {
        isOpen = open;
        if (window != null) window.SetActive(open);

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
            button.Setup(data, Choose);
        }
    }

    // A building button was clicked: close the menu and start placing it.
    void Choose(BuildingData data)
    {
        SetOpen(false);
        BlueprintPlacer.Begin(data);
    }
}
