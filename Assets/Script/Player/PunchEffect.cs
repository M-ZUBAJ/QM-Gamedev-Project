/*
First Date: 12/11/2026
Date: 13/11/2026
Author: Tony
Use by: The Player. Tools > Effects > Build Punch Wind Burst puts it there and fills it in.
        By hand: drag this script onto the Player, then drag Assets/Prefabs/Effects/PunchWindBurst.prefab into Burst Prefab.
Unity Editor: Testing: Play, then hit an enemy, a tree, a stone or an animal: with an empty hand (a punch) or with any
              tool (sword, axe, pickaxe). Or click the ⋮ on this component > "Test: Burst In Front".
              To change the look, open the prefab and change its parts (Puff, Streaks, Ring, Dust):
              see Docs/Punch Effect Setup.md. Untick this component to switch the burst off.
Purpose: A grey burst of wind (dust clouds, wind lines, a shock ring and a few specks) where a punch or a tool hits
         something with health, and the timing of every swing: a hit lands when the fist or tool does, not at the
         click (Impact Delay for the right jab, Left Jab / Left Hook / Heavy Punch Delay for PunchCombo's other punches,
         Swing Delay for a sword or axe, Mine Delay for a pickaxe). Every hit bursts wind: a heavy punch a bigger burst,
         a tool hit Tool Burst Size.
         Marko's PlayerCombat calls PunchEffect.Hit(target, hit, hurt, swing) when an attack hits an enemy
         (EnemyCombat) or a tree, stone or animal (ResourceNode). When the swing lands, the target is hurt (a tree or
         stone may break right then, not before) and the wind bursts on the spot that was hit, even if the target moved
         (a running deer). The burst removes itself when its particles are gone.
         Any script can make a burst: PunchEffect.Play(where, whichWayOut);
Requirement: PunchEffect.cs x-> PlayerAnimator.cs, PunchCombo.cs
             PunchEffect.cs <-x PlayerAnimationSetup.cs, PlayerCombat.cs, PunchEffectBuilder.cs, TierToolBuilder.cs
*/

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PunchEffect : MonoBehaviour
{
    // ===== Fields =====
    [Header("Burst")]
    [Tooltip("The grey wind burst: Assets/Prefabs/Effects/PunchWindBurst.prefab (Tools > Effects > Build Punch Wind Burst). Empty = no burst.")]
    [SerializeField] GameObject burstPrefab;

    [Tooltip("How big the burst is. 1 = as built (about 1.5 m across at its widest), 2 = twice as big.")]
    [Range(0.25f, 3f)]
    [SerializeField] float size = 1f;

    [Header("When and where")]
    [Tooltip("Seconds from the click to the burst, so it pops when the fist is fully out. The tool measures it from the first-person punch animation. 0 = at the click.")]
    [Range(0f, 1f)]
    [SerializeField] float impactDelay = 0.25f;

    [Tooltip("How far in front of the hit spot the burst starts (meters), so the enemy or tree doesn't cover half of it.")]
    [Range(0f, 0.5f)]
    [SerializeField] float pushOut = 0.15f;

    [Header("Punch combo (PunchCombo)")]
    [Tooltip("Seconds from the click until the left jab lands. Tools > Player > Set Up Player Animations measures it.")]
    [Range(0f, 1f)]
    [SerializeField] float leftJabDelay = 0.25f;

    [Tooltip("Seconds from the right click until the heavy left hook lands. Set Up Player Animations measures it.")]
    [Range(0f, 1.5f)]
    [SerializeField] float hookDelay = 0.4f;

    [Tooltip("Seconds from the right click until the heavy right punch lands. Set Up Player Animations measures it.")]
    [Range(0f, 1.5f)]
    [SerializeField] float heavyPunchDelay = 0.4f;

    [Tooltip("How much bigger a heavy punch's burst is (1.5 = one and a half times).")]
    [Range(1f, 3f)]
    [SerializeField] float heavyBurstSize = 1.5f;

    [Header("Tool swings")]
    [Tooltip("How big the burst is when a tool hits (sword, axe, pickaxe): 1 = like a jab, 0 = no burst for tools.")]
    [Range(0f, 3f)]
    [SerializeField] float toolBurstSize = 1f;

    [Tooltip("Seconds from the click until a sword or axe swing (Attack) lands. Tools > Items > Build Tier Tools measures it from the animation.")]
    [Range(0f, 1f)]
    [SerializeField] float swingDelay = 0.3f;

    [Tooltip("Seconds from the click until a pickaxe swing (Mine) lands. Tools > Items > Build Tier Tools measures it from the animation.")]
    [Range(0f, 1f)]
    [SerializeField] float mineDelay = 0.35f;

    float lifetime;          // seconds until a burst's last particle is gone (worked out from the prefab)
    GameObject lifetimeOf;   // the prefab that lifetime belongs to (worked out again if Burst Prefab changes)

    // The one on the Player (null if none), so Play works without searching.
    public static PunchEffect Current { get; private set; }

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Current = this;
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

    // A punch that lands after Impact Delay. Then it hurts the target (hurt, e.g. () => enemy.TakeDamage(10)) and the
    // wind bursts on the spot that was hit, even if the target moved or turned meanwhile (a running deer, a walking
    // slime). So a tree or stone this punch breaks stays until the fist lands, and breaks with the burst.
    // Without a PunchEffect on the Player (or when it's switched off) hurt happens at once and nothing bursts.
    public static void Punch(Component target, RaycastHit hit, System.Action hurt)
    {
        Hit(target, hit, hurt, PlayerAction.Punch);
    }

    // Any swing: it lands after that swing's delay (Punch = Impact Delay, Attack = Swing Delay, Mine = Mine Delay), then
    // hurts the target and bursts wind (a heavy punch bigger, a tool Tool Burst Size). Without a PunchEffect on the
    // Player, hurt happens at once.
    public static void Hit(Component target, RaycastHit hit, System.Action hurt, PlayerAction swing)
    {
        if (Current != null && Current.isActiveAndEnabled)
        {
            float burst = PunchCombo.IsHeavy(swing) ? Current.heavyBurstSize : PunchCombo.IsPunch(swing) ? 1f : Current.toolBurstSize;   // 0 = no burst
            Current.StartCoroutine(Current.Land(target, hit.point, hit.normal, hurt, Current.DelayFor(swing), burst));
        }
        else
        {
            hurt?.Invoke();
        }
    }

    // A burst at this point. normal = the way out of the surface that was hit (RaycastHit.normal), so the burst
    // flies out of it, not into it. Does nothing if the Player has no PunchEffect (or it's switched off).
    public static void Play(Vector3 point, Vector3 normal)
    {
        if (Current != null) Current.Burst(point, normal);
    }

    public void Burst(Vector3 point, Vector3 normal)
    {
        if (burstPrefab == null || !isActiveAndEnabled) return;

        if (impactDelay > 0f) StartCoroutine(BurstLater(point, normal));
        else Spawn(point, normal);
    }

    IEnumerator BurstLater(Vector3 point, Vector3 normal)
    {
        yield return new WaitForSeconds(impactDelay);   // the fist is on its way
        Spawn(point, normal);
    }

    // How long a swing takes to land.
    float DelayFor(PlayerAction swing)
    {
        switch (swing)
        {
            case PlayerAction.Attack: return swingDelay;
            case PlayerAction.Mine: return mineDelay;
            case PlayerAction.PunchLeft: return leftJabDelay;
            case PlayerAction.HeavyHook: return hookDelay;
            case PlayerAction.HeavyPunch: return heavyPunchDelay;
            default: return impactDelay;
        }
    }

    // burst = the size of the wind burst (1 = normal, 0 = none: a tool swing).
    IEnumerator Land(Component target, Vector3 point, Vector3 normal, System.Action hurt, float delay, float burst)
    {
        // Where on the target it was hit, in the target's own space, so the burst stays on it if it moves or turns.
        Transform body = target != null ? target.transform : null;
        Vector3 spot = body != null ? body.InverseTransformPoint(point) : point;
        Vector3 facing = body != null ? body.InverseTransformDirection(normal) : normal;

        if (delay > 0f) yield return new WaitForSeconds(delay);   // the fist or tool is on its way

        if (body != null)   // still there (once it's destroyed, Unity treats it as null)
        {
            point = body.TransformPoint(spot);
            normal = body.TransformDirection(facing);
        }
        if (target != null) hurt?.Invoke();   // it lands: now it's hurt (a tree or stone may break right here)
        if (burst > 0f) Spawn(point, normal, burst);   // a punch: the wind bursts where it landed
    }

    void Spawn(Vector3 point, Vector3 normal, float sizeScale = 1f)
    {
        if (burstPrefab == null) return;
        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;   // no direction given: burst upward
        normal.Normalize();

        if (lifetimeOf != burstPrefab)
        {
            lifetime = Lifetime(burstPrefab);
            lifetimeOf = burstPrefab;
        }

        // The prefab's front (blue arrow) points out of the surface, so its wind flies out toward the puncher.
        GameObject burst = Instantiate(burstPrefab, point + normal * pushOut, Quaternion.LookRotation(normal));
        burst.transform.localScale = Vector3.one * (size * sizeScale);   // its parts scale with it (Scaling Mode: Hierarchy)
        Destroy(burst, lifetime);
    }

    // The longest any part of the burst lasts: its start delay + how long it emits + how long its particles live.
    static float Lifetime(GameObject prefab)
    {
        float longest = 0.5f;
        if (prefab == null) return longest;

        foreach (ParticleSystem part in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!part.emission.enabled) continue;   // makes no particles (e.g. the top, which only groups the parts)
            ParticleSystem.MainModule main = part.main;
            float life = Mathf.Max(main.startLifetime.constantMax, main.startLifetime.curveMultiplier);
            longest = Mathf.Max(longest, main.startDelay.constantMax + main.duration + life);
        }
        return Mathf.Min(longest + 0.1f, 10f);   // a little spare time; never kept longer than 10 s
    }

    // Right-click the component title in the Inspector (or click its ⋮) while playing.
    [ContextMenu("Test: Burst In Front")]
    void TestBurst()
    {
        Camera cam = Camera.main;
        if (!Application.isPlaying || cam == null || burstPrefab == null) return;

        Transform view = cam.transform;
        Spawn(view.position + view.forward * 1.5f, -view.forward);   // 1.5 m in front, facing the camera, no delay
    }
}
