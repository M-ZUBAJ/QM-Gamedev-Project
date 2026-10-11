/*
Date: 6/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component).
        A mode that owns the mouse:   ClickClaim.Take(this) ... ClickClaim.Release(this)
        A single click that was used:  ClickClaim.UseThisFrame()
        Other scripts ask:             if (ClickClaim.Claimed) return;   (and ClickClaim.WheelClaimed for the wheel)
Purpose: Several scripts use the same mouse buttons (e.g. right click = levitate, cancel placing, close a panel).
         Instead of every script knowing every other one, the script that is using the mouse "claims" it here,
         and the others simply step aside. So e.g. ObjectLevitator works in another project without the building scripts.
         Claimed stays true for the frame the claim ends too, so the click that ended it isn't used twice.
Requirement: ClickClaim.cs <-x BlueprintPlacer.cs, HotbarSelector.cs, ItemConsumer.cs, ObjectLevitator.cs
*/

using UnityEngine;

public static class ClickClaim
{
    // ===== Fields =====
    static object owner;              // the mode that owns the mouse right now (null = nobody)
    static int usedFrame = -1;        // the last frame a claim ended, or a click was used

    // ===== My methods =====

    // The mouse buttons belong to someone else this frame: don't act on clicks.
    public static bool Claimed => owner != null || Time.frameCount == usedFrame;

    // The mouse wheel belongs to someone else (e.g. it rotates a blueprint instead of changing the hotbar slot).
    public static bool WheelClaimed => owner != null;

    // Starts owning the mouse (e.g. placing a blueprint).
    public static void Take(object who)
    {
        owner = who;
    }

    // Stops owning it. The click of this frame still counts as used.
    public static void Release(object who)
    {
        if (owner != who) return;   // someone else owns it now: not ours to release
        owner = null;
        usedFrame = Time.frameCount;
    }

    // One click was used this frame (e.g. it closed a panel), so nobody else should act on it.
    public static void UseThisFrame()
    {
        usedFrame = Time.frameCount;
    }

    // Static values outlive the scene (and Play sessions if domain reload is off): start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        owner = null;
        usedFrame = -1;
    }
}
