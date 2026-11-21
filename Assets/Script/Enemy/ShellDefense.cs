/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: The turtle enemy's top object (Assets/Prefabs/Enemy/TurtleShellEnemy), next to Marko's EnemyCombat, EnemyMovement
        and EnemyAttack and Tony's EnemyAnimator. Tools > Enemies > Set Up Night Monsters puts it there and fills it in.
Unity Editor: Fight Seconds / Shell Seconds: how long it fights near the player, then how long it hides. Hide When Hurt
              Chance: the chance it hides at once when it's hurt. Shell Damage: what a melee hit on the shell does to the
              player. Deflect Effect: the sparks (Assets/Prefabs/Effects/ShellDeflect.prefab).
              Testing: Play on night 2 or later (or the EnemySpawner's ⋮ > Test Spawn Enemy until a turtle comes), select
              a turtle, ⋮ > Test: Hide In Shell, then punch it: sparks, "BLOCKED!", its shell shakes, your hands bounce
              back and you lose 15 health (less with armor). A fireball still hurts it.
Purpose: The turtle's shell phase. It fights (walks and attacks) for Fight Seconds while the player is near (Fight
         Range), then pulls into its shell (the pack's Defend animation) for Shell Seconds: it stands still and doesn't
         attack. Being hurt while fighting can make it hide at once (Hide When Hurt Chance, 1 in 3).
         While it's in its shell, melee hits (punches, swords, axes, pickaxes) bounce off: no damage to it, and the
         player takes Shell Damage instead (the shell's damage, not their weapon's; their armor counts). So the player
         sees it didn't hit: sparks where the hit landed, "BLOCKED!" over the turtle (FloatingText), its shell-hit
         animation (DefendGetHit), and in first person the hands bounce back (FirstPersonHands.Recoil).
         PunchEffect asks Blocks and calls Deflect when a melee swing lands. The fireball isn't melee (a projectile): it
         still hurts the turtle in its shell.
Requirement: ShellDefense.cs x-> EnemyCombat.cs
             ShellDefense.cs o-> EnemyMovement.cs, EnemyAttack.cs, EnemyAnimator.cs, PlayerHealth.cs, FloatingText.cs, FirstPersonHands.cs, Rigidbody (Unity)
             ShellDefense.cs <-x EnemyBuilder.cs, NightMonsterBuilder.cs
             ShellDefense.cs <-o PunchEffect.cs
*/

using UnityEngine;

[RequireComponent(typeof(EnemyCombat))]
[DisallowMultipleComponent]
public class ShellDefense : MonoBehaviour
{
    // ===== Fields =====
    [Header("When it hides")]
    [Tooltip("Seconds it fights (walks and attacks) near the player before it pulls into its shell.")]
    [Range(1f, 30f)]
    [SerializeField] float fightSeconds = 6f;

    [Tooltip("Up to this many seconds more, at random, so a group of turtles doesn't hide all at once.")]
    [Range(0f, 5f)]
    [SerializeField] float fightJitter = 1.5f;

    [Tooltip("Seconds it stays in its shell.")]
    [Range(0.5f, 15f)]
    [SerializeField] float shellSeconds = 3f;

    [Tooltip("The chance it hides at once when it's hurt while fighting (0.33 = 1 in 3).")]
    [Range(0f, 1f)]
    [SerializeField] float hideWhenHurtChance = 0.33f;

    [Tooltip("It only counts its fighting time while the player is this close (meters), so it doesn't hide on its way to them.")]
    [Range(2f, 40f)]
    [SerializeField] float fightRange = 10f;

    [Header("Hitting the shell")]
    [Tooltip("What a melee hit on the shell does to the player: the shell's damage, not their weapon's. Their armor counts.")]
    [Range(0f, 100f)]
    [SerializeField] float shellDamage = 15f;

    [Tooltip("The sparks where a hit bounces off: Assets/Prefabs/Effects/ShellDeflect.prefab (Tools > Enemies > Set Up " +
             "Night Monsters). Empty = no sparks.")]
    [SerializeField] GameObject deflectEffect;

    [Tooltip("The word that pops up over the turtle when a hit bounces off.")]
    [SerializeField] string blockedText = "BLOCKED!";

    [Tooltip("Its color.")]
    [SerializeField] Color blockedColor = new Color(1f, 0.85f, 0.3f);

    const float EffectLife = 2f;   // seconds before a spark burst is removed (its sparks last well under a second)

    EnemyCombat combat;        // Marko's health
    EnemyMovement movement;    // Marko's: switched off while it's in its shell (can be missing)
    EnemyAttack attack;        // Marko's: the same
    EnemyAnimator shown;       // Tony's: the Defend / DefendGetHit animations (can be missing)
    Rigidbody body;            // stopped while it's in its shell (can be missing)
    PlayerHealth playerHealth; // who the shell hurts
    float timer;               // fighting: seconds of fighting left; in the shell: seconds left inside
    float lastHealth;          // to see when it was hurt
    bool movedBefore;          // what was switched on before it hid, to switch it back on
    bool attackedBefore;

    // True while it's in its shell (melee hits bounce off).
    public bool InShell { get; private set; }

    // Dying or burning away at dawn: EnemyAnimator has stopped everything, the shell doesn't matter any more.
    bool Gone => combat == null || combat.currentHealth <= 0f || (shown != null && (shown.IsDead || shown.IsVanishing));

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        combat = GetComponent<EnemyCombat>();
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
        shown = GetComponent<EnemyAnimator>();
        body = GetComponent<Rigidbody>();
        timer = FightTime();
    }

    // In Start: by now the EnemySpawner has set its health.
    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        lastHealth = combat.currentHealth;
    }

    void Update()
    {
        if (Gone)
        {
            InShell = false;   // EnemyAnimator stopped it for good; nothing is switched back on
            return;
        }

        bool hurt = combat.currentHealth < lastHealth;
        lastHealth = combat.currentHealth;

        if (InShell)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) ComeOut();
            return;
        }

        if (hurt && Random.value < hideWhenHurtChance)
        {
            Hide();
            return;
        }
        if (PlayerNear()) timer -= Time.deltaTime;
        if (timer <= 0f) Hide();
    }

    // In its shell it stays where it is: Marko's movement is off, but the body would keep sliding.
    void FixedUpdate()
    {
        if (!InShell || body == null || body.isKinematic) return;
        body.velocity = new Vector3(0f, body.velocity.y, 0f);   // it can still fall
    }

    // ===== My methods =====

    // Does a melee hit on this target bounce off? (PunchEffect asks when a swing lands.) True while its turtle is in
    // its shell.
    public static bool Blocks(Component target)
    {
        ShellDefense shell = target != null ? target.GetComponentInParent<ShellDefense>() : null;
        return shell != null && shell.isActiveAndEnabled && shell.InShell && !shell.Gone;
    }

    // A melee hit bounced off the shell at "point" (normal = out of the shell, toward the player): sparks, "BLOCKED!",
    // the shell's hit animation, the player's hands bouncing back, and Shell Damage to the player.
    public static void Deflect(Component target, Vector3 point, Vector3 normal)
    {
        ShellDefense shell = target != null ? target.GetComponentInParent<ShellDefense>() : null;
        if (shell != null) shell.BounceOff(point, normal);
    }

    void BounceOff(Vector3 point, Vector3 normal)
    {
        if (shown != null) shown.PlayShellHit();

        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
        if (deflectEffect != null)
        {
            GameObject sparks = Instantiate(deflectEffect, point + normal * 0.05f, Quaternion.LookRotation(normal));
            Destroy(sparks, EffectLife);
        }
        FloatingText.Show(point + Vector3.up * 0.6f, blockedText, blockedColor);
        FirstPersonHands.Recoil();

        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsDead && shellDamage > 0f) playerHealth.TakeDamage(shellDamage);
    }

    // Pulls into its shell: stops walking and attacking, and plays Defend.
    void Hide()
    {
        InShell = true;
        timer = shellSeconds;
        movedBefore = movement != null && movement.enabled;
        attackedBefore = attack != null && attack.enabled;
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        if (body != null && !body.isKinematic) body.velocity = new Vector3(0f, body.velocity.y, 0f);
        if (shown != null) shown.SetShell(true);
    }

    // Comes out of its shell and fights again.
    void ComeOut()
    {
        InShell = false;
        timer = FightTime();
        if (movement != null && movedBefore) movement.enabled = true;
        if (attack != null && attackedBefore) attack.enabled = true;
        if (shown != null) shown.SetShell(false);
    }

    float FightTime() => fightSeconds + Random.Range(0f, fightJitter);

    // Is the player (the one Marko's EnemyMovement goes after) within Fight Range?
    bool PlayerNear()
    {
        Transform target = movement != null ? movement.target : null;
        if (target == null) return false;
        Vector3 apart = target.position - transform.position;
        apart.y = 0f;
        return apart.sqrMagnitude <= fightRange * fightRange;
    }

    // Click the component's ⋮ menu in the Inspector while playing.
    [ContextMenu("Test: Hide In Shell")]
    void TestHide()
    {
        if (!Application.isPlaying) Debug.Log("Press Play first.");
        else if (!InShell && !Gone) Hide();
    }
}
