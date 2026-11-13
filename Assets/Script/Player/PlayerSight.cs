/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: The Player (drag it on). Tools > World > Set Up World Spawning also puts it there.
Unity Editor: Select the Player to see its sight in the Scene view: white = Sight Range (around the camera), yellow =
              Notice Radius. Testing: Play, then ⋮ (top right of this component) > Test: Sight +40 m For 20 s. The
              Console shows the new range, and new deer appear farther away until it ends.
Purpose: What the player can see (line of sight). Something counts as seen when it is:
         - closer than Sight Range (perks and buffs add to it),
         - on the screen (a little past the edges counts too, so nothing pops up while you turn),
         - and not hidden behind a hill, rock, tree trunk or wall.
         Anything closer than Notice Radius always counts as seen (you'd hear it right next to you).
         Spawners ask CanSee, so things appear only where the player isn't looking, like Minecraft spawning animals
         out of view (AnimalSpawner does this for the deer). A sight perk or buff calls AddRange: everything that asks
         follows the new range at once.
Requirement: PlayerSight.cs o-> Camera (Unity), PlayerHealth.cs
             PlayerSight.cs <-x AnimalSpawner.cs, WorldSpawnSetup.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSight : MonoBehaviour
{
    // ===== Fields =====
    [Header("How far")]
    [Tooltip("How far the player can see things, in meters (the map is 1000 m wide). Sight perks and buffs add to it.")]
    [Min(0f)]
    [SerializeField] float sightRange = 80f;

    [Tooltip("Anything closer than this (meters) always counts as seen, even behind the player or behind a wall: you'd hear a deer appear right next to you.")]
    [Min(0f)]
    [SerializeField] float noticeRadius = 20f;

    [Header("What hides things")]
    [Tooltip("On = only what's on the screen can be seen, so things behind you are hidden. Off = everything in range, all around.")]
    [SerializeField] bool onlyOnScreen = true;

    [Tooltip("How far past the screen's edges still counts as on screen (0.15 = 15% of the screen), so nothing pops up at the edge while you turn.")]
    [Range(0f, 0.5f)]
    [SerializeField] float screenMargin = 0.15f;

    [Tooltip("On = hills, rocks, tree trunks and walls hide what's behind them. (Leaves have no collider, so they don't.)")]
    [SerializeField] bool wallsBlockSight = true;

    [Tooltip("The layers that can hide things. The player itself and triggers never do.")]
    [SerializeField] LayerMask blockingLayers = ~((1 << 2) | (1 << 30) | (1 << 31));   // not Ignore Raycast, the first-person hands (30), icons (31)

    [Tooltip("The camera that is the player's eyes. Empty = the main camera.")]
    [SerializeField] Camera eyes;

    [Header("Perks and buffs")]
    [Tooltip("On = sight perks and buffs end when the player dies, like the other perks.")]
    [SerializeField] bool resetOnDeath = true;

    float perkRange;                                     // meters added by perks (AddRange without seconds)
    readonly List<Vector2> buffs = new List<Vector2>();  // timed buffs: x = meters, y = the Time.time it ends
    PlayerHealth health;                                 // can be missing (then nothing resets on death)
    Rigidbody body;                                      // the player's own body: its colliders never hide anything
    static readonly RaycastHit[] hits = new RaycastHit[16];

    // The one on the Player (null if none), for spawners and perks.
    public static PlayerSight Current { get; private set; }

    // How far the player sees right now, in meters: Sight Range + perks + buffs.
    public float Range => Mathf.Max(0f, sightRange + perkRange + BuffRange());
    public float BaseRange => sightRange;
    public float NoticeRadius => noticeRadius;

    // Fired with the new Range whenever a perk or buff changes it (e.g. for a HUD).
    public event System.Action<float> RangeChanged;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        Current = this;
        body = GetComponent<Rigidbody>();
        health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (health != null) health.Died += OnDied;
    }

    void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
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

    // Takes away buffs whose time is up.
    void Update()
    {
        if (buffs.RemoveAll(b => Time.time >= b.y) > 0) Changed();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(EyePosition(), Range);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, noticeRadius);
    }

    [ContextMenu("Test: Sight +40 m For 20 s")]
    void TestBuff()
    {
        if (Application.isPlaying) AddRange(40f, 20f);
        else Debug.Log("Press Play first.");
    }

    // ===== My methods =====

    // A sight perk: adds meters to the range until the player dies (if Reset On Death) or ResetBonus is called.
    // e.g. PlayerSight.Current.AddRange(20f);
    public void AddRange(float meters)
    {
        perkRange += meters;
        Changed();
    }

    // A sight buff: adds meters for some seconds, e.g. a potion: PlayerSight.Current.AddRange(40f, 30f);
    public void AddRange(float meters, float seconds)
    {
        if (seconds <= 0f)
        {
            AddRange(meters);
            return;
        }
        buffs.Add(new Vector2(meters, Time.time + seconds));
        Changed();
    }

    // Back to the plain Sight Range: no perks, no buffs.
    public void ResetBonus()
    {
        if (perkRange == 0f && buffs.Count == 0) return;
        perkRange = 0f;
        buffs.Clear();
        Changed();
    }

    // True if the player can see this point.
    // ignore = the thing at that point, so its own collider doesn't count as a wall in front of it.
    public bool CanSee(Vector3 point, Transform ignore = null)
    {
        return CanSee(point, 0f, ignore);
    }

    // True if the player can see any part of something standing at 'feet', 'height' meters tall: it checks a point low
    // on its body and one near its top (a deer's head can stick out over a hill).
    public bool CanSee(Vector3 feet, float height, Transform ignore = null)
    {
        Vector3 flat = feet - transform.position;
        flat.y = 0f;
        if (flat.magnitude < noticeRadius) return true;

        Vector3 eye = EyePosition();
        if (height <= 0f) return Sees(eye, feet, ignore);
        return Sees(eye, feet + Vector3.up * height * 0.25f, ignore) || Sees(eye, feet + Vector3.up * height * 0.9f, ignore);
    }

    // One point: in range, on screen, nothing in the way.
    bool Sees(Vector3 eye, Vector3 point, Transform ignore)
    {
        Vector3 toPoint = point - eye;
        float distance = toPoint.magnitude;
        if (distance > Range) return false;

        Camera cam = EyeCamera();
        if (onlyOnScreen && cam != null)
        {
            Vector3 v = cam.WorldToViewportPoint(point);   // (0, 0) = bottom left of the screen, (1, 1) = top right
            bool onScreen = v.z > 0f && v.x > -screenMargin && v.x < 1f + screenMargin && v.y > -screenMargin && v.y < 1f + screenMargin;
            if (!onScreen) return false;
        }

        if (!wallsBlockSight || distance < 0.05f) return true;
        int count = Physics.RaycastNonAlloc(eye, toPoint / distance, hits, distance - 0.05f, blockingLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (body != null && c.attachedRigidbody == body) continue;          // the player's own ball
            if (c.transform.IsChildOf(transform)) continue;                     // or anything on it
            if (ignore != null && c.transform.IsChildOf(ignore)) continue;      // the thing we're looking at
            return false;   // a hill, rock, trunk or wall is in the way
        }
        return true;
    }

    Camera EyeCamera()
    {
        return eyes != null ? eyes : Camera.main;
    }

    // Where the player looks from: the camera (also in third person: you see what's on your screen).
    Vector3 EyePosition()
    {
        Camera cam = EyeCamera();
        return cam != null ? cam.transform.position : transform.position + Vector3.up * 0.5f;
    }

    float BuffRange()
    {
        float meters = 0f;
        foreach (Vector2 buff in buffs) meters += buff.x;
        return meters;
    }

    void OnDied()
    {
        if (resetOnDeath) ResetBonus();
    }

    void Changed()
    {
        Debug.Log("Player sight range: " + Range.ToString("0") + " m");
        RangeChanged?.Invoke(Range);
    }
}
