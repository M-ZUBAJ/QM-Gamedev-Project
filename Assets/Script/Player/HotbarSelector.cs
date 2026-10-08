/*
First Date: 7/10/2026
Date: 7/10/2026
Author: Tony
Use by: Drag this script onto the Player (next to PlayerInventory). Tools > UI > Add Inventory Scripts adds it for you.
Unity Editor: Keys 1 - 8 are set in KeyBindings (Hotbar1 - Hotbar8).
              Testing: Play, press 1 - 8 or turn the mouse wheel: the selected hotbar slot changes color.
Purpose: Chooses the item in hand, like Minecraft:
         number key = select that hotbar slot (press it again = select nothing),
         mouse wheel = next / previous slot.
         Does nothing while a menu is open, or while another mode uses the wheel (e.g. turning a blueprint).
Requirement: HotbarSelector.cs x-> PlayerInventory.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs
             HotbarSelector.cs <-x InventoryUIBuilder.cs
*/

using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[DisallowMultipleComponent]
public class HotbarSelector : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Off (like Minecraft): wheel down = next slot. On: wheel up = next slot.")]
    [SerializeField] bool invertWheel = false;

    PlayerInventory inv;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (MenuState.AnyOpen || MenuState.KeysCaptured) return;   // a menu has the keyboard and mouse

        int keys = Mathf.Min(inv.HotbarSize, KeyBindings.HotbarKeyCount);
        for (int i = 0; i < keys; i++)
        {
            if (Input.GetKeyDown(KeyBindings.HotbarKey(i))) inv.ToggleHotbar(i);
        }

        if (ClickClaim.WheelClaimed) return;   // e.g. the wheel turns a blueprint while placing

        float wheel = Input.mouseScrollDelta.y;   // up = positive
        if (wheel != 0f)
        {
            int step = wheel > 0f ? -1 : 1;       // Minecraft: wheel up = previous slot
            inv.ScrollHotbar(invertWheel ? -step : step);
        }
    }
}
