using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple Enemy AI for chase game
/// Enemy patrols the environment and chases the player when detected
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Animator anim;
    public Transform player;

    [Header("Combat Settings")]
    public float attackRange = 0.8f; // Must be very close to attack
    public float attackCooldown = 2.0f;
    private float nextAttackTime = 0f;
    private bool isDead = false;
    
    [Header("Attack Visual Effects")]
    [Tooltip("Show attack indicator when attacking")]
    public bool showAttackIndicator = true;
    
    [Tooltip("Color of attack indicator")]
    public Color attackIndicatorColor = new Color(1f, 0.2f, 0.2f, 0.6f); // Red with transparency
    
    [Tooltip("Duration of attack indicator flash (seconds)")]
    public float attackIndicatorDuration = 0.5f;
    
    [Tooltip("Show attack range circle on ground")]
    public bool showRangeIndicator = true;
    
    [Tooltip("Color of range indicator")]
    public Color rangeIndicatorColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency
    
    // Visual components
    private LineRenderer attackLine;
    private GameObject rangeIndicator;
    private bool isAttacking = false;

    [Header("Animation Names")]
    public bool useTriggers = true;
    public string idleState = "idle";
    public string runState = "run";
    public string attackStatePrefix = "attack_0";
    public string damageState = "damage";
    public string dieState = "die";

    private string lastAnimationState = "";

    private void Awake()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponent<Animator>();
        
        // Find player automatically
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                player = GameObject.Find("Player").transform;
        }

        // Validate setup
        if (agent == null)
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
        if (player == null)
            Debug.LogError("Player not found!");
    }

    private void Start()
    {
        // Subscribe to HealthSystem for feedback animations
        HealthSystem health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.OnTakeDamage.AddListener(PlayDamageAnimation);
            health.OnDeath.AddListener(PlayDeathAnimation);
        }
        
        // Create visual effects for attacks
        CreateAttackVisuals();
    }

    private void Update()
    {
        if (player == null || agent == null || isDead)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }

        // Update movement animation state
        UpdateMovementAnimation();
        
        // Update attack visuals
        UpdateAttackVisuals();
    }

    private void UpdateMovementAnimation()
    {
        if (anim == null) return;

        float currentSpeed = agent.velocity.magnitude;
        
        if (currentSpeed > 0.1f)
        {
            PlayAnimation(runState);
        }
        else
        {
            PlayAnimation(idleState);
        }
    }

    private void PlayAnimation(string stateName)
    {
        if (lastAnimationState == stateName) return;

        if (useTriggers)
        {
            anim.SetTrigger(stateName);
        }
        else
        {
            anim.CrossFade(stateName, 0.1f);
        }
        
        lastAnimationState = stateName;
    }

    private void AttackPlayer()
    {
        // Stop moving
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (Time.time >= nextAttackTime)
        {
            if (anim != null)
            {
                // Randomly pick one of the 3 attack animations
                int attackRoll = Random.Range(1, 4);
                string attackTrigger = attackStatePrefix + attackRoll.ToString();
                
                if (useTriggers) anim.SetTrigger(attackTrigger);
                else anim.CrossFade(attackTrigger, 0.1f);
                
                lastAnimationState = "attacking";
            }
            
            // Show attack visual effect
            isAttacking = true;
            if (showAttackIndicator)
            {
                StartCoroutine(ShowAttackIndicator());
            }
            
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        
        // Removed the 0.5s delay to make the boss attack instantly when it catches the player
        
        Debug.DrawLine(transform.position, player.position, Color.red);
    }

    private void PlayDamageAnimation()
    {
        if (anim != null && !isDead)
        {
            if (useTriggers) anim.SetTrigger(damageState);
            else anim.CrossFade(damageState, 0.1f);
            
            lastAnimationState = "damage";
        }
    }

    private void PlayDeathAnimation()
    {
        isDead = true;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
        
        if (anim != null)
        {
            if (useTriggers) anim.SetTrigger(dieState);
            else anim.CrossFade(dieState, 0.1f);
            
            lastAnimationState = "dead";
        }
        
        // Hide attack visuals when dead
        if (attackLine != null) attackLine.enabled = false;
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
    }
    
    /// <summary>
    /// Create visual effects for attack indicators
    /// </summary>
    private void CreateAttackVisuals()
    {
        // Create attack line renderer
        GameObject lineObj = new GameObject("AttackLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        
        attackLine = lineObj.AddComponent<LineRenderer>();
        attackLine.startWidth = 0.05f;
        attackLine.endWidth = 0.05f;
        attackLine.positionCount = 2;
        attackLine.useWorldSpace = true;
        
        // Create material for the line
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = attackIndicatorColor;
        attackLine.material = lineMat;
        attackLine.enabled = false;
        
        // Create range indicator (circle on ground)
        if (showRangeIndicator)
        {
            rangeIndicator = CreateRangeCircle();
        }
    }
    
    /// <summary>
    /// Create a circle on the ground showing attack range
    /// </summary>
    private GameObject CreateRangeCircle()
    {
        GameObject circle = new GameObject("RangeIndicator");
        circle.transform.SetParent(transform);
        circle.transform.localPosition = Vector3.zero;
        
        LineRenderer lr = circle.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        
        // Create circle shape
        int segments = 32;
        lr.positionCount = segments;
        
        float angle = 0f;
        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Cos(angle) * attackRange;
            float z = Mathf.Sin(angle) * attackRange;
            lr.SetPosition(i, new Vector3(x, 0.05f, z)); // Slightly above ground
            
            angle += 2f * Mathf.PI / segments;
        }
        
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        
        // Create material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = rangeIndicatorColor;
        lr.material = mat;
        
        circle.SetActive(false); // Start hidden
        return circle;
    }
    
    /// <summary>
    /// Update attack visual effects
    /// </summary>
    private void UpdateAttackVisuals()
    {
        if (isDead) return;
        
        // Show range indicator when player is near
        if (rangeIndicator != null && showRangeIndicator)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // Show range when player is within detection range (2x attack range)
            bool shouldShow = distanceToPlayer <= attackRange * 3f;
            rangeIndicator.SetActive(shouldShow);
            
            // Pulse effect when player is close
            if (shouldShow)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.2f;
                LineRenderer lr = rangeIndicator.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    float baseWidth = distanceToPlayer <= attackRange ? 0.08f : 0.05f;
                    lr.startWidth = baseWidth * pulse;
                    lr.endWidth = baseWidth * pulse;
                    
                    // Change color based on proximity
                    if (distanceToPlayer <= attackRange)
                    {
                        // In range - make it more red
                        lr.material.color = new Color(1f, 0f, 0f, 0.5f);
                    }
                    else
                    {
                        // Out of range - orange
                        lr.material.color = rangeIndicatorColor;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Show attack indicator line from enemy to player
    /// </summary>
    private System.Collections.IEnumerator ShowAttackIndicator()
    {
        if (attackLine == null || player == null) yield break;
        
        attackLine.enabled = true;
        float elapsed = 0f;
        
        while (elapsed < attackIndicatorDuration)
        {
            elapsed += Time.deltaTime;
            
            // Line from enemy to player at chest height
            Vector3 startPos = transform.position + Vector3.up * 1f;
            Vector3 endPos = player.position + Vector3.up * 1f;
            
            attackLine.SetPosition(0, startPos);
            attackLine.SetPosition(1, endPos);
            
            // Flash effect
            float flash = Mathf.PingPong(Time.time * 10f, 1f);
            Color flashColor = attackIndicatorColor * (0.5f + flash * 0.5f);
            attackLine.material.color = flashColor;
            
            yield return null;
        }
        
        attackLine.enabled = false;
        isAttacking = false;
    }


    /// <summary>
    /// Draw gizmos for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
