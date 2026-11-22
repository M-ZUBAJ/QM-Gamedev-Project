/*
First Date: 17/11/2026
Date: 17/11/2026
Author: Turki
Use by: Put this on the BuffBox object inside HUDCanvas (it needs a CanvasGroup next to it).
Unity Editor: Drag the child objects into the slots: Icon (Image), Name Text, Time Text (TextMeshPro) and
              Timer Fill (an Image set to Filled / Horizontal). "Food Buff" can stay empty: it finds the Player's
              FoodBuff by itself.
Purpose: Shows the active food buff on the HUD: the food's icon, its name and strength (e.g. "Stew +5% Max HP"),
         the time left (e.g. 1:52) and a bar that drains as time runs out. The box fades in when a buff starts
         and fades out when it ends. It only reads from FoodBuff and never changes it.
Requirement: BuffDisplay.cs o-> FoodBuff.cs, ItemData.cs
*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BuffDisplay : MonoBehaviour
{
    // ===== Fields =====
    [Header("Source")]
    [Tooltip("The Player's FoodBuff. Leave empty to find it automatically.")]
    [SerializeField] FoodBuff foodBuff;

    [Header("Parts of the box")]
    [SerializeField] Image icon;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text timeText;
    [Tooltip("Must be an Image with Image Type = Filled and Fill Method = Horizontal.")]
    [SerializeField] Image timerFill;

    [Header("Look")]
    [Tooltip("How fast the box fades in and out (higher = faster).")]
    [Min(0.1f)]
    [SerializeField] float fadeSpeed = 4f;

    CanvasGroup group;
    ItemData shownFood;   // the food the icon and name are showing now (so we only redraw them when it changes)

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;              // hidden until a buff starts
        group.interactable = false;    // it is only a display: it must never catch clicks
        group.blocksRaycasts = false;
    }

    void Start()
    {
        // Find the Player's FoodBuff by itself, so nothing has to be dragged in.
        if (foodBuff == null) foodBuff = FindFirstObjectByType<FoodBuff>();
    }

    void Update()
    {
        bool show = foodBuff != null && foodBuff.IsActive;

        // MoveTowards slides alpha 0 <-> 1 smoothly instead of switching at once.
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, fadeSpeed * Time.deltaTime);
        if (!show) return;

        // A new food started: redraw the icon and the name.
        if (foodBuff.ActiveFood != shownFood)
        {
            shownFood = foodBuff.ActiveFood;
            ShowFood(shownFood);
        }

        // Time left as m:ss. CeilToInt rounds up, so it shows 0:01 until the very end, never 0:00 while still active.
        int seconds = Mathf.CeilToInt(foodBuff.Remaining);
        timeText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");

        // 1 = full bar, 0 = empty.
        if (timerFill != null && foodBuff.Duration > 0f)
            timerFill.fillAmount = foodBuff.Remaining / foodBuff.Duration;
    }

    // ===== My methods =====

    void ShowFood(ItemData food)
    {
        if (food == null) return;

        Sprite sprite = food.GetIcon();
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;   // no icon = hide the picture instead of showing a white square
        }

        int percent = Mathf.RoundToInt(foodBuff.MaxHealthPercent * 100f);   // 0.05 -> 5
        if (nameText != null) nameText.text = food.DisplayName + " +" + percent + "% Max HP";
    }
}