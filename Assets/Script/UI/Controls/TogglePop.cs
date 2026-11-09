/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto any Toggle (tick box). Tools > UI > Build Main Title Scene puts it on the New Game rules.
Unity Editor: Nothing to connect. Testing: Play and tick the box.
Purpose: Ticking the box makes the tick pop in: it grows from nothing past its size and settles (unticking hides it).
Requirement: TogglePop.cs x-> UITween.cs, Toggle (Unity)
             TogglePop.cs <-x TitleSceneBuilder.cs
*/

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
[DisallowMultipleComponent]
public class TogglePop : MonoBehaviour
{
    // ===== Fields =====
    Toggle toggle;
    Transform tick;   // the Toggle's Graphic (the tick picture)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.toggleTransition = Toggle.ToggleTransition.None;   // this script animates it instead of a fade
        tick = toggle.graphic != null ? toggle.graphic.transform : null;
        toggle.onValueChanged.AddListener(OnToggled);
    }

    void OnDisable()
    {
        if (tick != null) tick.localScale = Vector3.one;
    }

    // ===== My methods =====

    void OnToggled(bool on)
    {
        if (tick == null || !on || !isActiveAndEnabled) return;

        tick.localScale = Vector3.zero;
        UITween.Scale(this, tick, Vector3.one, 0.35f, UITween.Ease.OutBack);
    }
}
