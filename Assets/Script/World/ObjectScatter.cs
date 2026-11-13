
//Ask Sanyam

// [Tony] edited 13/11/2026 (the team agreed Tony may change it when a task needs it). Why: with 1000+ trees, Min Spacing
// was kept from EVERYTHING already placed, so the rules after the trees (berry bushes, forest rocks, deer) found almost no
// spot (deer: 0 of 40). Now Min Spacing counts only inside a Spacing Group and other kinds keep the small Clearance.
// Also added: Patch Size / Open Ground (forests with meadows between), Min / Max Size (random sizes), and the used spots
// kept in 10 m squares so the checks stay fast. Every change is marked [Tony]; replaced code is kept in comments.
// Log: Docs/TonyEditMate/Sanyam/13-11-2026 Scatter Spacing Groups And Forest Patches.md


using UnityEngine;
using System.Collections.Generic;

public class ObjectScatter : MonoBehaviour
{
    // All the settings for one type of object. Shows up as a list in the Inspector.
    [System.Serializable]
    public class ScatterRule
    {
        public string name = "New Rule";  // just a label, e.g. "Trees"
        public GameObject prefab;         // what to spawn
        public int count = 100;           // how many to spawn
        public float yOffset = 0f;        // lift above the ground, for prefabs whose pivot is in the middle

        [Header("Biome")]
        public int terrainLayer = 0;                     // which painted texture to spawn on (0 = top layer)
        [Range(0f, 1f)] public float minStrength = 0.6f; // how strongly it must be painted there (0-1)

        [Header("Slope")]
        public float maxSlope = 25f;      // skip spots steeper than this (in degrees)

        [Header("Spacing")]
        // public float minSpacing = 8f;     // minimum distance from anything already placed   // [Tony] edited: the comment (it's only the same group now)
        public float minSpacing = 8f;     // minimum distance from things of the same Spacing Group already placed   // [Tony] edited

        // [Tony] added (13/11/2026): every setting below.
        [Tooltip("Rules with the same group keep Min Spacing from each other, e.g. every tree look = \"Trees\". Empty = the prefab's name (rules spawning the same prefab are one group).")]
        public string spacingGroup = "";  // [Tony] added
        [Tooltip("Meters kept from things of OTHER groups (a rock not inside a tree trunk). Keep it small, so many trees still leave room for rocks and bushes.")]
        public float clearance = 3f;      // [Tony] added

        [Header("Patches")]               // [Tony] added
        [Tooltip("Meters across one patch: things grow in patches (forests) with open ground (meadows) between. 0 = spread evenly everywhere.")]
        public float patchSize = 0f;      // [Tony] added
        [Tooltip("About how much of the biome stays open between the patches (0 = almost none, 1 = almost all). Only used with a Patch Size.")]
        [Range(0f, 1f)] public float openGround = 0.35f;   // [Tony] added

        [Header("Size")]                  // [Tony] added
        [Tooltip("Each one is scaled by a random number between Min Size and Max Size (1 = the prefab's own size, 0 = not set: 1).")]
        public float minSize = 1f;        // [Tony] added
        [Tooltip("See Min Size. E.g. 0.85 and 1.2 for trees that aren't all the same.")]
        public float maxSize = 1f;        // [Tony] added
    }

    public Terrain terrain;                                 // the terrain to place things on
    public List<ScatterRule> rules = new List<ScatterRule>(); // one entry per object type

    // List<Vector3> placed = new List<Vector3>();   // every spot used so far, shared by all rules   // [Tony] replaced by the squares below

    // [Tony] added: every spot used so far (shared by all rules) with its group, sorted into squares of CellSize meters,
    // so a check only looks at the squares nearby instead of every spot (with 1000+ trees that got slow).
    struct Spot { public Vector3 pos; public string group; public float clearance; }      // [Tony] added
    const float CellSize = 10f;                                                            // [Tony] added
    Dictionary<Vector2Int, List<Spot>> placed = new Dictionary<Vector2Int, List<Spot>>();  // [Tony] added
    float widestClearance;   // [Tony] added: the biggest Clearance placed so far, so the checks look far enough
    Vector2 patchSeed;       // [Tony] added: where the patch pattern starts (new every game, so the forests move)

    void Start()
    {
        patchSeed = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));   // [Tony] added

        // Run the spawning routine once for each rule, in list order
        foreach (ScatterRule rule in rules)
        {
            if (rule.prefab == null) continue;   // skip rules with no prefab set
            Scatter(rule);
        }
    }

    void Scatter(ScatterRule rule)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;   // the terrain's corner in the world

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = rule.count * 30;   // safety limit, so it can't loop forever

        while (spawned < rule.count && attempts < maxAttempts)
        {
            attempts++;

            // 1. Pick a random spot, as a 0-1 fraction across the terrain
            float nx = Random.value;
            float nz = Random.value;
            Vector3 pos = new Vector3(origin.x + nx * data.size.x, 0, origin.z + nz * data.size.z);

            // 2. Biome check: skip if our texture isn't painted strongly enough here
            if (LayerStrength(data, nx, nz, rule.terrainLayer) < rule.minStrength) continue;

            // [Tony] added: 2b. Patch check: skip the open ground (meadows) between the patches
            if (!InPatch(pos, rule)) continue;

            // 3. Slope check: skip if the ground is too steep
            if (data.GetSteepness(nx, nz) > rule.maxSlope) continue;

            // 4. Spacing check: skip if too close to something already placed
            // if (TooClose(pos, rule.minSpacing)) continue;   // [Tony] replaced: Min Spacing for its own group, Clearance for the rest
            if (TooClose(pos, rule)) continue;                 // [Tony] replaced

            // 5. Find the ground height, lift by the offset
            pos.y = terrain.SampleHeight(pos) + origin.y + rule.yOffset;

            // 6. Spawn it with a random rotation, tucked under this object
            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            // Instantiate(rule.prefab, pos, rot, transform);   // [Tony] replaced: it also gets a random size
            GameObject made = Instantiate(rule.prefab, pos, rot, transform);                  // [Tony] replaced
            float size = RandomSize(rule);                                                    // [Tony] added
            if (size != 1f) made.transform.localScale = rule.prefab.transform.localScale * size;   // [Tony] added

            // placed.Add(pos);   // [Tony] replaced: remembers its group too
            Remember(pos, rule);   // [Tony] replaced
            spawned++;
        }

        // Tells you in the Console how it went for each rule
        Debug.Log(rule.name + ": spawned " + spawned + " of " + rule.count);
    }

    // How strongly a texture layer is painted at this spot (0 = not at all, 1 = fully)
    float LayerStrength(TerrainData data, float nx, float nz, int layer)
    {
        int x = Mathf.Clamp((int)(nx * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int z = Mathf.Clamp((int)(nz * data.alphamapHeight), 0, data.alphamapHeight - 1);
        float[,,] map = data.GetAlphamaps(x, z, 1, 1);
        return map[0, 0, layer];
    }

    // [Tony] replaced: the old check, kept for reference (it kept Min Spacing from everything placed):
    // // Is there already something placed within 'spacing' of this spot?
    // bool TooClose(Vector3 pos, float spacing)
    // {
    //     foreach (Vector3 p in placed)
    //     {
    //         // Compare flat distance only (ignore height differences)
    //         Vector2 a = new Vector2(p.x, p.z);
    //         Vector2 b = new Vector2(pos.x, pos.z);
    //         if (Vector2.Distance(a, b) < spacing) return true;
    //     }
    //     return false;
    // }

    // [Tony] added: Is something of the same group within Min Spacing, or anything else within Clearance, of this spot?
    // (Flat distance only, like before.) Only the squares near the spot are looked at.
    bool TooClose(Vector3 pos, ScatterRule rule)
    {
        string group = GroupOf(rule);
        float reach = Mathf.Max(rule.minSpacing, Mathf.Max(rule.clearance, widestClearance));
        int squares = Mathf.CeilToInt(reach / CellSize);
        Vector2Int middle = CellOf(pos);
        for (int x = -squares; x <= squares; x++)
        {
            for (int z = -squares; z <= squares; z++)
            {
                if (!placed.TryGetValue(new Vector2Int(middle.x + x, middle.y + z), out List<Spot> spots)) continue;
                foreach (Spot spot in spots)
                {
                    float need = spot.group == group ? rule.minSpacing : Mathf.Max(rule.clearance, spot.clearance);
                    if (Vector2.Distance(new Vector2(spot.pos.x, spot.pos.z), new Vector2(pos.x, pos.z)) < need) return true;
                }
            }
        }
        return false;
    }

    // [Tony] added: remembers a used spot in its square.
    void Remember(Vector3 pos, ScatterRule rule)
    {
        Vector2Int cell = CellOf(pos);
        if (!placed.TryGetValue(cell, out List<Spot> spots))
        {
            spots = new List<Spot>();
            placed[cell] = spots;
        }
        spots.Add(new Spot { pos = pos, group = GroupOf(rule), clearance = rule.clearance });
        widestClearance = Mathf.Max(widestClearance, rule.clearance);
    }

    // [Tony] added: which square of the grid a spot is in.
    Vector2Int CellOf(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / CellSize), Mathf.FloorToInt(pos.z / CellSize));
    }

    // [Tony] added: the rule's Spacing Group, or its prefab's name when that is empty.
    string GroupOf(ScatterRule rule)
    {
        if (!string.IsNullOrEmpty(rule.spacingGroup)) return rule.spacingGroup;
        return rule.prefab != null ? rule.prefab.name : rule.name;
    }

    // [Tony] added: true if the spot is inside a patch (always, when the rule has no Patch Size). A smooth random
    // pattern (Perlin noise) decides; every rule reads the same pattern, so all tree looks grow in the same forests.
    bool InPatch(Vector3 pos, ScatterRule rule)
    {
        if (rule.patchSize <= 0f) return true;
        float noise = Mathf.PerlinNoise(patchSeed.x + pos.x / rule.patchSize, patchSeed.y + pos.z / rule.patchSize);   // about 0.2-0.8
        return noise >= Mathf.Lerp(0.3f, 0.7f, rule.openGround);
    }

    // [Tony] added: a random size between Min Size and Max Size (1 when they aren't set).
    float RandomSize(ScatterRule rule)
    {
        if (rule.minSize <= 0f || rule.maxSize <= 0f) return 1f;
        return Random.Range(rule.minSize, rule.maxSize);
    }
}