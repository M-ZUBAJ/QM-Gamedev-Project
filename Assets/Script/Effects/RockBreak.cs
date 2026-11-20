/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: A rock's top object (Turki's StoneNode, CopperNode, IronNode...). Tools > Effects > Build Rock Break (also run by
        Tools > World > Use Low Poly Rocks) puts it on every rock prefab and fills in Debris and Dust Color.
Unity Editor: Testing: Play and break a rock (punch it, or hit it with a pickaxe): pieces of the same stone fly out,
              bounce on the ground and shrink away, with gravel and a puff of dust.
              ⋮ (top right of this component) > Test: Break Here shows it while playing without breaking the rock.
Purpose: When a rock breaks (Turki's ResourceNode reaches 0 health and removes it at once), it bursts into chunks made
         of its own material (a sand rock breaks into sand-colored pieces, a copper rock into copper ones), small
         gravel and a cloud of dust in Dust Color. Bigger rocks break into more and bigger pieces. The chunks tumble,
         bounce off the ground and anything else there, then shrink away.
         What the rock looks like is remembered when the game starts, because by the time it's removed its parts may
         already be gone. A rock removed without being broken (the scene closing) shows nothing.
Requirement: RockBreak.cs o-> ResourceNode.cs
             RockBreak.cs <-x RockBreakBuilder.cs
*/

using UnityEngine;

[DisallowMultipleComponent]
public class RockBreak : MonoBehaviour
{
    // ===== Fields =====
    [Header("Look")]
    [Tooltip("The debris: Assets/Prefabs/Effects/RockDebris (the chunks, with Gravel and Dust inside it), made by " +
             "Tools > Effects > Build Rock Break.")]
    [SerializeField] ParticleSystem debris;

    [Tooltip("The color of the dust and gravel. The tool sets it from the rock's material (its average color, a bit lighter).")]
    [SerializeField] Color dustColor = new Color(0.66f, 0.65f, 0.62f);

    [Header("How much (for a rock about 1.3 m big: bigger rocks give more)")]
    [Tooltip("Chunks of stone (3D pieces that bounce).")]
    [Min(0)]
    [SerializeField] int chunks = 16;

    [Tooltip("Small gravel bits.")]
    [Min(0)]
    [SerializeField] int gravel = 40;

    [Tooltip("Dust puffs.")]
    [Min(0)]
    [SerializeField] int dust = 10;

    [Tooltip("How big an average chunk is, in meters.")]
    [Range(0.05f, 0.6f)]
    [SerializeField] float chunkSize = 0.22f;

    [Tooltip("How fast the pieces fly out, in meters per second.")]
    [Range(0.5f, 10f)]
    [SerializeField] float burstSpeed = 3.2f;

    const float ReferenceSize = 1.3f;   // meters: the rock size the amounts above are for

    ResourceNode node;       // Turki's: the rock's health (missing = never breaks by itself)
    Mesh shape;              // the rock's shape (RockLook picks it in Awake, before Start)
    Material stone;          // its material: the chunks are made of it
    Matrix4x4 shapeOnRock = Matrix4x4.identity;   // where the shape sits in the rock's own space
    bool played;             // the break already happened (so it never happens twice)
    static bool quitting;    // the game is closing: removed rocks show nothing

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        node = GetComponent<ResourceNode>();
        Remember();
    }

    // Turki's ResourceNode removes a broken rock at once: this is the last moment it's still here.
    void OnDestroy()
    {
        if (played || quitting || !gameObject.scene.isLoaded) return;
        if (node == null || node.currentHealth > 0f) return;   // not broken (e.g. removed by something else)
        played = true;
        Burst();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    static void OnQuitting()
    {
        quitting = true;
    }

    [ContextMenu("Test: Break Here")]
    void TestBreak()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Press Play first.");
            return;
        }
        Burst();   // the rock stays
    }

    // ===== My methods =====

    // The rock bursts into chunks, gravel and dust (it doesn't remove the rock: ResourceNode does that).
    public void Burst()
    {
        if (debris == null)
        {
            Debug.LogWarning(name + ": RockBreak has no Debris. Run Tools > Effects > Build Rock Break.");
            return;
        }

        Bounds box = WorldBox();
        float size = Mathf.Pow(Mathf.Max(0.001f, box.size.x * box.size.y * box.size.z), 1f / 3f);   // about how big it is
        float k = Mathf.Max(0.2f, size / ReferenceSize);   // 1 = a normal rock, 2 = twice as big

        ParticleSystem chunkSystem = Instantiate(debris, box.center, Quaternion.identity);   // not a child: it stays when the rock goes
        if (stone != null) chunkSystem.GetComponent<ParticleSystemRenderer>().sharedMaterial = stone;   // pieces of the same stone
        ParticleSystem gravelSystem = Part(chunkSystem, "Gravel");
        ParticleSystem dustSystem = Part(chunkSystem, "Dust");
        chunkSystem.Play(true);   // their own emission is off: only the Emit calls below make pieces

        EmitChunks(chunkSystem, box, k);
        if (gravelSystem != null) EmitGravel(gravelSystem, box, k);
        if (dustSystem != null) EmitDust(dustSystem, box, k);

        Destroy(chunkSystem.gameObject, Longest(chunkSystem) + 0.5f);
    }

    // What the rock looks like: its shape, its material and where the shape sits on it.
    void Remember()
    {
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter == null) return;
        shape = filter.sharedMesh;
        MeshRenderer look = filter.GetComponent<MeshRenderer>();
        stone = look != null ? look.sharedMaterial : null;
        shapeOnRock = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
    }

    // 3D chunks of the rock's stone: thrown out and up from inside it, tumbling (RockDebris turns and shrinks them).
    void EmitChunks(ParticleSystem system, Bounds box, float k)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(chunks * k * k), Mathf.Min(chunks, 4), 80);
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 from = PointIn(box, 0.7f);
            emit.position = from;
            emit.velocity = (Outward(from - box.center) * Random.Range(0.4f, 1f) + Vector3.up * Random.Range(0.6f, 1.2f)) * burstSpeed;
            emit.startSize = chunkSize * Mathf.Sqrt(k) * Random.Range(0.5f, 1.5f);
            system.Emit(emit, 1);
        }
    }

    // Small gravel bits, faster and further than the chunks, a darker shade of the dust.
    void EmitGravel(ParticleSystem system, Bounds box, float k)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(gravel * k * k), Mathf.Min(gravel, 8), 160);
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 from = PointIn(box, 0.8f);
            emit.position = from;
            emit.velocity = (Outward(from - box.center) * Random.Range(0.5f, 1.3f) + Vector3.up * Random.Range(0.4f, 1.1f)) * burstSpeed * 1.2f;
            emit.startSize = Random.Range(0.03f, 0.08f) * Mathf.Sqrt(k);
            Color shade = Color.Lerp(dustColor * 0.7f, dustColor, Random.value);
            shade.a = 1f;
            emit.startColor = shade;
            system.Emit(emit, 1);
        }
    }

    // Soft dust puffs low around the rock, rolling out slowly.
    void EmitDust(ParticleSystem system, Bounds box, float k)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(dust * k), Mathf.Min(dust, 3), 40);
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 from = PointIn(box, 0.8f);
            from.y = box.min.y + box.size.y * Random.Range(0.2f, 0.5f);   // the lower half, above the part in the ground
            emit.position = from;
            emit.velocity = Outward(from - box.center) * Random.Range(0.3f, 1.1f) * burstSpeed * 0.35f + Vector3.up * Random.Range(0.1f, 0.4f);
            emit.startSize = Random.Range(0.5f, 1.1f) * k;
            Color puff = Color.Lerp(dustColor, Color.white, Random.Range(0f, 0.25f));
            puff.a = Random.Range(0.35f, 0.6f);
            emit.startColor = puff;
            system.Emit(emit, 1);
        }
    }

    // The rock's box in the world, from its remembered shape (its parts may already be gone when it breaks).
    Bounds WorldBox()
    {
        Matrix4x4 toWorld = transform.localToWorldMatrix * shapeOnRock;
        Bounds local = shape != null ? shape.bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
        Vector3 middle = local.center;
        Vector3 half = local.extents;
        Bounds box = new Bounds(toWorld.MultiplyPoint3x4(middle), Vector3.zero);
        for (int i = 0; i < 8; i++)   // the 8 corners of the box
        {
            Vector3 corner = middle + Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            box.Encapsulate(toWorld.MultiplyPoint3x4(corner));
        }
        return box;
    }

    // A random point inside the box (share = how much of the box, from its middle), never in the part under the ground.
    static Vector3 PointIn(Bounds box, float share)
    {
        Vector3 half = box.extents * share;
        Vector3 point = box.center + new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), Random.Range(-half.z, half.z));
        point.y = Mathf.Max(point.y, box.min.y + box.size.y * 0.2f);   // the bottom part of a rock is in the ground
        return point;
    }

    // Flat on the ground, away from the rock's middle (any way if right in the middle).
    static Vector3 Outward(Vector3 offset)
    {
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f)
        {
            Vector2 any = Random.insideUnitCircle.normalized;
            offset = new Vector3(any.x, 0f, any.y);
        }
        return offset.normalized;
    }

    // One of the debris' parts (Gravel, Dust), or null if the prefab hasn't got it.
    static ParticleSystem Part(ParticleSystem root, string partName)
    {
        Transform part = root.transform.Find(partName);
        return part != null ? part.GetComponent<ParticleSystem>() : null;
    }

    // Seconds until the last piece of any part has faded.
    static float Longest(ParticleSystem root)
    {
        float longest = 0f;
        foreach (ParticleSystem part in root.GetComponentsInChildren<ParticleSystem>())
            longest = Mathf.Max(longest, part.main.startLifetime.constantMax);
        return longest;
    }
}
