/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: An empty GameObject in the new world (ProceduralGenerateScene). Tools > Enemies > Build Human Enemies makes it
        ("Human Spawner") and fills in Kinds. The Player needs PlayerSight. It only works with the World Generator
        (the new world): in the old world it does nothing.
Unity Editor: Kinds = the human types and where they live (Bodies = a man and a woman prefab; Grassland / Hills /
              Desert / Snow = the biomes it appears in). Gear By Area = how rich the humans of each biome are (none,
              copper, iron, gold, diamond: weights). Height Variation = how much taller or shorter than its prefab
              each human may be.
              Testing: Play in the new world. The Console says how many humans came. Select this object to see where
              they may appear (yellow rings around the player) and where they're taken away (red). ⋮ > Test: Spawn One
              Now (out of sight, so turn around).
Purpose: Keeps a few human enemies around the player in the new world, like the animals: only where the player can't
         see (PlayerSight), never too close, never on steep ground or right against a trunk or rock. The biome under
         the spot decides which types may come (the plan: grassland types 1 - 3, hills 2 - 3, desert and snow 1 - 5;
         so far only Type 1 exists) and how rich they are (HumanGear.Dress with that area's odds: the grassland is mostly
         poor with a rare diamond, the snow mostly gold and diamond). New ones only come in the daytime (Only In Day).
         Ones left far behind and out of sight are taken away (no loot: nobody killed them), so they follow the player
         around the endless world. Each new human is a little taller or shorter than its prefab (Height Variation:
         people aren't all the same height), most of them near the middle.
Requirement: HumanSpawner.cs x-> PlayerSight.cs, WorldGenerator.cs, HumanGear.cs
             HumanSpawner.cs o-> DaylightCycle.cs, Terrain (Unity), CapsuleCollider (Unity)
             HumanSpawner.cs <-x HumanEnemyBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HumanSpawner : MonoBehaviour
{
    // One human type: its prefabs and where it lives.
    [System.Serializable]
    public class Kind
    {
        [Tooltip("Its name, for you and the Console (e.g. Type 1).")]
        public string name = "Type 1";

        [Tooltip("Its prefabs (a man and a woman, made by the tool): one is picked at random for each new human.")]
        public List<GameObject> bodies = new List<GameObject>();

        [Tooltip("How often it's picked compared with the other types that live in the same biome.")]
        [Min(0f)]
        public float weight = 1f;

        [Tooltip("The biomes it lives in.")]
        public bool grassland = true;
        public bool hills;
        public bool desert = true;
        public bool snow = true;

        [Tooltip("How many come together (1 = a loner; a group stays near each other).")]
        [Min(1)]
        public int groupMin = 1;
        [Min(1)]
        public int groupMax = 1;

        public bool LivesIn(WorldGenerator.Biome biome)
        {
            switch (biome)
            {
                case WorldGenerator.Biome.Grassland: return grassland;
                case WorldGenerator.Biome.Hills: return hills;
                case WorldGenerator.Biome.Desert: return desert;
                default: return snow;
            }
        }
    }

    // ===== Fields =====
    [Header("Who")]
    [Tooltip("The human types (the tool adds them).")]
    [SerializeField] List<Kind> kinds = new List<Kind>();

    [Header("Gear by area (weights: none, copper, iron, gold, diamond)")]
    [Tooltip("The grassland: mostly poor, iron sometimes, gold and diamond rarely.")]
    [SerializeField] HumanGear.TierOdds grassland = new HumanGear.TierOdds(40f, 40f, 15f, 4f, 1f);
    [Tooltip("The rocky hills: copper and iron.")]
    [SerializeField] HumanGear.TierOdds hills = new HumanGear.TierOdds(25f, 40f, 30f, 4f, 1f);
    [Tooltip("The desert (its caves have iron and gold): mostly iron and gold.")]
    [SerializeField] HumanGear.TierOdds desert = new HumanGear.TierOdds(10f, 15f, 45f, 25f, 5f);
    [Tooltip("The snow (its caves have gold and diamond): mostly gold and diamond.")]
    [SerializeField] HumanGear.TierOdds snow = new HumanGear.TierOdds(5f, 5f, 25f, 40f, 25f);

    [Header("Looks")]
    [Range(0f, 0.25f)]
    [Tooltip("Each new human is up to this much taller or shorter than its prefab (0.06 = 6 %: about 11 cm on a 1.8 m human), most of them near the middle. 0 = all the same.")]
    [SerializeField] float heightVariation = 0.06f;

    [Header("How many")]
    [Tooltip("How many humans live around the player at once (each one is a few drawn pieces: keep it small for the web build).")]
    [Min(0)]
    [SerializeField] int population = 6;

    [Tooltip("Seconds between checks: each check takes away humans left far behind and brings one new loner or group if some are missing.")]
    [Min(0.5f)]
    [SerializeField] float spawnEverySeconds = 8f;

    [Tooltip("On: new humans only come in the daytime.")]
    [SerializeField] bool onlyInDay = true;

    [Header("Where (around the player)")]
    [Tooltip("Never closer to the player than this (meters), even out of sight.")]
    [Min(0f)]
    [SerializeField] float minDistance = 35f;

    [Tooltip("How far from the player they may appear (meters). Never less than the sight range + Beyond Sight.")]
    [Min(1f)]
    [SerializeField] float maxDistance = 110f;

    [Tooltip("How far past the player's sight range they may still appear (meters).")]
    [Min(0f)]
    [SerializeField] float beyondSight = 30f;

    [Tooltip("Humans farther away than this (meters) and out of sight are taken away. Never less than the spawn reach + 50 m.")]
    [Min(1f)]
    [SerializeField] float despawnDistance = 220f;

    [Tooltip("How far a new loner or group stays from the other humans (meters).")]
    [Min(0f)]
    [SerializeField] float spacing = 30f;

    [Header("Ground")]
    [Tooltip("No spawning on slopes steeper than this (degrees).")]
    [Range(0f, 90f)]
    [SerializeField] float maxSlope = 25f;

    [Tooltip("Free room a new human needs around it (meters): no trunk, rock or wall that close.")]
    [Min(0f)]
    [SerializeField] float clearance = 1.5f;

    const int Tries = 30;             // random spots tried before giving up until the next check
    const float GroupRadius = 5f;     // a group's members appear within this many meters of the first one
    const float DefaultHeight = 1.9f; // a human's height when its prefab has no capsule to measure

    readonly List<Transform> alive = new List<Transform>();   // the humans this spawner made that still exist
    PlayerSight sight;
    float nextCheck;
    bool filled;            // the first humans came already
    bool warnedSight;       // told the Console that PlayerSight is missing
    float tallest = DefaultHeight;   // the tallest human that may come, with Height Variation: its head must be out of sight too
    bool warnedWorld;       // told the Console that this isn't the new world
    static readonly Collider[] overlaps = new Collider[8];

    public int Alive => alive.Count;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        nextCheck = Time.time + 0.5f;   // after the World Generator has made the start and the camera is at the player
    }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + spawnEverySeconds;
        if (!InNewWorld() || !FindSight()) return;

        RemoveLost();
        if (onlyInDay && DaylightCycle.Current != null && DaylightCycle.Current.IsNight) return;
        if (!filled)
        {
            filled = true;
            while (alive.Count < population && SpawnGroup()) { }
            Debug.Log(name + ": " + alive.Count + " human(s) around the player, all out of sight.");
        }
        else if (alive.Count < population)
        {
            SpawnGroup();
        }
    }

    void OnDrawGizmosSelected()
    {
        PlayerSight s = sight != null ? sight : FindFirstObjectByType<PlayerSight>();
        if (s == null) return;
        Vector3 player = s.transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player, minDistance);
        Gizmos.DrawWireSphere(player, Reach(s));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player, DespawnReach(s));
    }

    [ContextMenu("Test: Spawn One Now")]
    void TestSpawn()
    {
        if (!Application.isPlaying || !InNewWorld() || !FindSight())
        {
            Debug.Log("Press Play in the new world first.");
            return;
        }
        Debug.Log(SpawnGroup() ? name + ": a human appeared out of sight." : name + ": no free spot out of sight found, try again.");
    }

    // ===== My methods =====

    // One loner or group at a spot the player can't see, of a type that lives in that biome. False = none this time.
    bool SpawnGroup()
    {
        tallest = TallestHuman();
        if (!FindSpot(out Vector3 leader, out WorldGenerator.Biome biome)) return false;
        Kind kind = PickKind(biome);
        if (kind == null) return false;   // no type lives in this biome (yet)

        int size = Random.Range(kind.groupMin, Mathf.Max(kind.groupMin, kind.groupMax) + 1);
        size = Mathf.Min(size, Mathf.Max(1, population - alive.Count));
        HumanGear.TierOdds odds = OddsFor(biome);
        Spawn(kind, leader, odds);
        for (int i = 1; i < size; i++)
        {
            for (int t = 0; t < 8; t++)
            {
                Vector2 r = Random.insideUnitCircle * GroupRadius;
                if (!Usable(leader + new Vector3(r.x, 0f, r.y), out Vector3 feet)) continue;
                Spawn(kind, feet, odds);
                break;
            }
        }
        return true;
    }

    // A random type that lives in this biome, by weight (null = none does).
    Kind PickKind(WorldGenerator.Biome biome)
    {
        float total = 0f;
        foreach (Kind k in kinds)
        {
            if (Ready(k) && k.LivesIn(biome)) total += k.weight;
        }
        if (total <= 0f) return null;
        float roll = Random.value * total;
        foreach (Kind k in kinds)
        {
            if (!Ready(k) || !k.LivesIn(biome)) continue;
            roll -= k.weight;
            if (roll < 0f) return k;
        }
        return null;
    }

    static bool Ready(Kind k) => k != null && k.weight > 0f && k.bodies.Exists(b => b != null);

    HumanGear.TierOdds OddsFor(WorldGenerator.Biome biome)
    {
        switch (biome)
        {
            case WorldGenerator.Biome.Grassland: return grassland;
            case WorldGenerator.Biome.Hills: return hills;
            case WorldGenerator.Biome.Desert: return desert;
            default: return snow;
        }
    }

    // A human of this type (a man or a woman), dressed for its area at once (before anything on it starts).
    void Spawn(Kind kind, Vector3 feet, HumanGear.TierOdds odds)
    {
        List<GameObject> ready = kind.bodies.FindAll(b => b != null);
        GameObject prefab = ready[Random.Range(0, ready.Count)];
        GameObject human = Instantiate(prefab, feet + Vector3.up * 0.05f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        if (heightVariation > 0f)   // a bit taller or shorter (two random numbers added: most land near the middle)
            human.transform.localScale = prefab.transform.localScale * (1f + heightVariation * (Random.value + Random.value - 1f));
        HumanGear gear = human.GetComponent<HumanGear>();
        if (gear != null) gear.Dress(odds);
        alive.Add(human.transform);
    }

    // A random spot around the player, between Min Distance and the spawn reach, that is free and out of sight.
    bool FindSpot(out Vector3 feet, out WorldGenerator.Biome biome)
    {
        Vector3 player = sight.transform.position;
        float reach = Reach(sight);
        float near = Mathf.Min(minDistance, reach * 0.9f);
        for (int t = 0; t < Tries; t++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(Random.Range(near * near, reach * reach));   // spread evenly over the ring
            Vector3 spot = player + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (!Usable(spot, out feet) || NearOthers(feet)) continue;
            biome = WorldGenerator.Current.BiomeAt(feet);
            return true;
        }
        feet = Vector3.zero;
        biome = WorldGenerator.Biome.Grassland;
        return false;
    }

    // On the land that's shown, not steep, free room, not too close to the player, and out of the player's sight.
    bool Usable(Vector3 spot, out Vector3 feet)
    {
        feet = spot;
        Terrain t = TerrainAt(spot);
        if (t == null) return false;
        TerrainData data = t.terrainData;
        Vector3 origin = t.transform.position;
        float nx = (spot.x - origin.x) / data.size.x;   // 0-1 across the terrain
        float nz = (spot.z - origin.z) / data.size.z;
        if (data.GetSteepness(nx, nz) > maxSlope) return false;

        feet = new Vector3(spot.x, t.SampleHeight(spot) + origin.y, spot.z);
        Vector3 fromPlayer = feet - sight.transform.position;
        fromPlayer.y = 0f;
        if (fromPlayer.magnitude < minDistance) return false;
        if (Crowded(feet)) return false;
        return !sight.CanSee(feet, tallest);
    }

    // True if a trunk, rock, wall or another body is within Clearance of the spot (the ground itself doesn't count).
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

    bool NearOthers(Vector3 feet)
    {
        foreach (Transform human in alive)
        {
            if (human == null) continue;
            Vector3 d = human.position - feet;
            d.y = 0f;
            if (d.magnitude < spacing) return true;
        }
        return false;
    }

    // Forgets killed humans and takes away the ones left far behind, out of sight (no loot: nobody killed them).
    void RemoveLost()
    {
        Vector3 player = sight.transform.position;
        float far = DespawnReach(sight);
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            Transform human = alive[i];
            if (human == null)
            {
                alive.RemoveAt(i);
                continue;
            }
            Vector3 d = human.position - player;
            d.y = 0f;
            if (d.magnitude <= far || sight.CanSee(human.position, HeightOf(human), human)) continue;
            Destroy(human.gameObject);
            alive.RemoveAt(i);
        }
    }

    float Reach(PlayerSight s) => Mathf.Max(maxDistance, s.Range + beyondSight);

    // The tallest prefab of every type, made as tall as Height Variation allows.
    float TallestHuman()
    {
        float most = 0f;
        foreach (Kind k in kinds)
        {
            if (k == null) continue;
            foreach (GameObject body in k.bodies)
            {
                if (body != null) most = Mathf.Max(most, HeightOf(body.transform));
            }
        }
        return (most > 0f ? most : DefaultHeight) * (1f + heightVariation);
    }

    // How tall a human (or a human prefab) is: its capsule's height times its size.
    static float HeightOf(Transform human)
    {
        CapsuleCollider shape = human.GetComponent<CapsuleCollider>();
        return shape != null ? shape.height * human.lossyScale.y : DefaultHeight;
    }

    float DespawnReach(PlayerSight s) => Mathf.Max(despawnDistance, Reach(s) + 50f);

    // The shown land under this spot (the World Generator's chunks near the player). Null = none there.
    static Terrain TerrainAt(Vector3 spot)
    {
        foreach (Terrain t in Terrain.activeTerrains)
        {
            Vector3 origin = t.transform.position;
            Vector3 size = t.terrainData.size;
            if (spot.x >= origin.x && spot.x <= origin.x + size.x && spot.z >= origin.z && spot.z <= origin.z + size.z) return t;
        }
        return null;
    }

    // Only the new world (the World Generator tells the biomes); says so once otherwise.
    bool InNewWorld()
    {
        if (WorldGenerator.Current != null) return true;
        if (!warnedWorld)
        {
            Debug.Log(name + ": no World Generator in this scene, so no human enemies (they live in the new world only).");
            warnedWorld = true;
        }
        return false;
    }

    bool FindSight()
    {
        if (sight == null) sight = PlayerSight.Current != null ? PlayerSight.Current : FindFirstObjectByType<PlayerSight>();
        if (sight != null) return true;
        if (!warnedSight)
        {
            Debug.LogWarning(name + ": the Player has no PlayerSight, so no humans spawn. Drag PlayerSight onto the Player " +
                             "(or run Tools > World > Set Up World Spawning).");
            warnedSight = true;
        }
        return false;
    }
}
