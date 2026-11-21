/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: An object named "World Barrier" in a world's scene. The new world's World Generator makes one by itself; for
        the old world (or any map), Tools > World > Add World Barrier puts one around the map. By hand: Create Empty,
        add this, put the object in the middle of the map at (or below) the lowest ground, and set Size and Height.
        Keep its Rotation 0 and Scale 1.
Unity Editor: The cyan box in the Scene view is where the barrier stands. Size = how wide (x) and long (z) the inside
              is, in meters; Height = how tall the wall is from this object up. The Look numbers can be changed while
              playing. Guide: Docs/World Barrier Setup.md
              Testing: Play and walk to the edge of the map: a striped, glowing wall shows as you come near, lights up
              and ripples where you are, and stops you.
Purpose: The edge of the world, so a player can tell by looking that they can't go further: a see-through force field
         with slowly sliding stripes all the way round the map, faint from afar, bright and rippling where the player
         comes close (instead of mountains along the edge).
         - Solid for the player, enemies and anything with physics; rays (aiming, spawning, the player's sight) pass
           through it (its walls are on the Ignore Raycast layer and stand just outside the line).
         - WorldBarrier.Inside(spot) tells other scripts whether a spot is inside, so deer don't walk out and nothing
           is spawned outside (Sanyam's ObjectScatter, the AnimalSpawner and Marko's EnemySpawner ask it).
Requirement: WorldBarrier.cs o-> PlayerController.cs
             WorldBarrier.cs <-x EnemySpawner.cs, NewWorldBuilder.cs, ObjectScatter.cs, WorldBarrierBuilder.cs, WorldGenerator.cs
             WorldBarrier.cs <-o AnimalSpawner.cs, DesertMonsterSpawner.cs, WildAnimal.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // ShadowCastingMode, LightProbeUsage, ReflectionProbeUsage

[DisallowMultipleComponent]
public class WorldBarrier : MonoBehaviour
{
    // ===== Fields =====
    [Header("Where")]
    [Tooltip("How wide (x) and long (z) the inside is, in meters. Its middle is this object.")]
    [SerializeField] Vector2 size = new Vector2(1000f, 1000f);

    [Tooltip("How tall the wall is, in meters, from this object up (put this object at or below the lowest ground).")]
    [Min(1f)]
    [SerializeField] float height = 200f;

    [Tooltip("On = nothing with a body (the player, enemies, thrown things) goes through. Off = only a look.")]
    [SerializeField] bool solid = true;

    [Header("Look")]
    [Tooltip("The force field's color.")]
    [SerializeField] Color color = new Color(0.3f, 0.8f, 1f);

    [Tooltip("How much it shows from afar (0 = not at all until you come near).")]
    [Range(0f, 1f)]
    [SerializeField] float farAlpha = 0.12f;

    [Tooltip("How much it shows right where the player is.")]
    [Range(0f, 1f)]
    [SerializeField] float nearAlpha = 0.8f;

    [Tooltip("How far around the player (meters) the wall lights up and ripples.")]
    [Min(0.1f)]
    [SerializeField] float glowRadius = 12f;

    [Tooltip("Meters between two stripes.")]
    [Min(0.1f)]
    [SerializeField] float stripeSpacing = 2.5f;

    [Tooltip("How fast the stripes slide up the wall (stripes per second).")]
    [Range(0f, 3f)]
    [SerializeField] float stripeSpeed = 0.4f;

    [Tooltip("The wall fades out this many meters above the player, so it doesn't stand like a cliff over the whole world.")]
    [Min(1f)]
    [SerializeField] float fadeAbove = 40f;

    const string ShaderName = "WorldBarrier";   // Assets/Effects/WorldBarrier/Resources/WorldBarrier.shader (Resources: builds keep it)
    const float WallThickness = 10f;            // the solid walls stand just outside the line, this thick
    const int IgnoreRaycastLayer = 2;           // rays pass through, bodies bump into it

    static readonly List<WorldBarrier> active = new List<WorldBarrier>();
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int FarAlphaId = Shader.PropertyToID("_FarAlpha");
    static readonly int NearAlphaId = Shader.PropertyToID("_NearAlpha");
    static readonly int GlowRadiusId = Shader.PropertyToID("_GlowRadius");
    static readonly int StripeSpacingId = Shader.PropertyToID("_StripeSpacing");
    static readonly int StripeSpeedId = Shader.PropertyToID("_StripeSpeed");
    static readonly int FadeAboveId = Shader.PropertyToID("_FadeAbove");
    static readonly int PlayerPosId = Shader.PropertyToID("_PlayerPos");

    GameObject look;       // the see-through wall (a child), made in Start
    GameObject walls;      // the solid parts (a child)
    Mesh mesh;
    Material material;
    Transform player;      // found by itself (PlayerController); the camera if there's none
    bool built;

    public Vector2 Size => size;
    public float Height => height;

    // ===== Unity messages (Unity calls these) =====

    void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
    }

    void OnDisable()
    {
        active.Remove(this);
    }

    // In Start, not Awake: a script that makes the barrier in code (the World Generator) sets its size first.
    void Start()
    {
        Build();
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }

    // Statics outlive Play sessions when Unity skips reloading scripts, so start clean every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        active.Clear();
    }

    // The wall lights up around the player: it's told where they are every frame.
    void Update()
    {
        if (material == null) return;
        if (player == null) FindPlayer();
        Vector3 at = player != null ? player.position : transform.position;
        material.SetVector(PlayerPosId, at);
    }

    // Look numbers changed in the Inspector while playing: show them at once.
    void OnValidate()
    {
        if (material != null) ApplyLook();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * height / 2f, new Vector3(size.x, height, size.y));
    }

    // ===== My methods =====

    // True if a spot is inside the world's barrier, at least "margin" meters away from it (always true when the scene
    // has no barrier). e.g. a spawner: if (!WorldBarrier.Inside(spot, 2f)) try another spot;
    public static bool Inside(Vector3 point, float margin = 0f)
    {
        if (active.Count == 0) return true;
        foreach (WorldBarrier barrier in active)
        {
            if (barrier.Contains(point, margin)) return true;
        }
        return false;
    }

    // True if a spot is inside this barrier, at least "margin" meters away from it (height doesn't matter).
    public bool Contains(Vector3 point, float margin = 0f)
    {
        Vector3 local = point - transform.position;
        return Mathf.Abs(local.x) <= size.x / 2f - margin && Mathf.Abs(local.z) <= size.y / 2f - margin;
    }

    // Changes where the barrier stands (e.g. the World Generator, for the world it made). Remade at once if it's up.
    public void SetArea(Vector2 newSize, float newHeight)
    {
        size = new Vector2(Mathf.Max(1f, newSize.x), Mathf.Max(1f, newSize.y));
        height = Mathf.Max(1f, newHeight);
        if (built) Build();
    }

    // The see-through wall all round the inside, and (Solid) the walls that stop bodies.
    void Build()
    {
        RemoveParts();
        built = true;

        Shader shader = Resources.Load<Shader>(ShaderName);
        if (shader != null)
        {
            material = new Material(shader) { name = "World Barrier" };
            ApplyLook();
            mesh = WallMesh();
            look = new GameObject("Barrier Look");
            look.transform.SetParent(transform, false);
            look.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = look.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        else Debug.LogWarning(name + ": the barrier's shader (Assets/Effects/WorldBarrier/Resources/WorldBarrier.shader) " +
                              "is missing, so it can't be seen.", this);

        if (!solid) return;
        walls = new GameObject("Barrier Walls") { layer = IgnoreRaycastLayer };
        walls.transform.SetParent(transform, false);
        float halfX = size.x / 2f, halfZ = size.y / 2f, t = WallThickness, middle = height / 2f;
        AddWall(new Vector3(-halfX - t / 2f, middle, 0f), new Vector3(t, height, size.y + t * 2f));
        AddWall(new Vector3(halfX + t / 2f, middle, 0f), new Vector3(t, height, size.y + t * 2f));
        AddWall(new Vector3(0f, middle, -halfZ - t / 2f), new Vector3(size.x + t * 2f, height, t));
        AddWall(new Vector3(0f, middle, halfZ + t / 2f), new Vector3(size.x + t * 2f, height, t));
    }

    void AddWall(Vector3 center, Vector3 wallSize)
    {
        BoxCollider box = walls.AddComponent<BoxCollider>();
        box.center = center;
        box.size = wallSize;
    }

    // Takes away the parts made before (when the barrier is moved or resized while playing).
    void RemoveParts()
    {
        if (look != null) Destroy(look);
        if (walls != null) Destroy(walls);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }

    // Four upright sides around the inside, from this object up to Height (the shader draws both faces).
    Mesh WallMesh()
    {
        float halfX = size.x / 2f, halfZ = size.y / 2f;
        Vector3[] corners =
        {
            new Vector3(-halfX, 0f, -halfZ), new Vector3(halfX, 0f, -halfZ),
            new Vector3(halfX, 0f, halfZ), new Vector3(-halfX, 0f, halfZ),
        };
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            Vector3 a = corners[i], b = corners[(i + 1) % 4];
            int first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(a + Vector3.up * height);
            vertices.Add(b + Vector3.up * height);
            triangles.AddRange(new[] { first, first + 2, first + 1, first + 2, first + 3, first + 1 });
        }
        Mesh made = new Mesh { name = "World Barrier" };
        made.SetVertices(vertices);
        made.SetTriangles(triangles, 0);
        made.RecalculateBounds();
        return made;
    }

    void ApplyLook()
    {
        material.SetColor(ColorId, color);
        material.SetFloat(FarAlphaId, farAlpha);
        material.SetFloat(NearAlphaId, nearAlpha);
        material.SetFloat(GlowRadiusId, glowRadius);
        material.SetFloat(StripeSpacingId, stripeSpacing);
        material.SetFloat(StripeSpeedId, stripeSpeed);
        material.SetFloat(FadeAboveId, fadeAbove);
    }

    void FindPlayer()
    {
        PlayerController found = FindFirstObjectByType<PlayerController>();
        if (found != null) player = found.transform;
        else if (Camera.main != null) player = Camera.main.transform;
    }
}
