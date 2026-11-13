/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: An empty GameObject in the game scene. Tools > World > Set Up World Spawning makes it ("AnimalSpawner - DeerNode").
        By hand: Create Empty, add this, drag the animal prefab (DeerNode) into Animal Prefab. The Player needs PlayerSight.
Unity Editor: Testing: Play. The Console says how many animals came at the start. Select this object to see, around
              the player, where animals may appear (yellow: between Min Distance and the spawn reach) and where they're
              taken away (red). ⋮ > Test: Spawn A Herd Now. Try PlayerSight's sight test: new herds appear farther out.
Purpose: Keeps a number of wild animals (deer) around the player, and only ever makes a new one where the player can't
         see it (PlayerSight: behind you, behind a hill or tree trunk, or past your sight range), like Minecraft
         spawning animals out of view. They come in small herds. The spawn reach is at least PlayerSight's range +
         Beyond Sight, so when a perk or buff makes the player see farther, new animals appear farther out by
         themselves. Hunted animals are replaced after a while; animals left far behind (and out of sight) are taken
         away, so the herds follow the player around the map.
Requirement: AnimalSpawner.cs x-> PlayerSight.cs
             AnimalSpawner.cs o-> Terrain (Unity)
             AnimalSpawner.cs <-x WorldSpawnSetup.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AnimalSpawner : MonoBehaviour
{
    // ===== Fields =====
    [Header("What")]
    [Tooltip("The animal to spawn: a prefab with WildAnimal, e.g. Assets/Prefabs/ResourceNodes/DeerNode.")]
    [SerializeField] GameObject animalPrefab;

    [Tooltip("The terrain they live on (its painted textures and slopes). Empty = whichever terrain is under the spot.")]
    [SerializeField] Terrain terrain;

    [Header("How many")]
    [Tooltip("How many animals live around the player at once. The first ones all come when the game starts.")]
    [Min(0)]
    [SerializeField] int population = 15;

    [Tooltip("Fewest animals in one herd (a herd appears together).")]
    [Min(1)]
    [SerializeField] int herdMin = 1;

    [Tooltip("Most animals in one herd.")]
    [Min(1)]
    [SerializeField] int herdMax = 3;

    [Tooltip("Seconds between checks once the game runs: each check takes away animals left far behind and brings one new herd if any are missing.")]
    [Min(0.5f)]
    [SerializeField] float spawnEverySeconds = 10f;

    [Header("Where (around the player)")]
    [Tooltip("Never closer to the player than this (meters), even out of sight.")]
    [Min(0f)]
    [SerializeField] float minDistance = 30f;

    [Tooltip("How far from the player animals may appear (meters). It grows by itself when the player sees farther: never less than the sight range + Beyond Sight.")]
    [Min(1f)]
    [SerializeField] float maxDistance = 150f;

    [Tooltip("How far past the player's sight range animals may still appear (meters).")]
    [Min(0f)]
    [SerializeField] float beyondSight = 40f;

    [Tooltip("Animals farther away than this (meters) and out of sight are taken away. Never less than the spawn reach + 50 m.")]
    [Min(1f)]
    [SerializeField] float despawnDistance = 250f;

    [Header("Ground")]
    [Tooltip("Which painted terrain texture they live on (0 = the first one, the grass).")]
    [Min(0)]
    [SerializeField] int terrainLayer = 0;

    [Tooltip("How strongly that texture must be painted there (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] float minStrength = 0.6f;

    [Tooltip("No spawning on slopes steeper than this (degrees).")]
    [Range(0f, 90f)]
    [SerializeField] float maxSlope = 20f;

    [Tooltip("Free room a new animal needs around it (meters): no tree trunk, rock or wall that close.")]
    [Min(0f)]
    [SerializeField] float clearance = 1.5f;

    [Tooltip("How far a new herd stays from the other animals (meters), so the herds spread out.")]
    [Min(0f)]
    [SerializeField] float herdSpacing = 25f;

    const int Tries = 40;            // random spots tried for one herd before giving up until the next check
    const float HerdRadius = 6f;     // herd members appear within this many meters of the first one

    readonly List<Transform> alive = new List<Transform>();   // the animals this spawner made that still exist
    PlayerSight sight;
    float height = 2f;      // the animal's height, so its head must be out of sight too
    float nextCheck;
    bool filled;            // the first herds came already
    bool warned;            // told the Console that PlayerSight is missing
    static readonly Collider[] overlaps = new Collider[8];

    // How many of its animals are alive now.
    public int Alive => alive.Count;
    public GameObject AnimalPrefab => animalPrefab;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        height = MeasureHeight();
        nextCheck = Time.time + 0.5f;   // after the camera has moved to the player, so "out of sight" is right
    }

    void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + spawnEverySeconds;
        if (animalPrefab == null || !FindSight()) return;

        RemoveLost();
        if (!filled)
        {
            filled = true;
            while (alive.Count < population && SpawnHerd()) { }
            Debug.Log(name + ": " + alive.Count + " " + animalPrefab.name + " around the player, all out of sight.");
        }
        else if (alive.Count < population)
        {
            SpawnHerd();
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

    [ContextMenu("Test: Spawn A Herd Now")]
    void TestSpawn()
    {
        if (!Application.isPlaying || animalPrefab == null || !FindSight())
        {
            Debug.Log("Press Play first (and set Animal Prefab).");
            return;
        }
        Debug.Log(SpawnHerd() ? name + ": a herd appeared out of sight." : name + ": no free spot out of sight found, try again.");
    }

    // ===== My methods =====

    // One herd at a spot the player can't see. False = no such spot found this time.
    bool SpawnHerd()
    {
        if (!FindSpot(out Vector3 leader)) return false;
        int size = Mathf.Min(Random.Range(herdMin, Mathf.Max(herdMin, herdMax) + 1), Mathf.Max(1, population - alive.Count));
        Spawn(leader);
        for (int i = 1; i < size; i++)
        {
            for (int t = 0; t < 8; t++)
            {
                Vector2 r = Random.insideUnitCircle * HerdRadius;
                if (!Usable(leader + new Vector3(r.x, 0f, r.y), out Vector3 feet)) continue;
                Spawn(feet);
                break;
            }
        }
        return true;
    }

    // A random spot around the player, between Min Distance and the spawn reach, that is free and out of sight.
    bool FindSpot(out Vector3 feet)
    {
        Vector3 player = sight.transform.position;
        float reach = Reach(sight);
        float near = Mathf.Min(minDistance, reach * 0.9f);
        for (int t = 0; t < Tries; t++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Mathf.Sqrt(Random.Range(near * near, reach * reach));   // spread evenly over the ring, not bunched near the player
            Vector3 spot = player + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            if (Usable(spot, out feet) && !NearOtherAnimals(feet)) return true;
        }
        feet = Vector3.zero;
        return false;
    }

    // Good ground (the right texture, not steep), free room, not too close to the player, and out of the player's sight.
    bool Usable(Vector3 spot, out Vector3 feet)
    {
        feet = spot;
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

    // True if a tree trunk, rock, wall or another animal is within Clearance of the spot (the ground itself doesn't count).
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

    bool NearOtherAnimals(Vector3 feet)
    {
        foreach (Transform animal in alive)
        {
            if (animal == null) continue;
            Vector3 d = animal.position - feet;
            d.y = 0f;
            if (d.magnitude < herdSpacing) return true;
        }
        return false;
    }

    void Spawn(Vector3 feet)
    {
        GameObject animal = Instantiate(animalPrefab, feet, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
        alive.Add(animal.transform);
    }

    // Forgets hunted animals (ResourceNode destroys them) and takes away the ones left far behind, out of sight.
    void RemoveLost()
    {
        Vector3 player = sight.transform.position;
        float far = DespawnReach(sight);
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            Transform animal = alive[i];
            if (animal == null)
            {
                alive.RemoveAt(i);
                continue;
            }
            Vector3 d = animal.position - player;
            d.y = 0f;
            if (d.magnitude <= far || sight.CanSee(animal.position, height, animal)) continue;
            Destroy(animal.gameObject);
            alive.RemoveAt(i);
        }
    }

    // How far from the player animals may appear: Max Distance, or more when the player sees farther.
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
        if (terrain != null && Inside(terrain, spot)) return terrain;
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (Inside(t, spot)) return t;
        }
        return null;
    }

    static bool Inside(Terrain t, Vector3 spot)
    {
        Vector3 origin = t.transform.position;
        Vector3 size = t.terrainData.size;
        return spot.x >= origin.x && spot.x <= origin.x + size.x && spot.z >= origin.z && spot.z <= origin.z + size.z;
    }

    // The animal's height from its BoxCollider (DeerNode's covers its whole body), else 2 m.
    float MeasureHeight()
    {
        BoxCollider box = animalPrefab != null ? animalPrefab.GetComponent<BoxCollider>() : null;
        if (box == null) return 2f;
        return Mathf.Max(0.5f, (box.center.y + box.size.y * 0.5f) * animalPrefab.transform.localScale.y);
    }

    bool FindSight()
    {
        if (sight == null) sight = PlayerSight.Current != null ? PlayerSight.Current : FindFirstObjectByType<PlayerSight>();
        if (sight != null) return true;
        if (!warned)
        {
            Debug.LogWarning(name + ": the Player has no PlayerSight, so no animals spawn. Drag PlayerSight onto the Player " +
                             "(or run Tools > World > Set Up World Spawning).");
            warned = true;
        }
        return false;
    }
}
