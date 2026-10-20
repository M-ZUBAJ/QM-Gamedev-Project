/*
First Date: 20/10/2026
Date: 20/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component). The slots, the cursor stack and the build menu ask it
        what to show for an item.
Unity Editor: Controlled by GameRules > Item Icons: Overwrite Item Icon (tick box) and Item Icon Style (dropdown).
              Testing: Play, open the inventory (Tab), then change the two in GameRules: the slots redraw at once.
Purpose: Decides how an item is shown in the UI.
         Overwrite Item Icon off: the item's own icon (what its Item Data says), as before.
         On: every item the same way (Item Icon Style):
           Auto Icon          = a photo of the item's prefab
           Label              = no picture, the item's prefab name in capitals, e.g. "SWORD"
           Static Image       = the item's own still picture (Item Data > Icon > Image)
           Aseprite Animation = the item's own animation (Item Data > Icon = Animated, with an .aseprite file)
         An item without that picture / animation gets Auto Icon instead, so nothing is ever blank.
Requirement: ItemIconRules.cs x-> GameRules.cs, ItemData.cs, IconSource.cs, IconCapture.cs, PlayerInventory.cs
             ItemIconRules.cs <-x BuildingButton.cs, HeldItemUI.cs, SlotUI.cs
*/

using UnityEngine;

public static class ItemIconRules
{
    // ===== My methods =====

    // True while every item should show its name instead of a picture.
    public static bool UseLabel => GameRules.OverwriteItemIcon && GameRules.ItemIconStyle == ItemIconStyle.Label;

    // What to show for an item: a still picture and, if it should move, its animation (else null).
    // data = the item's Item Data (can be null for old items), stored = the picture the inventory already has.
    // Label: both come back null (the caller shows Label() text instead).
    public static void Resolve(ItemData data, Sprite stored, out Sprite still, out IconSource animation)
    {
        // Rule off (or an old item with no Item Data): the item's own icon, exactly as before.
        still = stored;
        animation = data != null ? data.Icon : null;
        if (!GameRules.OverwriteItemIcon || data == null) return;

        animation = null;
        switch (GameRules.ItemIconStyle)
        {
            case ItemIconStyle.Label:
                still = null;
                return;

            case ItemIconStyle.StaticImage:
                if (data.Icon != null && data.Icon.image != null) { still = data.Icon.image; return; }
                break;   // no still picture: Auto Icon below

            case ItemIconStyle.AsepriteAnimation:
                // IsAnimated = its Icon is set to Animated and the .aseprite frames were read.
                if (data.Icon != null && data.Icon.IsAnimated) { still = data.Icon.Still; animation = data.Icon; return; }
                break;   // no animation: Auto Icon below
        }

        still = AutoIcon(data, stored);
    }

    // The text for Label: the prefab's name in capitals ("Sword" -> "SWORD"), like Marko's tool names.
    // No prefab: the item's display name, then the name the slot remembers.
    public static string Label(PlayerInventory.Slot slot)
    {
        if (slot == null || slot.IsEmpty) return "";

        string name = slot.data != null && slot.data.Prefab != null ? slot.data.Prefab.name
                    : slot.data != null ? slot.data.DisplayName
                    : slot.name;
        return string.IsNullOrEmpty(name) ? slot.id.ToUpperInvariant() : name.ToUpperInvariant();
    }

    // A photo of the item's prefab (taken once per item, then remembered by IconCapture).
    // No prefab to photograph: the picture the inventory already has.
    static Sprite AutoIcon(ItemData data, Sprite stored)
    {
        if (data.Prefab == null) return stored;
        Sprite photo = IconCapture.GetIcon(data.Id, data.Prefab);
        return photo != null ? photo : stored;
    }
}
