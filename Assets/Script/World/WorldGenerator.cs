/*
First Date: 20/11/2026
Date: 22/11/2026
Author: Tony
Use by: An empty object in the new world's scene (Assets/Scenes/ProceduralGenerateScene, "World Generator").
        Tools > World > Build New World Scene puts it there, filled in.
Unity Editor: Seed = 0: a new random world every game (the Console says which seed each game used). Any other number =
              that world every time, handy to look at one world again. The ground textures, the terrain's material, the
              caves and the ores are filled in by the tool; the other numbers are in meters.
              Endless land: Chunk Size (one square of land), Load Distance (made this far ahead), Show / Hide Distance,
              Content Distance (past it a chunk lets go of its trees, rocks and chests to save memory), Frame Budget (how
              much of each frame making land may take).
              Biomes: Start Radius (always grassland), Desert From / Snow From (how far from the start they can begin),
              Climate Size (how big the hot and cold areas are). Caves: each biome's chance of a cave per chunk.
              Rules Area: how thick ObjectScatter's trees, rocks and chests grow (smaller = more).
              New World On Death (on): when the player dies, Respawn makes a brand-new world (Day 1, everything fresh,
              like a new game). Off: the player comes back at the start of the same world, like the old world.
              Testing: open MainTitleScene, Play, NEW GAME > Map: New world > START GAME. Or press Play in
              ProceduralGenerateScene. ⋮ > Test: Go To The Snow / Test: Go To The Desert takes the player there.
Purpose: Makes the new world while its scene loads, and then keeps making it, endlessly, as the player walks: the land
         comes in square chunks (Chunk Size) made around the player:
           - the start: a small clearing in green grassland; everything within Start Radius is grassland;
           - farther out the land changes with a slow noise (a climate): green grassland, rocky hills (snowy tops above
             the Snow Line), the sandy desert (only from Desert From on) and the snow (only from Snow From on: the
             farthest from the start, and the best place: more caves, diamonds and more and better chests);
           - each chunk: its ground's shape and painted textures (the old world's grass, sand, rock, snow, mud), grass
             tufts, maybe a cave or two (grassland: Iron, Copper, Stone rocks; desert: Gold, Iron, Sandstone; snow:
             Diamond, Gold, Stone; 1 desert / snow cave in 20 is a lucky one with 4 gold / 4 diamond rocks), and its
             trees, bushes, rocks and chests from Sanyam's ObjectScatter rules (a rule's Count is how
             many grow on Rules Area of ground painted with its texture, so each chunk gets its share; the rules on the
             snow texture, the snowy pines and the snow's chests, grow only in the snow biome, not on the hills' tops);
           - chunks within Load Distance are made ahead of the player, a few milliseconds of each frame (Frame Budget)
             so the game doesn't stop; within Show Distance they're shown (their trees, rocks and chests wake up a
             small group at a time over a few frames, not all in one). A chunk left behind (past Hide Distance) is
             hidden, not thrown away: its ground stays solid and its trees, rocks and opened chests stay as they were
             when the player comes back;
           - farther still (past Content Distance, 22/11/2026) a chunk lets go of its trees, bushes, rocks, chests and
             caves: every one is remembered (what, where, which way, how big) along with which ones the player used up
             (chopped, mined, picked, opened), and when the player comes back within Load Distance the rest is made
             again, the same, a few at a time. Before, every chunk ever made kept all its things, so the more land the
             player walked, the more memory the game held and the slower it got. A new chunk's ground is also built
             over a few frames now (heights, textures, grass, then the terrain), not all in one.
         No barrier: the world has no edge. The same seed always makes the same world, trees and rocks included.
         When the player dies, the death screen's Respawn makes a brand-new world (New World On Death): the scene loads
         again behind the screen fader ("DAY 1", "A new world"), so it's Day 1 with a new seed and everything fresh
         (inventory, armor, perks, coins), like a new game; the New Game page's choices stay.
Requirement: WorldGenerator.cs x-> Terrain (Unity)
             WorldGenerator.cs o-> ObjectScatter.cs, PlayerController.cs, PlayerRespawn.cs, ViewDistance.cs, ScreenFader.cs, PerkChest.cs, Rigidbody (Unity)
             WorldGenerator.cs <-x LootChestBuilder.cs, NewWorldBuilder.cs, WorldBarrierBuilder.cs
             WorldGenerator.cs <-o PlayerRespawn.cs
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]   // wakes up before everything else, so the land is there when the rest wakes up
[DisallowMultipleComponent]
public class WorldGenerator : MonoBehaviour
{
    public enum Biome { Grassland, Desert, Hills, Snow }

    // How much of each biome a spot has (they add up to 1; the edges between biomes have a bit of both).
    struct Biomes
    {
        public float grass, desert, hills, snow;
    }

    enum CaveKind { Rock, Desert, Snow }

    // A place chosen for a cave: its middle (x, z), the flattened ground's height, which way it faces, which cave and
    // what kind (the rocks put in it and the floor's paint).
    struct CaveSite
    {
        public Vector2 center;
        public float height;
        public float turn;
        public GameObject prefab;
        public CaveKind kind;
    }

    // One thing put in a chunk (a tree, bush, rock, chest, cave or a cave's rock), remembered so the chunk can let go of
    // it far away and make it again, the same, when the player comes back (Refill).
    class Placed
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;        // its local scale
        public bool cave;            // a cave (its rocks go inside it)
        public int inCave = -1;      // a cave's rock: that cave's place in the chunk's list (-1 = not in a cave)
        public bool gone;            // the player used it up (chopped, mined, picked, opened): it isn't made again
        public GameObject made;      // the thing in the world now (null while the chunk has let go of it)
    }

    // One square of land: its terrain (the ground) and its content (caves, trees, rocks, chests).
    class Chunk
    {
        public Vector2Int coord;
        public Vector3 corner;       // its corner with the smallest x and z (the terrain's position)
        public Terrain terrain;
        public GameObject content;
        public IEnumerator making;   // the steps still to do (null when it's made)
        public bool ready;           // completely made
        public bool shown;
        public readonly List<GameObject> sleeping = new List<GameObject>();   // groups of its content not woken up yet
        public GameObject filling;   // the group new trees, rocks and chests go into while it's being made
        public readonly List<Placed> things = new List<Placed>();   // everything put in it, in the order it was made
        public bool hasContent = true;   // its things are in the world (false = let go, far away)
        public IEnumerator refilling;    // bringing its things back (null when not)
    }

    // A used spot, like ObjectScatter's: shared by every chunk, so the rules keep their spacing across chunk edges.
    struct Spot
    {
        public Vector3 pos;
        public string group;
        public float clearance;
    }

    // ===== Fields =====
    [Header("World")]
    [Tooltip("0 = a new random world every game. Any other number = that same world every time (the Console says the " +
             "seed of each game, so a world you liked can be made again).")]
    [SerializeField] int seed = 0;

    [Tooltip("On: when the player dies, Respawn makes a brand-new world (Day 1 and everything fresh, like a new game; a " +
             "new seed unless Seed is set). Off: the player comes back at the start of this same world, as in the old world.")]
    [SerializeField] bool newWorldOnDeath = true;

    [Tooltip("The highest the ground can go, in meters (the rocky hills' tops reach about 100 m).")]
    [Min(30f)]
    [SerializeField] float maxHeight = 140f;

    [Header("Endless land (chunks)")]
    [Tooltip("How wide and long one square of land (a chunk) is, in meters. The land is made one chunk at a time.")]
    [Range(64f, 512f)]
    [SerializeField] float chunkSize = 256f;

    [Tooltip("Heights along one side of a chunk: 129 on a 256 m chunk = a height about every 2 m (the low-poly look). " +
             "Rounded to a power of two + 1.")]
    [Range(33, 513)]
    [SerializeField] int chunkHeights = 129;

    [Tooltip("Painted texture spots (and grass spots) along one side of a chunk: 128 = about every 2 m. Rounded to a power of two.")]
    [Range(32, 512)]
    [SerializeField] int chunkTextures = 128;

    [Tooltip("Chunks this close to the player (meters, to the chunk's nearest edge) are made ahead of time, so they're " +
             "ready before the player can see them.")]
    [Range(100f, 1000f)]
    [SerializeField] float loadDistance = 300f;

    [Tooltip("Chunks this close are shown. At least the camera's View Distance + 60 m.")]
    [Range(50f, 800f)]
    [SerializeField] float showDistance = 170f;

    [Tooltip("A chunk farther than this is hidden: not drawn, its trees, rocks and chests switched off. Its ground stays " +
             "solid, and everything in it stays as it was for when the player comes back.")]
    [Range(60f, 1000f)]
    [SerializeField] float hideDistance = 240f;

    [Tooltip("A chunk farther than this (meters, to its nearest edge) lets go of its trees, bushes, rocks, chests and " +
             "caves to save memory: the ones the player chopped, mined, picked or opened are remembered, the rest come back " +
             "the same when the player returns (its ground always stays). Kept at least Load Distance + half a chunk. " +
             "0 = keep everything (the game slows down the more land the player walks).")]
    [Range(0f, 3000f)]
    [SerializeField] float contentDistance = 500f;

    [Tooltip("Milliseconds of each frame spent making new land. More = chunks are ready sooner, but the game may stutter " +
             "(the web game is slower: keep it small).")]
    [Range(1f, 20f)]
    [SerializeField] float frameBudget = 4f;

    [Header("Biomes (meters)")]
    [Tooltip("Everything this close to the start is green grassland (it fades into the other biomes over 120 m).")]
    [SerializeField] float startRadius = 250f;

    [Tooltip("The desert can only begin this far from the start.")]
    [SerializeField] float desertFrom = 400f;

    [Tooltip("The snow can only begin this far from the start: the farthest biome, and the best place (diamonds, more " +
             "and better chests).")]
    [SerializeField] float snowFrom = 700f;

    [Tooltip("About how far it is from a hot (desert) area to a cold (snow) one: bigger = bigger biomes.")]
    [Range(300f, 5000f)]
    [SerializeField] float climateSize = 1100f;

    [Tooltip("About how big the rocky hills' areas are between the grassland.")]
    [Range(200f, 3000f)]
    [SerializeField] float hillsSize = 700f;

    [Tooltip("Snow covers the rocky hills above this height.")]
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
    [Tooltip("The grassland's caves to pick from (Tools > World > Build Rock Caves makes them). Each has children named " +
             "OreSpot where the rocks go.")]
    [SerializeField] GameObject[] cavePrefabs = new GameObject[0];

    [Tooltip("The chance a grassland chunk gets a cave, for each of its 2 tries (0.45 = about 1 cave every chunk).")]
    [Range(0f, 1f)]
    [SerializeField] float caveChance = 0.45f;

    [Tooltip("The flat ground made for each cave: a circle this wide (radius), a little bigger than the caves.")]
    [SerializeField] float caveRadius = 13f;

    [Header("Ores in the caves")]
    [Tooltip("The rocks put on a grassland cave's ore spots: the first few are iron, then copper, the rest stone.")]
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

    [Tooltip("The chance a desert chunk gets a cave, for each of its 2 tries.")]
    [Range(0f, 1f)]
    [SerializeField] float desertCaveChance = 0.5f;

    [Tooltip("The rocks in a desert cave: Gold (its rock needs an Iron Pickaxe) and iron (the Iron Ore above), the other " +
             "spots Sandstone (a hard desert rock that gives Stone; empty = Stone Ore). Snow caves have gold too.")]
    [SerializeField] GameObject goldOre;
    [SerializeField] GameObject sandstoneOre;

    [Tooltip("How many gold rocks a desert cave has (at least, at most; a lucky cave has Lucky Count). Each gives 2 - 4 gold.")]
    [SerializeField] Vector2Int goldPerCave = new Vector2Int(2, 3);

    [Tooltip("How many iron rocks a desert cave has too (at least, at most). Its other spots get sandstone.")]
    [SerializeField] Vector2Int desertIronPerCave = new Vector2Int(2, 3);

    [Header("Snow caves")]
    [Tooltip("The snow's caves (Tools > World > Build Snow Content makes them, of icy rock). Each has children named OreSpot " +
             "where the rocks go.")]
    [SerializeField] GameObject[] snowCavePrefabs = new GameObject[0];

    [Tooltip("The chance a snow chunk gets a cave, for each of its 2 tries (more than the other biomes: the snow is the " +
             "best place).")]
    [Range(0f, 1f)]
    [SerializeField] float snowCaveChance = 0.7f;

    [Tooltip("The diamond rock (Tools > World > Build Snow Content: it needs a Gold Pickaxe). A snow cave also has gold " +
             "(the desert's Gold Ore); its other spots get Stone Ore.")]
    [SerializeField] GameObject diamondOre;

    [Tooltip("How many diamond rocks a snow cave has (at least, at most; a lucky cave has Lucky Count). Each gives 1 - 3 " +
             "diamonds.")]
    [SerializeField] Vector2Int diamondPerCave = new Vector2Int(2, 3);

    [Tooltip("How many gold rocks a snow cave has too (at least, at most). Its other spots get stone.")]
    [SerializeField] Vector2Int snowGoldPerCave = new Vector2Int(2, 3);

    [Header("Lucky caves")]
    [Tooltip("The chance a desert cave is a lucky one with Lucky Count gold rocks, and a snow cave with Lucky Count " +
             "diamond rocks, instead of Gold / Diamond Per Cave (0.05 = 1 cave in 20).")]
    [Range(0f, 1f)]
    [SerializeField] float luckyCaveChance = 0.05f;

    [Tooltip("How many gold rocks (desert) or diamond rocks (snow) a lucky cave has. A cave has 7 ore spots: the best " +
             "rocks get theirs first.")]
    [Range(1, 7)]
    [SerializeField] int luckyCount = 4;

    [Header("Trees, bushes, rocks and chests")]
    [Tooltip("Sanyam's ObjectScatter: its Rules say what grows where (on which painted texture). Every chunk gets its " +
             "share of each rule's Count. Its own spawning is switched off here (it needs one fixed terrain).")]
    [SerializeField] ObjectScatter scatter;

    [Tooltip("A rule's Count is how many grow on this much ground (square meters) painted with its texture: 400,000 = " +
             "about 630 x 630 m, the old new world's grassland, so its forests are as thick as before. Smaller = more of " +
             "everything (more to draw), bigger = less.")]
    [Min(1000f)]
    [SerializeField] float rulesArea = 400000f;

    [Header("Links (found by themselves if empty)")]
    [Tooltip("The player, moved to the start.")]
    [SerializeField] Transform player;

    const float SpawnClearing = 6f;     // the start: a flat circle this wide (radius) with nothing growing on it
    const float CaveBlend = 10f;        // meters over which a cave's flat floor blends back into the land
    const float CaveSpacing = 70f;      // two caves in one chunk are at least this far apart
    const float CaveFromStart = 50f;    // no cave this close to the start
    const int CaveTries = 12;           // spots tried for each of a chunk's caves
    const int CavesPerChunk = 2;        // tries for a cave in each chunk (each with the biome's chance)
    const string OreSpotName = "OreSpot";
    const int TerrainGroup = 7;         // chunks with the same group join their edges (Unity's terrain neighbors)
    const float CheckEvery = 0.2f;      // seconds between looking for chunks to make, show or hide
    const float CellSize = 10f;         // the used-spots grid's squares, in meters (like ObjectScatter's)
    const int GroupSize = 40;           // things in one group of a chunk's content: each group wakes up in its own frame

    static WorldGenerator current;
    bool startingNewWorld;              // the player died and a new world is loading (it starts only once)
    Vector2 noiseAt;                    // where in the noise this world is (moves with the seed)
    Vector2 patchSeed;                  // where the forests' patch pattern starts (like ObjectScatter's)
    Vector3 spawn;                      // where the player starts (on the ground)
    float spawnLevel;                   // the start clearing's height, in meters
    Transform worldRoot;                // "Generated World": every chunk is under it
    readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    readonly List<Vector2Int> waiting = new List<Vector2Int>();   // chunks to make (the nearest first)
    readonly List<Chunk> refills = new List<Chunk>();             // chunks near again whose things come back (the nearest first)
    readonly Dictionary<Vector2Int, List<Spot>> placed = new Dictionary<Vector2Int, List<Spot>>();
    float widestClearance;              // the biggest clearance used so far, so the spacing checks look far enough
    bool building;                      // the chunk maker is running
    float nextCheck;
    readonly System.Diagnostics.Stopwatch budget = new System.Diagnostics.Stopwatch();
    readonly List<Chunk> waking = new List<Chunk>();   // shown chunks whose content is still waking up, group by group
    readonly System.Diagnostics.Stopwatch wakeTime = new System.Diagnostics.Stopwatch();

    // The generator of the world being played (null in the old world).
    public static WorldGenerator Current => current;

    public int Seed { get; private set; }

    // True when respawning makes a brand-new world instead of coming back here (PlayerRespawn asks): the new world, with
    // New World On Death on. False in the old world (no WorldGenerator).
    public static bool NewWorldOnRespawn => current != null && current.newWorldOnDeath;
    public Vector3 SpawnPoint => spawn;
    public float ChunkSize => chunkSize;
    public int ChunksMade => chunks.Count;

    // The start's terrain (the chunk the player starts on). Any spot: TerrainAt.
    public Terrain Terrain => TerrainAt(spawn);

    int HeightRes => Mathf.ClosestPowerOfTwo(Mathf.Max(32, chunkHeights - 1)) + 1;
    int TextureRes => Mathf.ClosestPowerOfTwo(Mathf.Max(16, chunkTextures));

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        current = this;
        Seed = seed != 0 ? seed : new System.Random().Next(1, int.MaxValue);   // System.Random: new every game
        Random.InitState(Seed);   // from here on, the same seed gives the same world
        noiseAt = new Vector2(Random.Range(1000f, 9000f), Random.Range(1000f, 9000f));
        patchSeed = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));
        worldRoot = new GameObject("Generated World").transform;
        if (layers == null || layers.Length == 0)
            Debug.LogWarning("World Generator: no ground textures, so no grass, trees, chests or deer. Run Tools > World > " +
                             "Build New World Scene again.", this);

        FitDistances();
        PickSpawn();
        LinkSpawners();
        MakeChunksNow(spawn, showDistance);   // the start and the land around it, before anything else wakes up
        MovePlayer();

        Debug.Log("World Generator: seed " + Seed + " (type it into Seed to get this world again), an endless world in " +
                  chunkSize + " m chunks, the player starts at " + spawn.ToString("F0") + ", " + chunks.Count +
                  " chunks made at the start (the desert from " + desertFrom + " m, the snow from " + snowFrom + " m).");
    }

    // The start's chunks are shown here, after every script in the scene has woken up: their trees, rocks and chests
    // then wake up the way ObjectScatter's did (it spawned them in its Start), all at once (behind the screen fader).
    void Start()
    {
        ShowOrHide(PlayerPosition(), true);
    }

    void Update()
    {
        WakeSome();
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckEvery;

        Vector3 at = PlayerPosition();
        QueueChunksNear(at);
        ShowOrHide(at);
        LetGoOrRefill(at);
        if (!building && (waiting.Count > 0 || refills.Count > 0)) StartCoroutine(Building());
    }

    void OnDestroy()
    {
        if (current == this) current = null;   // static values outlive the scene, so clear them

        // The ground made for this game isn't a file: it would stay in memory after the scene closes (every Play Again
        // would add more), so it goes with the scene.
        foreach (Chunk chunk in chunks.Values)
        {
            if (chunk.terrain != null && chunk.terrain.terrainData != null) Destroy(chunk.terrain.terrainData);
        }
    }

    // ===== My methods =====

    // Which biome a spot is in (the strongest one there), e.g. for a teammate's spawner.
    public Biome BiomeAt(Vector3 position)
    {
        Biomes b = BiomesAt(position.x, position.z);
        float most = Mathf.Max(Mathf.Max(b.grass, b.desert), Mathf.Max(b.hills, b.snow));
        if (most == b.grass) return Biome.Grassland;
        if (most == b.desert) return Biome.Desert;
        return most == b.snow ? Biome.Snow : Biome.Hills;
    }

    // The terrain of the chunk under a spot (null = that chunk isn't made yet).
    public Terrain TerrainAt(Vector3 position)
    {
        return chunks.TryGetValue(CoordOf(position), out Chunk chunk) ? chunk.terrain : null;
    }

    // ----- Making the chunks -----

    // Show Distance at least the camera's view (+ 60 m), Hide Distance past it, Load Distance past that.
    void FitDistances()
    {
        ViewDistance view = FindFirstObjectByType<ViewDistance>();   // found by itself: it wakes up after this
        float seen = view != null ? view.Distance : 100f;
        showDistance = Mathf.Max(showDistance, seen + 60f);
        hideDistance = Mathf.Max(hideDistance, showDistance + 40f);
        loadDistance = Mathf.Max(loadDistance, showDistance + 60f);
    }

    // Makes every chunk within "distance" of a spot right now, in one go (the start; a test jump). A chunk the
    // background maker is halfway through is finished here too.
    void MakeChunksNow(Vector3 at, float distance)
    {
        Vector2Int middle = CoordOf(at);
        int reach = Mathf.CeilToInt(distance / chunkSize) + 1;
        for (int dz = -reach; dz <= reach; dz++)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                Vector2Int coord = new Vector2Int(middle.x + dx, middle.y + dz);
                if (DistanceTo(coord, at) > distance) continue;
                if (!chunks.TryGetValue(coord, out Chunk chunk))
                {
                    waiting.Remove(coord);
                    chunk = NewChunk(coord);
                }
                IEnumerator making = chunk.making;
                while (making != null && making.MoveNext()) { }   // no waiting between the steps
                if (!chunk.hasContent)   // it let go of its things (it was far away): back now, all at once
                {
                    refills.Remove(chunk);
                    IEnumerator refilling = chunk.refilling ?? (chunk.refilling = Refill(chunk));
                    while (refilling.MoveNext()) { }
                }
            }
        }
    }

    // A chunk to make: its steps are run by MakeChunksNow (all at once) or by Building (a bit each frame).
    Chunk NewChunk(Vector2Int coord)
    {
        Chunk chunk = new Chunk { coord = coord, corner = CornerOf(coord) };
        chunks[coord] = chunk;
        chunk.making = MakeChunk(chunk);
        return chunk;
    }

    // Chunks within Load Distance that aren't made yet go on the waiting list.
    void QueueChunksNear(Vector3 at)
    {
        Vector2Int middle = CoordOf(at);
        int reach = Mathf.CeilToInt(loadDistance / chunkSize) + 1;
        for (int dz = -reach; dz <= reach; dz++)
        {
            for (int dx = -reach; dx <= reach; dx++)
            {
                Vector2Int coord = new Vector2Int(middle.x + dx, middle.y + dz);
                if (chunks.ContainsKey(coord) || waiting.Contains(coord)) continue;
                if (DistanceTo(coord, at) <= loadDistance) waiting.Add(coord);
            }
        }
    }

    // Makes the waiting chunks, and brings back the things of chunks that are near again (Refill), one job after the
    // other, nearest first, a few milliseconds of each frame (Frame Budget).
    IEnumerator Building()
    {
        building = true;
        while (waiting.Count > 0 || refills.Count > 0)
        {
            IEnumerator job = NextJob(PlayerPosition());
            if (job == null) continue;   // made, brought back or left behind meanwhile

            budget.Restart();
            while (job.MoveNext())   // false once it's done (also when MakeChunksNow finished it meanwhile)
            {
                yield return null;   // the rest next frame
                budget.Restart();
            }
            budget.Stop();
            ShowOrHide(PlayerPosition());
        }
        building = false;
    }

    // The nearest job: a chunk to make, or a chunk whose things come back. Null = that one needs nothing any more (made,
    // brought back, or the player went away again).
    IEnumerator NextJob(Vector3 at)
    {
        waiting.Sort((a, b) => DistanceTo(a, at).CompareTo(DistanceTo(b, at)));
        refills.Sort((a, b) => DistanceTo(a.coord, at).CompareTo(DistanceTo(b.coord, at)));
        bool refillFirst = refills.Count > 0 && (waiting.Count == 0 || DistanceTo(refills[0].coord, at) <= DistanceTo(waiting[0], at));
        if (refillFirst)
        {
            Chunk chunk = refills[0];
            refills.RemoveAt(0);
            if (chunk.hasContent || DistanceTo(chunk.coord, at) > loadDistance + chunkSize) return null;
            return chunk.refilling ?? (chunk.refilling = Refill(chunk));
        }
        Vector2Int next = waiting[0];
        waiting.RemoveAt(0);
        if (chunks.ContainsKey(next) || DistanceTo(next, at) > loadDistance + chunkSize) return null;   // made, or left behind
        return NewChunk(next).making;
    }

    // ----- Letting go of far things, and bringing them back -----

    // Content Distance, kept at least Load Distance + half a chunk, so a chunk at the edge doesn't let go of its things
    // and bring them back over and over. 0 = never let go.
    float ContentDistance => contentDistance <= 0f ? 0f : Mathf.Max(contentDistance, loadDistance + chunkSize * 0.5f);

    // Every check: one far chunk (past Content Distance, hidden) lets go of its things (one a check, so it never takes
    // long), and every chunk without its things that's within Load Distance again goes on the list to get them back.
    void LetGoOrRefill(Vector3 at)
    {
        float far = ContentDistance;
        if (far <= 0f) return;
        bool letGo = false;
        foreach (Chunk chunk in chunks.Values)
        {
            if (!chunk.ready) continue;
            float distance = DistanceTo(chunk.coord, at);
            if (chunk.hasContent)
            {
                if (letGo || chunk.shown || distance <= far) continue;
                LetGo(chunk);
                letGo = true;
            }
            else if (distance <= loadDistance && chunk.refilling == null && !refills.Contains(chunk)) refills.Add(chunk);
        }
    }

    // A far chunk lets go of its things: each one the player used up while it was there (chopped, mined, picked, a
    // chest bought) is remembered as gone, then the rest are thrown away (its ground stays). Refill brings them back.
    void LetGo(Chunk chunk)
    {
        foreach (Placed thing in chunk.things)
        {
            if (thing.gone) continue;
            thing.gone = thing.made == null || UsedUp(thing.made);
            thing.made = null;
        }
        waking.Remove(chunk);
        chunk.sleeping.Clear();
        chunk.filling = null;
        if (chunk.content != null) Destroy(chunk.content);
        chunk.content = null;
        chunk.hasContent = false;
    }

    // A thing that's still there but used up: an opened chest (they remove themselves when bought, unless Marko's
    // Despawn After Opened is off).
    static bool UsedUp(GameObject thing)
    {
        PerkChest chest = thing.GetComponent<PerkChest>();
        return chest != null && chest.IsOpened;
    }

    // A chunk's things made again from its list, the same as the first time, without the gone ones; a few each frame
    // (Pause). A cave gets its own group again and its rocks go inside it.
    IEnumerator Refill(Chunk chunk)
    {
        chunk.content = new GameObject("Content");
        chunk.content.transform.SetParent(chunk.terrain.transform, false);
        chunk.content.SetActive(false);   // switched on with the chunk (then a group at a time)
        chunk.filling = null;
        Dictionary<int, Transform> caves = new Dictionary<int, Transform>();
        for (int i = 0; i < chunk.things.Count; i++)
        {
            Placed thing = chunk.things[i];
            if (thing.gone || thing.prefab == null) continue;
            Transform parent;
            if (thing.cave) parent = GroupFor(chunk, true);
            else if (thing.inCave >= 0)
            {
                if (!caves.TryGetValue(thing.inCave, out parent)) continue;   // its cave isn't there
            }
            else parent = GroupFor(chunk);

            GameObject made = Instantiate(thing.prefab, thing.position, thing.rotation, parent);
            made.transform.localScale = thing.scale;
            thing.made = made;
            if (thing.cave) caves[i] = made.transform;
            if (Pause()) yield return null;
        }
        chunk.filling = null;
        chunk.hasContent = true;
        chunk.refilling = null;
        if (chunk.shown)   // shown while its things were coming back: switch them on now, a group at a time
        {
            chunk.content.SetActive(true);
            if (chunk.sleeping.Count > 0 && !waking.Contains(chunk)) waking.Add(chunk);
        }
    }

    // Remembers a thing just put in a chunk (what, where, which way, how big, in which cave). Returns its place in the
    // chunk's list.
    static int Record(Chunk chunk, GameObject prefab, GameObject made, int inCave, bool cave)
    {
        Transform t = made.transform;
        chunk.things.Add(new Placed
        {
            prefab = prefab,
            position = t.position,
            rotation = t.rotation,
            scale = t.localScale,
            cave = cave,
            inCave = inCave,
            made = made,
        });
        return chunk.things.Count - 1;
    }

    // True when this frame's Frame Budget is used up (the chunk maker then waits for the next frame).
    bool Pause() => budget.IsRunning && budget.Elapsed.TotalMilliseconds >= frameBudget;

    // Shows the chunks near the player and hides the far ones (their ground stays solid). wakeAll = everything in a
    // newly shown chunk wakes up now (the start, a test jump), else a group at a time (WakeSome).
    void ShowOrHide(Vector3 at, bool wakeAll = false)
    {
        foreach (Chunk chunk in chunks.Values)
        {
            if (!chunk.ready) continue;
            float distance = DistanceTo(chunk.coord, at);
            if (!chunk.shown && distance <= showDistance) Show(chunk, true, wakeAll);
            else if (chunk.shown && distance > hideDistance) Show(chunk, false, false);
        }
    }

    void Show(Chunk chunk, bool shown, bool wakeAll)
    {
        chunk.shown = shown;
        if (chunk.terrain != null) chunk.terrain.enabled = shown;   // drawn or not; its Terrain Collider stays
        if (chunk.content != null) chunk.content.SetActive(shown);
        if (!shown) return;
        Terrain.SetConnectivityDirty();                               // its edges join the chunks around it again
        if (wakeAll)
        {
            foreach (GameObject group in chunk.sleeping) if (group != null) group.SetActive(true);
            chunk.sleeping.Clear();
        }
        else if (chunk.sleeping.Count > 0 && !waking.Contains(chunk)) waking.Add(chunk);
    }

    // Wakes up the shown chunks' sleeping groups (trees, rocks, chests, caves): at least one group each frame, more
    // while half the Frame Budget lasts. Waking a thing (its Awake and Start, its colliders) costs a little, and a whole
    // chunk at once (hundreds of things) made the game stop for a moment, most of all in the web build.
    void WakeSome()
    {
        if (waking.Count == 0) return;
        wakeTime.Restart();
        bool woke = false;
        while (waking.Count > 0 && (!woke || wakeTime.Elapsed.TotalMilliseconds < frameBudget * 0.5f))
        {
            Chunk chunk = waking[0];
            if (!chunk.shown || chunk.sleeping.Count == 0)   // hidden again (it carries on when shown), or all awake
            {
                waking.RemoveAt(0);
                continue;
            }
            GameObject group = chunk.sleeping[0];
            chunk.sleeping.RemoveAt(0);
            if (group != null) group.SetActive(true);
            woke = true;
        }
        wakeTime.Stop();
    }

    // The group of a chunk's content the next thing goes into: a new one every Group Size things (and a cave always
    // gets its own: it's many rocks). Groups sleep (switched off) until the chunk is shown.
    Transform GroupFor(Chunk chunk, bool own = false)
    {
        if (!own && chunk.filling != null && chunk.filling.transform.childCount < GroupSize) return chunk.filling.transform;
        GameObject group = new GameObject((own ? "Cave " : "Group ") + (chunk.sleeping.Count + 1));
        group.transform.SetParent(chunk.content.transform, false);
        group.SetActive(false);
        chunk.sleeping.Add(group);
        if (!own) chunk.filling = group;
        return group.transform;
    }

    // One chunk, step by step (it waits a frame whenever Pause says so): the ground's heights, the start's clearing and
    // the caves' floors, the painted textures and grass, the terrain, the caves with their rocks, then the trees,
    // bushes, rocks and chests. Hidden until ShowOrHide shows it.
    IEnumerator MakeChunk(Chunk chunk)
    {
        Vector2Int coord = chunk.coord;
        int res = HeightRes;
        Ground ground = new Ground(chunk.corner, res, chunkSize / (res - 1), maxHeight);
        for (int zi = -1; zi <= res; zi++)
        {
            for (int xi = -1; xi <= res; xi++)
            {
                float x = chunk.corner.x + xi * ground.cell, z = chunk.corner.z + zi * ground.cell;
                ground.Set(zi, xi, NaturalHeight(x, z));
            }
            if (Pause()) yield return null;
        }

        if (Inside(chunk, spawn)) ground.FlattenTo(new Vector2(spawn.x, spawn.z), SpawnClearing, SpawnClearing, spawnLevel);
        System.Random rng = new System.Random(ChunkSeed(coord));
        List<CaveSite> caves = PickCaves(ground, rng);
        if (Pause()) yield return null;

        float[,,] paint = null;
        float[,] snowy = null;   // how much of the snow biome each painted spot is in (0 - 1)
        int[,] tufts = null;
        if (layers != null && layers.Length > 0)
        {
            int size = TextureRes;
            paint = new float[size, size, layers.Length];
            snowy = new float[size, size];
            float[] mix = new float[layers.Length];
            float step = chunkSize / size;
            for (int zi = 0; zi < size; zi++)
            {
                for (int xi = 0; xi < size; xi++)
                {
                    float x = chunk.corner.x + (xi + 0.5f) * step, z = chunk.corner.z + (zi + 0.5f) * step;
                    snowy[zi, xi] = PaintSpot(paint, mix, zi, xi, x, z, ground, caves);
                }
                if (Pause()) yield return null;
            }
            tufts = GrassCounts(paint, chunk.corner, step);
            if (Pause()) yield return null;
        }

        IEnumerator terrainSteps = MakingTerrain(chunk, ground, paint, tufts);
        while (terrainSteps.MoveNext()) yield return null;
        chunk.content = new GameObject("Content");
        chunk.content.transform.SetParent(chunk.terrain.transform, false);
        chunk.content.SetActive(false);   // nothing in it wakes up until the chunk is shown (then a group at a time)
        if (Pause()) yield return null;

        foreach (CaveSite site in caves) PlaceCave(chunk, site, GroupFor(chunk, true), rng);
        if (Pause()) yield return null;

        IEnumerator scattering = Scatter(chunk, ground, paint, snowy, rng);
        while (scattering.MoveNext()) yield return null;
        chunk.ready = true;
        chunk.making = null;   // lets go of this chunk's working arrays (heights, paint)
        chunk.filling = null;
    }

    // The terrain for a chunk: its heights, textures and grass, at its corner, joined to its neighbors, hidden for now.
    // Each slow step (the heights, the textures, the grass, the terrain with its collider) can wait for the next frame
    // (Pause), so making a chunk never stops the game for long in one go.
    IEnumerator MakingTerrain(Chunk chunk, Ground ground, float[,,] paint, int[,] tufts)
    {
        TerrainData data = new TerrainData { name = "World Chunk " + chunk.coord.x + ", " + chunk.coord.y };
        data.heightmapResolution = ground.res;   // first: changing it also changes the size
        data.size = new Vector3(chunkSize, maxHeight, chunkSize);
        data.SetHeights(0, 0, ground.Heights01());
        if (Pause()) yield return null;
        if (paint != null)
        {
            data.terrainLayers = layers;
            data.alphamapResolution = paint.GetLength(0);
            data.SetAlphamaps(0, 0, paint);
            if (Pause()) yield return null;
            if (tufts != null) AddGrass(data, tufts);
            if (Pause()) yield return null;
        }

        GameObject go = Terrain.CreateTerrainGameObject(data);   // a Terrain and its Terrain Collider
        go.name = "Chunk " + chunk.coord.x + ", " + chunk.coord.y;
        go.transform.SetParent(worldRoot, false);
        go.transform.position = chunk.corner;
        Terrain terrain = go.GetComponent<Terrain>();
        if (terrainMaterial != null) terrain.materialTemplate = terrainMaterial;
        terrain.detailObjectDistance = grassDistance;
        terrain.detailObjectDensity = 1f;
        terrain.allowAutoConnect = true;   // joins its edges to the chunks around it (no cracks between them)
        terrain.groupingID = TerrainGroup;
        terrain.enabled = false;           // shown by ShowOrHide when the player comes near
        chunk.terrain = terrain;
        Terrain.SetConnectivityDirty();
    }

    // ----- The ground's shape -----

    // The ground's height at a spot, in meters, before the caves and the start flatten bits of it: each biome's own
    // shape, mixed where biomes meet.
    float NaturalHeight(float x, float z)
    {
        Biomes b = BiomesAt(x, z);
        float height = 0f;
        if (b.grass > 0f) height += b.grass * (24f + Fbm(x, z, 0.0045f, 4, 0f) * 16f);                                   // rolling, easy to walk
        if (b.desert > 0f) height += b.desert * (21f + Fbm(x, z, 0.007f, 3, 300f) * 7f + Ridge(x, z, 0.02f, 600f) * 1.5f);  // low dunes
        if (b.hills > 0f) height += b.hills * (28f + Mathf.Pow(RidgedFbm(x, z, 0.004f, 3, 900f), 1.8f) * 75f);            // rocky, partly too steep
        if (b.snow > 0f) height += b.snow * (30f + Fbm(x, z, 0.004f, 4, 1200f) * 22f + Mathf.Pow(RidgedFbm(x, z, 0.006f, 2, 1500f), 2f) * 18f);   // snowy rolling hills
        return height;
    }

    // How much of each biome a spot has. A slow climate noise makes hot (desert) and cold (snow) areas, a second one the
    // rocky hills between the green grassland; the desert only from Desert From on, the snow only from Snow From on, and
    // everything within Start Radius is grassland.
    Biomes BiomesAt(float x, float z)
    {
        float distance = Mathf.Sqrt(x * x + z * z);
        float climate = Stretch(Fbm(x, z, 1f / climateSize, 2, 7000f));   // 0 = cold, 1 = hot
        float rugged = Stretch(Fbm(x, z, 1f / hillsSize, 2, 8000f));

        float desert = SmoothStep(0.6f, 0.7f, climate) * SmoothStep(desertFrom - 50f, desertFrom + 100f, distance);
        float snow = (1f - SmoothStep(0.3f, 0.4f, climate)) * SmoothStep(snowFrom - 50f, snowFrom + 100f, distance);
        float mild = Mathf.Clamp01(1f - desert - snow);
        float hills = mild * SmoothStep(0.55f, 0.65f, rugged);
        float grass = mild - hills;

        float start = 1f - SmoothStep(startRadius, startRadius + 120f, distance);   // the start: all grassland
        return new Biomes
        {
            grass = grass * (1f - start) + start,
            desert = desert * (1f - start),
            hills = hills * (1f - start),
            snow = snow * (1f - start),
        };
    }

    // 0 below a, 1 above b, smooth between.
    static float SmoothStep(float a, float b, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));

    // Noise sits mostly near the middle (0.3 - 0.7): this spreads it over 0 - 1.
    static float Stretch(float value) => Mathf.Clamp01((value - 0.5f) * 2.2f + 0.5f);

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

    // How steep the natural ground is at a spot, in degrees (before anything is flattened).
    float NaturalSlope(float x, float z)
    {
        const float d = 2f;
        float dx = NaturalHeight(x + d, z) - NaturalHeight(x - d, z);
        float dz = NaturalHeight(x, z + d) - NaturalHeight(x, z - d);
        return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz) / (2f * d)) * Mathf.Rad2Deg;
    }

    // ----- The start -----

    // A flat spot near the middle of the grassland (within 60 m of 0, 0), and its clearing's height: the ground there is
    // flattened to it when its chunk is made.
    void PickSpawn()
    {
        Vector2 best = Vector2.zero;
        float bestSlope = float.MaxValue;
        for (int i = 0; i < 200; i++)
        {
            Vector2 spot = i == 0 ? Vector2.zero : Random.insideUnitCircle * 60f;
            float slope = NaturalSlope(spot.x, spot.y);
            for (int k = 0; k < 8 && slope < bestSlope; k++)
            {
                float a = k * Mathf.PI / 4f;
                slope = Mathf.Max(slope, NaturalSlope(spot.x + Mathf.Cos(a) * SpawnClearing, spot.y + Mathf.Sin(a) * SpawnClearing));
            }
            if (slope < bestSlope)
            {
                bestSlope = slope;
                best = spot;
            }
            if (bestSlope < 6f) break;   // flat enough
        }

        float sum = 0f;
        int count = 0;
        for (float dx = -SpawnClearing; dx <= SpawnClearing; dx += 2f)
        {
            for (float dz = -SpawnClearing; dz <= SpawnClearing; dz += 2f)
            {
                if (dx * dx + dz * dz > SpawnClearing * SpawnClearing) continue;
                sum += NaturalHeight(best.x + dx, best.y + dz);
                count++;
            }
        }
        spawnLevel = count > 0 ? sum / count : NaturalHeight(best.x, best.y);
        spawn = new Vector3(best.x, spawnLevel, best.y);
        Remember(spawn, "Start", SpawnClearing + 2f);   // nothing spawns in the clearing
    }

    // ----- The caves -----

    // Up to 2 caves in a chunk: for each, a roll against the chance of the biome found (grassland, desert or snow) on
    // gentle ground, far enough from the chunk's edges that its flattened floor stays inside the chunk, away from the
    // start and the chunk's other cave. The ground under each is flattened (the cave's floor).
    List<CaveSite> PickCaves(Ground ground, System.Random rng)
    {
        List<CaveSite> found = new List<CaveSite>();
        float margin = caveRadius + CaveBlend + 4f;
        float room = chunkSize - margin * 2f;
        if (room <= 0f) return found;
        Vector2 start = new Vector2(spawn.x, spawn.z);

        for (int cave = 0; cave < CavesPerChunk; cave++)
        {
            float roll = (float)rng.NextDouble();   // this try's luck, against the biome's chance
            for (int i = 0; i < CaveTries; i++)
            {
                Vector2 center = new Vector2(ground.corner.x + margin + (float)rng.NextDouble() * room,
                                             ground.corner.z + margin + (float)rng.NextDouble() * room);
                if (Vector2.Distance(center, start) < CaveFromStart) continue;
                bool roomy = true;
                foreach (CaveSite other in found) roomy &= Vector2.Distance(other.center, center) >= CaveSpacing;
                if (!roomy) continue;

                Biomes b = BiomesAt(center.x, center.y);
                CaveKind kind;
                GameObject[] prefabs;
                float chance;
                if (b.grass >= 0.9f) { kind = CaveKind.Rock; prefabs = cavePrefabs; chance = caveChance; }
                else if (b.desert >= 0.9f) { kind = CaveKind.Desert; prefabs = desertCavePrefabs; chance = desertCaveChance; }
                else if (b.snow >= 0.9f) { kind = CaveKind.Snow; prefabs = snowCavePrefabs; chance = snowCaveChance; }
                else continue;   // between biomes, or the rocky hills
                if (roll >= chance || prefabs == null || prefabs.Length == 0) continue;
                if (ground.SteepestIn(center, caveRadius) > 16f) continue;   // not on a steep hillside

                GameObject prefab = prefabs[rng.Next(prefabs.Length)];
                if (prefab == null) continue;
                float height = ground.Flatten(center, caveRadius, CaveBlend);
                found.Add(new CaveSite { center = center, height = height, turn = (float)rng.NextDouble() * 360f, prefab = prefab, kind = kind });
                Remember(new Vector3(center.x, height, center.y), "Caves", caveRadius + 4f);   // nothing spawns inside it
                break;
            }
        }
        return found;
    }

    void PlaceCave(Chunk chunk, CaveSite site, Transform parent, System.Random rng)
    {
        Vector3 at = new Vector3(site.center.x, site.height, site.center.y);
        GameObject cave = Instantiate(site.prefab, at, Quaternion.Euler(0f, site.turn, 0f), parent);
        int index = Record(chunk, site.prefab, cave, -1, true);
        FillOres(chunk, index, cave.transform, site.kind, rng);
    }

    // Puts the rocks on a cave's ore spots (its children named OreSpot), in a random order: a grassland cave a few iron,
    // a few copper, the rest stone; a desert cave a few gold, a few iron, the rest sandstone; a snow cave a few diamond,
    // a few gold, the rest stone (1 desert / snow cave in 20 is lucky: 4 gold / 4 diamond). They settle onto the floor
    // by themselves (StandOnGround).
    void FillOres(Chunk chunk, int caveIndex, Transform cave, CaveKind kind, System.Random rng)
    {
        List<Transform> spots = new List<Transform>();
        foreach (Transform part in cave.GetComponentsInChildren<Transform>(true))
        {
            if (part.name.StartsWith(OreSpotName)) spots.Add(part);
        }
        for (int i = spots.Count - 1; i > 0; i--)   // shuffle, so the rare rocks aren't always in the same corner
        {
            int j = rng.Next(i + 1);
            (spots[i], spots[j]) = (spots[j], spots[i]);
        }

        // The first "rare" spots get the rare rocks, the rest the common rock.
        int first = 0, second = 0;
        GameObject rare = null, alsoRare = null, common = stoneOre;
        switch (kind)
        {
            case CaveKind.Rock:
                first = Between(ironPerCave, rng); rare = ironOre;
                second = Between(copperPerCave, rng); alsoRare = copperOre;
                break;
            case CaveKind.Desert:
                first = Lucky(goldPerCave, rng); rare = goldOre;
                second = Between(desertIronPerCave, rng); alsoRare = ironOre;
                if (sandstoneOre != null) common = sandstoneOre;
                break;
            case CaveKind.Snow:
                first = Lucky(diamondPerCave, rng); rare = diamondOre;
                second = Between(snowGoldPerCave, rng); alsoRare = goldOre;
                break;
        }
        for (int i = 0; i < spots.Count; i++)
        {
            GameObject ore = i < first ? rare : i < first + second ? alsoRare : common;
            if (ore == null) ore = common;
            if (ore == null) continue;
            GameObject made = Instantiate(ore, spots[i].position, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), cave);
            Record(chunk, ore, made, caveIndex, false);
        }
    }

    static int Between(Vector2Int range, System.Random rng) => rng.Next(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y) + 1);

    // How many of a cave's best rock: usually "range", but a lucky cave (Lucky Cave Chance, 1 in 20) gets Lucky Count.
    int Lucky(Vector2Int range, System.Random rng) => rng.NextDouble() < luckyCaveChance ? luckyCount : Between(range, rng);

    // ----- Painting the ground -----

    // The textures painted on one spot: grass with patches of mud in the grassland, sand in the desert, snow in the snow,
    // grass low down and rock higher up on the hills (snow above the Snow Line), bare rock on every steep slope (not the
    // desert's dunes), and the caves' and the start's own floors. Returns how much of the snow biome the spot is in.
    float PaintSpot(float[,,] map, float[] mix, int zi, int xi, float x, float z, Ground ground, List<CaveSite> caves)
    {
        int count = mix.Length;
        System.Array.Clear(mix, 0, count);

        Biomes b = BiomesAt(x, z);
        float height = ground.HeightAt(x, z);
        float mud = Mathf.Clamp01((Fbm(x, z, 0.02f, 2, 3000f) - 0.55f) * 3f);   // patches of mud in the grass
        float lowGreen = 1f - Mathf.InverseLerp(36f, 50f, height);             // the hills' low slopes stay green
        float cap = Mathf.InverseLerp(snowLine - 6f, snowLine + 6f, height);    // snow on the high hills
        Add(mix, grassLayer, b.grass * (1f - mud) + b.hills * lowGreen * (1f - cap));
        Add(mix, mudLayer, b.grass * mud);
        Add(mix, sandLayer, b.desert);
        Add(mix, rockLayer, b.hills * (1f - lowGreen) * (1f - cap));
        Add(mix, snowLayer, b.snow + b.hills * cap);

        // Steep ground (from 30 degrees, all rock by 42): rock, or snow up high. Not the desert's steep dunes: they stay
        // sand, so no rock (and no stones spawned on rock) in the desert.
        float steep = Mathf.InverseLerp(30f, 42f, ground.SlopeAt(x, z)) * (1f - b.desert);
        if (steep > 0f)
        {
            for (int i = 0; i < count; i++) mix[i] *= 1f - steep;
            Add(mix, rockLayer, steep * (1f - cap));
            Add(mix, snowLayer, steep * cap);
        }

        PaintPlaces(mix, new Vector2(x, z), caves);

        float total = 0f;
        for (int i = 0; i < count; i++) total += mix[i];
        if (total <= 0f)
        {
            Add(mix, grassLayer, 1f);
            total = 1f;
        }
        for (int i = 0; i < count; i++) map[zi, xi, i] = mix[i] / total;
        return b.snow;
    }

    // In and around a cave: a grassland cave rock and mud (no grass, so no tree grows into it); a desert cave sand and
    // dirt; a snow cave snow and dirt with rock at the walls (each with too little of one texture for the rules' rocks,
    // trees and chests to grow inside); at the start: half grass, half mud.
    void PaintPlaces(float[] mix, Vector2 spot, List<CaveSite> caves)
    {
        foreach (CaveSite site in caves)
        {
            float distance = Vector2.Distance(spot, site.center);
            if (distance > caveRadius + 6f) continue;
            bool floor = distance < caveRadius - 3f;
            System.Array.Clear(mix, 0, mix.Length);
            switch (site.kind)
            {
                case CaveKind.Desert:
                    Add(mix, sandLayer, floor ? 0.45f : 0.55f);   // a sandy floor, sand blowing in at the walls
                    Add(mix, floor ? mudLayer : rockLayer, floor ? 0.55f : 0.45f);
                    break;
                case CaveKind.Snow:
                    Add(mix, snowLayer, floor ? 0.45f : 0.55f);   // snow blown in, frozen dirt on the floor
                    Add(mix, floor ? mudLayer : rockLayer, floor ? 0.55f : 0.45f);
                    break;
                default:
                    float inside = floor ? 0.75f : 0.5f;   // mostly mud on the floor, rockier at the walls
                    Add(mix, mudLayer, inside);
                    Add(mix, rockLayer, 1f - inside);
                    break;
            }
            return;
        }
        if (Vector2.Distance(spot, new Vector2(spawn.x, spawn.z)) <= SpawnClearing + 3f)
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

    // How many grass tufts grow on each grass spot: thick on full grass, thinner where it fades into mud or the start
    // clearing, none on sand, rock or snow; in thicker and thinner patches (a noise).
    int[,] GrassCounts(float[,,] paint, Vector3 corner, float step)
    {
        if (grassTexture == null || grassDensity <= 0 || grassLayer < 0 || grassLayer >= paint.GetLength(2)) return null;
        int size = paint.GetLength(0);
        int[,] counts = new int[size, size];
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
        return counts;
    }

    // The grass tufts on a chunk's terrain: Unity draws them as its own terrain grass, waving in the wind, near the
    // camera only (Grass Distance).
    void AddGrass(TerrainData data, int[,] counts)
    {
        int size = counts.GetLength(0);
        data.SetDetailResolution(size, 16);                                  // one count per texture spot (about 2 x 2 m)
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
        data.wavingGrassTint = Color.white;      // Unity's own tint would turn the green grey
        data.wavingGrassAmount = grassBending;   // Unity's own 0.5 bent the tufts far over in gusts
        data.wavingGrassSpeed = grassWindSpeed;
        data.SetDetailLayer(0, 0, 0, counts);
    }

    // ----- Trees, bushes, rocks and chests (Sanyam's ObjectScatter rules, a share for each chunk) -----

    // Each rule spawns its share in this chunk: its Count for every Rules Area of ground painted with its texture (at
    // least Min Strength), so a chunk that's half grass gets half the trees of an all-grass one (e.g. 1.3 chests = 1,
    // or 2 a third of the time). Then like ObjectScatter: on that ground, in its patches, not too steep, with its
    // spacing, each with a random turn and size, under the chunk's content.
    IEnumerator Scatter(Chunk chunk, Ground ground, float[,,] paint, float[,] snowy, System.Random rng)
    {
        if (scatter == null || scatter.rules == null || paint == null) yield break;
        int size = paint.GetLength(0);
        int layerCount = paint.GetLength(2);
        float spotArea = chunkSize / size * (chunkSize / size);   // square meters of one painted spot
        Dictionary<(int, float), List<int>> grounds = new Dictionary<(int, float), List<int>>();

        foreach (ObjectScatter.ScatterRule rule in scatter.rules)
        {
            if (rule == null || rule.prefab == null || rule.count <= 0 || rule.terrainLayer < 0 || rule.terrainLayer >= layerCount) continue;
            List<int> spots = GroundFor(rule, paint, snowy, grounds);
            if (spots.Count == 0) continue;   // none of its ground in this chunk

            float expected = rule.count * spots.Count * spotArea / rulesArea;
            int want = Mathf.FloorToInt(expected);
            if (rng.NextDouble() < expected - want) want++;   // the leftover fraction as a chance

            int spawned = 0;
            for (int attempt = 0; spawned < want && attempt < want * 30; attempt++)
            {
                int spot = spots[rng.Next(spots.Count)];   // a random spot of its ground, then a random point in it
                float fx = (spot % size + (float)rng.NextDouble()) / size;
                float fz = (spot / size + (float)rng.NextDouble()) / size;
                Vector3 pos = new Vector3(chunk.corner.x + fx * chunkSize, 0f, chunk.corner.z + fz * chunkSize);
                if (!InPatch(pos, rule)) continue;
                if (ground.SlopeAt(pos.x, pos.z) > rule.maxSlope) continue;
                if (TooClose(pos, rule)) continue;

                pos.y = ground.HeightAt(pos.x, pos.z) + rule.yOffset;
                GameObject made = Instantiate(rule.prefab, pos, Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f), GroupFor(chunk));
                float scale = RandomSize(rule, rng);
                if (scale != 1f) made.transform.localScale = rule.prefab.transform.localScale * scale;
                Record(chunk, rule.prefab, made, -1, false);
                Remember(pos, GroupOf(rule), rule.clearance);
                spawned++;
                if (Pause()) yield return null;
            }
            if (Pause()) yield return null;
        }
    }

    // The painted spots (index = z x size + x) where a rule may grow: its texture painted at least Min Strength there.
    // Rules on the snow texture only in the snow biome: the rocky hills' snowy tops are above the tree line, so nothing
    // grows up there (and the snow's chests stay far from the start). Rules with the same texture and strength share
    // one list.
    List<int> GroundFor(ObjectScatter.ScatterRule rule, float[,,] paint, float[,] snowy, Dictionary<(int, float), List<int>> known)
    {
        (int, float) key = (rule.terrainLayer, rule.minStrength);
        if (known.TryGetValue(key, out List<int> spots)) return spots;

        spots = new List<int>();
        int size = paint.GetLength(0);
        bool snowRule = rule.terrainLayer == snowLayer;
        for (int zi = 0; zi < size; zi++)
        {
            for (int xi = 0; xi < size; xi++)
            {
                if (paint[zi, xi, rule.terrainLayer] < rule.minStrength) continue;
                if (snowRule && snowy[zi, xi] < 0.5f) continue;
                spots.Add(zi * size + xi);
            }
        }
        known[key] = spots;
        return spots;
    }

    // Is something of the same group within Min Spacing, or anything else within Clearance, of this spot? (Flat
    // distance only, like ObjectScatter.) Only the squares near the spot are looked at.
    bool TooClose(Vector3 pos, ObjectScatter.ScatterRule rule)
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
                    if (new Vector2(spot.pos.x - pos.x, spot.pos.z - pos.z).sqrMagnitude < need * need) return true;
                }
            }
        }
        return false;
    }

    // Remembers a used spot in its square, with its group and how much room it keeps from other kinds.
    void Remember(Vector3 pos, string group, float clearance)
    {
        Vector2Int cell = CellOf(pos);
        if (!placed.TryGetValue(cell, out List<Spot> spots))
        {
            spots = new List<Spot>();
            placed[cell] = spots;
        }
        spots.Add(new Spot { pos = pos, group = group, clearance = clearance });
        widestClearance = Mathf.Max(widestClearance, clearance);
    }

    static Vector2Int CellOf(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x / CellSize), Mathf.FloorToInt(pos.z / CellSize));

    // A rule's Spacing Group, or its prefab's name when that is empty (like ObjectScatter).
    static string GroupOf(ObjectScatter.ScatterRule rule)
    {
        if (!string.IsNullOrEmpty(rule.spacingGroup)) return rule.spacingGroup;
        return rule.prefab != null ? rule.prefab.name : rule.name;
    }

    // True if the spot is inside a patch (always, when the rule has no Patch Size): the same pattern for every rule, so
    // every tree look grows in the same forests (like ObjectScatter).
    bool InPatch(Vector3 pos, ObjectScatter.ScatterRule rule)
    {
        if (rule.patchSize <= 0f) return true;
        float noise = Mathf.PerlinNoise(patchSeed.x + pos.x / rule.patchSize, patchSeed.y + pos.z / rule.patchSize);
        return noise >= Mathf.Lerp(0.3f, 0.7f, rule.openGround);
    }

    // A random size between Min Size and Max Size (1 when they aren't set).
    static float RandomSize(ObjectScatter.ScatterRule rule, System.Random rng)
    {
        if (rule.minSize <= 0f || rule.maxSize <= 0f) return 1f;
        return Mathf.Lerp(rule.minSize, rule.maxSize, (float)rng.NextDouble());
    }

    // ----- Chunks: where they are -----

    // The chunk a spot is in (chunk 0, 0 has the start, 0, 0, in its middle).
    Vector2Int CoordOf(Vector3 position)
    {
        return new Vector2Int(Mathf.RoundToInt(position.x / chunkSize), Mathf.RoundToInt(position.z / chunkSize));
    }

    Vector3 CornerOf(Vector2Int coord)
    {
        return new Vector3(coord.x * chunkSize - chunkSize / 2f, 0f, coord.y * chunkSize - chunkSize / 2f);
    }

    // Meters from a spot to a chunk's nearest edge (0 = inside it), flat.
    float DistanceTo(Vector2Int coord, Vector3 at)
    {
        float dx = Mathf.Max(0f, Mathf.Abs(at.x - coord.x * chunkSize) - chunkSize / 2f);
        float dz = Mathf.Max(0f, Mathf.Abs(at.z - coord.y * chunkSize) - chunkSize / 2f);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    bool Inside(Chunk chunk, Vector3 spot) => CoordOf(spot) == chunk.coord;

    // A chunk's own seed: the same world seed always gives each chunk the same caves, trees and rocks, whatever order
    // the player walks in.
    int ChunkSeed(Vector2Int coord)
    {
        unchecked
        {
            int hash = Seed;
            hash = hash * 73856093 ^ coord.x * 19349663;
            hash = hash * 83492791 ^ coord.y * 50331653;
            return hash;
        }
    }

    // ----- The player -----

    Vector3 PlayerPosition()
    {
        if (player == null)
        {
            PlayerController found = FindFirstObjectByType<PlayerController>();
            if (found != null) player = found.transform;
        }
        if (player != null) return player.position;
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position : spawn;
    }

    // The player starts just above the clearing (and comes back there after dying, with New World On Death off).
    void MovePlayer()
    {
        PlayerPosition();   // finds the player
        if (player == null) return;
        Vector3 at = spawn + Vector3.up * 1.5f;   // a little above the ground: the player drops onto it
        Teleport(at);
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.SetRespawnPoint(at);
    }

    void Teleport(Vector3 at)
    {
        player.position = at;
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = at;
            body.velocity = Vector3.zero;
        }
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

    // Sanyam's ObjectScatter: its Rules are used chunk by chunk here, so its own spawning (one fixed terrain, at its
    // Start) is switched off before it starts.
    void LinkSpawners()
    {
        if (scatter == null) scatter = FindFirstObjectByType<ObjectScatter>();
        if (scatter != null) scatter.enabled = false;
        else Debug.LogWarning("World Generator: no ObjectScatter in the scene, so no trees, bushes, rocks or chests.", this);
    }

    // ----- Testing -----

    // Click the component's ⋮ menu in the Inspector while playing: takes the player to the nearest snow / desert.
    [ContextMenu("Test: Go To The Snow")]
    void TestGoToSnow() => GoToBiome(Biome.Snow);

    [ContextMenu("Test: Go To The Desert")]
    void TestGoToDesert() => GoToBiome(Biome.Desert);

    void GoToBiome(Biome biome)
    {
        if (!Application.isPlaying) return;
        Vector3 from = PlayerPosition();
        if (player == null) return;
        for (float radius = 100f; radius <= 6000f; radius += 50f)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                Vector3 spot = from + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                Biomes b = BiomesAt(spot.x, spot.z);
                float strength = biome == Biome.Snow ? b.snow : biome == Biome.Desert ? b.desert : biome == Biome.Hills ? b.hills : b.grass;
                if (strength < 0.95f || NaturalSlope(spot.x, spot.z) > 20f) continue;

                MakeChunksNow(spot, showDistance);   // the land there now, so the player doesn't fall
                ShowOrHide(spot, true);
                Terrain under = TerrainAt(spot);
                spot.y = (under != null ? under.SampleHeight(spot) + under.transform.position.y : NaturalHeight(spot.x, spot.z)) + 2f;
                Teleport(spot);
                Debug.Log("World Generator: test jump to the " + biome + " at " + spot.ToString("F0") + " (" + radius + " m away).");
                return;
            }
        }
        Debug.Log("World Generator: no " + biome + " found within 6 km.");
    }

    // ----- One chunk's ground heights -----

    // The heights of a chunk being made (in meters), with one extra height all round (so slopes at its edges are
    // measured from the real ground next door, and neighboring chunks paint their edges the same), and what's needed
    // to read and flatten them.
    class Ground
    {
        public readonly Vector3 corner;   // the chunk's corner
        public readonly int res;          // heights along a side (without the extra ring)
        public readonly float cell;       // meters between two heights
        readonly float maxHeight;
        readonly float[,] heights;        // [z + 1, x + 1], meters

        public Ground(Vector3 corner, int res, float cell, float maxHeight)
        {
            this.corner = corner;
            this.res = res;
            this.cell = cell;
            this.maxHeight = maxHeight;
            heights = new float[res + 2, res + 2];
        }

        // zi, xi from -1 to res.
        public void Set(int zi, int xi, float meters) => heights[zi + 1, xi + 1] = meters;

        // The heights the terrain wants: the chunk's own (no extra ring), 0 - 1 of Max Height.
        public float[,] Heights01()
        {
            float[,] values = new float[res, res];
            for (int zi = 0; zi < res; zi++)
            {
                for (int xi = 0; xi < res; xi++) values[zi, xi] = Mathf.Clamp01(heights[zi + 1, xi + 1] / maxHeight);
            }
            return values;
        }

        // The ground's height at any spot of the chunk (mixed between the 4 nearest heights), in meters.
        public float HeightAt(float x, float z)
        {
            float gx = Mathf.Clamp((x - corner.x) / cell + 1f, 0f, res + 0.999f);
            float gz = Mathf.Clamp((z - corner.z) / cell + 1f, 0f, res + 0.999f);
            int x0 = (int)gx, z0 = (int)gz;
            float fx = gx - x0, fz = gz - z0;
            float near = Mathf.Lerp(heights[z0, x0], heights[z0, x0 + 1], fx);
            float far = Mathf.Lerp(heights[z0 + 1, x0], heights[z0 + 1, x0 + 1], fx);
            return Mathf.Lerp(near, far, fz);
        }

        // How steep the ground is at a spot, in degrees (0 = flat).
        public float SlopeAt(float x, float z)
        {
            float dx = HeightAt(x + cell, z) - HeightAt(x - cell, z);
            float dz = HeightAt(x, z + cell) - HeightAt(x, z - cell);
            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz) / (2f * cell)) * Mathf.Rad2Deg;
        }

        // The steepest ground in a circle (its middle, and 8 spots on its edge and halfway).
        public float SteepestIn(Vector2 center, float radius)
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

        // Levels the ground in a circle to its average height, blending back into the land around it over "blend"
        // meters. Returns that height in meters.
        public float Flatten(Vector2 center, float radius, float blend)
        {
            float sum = 0f;
            int count = 0;
            ForHeightsNear(center, radius, (zi, xi, distance) => { sum += heights[zi, xi]; count++; });
            float level = count > 0 ? sum / count : HeightAt(center.x, center.y);
            FlattenTo(center, radius, blend, level);
            return level;
        }

        // Levels the ground in a circle to "level" (meters), blending back over "blend" meters.
        public void FlattenTo(Vector2 center, float radius, float blend, float level)
        {
            ForHeightsNear(center, radius + blend, (zi, xi, distance) =>
            {
                float amount = distance <= radius ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (distance - radius) / blend);
                heights[zi, xi] = Mathf.Lerp(heights[zi, xi], level, amount);
            });
        }

        // Calls "use" for every height in a circle (grid places with the extra ring), with its distance from the middle.
        void ForHeightsNear(Vector2 center, float radius, System.Action<int, int, float> use)
        {
            int last = res + 1;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - corner.x) / cell) + 1);
            int x1 = Mathf.Min(last, Mathf.CeilToInt((center.x + radius - corner.x) / cell) + 1);
            int z0 = Mathf.Max(0, Mathf.FloorToInt((center.y - radius - corner.z) / cell) + 1);
            int z1 = Mathf.Min(last, Mathf.CeilToInt((center.y + radius - corner.z) / cell) + 1);
            for (int zi = z0; zi <= z1; zi++)
            {
                for (int xi = x0; xi <= x1; xi++)
                {
                    float x = corner.x + (xi - 1) * cell, z = corner.z + (zi - 1) * cell;
                    float distance = Vector2.Distance(center, new Vector2(x, z));
                    if (distance <= radius) use(zi, xi, distance);
                }
            }
        }
    }
}
