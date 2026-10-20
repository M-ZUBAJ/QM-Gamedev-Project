
//Ask Sanyam


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
        public float minSpacing = 8f;     // minimum distance from anything already placed
    }

    public Terrain terrain;                                 // the terrain to place things on
    public List<ScatterRule> rules = new List<ScatterRule>(); // one entry per object type

    List<Vector3> placed = new List<Vector3>();   // every spot used so far, shared by all rules

    void Start()
    {
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

            // 3. Slope check: skip if the ground is too steep
            if (data.GetSteepness(nx, nz) > rule.maxSlope) continue;

            // 4. Spacing check: skip if too close to something already placed
            if (TooClose(pos, rule.minSpacing)) continue;

            // 5. Find the ground height, lift by the offset
            pos.y = terrain.SampleHeight(pos) + origin.y + rule.yOffset;

            // 6. Spawn it with a random rotation, tucked under this object
            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            Instantiate(rule.prefab, pos, rot, transform);

            placed.Add(pos);
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

    // Is there already something placed within 'spacing' of this spot?
    bool TooClose(Vector3 pos, float spacing)
    {
        foreach (Vector3 p in placed)
        {
            // Compare flat distance only (ignore height differences)
            Vector2 a = new Vector2(p.x, p.z);
            Vector2 b = new Vector2(pos.x, pos.z);
            if (Vector2.Distance(a, b) < spacing) return true;
        }
        return false;
    }
}