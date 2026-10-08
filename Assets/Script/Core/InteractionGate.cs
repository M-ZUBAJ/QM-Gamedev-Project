/*
First Date: 6/10/2026
Date: 7/10/2026
Author: Tony
Use by: Nothing to set up (code only, no component).
        Scripts that grab / levitate / build ask:  InteractionGate.CanInteractWith(collider)  or  CanInteractAt(point)
        A rule plugs itself in:                    InteractionGate.AllowsBox = ...;  InteractionGate.AllowsPoint = ...;
        (InteractionMachine does this by itself when the game starts.)
Purpose: "May the player interact with this?" asked in ONE neutral place.
         With no rule plugged in (e.g. a project without InteractionMachine), everything is allowed,
         so ObjectLevitator, ItemGrabber and the building scripts work in other projects too.
Requirement: InteractionGate.cs <-x BlueprintPlacer.cs, ItemGrabber.cs, ObjectLevitator.cs
*/

using UnityEngine;

public static class InteractionGate
{
    // ===== Fields =====
    // The rules (null = no rule = allowed). Set by e.g. InteractionMachine.
    public static System.Func<Bounds, bool> AllowsBox;     // for an object's box (any part of it)
    public static System.Func<Vector3, bool> AllowsPoint;  // for one point (e.g. where a held tool floats)

    // ===== My methods =====

    // True if the player may interact with this object (its collider's box).
    public static bool CanInteractWith(Collider col)
    {
        if (AllowsBox == null) return true;   // no rule: allowed
        return col != null && AllowsBox(col.bounds);
    }

    // Same, for a box (e.g. a whole blueprint made of many parts).
    public static bool CanInteractWith(Bounds box)
    {
        return AllowsBox == null || AllowsBox(box);
    }

    // Same, for one point.
    public static bool CanInteractAt(Vector3 point)
    {
        return AllowsPoint == null || AllowsPoint(point);
    }

    // Static values outlive Play sessions if domain reload is off: start with no rules every time
    // (the rule scripts plug themselves in again right after).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        AllowsBox = null;
        AllowsPoint = null;
    }
}
