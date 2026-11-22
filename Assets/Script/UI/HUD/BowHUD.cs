/*
First Date: 22/11/2026
Date: 22/11/2026
Author: Tony
Use by: BowCanvas (Tools > Items > Build Bow And Arrows makes Assets/Prefabs/UI/Bow/BowCanvas.prefab and puts it in the
        game scenes). It finds by NAME: "Bow" (the part that fades in and out), "DrawRing" (an Image set to Filled, Radial
        360), "ArrowIcon" (an Image) and "ArrowCount" (a TextMeshPro text).
Unity Editor: Nothing to connect. Testing: Play, select a bow in the hotbar: the arrow and how many you have show under the
              crosshair; hold the left mouse button: the ring around the crosshair fills, gold once fully drawn.
Purpose: The bow's little display, only while a bow is in the hand (hidden while a menu is open):
           - a ring around the crosshair that fills as the bow is drawn, white while drawing and gold when fully drawn
             (full damage; only then can a shot be a critical hit);
           - under it the arrow it will shoot (the strongest you carry) and how many of it you have, or "No arrows" in
             red when there are none.
Requirement: BowHUD.cs x-> BowShooter.cs, ItemData.cs, MenuState.cs
             BowHUD.cs <-x BowBuilder.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BowHUD : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The ring's color while the bow is being drawn.")]
    [SerializeField] Color drawingColor = new Color(1f, 1f, 1f, 0.9f);

    [Tooltip("The ring's color once the bow is fully drawn.")]
    [SerializeField] Color fullColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Tooltip("The arrow count's color.")]
    [SerializeField] Color countColor = Color.white;

    [Tooltip("The color of \"No arrows\".")]
    [SerializeField] Color noArrowsColor = new Color(1f, 0.38f, 0.32f);

    [Tooltip("Said under the crosshair when the bow has nothing to shoot.")]
    [SerializeField] string noArrowsText = "No arrows";

    [Range(1f, 30f)]
    [Tooltip("How fast it fades in and out (higher = quicker).")]
    [SerializeField] float fadeSpeed = 8f;

    CanvasGroup group;
    Image ring;
    Image icon;
    TMP_Text count;
    ItemData shownArrow;
    int shownCount = -1;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Transform bow = FindDeep(transform, "Bow");
        Transform holder = bow != null ? bow : transform;
        group = holder.GetComponent<CanvasGroup>();
        if (group == null) group = holder.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        ring = FindDeep(transform, "DrawRing")?.GetComponent<Image>();
        icon = FindDeep(transform, "ArrowIcon")?.GetComponent<Image>();
        count = FindDeep(transform, "ArrowCount")?.GetComponent<TMP_Text>();
    }

    void Update()
    {
        bool show = BowShooter.HoldingBow && !MenuState.AnyOpen;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * fadeSpeed);
        if (group.alpha <= 0f) return;

        float power = BowShooter.Power;
        if (ring != null)
        {
            ring.fillAmount = power;
            ring.color = power >= 1f ? fullColor : drawingColor;
        }

        ItemData arrow = BowShooter.NextArrow;
        int have = BowShooter.ArrowCount;
        if (arrow != shownArrow || have != shownCount) Write(arrow, have);
    }

    // ===== My methods =====

    // The arrow's picture and how many (only when it changes).
    void Write(ItemData arrow, int have)
    {
        shownArrow = arrow;
        shownCount = have;
        bool any = arrow != null && have > 0;
        if (icon != null)
        {
            icon.sprite = any ? arrow.GetIcon() : null;
            icon.enabled = icon.sprite != null;
        }
        if (count != null)
        {
            count.text = any ? have.ToString() : noArrowsText;
            count.color = any ? countColor : noArrowsColor;
        }
    }

    // A child (or grandchild...) with this name, or null.
    static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
