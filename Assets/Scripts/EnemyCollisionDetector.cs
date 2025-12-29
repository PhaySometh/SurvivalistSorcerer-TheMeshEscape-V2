using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy collision detector - Deals damage to player on collision
/// ENHANCED: Works with trigger colliders, regular colliders, or proximity detection
/// </summary>
public class EnemyCollisionDetector : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 25f; // Damage dealt to player per hit
    public float damageCooldown = 1f; // Cooldown between damage hits
    
    [Header("Detection Settings")]
    [Tooltip("Enable proximity-based damage detection (backup if colliders fail)")]
    public bool useProximityDetection = false;
    
    [Tooltip("Range for proximity detection")]
    public float proximityRange = 0.5f;
    
    [Tooltip("Maximum distance to deal damage (safety check) - increase if enemies aren't dealing damage")]
    public float maxDamageDistance = 5.0f;
    
    [Tooltip("How often to check proximity (seconds)")]
    public float proximityCheckInterval = 0.2f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private float lastDamageTime = 0f;
    private Transform playerTransform;
    private HealthSystem playerHealth;
    private float nextProximityCheck = 0f;
    private HealthSystem myHealth; // Enemy's own health
    private bool isEnemyDead = false;

    /* 
    SETUP GUIDE FOR PHAY:
    -----------------------------
    OPTION 1 (Best): Put this script on a CHILD object with a Trigger Collider
    - Main enemy has Rigidbody + Collider (not trigger) for physics
    - Child has Collider (IS TRIGGER = true) + this script
    
    OPTION 2 (Also works): Put this script on the main enemy object
    - The script will use proximity detection as a backup
    
    OPTION 3: Attach to any parent/child - it will find the right setup
    */

    private void Start()
    {
        // Find player at start
        FindPlayer();
        
        // Get enemy's health system
        myHealth = GetComponent<HealthSystem>();
        if (myHealth == null) myHealth = GetComponentInParent<HealthSystem>();
        if (myHealth != null)
        {
            myHealth.OnDeath.AddListener(OnEnemyDeath);
        }
        
        // Check if we have a collider set as trigger
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && !myCollider.isTrigger)
        {
            if (showDebugLogs) Debug.LogWarning($"⚠️ {gameObject.name}: EnemyCollisionDetector works best with a Trigger collider! Consider enabling useProximityDetection manually if needed.");
            // DON'T auto-enable proximity detection - let user decide
            // useProximityDetection = true;
        }
    }
    
    private void OnEnemyDeath()
    {
        isEnemyDead = true;
        // Disable this script when enemy dies
        enabled = false;
    }
    
    private void FindPlayer()
    {
        // Try to find player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<HealthSystem>();
            return;
        }
        
        // Fallback: find by name
        playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<HealthSystem>();
            return;
        }
        
        // Fallback: find by CharacterController
        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            playerTransform = cc.transform;
            playerHealth = cc.GetComponent<HealthSystem>();
        }
    }
    
    private void Update()
    {
        // Proximity-based damage detection as backup
        if (useProximityDetection && Time.time >= nextProximityCheck)
        {
            nextProximityCheck = Time.time + proximityCheckInterval;
            CheckProximityDamage();
        }
    }
    
    /// <summary>
    /// Backup: Check if player is within range and deal damage
    /// </summary>
    private void CheckProximityDamage()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        // Use THIS object's position, not root (more accurate for collision detectors on child objects)
        Vector3 enemyPos = transform.position;
        float distance = Vector3.Distance(enemyPos, playerTransform.position);
        
        if (distance <= proximityRange)
        {
            // Within range - try to deal damage
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 Proximity damage from {gameObject.name} at distance: {distance:F2}");
                }
                DealDamageToPlayer(playerTransform.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        // Don't deal damage if this enemy is dead
        if (isEnemyDead) return;
        if (myHealth != null && myHealth.IsDead) return;
        
        // Check if it's the player
        if (IsPlayer(collision.gameObject))
        {
            // SAFETY CHECK: Verify actual distance before dealing damage
            // Use root position for main enemy distance, or this object's position if closer
            float distanceFromThis = Vector3.Distance(transform.position, collision.transform.position);
            float distanceFromRoot = Vector3.Distance(transform.root.position, collision.transform.position);
            float distance = Mathf.Min(distanceFromThis, distanceFromRoot);
            
            if (distance <= maxDamageDistance)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 OnTriggerEnter damage from {gameObject.name} at distance: {distance:F2}");
                }
                DealDamageToPlayer(collision.gameObject);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"⚠️ Trigger collision detected but player too far ({distance:F2} > {maxDamageDistance})! Increase maxDamageDistance or check collider sizes.");
            }
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        // Don't deal damage if this enemy is dead
        if (isEnemyDead) return;
        if (myHealth != null && myHealth.IsDead) return;
        
        // Deal continuous damage while in contact
        if (IsPlayer(collision.gameObject))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                // SAFETY CHECK: Verify actual distance
                float distanceFromThis = Vector3.Distance(transform.position, collision.transform.position);
                float distanceFromRoot = Vector3.Distance(transform.root.position, collision.transform.position);
                float distance = Mathf.Min(distanceFromThis, distanceFromRoot);
                
                if (distance <= maxDamageDistance)
                {
                    DealDamageToPlayer(collision.gameObject);
                }
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Don't deal damage if this enemy is dead
        if (isEnemyDead) return;
        if (myHealth != null && myHealth.IsDead) return;
        
        // Also handle regular collisions (not just triggers)
        if (IsPlayer(collision.gameObject))
        {
            // SAFETY CHECK: Verify actual distance
            float distanceFromThis = Vector3.Distance(transform.position, collision.transform.position);
            float distanceFromRoot = Vector3.Distance(transform.root.position, collision.transform.position);
            float distance = Mathf.Min(distanceFromThis, distanceFromRoot);
            
            if (distance <= maxDamageDistance)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 OnCollisionEnter damage from {gameObject.name} at distance: {distance:F2}");
                }
                DealDamageToPlayer(collision.gameObject);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"⚠️ Collision detected but player too far ({distance:F2} > {maxDamageDistance})! Increase maxDamageDistance or check collider sizes.");
            }
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        // Don't deal damage if this enemy is dead
        if (isEnemyDead) return;
        if (myHealth != null && myHealth.IsDead) return;
        
        // Continuous damage on regular collision
        if (IsPlayer(collision.gameObject))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                // SAFETY CHECK: Verify actual distance
                float distanceFromThis = Vector3.Distance(transform.position, collision.transform.position);
                float distanceFromRoot = Vector3.Distance(transform.root.position, collision.transform.position);
                float distance = Mathf.Min(distanceFromThis, distanceFromRoot);
                
                if (distance <= maxDamageDistance)
                {
                    DealDamageToPlayer(collision.gameObject);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if the given object is the player
    /// </summary>
    private bool IsPlayer(GameObject obj)
    {
        // Check tag
        if (obj.CompareTag("Player")) return true;
        
        // Check name
        if (obj.name.Contains("Player")) return true;
        
        // Check for CharacterController
        if (obj.GetComponent<CharacterController>() != null) return true;
        
        // Check parent
        if (obj.transform.root.CompareTag("Player")) return true;
        
        return false;
    }

    /// <summary>
    /// Deal damage to the player
    /// </summary>
    private void DealDamageToPlayer(GameObject playerObject)
    {
        // Don't deal damage if this enemy is dead
        if (isEnemyDead) return;
        if (myHealth != null && myHealth.IsDead) return;
        
        // Find HealthSystem on the player
        HealthSystem health = playerObject.GetComponent<HealthSystem>();
        if (health == null) health = playerObject.GetComponentInParent<HealthSystem>();
        if (health == null) health = playerObject.GetComponentInChildren<HealthSystem>();
        if (health == null) health = playerHealth; // Use cached reference
        
        if (health != null && !health.IsDead)
        {
            lastDamageTime = Time.time;
            health.TakeDamage(damageAmount);
            
            if (showDebugLogs)
            {
                string enemyName = transform.root.name;
                Debug.Log($"🎯 {enemyName} dealt {damageAmount} damage to player! Player health: {health.CurrentHealth}");
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (useProximityDetection)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.root.position, proximityRange);
        }
    }
}

