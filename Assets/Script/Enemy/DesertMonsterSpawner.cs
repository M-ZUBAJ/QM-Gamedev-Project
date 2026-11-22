/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: Not used since 21/11/2026: the turtle is a night monster now (NightMonsters, Tools > Enemies > Set Up Night
        Monsters removes the new world's spawner). Kept for a future day monster.
        An empty object, e.g. "Desert Monster Spawner" (Tools > World > Build New World Scene used to put one in the new world).
        By hand: Create Empty, add this, drag the monster (Assets/Prefabs/Enemy/TurtleShellEnemy) into Monster Prefab
        and set Terrain Layer to the sand texture's number (its place in the terrain's Paint Texture list). The Player
        needs PlayerSight.
Unity Editor: Testing: Play the new world in the day and walk into the desert: turtles appear out of sight (never in
              front of you), stand still, and come for you when you get close (EnemyAggro on the monster: Notice
              Range). At night they turn to sand and are gone; the next morning they come back.
              ⋮ (top right of this component) > Test: Spawn One Now.
Purpose: The desert's day monsters: keeps up to Population of them on the sand around the player while it's day
         (DaylightCycle), the opposite of Marko's night slimes:
           - a new one only appears on the chosen terrain texture (the sand), not too steep, inside the world's barrier,
             between Min Distance and the spawn reach from the player, and where the player can't see it (PlayerSight:
             behind them, behind a dune or past the mist), like the deer;
           - it gets Health and Damage (more each day, like the night's enemies), and the player as its target if it
             has no EnemyAggro (then it waits until the player comes near);
           - monsters left far behind (and out of sight) are taken away, so they follow the player around the desert;
           - at night (Leave At Night) each one dissolves where it stands (EnemyAnimator.Vanish: its DeathEffect's Vanish
             Color, sand for the turtle), with no coins, and none come until morning.
Requirement: DesertMonsterSpawner.cs x-> PlayerSight.cs
             DesertMonsterSpawner.cs o-> DaylightCycle.cs, EnemyCombat.cs, EnemyAttack.cs, EnemyMovement.cs, EnemyAggro.cs, EnemyAnimator.cs, WorldBarrier.cs, Terrain (Unity)
             DesertMonsterSpawner.cs <-x NightMonsterBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DesertMonsterSpawner : MonoBehaviour
{
    // ===== Fields =====
    [Header("What")]
    [Tooltip("The monster to spawn: an enemy prefab, e.g. Assets/Prefabs/Enemy/TurtleShellEnemy (Tools > World > Build " +
             "Desert Content).")]
    [SerializeField] GameObject monsterPrefab;

    [Tooltip("The terrain they live on (its painted textures and slopes). Empty = whichever terrain is under the spot.")]
    [SerializeField] Terrain terrain;

    [Header("How many")]
    [Tooltip("How many live around the player at once (only in the desert, only in the day).")]
    [Min(0)]
    [SerializeField] int population = 5;

    [Tooltip("Seconds between checks: each check takes away monsters left far behind and brings one new one if any are " +
             "missing.")]
    [Min(0.5f)]
    [SerializeField] float spawnEverySeconds = 6f;

    [Header("Where (around the player)")]
    [Tooltip("Which painted terrain texture they live on (its number in the terrain's Paint Texture list: the sand).")]
    [Min(0)]
    [SerializeField] int terrainLayer = 1;

    [Tooltip("How strongly that texture must be painted there (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] float minStrength = 0.6f;

    [Tooltip("No spawning on slopes steeper than this (degrees).")]
    [Range(0f, 90f)]
    [SerializeField] float maxSlope = 30f;

    [Tooltip("Never closer to the player than this (meters), even out of sight.")]
    [Min(0f)]
    [SerializeField] float minDistance = 25f;

    [Tooltip("How far from the player they may appear (meters). Never less than the player's sight range + Beyond Sight.")]
    [Min(1f)]
    [SerializeField] float maxDistance = 90f;

    [Tooltip("How far past the player's sight range they may still appear (meters).")]
    [Min(0f)]
    [SerializeField] float beyondSight = 25f;

    [Tooltip("Monsters farther away than this (meters) and out of sight are taken away. Never less than the spawn reach + " +
             "50 m.")]
    [Min(1f)]
    [SerializeField] float despawnDistance = 170f;

    [Tooltip("Free room a new monster needs around it (meters): no rock, cave wall or other monster that close.")]
    [Min(0f)]
    [SerializeField] float clearance = 1.5f;

    [Header("When")]
    [Tooltip("On: they only appear in the day (DaylightCycle). Off: day and night.")]
    [SerializeField] bool onlyInDay = true;

    [Tooltip("On: when night comes they dissolve where they stand (no coins), and come back the next day.")]
    [SerializeField] bool leaveAtNight = true;

    [Header("How strong (on day 1, more each day)")]
    [Tooltip("Health on day 1 (the night slimes have 50).")]
    [Min(1f)]
    [SerializeField] float health = 90f;

    [Tooltip("Health added each day after the first.")]
    [Min(0f)]
    [SerializeField] float healthPerDay = 10f;

    [Tooltip("Damage per attack on day 1 (the night slimes do 10).")]
    [Min(0f)]
    [SerializeField] float damage = 12f;

    [Tooltip("Damage added each day after the first.")]
    [Min(0f)]
    [SerializeField] float damagePerDay = 2f;

    const int Tries = 30;   // random spots tried for one monster before giving up until the next check

    readonly List<Transform> alive = new List<Transform>();   // the monsters this spawner made that still exist
    PlayerSight sight;
    float height = 1f;      // the monster's height, so its top must be out of sight too
    float nextCheck;
    bool warned;            // told the Console that PlayerSight is missing
    static readonly Collider[] overlaps = new Collider[8];

    // How many of its monsters are alive now.
    public int Alive => alive.Count;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        height = MeasureHeight();
        nextCheck = Time.time + 1f;   // after the world is made and the camera is at the player
    }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + spawnEverySeconds;
        if (monsterPrefab == null || !FindSight()) return;

        bool night = DaylightCycle.Current != null && DaylightCycle.Current.IsNight;
        if (night && leaveAtNight)
        {
            LeaveAll();
            return;
        }
        RemoveLost();
        if ((!onlyInDay || !night) && alive.Count < population) SpawnOne();
    }

    void OnDrawGizmosSelected()
    {
        PlayerSight s = sight != null ? sight : FindFirstObjectByType<PlayerSight>();
        if (s == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(s.transform.position, minDistance);
        Gizmos.DrawWireSphere(s.transform.position, Reach(s));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(s.transform.position, DespawnReach(s));
    }

    [ContextMenu("Test: Spawn One Now")]
    void TestSpawn()
    {
        if (!Application.isPlaying || monsterPrefab == null || !FindSight())
        {
            Debug.Log("Press Play first (and set Monster Prefab).");
            return;
        }
        Debug.Log(SpawnOne() ? name + ": a monster appeared out of sight." : name + ": no free sand spot out of sight found (go into the desert).");
    }

    // ===== My methods =====

    // One monster at a sand spot the player can't see. False = no such spot found this time.
    bool SpawnOne()
    {
        Vector3 player = sight.transform.position;
        float reach = Reach(sight);
        float near = Mathf.Min(minDistance, reach * 0.9f);
        for (int t = 0; t < Tries; t++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(Random.Range(near * near, reach * reach));   // spread evenly over the ring
            Vector3 spot = player + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (!Usable(spot, out Vector3 feet)) continue;
            Spawn(feet);
            return true;
        }
        return false;
    }

    // The right texture, not steep, inside the barrier, free room, not too close to the player, out of their sight.
    bool Usable(Vector3 spot, out Vector3 feet)
    {
        feet = spot;
        if (!WorldBarrier.Inside(spot, 3f)) return false;
        Terrain t = TerrainAt(spot);
        if (t == null) return false;
        TerrainData data = t.terrainData;
        Vector3 origin = t.transform.position;
        float nx = (spot.x - origin.x) / data.size.x;   // 0-1 across the terrain
        float nz = (spot.z - origin.z) / data.size.z;
        if (LayerStrength(data, nx, nz) < minStrength) return false;
        if (data.GetSteepness(nx, nz) > maxSlope) return false;

        feet = new Vector3(spot.x, t.SampleHeight(spot) + origin.y, spot.z);
        Vector3 fromPlayer = feet - sight.transform.position;
        fromPlayer.y = 0f;
        if (fromPlayer.magnitude < minDistance) return false;
        if (Crowded(feet)) return false;
        return !sight.CanSee(feet, height);
    }

    // The monster, as strong as the day asks, chasing the player at once unless it has EnemyAggro (then it waits).
    void Spawn(Vector3 feet)
    {
        GameObject monster = Instantiate(monsterPrefab, feet + Vector3.up * 0.1f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        alive.Add(monster.transform);

        int daysPassed = DaylightCycle.Current != null ? Mathf.Max(0, DaylightCycle.Current.Day - 1) : 0;
        EnemyCombat combat = monster.GetComponent<EnemyCombat>();
        if (combat != null)
        {
            combat.maxHealth = health + healthPerDay * daysPassed;
            combat.currentHealth = combat.maxHealth;
        }
        EnemyAttack attack = monster.GetComponent<EnemyAttack>();
        if (attack != null) attack.attackDamage = damage + damagePerDay * daysPassed;

        if (monster.GetComponent<EnemyAggro>() != null) return;   // it finds the player itself when they come near
        Transform player = sight.transform;
        EnemyMovement movement = monster.GetComponent<EnemyMovement>();
        if (movement != null) movement.target = player;
        if (attack != null) attack.target = player;
    }

    // Night: every monster dissolves where it stands (no coins), and the list starts empty for the next day.
    void LeaveAll()
    {
        foreach (Transform monster in alive)
        {
            if (monster != null) EnemyAnimator.Vanish(monster.gameObject);
        }
        alive.Clear();
    }

    // Forgets killed monsters and takes away the ones left far behind, out of sight.
    void RemoveLost()
    {
        Vector3 player = sight.transform.position;
        float far = DespawnReach(sight);
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            Transform monster = alive[i];
            if (monster == null)
            {
                alive.RemoveAt(i);
                continue;
            }
            Vector3 d = monster.position - player;
            d.y = 0f;
            if (d.magnitude <= far || sight.CanSee(monster.position, height, monster)) continue;
            Destroy(monster.gameObject);
            alive.RemoveAt(i);
        }
    }

    // True if a rock, cave wall or another monster is within Clearance of the spot (the ground itself doesn't count).
    bool Crowded(Vector3 feet)
    {
        if (clearance <= 0f) return false;
        int count = Physics.OverlapSphereNonAlloc(feet + Vector3.up * (clearance + 0.3f), clearance, overlaps, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (!(overlaps[i] is TerrainCollider)) return true;
        }
        return false;
    }

    // How far from the player monsters may appear: Max Distance, or more when the player sees farther.
    float Reach(PlayerSight s)
    {
        return Mathf.Max(maxDistance, s.Range + beyondSight);
    }

    float DespawnReach(PlayerSight s)
    {
        return Mathf.Max(despawnDistance, Reach(s) + 50f);
    }

    // How strongly the chosen texture is painted at this spot (0 = not at all, 1 = fully). nx, nz = 0-1 across the terrain.
    float LayerStrength(TerrainData data, float nx, float nz)
    {
        if (terrainLayer >= data.alphamapLayers) return 0f;
        int x = Mathf.Clamp((int)(nx * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int z = Mathf.Clamp((int)(nz * data.alphamapHeight), 0, data.alphamapHeight - 1);
        return data.GetAlphamaps(x, z, 1, 1)[0, 0, terrainLayer];
    }

    // The terrain under this spot (null = off every terrain).
    Terrain TerrainAt(Vector3 spot)
    {
        if (terrain != null && Covers(terrain, spot)) return terrain;
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (Covers(t, spot)) return t;
        }
        return null;
    }

    static bool Covers(Terrain t, Vector3 spot)
    {
        Vector3 origin = t.transform.position;
        Vector3 size = t.terrainData.size;
        return spot.x >= origin.x && spot.x <= origin.x + size.x && spot.z >= origin.z && spot.z <= origin.z + size.z;
    }

    // The monster's height from its collider (the turtle's Sphere Collider), else 1 m.
    float MeasureHeight()
    {
        Collider body = monsterPrefab != null ? monsterPrefab.GetComponent<Collider>() : null;
        if (body is SphereCollider ball) return Mathf.Max(0.5f, (ball.center.y + ball.radius) * monsterPrefab.transform.localScale.y);
        if (body is BoxCollider box) return Mathf.Max(0.5f, (box.center.y + box.size.y * 0.5f) * monsterPrefab.transform.localScale.y);
        return 1f;
    }

    bool FindSight()
    {
        if (sight == null) sight = PlayerSight.Current != null ? PlayerSight.Current : FindFirstObjectByType<PlayerSight>();
        if (sight != null) return true;
        if (!warned)
        {
            Debug.LogWarning(name + ": the Player has no PlayerSight, so no desert monsters spawn. Drag PlayerSight onto the " +
                             "Player (or run Tools > World > Set Up World Spawning).");
            warned = true;
        }
        return false;
    }
}
