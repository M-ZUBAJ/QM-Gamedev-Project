/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: The top object of each Bear Trap prefab (Assets/Prefabs/Building/Traps/<Copper|Iron|Gold|Diamond> Bear
        Trap.prefab), made by Tools > Building > Build Traps, which also writes each one's Damage, Hold Seconds, Uses
        and Player Damage. The player builds it with B (the build menu, Traps tab), like a wall.
Unity Editor: Testing: Play, press B, pick Copper Bear Trap (Traps tab) and place it on the ground (left click), then let
              a night monster walk onto it: the jaws snap shut, the monster takes Damage and stands still for Hold
              Seconds ("CAUGHT!"), then the trap is used up. Walk onto it yourself: it hurts you (Player Damage). To see
              the jaws without a monster: select a built trap while playing, ⋮ (top right of this component) > Test:
              Snap Shut, then Test: Open.
Purpose: A jaw trap the player builds to get ready for the night (the Bear Traps: Copper, Iron, Gold, Diamond):
         - a walking monster (Marko's EnemyCombat) whose feet come within Catch Radius of the trap's middle is caught:
           the two jaws snap shut (each turns Closed Angle around the hinge, the trap's long middle line), it takes
           Damage (a kill gives its coins as usual), and EnemyStun holds it still on the trap's middle for Hold
           Seconds: it can't move or attack, so the player can hit it freely;
         - the player stepping on it is hurt too (Player Damage, less with armor) when Hurts Player is on (23/11/2026);
         - flying monsters (FlyingEnemy) fly over it unless Catch Flying is on; a monster that's dying, burning away
           or already held isn't caught; animals never are;
         - after Uses catches it's used up and goes away (1 = one time use: copper and iron; 2: gold and diamond;
           0 = never); before that it stays shut for Rearm Seconds after letting go, then the jaws open again;
         - the build menu's info box shows its numbers (IBuildingInfo: damage, stun time, uses).
         Its Box Collider is a trigger (monsters walk onto it, nothing bumps into it) on the Ignore Raycast layer, so
         the crosshair, attacks and E look through it.
Requirement: BearTrap.cs x-> EnemyStun.cs, EnemyCombat.cs, DamageNumbers.cs, FloatingText.cs, IBuildingInfo.cs, BoxCollider (Unity)
             BearTrap.cs o-> EnemyAnimator.cs, FlyingEnemy.cs, PlayerHealth.cs, NotificationFeed.cs
             BearTrap.cs <-x TrapBuilder.cs
*/

using System.Collections;
using System.Globalization;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class BearTrap : MonoBehaviour, IBuildingInfo
{
    enum State { Open, Holding, Shut, Opening, Broken }

    // ===== Fields =====
    [Header("Catching")]
    [Tooltip("Damage to a monster when the jaws snap shut on it (a slime has about 50 - 100 health).")]
    [Min(0f)]
    [SerializeField] float damage = 20f;

    [Tooltip("Seconds the caught monster stands still: it can't move or attack.")]
    [Min(0f)]
    [SerializeField] float holdSeconds = 4f;

    [Tooltip("How many times it snaps before it's used up and goes away. 1 = one time use (copper, iron), 0 = never.")]
    [Min(0)]
    [SerializeField] int uses = 1;

    [Tooltip("With more than 1 use: seconds after it lets go before the jaws open again (it catches nothing while shut).")]
    [Min(0f)]
    [SerializeField] float rearmSeconds = 6f;

    [Tooltip("How close (meters, along the ground) a monster's feet must come to the trap's middle to be caught.")]
    [Range(0.1f, 1f)]
    [SerializeField] float catchRadius = 0.35f;

    [Tooltip("On: flying monsters (the beholder) are caught too when they come down onto it. Off: they fly over it.")]
    [SerializeField] bool catchFlying = false;

    [Header("The player")]
    [Tooltip("On: the player stepping on it is hurt too (and it snaps, so a one time trap is used up).")]
    [SerializeField] bool hurtsPlayer = true;

    [Tooltip("Damage to the player who steps on it (less with armor).")]
    [Min(0f)]
    [SerializeField] float playerDamage = 20f;

    [Tooltip("Message at the bottom left when the player steps on it. Empty = none.")]
    [SerializeField] string playerText = "Ouch! You stepped on a trap.";

    [Header("Jaws (Tools > Building > Build Traps fills these)")]
    [Tooltip("The front jaw: it turns around the hinge to close.")]
    [SerializeField] Transform frontJaw;

    [Tooltip("The back jaw.")]
    [SerializeField] Transform backJaw;

    [Tooltip("The hinge's direction in the trap's own space: along its long side, through its middle.")]
    [SerializeField] Vector3 hingeAxis = Vector3.right;

    [Tooltip("Which way the front jaw turns to close (+1 or -1; the tool works it out).")]
    [SerializeField] float frontTurn = 1f;

    [Tooltip("Which way the back jaw turns to close (+1 or -1; the tool works it out).")]
    [SerializeField] float backTurn = -1f;

    [Tooltip("Degrees each jaw turns to close (90 = standing straight up, teeth meeting in the middle).")]
    [Range(30f, 100f)]
    [SerializeField] float closedAngle = 88f;

    [Tooltip("Seconds the jaws take to snap shut.")]
    [Range(0.02f, 0.5f)]
    [SerializeField] float snapSeconds = 0.06f;

    [Tooltip("Seconds the jaws take to open again.")]
    [Range(0.1f, 2f)]
    [SerializeField] float openSeconds = 0.6f;

    [Header("Words")]
    [Tooltip("Floats up from a monster it caught.")]
    [SerializeField] string caughtText = "CAUGHT!";

    [SerializeField] Color caughtColor = new Color(1f, 0.6f, 0.15f);

    [Tooltip("Floats up where it went away after its last use. Empty = nothing.")]
    [SerializeField] string brokeText = "Used up";

    const float ShortestShut = 0.8f;   // seconds the shut jaws stay in sight at least (after a kill or snapping on the player)

    State state = State.Open;
    float stateEnds;            // game time (Time.time) when Holding / Shut ends
    float caughtAt;             // game time it last snapped
    int caught;                 // times it snapped so far
    float shut;                 // what the jaws show: 0 = open, 1 = shut
    Quaternion frontOpen = Quaternion.identity;
    Quaternion backOpen = Quaternion.identity;
    Coroutine moving;
    EnemyCombat victim;         // the monster held right now (null after snapping on the player)

    public bool IsOpen => state == State.Open;                                   // ready to catch
    public int UsesLeft => uses <= 0 ? -1 : Mathf.Max(0, uses - caught);         // -1 = never used up

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;   // monsters walk onto it; nothing bumps into it
        if (frontJaw != null) frontOpen = frontJaw.localRotation;
        if (backJaw != null) backOpen = backJaw.localRotation;
    }

    // Every physics step for each thing standing in its box (monsters and the player have the Rigidbody that makes this run).
    void OnTriggerStay(Collider other)
    {
        if (state != State.Open) return;
        EnemyCombat monster = other.GetComponentInParent<EnemyCombat>();
        if (CanCatch(monster))
        {
            Catch(monster);
            return;
        }
        if (!hurtsPlayer) return;
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && !player.IsDead && Near(player.transform.position)) SnapOn(player);
    }

    void Update()
    {
        if (state == State.Holding)
        {
            bool timeUp = Time.time >= stateEnds;
            bool victimGone = victim == null || victim.currentHealth <= 0f;   // dead (or it snapped on the player)
            if (timeUp || (victimGone && Time.time >= caughtAt + ShortestShut)) LetGo();
        }
        else if (state == State.Shut && Time.time >= stateEnds)
        {
            Open();
        }
    }

    [ContextMenu("Test: Snap Shut")]
    void TestSnap()
    {
        if (!Application.isPlaying || state != State.Open) return;
        MoveJaws(1f, snapSeconds);
        state = State.Shut;
        stateEnds = Time.time + rearmSeconds;
    }

    [ContextMenu("Test: Open")]
    void TestOpen()
    {
        if (Application.isPlaying && state != State.Holding && state != State.Broken) Open();
    }

    // ===== My methods =====

    // The build menu's info box (IBuildingInfo): its numbers, from its own settings.
    public string InfoLines()
    {
        CultureInfo n = CultureInfo.InvariantCulture;
        string lines = "Damage " + damage.ToString("0.#", n) + "\nStun " + holdSeconds.ToString("0.#", n) + " s";
        lines += uses == 1 ? "\nOne time use" : uses <= 0 ? "\nNever used up" : "\n" + uses + " uses";
        if (hurtsPlayer) lines += "\nHurts you too: " + playerDamage.ToString("0.#", n);
        if (!catchFlying) lines += "\nFlying monsters fly over it";
        return lines;
    }

    // A living, walking monster that isn't held already, with its feet near the middle.
    bool CanCatch(EnemyCombat monster)
    {
        if (monster == null || monster.currentHealth <= 0f || EnemyStun.IsStunned(monster)) return false;
        EnemyAnimator look = monster.GetComponent<EnemyAnimator>();
        if (look != null && (look.IsDead || look.IsVanishing)) return false;
        if (!catchFlying && monster.GetComponent<FlyingEnemy>() != null) return false;
        return Near(monster.transform.position);
    }

    // Within Catch Radius of the trap's middle, measured along the trap (it may lean on a slope).
    bool Near(Vector3 point)
    {
        return Vector3.ProjectOnPlane(point - transform.position, transform.up).magnitude <= catchRadius;
    }

    // Snaps shut on a monster: the jaws close, it's hurt, and EnemyStun holds it still on the trap's middle.
    void Catch(EnemyCombat monster)
    {
        Snapped();
        victim = monster;
        stateEnds = Time.time + holdSeconds;

        // Where the numbers go, found before the hit: a monster the snap kills switches its colliders off at once.
        Vector3 top = DamageNumbers.TopOf(monster);
        float before = DamageNumbers.HealthOf(monster, out bool creature);
        if (damage > 0f) monster.TakeDamage(damage);
        DamageNumbers.ShowDealt(monster, before, creature, top, false);
        if (monster.currentHealth > 0f && EnemyStun.Stun(monster.gameObject, holdSeconds, transform.position))
            FloatingText.Show(top + Vector3.up * 0.5f, caughtText, caughtColor);
    }

    // Snaps shut on the player's foot: it hurts (armor makes it less), and the trap counts as used.
    void SnapOn(PlayerHealth player)
    {
        Snapped();
        victim = null;
        stateEnds = Time.time + ShortestShut;
        if (playerDamage > 0f) player.TakeDamage(playerDamage);
        if (!string.IsNullOrEmpty(playerText)) NotificationFeed.Show(playerText);
    }

    void Snapped()
    {
        caught++;
        caughtAt = Time.time;
        state = State.Holding;
        MoveJaws(1f, snapSeconds);
    }

    // Its time is up or the monster died: used up after its last use, else shut for Rearm Seconds (EnemyStun frees
    // the monster by itself).
    void LetGo()
    {
        victim = null;
        if (uses > 0 && caught >= uses)
        {
            Break();
            return;
        }
        state = State.Shut;
        stateEnds = Time.time + rearmSeconds;
    }

    // The jaws open again; it can catch once they're open.
    void Open()
    {
        state = State.Opening;
        MoveJaws(0f, openSeconds, () => state = State.Open);
    }

    void Break()
    {
        state = State.Broken;
        if (!string.IsNullOrEmpty(brokeText)) FloatingText.Show(transform.position + Vector3.up * 0.6f, brokeText, Color.white, 2.4f);
        Destroy(gameObject);
    }

    // ----- The jaws -----

    void MoveJaws(float to, float seconds, System.Action done = null)
    {
        if (moving != null) StopCoroutine(moving);
        moving = StartCoroutine(Moving(to, seconds, done));
    }

    // From where they are to "to" (1 = shut): snapping speeds up (like a spring), opening eases in and out.
    IEnumerator Moving(float to, float seconds, System.Action done)
    {
        float from = shut;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = t / seconds;
            ShowJaws(Mathf.Lerp(from, to, to > from ? k * k : Mathf.SmoothStep(0f, 1f, k)));
            yield return null;
        }
        ShowJaws(to);
        moving = null;
        done?.Invoke();
    }

    // Turns each jaw around the hinge: 0 = open (as built), 1 = shut (Closed Angle).
    void ShowJaws(float amount)
    {
        shut = amount;
        Vector3 axis = hingeAxis.sqrMagnitude > 0f ? hingeAxis.normalized : Vector3.right;
        if (frontJaw != null) frontJaw.localRotation = Quaternion.AngleAxis(frontTurn * closedAngle * amount, axis) * frontOpen;
        if (backJaw != null) backJaw.localRotation = Quaternion.AngleAxis(backTurn * closedAngle * amount, axis) * backOpen;
    }
}
