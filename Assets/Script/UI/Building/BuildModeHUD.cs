/*
First Date: 8/10/2026
Date: 8/10/2026
Author: Tony
Use by: Drag this script onto BuildModeCanvas (the canvas with the mouse button hints for build mode).
        Tools > UI > Add Build Mode Scripts adds it for you.
Unity Editor: Nothing to set. Leave BuildModeCanvas switched on in the Hierarchy: this script hides it by itself.
              Testing: Play, press B, click a building: the canvas shows. Place it or cancel: it hides again.
Purpose: Shows the build mode canvas only while the player is placing a building (BlueprintPlacer),
         and hides it the rest of the time.
         It switches off the Canvas component, not the object, so this script keeps running and can show it again.
Requirement: BuildModeHUD.cs x-> BlueprintPlacer.cs, Canvas (Unity)
             BuildModeHUD.cs <-x BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs
*/

using UnityEngine;

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class BuildModeHUD : MonoBehaviour
{
    // ===== Fields =====
    Canvas canvas;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;   // not placing when the game starts
    }

    void Update()
    {
        bool placing = BlueprintPlacer.IsPlacing;
        if (canvas.enabled != placing) canvas.enabled = placing;   // only change it when it really changes
    }
}
