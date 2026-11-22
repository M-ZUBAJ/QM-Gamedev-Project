/*
First Date: 21/11/2026
Date: 22/11/2026
Author: Tony
Use by: The object with Marko's EnemySpawner (in both game scenes). Tools > Enemies > Set Up Night Monsters puts it there
        with the turtle (night 2), the Chest Monster (night 3) and the flying Beholder (night 4).
Unity Editor: Monsters = the night monsters after the first night's (the EnemySpawner's Enemy Prefab, the slime, comes
              every night): each with its First Night, its Share of the night's spawns, Max Alive, its own Health and
              Damage (0 = the spawner's numbers) and Coins (times the slime's 3 - 6). A monster for a later night: click + under Monsters, drag its enemy
              prefab in, First Night 5 (the list is read top to bottom: keep it in order of First Night).
              Where they appear: Min / Max Distance from the player, and Out Of Sight.
              Limits: Most At Once and Leave Distance, both 0 = off (Normal, Hard). Easy on the NEW GAME page sets them
              when the game loads (20 and 90 m); to try them while testing, type them in here.
              Testing: Play, select the sun (DaylightCycle): Day = 2, then drag Time Of Day past 0.77 (night). About a
              third of the monsters are turtles. Turn around quickly: none pops up right in front of you.
Purpose: Which monsters come each night, how strong they are, and where they appear. Marko's EnemySpawner asks it for
         every spawn:
           - Pick: night 1 = only the spawner's Enemy Prefab (the slime); from a monster's First Night on, that share of
             the spawns is that monster (the turtle from night 2: 30 %, 5 % more each night, at most Max Alive at once,
             one more each night). So every night can bring a new monster, and the ones before keep coming. Together
             they take at most Most Share (95 %) of the spawns: once their shares grow past it (night 6 on), each keeps
             its part of it, so a monster lower in the list (the Beholder) isn't squeezed out by the ones above it;
           - Prepare: the picked monster's own Health and Damage (growing each night after its first) and its Coins
             (EnemyCombat's Coin Multiplier: the turtle 1.5, the Chest Monster 2, the Beholder 3 times the slime's 3 -
             6 coins); the slime keeps the spawner's numbers;
           - FindSpot: a spot Min - Max Distance away that the player can't see (PlayerSight: behind them, or behind a
             hill or a tree), inside the world barrier, so no monster appears right in front of the player when they
             turn around (before, they appeared 10 m away in any direction). If every try is in sight, the one most
             behind the camera.
         Limits: none on Normal and Hard (the monsters the player doesn't kill gather and follow them until dawn, as
         Marko made it). Easy on the NEW GAME page turns two on through GameSetup (SetLimits):
           - Most At Once (20): at most that many night monsters alive together, every kind (Marko's EnemySpawner asks
             RoomForMore() before each spawn);
           - Leave Distance (90 m): one that far from the player, where they can't see it, gives up and is taken away
             quietly (no coins), so ones left far behind don't follow all night.
         Both were every night's rule for a while on 22/11/2026 (to stop a slowdown that turned out to come from
         exploring); Tony found the nights easier that way, so it became Easy.
Requirement: NightMonsters.cs o-> PlayerSight.cs, WorldBarrier.cs, EnemyCombat.cs, EnemyAttack.cs, EnemyAnimator.cs, PlayerController.cs
             NightMonsters.cs <-x EnemySpawner.cs, GameSetup.cs, NightMonsterBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NightMonsters : MonoBehaviour
{
    // One night monster after the first night's: from which night it comes, how many, and how strong.
    [System.Serializable]
    public class Monster
    {
        [Tooltip("Its name, for you and the Console (e.g. Turtle).")]
        public string name = "Turtle";

        [Tooltip("The enemy prefab (an enemy like Tools > Enemies > Build Monster Enemy makes, e.g. TurtleShellEnemy).")]
        public GameObject prefab;

        [Tooltip("The first night it comes (1 = the first night). From then on it comes every night, with the ones before it.")]
        [Min(1)]
        public int firstNight = 2;

        [Tooltip("How much of the night's spawns are this monster on its first night (0.3 = 3 in 10).")]
        [Range(0f, 1f)]
        public float share = 0.3f;

        [Tooltip("Added to Share for every night after its first (0.05 = 5 % more each night).")]
        [Range(0f, 0.5f)]
        public float sharePerNight = 0.05f;

        [Tooltip("The most of it alive at once on its first night (more spawns are the first night's monster then).")]
        [Min(0)]
        public int maxAlive = 3;

        [Tooltip("Added to Max Alive for every night after its first.")]
        [Min(0)]
        public int maxAlivePerNight = 1;

        [Tooltip("Its health on its first night. 0 = the spawner's numbers (50 + 10 for every day passed).")]
        [Min(0f)]
        public float health = 90f;

        [Tooltip("More health for every night after its first.")]
        [Min(0f)]
        public float healthPerNight = 15f;

        [Tooltip("Its attack damage on its first night. 0 = the spawner's numbers (10 + 2 for every day passed).")]
        [Min(0f)]
        public float damage = 12f;

        [Tooltip("More damage for every night after its first.")]
        [Min(0f)]
        public float damagePerNight = 2f;

        [Tooltip("Coins for killing it, times the first night's monster's (the slime drops 3 - 6): 1.5 = half as many " +
                 "again, 2 = twice, 3 = three times. 0 = the same as the slime.")]
        [Min(0f)]
        public float coins = 0f;

        [System.NonSerialized] List<GameObject> alive;   // the ones out now (for Max Alive)

        public List<GameObject> Alive => alive ?? (alive = new List<GameObject>());

        // Nights after its first (0 on its first night).
        public int NightsAfterFirst(int night) => Mathf.Max(0, night - firstNight);

        public float ShareOn(int night) => Mathf.Clamp01(share + sharePerNight * NightsAfterFirst(night));

        public int MaxAliveOn(int night) => maxAlive + maxAlivePerNight * NightsAfterFirst(night);

        public float CoinsTimes => coins > 0f ? coins : 1f;   // 0 = the slime's coins

        public int AliveCount()
        {
            Alive.RemoveAll(enemy => enemy == null);   // killed, or burnt away at dawn
            return Alive.Count;
        }
    }

    // ===== Fields =====
    [Header("Monsters after the first night's")]
    [Tooltip("The EnemySpawner's Enemy Prefab (the slime) comes every night. These join it from their First Night on " +
             "(the turtle from night 2). Add the next night's monster here.")]
    [SerializeField] List<Monster> monsters = new List<Monster>();

    [Tooltip("The most of the night's spawns these monsters take together (0.95 = 95 %: the first night's monster, the " +
             "slime, keeps at least 5 %). Their shares grow every night; once they add up to more, each keeps its part " +
             "of this (turtle 0.5 + chest 0.35 + beholder 0.25 = 1.1 on night 6 -> about 43 / 30 / 22 %). Before " +
             "22/11/2026 the list was read top to bottom, so the last monsters got squeezed out (no beholders by night 8).")]
    [Range(0.5f, 1f)]
    [SerializeField] float mostShare = 0.95f;

    [Header("Limits (off on Normal and Hard: Easy on the NEW GAME page turns them on)")]
    [Tooltip("The most night monsters alive at once, every kind together (slimes too). 0 = no limit: the ones the player " +
             "doesn't kill gather until dawn, as Marko made it (Normal, Hard). Easy sets 20 (NewGamePanel > Easy Most At " +
             "Once). Marko's EnemySpawner asks RoomForMore() before each spawn.")]
    [Range(0, 100)]
    [SerializeField] int mostAtOnce = 0;

    [Tooltip("A night monster farther than this from the player (meters, over the ground), where the player can't see " +
             "it, gives up and is taken away quietly (no coins). 0 = never (Normal, Hard). Easy sets 90 (NewGamePanel > " +
             "Easy Leave Distance).")]
    [Range(0f, 400f)]
    [SerializeField] float leaveDistance = 0f;

    [Header("Where they appear")]
    [Tooltip("The nearest a monster appears to the player, in meters (more than PlayerSight's Notice Radius, 20: closer " +
             "always counts as seen).")]
    [Range(5f, 80f)]
    [SerializeField] float minDistance = 24f;

    [Tooltip("The farthest a monster appears from the player, in meters.")]
    [Range(10f, 120f)]
    [SerializeField] float maxDistance = 34f;

    [Tooltip("On: only spots the player can't see (behind them, or behind a hill or a tree).")]
    [SerializeField] bool outOfSight = true;

    [Tooltip("Spots tried for each monster before taking the one most behind the player.")]
    [Range(1, 40)]
    [SerializeField] int tries = 16;

    const float SpotHeight = 1.5f;   // how tall a monster is taken to be, for "could the player see it there"
    const float LeaveCheckEvery = 2f;   // seconds between two looks for monsters left far behind

    Monster picked;                  // what Pick chose, for Prepare right after
    readonly List<GameObject> spawned = new List<GameObject>();   // every monster the spawner made (for the limits)
    Transform playerBody;            // the player (PlayerController), found once
    float nextLeaveCheck;

    public IReadOnlyList<Monster> Monsters => monsters;
    public int MostAtOnce => mostAtOnce;            // 0 = no limit
    public float LeaveDistance => leaveDistance;    // 0 = never

    // ===== Unity messages (Unity calls these) =====

    // With a Leave Distance (Easy): every 2 s, monsters left far behind, out of sight, give up and are taken away
    // (no coins, nobody sees it).
    void Update()
    {
        if (leaveDistance <= 0f || Time.time < nextLeaveCheck) return;
        nextLeaveCheck = Time.time + LeaveCheckEvery;
        spawned.RemoveAll(enemy => enemy == null);   // killed, or burnt away at dawn
        if (spawned.Count == 0 || Player() == null) return;

        Camera cam = Camera.main;
        float far = leaveDistance * leaveDistance;
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            GameObject enemy = spawned[i];
            Vector3 flat = enemy.transform.position - playerBody.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < far) continue;
            EnemyAnimator look = enemy.GetComponent<EnemyAnimator>();
            if (look != null && (look.IsDead || look.IsVanishing)) continue;   // going anyway
            if (Seen(enemy.transform.position, cam)) continue;                  // never in front of the player
            spawned.RemoveAt(i);
            Destroy(enemy);
        }
    }

    // ===== My methods =====

    // Marko's EnemySpawner asks this before each spawn: false while Most At Once night monsters are out (Easy). With
    // no limit (0: Normal, Hard) always true, so they gather until dawn.
    public bool RoomForMore()
    {
        if (mostAtOnce <= 0) return true;
        spawned.RemoveAll(enemy => enemy == null);   // killed, burnt away at dawn, or given up
        return spawned.Count < mostAtOnce;
    }

    // GameSetup calls this for Easy when the game loads: Most At Once and Leave Distance (0 = off).
    public void SetLimits(int most, float leave)
    {
        mostAtOnce = Mathf.Max(0, most);
        leaveDistance = Mathf.Max(0f, leave);
    }

    // Marko's EnemySpawner asks this for every spawn: which prefab to make. "first" = its Enemy Prefab (the first
    // night's monster, which comes every night). A monster whose First Night has come gets its Share of the spawns
    // while fewer than its Max Alive are out; otherwise it's "first". Shares adding up to more than Most Share are
    // shrunk evenly to fit it, so every monster keeps its part (none is squeezed out by the ones above it in the list).
    public GameObject Pick(GameObject first, int night)
    {
        picked = null;

        float total = 0f;   // tonight's shares together (also of the ones at Max Alive, so the parts don't jump around)
        foreach (Monster monster in monsters)
        {
            if (Comes(monster, night)) total += monster.ShareOn(night);
        }
        float fit = total > mostShare ? mostShare / total : 1f;   // 1 = nothing to shrink (nights 1 - 5)

        float roll = Random.value;
        foreach (Monster monster in monsters)
        {
            if (!Comes(monster, night)) continue;
            if (monster.AliveCount() >= monster.MaxAliveOn(night)) continue;   // its part goes to "first" tonight
            float share = monster.ShareOn(night) * fit;
            if (roll < share)
            {
                picked = monster;
                return monster.prefab;
            }
            roll -= share;
        }
        return first;
    }

    // Has this monster's First Night come (and does it have a prefab)?
    static bool Comes(Monster monster, int night)
    {
        return monster != null && monster.prefab != null && night >= monster.firstNight;
    }

    // Right after the spawn (Marko's EnemySpawner calls it): the picked monster's own health and damage for tonight,
    // and it's counted for Max Alive. The first night's monster keeps the spawner's numbers.
    public void Prepare(GameObject enemy, int night)
    {
        if (enemy != null) spawned.Add(enemy);   // every spawn, the first night's monster too (for the limits)
        Monster monster = picked;
        picked = null;
        if (monster == null || enemy == null) return;

        monster.Alive.Add(enemy);
        int after = monster.NightsAfterFirst(night);
        EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
        if (combat != null && monster.health > 0f)
        {
            combat.maxHealth = monster.health + monster.healthPerNight * after;
            combat.currentHealth = combat.maxHealth;
        }
        if (combat != null) combat.coinMultiplier = monster.CoinsTimes;   // Marko's 3 - 6 coins x this when it dies
        EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
        if (attack != null && monster.damage > 0f) attack.attackDamage = monster.damage + monster.damagePerNight * after;
    }

    // A spot for a monster: Min - Max Distance from the player, on the ground, inside the world barrier, and (Out Of
    // Sight) where the player can't see it. If every try is in sight (open flat land all round), the one most behind
    // the camera. False = no ground found at all (Marko's own spot is used then).
    public bool FindSpot(Transform player, out Vector3 spot)
    {
        spot = Vector3.zero;
        if (player == null) return false;

        float near = Mathf.Min(minDistance, maxDistance);
        float far = Mathf.Max(minDistance, maxDistance);
        Camera cam = Camera.main;
        bool found = false;
        float mostBehind = float.MinValue;
        for (int i = 0; i < tries; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 at = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(near, far);
            if (!WorldBarrier.Inside(at, 2f) || !Ground(ref at)) continue;
            if (!outOfSight || !Seen(at, cam))
            {
                spot = at;
                return true;
            }

            float behind = cam != null ? -Vector3.Dot((at - cam.transform.position).normalized, cam.transform.forward) : 0f;
            if (behind > mostBehind)
            {
                mostBehind = behind;
                spot = at;
                found = true;
            }
        }
        return found;
    }

    // Could the player see a monster standing here? PlayerSight decides (it knows the view, the hills and the trees);
    // without one, anything in front of the camera counts as seen.
    bool Seen(Vector3 feet, Camera cam)
    {
        PlayerSight sight = PlayerSight.Current;
        if (sight != null) return sight.CanSee(feet, SpotHeight);
        if (cam == null) return false;
        Vector3 view = cam.WorldToViewportPoint(feet + Vector3.up * (SpotHeight * 0.5f));
        return view.z > 0f && view.x > -0.1f && view.x < 1.1f && view.y > -0.1f && view.y < 1.1f;
    }

    // The player's body (PlayerController), found the first time it's needed.
    Transform Player()
    {
        if (playerBody == null)
        {
            PlayerController found = FindFirstObjectByType<PlayerController>();
            if (found != null) playerBody = found.transform;
        }
        return playerBody;
    }

    // The ground under a spot (a ray from high above): "at" gets its height. False = no ground there.
    static bool Ground(ref Vector3 at)
    {
        Vector3 from = new Vector3(at.x, at.y + 500f, at.z);
        if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 1000f, Physics.DefaultRaycastLayers,
                             QueryTriggerInteraction.Ignore)) return false;
        at.y = hit.point.y;
        return true;
    }
}
