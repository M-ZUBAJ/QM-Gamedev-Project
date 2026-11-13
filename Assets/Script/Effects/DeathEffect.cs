/*
First Date: 13/11/2026
Date: 13/11/2026
Author: Tony
Use by: The top object of a creature that can die, e.g. DeerNode or SlimeEnemy. Tools > Effects > Build Death Particles
        puts it on every animal and enemy made by Tony's tools (and Build Wild Animal / Build Monster Enemy add it too).
Unity Editor: Testing: Play, then kill a deer: it bursts into green particles shaped like its body. Kill a slime: it
              plays its death animation, and during it the body breaks into red particles until nothing is left.
              ⋮ (top right of this component) > Test: Burst Here shows the burst without killing anything.
Purpose: What's left when a creature dies, so you can tell at a glance what you killed:
         - Harmless creatures (deer) turn green, harmful ones (slimes) turn red. Auto = harmful if it has Marko's
           EnemyAttack.
         - A creature that dies at once (the deer: Turki's ResourceNode removes it straight away) bursts into
           particles in the shape of its body (along its bones).
         - A creature with a death animation (the slime) calls Dissolve (from EnemyAnimator.PlayDeath): it falls,
           then particles come off its body's surface faster and faster while the body shrinks into them, and a last
           pop ends it as the animation ends.
         A creature removed without being killed (an animal left far behind, the scene closing) shows nothing.
Requirement: DeathEffect.cs o-> ResourceNode.cs, EnemyCombat.cs, EnemyAttack.cs
             DeathEffect.cs <-x DeathEffectBuilder.cs
             DeathEffect.cs <-o EnemyAnimator.cs
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeathKind { Auto, Harmless, Harmful }

[DisallowMultipleComponent]
public class DeathEffect : MonoBehaviour
{
    // ===== Fields =====
    [Header("Look")]
    [Tooltip("The particles to use: Assets/Prefabs/Effects/DeathParticles (made by Tools > Effects > Build Death Particles).")]
    [SerializeField] ParticleSystem particles;

    [Tooltip("Auto = Harmful (red) if it has Marko's EnemyAttack, else Harmless (green).")]
    [SerializeField] DeathKind kind = DeathKind.Auto;

    [Tooltip("The color of creatures that don't hurt the player (deer).")]
    [SerializeField] Color harmlessColor = new Color(0.3f, 1f, 0.35f);

    [Tooltip("The color of creatures that hurt the player (slimes).")]
    [SerializeField] Color harmfulColor = new Color(1f, 0.12f, 0.08f);

    [Header("Dying at once (deer)")]
    [Tooltip("How many particles the body bursts into.")]
    [Min(0)]
    [SerializeField] int burstCount = 220;

    [Tooltip("How fast the burst flies apart (meters per second).")]
    [Min(0f)]
    [SerializeField] float burstSpeed = 1.6f;

    [Header("With a death animation (slime)")]
    [Tooltip("Seconds after dying before the body starts turning into particles (it falls first). Build Death Particles sets it from the death animation.")]
    [Min(0f)]
    [SerializeField] float dissolveStart = 0.4f;

    [Tooltip("Seconds it takes the whole body to turn into particles. Build Death Particles makes it end with the death animation.")]
    [Min(0.1f)]
    [SerializeField] float dissolveSeconds = 1.2f;

    [Tooltip("The most particles per second coming off the body while it turns into particles.")]
    [Min(0f)]
    [SerializeField] float dissolveRate = 320f;

    [Tooltip("Particles in the last pop, when the body is gone.")]
    [Min(0)]
    [SerializeField] int endBurst = 120;

    ResourceNode node;           // Turki's: the deer's health (can be missing)
    EnemyCombat combat;          // Marko's: an enemy's health (can be missing)
    bool played;                 // the effect already ran (so it never runs twice)
    Vector3 localCenter;         // the middle of the body, measured from its colliders at the start
    Vector3 size = Vector3.one;  // the body's size in meters
    readonly List<Transform[]> bones = new List<Transform[]>();   // pairs: a bone and the bone it hangs from
    static bool quitting;        // the game is closing: removed creatures show nothing

    // The color this creature dies in.
    public Color DeathColor => IsHarmful ? harmfulColor : harmlessColor;
    public bool IsHarmful => kind == DeathKind.Harmful || (kind == DeathKind.Auto && GetComponent<EnemyAttack>() != null);
    public bool Played => played;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        node = GetComponent<ResourceNode>();
        combat = GetComponent<EnemyCombat>();
        MeasureBody();
    }

    // Turki's ResourceNode removes a killed deer at once: this is the last moment it's still here.
    void OnDestroy()
    {
        if (played || quitting || !gameObject.scene.isLoaded) return;
        bool killed = (node != null && node.currentHealth <= 0f) || (combat != null && combat.currentHealth <= 0f);
        if (killed) Burst();
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

    [ContextMenu("Test: Burst Here")]
    void TestBurst()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Press Play first.");
            return;
        }
        if (bones.Count == 0) MeasureBody();
        ParticleSystem ps = NewParticles();
        if (ps != null) BurstInto(ps);
    }

    // ===== My methods =====

    // The whole body bursts into particles at once (it died without a death animation).
    public void Burst()
    {
        if (played) return;
        played = true;
        ParticleSystem ps = NewParticles();
        if (ps != null) BurstInto(ps);
    }

    // The body turns into particles during its death animation. EnemyAnimator.PlayDeath calls this when it dies.
    // Returns the seconds until the body is gone, so it can be removed then.
    public float Dissolve()
    {
        float takes = dissolveStart + dissolveSeconds + 0.1f;
        if (played) return takes;
        played = true;
        StartCoroutine(Dissolving());
        return takes;
    }

    IEnumerator Dissolving()
    {
        ParticleSystem ps = NewParticles();
        if (ps == null) yield break;

        // From the body's real surface (it follows the death animation), if Unity may read the mesh; else from its bones.
        SkinnedMeshRenderer skin = BiggestSkin();
        bool fromSurface = skin != null && skin.sharedMesh != null && skin.sharedMesh.isReadable;
        if (fromSurface)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
            shape.skinnedMeshRenderer = skin;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;   // anywhere on the surface, not only its corners
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null) body.interpolation = RigidbodyInterpolation.None;   // else physics smoothing fights the shrinking below

        Renderer[] looks = GetComponentsInChildren<Renderer>();
        Vector3 startScale = transform.localScale;
        Vector3 startPosition = transform.position;
        Vector3 shrinkTo = startPosition + Vector3.up * size.y * 0.35f;   // a dead body lies low: shrink toward its lower middle
        float started = Time.time;
        float owed = 0f;   // part-particles carried to the next frame, so the rate is right at any frame rate

        while (Time.time - started < dissolveStart + dissolveSeconds)
        {
            float time = Time.time - started;
            float k = Mathf.Clamp01((time - dissolveStart) / dissolveSeconds);   // 0 = whole body, 1 = all particles

            // A trickle while it falls, then more and more.
            float rate = time < dissolveStart ? dissolveRate * 0.08f : Mathf.Lerp(dissolveRate * 0.3f, dissolveRate, k);
            owed += rate * Time.deltaTime;
            int count = (int)owed;
            owed -= count;
            Emit(ps, count, fromSurface, shrinkTo, 0.5f);

            // The body shrinks into its particles: slowly at first, quickly at the end.
            float scale = 1f - 0.85f * k * k;
            transform.localScale = startScale * scale;
            transform.position = shrinkTo + (startPosition - shrinkTo) * scale;
            yield return null;
        }

        Emit(ps, endBurst, fromSurface, shrinkTo, 1f);   // the last pop
        foreach (Renderer look in looks)
        {
            if (look != null) look.enabled = false;
        }
        RemoveLater(ps);
    }

    // A copy of the particles at the body, in the death color. Not a child: it stays when the body is removed.
    ParticleSystem NewParticles()
    {
        if (particles == null)
        {
            Debug.LogWarning(name + ": DeathEffect has no Particles. Run Tools > Effects > Build Death Particles.");
            return null;
        }
        ParticleSystem ps = Instantiate(particles, BodyCenter(), Quaternion.identity);
        ParticleSystem.MainModule main = ps.main;
        Color color = DeathColor;
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.45f));   // a mix of deep and light
        ps.Play();   // its own emission is off: only Emit makes particles
        return ps;
    }

    void BurstInto(ParticleSystem ps)
    {
        Emit(ps, burstCount, false, BodyCenter(), 1f);
        RemoveLater(ps);
    }

    // count particles: from the shape (the body's surface) or at random points along the body's bones, flying outward.
    void Emit(ParticleSystem ps, int count, bool fromSurface, Vector3 middle, float speedScale)
    {
        if (count <= 0) return;
        if (fromSurface)
        {
            ps.Emit(count);   // the shape module puts them on the surface, moving out of it
            return;
        }
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 point = RandomBodyPoint();
            Vector3 outward = point - middle;
            outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Random.onUnitSphere;
            emit.position = point;
            emit.velocity = (outward * Random.Range(0.3f, 1f) + Vector3.up * 0.35f) * burstSpeed * speedScale;
            ps.Emit(emit, 1);
        }
    }

    // A random point inside the body: somewhere along a bone, a little to the side. No bones: inside its collider box.
    Vector3 RandomBodyPoint()
    {
        for (int tries = 0; tries < 4 && bones.Count > 0; tries++)
        {
            Transform[] pair = bones[Random.Range(0, bones.Count)];
            if (pair[0] == null || pair[1] == null) continue;
            return Vector3.Lerp(pair[0].position, pair[1].position, Random.value) + Random.insideUnitSphere * size.y * 0.12f;
        }
        Vector3 offset = new Vector3((Random.value - 0.5f) * size.x, (Random.value - 0.5f) * size.y, (Random.value - 0.5f) * size.z);
        return BodyCenter() + transform.rotation * offset;
    }

    // The particle copy removes itself once its last particle has faded.
    void RemoveLater(ParticleSystem ps)
    {
        Destroy(ps.gameObject, ps.main.startLifetime.constantMax + 0.5f);
    }

    Vector3 BodyCenter()
    {
        return transform.TransformPoint(localCenter);
    }

    // The body's middle and size from its colliders (they're switched off once it dies), and its bones.
    void MeasureBody()
    {
        bool found = false;
        Bounds box = new Bounds(transform.position, Vector3.zero);
        foreach (Collider part in GetComponentsInChildren<Collider>())
        {
            if (!part.enabled || part.isTrigger) continue;
            if (found) box.Encapsulate(part.bounds);
            else box = part.bounds;
            found = true;
        }
        if (!found) box = new Bounds(transform.position + Vector3.up * 0.5f, Vector3.one);
        localCenter = transform.InverseTransformPoint(box.center);
        size = box.size;

        bones.Clear();
        SkinnedMeshRenderer skin = BiggestSkin();
        if (skin == null) return;
        HashSet<Transform> used = new HashSet<Transform>(skin.bones);
        foreach (Transform bone in skin.bones)
        {
            if (bone != null && bone.parent != null && used.Contains(bone.parent)) bones.Add(new[] { bone.parent, bone });
        }
    }

    // The creature's main skinned mesh (the one with the most corners).
    SkinnedMeshRenderer BiggestSkin()
    {
        SkinnedMeshRenderer best = null;
        foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (skin.sharedMesh == null) continue;
            if (best == null || skin.sharedMesh.vertexCount > best.sharedMesh.vertexCount) best = skin;
        }
        return best;
    }
}
