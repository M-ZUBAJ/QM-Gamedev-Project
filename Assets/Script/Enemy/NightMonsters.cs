/*
First Date: 21/11/2026
Date: 21/11/2026
Author: Tony
Use by: The object with Marko's EnemySpawner (in both game scenes). Tools > Enemies > Set Up Night Monsters puts it there
        with the turtle.
Unity Editor: Monsters = the night monsters after the first night's (the EnemySpawner's Enemy Prefab, the slime, comes
              every night): each with its First Night, its Share of the night's spawns, Max Alive, and its own Health and
              Damage (0 = the spawner's numbers). A monster for night 3: click + under Monsters, drag its enemy prefab in,
              First Night 3.
              Where they appear: Min / Max Distance from the player, and Out Of Sight.
              Testing: Play, select the sun (DaylightCycle): Day = 2, then drag Time Of Day past 0.77 (night). About a
              third of the monsters are turtles. Turn around quickly: none pops up right in front of you.
Purpose: Which monsters come each night, how strong they are, and where they appear. Marko's EnemySpawner asks it for
         every spawn:
           - Pick: night 1 = only the spawner's Enemy Prefab (the slime); from a monster's First Night on, that share of
             the spawns is that monster (the turtle from night 2: 30 %, 5 % more each night, at most Max Alive at once,
             one more each night). So every night can bring a new monster, and the ones before keep coming;
           - Prepare: the picked monster's own Health and Damage (growing each night after its first); the slime keeps
             the spawner's numbers;
           - FindSpot: a spot Min - Max Distance away that the player can't see (PlayerSight: behind them, or behind a
             hill or a tree), inside the world barrier, so no monster appears right in front of the player when they
             turn around (before, they appeared 10 m away in any direction). If every try is in sight, the one most
             behind the camera.
Requirement: NightMonsters.cs o-> PlayerSight.cs, WorldBarrier.cs, EnemyCombat.cs, EnemyAttack.cs
             NightMonsters.cs <-x EnemySpawner.cs, NightMonsterBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NightMonsters : MonoBehaviour
{
    // One night monster after the first night's: from which night it comes, how many, and how strong.
    [System.Serializable]
    public class Monster
    {
        [Tooltip("Its name, for you and the Console (e.g. Turtle).")]
        public string name = "Turtle";

        [Tooltip("The enemy prefab (an enemy like Tools > Enemies > Build Monster Enemy makes, e.g. TurtleShellEnemy).")]
        public GameObject prefab;

        [Tooltip("The first night it comes (1 = the first night). From then on it comes every night, with the ones before it.")]
        [Min(1)]
        public int firstNight = 2;

        [Tooltip("How much of the night's spawns are this monster on its first night (0.3 = 3 in 10).")]
        [Range(0f, 1f)]
        public float share = 0.3f;

        [Tooltip("Added to Share for every night after its first (0.05 = 5 % more each night).")]
        [Range(0f, 0.5f)]
        public float sharePerNight = 0.05f;

        [Tooltip("The most of it alive at once on its first night (more spawns are the first night's monster then).")]
        [Min(0)]
        public int maxAlive = 3;

        [Tooltip("Added to Max Alive for every night after its first.")]
        [Min(0)]
        public int maxAlivePerNight = 1;

        [Tooltip("Its health on its first night. 0 = the spawner's numbers (50 + 10 for every day passed).")]
        [Min(0f)]
        public float health = 90f;

        [Tooltip("More health for every night after its first.")]
        [Min(0f)]
        public float healthPerNight = 15f;

        [Tooltip("Its attack damage on its first night. 0 = the spawner's numbers (10 + 2 for every day passed).")]
        [Min(0f)]
        public float damage = 12f;

        [Tooltip("More damage for every night after its first.")]
        [Min(0f)]
        public float damagePerNight = 2f;

        [System.NonSerialized] List<GameObject> alive;   // the ones out now (for Max Alive)

        public List<GameObject> Alive => alive ?? (alive = new List<GameObject>());

        // Nights after its first (0 on its first night).
        public int NightsAfterFirst(int night) => Mathf.Max(0, night - firstNight);

        public float ShareOn(int night) => Mathf.Clamp01(share + sharePerNight * NightsAfterFirst(night));

        public int MaxAliveOn(int night) => maxAlive + maxAlivePerNight * NightsAfterFirst(night);

        public int AliveCount()
        {
            Alive.RemoveAll(enemy => enemy == null);   // killed, or burnt away at dawn
            return Alive.Count;
        }
    }

    // ===== Fields =====
    [Header("Monsters after the first night's")]
    [Tooltip("The EnemySpawner's Enemy Prefab (the slime) comes every night. These join it from their First Night on " +
             "(the turtle from night 2). Add the next night's monster here.")]
    [SerializeField] List<Monster> monsters = new List<Monster>();

    [Header("Where they appear")]
    [Tooltip("The nearest a monster appears to the player, in meters (more than PlayerSight's Notice Radius, 20: closer " +
             "always counts as seen).")]
    [Range(5f, 80f)]
    [SerializeField] float minDistance = 24f;

    [Tooltip("The farthest a monster appears from the player, in meters.")]
    [Range(10f, 120f)]
    [SerializeField] float maxDistance = 34f;

    [Tooltip("On: only spots the player can't see (behind them, or behind a hill or a tree).")]
    [SerializeField] bool outOfSight = true;

    [Tooltip("Spots tried for each monster before taking the one most behind the player.")]
    [Range(1, 40)]
    [SerializeField] int tries = 16;

    const float SpotHeight = 1.5f;   // how tall a monster is taken to be, for "could the player see it there"

    Monster picked;                  // what Pick chose, for Prepare right after

    public IReadOnlyList<Monster> Monsters => monsters;

    // ===== My methods =====

    // Marko's EnemySpawner asks this for every spawn: which prefab to make. "first" = its Enemy Prefab (the first
    // night's monster, which comes every night). A monster whose First Night has come gets its Share of the spawns
    // while fewer than its Max Alive are out; otherwise it's "first".
    public GameObject Pick(GameObject first, int night)
    {
        picked = null;
        float roll = Random.value;
        foreach (Monster monster in monsters)
        {
            if (monster == null || monster.prefab == null || night < monster.firstNight) continue;
            if (monster.AliveCount() >= monster.MaxAliveOn(night)) continue;
            float share = monster.ShareOn(night);
            if (roll < share)
            {
                picked = monster;
                return monster.prefab;
            }
            roll -= share;
        }
        return first;
    }

    // Right after the spawn (Marko's EnemySpawner calls it): the picked monster's own health and damage for tonight,
    // and it's counted for Max Alive. The first night's monster keeps the spawner's numbers.
    public void Prepare(GameObject enemy, int night)
    {
        Monster monster = picked;
        picked = null;
        if (monster == null || enemy == null) return;

        monster.Alive.Add(enemy);
        int after = monster.NightsAfterFirst(night);
        EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
        if (combat != null && monster.health > 0f)
        {
            combat.maxHealth = monster.health + monster.healthPerNight * after;
            combat.currentHealth = combat.maxHealth;
        }
        EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
        if (attack != null && monster.damage > 0f) attack.attackDamage = monster.damage + monster.damagePerNight * after;
    }

    // A spot for a monster: Min - Max Distance from the player, on the ground, inside the world barrier, and (Out Of
    // Sight) where the player can't see it. If every try is in sight (open flat land all round), the one most behind
    // the camera. False = no ground found at all (Marko's own spot is used then).
    public bool FindSpot(Transform player, out Vector3 spot)
    {
        spot = Vector3.zero;
        if (player == null) return false;

        float near = Mathf.Min(minDistance, maxDistance);
        float far = Mathf.Max(minDistance, maxDistance);
        Camera cam = Camera.main;
        bool found = false;
        float mostBehind = float.MinValue;
        for (int i = 0; i < tries; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 at = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(near, far);
            if (!WorldBarrier.Inside(at, 2f) || !Ground(ref at)) continue;
            if (!outOfSight || !Seen(at, cam))
            {
                spot = at;
                return true;
            }

            float behind = cam != null ? -Vector3.Dot((at - cam.transform.position).normalized, cam.transform.forward) : 0f;
            if (behind > mostBehind)
            {
                mostBehind = behind;
                spot = at;
                found = true;
            }
        }
        return found;
    }

    // Could the player see a monster standing here? PlayerSight decides (it knows the view, the hills and the trees);
    // without one, anything in front of the camera counts as seen.
    bool Seen(Vector3 feet, Camera cam)
    {
        PlayerSight sight = PlayerSight.Current;
        if (sight != null) return sight.CanSee(feet, SpotHeight);
        if (cam == null) return false;
        Vector3 view = cam.WorldToViewportPoint(feet + Vector3.up * (SpotHeight * 0.5f));
        return view.z > 0f && view.x > -0.1f && view.x < 1.1f && view.y > -0.1f && view.y < 1.1f;
    }

    // The ground under a spot (a ray from high above): "at" gets its height. False = no ground there.
    static bool Ground(ref Vector3 at)
    {
        Vector3 from = new Vector3(at.x, at.y + 500f, at.z);
        if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 1000f, Physics.DefaultRaycastLayers,
                             QueryTriggerInteraction.Ignore)) return false;
        at.y = hit.point.y;
        return true;
    }
}
