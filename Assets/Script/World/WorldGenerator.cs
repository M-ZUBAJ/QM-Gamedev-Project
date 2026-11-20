/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: An empty object in the new world's scene (Assets/Scenes/ProceduralGenerateScene, "World Generator").
        Tools > World > Build New World Scene puts it there, filled in.
Unity Editor: Seed = 0: a new random world every game (the Console says which seed each game used). Any other number =
              that world every time, handy to look at one world again. The ground textures, the terrain's material, the
              caves and the ores are filled in by the tool; the other numbers are in meters.
              Testing: open MainTitleScene, Play, NEW GAME > Map: New world > START GAME. Or press Play in
              ProceduralGenerateScene. Every game is a different world.
Purpose: Makes the new world while its scene loads, before anything else in it starts:
         - a 1000 x 1000 m terrain: green grassland with gentle hills in the middle, a sandy desert on one side and
           rocky hills with snowy tops around the rest, steep mountains along the edge (and invisible walls behind
           them), painted with the project's ground textures (grass, mud, sand, rock, snow);
         - grass tufts on the grassy ground (Unity's waving terrain grass, with Polytope's grass blades picture);
         - the player starts in a small clearing in the middle of the grassland (also where they come back after
           dying);
         - rock caves in the grassland, each with Stone, Copper and Iron rocks inside;
         - then the usual spawners fill it in, as in the old world: Sanyam's ObjectScatter (trees, berry bushes,
           rocks and chests, on the painted textures) and AnimalSpawner (deer in the grassland).
         The same seed always makes the same world, trees and rocks included.
Requirement: WorldGenerator.cs x-> Terrain (Unity)
             WorldGenerator.cs o-> ObjectScatter.cs, PlayerController.cs, PlayerRespawn.cs, Rigidbody (Unity)
             WorldGenerator.cs <-x NewWorldBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]   // wakes up before everything else, so the world is there when the rest wakes up
[DisallowMultipleComponent]
public class WorldGenerator : MonoBehaviour
{
    public enum Biome { Grassland, Desert, Hills }

    // How much of each biome a spot has (they add up to 1; the edges between biomes have a bit of both).
    struct Biomes
    {
        public float grass, desert, hills;
    }

    // A place chosen for a cave: its middle (x, z), the flattened ground's height, which way it faces and which cave.
    struct CaveSite
    {
        public Vector2 center;
        public float height;
        public float turn;
        public GameObject prefab;
    }

    // ===== Fields =====
    [Header("World")]
    [Tooltip("0 = a new random world every game. Any other number = that same world every time (the Console says the " +
             "seed of each game, so a world you liked can be made again).")]
    [SerializeField] int seed = 0;

    [Tooltip("How wide and long the world is, in meters (a square, its middle at 0, 0).")]
    [Min(200f)]
    [SerializeField] float worldSize = 1000f;

    [Tooltip("The highest the ground can go, in meters (the mountains along the edge reach almost this high).")]
    [Min(30f)]
    [SerializeField] float maxHeight = 140f;

    [Tooltip("Detail of the ground's shape: 513 = a height every 2 m (low-poly look, quick to make). " +
             "Rounded to a power of two + 1.")]
    [Range(129, 1025)]
    [SerializeField] int heightResolution = 513;

    [Tooltip("Detail of the painted ground textures: 512 = a color every 2 m. Rounded to a power of two.")]
    [Range(128, 1024)]
    [SerializeField] int textureResolution = 512;

    [Header("Biomes (meters)")]
    [Tooltip("How far the green grassland reaches from the middle of the world (its edge wobbles a little).")]
    [SerializeField] float grasslandRadius = 280f;

    [Tooltip("How much of the land around the grassland is desert, in degrees (360 = all the way round). The rest is " +
             "rocky hills.")]
    [Range(30f, 300f)]
    [SerializeField] float desertWidth = 140f;

    [Tooltip("Snow covers the hills and mountains above this height.")]
    [SerializeField] float snowLine = 92f;

    [Header("Ground textures (filled in by Build New World Scene)")]
    [Tooltip("The old world's ground textures, in its order, so ObjectScatter's rules (e.g. trees on texture 0 = " +
             "grass) work the same here.")]
    [SerializeField] TerrainLayer[] layers = new TerrainLayer[0];

    [Tooltip("Which of those textures is which (their place in the list, 0 = the first).")]
    [SerializeField] int grassLayer = 0;
    [SerializeField] int sandLayer = 1;
    [SerializeField] int rockLayer = 2;
    [SerializeField] int snowLayer = 3;
    [SerializeField] int mudLayer = 4;

    [Tooltip("The terrain's material (the old world's). Empty = Unity's own.")]
    [SerializeField] Material terrainMaterial;

    [Header("Grass (tufts on the grassy ground)")]
    [Tooltip("The grass blades picture (Build New World Scene copies Polytope's into Assets/Effects/Grass). Empty = no grass.")]
    [SerializeField] Texture2D grassTexture;

    [Tooltip("How many grass tufts grow on 2 x 2 m of fully grassy ground (fewer where the grass thins out, none on mud, " +
             "sand, rock or snow). More = thicker grass, but more to draw.")]
    [Range(0, 16)]
    [SerializeField] int grassDensity = 6;

    [Tooltip("A tuft's width, in meters (smallest, biggest).")]
    [SerializeField] Vector2 grassWidth = new Vector2(0.6f, 1.1f);

    [Tooltip("A tuft's height, in meters (smallest, biggest). The player is 1.8 m tall.")]
    [SerializeField] Vector2 grassHeight = new Vector2(0.45f, 0.85f);

    [Tooltip("The grass's colors, mixed across the ground by a noise so it isn't one flat green.")]
    [SerializeField] Color grassHealthy = new Color(0.82f, 1f, 0.7f);
    [SerializeField] Color grassDry = new Color(0.86f, 0.88f, 0.55f);

    [Tooltip("How far from the camera the grass is drawn, in meters (further = more to draw).")]
    [Range(20f, 250f)]
    [SerializeField] float grassDistance = 80f;

    [Header("Caves")]
    [Tooltip("The caves to pick from (Tools > World > Build Rock Caves makes them). Each has children named OreSpot " +
             "where the ores go.")]
    [SerializeField] GameObject[] cavePrefabs = new GameObject[0];

    [Tooltip("How many caves the grassland gets (fewer if there's no room).")]
    [Range(0, 20)]
    [SerializeField] int caveCount = 6;

    [Tooltip("How far from the start the caves are, at least and at most.")]
    [SerializeField] float caveMinDistance = 60f;
    [SerializeField] float caveMaxDistance = 230f;

    [Tooltip("How far apart two caves are, at least.")]
    [SerializeField] float caveSpacing = 70f;

    [Tooltip("The flat ground made for each cave: a circle this wide (radius), a little bigger than the caves.")]
    [SerializeField] float caveRadius = 13f;

    [Header("Ores in the caves")]
    [Tooltip("The rocks put on a cave's ore spots: the first few are iron, then copper, the rest stone.")]
    [SerializeField] GameObject stoneOre;
    [SerializeField] GameObject copperOre;
    [SerializeField] GameObject ironOre;

    [Tooltip("How many iron rocks a cave has (at least, at most). Each gives 3 - 5 iron.")]
    [SerializeField] Vector2Int ironPerCave = new Vector2Int(2, 3);

    [Tooltip("How many copper rocks a cave has (at least, at most). The cave's other spots get stone.")]
    [SerializeField] Vector2Int copperPerCave = new Vector2Int(2, 3);

    [Header("Links (found by themselves if empty)")]
    [Tooltip("Sanyam's spawner of trees, bushes, rocks and chests. It gets the new terrain before it starts.")]
    [SerializeField] ObjectScatter scatter;

    [Tooltip("The player, moved to the start.")]
    [SerializeField] Transform player;

    const float SpawnClearing = 6f;     // the start: a flat circle this wide (radius) with no trees on it
    const string OreSpotName = "OreSpot";

    static WorldGenerator current;
    float[,] heights;      // the ground's height grid, [z, x], 0 = bottom, 1 = Max Height
    int res;               // heights along each side
    float cell;            // meters between two heights
    Vector3 corner;        // the terrain's corner (its middle is at 0, 0)
    Vector2 noiseAt;       // where in the noise this world is (moves with the seed)
    float desertAngle;     // which side the desert is on, in degrees
    Vector3 spawn;         // where the player starts (on the ground)
    readonly List<CaveSite> caves = new List<CaveSite>();
    Terrain terrain;

    // The generator of the world being played (null in the old world).
    public static WorldGenerator Current => current;

    public int Seed { get; private set; }
    public Vector3 SpawnPoint => spawn;
    public Terrain Terrain => terrain;
    public float WorldSize => worldSize;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        Seed = seed != 0 ? seed : new System.Random().Next(1, int.MaxValue);   // System.Random: new every game
        Random.InitState(Seed);   // from here on (trees and rocks too), the same seed gives the same world

        corner = new Vector3(-worldSize / 2f, 0f, -worldSize / 2f);
        res = Mathf.ClosestPowerOfTwo(heightResolution - 1) + 1;
        cell = worldSize / (res - 1);
        noiseAt = new Vector2(Random.Range(1000f, 9000f), Random.Range(1000f, 9000f));
        desertAngle = Random.Range(0f, 360f);

        MakeHeights();
        spawn = PickSpawn();
        PickCaves();
        terrain = MakeTerrain();
        PlaceCaves();
        MakeEdgeWalls();
        MovePlayer();
        LinkSpawners();

        Debug.Log("World Generator: seed " + Seed + " (type it into Seed to get this world again), " + worldSize + " x " +
                  worldSize + " m, the player starts at " + spawn.ToString("F0") + ", " + caves.Count + " caves.");
    }

    void OnDestroy()
    {
        if (current == this) current = null;   // static values outlive the scene, so clear them

        // The ground made for this game isn't a file: it would stay in memory after the scene closes (every Play Again
        // would add one), so it goes with the scene.
        if (terrain != null && terrain.terrainData != null) Destroy(terrain.terrainData);
    }

    // ===== My methods =====

    // Which biome a spot is in (the strongest one there), e.g. for a teammate's spawner.
    public Biome BiomeAt(Vector3 position)
    {
        Biomes b = BiomesAt(position.x, position.z);
        if (b.grass >= b.desert && b.grass >= b.hills) return Biome.Grassland;
        return b.desert >= b.hills ? Biome.Desert : Biome.Hills;
    }

    // ----- The ground's shape -----

    void MakeHeights()
    {
        heights = new float[res, res];
        for (int zi = 0; zi < res; zi++)
        {
            for (int xi = 0; xi < res; xi++)
            {
                heights[zi, xi] = Mathf.Clamp01(NaturalHeight(corner.x + xi * cell, corner.z + zi * cell) / maxHeight);
            }
        }
    }

    // The ground's height at a spot (x, z from the middle of the world), in meters, before the caves and the start
    // flatten bits of it.
    float NaturalHeight(float x, float z)
    {
        Biomes b = BiomesAt(x, z);
        float grass = 24f + Fbm(x, z, 0.0045f, 4, 0f) * 16f;                                   // rolling, easy to walk
        float desert = 21f + Fbm(x, z, 0.007f, 3, 300f) * 7f + Ridge(x, z, 0.02f, 600f) * 1.5f;  // low dunes
        float hills = 28f + Mathf.Pow(RidgedFbm(x, z, 0.004f, 3, 900f), 1.8f) * 75f;            // rocky, partly too steep
        float land = b.grass * grass + b.desert * desert + b.hills * hills;
        return Mathf.Lerp(land, maxHeight * 0.95f, EdgeAmount(x, z));                           // mountains at the edge
    }

    // How much of each biome a spot has: grassland in the middle (its edge wobbles), and around it the desert on one
    // side (Desert Width degrees of it) and rocky hills on the rest.
    Biomes BiomesAt(float x, float z)
    {
        float distance = Mathf.Sqrt(x * x + z * z);
        float angle = Mathf.Atan2(z, x);

        // Noise read around a circle: wobbles smoothly all the way round, with no seam where it meets itself.
        float wobble = Mathf.PerlinNoise(noiseAt.x + 50f + Mathf.Cos(angle) * 1.5f, noiseAt.y + 50f + Mathf.Sin(angle) * 1.5f);
        float edge = grasslandRadius * (0.85f + 0.3f * wobble);
        float grass = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 35f, edge + 35f, distance));

        float fromDesert = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, desertAngle));
        float half = desertWidth / 2f + (wobble - 0.5f) * 30f;
        float desert = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half - 15f, half + 15f, fromDesert));

        return new Biomes { grass = grass, desert = (1f - grass) * desert, hills = (1f - grass) * (1f - desert) };
    }

    // 0 inland, 1 within 10 m of the world's edge: the mountains there rise too steeply to climb.
    float EdgeAmount(float x, float z)
    {
        float toEdge = worldSize / 2f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 70f, toEdge));
    }

    // Smooth noise (0 - 1) at a spot; "salt" moves to another part of the noise, so each use looks different.
    float Noise(float x, float z, float frequency, float salt)
    {
        return Mathf.PerlinNoise(noiseAt.x + salt + x * frequency, noiseAt.y + salt + z * frequency);
    }

    // Several layers of noise: big shapes plus smaller and smaller bumps (0 - 1).
    float Fbm(float x, float z, float frequency, int layersOfNoise, float salt)
    {
        float sum = 0f, strength = 1f, total = 0f;
        for (int i = 0; i < layersOfNoise; i++)
        {
            sum += Noise(x, z, frequency, salt + i * 37.1f) * strength;
            total += strength;
            strength *= 0.5f;
            frequency *= 2f;
        }
        return sum / total;
    }

    // Noise folded into sharp ridges (0 - 1): good for rocky crests and dunes.
    float Ridge(float x, float z, float frequency, float salt)
    {
        return 1f - Mathf.Abs(Noise(x, z, frequency, salt) * 2f - 1f);
    }

    float RidgedFbm(float x, float z, float frequency, int layersOfNoise, float salt)
    {
        float sum = 0f, strength = 1f, total = 0f;
        for (int i = 0; i < layersOfNoise; i++)
        {
            sum += Ridge(x, z, frequency, salt + i * 41.7f) * strength;
            total += strength;
            strength *= 0.5f;
            frequency *= 2f;
        }
        return sum / total;
    }

    // The ground's height at any spot, in meters, from the height grid (mixed between the 4 nearest heights).
    float GroundHeight(float x, float z)
    {
        float gx = Mathf.Clamp((x - corner.x) / cell, 0f, res - 1.001f);
        float gz = Mathf.Clamp((z - corner.z) / cell, 0f, res - 1.001f);
        int x0 = (int)gx, z0 = (int)gz;
        float fx = gx - x0, fz = gz - z0;
        float near = Mathf.Lerp(heights[z0, x0], heights[z0, x0 + 1], fx);
        float far = Mathf.Lerp(heights[z0 + 1, x0], heights[z0 + 1, x0 + 1], fx);
        return Mathf.Lerp(near, far, fz) * maxHeight;
    }

    // How steep the ground is at a spot, in degrees (0 = flat).
    float SlopeAt(float x, float z)
    {
        float dx = GroundHeight(x + cell, z) - GroundHeight(x - cell, z);
        float dz = GroundHeight(x, z + cell) - GroundHeight(x, z - cell);
        return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz) / (2f * cell)) * Mathf.Rad2Deg;
    }

    // The steepest ground in a circle (its middle, and 8 spots on its edge and halfway).
    float SteepestIn(Vector2 center, float radius)
    {
        float steepest = SlopeAt(center.x, center.y);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            Vector2 way = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            steepest = Mathf.Max(steepest, SlopeAt(center.x + way.x * radius, center.y + way.y * radius));
            steepest = Mathf.Max(steepest, SlopeAt(center.x + way.x * radius * 0.5f, center.y + way.y * radius * 0.5f));
        }
        return steepest;
    }

    // Levels the ground in a circle to its average height, blending back into the land around it over "blend" meters.
    // Returns that height in meters.
    float Flatten(Vector2 center, float radius, float blend)
    {
        float sum = 0f;
        int count = 0;
        ForHeightsNear(center, radius, (zi, xi, distance) => { sum += heights[zi, xi]; count++; });
        float level = count > 0 ? sum / count : GroundHeight(center.x, center.y) / maxHeight;

        ForHeightsNear(center, radius + blend, (zi, xi, distance) =>
        {
            float amount = distance <= radius ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (distance - radius) / blend);
            heights[zi, xi] = Mathf.Lerp(heights[zi, xi], level, amount);
        });
        return level * maxHeight;
    }

    // Calls "use" for every height in a circle, with its place in the grid and its distance from the middle (meters).
    void ForHeightsNear(Vector2 center, float radius, System.Action<int, int, float> use)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - corner.x) / cell));
        int x1 = Mathf.Min(res - 1, Mathf.CeilToInt((center.x + radius - corner.x) / cell));
        int z0 = Mathf.Max(0, Mathf.FloorToInt((center.y - radius - corner.z) / cell));
        int z1 = Mathf.Min(res - 1, Mathf.CeilToInt((center.y + radius - corner.z) / cell));
        for (int zi = z0; zi <= z1; zi++)
        {
            for (int xi = x0; xi <= x1; xi++)
            {
                float distance = Vector2.Distance(center, new Vector2(corner.x + xi * cell, corner.z + zi * cell));
                if (distance <= radius) use(zi, xi, distance);
            }
        }
    }

    // ----- The start and the caves -----

    // A flat spot near the middle of the grassland, flattened a little more (a clearing). Returns it on the ground.
    Vector3 PickSpawn()
    {
        Vector2 best = Vector2.zero;
        float bestSlope = float.MaxValue;
        for (int i = 0; i < 200; i++)
        {
            Vector2 spot = i == 0 ? Vector2.zero : Random.insideUnitCircle * 60f;
            float slope = SteepestIn(spot, SpawnClearing);
            if (slope < bestSlope)
            {
                bestSlope = slope;
                best = spot;
            }
            if (slope < 6f) break;   // flat enough
        }
        float height = Flatten(best, SpawnClearing, SpawnClearing);
        return new Vector3(best.x, height, best.y);
    }

    // Places for the caves: in the grassland, between Cave Min and Max Distance from the start, on gentle ground and
    // Cave Spacing apart. The ground under each is flattened (the cave's floor).
    void PickCaves()
    {
        caves.Clear();
        if (cavePrefabs == null || cavePrefabs.Length == 0) return;

        Vector2 start = new Vector2(spawn.x, spawn.z);
        for (int tries = 0; tries < 500 && caves.Count < caveCount; tries++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 center = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(caveMinDistance, caveMaxDistance);
            if (BiomesAt(center.x, center.y).grass < 0.9f) continue;   // only in the grassland
            if (SteepestIn(center, caveRadius) > 16f) continue;        // not on a steep hillside

            bool roomy = true;
            foreach (CaveSite other in caves) roomy &= Vector2.Distance(other.center, center) >= caveSpacing;
            if (!roomy) continue;

            GameObject prefab = cavePrefabs[Random.Range(0, cavePrefabs.Length)];
            if (prefab == null) continue;
            float height = Flatten(center, caveRadius, 10f);
            caves.Add(new CaveSite { center = center, height = height, turn = Random.Range(0f, 360f), prefab = prefab });
        }
    }

    void PlaceCaves()
    {
        if (caves.Count == 0) return;
        Transform holder = new GameObject("Caves").transform;
        foreach (CaveSite site in caves)
        {
            Vector3 at = new Vector3(site.center.x, site.height, site.center.y);
            GameObject cave = Instantiate(site.prefab, at, Quaternion.Euler(0f, site.turn, 0f), holder);
            FillOres(cave.transform);
        }
    }

    // Puts the rocks on a cave's ore spots (its children named OreSpot), in a random order: a few iron, a few copper,
    // the rest stone. They settle onto the cave's floor by themselves (StandOnGround).
    void FillOres(Transform cave)
    {
        List<Transform> spots = new List<Transform>();
        foreach (Transform part in cave.GetComponentsInChildren<Transform>())
        {
            if (part.name.StartsWith(OreSpotName)) spots.Add(part);
        }
        for (int i = spots.Count - 1; i > 0; i--)   // shuffle, so iron isn't always in the same corner
        {
            int j = Random.Range(0, i + 1);
            (spots[i], spots[j]) = (spots[j], spots[i]);
        }

        int iron = Random.Range(ironPerCave.x, ironPerCave.y + 1);
        int copper = Random.Range(copperPerCave.x, copperPerCave.y + 1);
        for (int i = 0; i < spots.Count; i++)
        {
            GameObject ore = i < iron ? ironOre : i < iron + copper ? copperOre : stoneOre;
            if (ore != null) Instantiate(ore, spots[i].position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), cave);
        }
    }

    // ----- The terrain -----

    Terrain MakeTerrain()
    {
        TerrainData data = new TerrainData { name = "Generated World " + Seed };
        data.heightmapResolution = res;                          // first: changing it also changes the size
        data.size = new Vector3(worldSize, maxHeight, worldSize);
        data.SetHeights(0, 0, heights);

        if (layers != null && layers.Length > 0)
        {
            data.terrainLayers = layers;
            data.alphamapResolution = Mathf.ClosestPowerOfTwo(textureResolution);
            float[,,] paint = Paint(data.alphamapResolution);
            data.SetAlphamaps(0, 0, paint);
            AddGrass(data, paint);
        }
        else Debug.LogWarning("World Generator: no ground textures, so no grass, trees or deer. Run Tools > World > " +
                              "Build New World Scene again.", this);

        GameObject go = Terrain.CreateTerrainGameObject(data);   // a Terrain and its Terrain Collider
        go.name = "Generated Terrain";
        go.transform.position = corner;
        Terrain made = go.GetComponent<Terrain>();
        if (terrainMaterial != null) made.materialTemplate = terrainMaterial;
        made.detailObjectDistance = grassDistance;
        made.detailObjectDensity = 1f;
        return made;
    }

    // Grass tufts where the ground is painted grass: thick on full grass, thinner where it fades into mud or the start
    // clearing, none on sand, rock or snow; in thicker and thinner patches (a noise). Unity draws them as its own
    // terrain grass, waving in the wind, near the camera only (Grass Distance).
    void AddGrass(TerrainData data, float[,,] paint)
    {
        if (grassTexture == null || grassDensity <= 0 || grassLayer < 0 || grassLayer >= paint.GetLength(2)) return;

        int size = paint.GetLength(0);
        data.SetDetailResolution(size, 32);                                  // one count per 2 x 2 m, like the textures
        data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);      // the map says how many tufts, not "how much"
        data.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                prototypeTexture = grassTexture,
                usePrototypeMesh = false,
                renderMode = DetailRenderMode.Grass,   // crossed cards (not turning to face the camera: better in first person)
                minWidth = grassWidth.x,
                maxWidth = grassWidth.y,
                minHeight = grassHeight.x,
                maxHeight = grassHeight.y,
                healthyColor = grassHealthy,
                dryColor = grassDry,
                noiseSpread = 0.08f,
            },
        };
        data.wavingGrassTint = Color.white;   // Unity's own tint would turn the green grey

        int[,] counts = new int[size, size];
        float step = worldSize / size;
        for (int zi = 0; zi < size; zi++)
        {
            for (int xi = 0; xi < size; xi++)
            {
                float green = paint[zi, xi, grassLayer];
                if (green < 0.5f) continue;   // mostly mud, sand, rock or snow here
                float x = corner.x + (xi + 0.5f) * step, z = corner.z + (zi + 0.5f) * step;
                float patches = Noise(x, z, 0.03f, 5000f);
                counts[zi, xi] = Mathf.RoundToInt(grassDensity * Mathf.InverseLerp(0.5f, 1f, green) * Mathf.Lerp(0.3f, 1.2f, patches));
            }
        }
        data.SetDetailLayer(0, 0, 0, counts);
    }

    // The textures painted on the ground, one mix per spot: grass with patches of mud in the grassland, sand in the
    // desert, grass low down and rock higher up on the hills, snow above the Snow Line, bare rock on every steep slope,
    // rock and mud in and around the caves, and a grass-and-mud clearing at the start (too little grass for trees).
    float[,,] Paint(int size)
    {
        int count = layers.Length;
        float[,,] map = new float[size, size, count];
        float[] mix = new float[count];
        float step = worldSize / size;
        Vector2 start = new Vector2(spawn.x, spawn.z);

        for (int zi = 0; zi < size; zi++)
        {
            for (int xi = 0; xi < size; xi++)
            {
                float x = corner.x + (xi + 0.5f) * step, z = corner.z + (zi + 0.5f) * step;
                System.Array.Clear(mix, 0, count);

                Biomes b = BiomesAt(x, z);
                float height = GroundHeight(x, z);
                float mud = Mathf.Clamp01((Fbm(x, z, 0.02f, 2, 3000f) - 0.55f) * 3f);   // patches of mud in the grass
                float lowGreen = 1f - Mathf.InverseLerp(36f, 50f, height);           // the hills' low slopes stay green
                float snow = Mathf.InverseLerp(snowLine - 6f, snowLine + 6f, height);
                Add(mix, grassLayer, b.grass * (1f - mud) + b.hills * lowGreen * (1f - snow));
                Add(mix, mudLayer, b.grass * mud);
                Add(mix, sandLayer, b.desert);
                Add(mix, rockLayer, b.hills * (1f - lowGreen) * (1f - snow));
                Add(mix, snowLayer, b.hills * snow);

                // Steep ground (from 30 degrees, all rock by 42) and the edge mountains: rock, or snow up high.
                float bare = Mathf.Max(Mathf.InverseLerp(30f, 42f, SlopeAt(x, z)), EdgeAmount(x, z));
                if (bare > 0f)
                {
                    for (int i = 0; i < count; i++) mix[i] *= 1f - bare;
                    Add(mix, rockLayer, bare * (1f - snow));
                    Add(mix, snowLayer, bare * snow);
                }

                PaintPlaces(mix, new Vector2(x, z), start);

                float total = 0f;
                for (int i = 0; i < count; i++) total += mix[i];
                if (total <= 0f)
                {
                    Add(mix, grassLayer, 1f);
                    total = 1f;
                }
                for (int i = 0; i < count; i++) map[zi, xi, i] = mix[i] / total;
            }
        }
        return map;
    }

    // In and around a cave: rock and mud, no grass (so no tree grows into it); at the start: half grass, half mud.
    void PaintPlaces(float[] mix, Vector2 spot, Vector2 start)
    {
        foreach (CaveSite site in caves)
        {
            float distance = Vector2.Distance(spot, site.center);
            if (distance > caveRadius + 6f) continue;
            float inside = distance < caveRadius - 3f ? 0.75f : 0.5f;   // mostly mud on the floor, rockier at the walls
            System.Array.Clear(mix, 0, mix.Length);
            Add(mix, mudLayer, inside);
            Add(mix, rockLayer, 1f - inside);
            return;
        }
        if (Vector2.Distance(spot, start) <= SpawnClearing + 3f)
        {
            System.Array.Clear(mix, 0, mix.Length);
            Add(mix, grassLayer, 0.55f);
            Add(mix, mudLayer, 0.45f);
        }
    }

    // Adds some of a texture to a mix (a texture the terrain doesn't have is skipped).
    static void Add(float[] mix, int layer, float amount)
    {
        if (layer >= 0 && layer < mix.Length && amount > 0f) mix[layer] += amount;
    }

    // Invisible walls just outside the edge mountains, so nobody can ever walk off the world. On the Ignore Raycast
    // layer: rays (enemy spawning, aiming) pass through them, only bodies bump into them.
    void MakeEdgeWalls()
    {
        GameObject walls = new GameObject("World Edge") { layer = 2 };
        float half = worldSize / 2f, height = maxHeight * 3f, thick = 10f, length = worldSize + thick * 2f;
        AddWall(walls, new Vector3(-half - thick / 2f, height / 2f, 0f), new Vector3(thick, height, length));
        AddWall(walls, new Vector3(half + thick / 2f, height / 2f, 0f), new Vector3(thick, height, length));
        AddWall(walls, new Vector3(0f, height / 2f, -half - thick / 2f), new Vector3(length, height, thick));
        AddWall(walls, new Vector3(0f, height / 2f, half + thick / 2f), new Vector3(length, height, thick));
    }

    static void AddWall(GameObject walls, Vector3 center, Vector3 size)
    {
        BoxCollider box = walls.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    // ----- The player and the spawners -----

    // The player starts just above the clearing (and comes back there after dying).
    void MovePlayer()
    {
        if (player == null)
        {
            PlayerController found = FindFirstObjectByType<PlayerController>();
            if (found != null) player = found.transform;
        }
        if (player == null) return;

        Vector3 at = spawn + Vector3.up * 1.5f;   // a little above the ground: the player drops onto it
        player.position = at;
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = at;
            body.velocity = Vector3.zero;
        }
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.SetRespawnPoint(at);
    }

    // Sanyam's ObjectScatter spawns the trees, bushes, rocks and chests on the painted textures in its Start: it gets
    // the new terrain now. Without textures it can't (it would only show errors), so it's switched off then.
    void LinkSpawners()
    {
        if (scatter == null) scatter = FindFirstObjectByType<ObjectScatter>();
        if (scatter == null) return;
        if (layers != null && layers.Length > 0) scatter.terrain = terrain;
        else scatter.enabled = false;
    }
}
