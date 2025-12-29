using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : MonoBehaviour
{
    public enum WaveState { WaitingToStart, WaveInProgress, PreparationBuffer, BossFight, SuddenDeath, Victory }

    [Header("Wave Configuration")]
    public int totalWaves = 5;
    public float waveDuration = 120f; // 2 minutes soft limit
    public float bufferDuration = 15f; // 15 seconds break
    
    private int startWaveIndex = 1; // Which wave to start from
    private int endWaveIndex = 5;   // Which wave to end at
    private bool startWithSuddenDeath = false;
    private int coinsRequired = 0;

    [Header("Current Status")]
    public int currentWave = 0; // 0 means not started, 1-5 are waves
    public WaveState currentState = WaveState.WaitingToStart;
    public float stateTimer = 0f;

    [Header("References")]
    public EnemySpawner enemySpawner;
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;

    // Events for UI
    public UnityEvent<int> OnWaveChange = new UnityEvent<int>();
    public UnityEvent<string> OnStateChange = new UnityEvent<string>(); // e.g. "Wave 1", "Resting...", "BOSS!"

    void Start()
    {
        // Load difficulty settings
        LoadDifficultySettings();
        
        // Optional: Auto start or wait for player input
        StartCoroutine(StartGameLoop());
    }
    
    /// <summary>
    /// Load wave configuration based on selected difficulty
    /// </summary>
    void LoadDifficultySettings()
    {
        if (GameSettings.Instance != null)
        {
            WaveConfig config = GameSettings.Instance.GetWaveConfig();
            
            totalWaves = config.totalWaves;
            waveDuration = config.waveDuration;
            bufferDuration = config.bufferDuration;
            startWaveIndex = config.startWave;
            endWaveIndex = config.endWave;
            startWithSuddenDeath = config.startWithSuddenDeath;
            coinsRequired = config.coinsRequired;
            
            // Apply to GameManager time limit
            if (GameManager.Instance != null)
            {
                GameManager.Instance.levelTimeLimit = config.timeLimit;
            }
            
            Debug.Log($"Loaded {GameSettings.Instance.currentDifficulty} difficulty: Waves {startWaveIndex}-{endWaveIndex}, {waveDuration}s per wave, {config.timeLimit}s total time");
            
            if (startWithSuddenDeath)
            {
                Debug.Log("HARD MODE: Sudden death will activate after wave!");
            }
            
            if (coinsRequired > 0)
            {
                Debug.Log($"Coin collection required: {coinsRequired} coins");
            }
        }
        else
        {
            Debug.LogWarning("GameSettings not found! Using default wave configuration.");
            startWaveIndex = 1;
            endWaveIndex = 5;
        }
    }

    IEnumerator StartGameLoop()
    {
        currentState = WaveState.WaitingToStart;
        
        // Minor delay to ensure UI is initialized, but feels immediate
        yield return new WaitForSeconds(0.1f); 

        // A. Game Start Sequence - Different messages based on difficulty
        if (GameSettings.Instance != null)
        {
            switch (GameSettings.Instance.currentDifficulty)
            {
                case GameSettings.Difficulty.Easy:
                    OnStateChange?.Invoke("You have 2 minutes to escape...");
                    yield return new WaitForSeconds(5.0f);
                    OnStateChange?.Invoke("Survive one wave of enemies!");
                    break;
                    
                case GameSettings.Difficulty.Medium:
                    OnStateChange?.Invoke("You have 8 minutes to escape...");
                    yield return new WaitForSeconds(5.0f);
                    OnStateChange?.Invoke("Survive three waves of enemies!");
                    break;
                    
                case GameSettings.Difficulty.Hard:
                    OnStateChange?.Invoke("You have 1 minute to escape...");
                    yield return new WaitForSeconds(5.0f);
                    OnStateChange?.Invoke("Defeat the boss as fast as you can!");
                    break;
                    
                default:
                    OnStateChange?.Invoke("You have 10 minutes to escape...");
                    yield return new WaitForSeconds(5.0f);
                    OnStateChange?.Invoke("Survive all waves and defeat the boss!");
                    break;
            }
        }
        else
        {
            OnStateChange?.Invoke("You have 10 minutes to escape...");
            yield return new WaitForSeconds(5.0f);
            OnStateChange?.Invoke("Survive and escape!");
        }
        
        OnStateChange?.Invoke("Are you ready?");
        yield return new WaitForSeconds(4.0f);
        
        OnStateChange?.Invoke("THE GAME BEGINS!");
        yield return new WaitForSeconds(2.0f);

        // Start the actual game timer in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }

        currentWave = 0;
        StartNextWave();
    }

    void Update()
    {
        if (currentState == WaveState.Victory || currentState == WaveState.WaitingToStart) return;

        // Handle Timers
        if (stateTimer > 0)
        {
            stateTimer -= Time.deltaTime;
        }

        // State Machine
        switch (currentState)
        {
            case WaveState.WaveInProgress:
                HandleWaveLogic();
                break;

            case WaveState.PreparationBuffer:
                if (stateTimer <= 0)
                {
                    StartNextWave();
                }
                break;

            case WaveState.BossFight:
                // Check if Boss is dead by checking if there are any active enemies
                if (enemySpawner.ActiveEnemyCount == 0)
                {
                    // Boss defeated, trigger victory if not already triggered
                    if (currentState == WaveState.BossFight)
                    {
                        OnBossDefeated();
                    }
                }
                break;
                
            case WaveState.SuddenDeath:
                // Relaxed spawning (once per second) so it's challenging but not impossible
                if (Time.frameCount % 60 == 0) 
                {
                    // Mix 60% Weak (Slime/Turtle) and 40% Medium (Skeleton/Golem)
                    enemySpawner.SpawnMixedEnemies(1, new float[] { 0.6f, 0.4f, 0.0f });
                }
                break;
        }
    }

    void HandleWaveLogic()
    {
        // Check if we've finished all waves for this difficulty
        int actualWaveIndex = startWaveIndex + currentWave;
        if (actualWaveIndex > endWaveIndex) return;

        // Check if all enemies are cleared - immediately advance to next wave
        if (enemySpawner.ActiveEnemyCount == 0)
        {
            Debug.Log("✅ Wave Cleared! All enemies defeated!");
            OnStateChange?.Invoke("Wave Clear! Well done!");
            StartBuffer();
            return;
        }

        // Scenario B: Time up -> Force buffer (enemies stay)
        if (stateTimer <= 0)
        {
            Debug.Log("⏰ Time's Up! Enemies remain.");
            OnStateChange?.Invoke("Haven't cleared the wave yet?");
            
            // Random mocking text
            string[] mocks = { "You're so weak...", "Too slow!", "My grandma moves faster than you.", "Is that all?" };
            string randomMock = mocks[Random.Range(0, mocks.Length)];
            
            StartCoroutine(ShowSequenceDelayed(randomMock, 2f, "Next wave spawning anyway!"));
            StartBuffer();
        }
    }

    IEnumerator ShowSequenceDelayed(string first, float delay, string second)
    {
        OnStateChange?.Invoke(first);
        yield return new WaitForSeconds(delay);
        OnStateChange?.Invoke(second);
    }

    void StartBuffer()
    {
        currentState = WaveState.PreparationBuffer;
        stateTimer = bufferDuration;
        
        StartCoroutine(BufferSequence());
    }

    IEnumerator BufferSequence()
    {
        yield return new WaitForSeconds(2f);
        OnStateChange?.Invoke("Prepare yourself...");
        
        // Check if there's a next wave
        int nextActualWaveIndex = startWaveIndex + currentWave;
        if (nextActualWaveIndex <= endWaveIndex)
        {
            yield return new WaitForSeconds(bufferDuration - 5f);
            OnStateChange?.Invoke($"Wave {nextActualWaveIndex} is coming!");
        }
        else if (startWithSuddenDeath)
        {
            yield return new WaitForSeconds(bufferDuration - 5f);
            OnStateChange?.Invoke("Get ready for SUDDEN DEATH!");
        }
    }

    void StartNextWave()
    {
        // Calculate which actual wave to spawn
        int wavesToSpawned = currentWave + 1;
        int actualWaveIndex = startWaveIndex + currentWave;
        
        // Check if we've spawned all waves for this difficulty
        if (actualWaveIndex > endWaveIndex) 
        {
            Debug.Log("All waves complete for this difficulty.");
            
            // For hard mode, check if we need to activate sudden death
            if (startWithSuddenDeath && currentState != WaveState.SuddenDeath)
            {
                TriggerSuddenDeath();
            }
            return;
        }

        currentWave++;

        currentState = WaveState.WaveInProgress;
        stateTimer = waveDuration; 
        
        OnWaveChange?.Invoke(currentWave);
        OnStateChange?.Invoke($"WAVE {actualWaveIndex}");

        // Start the specialized sequence for the actual wave
        StartCoroutine(SpawnWaveSequence(actualWaveIndex));
    }

    IEnumerator SpawnWaveSequence(int wave)
    {
        // INDEX REFERENCE (Based on your setup):
        // Weak Category [0]:  [0] Slime, [1] Turtle
        // Medium Category [1]: [0] Skeleton, [1] Golem
        // Strong Category [2]: (Empty or other monsters)
        // Boss Prefab: Bull King (Assigned to bossPrefab slot, NOT in spawner lists)

        switch (wave)
        {
            case 1:
                yield return StartCoroutine(SpawnSequenceStep(5, 0, 0)); // 5 Slimes (Weak 0)
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(3, 0, 1)); // 3 Turtles (Weak 1)
                break;

            case 2:
                yield return StartCoroutine(SpawnSequenceStep(4, 0, 0)); // 4 Slimes
                yield return new WaitForSeconds(4f);
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 1)); // 2 Turtles
                yield return new WaitForSeconds(4f);
                yield return StartCoroutine(SpawnSequenceStep(1, 1, 0)); // 1 Skeleton (Medium 0)
                break;

            case 3:
                yield return StartCoroutine(SpawnSequenceStep(4, 0, 0)); // 4 Slimes
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 1)); // 2 Turtles
                yield return new WaitForSeconds(4f);
                yield return StartCoroutine(SpawnSequenceStep(2, 1, 0)); // 2 Skeletons
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(1, 1, 1)); // 1 Golem (Medium 1)
                break;

            case 4:
                yield return StartCoroutine(SpawnSequenceStep(3, 0, 0)); // 3 Slimes
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 1)); // 2 Turtles
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(3, 1, 0)); // 3 Skeletons
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(2, 1, 1)); // 2 Golems
                break;

            case 5:
                yield return StartCoroutine(SpawnSequenceStep(2, 0, 0)); // 2 Slimes
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(1, 0, 1)); // 1 Turtle
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(SpawnSequenceStep(3, 1, 0)); // 3 Skeletons
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(SpawnSequenceStep(2, 1, 1)); // 2 Golems
                yield return new WaitForSeconds(3f);
                StartBossFight(); // The Bull King (Unique Boss Slot)
                break;
        }
    }

    IEnumerator SpawnSequenceStep(int count, int difficulty, int subIndex)
    {
        enemySpawner.SpawnEnemies(count, difficulty, subIndex);
        // Wait just a bit before spawning the next tier to avoid total overlap
        yield return new WaitForSeconds(1.0f);
    }

    void StartBossFight()
    {
        currentState = WaveState.BossFight;
        OnStateChange?.Invoke("BOSS FIGHT!");
        
        // Spawn Boss logic...
        if (bossPrefab != null && bossSpawnPoint != null)
        {
            Vector3 spawnPos = bossSpawnPoint.position;
            
            // ENSURE BOSS IS GROUNDED ON NAVMESH
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
            
            // ✨ AUTO-FIX: Ensure boss has collision detector for damage
            EnsureBossHasCollisionDetector(boss);
            
            // Force NavMeshAgent to snap to position
            UnityEngine.AI.NavMeshAgent agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(spawnPos);
                agent.enabled = true;
            }

            // TRIGGER BOSS TAUNT (DEFY)
            Animator anim = boss.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("defy");
            }

            HealthSystem bossHealth = boss.GetComponent<HealthSystem>();
            if (bossHealth != null)
            {
                bossHealth.OnDeath.AddListener(OnBossDefeated);
            }
        }
    }

    public void TriggerSuddenDeath()
    {
        if (currentState != WaveState.Victory && currentState != WaveState.SuddenDeath)
        {
            currentState = WaveState.SuddenDeath;
            string[] timeUpMocks = { "Hehe, still haven't defeated the boss?", "Broooo, go defeat the boss!", "Time is UP. Survive this!" };
            OnStateChange?.Invoke(timeUpMocks[Random.Range(0, timeUpMocks.Length)]);
            
            // UI Notification is already called from GameManager for the timer color
        }
    }

    void OnBossDefeated()
    {
        currentState = WaveState.Victory;
        OnStateChange?.Invoke("Damn, boi. You got this!");
        
        StartCoroutine(VictorySequence());
    }

    IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(3f);
        OnStateChange?.Invoke("ESCAPE PORTAL OPEN!");
        GameManager.Instance.LevelComplete();
    }
    
    /// <summary>
    /// Ensures boss has proper collision detection setup for dealing damage to player
    /// This fixes bull boss not dealing damage
    /// </summary>
    void EnsureBossHasCollisionDetector(GameObject boss)
    {
        // Check if boss already has collision detector
        EnemyCollisionDetector detector = boss.GetComponentInChildren<EnemyCollisionDetector>();
        
        if (detector == null)
        {
            // No detector found - add one
            Collider[] colliders = boss.GetComponentsInChildren<Collider>();
            GameObject targetObject = null;
            
            // Look for trigger collider first
            foreach (Collider col in colliders)
            {
                if (col.isTrigger)
                {
                    targetObject = col.gameObject;
                    break;
                }
            }
            
            // If no trigger, use main object
            if (targetObject == null)
            {
                targetObject = boss;
            }
            
            // Add component with boss-appropriate settings
            detector = targetObject.AddComponent<EnemyCollisionDetector>();
            detector.damageAmount = 35f; // Bosses deal more damage
            detector.damageCooldown = 1.5f;
            detector.useProximityDetection = false;
            detector.proximityRange = 1.0f;
            detector.maxDamageDistance = 6.0f; // Larger range for big boss
            detector.showDebugLogs = true;
            
            Debug.Log($"✅ Auto-added EnemyCollisionDetector to BOSS {boss.name}");
        }
        else
        {
            // Exists - ensure good settings
            detector.maxDamageDistance = Mathf.Max(detector.maxDamageDistance, 6.0f);
            detector.damageAmount = Mathf.Max(detector.damageAmount, 35f);
            detector.showDebugLogs = true;
        }
    }
}
