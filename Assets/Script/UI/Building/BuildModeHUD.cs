/*
First Date: 8/10/2026
Date: 2/11/2026
Author: Tony
Use by: Drag this script onto BuildModeCanvas (the canvas with the mouse button hints for build mode).
        Tools > UI > Add Build Mode Scripts adds it for you.
Unity Editor: Nothing to set. Leave BuildModeCanvas switched on in the Hierarchy: this script hides it by itself.
              The middle click hint's text (a child named "MiddleClickText") shows the placing mode right now:
              "Snap" or "Surface" (BlueprintPlacer.Mode).
              Testing: Play, press B, click a building: the canvas shows. Middle click: the hint changes.
              Place it or cancel: it hides again.
Purpose: Shows the build mode canvas only while the player is placing a building (BlueprintPlacer),
         and hides it the rest of the time. Keeps the middle click hint showing the current placing mode.
         It switches off the Canvas component, not the object, so this script keeps running and can show it again.
Requirement: BuildModeHUD.cs x-> BlueprintPlacer.cs, Canvas (Unity)
             BuildModeHUD.cs <-x BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs
*/

using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class BuildModeHUD : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("Middle click hint text while in Snap mode.")]
    [SerializeField] string snapText = "Snap";

    [Tooltip("Middle click hint text while in Surface mode.")]
    [SerializeField] string surfaceText = "Surface";

    const string ModeTextName = "MiddleClickText";   // the hint text this script rewrites

    Canvas canvas;
    TMP_Text modeText;   // can be missing (then only showing / hiding works)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.enabled = false;   // not placing when the game starts
        modeText = FindText(transform, ModeTextName);
    }

    void Update()
    {
        bool placing = BlueprintPlacer.IsPlacing;
        if (canvas.enabled != placing) canvas.enabled = placing;   // only change it when it really changes
        if (!placing || modeText == null) return;

        string text = BlueprintPlacer.Mode == PlaceMode.Snap ? snapText : surfaceText;
        if (modeText.text != text) modeText.text = text;
    }

    // ===== My methods =====

    // A TextMeshPro text with this name anywhere under "parent", or null.
    static TMP_Text FindText(Transform parent, string childName)
    {
        foreach (TMP_Text t in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.name == childName) return t;
        }
        return null;
    }
}
