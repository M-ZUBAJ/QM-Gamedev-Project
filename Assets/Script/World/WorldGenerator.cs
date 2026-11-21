/*
First Date: 20/11/2026
Date: 21/11/2026
Author: Tony
Use by: An empty object in the new world's scene (Assets/Scenes/ProceduralGenerateScene, "World Generator").
        Tools > World > Build New World Scene puts it there, filled in.
Unity Editor: Seed = 0: a new random world every game (the Console says which seed each game used). Any other number =
              that world every time, handy to look at one world again. The ground textures, the terrain's material, the
              caves and the ores are filled in by the tool; the other numbers are in meters.
              New World On Death (on): when the player dies, Respawn makes a brand-new world (Day 1, everything fresh,
              like a new game). Off: the player comes back at the start of the same world, like the old world.
              Testing: open MainTitleScene, Play, NEW GAME > Map: New world > START GAME. Or press Play in
              ProceduralGenerateScene. Every game is a different world.
Purpose: Makes the new world while its scene loads, before anything else in it starts:
         - a 1000 x 1000 m world: green grassland with gentle hills in the middle, a sandy desert on one side and
           rocky hills with snowy tops around the rest, painted with the project's ground textures (grass, mud, sand,
           rock, snow; the desert is all sand, even its steep dunes, so nothing meant for rock spawns there);
         - all round it a glowing barrier (WorldBarrier) instead of mountains: players see it and can't pass. The
           land goes on for Land Beyond Barrier meters past it (bare rock and mud, so no trees, chests or deer out
           there), so through the barrier the world fades into the mist instead of ending;
         - grass tufts on the grassy ground (Unity's waving terrain grass, with Polytope's grass blades picture),
           standing upright and swaying gently;
         - the player starts in a small clearing in the middle of the grassland (also where they come back after
           dying);
         - rock caves in the grassland, each with Stone, Copper and Iron rocks inside, and sandstone caves deep in the
           desert, each with Gold (an Iron Pickaxe or better) and hard Sandstone rocks (they give Stone);
         - then the usual spawners fill it in, as in the old world: Sanyam's ObjectScatter (trees, berry bushes,
           rocks and chests, on the painted textures) and AnimalSpawner (deer in the grassland).
         The same seed always makes the same world, trees and rocks included.
         When the player dies, the death screen's Respawn makes a brand-new world (New World On Death): the scene loads
         again behind the screen fader ("DAY 1", "A new world"), so it's Day 1 with a new seed and everything fresh
         (inventory, armor, perks, coins), like a new game; the New Game page's choices stay.
Requirement: WorldGenerator.cs x-> Terrain (Unity), WorldBarrier.cs
             WorldGenerator.cs o-> ObjectScatter.cs, PlayerController.cs, PlayerRespawn.cs, ViewDistance.cs, ScreenFader.cs, Rigidbody (Unity)
             WorldGenerator.cs <-x LootChestBuilder.cs, NewWorldBuilder.cs, WorldBarrierBuilder.cs
             WorldGenerator.cs <-o PlayerRespawn.cs
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

    // A place chosen for a cave: its middle (x, z), the flattened ground's height, which way it faces, which cave, and
    // whether it's a desert cave (gold and sandstone in it, a sandy floor).
    struct CaveSite
    {
        public Vector2 center;
        public float height;
        public float turn;
        public GameObject prefab;
        public bool desert;
    }

    // ===== Fields =====
    [Header("World")]
    [Tooltip("0 = a new random world every game. Any other number = that same world every time (the Console says the " +
             "seed of each game, so a world you liked can be made again).")]
    [SerializeField] int seed = 0;

    [Tooltip("On: when the player dies, Respawn makes a brand-new world (Day 1 and everything fresh, like a new game; a " +
             "new seed unless Seed is set). Off: the player comes back at the start of this same world, as in the old world.")]
    [SerializeField] bool newWorldOnDeath = true;

    [Tooltip("How wide and long the world is, in meters: the square inside the barrier, its middle at 0, 0.")]
    [Min(200f)]
    [SerializeField] float worldSize = 1000f;

    [Tooltip("The highest the ground can go, in meters (the rocky hills' tops reach about 100 m).")]
    [Min(30f)]
    [SerializeField] float maxHeight = 140f;

    [Tooltip("How far the land goes on past the barrier, in meters, on every side (at least the camera's View Distance, " +
             "up to 250 m). Nobody gets there, but through the barrier the world fades into the mist instead of ending " +
             "in an edge. It's bare rock and mud, so nothing useful grows there (trees, bushes, chests and deer need grass).")]
    [Min(0f)]
    [SerializeField] float landBeyondBarrier = 60f;

    [Tooltip("Detail of the ground's shape: 513 = a height about every 2 m (low-poly look, quick to make). " +
             "Rounded to a power of two + 1.")]
    [Range(129, 1025)]
    [SerializeField] int heightResolution = 513;

    [Tooltip("Detail of the painted ground textures: 512 = a color about every 2 m. Rounded to a power of two.")]
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

    [Tooltip("A tuft's width, in meters (smallest, biggest). Keep it below the height: the grass picture is square, so " +
             "a tuft wider than it is tall squashes its blades flat, as if bent over.")]
    [SerializeField] Vector2 grassWidth = new Vector2(0.45f, 0.7f);

    [Tooltip("A tuft's height, in meters (smallest, biggest). The player is 1.8 m tall.")]
    [SerializeField] Vector2 grassHeight = new Vector2(0.55f, 0.9f);

    [Tooltip("How far the wind bends the grass (Unity's Bending): 0 = still, 0.5 = Unity's own (a strong wind).")]
    [Range(0f, 1f)]
    [SerializeField] float grassBending = 0.2f;

    [Tooltip("How fast the wind's waves move over the grass (Unity's Speed, 0.5 = Unity's own).")]
    [Range(0f, 1f)]
    [SerializeField] float grassWindSpeed = 0.4f;

    [Tooltip("The grass's colors, mixed across the ground by a noise so it isn't one flat green.")]
    [SerializeField] Color grassHealthy = new Color(0.82f, 1f, 0.7f);
    [SerializeField] Color grassDry = new Color(0.86f, 0.88f, 0.55f);

    [Tooltip("How far from the camera the grass is drawn, in meters (further = more to draw). The camera's ViewDistance " +
             "caps it (100 m in the new world).")]
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

    [Header("Desert caves")]
    [Tooltip("The desert's caves (Tools > World > Build Desert Content makes them, of sandstone). Each has children named " +
             "OreSpot where the rocks go.")]
    [SerializeField] GameObject[] desertCavePrefabs = new GameObject[0];

    [Tooltip("How many caves the desert gets (fewer if there's no room). They're anywhere deep in the desert, Cave " +
             "Spacing apart from every cave.")]
    [Range(0, 10)]
    [SerializeField] int desertCaveCount = 3;

    [Tooltip("The rocks in a desert cave: Gold (its rock needs an Iron Pickaxe), the other spots Sandstone (a hard desert " +
             "rock that gives Stone; empty = Stone Ore).")]
    [SerializeField] GameObject goldOre;
    [SerializeField] GameObject sandstoneOre;

    [Tooltip("How many gold rocks a desert cave has (at least, at most). Each gives 2 - 4 gold.")]
    [SerializeField] Vector2Int goldPerCave = new Vector2Int(2, 3);

    [Header("Links (found by themselves if empty)")]
    [Tooltip("Sanyam's spawner of trees, bushes, rocks and chests. It gets the new terrain before it starts.")]
    [SerializeField] ObjectScatter scatter;

    [Tooltip("The player, moved to the start.")]
    [SerializeField] Transform player;

    const float SpawnClearing = 6f;     // the start: a flat circle this wide (radius) with no trees on it
    const string OreSpotName = "OreSpot";

    static WorldGenerator current;
    bool startingNewWorld;   // the player died and a new world is loading (it starts only once)
    float[,] heights;      // the ground's height grid, [z, x], 0 = bottom, 1 = Max Height
    int res;               // heights along each side
    float terrainSize;     // the terrain's width and length: the world + the land beyond the barrier on both sides
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

    // True when respawning makes a brand-new world instead of coming back here (PlayerRespawn asks): the new world, with
    // New World On Death on. False in the old world (no WorldGenerator).
    public static bool NewWorldOnRespawn => current != null && current.newWorldOnDeath;
    public Vector3 SpawnPoint => spawn;
    public Terrain Terrain => terrain;
    public float WorldSize => worldSize;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        Seed = seed != 0 ? seed : new System.Random().Next(1, int.MaxValue);   // System.Random: new every game
        Random.InitState(Seed);   // from here on (trees and rocks too), the same seed gives the same world

        terrainSize = worldSize + LandBeyond() * 2f;
        corner = new Vector3(-terrainSize / 2f, 0f, -terrainSize / 2f);
        res = Mathf.ClosestPowerOfTwo(heightResolution - 1) + 1;
        cell = terrainSize / (res - 1);
        noiseAt = new Vector2(Random.Range(1000f, 9000f), Random.Range(1000f, 9000f));
        desertAngle = Random.Range(0f, 360f);

        MakeHeights();
        spawn = PickSpawn();
        PickCaves();
        PickDesertCaves();
        terrain = MakeTerrain();
        PlaceCaves();
        MakeBarrier();
        MovePlayer();
        LinkSpawners();

        Debug.Log("World Generator: seed " + Seed + " (type it into Seed to get this world again), " + worldSize + " x " +
                  worldSize + " m inside the barrier, the player starts at " + spawn.ToString("F0") + ", " + caves.Count +
                  " caves (" + caves.FindAll(site => site.desert).Count + " of them in the desert, with gold).");
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
        return b.grass * grass + b.desert * desert + b.hills * hills;   // no mountains at the edge: the barrier is there
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

    // How far the land goes on past the barrier: Land Beyond Barrier, but at least the camera's view distance
    // (ViewDistance, up to 250 m), so from the barrier the land never visibly ends before the mist hides it.
    float LandBeyond()
    {
        ViewDistance view = FindFirstObjectByType<ViewDistance>();   // found by itself: it wakes up after this
        float seen = view != null ? Mathf.Min(view.Distance, 250f) : 0f;
        return Mathf.Max(landBeyondBarrier, seen);
    }

    // 0 inside the world, 1 from 6 m past the barrier: how much a spot is "outside", where the land is bare (see Paint).
    // It starts 8 m inside, so the last strip along the barrier has too little grass for trees to grow into it.
    float Outside(float x, float z)
    {
        float past = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - worldSize / 2f;   // meters past the barrier (below 0 = inside)
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-8f, 6f, past));
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

    // Places for the desert's caves: anywhere deep in the desert (not near the barrier), on gentle ground, Cave Spacing
    // apart from every cave. The ground under each is flattened too.
    void PickDesertCaves()
    {
        if (desertCavePrefabs == null || desertCavePrefabs.Length == 0) return;
        float reach = worldSize / 2f - caveRadius - 25f;   // keeps the cave clear of the barrier
        int placed = 0;
        for (int tries = 0; tries < 800 && placed < desertCaveCount; tries++)
        {
            Vector2 center = new Vector2(Random.Range(-reach, reach), Random.Range(-reach, reach));
            if (BiomesAt(center.x, center.y).desert < 0.9f) continue;   // only deep in the desert
            if (SteepestIn(center, caveRadius) > 16f) continue;         // not on a steep dune

            bool roomy = true;
            foreach (CaveSite other in caves) roomy &= Vector2.Distance(other.center, center) >= caveSpacing;
            if (!roomy) continue;

            GameObject prefab = desertCavePrefabs[Random.Range(0, desertCavePrefabs.Length)];
            if (prefab == null) continue;
            float height = Flatten(center, caveRadius, 10f);
            caves.Add(new CaveSite { center = center, height = height, turn = Random.Range(0f, 360f), prefab = prefab, desert = true });
            placed++;
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
            FillOres(cave.transform, site.desert);
        }
    }

    // Puts the rocks on a cave's ore spots (its children named OreSpot), in a random order: a few iron, a few copper,
    // the rest stone; in a desert cave a few gold, the rest sandstone. They settle onto the floor by themselves
    // (StandOnGround).
    void FillOres(Transform cave, bool desert)
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

        // The first "rare" spots get the rare rocks (iron then copper; gold in the desert), the rest the common rock.
        int iron = desert ? 0 : Random.Range(ironPerCave.x, ironPerCave.y + 1);
        int copper = desert ? 0 : Random.Range(copperPerCave.x, copperPerCave.y + 1);
        int gold = desert ? Random.Range(goldPerCave.x, goldPerCave.y + 1) : 0;
        GameObject common = desert && sandstoneOre != null ? sandstoneOre : stoneOre;
        for (int i = 0; i < spots.Count; i++)
        {
            GameObject ore = i < gold ? goldOre : i < iron ? ironOre : i < iron + copper ? copperOre : common;
            if (ore != null) Instantiate(ore, spots[i].position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), cave);
        }
    }

    // ----- The terrain -----

    Terrain MakeTerrain()
    {
        TerrainData data = new TerrainData { name = "Generated World " + Seed };
        data.heightmapResolution = res;                          // first: changing it also changes the size
        data.size = new Vector3(terrainSize, maxHeight, terrainSize);
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
        data.SetDetailResolution(size, 32);                                  // one count per texture spot (about 2 x 2 m)
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
        data.wavingGrassAmount = grassBending;    // Unity's own 0.5 bent the tufts far over in gusts
        data.wavingGrassSpeed = grassWindSpeed;

        int[,] counts = new int[size, size];
        float step = terrainSize / size;
        float perSpot = grassDensity * step * step / 4f;   // Grass Density is per 2 x 2 m; a spot is step x step meters
        for (int zi = 0; zi < size; zi++)
        {
            for (int xi = 0; xi < size; xi++)
            {
                float green = paint[zi, xi, grassLayer];
                if (green < 0.5f) continue;   // mostly mud, sand, rock or snow here
                float x = corner.x + (xi + 0.5f) * step, z = corner.z + (zi + 0.5f) * step;
                float patches = Noise(x, z, 0.03f, 5000f);
                counts[zi, xi] = Mathf.RoundToInt(perSpot * Mathf.InverseLerp(0.5f, 1f, green) * Mathf.Lerp(0.3f, 1.2f, patches));
            }
        }
        data.SetDetailLayer(0, 0, 0, counts);
    }

    // The textures painted on the ground, one mix per spot: grass with patches of mud in the grassland, sand in the
    // desert, grass low down and rock higher up on the hills, snow above the Snow Line, bare rock on every steep slope,
    // rock and mud in and around the caves, a grass-and-mud clearing at the start (too little grass for trees), and
    // bare rock and mud past the barrier.
    float[,,] Paint(int size)
    {
        int count = layers.Length;
        float[,,] map = new float[size, size, count];
        float[] mix = new float[count];
        float step = terrainSize / size;
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

                // Steep ground (from 30 degrees, all rock by 42): rock, or snow up high. Not the desert's steep dunes:
                // they stay sand, so no rock (and no stones spawned on rock) in the desert.
                float steep = Mathf.InverseLerp(30f, 42f, SlopeAt(x, z)) * (1f - b.desert);
                if (steep > 0f)
                {
                    for (int i = 0; i < count; i++) mix[i] *= 1f - steep;
                    Add(mix, rockLayer, steep * (1f - snow));
                    Add(mix, snowLayer, steep * snow);
                }

                // Past the barrier: bare ground nobody reaches, so nothing useful spawns there (trees, bushes, chests
                // and deer need grass; the hills' stone rocks need more rock than this).
                float outside = Outside(x, z);
                if (outside > 0f)
                {
                    for (int i = 0; i < count; i++) mix[i] *= 1f - outside;
                    Add(mix, mudLayer, outside * 0.55f * (1f - snow));
                    Add(mix, rockLayer, outside * 0.45f * (1f - snow));
                    Add(mix, snowLayer, outside * snow);
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

    // In and around a cave: rock and mud, no grass (so no tree grows into it); a desert cave: sand and dirt (mud), with
    // too little of either for the spawners' rocks (Sandstone Rocks need 0.6 sand, Hill Rocks 0.5 rock); at the start:
    // half grass, half mud.
    void PaintPlaces(float[] mix, Vector2 spot, Vector2 start)
    {
        foreach (CaveSite site in caves)
        {
            float distance = Vector2.Distance(spot, site.center);
            if (distance > caveRadius + 6f) continue;
            bool floor = distance < caveRadius - 3f;
            System.Array.Clear(mix, 0, mix.Length);
            if (site.desert)
            {
                Add(mix, sandLayer, floor ? 0.45f : 0.55f);   // a sandy floor, sand blowing in at the walls
                Add(mix, floor ? mudLayer : rockLayer, floor ? 0.55f : 0.45f);
                return;
            }
            float inside = floor ? 0.75f : 0.5f;   // mostly mud on the floor, rockier at the walls
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

    // The glowing barrier all round the world (WorldBarrier): players see it as they come near and can't pass it.
    // From below the lowest ground to well above the highest hills; its solid walls stand just outside the line, on the
    // Ignore Raycast layer (rays for aiming and spawning pass through, only bodies bump into them).
    void MakeBarrier()
    {
        const float below = 10f;   // starts this far under the ground (the lowest ground is at 0)
        GameObject go = new GameObject("World Barrier");
        go.transform.position = new Vector3(0f, -below, 0f);
        WorldBarrier barrier = go.AddComponent<WorldBarrier>();
        barrier.SetArea(new Vector2(worldSize, worldSize), maxHeight + 60f + below);
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

    // The player died and clicked Respawn (PlayerRespawn asks NewWorldOnRespawn first): this scene loads again behind the
    // screen fader's title card, which makes a brand-new world (Seed 0 = a new seed) with Day 1 and everything fresh,
    // like a new game. The New Game page's choices stay (GameSetup keeps them, as for Play Again).
    public void StartNewWorld()
    {
        if (startingNewWorld) return;   // only once (e.g. the button clicked twice)
        startingNewWorld = true;
        Time.timeScale = 1f;            // the new world must not start frozen
        Debug.Log("World Generator: the player died, so a new world is made (New World On Death).");
        ScreenFader.LoadScene(gameObject.scene.name, "DAY 1", "A new world");
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
