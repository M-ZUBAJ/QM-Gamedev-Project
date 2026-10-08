/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: Drag this script onto InventoryCanvas (NOT onto InventoryWindow itself: a hidden object can't listen for keys).
Unity Editor: Drag InventoryWindow (the child that holds the tabs and pages) into "Window".
              The hotbar should NOT be inside InventoryWindow, so it stays on screen when the inventory is closed.
              Testing: press Play, then Tab (the Inventory key, set in KeyBindings). Esc also closes it.
              Full steps: Docs/Inventory Setup.md
Purpose: Opens and closes the inventory window.
         Open: the mouse cursor shows (to click slots and tabs) and the camera stops turning.
         Closed: the cursor hides again and the camera follows the mouse.
Requirement: InventoryWindow.cs x-> KeyBindings.cs, MenuState.cs
             InventoryWindow.cs <-x InventoryUIBuilder.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class InventoryWindow : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The object to show and hide: InventoryWindow (tabs + pages). Not the hotbar.")]
    [SerializeField] GameObject window;

    bool isOpen;

    public bool IsOpen => isOpen;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        SetOpen(false);   // always start closed, even if the window was left visible in the editor
    }

    void Update()
    {
        // Another menu has the cursor, or a menu is reading every key (e.g. rebinding): not for us.
        if (MenuState.OthersOpen(this) || MenuState.KeysCaptured) return;

        if (Input.GetKeyDown(KeyBindings.Inventory)) SetOpen(!isOpen);
        else if (isOpen && Input.GetKeyDown(KeyBindings.UnlockCursor)) SetOpen(false);   // Esc closes it too
    }

    void OnDestroy()
    {
        MenuState.SetOpen(this, false);   // static values outlive the scene, so clear them
    }

    // ===== My methods =====

    // Also usable from a Close button: On Click () > InventoryCanvas > InventoryWindow.SetOpen (ticked = open).
    public void SetOpen(bool open)
    {
        isOpen = open;
        if (window != null) window.SetActive(open);

        MenuState.SetOpen(this, open);   // the camera, levitating, building... all pause while a menu is open

        // Open: free cursor to click. Closed: hidden cursor so the mouse turns the camera.
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }
}
