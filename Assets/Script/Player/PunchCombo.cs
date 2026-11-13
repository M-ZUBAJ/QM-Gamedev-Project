/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: The Player (next to Marko's PlayerCombat). Tools > Player > Set Up Player Animations puts it there.
Unity Editor: Testing: Play with nothing in the hand (or wood, food: anything that isn't a tool).
              Left click: left jab, left click again: right jab, then left, right...
              Right click: heavy left hook, right click again: heavy right punch, then left hook, heavy punch...
              A pause (Combo Reset Seconds) starts each line again from its first punch.
Purpose: Bare-handed punch combos. While no tool is in the hand, Marko's PlayerCombat asks it every frame which punch
         to throw ([Tony] lines), and then hits with it as usual:
         - left click = a jab: left, right, left, right...;
         - right click = a heavy punch: left hook (left fist), heavy punch (right fist), left hook... Heavy punches hit
           Heavy Damage times harder and cost Heavy Stamina times the stamina of a jab. While food is in the hand (right
           click eats it) or something is levitating, right click is left alone.
Requirement: PunchCombo.cs x-> PlayerInventory.cs, PlayerAnimator.cs, ToolRules.cs
             PunchCombo.cs o-> ObjectLevitator.cs
             PunchCombo.cs <-x PlayerAnimationSetup.cs, PlayerCombat.cs, PunchEffect.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class PunchCombo : MonoBehaviour
{
    // ===== Fields =====
    [Header("Heavy punches (right click)")]
    [Tooltip("How many times harder a heavy punch hits than a jab (Marko's Hand Damage x this).")]
    [Range(1f, 5f)]
    [SerializeField] float heavyDamage = 2f;

    [Tooltip("How many times a jab's stamina a heavy punch costs (Marko's Attack Stamina Cost x this: 10 x 2 = 20).")]
    [Range(1f, 5f)]
    [SerializeField] float heavyStamina = 2f;

    [Header("Combos")]
    [Tooltip("Seconds without punching before a click starts its line again: left click with the left jab, right click with the left hook. 0 = never: always the other one.")]
    [Min(0f)]
    [SerializeField] float comboResetSeconds = 2f;

    bool nextJabIsRight;       // false = the next left click is the left jab
    bool nextHeavyIsPunch;     // false = the next right click is the left hook
    float lastJabAt = -100f;   // when the last jab was thrown
    float lastHeavyAt = -100f; // when the last heavy punch was thrown
    ObjectLevitator levitator; // right click also levitates (can be missing)

    // The one on the Player (null if none), so the static helpers can read its settings.
    public static PunchCombo Current { get; private set; }

    // True while a click punches: nothing in the selected hotbar slot, or something that isn't a tool.
    public bool ForBareHands => ToolRules.ToolIn(PlayerInventory.Instance != null ? PlayerInventory.Instance.ActiveItem : null) == null;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Current = this;
        levitator = GetComponent<ObjectLevitator>();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        Current = null;
    }

    // ===== My methods =====

    // PlayerCombat calls this every frame it could attack. Returns the punch to throw now, or null.
    // ready = the attack cooldown is over (a click during the cooldown is lost, as before).
    public PlayerAction? Next(bool ready)
    {
        if (!ready) return null;
        if (Input.GetMouseButtonDown(0)) return NextJab();
        if (Input.GetMouseButtonDown(1) && RightIsFree()) return NextHeavy();
        return null;
    }

    // Left, right, left, right... back to the left after a pause.
    PlayerAction NextJab()
    {
        if (comboResetSeconds > 0f && Time.time - lastJabAt > comboResetSeconds) nextJabIsRight = false;
        PlayerAction jab = nextJabIsRight ? PlayerAction.Punch : PlayerAction.PunchLeft;   // Punch = the right jab
        nextJabIsRight = !nextJabIsRight;
        lastJabAt = Time.time;
        return jab;
    }

    // Left hook, heavy punch, left hook... back to the left hook after a pause.
    PlayerAction NextHeavy()
    {
        if (comboResetSeconds > 0f && Time.time - lastHeavyAt > comboResetSeconds) nextHeavyIsPunch = false;
        PlayerAction heavy = nextHeavyIsPunch ? PlayerAction.HeavyPunch : PlayerAction.HeavyHook;
        nextHeavyIsPunch = !nextHeavyIsPunch;
        lastHeavyAt = Time.time;
        return heavy;
    }

    // Right click is free for punching: no food in the hand (it eats) and nothing levitating.
    bool RightIsFree()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        PlayerInventory.Slot slot = inventory != null ? inventory.ActiveItem : null;
        bool food = slot != null && !slot.IsEmpty && slot.data != null && slot.data.IsConsumable;
        bool levitating = levitator != null && levitator.Held != null;
        return !food && !levitating;
    }

    // A heavy punch (right click).
    public static bool IsHeavy(PlayerAction action)
    {
        return action == PlayerAction.HeavyHook || action == PlayerAction.HeavyPunch;
    }

    // Any bare-handed punch: a jab or a heavy one (they all burst wind).
    public static bool IsPunch(PlayerAction action)
    {
        return action == PlayerAction.Punch || action == PlayerAction.PunchLeft || IsHeavy(action);
    }

    // How many times harder this punch hits: Heavy Damage for a heavy one, else 1.
    public static float Power(PlayerAction action)
    {
        return IsHeavy(action) && Current != null ? Current.heavyDamage : 1f;
    }

    // How many times a jab's stamina this punch costs: Heavy Stamina for a heavy one, else 1.
    public static float StaminaScale(PlayerAction action)
    {
        return IsHeavy(action) && Current != null ? Current.heavyStamina : 1f;
    }
}
