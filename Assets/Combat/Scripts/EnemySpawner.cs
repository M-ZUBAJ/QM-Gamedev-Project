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
    [Tooltip("[Tony] How many enemies spawn each night.")]
    public int enemiesPerNight = 3;                                           // [Tony] added

    void OnEnable()                                                           // [Tony] added
    {                                                                         // [Tony] added
        DaylightCycle.NightStarted += OnNightStarted;                         // [Tony] added
        DaylightCycle.DayStarted += OnDayStarted;                             // [Tony] added
    }                                                                         // [Tony] added

    void OnDisable()                                                          // [Tony] added
    {                                                                         // [Tony] added
        DaylightCycle.NightStarted -= OnNightStarted;                         // [Tony] added
        DaylightCycle.DayStarted -= OnDayStarted;                             // [Tony] added
    }                                                                         // [Tony] added

    // [Tony] added: night started (day = the day number), spawn this night's enemies.
    void OnNightStarted(int day)                                              // [Tony] added
    {                                                                         // [Tony] added
        if (!spawnAtNight) return;                                            // [Tony] added
        for (int i = 0; i < enemiesPerNight; i++) SpawnEnemy();               // [Tony] added
    }                                                                         // [Tony] added

    // [Tony] added: morning, remove the night's enemies (only the ones this spawner made).
    void OnDayStarted(int day)                                                // [Tony] added
    {                                                                         // [Tony] added
        if (spawnAtNight) DespawnEnemies();                                   // [Tony] added
    }                                                                         // [Tony] added

    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab has not been assigned!");
            return;
        }

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
            spawnPosition.y = transform.position.y;
        }

        GameObject enemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

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

    public void DespawnEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
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