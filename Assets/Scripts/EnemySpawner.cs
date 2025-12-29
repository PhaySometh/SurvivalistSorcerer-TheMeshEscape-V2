using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] weakPrefabs;   // Difficulty 0
    public GameObject[] mediumPrefabs; // Difficulty 1
    public GameObject[] strongPrefabs; // Difficulty 2

    [Header("Settings")]
    public Transform playerTransform;
    public float spawnRadius = 20f;
    public float minSpawnDistance = 10f;

    [Header("Coin Drops")]
    public GameObject coinPrefab;
    [Tooltip("Number of coins to drop when enemy dies")]
    public int coinsPerEnemy = 3;
    [Tooltip("Radius around enemy to scatter coins")]
    public float coinDropRadius = 2f;
    
    [Header("Tracking")]
    public List<GameObject> activeEnemies = new List<GameObject>();
    public int ActiveEnemyCount => activeEnemies.Count;

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    public void SpawnEnemies(int count, int difficultyIndex = 0, int subIndex = -1)
    {
        if (playerTransform == null)
        {
            Debug.LogError("EnemySpawner: Player missing!");
            return;
        }

        GameObject prefabToSpawn = GetPrefabByDifficulty(difficultyIndex, subIndex);
        if (prefabToSpawn == null) return;

        StartCoroutine(SpawnRoutine(count, prefabToSpawn));
    }

    /// <summary>
    /// Spawns mixed enemies. subIndexCaps allows limiting which prefabs in the arrays are used (e.g. only use first 2 weak ones).
    /// </summary>
    public void SpawnMixedEnemies(int totalCount, float[] weights, float duration = 0f, int[] subIndexCaps = null)
    {
        if (playerTransform == null) return;
        
        bool hasAnyPrefabs = (weakPrefabs != null && weakPrefabs.Length > 0) || 
                             (mediumPrefabs != null && mediumPrefabs.Length > 0) || 
                             (strongPrefabs != null && strongPrefabs.Length > 0);
                             
        if (!hasAnyPrefabs) 
        {
            Debug.LogError("EnemySpawner: No prefabs assigned in any category!");
            return;
        }

        StartCoroutine(SpawnMixedRoutine(totalCount, weights, duration, subIndexCaps));
    }

    IEnumerator SpawnMixedRoutine(int totalCount, float[] weights, float duration, int[] subIndexCaps)
    {
        float interval = duration > 0 ? duration / totalCount : 0.1f;

        for (int i = 0; i < totalCount; i++)
        {
            float randomValue = Random.value;
            float cumulative = 0f;
            int selectedIndex = 0;

            for (int j = 0; j < weights.Length; j++)
            {
                cumulative += weights[j];
                if (randomValue <= cumulative)
                {
                    selectedIndex = j;
                    break;
                }
            }
            
            // Safety check for index
            selectedIndex = Mathf.Clamp(selectedIndex, 0, 2);
            
            int cap = -1;
            if (subIndexCaps != null && selectedIndex < subIndexCaps.Length) cap = subIndexCaps[selectedIndex];

            GameObject prefab = GetPrefabByDifficulty(selectedIndex, cap);
            
            if (prefab != null)
            {
                SpawnOneEnemy(prefab);
            }

            if (duration > 0)
            {
                yield return new WaitForSeconds(interval);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private GameObject GetPrefabByDifficulty(int index, int forcedSubIndex = -1)
    {
        GameObject[] targetArray;
        
        switch (index)
        {
            case 0: targetArray = weakPrefabs; break;
            case 1: targetArray = mediumPrefabs; break;
            case 2: targetArray = strongPrefabs; break;
            default: targetArray = weakPrefabs; break;
        }

        if (targetArray == null || targetArray.Length == 0)
        {
            // Fallback: try other arrays if one is empty
            if (weakPrefabs != null && weakPrefabs.Length > 0) targetArray = weakPrefabs;
            else if (mediumPrefabs != null && mediumPrefabs.Length > 0) targetArray = mediumPrefabs;
            else if (strongPrefabs != null && strongPrefabs.Length > 0) targetArray = strongPrefabs;
            else
            {
                Debug.LogError("EnemySpawner: All prefab arrays are empty!");
                return null;
            }
        }

        // If a specific subIndex is requested, use it (clamped to array size)
        if (forcedSubIndex >= 0)
        {
            int subIndex = Mathf.Clamp(forcedSubIndex, 0, targetArray.Length - 1);
            return targetArray[subIndex];
        }

        return targetArray[Random.Range(0, targetArray.Length)];
    }

    IEnumerator SpawnRoutine(int count, GameObject prefab)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOneEnemy(prefab);
            yield return new WaitForSeconds(0.2f); // Stagger spawns slightly
        }
    }

    void SpawnOneEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GetRandomNavMeshPosition();
        if (spawnPos != Vector3.zero)
        {
            GameObject newEnemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // ✨ AUTO-FIX: Ensure enemy has collision detector for damage
            EnsureEnemyHasCollisionDetector(newEnemy);
            
            // Track enemy death to update count and drop coins
            HealthSystem health = newEnemy.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.OnDeath.AddListener(() => OnEnemyDeath(newEnemy));
            }
            
            activeEnemies.Add(newEnemy);
        }
    }
    
    /// <summary>
    /// Ensures enemy has proper collision detection setup for dealing damage to player
    /// This fixes slime, golem, and bull boss not dealing damage
    /// </summary>
    void EnsureEnemyHasCollisionDetector(GameObject enemy)
    {
        // Check if enemy already has collision detector
        EnemyCollisionDetector detector = enemy.GetComponentInChildren<EnemyCollisionDetector>();
        
        if (detector == null)
        {
            // No detector found - we need to add one
            // Strategy: Look for existing colliders and add detector to best location
            
            // First, try to find a child with a trigger collider
            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            GameObject targetObject = null;
            
            foreach (Collider col in colliders)
            {
                if (col.isTrigger)
                {
                    targetObject = col.gameObject;
                    break;
                }
            }
            
            // If no trigger collider found, use the main enemy object
            if (targetObject == null)
            {
                targetObject = enemy;
            }
            
            // Add EnemyCollisionDetector component
            detector = targetObject.AddComponent<EnemyCollisionDetector>();
            
            // Configure it with good default values
            detector.damageAmount = 25f;
            detector.damageCooldown = 1f;
            detector.useProximityDetection = false; // Rely on colliders
            detector.proximityRange = 0.5f;
            detector.maxDamageDistance = 5.0f;
            detector.showDebugLogs = true; // Enable for debugging
            
            Debug.Log($"✅ Auto-added EnemyCollisionDetector to {enemy.name} on {targetObject.name}");
        }
        else
        {
            // Detector exists - make sure it has good settings
            detector.maxDamageDistance = Mathf.Max(detector.maxDamageDistance, 5.0f);
            detector.showDebugLogs = true;
        }
        
        // Also ensure enemy has at least one collider
        Collider mainCollider = enemy.GetComponent<Collider>();
        if (mainCollider == null)
        {
            // Add a capsule collider as default
            CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            capsule.center = new Vector3(0, 1f, 0);
            Debug.Log($"⚠️ Added default CapsuleCollider to {enemy.name}");
        }
    }

    void OnEnemyDeath(GameObject enemy)
    {
        // Remove from active list
        RemoveEnemy(enemy);
        
        // Drop coins at enemy position
        DropCoins(enemy.transform.position);
    }
    
    void DropCoins(Vector3 position)
    {
        if (coinPrefab == null) return;
        
        for (int i = 0; i < coinsPerEnemy; i++)
        {
            // Scatter coins in a circle around the enemy
            Vector2 randomCircle = Random.insideUnitCircle * coinDropRadius;
            Vector3 coinPos = position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // CRITICAL FIX: Raycast down to find actual ground level
            RaycastHit hit;
            if (Physics.Raycast(coinPos + Vector3.up * 5f, Vector3.down, out hit, 10f, LayerMask.GetMask("Ground", "Default", "Terrain")))
            {
                // Place coin on ground with slight offset
                coinPos = hit.point + Vector3.up * 0.3f;
            }
            else
            {
                // Fallback: use enemy's Y position
                coinPos.y = position.y + 0.3f;
            }
            
            Instantiate(coinPrefab, coinPos, Quaternion.identity);
        }
    }

    void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    Vector3 GetRandomNavMeshPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        for (int attempts = 0; attempts < 10; attempts++)
        {
            // Random point between minSpawnDistance and spawnRadius
            float distance = Random.Range(minSpawnDistance, spawnRadius);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * distance;
            Vector3 randomPoint = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
            {
                // ADDITION: Check if hit point is inside an obstacle or building
                // We assume buildings have MeshColliders or BoxColliders
                // Check if there are any colliders nearby that are NOT the floor
                Collider[] colliders = Physics.OverlapSphere(hit.position, 1.5f);
                bool occupied = false;
                foreach (var col in colliders)
                {
                    // If it's not the enemy itself (if we had some) and not the ground (assuming ground is tagged or named)
                    // Safety check: only avoid spawning if the name contains common obstacle keywords
                    if (col.gameObject.name.Contains("Building") || 
                        col.gameObject.name.Contains("Wall") || 
                        col.gameObject.name.Contains("House") ||
                        col.gameObject.name.Contains("Tree"))
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    // CRITICAL FIX: Verify ground level with raycast to prevent underground spawning
                    Vector3 spawnPos = hit.position;
                    RaycastHit groundHit;
                    
                    // Cast ray from above to find actual ground
                    if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out groundHit, 20f, LayerMask.GetMask("Ground", "Default", "Terrain")))
                    {
                        // Use the ground hit position + small offset to ensure enemy spawns ON ground
                        spawnPos = groundHit.point + Vector3.up * 0.5f;
                    }
                    else
                    {
                        // If raycast fails, add offset to NavMesh position
                        spawnPos.y += 0.5f;
                    }
                    
                    return spawnPos;
                }
            }
        }
        
        return Vector3.zero;
    }
}
