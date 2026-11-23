/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: A human enemy's top object, next to its Rigidbody, capsule, Marko's EnemyCombat, EnemyAnimator, HumanGear and
        HumanLoot. Tools > Enemies > Build Human Enemies puts it there.
Unity Editor: Kind = which human type it is. The numbers can be changed while playing.
              Testing: Play in the new world (from the title screen: NEW GAME > Map: New world). Humans appear out of
              sight around you in the daytime. Select one and read Doing: it walks (as fast as you), goes to a tree, a
              rock or a berry bush and chops, mines or picks it for real (with its axe or pickaxe, or punching it when
              it has no tool for it): the tree falls, the rock breaks, the bush is gone, and what it gave goes into its
              bag (HumanLoot > Carried). Hit it: it fights you like another player: it faces you, sprints at you while
              its stamina lasts, steps in as it swings (backing off doesn't dodge it), circles you between attacks and
              backpedals and strafes with the right animations. Walk more than Give Up Range (10 m) away and it stops
              following, but it remembers: come within Notice Range (8 m) again and it attacks again. Shoot it with an
              arrow from afar: it comes after you for Chase After Hit (8 s). One that walked into a cave walks out
              through the entrance (the cave's CaveTrail).
Purpose: How a human enemy behaves, the first type (Docs/Human Enemies Plan.md): a survivor like the player that never
         attacks first:
           - it walks around its home as fast as the player walks (the Player's PlayerController > Speed), resting now
             and then;
           - it goes to a nearby tree, rock or berry bush (rocks about as often as trees: Rock Share) and breaks it
             like the player does: each swing takes health off, a berry bush is picked at once; it gets what that
             gives (Wood, Spruce Wood, Stone, ore, berries, an apple now and then: Turki's ResourceNode.TakeDamageFor
             puts it in its bag, not in the player's inventory). With a tool for the job (HumanGear) it swings its axe
             at trees and its pickaxe at rocks, harder by their tier, each swing landing when the tool comes down in
             the work animation (Swing At); without one it puts its weapon away and punches (the fist attack
             animation, each punch landing when the fist does). Berries it picks by hand (a bend-and-pick animation).
             The player's tool rules count: only a pickaxe mines ore, the snow's spruce needs an iron axe (a war axe
             counts as an axe of its tier). Each hit bursts the same grey wind as the player's (PunchEffect);
           - when a tree or rock is gone it goes on to the next one (or rests), until its bag is full;
           - caves: a cave is a ring of rocks with one entrance, so it follows the cave's invisible trail (CaveTrail):
             out through the entrance when its goal is outside, in through it to reach an ore inside; and when it's
             stuck inside a cave it takes the way out;
           - when the player hurts it, it fights back (When Hit = Fight Back), like another player would: it keeps
             its eyes on the player, runs at them and sprints while its stamina lasts (like the player's: Sprint Drain,
             Stamina Regen), starts a swing when the player will be within reach as the blow lands (it follows a
             player walking away) and keeps stepping in while it winds up, so backing off doesn't dodge it; between
             attacks it circles the player at reach (Strafes). Its legs move by direction (MoveX / MoveZ): it
             strafes and backpedals with the right animation. It flinches at most once in Flinch Every seconds (fast
             hits can't keep it stunned), and never in the middle of a swing. Each blow hurts the player only if
             still within reach and in front: the player's armor and Last Stand count (PlayerHealth), the Thorns perk
             hits it back. How hard it hits is its weapon's (HumanGear > Weapons > Damage, a better tier harder; Fist
             Damage with bare hands), and its own armor takes its share off the player's hits (HumanGear.Reduce);
           - it knows who hurt it, however far away (an arrow from afar too): after each hit it chases the player for
             Chase After Hit seconds whatever the distance; after that, farther than Give Up Range it stops following
             and goes back to its life, but it remembers (Holds Grudge): when the player comes within Notice Range
             again, it attacks again;
           - When Hit = Run Away: it runs off instead (faster than the player walks), for a while, also when shot from
             afar.
         It moves with its Rigidbody (it walks up slopes and bumps into things), looking ahead for trunks, rocks and
         walls and going around them. A punch's push (Knockback) plays out first. It stops for good when it dies
         (EnemyAnimator).
         The other types (fight or flee by the odds, groups, archers) come in the next steps of the plan; until then
         every Kind acts like Type 1.
Requirement: HumanBrain.cs x-> EnemyCombat.cs, HumanLoot.cs, ResourceNode.cs, ToolRules.cs, ToolStats.cs, Rigidbody (Unity)
             HumanBrain.cs o-> HumanGear.cs, EnemyAnimator.cs, Knockback.cs, PlayerController.cs, PlayerHealth.cs, PunchEffect.cs, PerkEffects.cs, CaveTrail.cs, Animator (Unity), CapsuleCollider (Unity)
             HumanBrain.cs <-x HumanEnemyBuilder.cs
             HumanBrain.cs <-o MonsterBounty.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(10)]   // after EnemyAnimator's Update: it can shrug off a flinch EnemyAnimator just asked for
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyCombat))]
public class HumanBrain : MonoBehaviour
{
    // The five human types of the plan. So far every one acts like Type 1.
    public enum Kind { Type1, Type2, Type3, Type4, Type5 }

    // What it does when the player hurts it.
    public enum WhenHit { FightBack, RunAway }

    // When an attack (or a punch, or a pick) lands and when its move is over, in seconds from its start.
    [System.Serializable]
    public struct Strike
    {
        [Tooltip("Seconds from the start of the move until the blow lands (or the hand picks).")]
        public float hit;
        [Tooltip("Seconds until the move is over (it can move again then).")]
        public float done;

        public Strike(float hit, float done)
        {
            this.hit = hit;
            this.done = done;
        }
    }

    // The Animator's parameters for fighting (the tool's controller has them): which attack animation (0 = fists,
    // 1 = a one-handed weapon, 2 = a two-handed one), and "swinging right now" (a hit doesn't make it flinch then).
    public const string AttackKindName = "AttackKind";
    public const string AttackingName = "Attacking";

    // The Animator's on / off parameter for chopping and mining with a tool (the tool's controller has it), and the
    // state that plays the swing (named the same).
    public const string WorkName = "Work";
    public const string WorkStateName = "Work";

    // The Animator's trigger for picking berries (a one-shot bend and pick).
    public const string GatherName = "Gather";

    // The Animator's floats for its legs: how fast it moves to its own right (MoveX) and forward (MoveZ), in meters a
    // second (minus = left / backward), so it runs, strafes and backpedals with the right animation, like the player.
    public const string MoveXName = "MoveX";
    public const string MoveZName = "MoveZ";

    // The Animator's float that plays the walk and run faster for a smaller body (its legs are shorter, so they step
    // faster to keep up) and slower for a bigger one; 1 = a body the player's size, as big as the animations were made for.
    public const string StrideName = "StrideSpeed";

    // Raised when the default numbers change, so Build Human Enemies gives old prefabs the new ones once.
    // 3 (23/11/2026): Work Search Radius 50. 4: Work Reach 1.2 for a human the player's size, times its size.
    // 5: fighting like a player (Attack Every 0.9, sprinting, strafing, Flinch Every).
    public const int SettingsVersion = 5;

    enum Mood { Resting, Walking, GoingToWork, Working, Fleeing, Fighting }

    enum Job { Chop, Mine, Pick }   // a tree, a rock, a berry bush

    // ===== Fields =====
    [Header("Who")]
    [Tooltip("Which human type it is (the plan's 1 - 5). For now every type acts like Type 1: never attacks first, works, fights back when hit.")]
    [SerializeField] Kind kind = Kind.Type1;

    [Header("Moving")]
    [Tooltip("On: it walks as fast as the player walks (the Player's PlayerController > Speed when the game starts) and runs away Flee Times Player as fast. Off: Walk Speed and Flee Speed below.")]
    [SerializeField] bool matchPlayerSpeed = true;

    [Range(1f, 3f)]
    [Tooltip("With Match Player Speed: running away = the player's walking speed x this (1.3 = faster than the player walks, slower than the player sprints).")]
    [SerializeField] float fleeTimesPlayer = 1.3f;

    [Range(0.5f, 20f)]
    [Tooltip("Walking speed when Match Player Speed is off (meters per second; the player walks at 7.8).")]
    [SerializeField] float walkSpeed = 7.8f;

    [Range(1f, 25f)]
    [Tooltip("Running-away speed when Match Player Speed is off.")]
    [SerializeField] float fleeSpeed = 10f;

    [Range(30f, 1000f)]
    [Tooltip("How fast it turns (degrees per second).")]
    [SerializeField] float turnSpeed = 540f;

    [Range(3f, 100f)]
    [Tooltip("How far from home it wanders (meters). Home = where it appeared, or where it calmed down after running away.")]
    [SerializeField] float wanderRadius = 40f;

    [Tooltip("Seconds it stands still between walks (a random number in this range).")]
    [SerializeField] Vector2 restSeconds = new Vector2(2f, 6f);

    [Header("Working (chopping, mining, picking)")]
    [Range(0f, 1f)]
    [Tooltip("After a rest: the chance it goes to work instead of walking around.")]
    [SerializeField] float workChance = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("When a tree or rock it worked on is gone: the chance it goes straight to the next one (else it rests).")]
    [SerializeField] float keepWorkingChance = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("When it goes to work: the chance it looks for a rock first (stone, ore). Else a tree, or a berry bush (Berry Share).")]
    [SerializeField] float rockShare = 0.45f;

    [Range(0f, 1f)]
    [Tooltip("When it goes to work: the chance it looks for a berry bush first.")]
    [SerializeField] float berryShare = 0.1f;

    [Range(5f, 100f)]
    [Tooltip("How far it looks for a tree, rock or berry bush to work on (meters). Rocks are rarer than trees in the grassland (about one in a 30 m circle), so it looks a good way.")]
    [SerializeField] float workSearchRadius = 50f;

    [Range(0.3f, 3f)]
    [Tooltip("How close to the tree or rock it stands to swing a tool at it (meters from its edge), for a human the player's size (a bigger or smaller one stands that much further or closer).")]
    [SerializeField] float workReach = 1.2f;

    [Range(0.2f, 2f)]
    [Tooltip("How close it stands to punch a tree or rock with bare hands, or to pick berries (meters from its edge).")]
    [SerializeField] float handReach = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("When its tool lands in the work animation, as a share of one swing: each swing of the animation hits the tree or rock there, with a wind burst. Build Human Enemies measures it (Blink's MiningLoop: about 0.46, when the hand comes down).")]
    [SerializeField] float swingAt = 0.46f;

    [Range(0.3f, 5f)]
    [Tooltip("Seconds between its swings when its Animator has no Work state (with the work animation each swing lands at Swing At instead).")]
    [SerializeField] float swingEvery = 1f;

    [Range(1f, 100f)]
    [Tooltip("Damage of one swing without the right tool: a punch with bare hands (a tree has 30 health, a stone 30, the snow's spruce 300).")]
    [SerializeField] float swingDamage = 10f;

    [Range(1f, 100f)]
    [Tooltip("Damage of one swing with the right tool (an axe at a tree, a pickaxe at a rock) of wooden tier; each tier above adds 25 %: stone x1.25, iron x1.56, gold x1.95, diamond x2.44.")]
    [SerializeField] float toolDamage = 15f;

    [Range(5f, 120f)]
    [Tooltip("Gives up on a tree or rock that's still standing after this many seconds of work.")]
    [SerializeField] float workMostSeconds = 60f;

    [Range(0, 200)]
    [Tooltip("The most things it carries; with a full bag it only walks around.")]
    [SerializeField] int carryMost = 40;

    [Range(3f, 40f)]
    [Tooltip("Gives up walking to a tree or rock it can't reach after this many seconds.")]
    [SerializeField] float giveUpSeconds = 12f;

    [Header("When the player hits it")]
    [Tooltip("Fight Back: it attacks the player (Type 1). Run Away: it runs off instead (the numbers under Running Away).")]
    [SerializeField] WhenHit whenHit = WhenHit.FightBack;

    [Range(2f, 60f)]
    [Tooltip("It stops following a player who is farther than this (meters) and goes back to its life (once Chase After Hit is over).")]
    [SerializeField] float giveUpRange = 10f;

    [Range(0f, 30f)]
    [Tooltip("After each hit (an arrow from afar too) it chases the player for at least this many seconds, however far away they are: it knows who hurt it. Then Give Up Range counts again.")]
    [SerializeField] float chaseAfterHit = 8f;

    [Tooltip("On: it remembers that the player hit it, and attacks again when the player comes within Notice Range.")]
    [SerializeField] bool holdsGrudge = true;

    [Range(1f, 60f)]
    [Tooltip("With a grudge: it attacks again when the player comes this close (meters). Keep it below Give Up Range, or it starts and stops at the edge.")]
    [SerializeField] float noticeRange = 8f;

    [Range(0.5f, 2f)]
    [Tooltip("Its running speed while chasing the player: the player's walking speed x this (it sprints faster while its stamina lasts).")]
    [SerializeField] float chaseTimesPlayer = 1.2f;

    [Header("Fighting like a player")]
    [Range(1f, 100f)]
    [Tooltip("One punch with bare hands. With a weapon it hits for the weapon's own damage instead (HumanGear > Weapons > Damage at copper, each tier above +25 %). The player's armor takes its share off.")]
    [SerializeField] float fistDamage = 5f;

    [Range(0.5f, 4f)]
    [Tooltip("How close it gets to hit (meters from its middle to the player's), with a one-handed weapon; fists reach 70 % of it, a two-handed weapon (longsword, pike) 130 %.")]
    [SerializeField] float attackReach = 2f;

    [Range(0.3f, 5f)]
    [Tooltip("Seconds from one attack to the next (never less than the attack's move).")]
    [SerializeField] float attackEvery = 0.9f;

    [Tooltip("On: it sprints at the player (as fast as the player sprints) while its stamina lasts, like the player.")]
    [SerializeField] bool sprints = true;

    [Range(1f, 30f)]
    [Tooltip("It sprints when the player is farther than this (meters); closer it runs.")]
    [SerializeField] float sprintFrom = 4f;

    [Range(1f, 100f)]
    [Tooltip("Stamina it uses a second while sprinting (it has 100, like the player: 20 = a 5 second sprint).")]
    [SerializeField] float sprintDrain = 20f;

    [Range(1f, 100f)]
    [Tooltip("Stamina it gets back a second, a second after it stops sprinting.")]
    [SerializeField] float staminaRegen = 15f;

    [Range(0f, 100f)]
    [Tooltip("Out of stamina it's tired (no sprinting) until it has this much again.")]
    [SerializeField] float recoverTo = 30f;

    [Tooltip("On: between attacks it circles the player at reach, facing them, like a player strafing; off: it stands and waits.")]
    [SerializeField] bool strafes = true;

    [Range(0f, 10f)]
    [Tooltip("It flinches at most once in this many seconds: fast hits can't keep it stunned (it never flinches mid-swing). 0 = every hit.")]
    [SerializeField] float flinchEvery = 2f;

    [Header("Move timing (Build Human Enemies measures these)")]
    [Tooltip("Fists (Blink's PunchRight; its big punch is thrown with the left hand): also each punch at a tree or rock it has no tool for.")]
    [SerializeField] Strike fistStrike = new Strike(0.46f, 0.78f);
    [Tooltip("A one-handed weapon: sword, mace, war axe (Blink's MeleeAttack_OneHanded, a downward slash).")]
    [SerializeField] Strike oneHandStrike = new Strike(0.31f, 0.73f);
    [Tooltip("A two-handed weapon: longsword, pike (Blink's MeleeAttack_TwoHanded, a big overhead blow with a step).")]
    [SerializeField] Strike twoHandStrike = new Strike(0.69f, 1.56f);
    [Tooltip("Picking berries (Blink's Gathering): when the hand picks, and when it's standing again.")]
    [SerializeField] Strike gatherTiming = new Strike(1f, 2f);

    [Header("Running away")]
    [Range(1f, 30f)]
    [Tooltip("It runs at least this many seconds after being hurt.")]
    [SerializeField] float fleeSeconds = 6f;

    [Range(5f, 150f)]
    [Tooltip("Then it stops once it's this far from the player (meters).")]
    [SerializeField] float safeDistance = 40f;

    [Range(5f, 60f)]
    [Tooltip("And it stops after this many seconds whatever happens (a player chasing it all the way).")]
    [SerializeField] float fleeMostSeconds = 15f;

    [Header("Now (while playing)")]
    [Tooltip("What it's doing (only to read).")]
    [SerializeField] string doing = "";

    [SerializeField, HideInInspector] int settingsVersion = SettingsVersion;   // which defaults this prefab was made with

    const float LookEvery = 0.15f;      // seconds between looks ahead for trunks, rocks and walls
    const float MaxWalkSlope = 45f;     // ground ahead steeper than this counts as a wall
    const float StuckEvery = 2f;        // seconds between "am I getting anywhere?" checks
    const float StuckDistance = 0.6f;   // moved less than this in that time = stuck
    const float WalkGiveUp = 15f;       // seconds before a walk that doesn't arrive is given up
    const float TierDamage = 1.25f;     // each tool tier makes a swing this many times stronger
    const int NearestToPick = 3;        // works on one of the 3 nearest (not always the same one)
    const float BurstDistance = 60f;    // no wind burst farther from the camera than this (too small to see: saves the particles)
    const float FistReach = 0.7f;       // fists reach this share of Attack Reach, a two-handed weapon TwoHandReach
    const float TwoHandReach = 1.3f;
    const float LungeReach = 1.25f;     // a blow still lands on a player up to this much past its reach (the weapon sweeps)
    const float HitAngle = 70f;         // ... and up to this many degrees to its side
    const float FaceAngle = 35f;        // it starts a swing only once it faces the player this well
    const float StartWithin = 2f;       // it starts a swing from at most this many reaches away (it steps in while it winds up)
    const float StepInUntil = 0.7f;     // while winding up it keeps running in until the player is this share of its reach away
    const float StandWithin = 0.85f;    // closer than this share of its reach it stops running and stands
    const float StrafeWithin = 1.8f;    // between attacks it circles a player within this many reaches
    const float StrafeGap = 0.9f;       // ... keeping about this share of its reach away
    const float StrafeShare = 0.5f;     // ... at this share of its walking speed
    const float MaxStamina = 100f;      // like the player's
    const float StaminaRestDelay = 1f;  // seconds after a sprint before stamina comes back
    const float LegsDamp = 0.1f;        // seconds the leg animation takes to follow a change of direction
    const float HandPause = 0.25f;      // seconds between two punches at a tree or rock
    const float TrailReach = 1.5f;      // a trail point counts as reached this close (meters)
    const float TrailCheckEvery = 0.5f; // seconds between checks whether a fight goes through a cave entrance

    static readonly int WorkHash = Animator.StringToHash(WorkName);
    static readonly int StrideHash = Animator.StringToHash(StrideName);
    static readonly int WorkStateHash = Animator.StringToHash(WorkStateName);
    static readonly int AttackKindHash = Animator.StringToHash(AttackKindName);
    static readonly int AttackingHash = Animator.StringToHash(AttackingName);
    static readonly int AttackHash = Animator.StringToHash(EnemyAnimator.AttackName);
    static readonly int HitHash = Animator.StringToHash(EnemyAnimator.HitName);
    static readonly int GatherHash = Animator.StringToHash(GatherName);
    static readonly int MoveXHash = Animator.StringToHash(MoveXName);
    static readonly int MoveZHash = Animator.StringToHash(MoveZName);
    static readonly RaycastHit[] hits = new RaycastHit[8];
    static readonly Collider[] nearby = new Collider[256];   // everything within Work Search Radius (a forest: ~60 trees in 50 m)

    Rigidbody body;
    EnemyCombat combat;        // Marko's health script: it says when it's hurt and when it's dead
    EnemyAnimator shown;       // plays the animations, and stops it for good when it dies (can be missing)
    HumanLoot loot;            // its bag (can be missing: then it doesn't work)
    HumanGear gear;            // its weapon and tools (can be missing: bare hands)
    Animator animator;
    Transform player;
    Rigidbody playerBody;      // the player's body: how fast they move (it times its swings by it)
    PlayerHealth playerHealth; // the player's health: hurt by its attacks, and no fighting a dead player
    System.Action<ItemData, int> gathered;   // what a broken tree / rock / bush gives goes into the bag
    bool hasWork;              // the Animator's controller has the Work parameter
    bool hasWorkState;         // ... and the Work state (else its swings are timed by Swing Every)
    bool hasAttack;            // ... the attack animations
    bool hasGather;            // ... the berry-picking animation
    bool hasMove;              // ... the legs by direction (MoveX / MoveZ)
    bool hasHit;               // ... the flinch
    bool inWorkState;          // the Work state was seen since it started this job (its swings are counted from there)
    int swingsDone;            // which swing of the work animation landed last
    float lastWorkTime;        // how far the work animation had played last frame (less now = it started over)
    Transform hand;            // its right hand (a humanoid body's bone): where its swings come from
    float probeHeight = 0.9f;  // looking ahead at chest height (from its capsule)
    float probeRadius = 0.25f;
    float size = 1f;           // how big its body is: 1 = the player's size (and the animations')
    float walkNow;             // its speeds this game (the player's, or Walk / Flee Speed)
    float fleeNow;
    float chaseNow;
    float sprintNow;
    float lungeNow;

    bool grudge;               // the player hurt it: it attacks again when the player comes near (Holds Grudge)
    int attackKind;            // 0 = fists, 1 = a one-handed weapon, 2 = a two-handed one
    float attackStarted = -1f; // Time.time its attack began (-1 = not attacking)
    bool struck;               // this attack's blow has landed (or missed)
    float nextAttack;          // Time.time it may attack again
    float chaseUntil;          // Time.time it chases until whatever the distance (it was just hit: Chase After Hit)
    int strafeSide = 1;        // which way it circles the player (1 = to its right)
    float stamina = MaxStamina;
    bool tired;                // ran out of stamina: no sprinting until Recover To
    bool sprinting;
    float lastSprint = -10f;
    float nextFlinch;          // Time.time it may flinch again (Flinch Every)

    bool handWork;             // working a tree or rock with bare hands (no tool for it): punches
    bool picking;              // picking a berry bush by hand
    float handStarted = -1f;   // Time.time the punch or pick began (-1 = none playing)
    bool handLanded;           // ... and it has landed
    float nextHand;            // Time.time the next punch or pick may start

    readonly List<Vector3> trail = new List<Vector3>();   // waypoints through cave entrances before its goal (CaveTrail)
    float nextTrailCheck;

    Mood mood;
    float moodUntil;           // Time.time when this mood ends (resting, working, giving up)
    Vector3 home;
    Vector3 goal;
    ResourceNode workNode;     // the tree, rock or bush it works on
    Collider workShape;        // its collider (to measure how close it is)
    float nextSwing;
    float lastHealth;
    float fleeStarted;
    Vector3 heading;           // the way it's really going (around things in the way)
    float nextLook;
    int detourSide = 1;        // goes around things on this side first (keeps it from zigzagging)
    float nextStuckCheck;
    Vector3 stuckFrom;

    public Kind Type => kind;
    public bool IsFleeing => mood == Mood.Fleeing;
    public bool IsFighting => mood == Mood.Fighting;
    public bool HoldsGrudge => grudge;   // the player hit it before: it attacks when the player comes near
    public bool IsWorking => mood == Mood.Working;
    public bool IsSprinting => sprinting;
    public float Stamina => stamina;
    public int MadeWithSettings => settingsVersion;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        combat = GetComponent<EnemyCombat>();
        shown = GetComponent<EnemyAnimator>();
        loot = GetComponent<HumanLoot>();
        gear = GetComponent<HumanGear>();
        body.freezeRotation = true;   // it turns itself; physics never tips it over
        gathered = (item, amount) => { if (loot != null) loot.Carry(item, amount); };
    }

    void Start()
    {
        // Measured here, not in Awake: the Human Spawner makes each human a little taller or shorter right after it
        // appears. The body's size is its Model's size in the world (Polytope's 1.8 m body = 1 = the player's size).
        animator = GetComponentInChildren<Animator>();
        size = Mathf.Max(0.2f, animator != null ? animator.transform.lossyScale.y : transform.lossyScale.y);
        CapsuleCollider shape = GetComponent<CapsuleCollider>();
        if (shape != null)
        {
            probeHeight = shape.center.y * transform.lossyScale.y;
            probeRadius = shape.radius * 0.8f * transform.lossyScale.x;
        }

        // Its attack animation by what it holds (HumanGear dressed it before this Start).
        attackKind = gear == null || !gear.HasWeapon ? 0 : gear.TwoHanded ? 2 : 1;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == WorkHash) hasWork = true;
                if (p.nameHash == AttackingHash) hasAttack = true;
                if (p.nameHash == GatherHash) hasGather = true;
                if (p.nameHash == MoveZHash) hasMove = true;
                if (p.nameHash == HitHash) hasHit = true;
                if (p.nameHash == AttackKindHash) animator.SetInteger(AttackKindHash, attackKind);
                if (p.nameHash == StrideHash) animator.SetFloat(StrideHash, 1f / size);   // short legs step faster, long ones slower
            }
            hasWorkState = hasWork && animator.HasState(0, WorkStateHash);
            if (animator.isHuman) hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null)
        {
            player = controller.transform;
            playerBody = controller.GetComponent<Rigidbody>();
            playerHealth = controller.GetComponent<PlayerHealth>();
        }
        walkNow = walkSpeed;
        fleeNow = fleeSpeed;
        float sprintTimes = 2f;
        if (matchPlayerSpeed && controller != null)
        {
            walkNow = controller.StartSpeed > 0f ? controller.StartSpeed : controller.Speed;   // as fast as the player walks
            fleeNow = walkNow * fleeTimesPlayer;
            sprintTimes = Mathf.Max(1f, controller.BaseSprintMultiplier);                    // ... and sprints (no perks)
        }
        chaseNow = walkNow * chaseTimesPlayer;
        sprintNow = Mathf.Max(chaseNow, walkNow * sprintTimes);
        lungeNow = chaseNow;   // it keeps running in while it winds up: the attacks move only its upper body (Actions layer)

        home = transform.position;
        lastHealth = combat.currentHealth;
        stuckFrom = transform.position;
        Rest();
    }

    void Update()
    {
        if (Dead())
        {
            SetWorking(false);
            enabled = false;   // dying: EnemyAnimator holds it still and Marko's EnemyCombat removes it
            return;
        }

        if (combat.currentHealth < lastHealth)
        {
            Flinch();
            Hurt();
        }
        lastHealth = combat.currentHealth;

        // It remembers who hit it: that player coming near again gets attacked again.
        if (grudge && mood != Mood.Fighting && mood != Mood.Fleeing && PlayerWithin(noticeRange)) Fight();

        FollowTrail();
        switch (mood)
        {
            case Mood.Resting:
                if (Time.time < moodUntil) break;
                if (Random.value < workChance && GoToWork()) break;
                Walk();
                break;
            case Mood.Walking:
                if ((Flat(goal - transform.position) < 1f && trail.Count == 0) || Time.time >= moodUntil) Rest();
                break;
            case Mood.GoingToWork:
                if (!StillThere()) NextJob();
                else if (DistanceToWork() <= WorkReachFor(workNode)) StartWorking();
                else if (Time.time >= moodUntil) Rest();   // couldn't get there: something else then
                break;
            case Mood.Working:
                Working();
                break;
            case Mood.Fleeing:
                float away = player != null ? Flat(transform.position - player.position) : safeDistance;
                float running = Time.time - fleeStarted;
                if ((running >= fleeSeconds && away >= safeDistance) || running >= fleeMostSeconds) CalmDown();
                break;
            case Mood.Fighting:
                Fighting();
                break;
        }
        CheckStuck();
        Breathe();
        ShowLegs();
        doing = Describe();
    }

    void FixedUpdate()
    {
        if (body.isKinematic || Dead()) return;   // dying: EnemyAnimator holds it still
        if (Knockback.IsPushed(body)) return;      // a punch is pushing it: let the push play out

        Vector3 wanted = WantedDirection(out float speed);
        bool facePlayer = mood == Mood.Fighting && player != null && trail.Count == 0;   // fighting: eyes on the player
        if (speed <= 0f || wanted == Vector3.zero)
        {
            body.velocity = new Vector3(0f, body.velocity.y, 0f);   // stands (gravity still works)
            if (mood == Mood.Working && workShape != null) Face(workShape.bounds.center - transform.position);
            else if (facePlayer) Face(player.position - transform.position);
            return;
        }

        if (Time.time >= nextLook || Vector3.Angle(wanted, heading) > 60f)
        {
            nextLook = Time.time + LookEvery;
            float distance = speed * 0.6f + 1f;
            heading = Clear(wanted, distance) ? wanted : Detour(wanted, distance);
        }
        body.velocity = new Vector3(heading.x * speed, body.velocity.y, heading.z * speed);
        Face(facePlayer ? player.position - transform.position : heading);   // strafes and backs off facing the player
    }

    // ===== My methods =====

    // Hurt by something (so far only the player can hurt humans): it fights back (and holds a grudge), or runs away.
    void Hurt()
    {
        SetWorking(false);
        workNode = null;
        workShape = null;
        if (whenHit == WhenHit.FightBack && player != null)
        {
            if (holdsGrudge) grudge = true;
            chaseUntil = Time.time + chaseAfterHit;   // it knows who hurt it: after them, however far (an arrow from afar)
            Fight();
            return;
        }
        if (attackStarted >= 0f) EndAttack();
        mood = Mood.Fleeing;
        fleeStarted = Time.time;
        if (player != null) PlanTrail(transform.position + Level(transform.position - player.position) * 20f);   // out of a cave first
        nextLook = 0f;
        stuckFrom = transform.position;
        nextStuckCheck = Time.time + StuckEvery;
    }

    // EnemyAnimator has just asked for the flinch (it saw the same health drop this frame, before this script): it
    // plays only if the last one was at least Flinch Every ago, so fast hits can't keep it stunned.
    void Flinch()
    {
        if (!hasHit || animator == null) return;
        if (Time.time < nextFlinch) animator.ResetTrigger(HitHash);
        else nextFlinch = Time.time + flinchEvery;
    }

    // After the player: it chases them and attacks within reach (its weapon out, its tools away).
    void Fight()
    {
        SetWorking(false);
        workNode = null;
        workShape = null;
        if (mood == Mood.Fighting) return;
        mood = Mood.Fighting;
        if (player != null) PlanTrail(player.position);
        nextTrailCheck = Time.time + TrailCheckEvery;
        StartMoving();
    }

    // Fighting, every frame: a swing plays out first; a dead or far-away player ends the fight (Give Up Range: it goes
    // back to its life, still remembering); through a cave's entrance first if the player is on the other side;
    // facing the player, it starts a swing when the player will be within reach as the blow lands.
    void Fighting()
    {
        if (attackStarted >= 0f)
        {
            Swinging();
            return;
        }
        bool justHit = Time.time < chaseUntil;   // just hurt: it follows however far away the player is
        if (!PlayerWithin(justHit ? float.MaxValue : giveUpRange))
        {
            Rest();
            return;
        }
        if (Time.time >= nextTrailCheck)
        {
            nextTrailCheck = Time.time + TrailCheckEvery;
            if (trail.Count == 0) PlanTrail(player.position);
        }
        if (trail.Count > 0) return;   // getting out of (or into) a cave first

        Vector3 to = player.position - transform.position;
        to.y = 0f;
        float gap = to.magnitude;
        if (Time.time < nextAttack || Vector3.Angle(transform.forward, to) > FaceAngle) return;

        // Like a player timing a hit: where will the player be when the blow lands? It steps in while it winds up
        // (Lunge), and a player walking away keeps some of the gap.
        Strike timing = StrikeNow();
        float away = Vector3.Dot(PlayerVelocity(), gap > 0.001f ? to / gap : transform.forward);   // + = moving away from it
        float gapAtBlow = gap - (lungeNow - away) * timing.hit;
        if (gapAtBlow <= Reach() && gap <= Reach() * StartWithin) StartAttack();
    }

    void StartAttack()
    {
        attackStarted = Time.time;
        struck = false;
        nextAttack = Time.time + Mathf.Max(attackEvery, StrikeNow().done);
        strafeSide = Random.value < 0.5f ? 1 : -1;   // which way it circles the player after this attack
        if (hasAttack && animator != null)
        {
            animator.SetInteger(AttackKindHash, attackKind);   // its weapon's attack (a punch at a tree may have set fists)
            animator.SetBool(AttackingHash, true);             // mid-swing a hit doesn't make it flinch
        }
        if (shown != null) shown.PlayAttack();   // its attack animation (EnemyAnimator: the Attack trigger)
    }

    // An attack playing: the blow lands at its hit time, and the move ends at its done time.
    void Swinging()
    {
        Strike timing = StrikeNow();
        float playing = Time.time - attackStarted;
        if (!struck && playing >= timing.hit)
        {
            struck = true;
            LandAttack();
        }
        if (playing >= timing.done) EndAttack();
    }

    void EndAttack()
    {
        attackStarted = -1f;
        if (!hasAttack || animator == null) return;
        animator.SetBool(AttackingHash, false);
        if (hasHit) animator.ResetTrigger(HitHash);   // the hits it took mid-swing don't make it flinch now
    }

    // The blow lands: the player is hurt if still within reach (a bit more: the weapon sweeps) and in front of it. The
    // player's armor and Last Stand count (PlayerHealth.TakeDamage), and the Thorns perk hurts it back, like a monster.
    void LandAttack()
    {
        if (player == null || playerHealth == null || playerHealth.IsDead) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.magnitude > Reach() * LungeReach) return;              // got out of reach anyway: a miss
        if (Vector3.Angle(transform.forward, to) > HitAngle) return;   // slipped round its side: a miss
        float damage = AttackDamage();
        playerHealth.TakeDamage(damage);
        PerkEffects.Reflect(this, damage);
    }

    // One hit's damage: its weapon's (by kind and tier: HumanGear.WeaponDamage), or Fist Damage with bare hands.
    float AttackDamage() => gear != null && gear.HasWeapon ? gear.WeaponDamage : fistDamage;

    // How close it gets to hit: Attack Reach for a one-handed weapon, fists less, a two-handed weapon more; a bigger
    // body reaches farther.
    float Reach()
    {
        float share = attackKind == 0 ? FistReach : attackKind == 2 ? TwoHandReach : 1f;
        return attackReach * share * size;
    }

    Strike StrikeNow() => attackKind == 0 ? fistStrike : attackKind == 2 ? twoHandStrike : oneHandStrike;

    // How it moves while fighting, like a player: through a cave's entrance first; runs (or sprints) at the player;
    // keeps stepping in while its swing winds up; between attacks it circles the player at reach.
    Vector3 FightingMove(out float speed)
    {
        speed = 0f;
        if (player == null) return Vector3.zero;
        if (trail.Count > 0)
        {
            speed = sprinting ? sprintNow : chaseNow;
            return Level(trail[0] - transform.position);
        }
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        float gap = to.magnitude;
        Vector3 toward = gap > 0.001f ? to / gap : transform.forward;
        float reach = Reach();

        if (attackStarted >= 0f && !struck)   // winding up: keeps running in until the blow lands (backing off doesn't dodge it)
        {
            if (gap <= reach * StepInUntil) return Vector3.zero;
            speed = lungeNow;
            return toward;
        }
        bool waiting = attackStarted >= 0f || Time.time < nextAttack;   // the blow is in, or its next attack isn't ready
        if (strafes && waiting && gap <= reach * StrafeWithin)          // ... it circles the player at reach
        {
            Vector3 around = Vector3.Cross(Vector3.up, toward) * strafeSide;
            float keep = Mathf.Clamp((gap - reach * StrafeGap) / reach, -1f, 1f);   // + = too far: in, - = too close: back off
            speed = walkNow * StrafeShare;
            return (around + toward * keep * 1.5f).normalized;
        }
        if (gap <= reach * StandWithin || attackStarted >= 0f) return Vector3.zero;   // close enough (or finishing a swing): stands
        speed = sprinting ? sprintNow : chaseNow;
        return toward;
    }

    // Its stamina, like the player's: sprinting uses Sprint Drain a second; a second after it stops it comes back
    // Stamina Regen a second; at 0 it's tired (no sprinting) until it has Recover To again.
    void Breathe()
    {
        sprinting = sprints && mood == Mood.Fighting && attackStarted < 0f && !tired && stamina > 0f && player != null &&
                    Flat(player.position - transform.position) > sprintFrom;
        if (sprinting)
        {
            stamina -= sprintDrain * Time.deltaTime;
            lastSprint = Time.time;
            if (stamina <= 0f)
            {
                stamina = 0f;
                tired = true;
                sprinting = false;
            }
        }
        else if (Time.time - lastSprint >= StaminaRestDelay)
        {
            stamina = Mathf.Min(MaxStamina, stamina + staminaRegen * Time.deltaTime);
            if (tired && stamina >= recoverTo) tired = false;
        }
    }

    // Its legs: how fast it moves to its right and forward (meters a second), eased, so the animation runs, strafes,
    // backpedals or sprints the way it really moves.
    void ShowLegs()
    {
        if (!hasMove || animator == null) return;
        Vector3 local = transform.InverseTransformDirection(body.velocity);
        animator.SetFloat(MoveXHash, local.x, LegsDamp, Time.deltaTime);
        animator.SetFloat(MoveZHash, local.z, LegsDamp, Time.deltaTime);
    }

    Vector3 PlayerVelocity()
    {
        if (playerBody == null) return Vector3.zero;
        Vector3 v = playerBody.velocity;
        v.y = 0f;
        return v;
    }

    // The player is alive and within this many meters (flat).
    bool PlayerWithin(float range)
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead)) return false;
        return Flat(player.position - transform.position) <= range;
    }

    // Far enough away: it stops running and lives here now.
    void CalmDown()
    {
        home = transform.position;
        Rest();
    }

    void Rest()
    {
        if (attackStarted >= 0f) EndAttack();   // (a swing cut short: its Attacking flag off again)
        SetWorking(false);
        workNode = null;
        workShape = null;
        trail.Clear();
        mood = Mood.Resting;
        moodUntil = Time.time + Random.Range(restSeconds.x, Mathf.Max(restSeconds.x, restSeconds.y));
    }

    // A walk to a random spot around its home (through a cave's entrance when it's in one, or the spot is).
    void Walk()
    {
        SetWorking(false);
        Vector2 spot = Random.insideUnitCircle * wanderRadius;
        goal = home + new Vector3(spot.x, 0f, spot.y);
        PlanTrail(goal);
        mood = Mood.Walking;
        moodUntil = Time.time + WalkGiveUp;
        StartMoving();
    }

    // Off to a nearby tree, rock or berry bush (rocks about as often as trees). False = nothing it can work on near
    // here, or the bag is full.
    bool GoToWork()
    {
        if (loot == null || loot.CarriedCount >= carryMost) return false;
        float roll = Random.value;
        Job prefer = roll < rockShare ? Job.Mine : roll < rockShare + berryShare ? Job.Pick : Job.Chop;
        if (!FindWork(prefer, out workNode, out workShape)) return false;
        SetWorking(false);
        PlanTrail(workShape.bounds.center);   // an ore inside a cave: in through its entrance
        mood = Mood.GoingToWork;
        moodUntil = Time.time + giveUpSeconds + trail.Count * 3f;   // a cave's trail is a longer walk
        StartMoving();
        return true;
    }

    // The tree or rock it worked on is gone (it broke it, or the player did): the next one, or a rest.
    void NextJob()
    {
        if (Random.value < keepWorkingChance && GoToWork()) return;
        Rest();
    }

    // At the tree, rock or bush: the axe for a tree, the pickaxe for a rock (the work animation); no tool for it = it
    // puts its weapon away and punches; a bush = it picks it by hand.
    void StartWorking()
    {
        trail.Clear();
        mood = Mood.Working;
        moodUntil = Time.time + workMostSeconds;
        nextSwing = Time.time + swingEvery * 0.5f;   // (no work animation) the first swing lands half a swing in
        Job job = JobOf(workNode);
        picking = job == Job.Pick;
        handWork = !picking && ToolFor(job) == ToolTier.None;
        if (picking || handWork)
        {
            SetWorking(false);
            if (gear != null) gear.HoldNothing();   // bare hands
            handStarted = -1f;
            nextHand = Time.time + 0.15f;
            return;
        }
        inWorkState = false;                      // the animation's swings are counted from when it starts
        if (gear != null) gear.HoldTool(job == Job.Chop ? ToolKind.Axe : ToolKind.Pickaxe);
        SetWorking(true);
    }

    // Working at it: tool swings (each one landing when the tool comes down in the work animation), or punches, or
    // picking. When it breaks, what it gives goes into the bag (Turki's ResourceNode.TakeDamageFor), and the tree falls
    // / the rock bursts like the player's.
    void Working()
    {
        if (!StillThere())
        {
            NextJob();
            return;
        }
        if (handWork || picking) HandWork();
        else if (SwingLanded()) LandSwing();
        if (Time.time >= moodUntil) Walk();   // too tough for it: give up on this one
    }

    // Bare hands: one punch (the fist attack animation) or one pick (the gather animation) at a time, each landing when
    // the fist or hand does (Fist Strike / Gather Timing), the next one a moment after the last is over.
    void HandWork()
    {
        Strike timing = picking ? gatherTiming : fistStrike;
        if (handStarted >= 0f)
        {
            float playing = Time.time - handStarted;
            if (!handLanded && playing >= timing.hit)
            {
                handLanded = true;
                LandSwing();
            }
            if (playing >= timing.done) handStarted = -1f;
            return;
        }
        if (Time.time < nextHand) return;
        handStarted = Time.time;
        handLanded = false;
        nextHand = Time.time + timing.done + HandPause;
        if (animator == null) return;
        if (picking)
        {
            if (hasGather) animator.SetTrigger(GatherHash);
        }
        else if (hasAttack)
        {
            animator.SetInteger(AttackKindHash, 0);   // the fists' attack: a punch
            animator.SetTrigger(AttackHash);
        }
    }

    // True once for each swing, when its tool comes down: at Swing At of every loop of the work animation (it keeps
    // counting off screen too, where the bones stop moving). Without that animation: every Swing Every seconds.
    bool SwingLanded()
    {
        if (hasWorkState)
        {
            if (!WorkAnimationTime(out float time)) return false;   // still blending into the swing
            int swing = Mathf.FloorToInt(time - swingAt);           // which swing has landed by now
            if (!inWorkState || time < lastWorkTime)
            {
                // The animation just started (or started over: the last job's swing was still blending out when this
                // one began): count the swings from here.
                inWorkState = true;
                swingsDone = swing;
                lastWorkTime = time;
                return false;
            }
            lastWorkTime = time;
            if (swing <= swingsDone) return false;
            swingsDone = swing;
            return true;
        }
        if (Time.time < nextSwing) return false;
        nextSwing = Time.time + swingEvery;
        return true;
    }

    // How far the work animation has played: 0.5 = halfway through the first swing, 2.25 = a quarter into the third
    // (also while blending into it). False = it isn't playing.
    bool WorkAnimationTime(out float time)
    {
        time = 0f;
        if (animator == null) return false;
        AnimatorStateInfo now = animator.GetCurrentAnimatorStateInfo(0);
        if (now.shortNameHash == WorkStateHash)
        {
            time = now.normalizedTime;
            return true;
        }
        if (!animator.IsInTransition(0)) return false;
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        if (next.shortNameHash != WorkStateHash) return false;
        time = next.normalizedTime;
        return true;
    }

    // Its tool or fist comes down: the tree, rock or bush loses health (Turki's ResourceNode.TakeDamageFor; a bush is
    // picked at once) and the wind bursts where it hit, like the player's swings (PunchEffect: a tool's burst, or a
    // fist's with bare hands). No burst for picking berries, or too far from the camera to see.
    void LandSwing()
    {
        Vector3 point = Vector3.zero;
        Vector3 normal = Vector3.up;
        bool burst = JobOf(workNode) != Job.Pick && HitSpot(out point, out normal) && Seen(point);   // found first: a broken tree is gone after the hit
        bool holding = gear != null && gear.HoldsSomething;
        workNode.TakeDamageFor(SwingDamage(), gathered);
        if (burst) PunchEffect.PlayNow(point, normal, holding);
    }

    // Where its swing meets the tree or rock (and which way is out of it): a level ray from its hand toward the middle
    // of the target (a tree's middle is far up its trunk, so level with the hand), else straight at the middle (a low
    // rock under the hand), else the nearest point of the target's box.
    bool HitSpot(out Vector3 point, out Vector3 normal)
    {
        Bounds box = workShape.bounds;
        Vector3 from = hand != null ? hand.position : transform.position + Vector3.up * probeHeight;
        Vector3 level = box.center - from;
        level.y = 0f;
        if (level.sqrMagnitude < 0.0001f) level = transform.forward;
        if (HitsWork(from, level, out point, out normal)) return true;
        if (HitsWork(from, box.center - from, out point, out normal)) return true;
        point = box.ClosestPoint(from);
        normal = -level.normalized;
        return true;
    }

    // A ray from a meter behind "from" (in case the hand is already in it) along "way", against its target only.
    bool HitsWork(Vector3 from, Vector3 way, out Vector3 point, out Vector3 normal)
    {
        point = from;
        normal = Vector3.up;
        if (way.sqrMagnitude < 0.0001f) return false;
        Vector3 along = way.normalized;
        if (!workShape.Raycast(new Ray(from - along, along), out RaycastHit hit, way.magnitude + 3f)) return false;
        point = hit.point;
        normal = hit.normal;
        return true;
    }

    // Close enough to the camera for a wind burst to be seen.
    static bool Seen(Vector3 point)
    {
        Camera cam = Camera.main;
        return cam == null || (point - cam.transform.position).sqrMagnitude <= BurstDistance * BurstDistance;
    }

    // One swing's damage: Tool Damage (25 % more for each tier above wooden) with the right tool for the job, else
    // Swing Damage (a punch).
    float SwingDamage()
    {
        ToolTier tool = workNode != null ? ToolFor(JobOf(workNode)) : ToolTier.None;
        if (tool == ToolTier.None) return swingDamage;
        return toolDamage * Mathf.Pow(TierDamage, (int)tool - (int)ToolTier.Wood);
    }

    // The tier of what it swings at this job: its axe (or war axe) at a tree, its pickaxe at a rock; None = bare hands.
    ToolTier ToolFor(Job job)
    {
        if (gear == null) return ToolTier.None;
        if (job == Job.Chop) return gear.AxeTier;
        return job == Job.Mine ? gear.PickaxeTier : ToolTier.None;
    }

    static Job JobOf(ResourceNode node)
    {
        if (node.CollectWithKey) return Job.Pick;
        return node.resourceType == ResourceNode.ResourceType.Tree ? Job.Chop : Job.Mine;
    }

    // How close it stands to work on this: a tool's reach, or a hand's (punching, picking); a bigger body farther.
    float WorkReachFor(ResourceNode node)
    {
        Job job = JobOf(node);
        bool hands = job == Job.Pick || ToolFor(job) == ToolTier.None;
        return (hands ? handReach : workReach) * size;
    }

    // The nearest trees, rocks and berry bushes it can break (with its tools), the preferred kind first (any kind when
    // there's none of it), and one of the closest few.
    bool FindWork(Job prefer, out ResourceNode node, out Collider shape)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, workSearchRadius, nearby, ~0, QueryTriggerInteraction.Ignore);
        return PickNear(count, prefer, out node, out shape) || PickNear(count, null, out node, out shape);
    }

    // One of the 3 nearest workable ones among the found colliders (only of this kind when "only" is set).
    bool PickNear(int count, Job? only, out ResourceNode node, out Collider shape)
    {
        node = null;
        shape = null;
        ResourceNode[] bestNodes = new ResourceNode[NearestToPick];
        Collider[] bestShapes = new Collider[NearestToPick];
        float[] bestDistances = { float.MaxValue, float.MaxValue, float.MaxValue };

        for (int i = 0; i < count; i++)
        {
            ResourceNode n = nearby[i].GetComponentInParent<ResourceNode>();
            if (!Workable(n)) continue;
            if (only.HasValue && JobOf(n) != only.Value) continue;
            float d = Flat(nearby[i].bounds.ClosestPoint(transform.position) - transform.position);
            for (int slot = 0; slot < NearestToPick; slot++)   // keep the 3 nearest
            {
                if (bestNodes[slot] == n) break;
                if (d >= bestDistances[slot]) continue;
                for (int move = NearestToPick - 1; move > slot; move--)
                {
                    bestNodes[move] = bestNodes[move - 1];
                    bestShapes[move] = bestShapes[move - 1];
                    bestDistances[move] = bestDistances[move - 1];
                }
                bestNodes[slot] = n;
                bestShapes[slot] = nearby[i];
                bestDistances[slot] = d;
                break;
            }
        }

        int found = 0;
        while (found < NearestToPick && bestNodes[found] != null) found++;
        if (found == 0) return false;
        int pick = Random.Range(0, found);
        node = bestNodes[pick];
        shape = bestShapes[pick];
        return true;
    }

    // A tree, rock or berry bush still standing, with something to give, that it has the tool for (the player's rules:
    // an ore needs a pickaxe of its tier, the snow's spruce an iron axe; plain trees, stone and berries need none).
    // Not an animal.
    bool Workable(ResourceNode node)
    {
        if (node == null || node.currentHealth <= 0f || node.resourceType == ResourceNode.ResourceType.Animal || node.DropItem == null)
            return false;
        return ToolRules.NeededFor(node) <= ToolFor(JobOf(node));
    }

    bool StillThere() => workNode != null && workShape != null && workShape.enabled && workNode.currentHealth > 0f;

    // Meters from it to the edge of its tree or rock (the collider's box: rocks have mesh colliders).
    float DistanceToWork() => Flat(workShape.bounds.ClosestPoint(transform.position) - transform.position);

    // ----- Caves: the invisible trail through the entrance (CaveTrail) -----

    // The trail to a target: out of the cave it's in by the entrance, and into the target's cave by its entrance.
    // Nothing when both are out in the open or in the same cave.
    void PlanTrail(Vector3 target)
    {
        trail.Clear();
        CaveTrail from = CaveTrail.Around(transform.position);
        CaveTrail to = CaveTrail.Around(target);
        if (from == to) return;
        if (from != null) from.AddWayOut(trail, target);
        if (to != null) to.AddWayIn(trail, from != null ? from.Front : transform.position);
    }

    // Drops the trail points it has reached.
    void FollowTrail()
    {
        while (trail.Count > 0 && Flat(trail[0] - transform.position) < TrailReach) trail.RemoveAt(0);
    }

    // Where it's heading now, for a way out of a cave (its walk's goal, its tree or rock, or straight on).
    Vector3 Target()
    {
        if (mood == Mood.Walking) return goal;
        if (mood == Mood.GoingToWork && workShape != null) return workShape.bounds.center;
        return transform.position + transform.forward * 20f;
    }

    // ----- Moving -----

    // Which way it wants to go now, and how fast (0 = stand still). A cave's trail comes first.
    Vector3 WantedDirection(out float speed)
    {
        Vector3 to;
        switch (mood)
        {
            case Mood.Walking:
                speed = walkNow;
                to = goal - transform.position;
                break;
            case Mood.GoingToWork:
                speed = walkNow;
                to = workShape != null ? workShape.bounds.ClosestPoint(transform.position) - transform.position : Vector3.zero;
                break;
            case Mood.Fleeing:
                speed = fleeNow;
                to = player != null ? transform.position - player.position : transform.forward;
                break;
            case Mood.Fighting:
                return FightingMove(out speed);
            default:
                speed = 0f;
                return Vector3.zero;
        }
        if (trail.Count > 0) to = trail[0] - transform.position;   // through a cave's entrance first
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) to = transform.forward;   // right on top of it: keep going the way it faces
        return to.normalized;
    }

    // Is the way ahead free for "distance" meters? Trunks, rocks, walls and bodies block it; ground it can walk up and
    // the tree or rock it's walking to don't.
    bool Clear(Vector3 direction, float distance)
    {
        Vector3 from = transform.position + Vector3.up * probeHeight;
        int count = Physics.SphereCastNonAlloc(from, probeRadius, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.attachedRigidbody == body) continue;   // itself
            if (hit.collider == workShape) continue;                // where it's going to work
            if (mood == Mood.Fighting && player != null && hit.collider.transform.IsChildOf(player)) continue;   // the player it's after
            if (hit.distance <= 0f) continue;                       // touching it already at the start (the ground under it)
            if (hit.collider is TerrainCollider && Vector3.Angle(hit.normal, Vector3.up) < MaxWalkSlope) continue;   // a slope it can walk up
            return false;
        }
        return true;
    }

    // Something's in the way: the nearest free direction to either side (its usual side first), or back the way it came.
    Vector3 Detour(Vector3 wanted, float distance)
    {
        for (int step = 1; step <= 5; step++)
        {
            for (int side = 0; side < 2; side++)
            {
                int turn = side == 0 ? detourSide : -detourSide;
                Vector3 option = Quaternion.Euler(0f, 30f * step * turn, 0f) * wanted;
                if (!Clear(option, distance)) continue;
                detourSide = turn;
                return option;
            }
        }
        if (mood == Mood.Fighting) strafeSide = -strafeSide;   // circling into a wall: the other way round
        return -wanted;
    }

    // Turns its body toward a direction, a bit every physics step.
    void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, target, turnSpeed * Time.fixedDeltaTime));
    }

    void StartMoving()
    {
        nextLook = 0f;
        stuckFrom = transform.position;
        nextStuckCheck = Time.time + StuckEvery;
    }

    // Walking or running but not getting anywhere (caught between trunks, on a ledge, against a cave's wall): inside a
    // cave it takes the way out through the entrance; else it tries something else.
    void CheckStuck()
    {
        bool moving = mood == Mood.Walking || mood == Mood.GoingToWork || mood == Mood.Fleeing;
        if (!moving || Time.time < nextStuckCheck) return;
        nextStuckCheck = Time.time + StuckEvery;
        bool stuck = Flat(transform.position - stuckFrom) < StuckDistance;
        stuckFrom = transform.position;
        if (!stuck) return;

        CaveTrail cave = CaveTrail.Around(transform.position);
        Vector3 target = Target();
        if (cave != null && trail.Count == 0 && !cave.Contains(target))
        {
            cave.AddWayOut(trail, target);   // the way out of the cave
            return;
        }
        if (mood == Mood.Fleeing)
        {
            detourSide = -detourSide;   // run the other way round whatever is in the way
            heading = Quaternion.Euler(0f, 90f * detourSide, 0f) * heading;
            nextLook = Time.time + 0.6f;
        }
        else
        {
            Rest();
        }
    }

    // The work animation on or off; off also stops a punch or pick and takes its weapon again.
    void SetWorking(bool on)
    {
        if (hasWork && animator != null) animator.SetBool(WorkHash, on);
        if (on) return;
        inWorkState = false;
        handStarted = -1f;
        if (gear != null) gear.HoldWeapon();
    }

    bool Dead() => (shown != null && shown.IsDead) || combat.currentHealth <= 0f;

    static Vector3 Level(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.zero;
    }

    static float Flat(Vector3 v)
    {
        v.y = 0f;
        return v.magnitude;
    }

    string Describe()
    {
        string target = workNode != null ? workNode.name.Replace("(Clone)", "") : "?";
        string cave = trail.Count > 0 ? " (through a cave entrance)" : "";
        switch (mood)
        {
            case Mood.Working: return Verb() + " " + target;
            case Mood.GoingToWork: return "Walking to " + target + cave;
            case Mood.Walking: return "Walking" + cave;
            case Mood.Fleeing: return "Running away" + cave;
            case Mood.Fighting:
                if (attackStarted >= 0f) return "Attacking the player";
                if (player != null && Flat(player.position - transform.position) > Reach())
                    return (sprinting ? "Sprinting at the player" : "Chasing the player") + cave + " | stamina " + Mathf.RoundToInt(stamina);
                return (Time.time < nextAttack && strafes ? "Circling the player" : "Fighting the player") + " | stamina " + Mathf.RoundToInt(stamina);
            default: return mood.ToString();
        }
    }

    // "Chopping", "Mining", "Punching" or "Picking", by what it works on and with.
    string Verb()
    {
        if (workNode == null) return "Working on";
        if (picking || workNode.CollectWithKey) return "Picking";
        if (handWork) return "Punching";
        return workNode.resourceType == ResourceNode.ResourceType.Tree ? "Chopping" : "Mining";
    }
}
