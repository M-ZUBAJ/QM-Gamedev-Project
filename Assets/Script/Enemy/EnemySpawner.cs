using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// important : this script only tracks enemies created by this script. Enemies manually placed
/// will not be affected by this script
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // what will be spawned
    public Transform player;

    public float spawnDistance = 10f; // how far away from the player enemy will spawn

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // [Tony] added (18/10/2026): spawn enemies when night starts and remove them when day starts,
    // using Tony's DaylightCycle events. Nothing of Marko's code was removed or changed.
    // To undo: delete every line marked [Tony] in this file.
    [Tooltip("[Tony] On: enemies spawn when night starts (DaylightCycle.NightStarted) and are removed when day starts.")]
    public bool spawnAtNight = true;                                          // [Tony] added


    // total number of enemies allowed to exist in game at any one time.
    public int maxEnemyNumber = 40;
    private int enemyCounter = 0;

    [Header("Spawn Interval By Night (Seconds)")]

    [Min(0.1f)]
    public float night1SpawnInterval = 10f;

    [Min(0.1f)]
    public float night2SpawnInterval = 7f;

    [Min(0.1f)]
    public float night3SpawnInterval = 5f;

    [Min(0.1f)]
    public float night4SpawnInterval = 5f;

    [Min(0.1f)]
    public float night5SpawnInterval = 5f;


    private Coroutine nightSpawnRoutine;

    // [Tony] added

    void OnEnable()                                                           // [Tony] added
    {                                                                         // [Tony] added
        DaylightCycle.NightStarted += OnNightStarted;                         // [Tony] added
        DaylightCycle.DayStarted += OnDayStarted;                             // [Tony] added
    }                                                                         // [Tony] added

    void OnDisable()                                                          // [Tony] added
    {                                                                         // [Tony] added
        DaylightCycle.NightStarted -= OnNightStarted;                         // [Tony] added
        DaylightCycle.DayStarted -= OnDayStarted;

        StopNightSpawning();                           // [Tony] added
    }                                                                         // [Tony] added

    // [Tony] added: night started (day = the day number), spawn this night's enemies.
    void OnNightStarted(int day)                                              // [Tony] added
    {                                                                         // [Tony] added
        if (!spawnAtNight) return;                                            // [Tony] added

        StopNightSpawning();

        float interval = GetSpawnInterval(day);

        if (interval <= 0f)
        {
            Debug.Log("No enemy spawning configured for night " + day);
            return;
        }

        Debug.Log("Night " + day + " started. Enemies will spawn every " + interval + " seconds.");

        nightSpawnRoutine = StartCoroutine(SpawnThroughoutNight(interval));



    }                                                                         // [Tony] added



    // [Tony] added: morning, remove the night's enemies (only the ones this spawner made).
    void OnDayStarted(int day)
    {
        StopNightSpawning();

        DespawnEnemies();

        Debug.Log("Day " + day + " started. All spawned enemies have been removed.");
    }                                                                        // [Tony] added




    // returns the configured enemy spawn interval for each day 
    float GetSpawnInterval(int night)
    {
        switch (night)
        {
            case 1: return night1SpawnInterval;
            case 2: return night2SpawnInterval;
            case 3: return night3SpawnInterval;
            case 4: return night4SpawnInterval;
            case 5: return night5SpawnInterval;

            default: return 0f;
        }
    }



    IEnumerator SpawnThroughoutNight(float interval)
    {
        while (DaylightCycle.Current != null && DaylightCycle.Current.IsNight)
        {
            if (enemyCounter < maxEnemyNumber)
            {
                SpawnEnemy();

            }

            yield return new WaitForSeconds(interval);
        }

        nightSpawnRoutine = null;
    }



    // disables spawning at night
    void StopNightSpawning()
    {
        if (nightSpawnRoutine != null)
        {
            StopCoroutine(nightSpawnRoutine);
            nightSpawnRoutine = null;
        }
    }





    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab has not been assigned!");
            return;
        }

        // [Tony] added (21/11/2026): Tony's NightMonsters (on this object) decides more about each spawn: which monster
        // (from night 2 some are turtles; every night can bring a new one, and the ones before keep coming), its
        // strength, and where it appears (farther away and out of the player's sight, so nothing pops up right in
        // front of them when they turn around). Without NightMonsters everything works exactly as before.
        // To undo: delete the lines marked [Tony] (21/11/2026) and put back enemyPrefab in Instantiate.
        // Log: Docs/TonyEditMate/Marko/21-11-2026 Night Monsters And The Turtle Shell.md
        NightMonsters nightMonsters = GetComponent<NightMonsters>();                                       // [Tony] added
        int night = DaylightCycle.Current != null ? DaylightCycle.Current.Day : 1;                        // [Tony] added

        Vector3 spawnPosition = transform.position;

        // if the player is assigned, enemy appears in a random direction around them. If the
        // player isn't assigned, (like as of the point i'm writing this message) it will 
        // instead spawn at the spawners positon (another game object in the hierachy)

        if (player != null)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0f;
            randomDirection.Normalize();

            spawnPosition = player.position + randomDirection * spawnDistance;
            // [Tony] added (20/11/2026): near the edge of the world, a spot past Tony's barrier (WorldBarrier) is moved to
            // the player's other side, so no enemy appears outside the world (stuck behind the barrier, or falling where
            // there's no ground). Log: Docs/TonyEditMate/Marko/20-11-2026 Night Enemies Burn Away At Day.md
            if (!WorldBarrier.Inside(spawnPosition, 2f)) spawnPosition = player.position - randomDirection * spawnDistance;   // [Tony] added
            if (nightMonsters != null && nightMonsters.FindSpot(player, out Vector3 hidden)) spawnPosition = hidden;   // [Tony] added (21/11/2026): out of the player's sight
            //spawnPosition.y = transform.position.y;
            // [Tony] replaced (10/11/2026): spawnPosition.y = 0.5f;
            // A fixed 0.5 put enemies inside hills or in the air over dips. Now they appear on the ground that's there.
            // Log: Docs/TonyEditMate/Marko/10-11-2026 Slime Enemy With Animations.md
            spawnPosition.y = GroundHeight(spawnPosition);                            // [Tony] replaced
        }

        GameObject enemy = Instantiate(
            // [Tony] replaced (21/11/2026): enemyPrefab,
            nightMonsters != null ? nightMonsters.Pick(enemyPrefab, night) : enemyPrefab,   // [Tony] replaced: tonight's monster
            spawnPosition,
            Quaternion.identity
        );

        // Scale enemy strength according to the current day.
        int currentDay = DaylightCycle.Current != null
            ? DaylightCycle.Current.Day
            : 1;

        int daysPassed = Mathf.Max(0, currentDay - 1);

        EnemyCombat enemyCombat = enemy.GetComponent<EnemyCombat>();
        if (enemyCombat != null)
        {
            enemyCombat.maxHealth = 50f + (daysPassed * 10f);
            enemyCombat.currentHealth = enemyCombat.maxHealth;
        }

        EnemyAttack enemyAttack = enemy.GetComponent<EnemyAttack>();
        if (enemyAttack != null)
        {
            enemyAttack.attackDamage = 10f + (daysPassed * 2f);
        }

        if (nightMonsters != null) nightMonsters.Prepare(enemy, night);   // [Tony] added (21/11/2026): a picked monster's own health / damage (the turtle)


        // Connect the spawned enemy to the player.
        if (player != null)
        {
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.target = player;
            }

            EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
            if (attack != null)
            {
                attack.target = player;
            }
        }
        else
        {
            Debug.LogWarning("EnemySpawner has no Player assigned!");
        }

        spawnedEnemies.Add(enemy);
    }

    // [Tony] added (10/11/2026): the height to spawn at = the ground found by a ray from high above (terrain, rock,
    // floor...) plus half a meter. The old ball enemy (its middle is 0.5 above its bottom) then stands right on the
    // ground, and a monster with physics (its pivot is at its feet) drops the last bit onto it. Nothing found = 0.5.
    float GroundHeight(Vector3 spot)                                                  // [Tony] added
    {                                                                                 // [Tony] added
        Vector3 from = new Vector3(spot.x, spot.y + 500f, spot.z);                    // [Tony] added
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 1000f,            // [Tony] added
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))   // [Tony] added
        {                                                                             // [Tony] added
            return hit.point.y + 0.5f;                                                // [Tony] added
        }                                                                             // [Tony] added
        return 0.5f;                                                                  // [Tony] added
    }                                                                                 // [Tony] added

    public void DespawnEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                // [Tony] replaced (20/11/2026): Destroy(enemy);
                // The night's enemies now burn away into orange embers when day comes (Tony's EnemyAnimator.Vanish),
                // instead of blinking out. An enemy without Tony's EnemyAnimator is still removed at once.
                // Log: Docs/TonyEditMate/Marko/20-11-2026 Night Enemies Burn Away At Day.md
                EnemyAnimator.Vanish(enemy);                                                  // [Tony] replaced
            }
        }

        spawnedEnemies.Clear();
    }


    // creates temporary Testing buttons in unitys built in 
    // context menu. These buttons can be pressed for testing.

    [ContextMenu("Test Spawn Enemy")]
    private void TestSpawnEnemy()
    {
        SpawnEnemy();
    }

    [ContextMenu("Test Despawn Enemies")]
    private void TestDespawnEnemies()
    {
        DespawnEnemies();
    }
}