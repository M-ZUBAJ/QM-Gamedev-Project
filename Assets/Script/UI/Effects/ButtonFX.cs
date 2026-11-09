/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Drag this script onto any Button (its Image is the box, its TextMeshPro child named "Label" is the text).
        Tools > UI > Build Main Title Scene puts it on the menu buttons.
Unity Editor: Nothing to connect. Optional: a switched-off TextMeshPro child named "Arrow" (e.g. ">") slides in on hover.
              The Button's own color change (Transition) is turned off: this script does the colors.
              Testing: Play and move the mouse over the button, then click it.
Purpose: Makes a button feel alive:
           Mouse over: grows a little (Hover Scale), the box turns gold and the text dark, a light streak sweeps
                       across it once, the Arrow slides in, and the other buttons next to it dim.
           Click:      squashes while held, then springs back past its size and settles (a bouncy spring),
                       a ripple spreads from the exact point clicked, and a burst of sparks flies out (UISparks).
         A button that can't be clicked (Interactable off) is shown faded and ignores the mouse.
Requirement: ButtonFX.cs x-> UITween.cs, UIFXSprites.cs, UISparks.cs, UISound.cs, Button (Unity)
             ButtonFX.cs <-x TitleMenu.cs, TitleSceneBuilder.cs
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // ===== Fields =====
    [Header("Mouse over")]
    [Range(1f, 1.3f)]
    [Tooltip("How big it grows when the mouse is over it (1.1 = 10% bigger).")]
    [SerializeField] float hoverScale = 1.1f;

    [Tooltip("The box color when the mouse is over it.")]
    [SerializeField] Color hoverColor = new Color(1f, 0.8f, 0.25f);

    [Tooltip("The text color when the mouse is over it (dark, so it reads on the gold box).")]
    [SerializeField] Color hoverTextColor = new Color(0.12f, 0.09f, 0.04f);

    [Tooltip("A light streak sweeps across the button when the mouse comes over it.")]
    [SerializeField] bool shine = true;

    [Tooltip("The other buttons next to this one (same parent) fade a little while the mouse is over this one.")]
    [SerializeField] bool dimOthers = true;

    [Header("Click")]
    [Range(0.7f, 1f)]
    [Tooltip("How small it squashes while held down (0.92 = 8% smaller).")]
    [SerializeField] float pressScale = 0.92f;

    [Tooltip("A ripple spreads from the point clicked.")]
    [SerializeField] bool ripple = true;

    [Range(0, 40)]
    [Tooltip("How many sparks fly out on a click (0 = none).")]
    [SerializeField] int sparks = 14;

    [Header("Sounds (optional)")]
    [Tooltip("Played when the mouse comes over it. Empty = TitleMenu's Hover Sound, or silent.")]
    [SerializeField] AudioClip hoverSound;

    [Tooltip("Played on a click. Empty = TitleMenu's Click Sound, or silent.")]
    [SerializeField] AudioClip clickSound;

    const float Stiffness = 320f;    // the size spring: how hard it's pulled to its size (higher = snappier)
    const float Damping = 16f;       // how fast its wobble dies out (lower = more bounce)
    const float ColorSpeed = 14f;    // how fast the colors / arrow follow (higher = quicker)
    const float DimAlpha = 0.55f;    // how see-through the other buttons get
    const float FadedAlpha = 0.4f;   // how see-through it is while it can't be clicked
    const float ArrowSlide = 22f;    // pixels the Arrow slides in from

    static readonly List<ButtonFX> all = new List<ButtonFX>();   // every ButtonFX switched on right now

    Button button;
    Graphic box;            // the button's picture (gets the gold)
    TMP_Text label;         // the button's text
    RectTransform arrow;    // optional ">" that slides in
    Color boxColor;         // the normal colors, as set in the Inspector
    Color labelColor;
    Vector2 arrowHome;

    float scale = 1f, scaleSpeed;   // the size spring
    float hover, hoverTarget;       // 0 = normal look, 1 = full mouse-over look
    float fade = 1f;                // 1 = fully shown, less = dimmed / can't be clicked (through its colors, so a
                                    // CanvasGroup on the button stays free for other scripts, e.g. CascadeIn)
    bool hovered, pressed, dimmed;
    RectTransform shineRect;        // made the first time it's needed
    RectTransform rippleRect;

    public bool IsHovered => hovered;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;   // this script does the colors
        box = button.targetGraphic != null ? button.targetGraphic : GetComponent<Graphic>();

        Transform labelChild = transform.Find("Label");
        label = labelChild != null ? labelChild.GetComponent<TMP_Text>() : GetComponentInChildren<TMP_Text>(true);
        arrow = transform.Find("Arrow") as RectTransform;

        if (box != null) boxColor = box.color;
        if (label != null) labelColor = label.color;
        if (arrow != null)
        {
            arrowHome = arrow.anchoredPosition;
            arrow.gameObject.SetActive(true);
            SetAlpha(arrow, 0f);
        }

        // The shine and ripple must stay inside the button's box.
        if ((shine || ripple) && GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();

        button.onClick.AddListener(OnClicked);
    }

    void OnEnable()
    {
        all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
        hovered = pressed = dimmed = false;
        hover = hoverTarget = 0f;
        fade = button != null && button.IsInteractable() ? 1f : FadedAlpha;
        scale = 1f;
        scaleSpeed = 0f;
        transform.localScale = Vector3.one;
        ApplyLook();
    }

    void Update()
    {
        // Large frame jumps would make the spring explode, so it never steps more than 1/30 of a second.
        float dt = Mathf.Min(UITween.DeltaTime, 1f / 30f);
        bool clickable = button.IsInteractable();
        if (!clickable) { hovered = pressed = false; hoverTarget = 0f; }

        // The size is a spring: pressing squashes it, letting go makes it bounce back past its size and settle.
        float target = pressed ? pressScale : hovered ? hoverScale : 1f;
        scaleSpeed += (target - scale) * Stiffness * dt;
        scaleSpeed *= Mathf.Exp(-Damping * dt);
        scale += scaleSpeed * dt;
        transform.localScale = new Vector3(scale, scale, 1f);

        // The colors and the arrow follow smoothly.
        float follow = 1f - Mathf.Exp(-ColorSpeed * dt);
        hover = Mathf.Lerp(hover, hoverTarget, follow);
        fade = Mathf.Lerp(fade, (clickable ? 1f : FadedAlpha) * (dimmed ? DimAlpha : 1f), follow);
        ApplyLook();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.IsInteractable()) return;

        hovered = true;
        hoverTarget = 1f;
        if (shine) Shine();
        if (dimOthers) DimSiblings(true);
        UISound.Play(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = pressed = false;
        hoverTarget = 0f;
        if (dimOthers) DimSiblings(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.IsInteractable() || eventData.button != PointerEventData.InputButton.Left) return;

        pressed = true;
        if (ripple) Ripple(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    // ===== My methods =====

    // TitleMenu gives every button its menu sounds, unless the button has its own.
    public void UseSoundsIfEmpty(AudioClip hover, AudioClip click)
    {
        if (hoverSound == null) hoverSound = hover;
        if (clickSound == null) clickSound = click;
    }

    void OnClicked()
    {
        UISparks.Burst((RectTransform)transform, hoverColor, sparks);
        UISound.Play(clickSound);
    }

    void ApplyLook()
    {
        if (box != null) box.color = Faded(Color.Lerp(boxColor, hoverColor, hover));
        if (label != null) label.color = Faded(Color.Lerp(labelColor, hoverTextColor, hover));
        if (arrow != null)
        {
            arrow.anchoredPosition = arrowHome + new Vector2(-ArrowSlide * (1f - hover), 0f);
            SetAlpha(arrow, hover * fade);
        }
    }

    Color Faded(Color c)
    {
        return new Color(c.r, c.g, c.b, c.a * fade);
    }

    // Fades (or brings back) the other ButtonFX buttons that share this button's parent.
    void DimSiblings(bool dim)
    {
        foreach (ButtonFX other in all)
        {
            if (other != this && other.transform.parent == transform.parent) other.dimmed = dim;
        }
        dimmed = false;
    }

    // A soft light streak sweeping from left to right across the button.
    void Shine()
    {
        RectTransform me = (RectTransform)transform;
        if (shineRect == null) shineRect = MakeFxImage("Shine (fx)", UIFXSprites.Shape.Shine, new Color(1f, 1f, 1f, 0.45f));

        float width = me.rect.width, height = me.rect.height;
        shineRect.sizeDelta = new Vector2(height * 0.7f, height * 2.2f);
        shineRect.localRotation = Quaternion.Euler(0f, 0f, -20f);
        shineRect.gameObject.SetActive(true);

        float from = -width / 2f - height, to = width / 2f + height;
        UITween.Run(this, 0.45f, UITween.Ease.OutCubic,
                    t => shineRect.anchoredPosition = new Vector2(Mathf.Lerp(from, to, t), 0f),
                    () => shineRect.gameObject.SetActive(false));
    }

    // A circle of light growing from where the mouse pressed, fading as it spreads.
    void Ripple(PointerEventData eventData)
    {
        RectTransform me = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(me, eventData.position, eventData.pressEventCamera,
                                                                     out Vector2 point)) return;

        if (rippleRect == null) rippleRect = MakeFxImage("Ripple (fx)", UIFXSprites.Shape.SoftDot, Color.white);
        Image image = rippleRect.GetComponent<Image>();
        rippleRect.anchoredPosition = point - me.rect.center;
        rippleRect.gameObject.SetActive(true);

        float size = Mathf.Max(me.rect.width, me.rect.height) * 2.4f;
        UITween.Run(this, 0.5f, UITween.Ease.OutCubic, t =>
        {
            rippleRect.sizeDelta = Vector2.one * size * t;
            image.color = new Color(1f, 1f, 1f, 0.5f * (1f - t));
        }, () => rippleRect.gameObject.SetActive(false));
    }

    // A click-through picture inside the button, under its text (made once, then reused).
    RectTransform MakeFxImage(string objectName, UIFXSprites.Shape shape, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.SetSiblingIndex(0);   // first child = drawn under the text

        Image image = go.AddComponent<Image>();
        image.sprite = UIFXSprites.Get(shape);
        image.color = color;
        image.raycastTarget = false;
        go.SetActive(false);
        return rt;
    }

    static void SetAlpha(RectTransform target, float alpha)
    {
        Graphic g = target.GetComponent<Graphic>();
        if (g != null) g.color = new Color(g.color.r, g.color.g, g.color.b, alpha);
    }
}
