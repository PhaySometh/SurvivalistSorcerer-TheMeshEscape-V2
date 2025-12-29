using UnityEngine;

/// <summary>
/// Checks victory conditions without modifying wave logic
/// Victory = All waves complete AND all enemies defeated (+ optional coin requirement)
/// Attach this to WaveManager or GameManager
/// </summary>
public class VictoryConditionChecker : MonoBehaviour
{
    [Header("Victory Conditions")]
    [Tooltip("Win when all enemies are cleared (simplified victory)")]
    public bool simpleVictoryMode = true;
    
    [Tooltip("Time to survive in the final wave (in seconds) - Only used if simpleVictoryMode is false")]
    public float surviveTime = 120f; // 2 minutes
    
    [Tooltip("Number of coins needed to win - Only used if simpleVictoryMode is false")]
    public int requiredCoins = 0; // Set to 0 to disable coin requirement
    
    [Tooltip("Win immediately when all enemies are cleared (ignores time/coin requirements)")]
    public bool instantWinOnClear = true;
    
    [Header("References (Auto-found if not assigned)")]
    public WaveManager waveManager;
    public PlayerStats playerStats;
    public EnemySpawner enemySpawner;
    
    [Header("Debug")]
    [SerializeField] private bool isFinalWave = false;
    [SerializeField] private bool allWavesComplete = false;
    [SerializeField] private float finalWaveTimer = 0f;
    [SerializeField] private bool victoryTriggered = false;
    [SerializeField] private bool gameHasStarted = false;
    [SerializeField] private bool enemiesHaveSpawned = false;
    [SerializeField] private float gameStartTime = 0f;

    void Start()
    {
        // Auto-find references
        if (waveManager == null)
            waveManager = FindObjectOfType<WaveManager>();
            
        if (enemySpawner == null)
            enemySpawner = FindObjectOfType<EnemySpawner>();
            
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerStats = player.GetComponent<PlayerStats>();
        }
        
        if (waveManager == null)
        {
            Debug.LogError("VictoryConditionChecker: WaveManager not found!");
            enabled = false;
        }
        
        if (enemySpawner == null)
        {
            Debug.LogError("VictoryConditionChecker: EnemySpawner not found!");
        }
        
        // Initialize flags
        gameHasStarted = false;
        enemiesHaveSpawned = false;
        gameStartTime = 0f;
    }

    void Update()
    {
        if (victoryTriggered) return;
        
        // Track when game actually starts (not in WaitingToStart state)
        if (!gameHasStarted && waveManager != null)
        {
            if (waveManager.currentState != WaveManager.WaveState.WaitingToStart)
            {
                gameHasStarted = true;
                gameStartTime = Time.time;
                Debug.Log("✅ Game has started! Victory checking will begin once enemies spawn.");
            }
        }
        
        // Don't check victory until game has started
        if (!gameHasStarted) return;
        
        // Track when enemies have spawned (at least once)
        if (!enemiesHaveSpawned && enemySpawner != null)
        {
            if (enemySpawner.ActiveEnemyCount > 0)
            {
                enemiesHaveSpawned = true;
                Debug.Log($"✅ Enemies have spawned! Victory checking is now active. Current enemies: {enemySpawner.ActiveEnemyCount}");
            }
            else
            {
                // Also check if enough time has passed (15+ seconds means intro is over)
                if (Time.time - gameStartTime > 15f)
                {
                    enemiesHaveSpawned = true;
                    Debug.Log("✅ Intro period over. Victory checking is now active.");
                }
            }
        }
        
        // Don't check victory until enemies have spawned at least once
        if (!enemiesHaveSpawned) return;
        
        // Simple victory mode: Just check if all enemies are dead
        if (simpleVictoryMode)
        {
            CheckSimpleVictoryCondition();
            return;
        }
        
        // Original complex victory system
        // Check if we're in the final wave
        CheckIfFinalWave();
        
        // Check if all waves are complete
        CheckIfAllWavesComplete();
        
        // If all waves complete, check for instant win condition
        if (allWavesComplete && instantWinOnClear)
        {
            CheckInstantWinCondition();
        }
        
        // If in final wave, count survival time
        if (isFinalWave)
        {
            finalWaveTimer += Time.deltaTime;
            
            // Check victory conditions (time + coins)
            CheckVictoryConditions();
        }
    }
    
    /// <summary>
    /// Simple victory check: All waves complete AND all enemies dead = Win
    /// </summary>
    void CheckSimpleVictoryCondition()
    {
        // First, check if all waves are complete
        if (waveManager != null)
        {
            // Check if we've completed all required waves
            // Get wave indices using reflection
            int startWaveIndex = (int)(waveManager.GetType().GetField("startWaveIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(waveManager) ?? 1);
            int endWaveIndex = (int)(waveManager.GetType().GetField("endWaveIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(waveManager) ?? 5);
            
            int actualWaveIndex = waveManager.currentWave + startWaveIndex;
            
            // If we haven't completed all waves yet, don't check for victory
            if (actualWaveIndex <= endWaveIndex)
            {
                // Still have waves to go
                return;
            }
        }
        
        // All waves are complete, now check if all enemies are defeated
        if (enemySpawner != null && enemySpawner.ActiveEnemyCount == 0)
        {
            // Check if there are ANY enemies in the scene at all
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length == 0)
            {
                TriggerVictory();
            }
        }
    }
    
    void CheckIfAllWavesComplete()
    {
        // Check if all waves have been spawned
        int actualWaveIndex = waveManager.currentWave + (waveManager.GetType().GetField("startWaveIndex", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(waveManager) as int? ?? 1);
        int endWave = (int)(waveManager.GetType().GetField("endWaveIndex", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(waveManager) ?? 5);
        
        if (waveManager.currentWave > 0 && actualWaveIndex > endWave)
        {
            if (!allWavesComplete)
            {
                allWavesComplete = true;
                Debug.Log($"🎯 All waves complete! Clear remaining enemies to win!");
            }
        }
    }
    
    void CheckInstantWinCondition()
    {
        // Check if all enemies are defeated
        if (enemySpawner != null && enemySpawner.ActiveEnemyCount == 0)
        {
            // Check coin requirement if enabled
            bool meetsRequirement = true;
            
            // Only check coins if requirement is set (> 0)
            if (requiredCoins > 0)
            {
                meetsRequirement = playerStats != null && playerStats.TotalCoins >= requiredCoins;
            }
            
            if (meetsRequirement)
            {
                TriggerVictory();
            }
            else if (requiredCoins > 0)
            {
                int coinsLeft = requiredCoins - (playerStats != null ? playerStats.TotalCoins : 0);
                Debug.Log($"All enemies cleared! Collect {coinsLeft} more coins to win!");
            }
        }
    }

    void CheckIfFinalWave()
    {
        // Check if current wave is the last wave
        if (waveManager.currentWave >= waveManager.totalWaves)
        {
            if (!isFinalWave)
            {
                isFinalWave = true;
                finalWaveTimer = 0f;
                Debug.Log($"🎯 Final Wave Started! Survive {surviveTime}s and collect {requiredCoins} coins to win!");
            }
        }
    }

    void CheckVictoryConditions()
    {
        // Check both conditions
        bool survivedLongEnough = finalWaveTimer >= surviveTime;
        bool hasEnoughCoins = playerStats != null && playerStats.TotalCoins >= requiredCoins;
        
        if (survivedLongEnough && hasEnoughCoins)
        {
            TriggerVictory();
        }
        else
        {
            // Optional: Show progress (you can remove this if too spammy)
            if (Mathf.FloorToInt(finalWaveTimer) % 10 == 0 && finalWaveTimer > 0.1f)
            {
                int timeLeft = Mathf.CeilToInt(surviveTime - finalWaveTimer);
                int coinsLeft = Mathf.Max(0, requiredCoins - (playerStats != null ? playerStats.TotalCoins : 0));
                
                if (timeLeft > 0 || coinsLeft > 0)
                {
                    Debug.Log($"Victory Progress - Time: {Mathf.FloorToInt(finalWaveTimer)}/{surviveTime}s | Coins: {(playerStats != null ? playerStats.TotalCoins : 0)}/{requiredCoins}");
                }
            }
        }
    }

    void TriggerVictory()
    {
        if (victoryTriggered) return;
        
        victoryTriggered = true;
        Debug.Log("🎉 VICTORY CONDITIONS MET!");
        
        // Show victory message
        if (waveManager != null)
        {
            waveManager.OnStateChange?.Invoke("🎉 YOU WIN! 🎉");
        }
        
        // Trigger game victory
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LevelComplete();
        }
    }

    // Public method to check current progress (can be called from UI)
    public string GetVictoryProgress()
    {
        if (!isFinalWave)
            return "Reach final wave to win!";
            
        int timeLeft = Mathf.CeilToInt(surviveTime - finalWaveTimer);
        int coinsLeft = Mathf.Max(0, requiredCoins - (playerStats != null ? playerStats.TotalCoins : 0));
        
        return $"Survive: {Mathf.Max(0, timeLeft)}s | Coins: {coinsLeft} more";
    }
}
