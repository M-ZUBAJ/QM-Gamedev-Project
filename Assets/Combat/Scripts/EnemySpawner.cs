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

        // Connect the enemy to the player.
        // Uncomment this when the player's Transform is available.
        // enemy.GetComponent<EnemyMovement>().target = player;
        // enemy.GetComponent<EnemyAttack>().target = player;

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