/*
First Date: 3/10/2026
Date: 11/10/2026
Author: Tony
Use by: Put this on a bar under HUDCanvas (HealthBar, HungerBar, StaminaBar). The bar needs a child Image named "Fill".
Unity Editor: Pick which value to show in "Stat". Nothing else to wire: the bar finds the player and
              its Fill child by itself (adding the script to an object named "...Hunger..." / "...Stamina..."
              picks that stat).
              Fill Image settings: Image Type = Filled, Fill Method = Horizontal, a white sprite.
              Testing: in Play mode, change "Health" / "Hunger" / "Stamina" on the Player and watch the bar move.
Purpose: Shows the player's health, hunger or stamina as a bar that fills from 0 to max.
Requirement: StatBar.cs o-> PlayerHealth.cs, PlayerHunger.cs, PlayerStamina.cs
*/

using UnityEngine;
using UnityEngine.UI;   // Image

[DisallowMultipleComponent]
public class StatBar : MonoBehaviour
{
    // The values a bar can show. Add a new one here and in ReadStat to make a new kind of bar.
    public enum Stat { Health, Hunger, Stamina }

    // ===== Fields =====
    [Tooltip("Which player value this bar shows.")]
    [SerializeField] Stat stat = Stat.Health;

    [Tooltip("The Image that grows/shrinks. Found by itself from a child named \"Fill\" if left empty.")]
    [SerializeField] Image fill;

    [Tooltip("Width of the fill sprite in pixels. The bar moves one pixel at a time so pixel art stays sharp. 0 = smooth.")]
    [Min(0)]
    [SerializeField] int pixelWidth = 96;

    [Tooltip("Bar color. Use a white fill sprite so this color shows as it is.")]
    [SerializeField] Color color = Color.red;

    PlayerHealth health;     // found by itself, can be missing (e.g. no player in the scene yet)
    PlayerHunger hunger;
    PlayerStamina stamina;
    float nextSearchTime;    // when to look for the player again if it's missing

    // How often (seconds) to look for the player while it's missing. Searching the whole scene is slow, so not every frame.
    const float SearchInterval = 1f;

    public Stat ShownStat => stat;

    // ===== Unity messages (Unity calls these) =====

    // Unity calls this when the script is first added (or "Reset" in the ⋮ menu): guess everything we can.
    void Reset()
    {
        if (name.Contains("Hunger")) stat = Stat.Hunger;
        if (name.Contains("Stamina")) stat = Stat.Stamina;
        FindFill();
    }

    void Awake()
    {
        FindFill();
        ApplyColor();
    }

    void Update()
    {
        if (ReadStat(out float current, out float max))
        {
            SetValue(current, max);
        }
    }

    // Unity calls this when a value is changed in the Inspector, so the color shows right away.
    void OnValidate()
    {
        ApplyColor();
    }

    // ===== My methods =====

    // Get current / max of the chosen stat from the player. False if the player isn't found (yet).
    bool ReadStat(out float current, out float max)
    {
        current = 0f;
        max = 0f;
        FindPlayer();

        switch (stat)
        {
            case Stat.Health:
                if (health == null) return false;
                current = health.Health;
                max = health.MaxHealth;
                return true;

            case Stat.Hunger:
                if (hunger == null) return false;
                current = hunger.Hunger;
                max = hunger.MaxHunger;
                return true;

            case Stat.Stamina:
                if (stamina == null) return false;
                current = stamina.Stamina;
                max = stamina.MaxStamina;
                return true;
        }
        return false;
    }

    // Look for the player scripts in the scene, at most once per second while they're missing.
    void FindPlayer()
    {
        bool missing = (stat == Stat.Health && health == null)
                    || (stat == Stat.Hunger && hunger == null)
                    || (stat == Stat.Stamina && stamina == null);
        if (!missing || Time.unscaledTime < nextSearchTime) return;

        nextSearchTime = Time.unscaledTime + SearchInterval;
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (hunger == null) hunger = FindFirstObjectByType<PlayerHunger>();
        if (stamina == null) stamina = FindFirstObjectByType<PlayerStamina>();
    }

    // Use the child named "Fill" if nothing was dragged in.
    void FindFill()
    {
        if (fill != null) return;

        Transform child = transform.Find("Fill");
        if (child != null) fill = child.GetComponent<Image>();
    }

    void ApplyColor()
    {
        if (fill != null) fill.color = color;   // tint the white sprite
    }

    // Show current / max on the bar (0 = empty, max = full).
    // Public so a UnityEvent can also drive the bar, but normally Update does it.
    public void SetValue(float current, float max)
    {
        if (fill == null) return;   // nothing to draw on

        // 0 - 1 how full the bar is. Guard against max = 0 (dividing by 0).
        float amount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Snap to whole pixels: e.g. 96 px wide = 96 steps.
        if (pixelWidth > 0)
        {
            amount = Mathf.Round(amount * pixelWidth) / pixelWidth;
        }

        fill.fillAmount = amount;
    }
}
