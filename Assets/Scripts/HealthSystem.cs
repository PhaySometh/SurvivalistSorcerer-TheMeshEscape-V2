using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 150f;
    public bool destroyOnDeath = true;
    
    [Header("Death Effects")]
    [Tooltip("Particle effect to spawn when enemy dies")]
    public GameObject deathParticlePrefab;
    
    [Tooltip("Create default particle effect if none assigned")]
    public bool useDefaultParticles = true;
    
    [Tooltip("Disable coin drops on enemy death")]
    public bool disableCoinDrops = true;

    [Header("Debug/Status")]
    [SerializeField] private float currentHealth;

    // Events
    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;
    public UnityEvent<float, float> OnHealthChanged; // currentHealth, maxHealth

    public bool IsDead => currentHealth <= 0;
    public float CurrentHealth => currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        // Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}");

        // Flash red when hit (for enemies)
        if (!gameObject.CompareTag("Player"))
        {
            StartCoroutine(FlashRed());
        }

        // Trigger animation if player
        PlayerAnimatorController anim = GetComponent<PlayerAnimatorController>();
        if (anim != null) anim.TriggerGetHit();

        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Set health to a specific value (used when leveling up)
    /// </summary>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    /// <summary>
    /// Get health as percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    private void Die()
    {
        currentHealth = 0;
        Debug.Log($"💀 {gameObject.name} died!");
        
        // Trigger death animation
        PlayerAnimatorController playerAnim = GetComponent<PlayerAnimatorController>();
        if (playerAnim != null) 
        {
            playerAnim.TriggerDeath();
        }
        
        // For enemies: Check for regular Animator
        Animator basicAnim = GetComponent<Animator>();
        if (basicAnim != null && playerAnim == null)
        {
            // Try multiple possible trigger names for death animation
            basicAnim.SetTrigger("Die");
            basicAnim.SetTrigger("die");
            basicAnim.SetTrigger("Death");
            basicAnim.SetTrigger("death");
            basicAnim.SetBool("IsDead", true);
            basicAnim.SetBool("isDead", true);
        }
        
        // For enemies: Stop NavMeshAgent
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        // For enemies: Disable AI
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // For enemies: Disable collision detector
        EnemyCollisionDetector collisionDetector = GetComponentInChildren<EnemyCollisionDetector>();
        if (collisionDetector != null)
        {
            collisionDetector.enabled = false;
        }
        
        // Optionally disable the main collider so player can walk through
        Collider col = GetComponent<Collider>();
        if (col != null && !gameObject.CompareTag("Player"))
        {
            col.enabled = false;
        }

        OnDeath?.Invoke();

        if (gameObject.CompareTag("Player"))
        {
            Debug.Log("🎮 PLAYER DIED - GAME OVER");
            
            // Disable movement
            var mover = GetComponent<PlayerMovementScript>();
            if (mover != null) mover.enabled = false;
            
            // Trigger Game Over in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
        else if (destroyOnDeath)
        {
            // Increment kill count in GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddKill();
            }
            
            // Spawn death particles for enemies
            SpawnDeathParticles();
            
            // For enemies: Start death sequence with animation and fade out
            StartCoroutine(DeathSequence());
        }
    }
    
    /// <summary>
    /// Spawn death particle effects
    /// </summary>
    private void SpawnDeathParticles()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 1f;
        
        // Use assigned particle prefab
        if (deathParticlePrefab != null)
        {
            GameObject particles = Instantiate(deathParticlePrefab, spawnPosition, Quaternion.identity);
            Destroy(particles, 3f);
            Debug.Log($"💥 Spawned death particles for {gameObject.name}");
            return;
        }
        
        // Create default particle system if none assigned
        if (useDefaultParticles)
        {
            CreateDefaultDeathParticles(spawnPosition);
        }
    }
    
    /// <summary>
    /// Create a simple default particle effect for enemy death
    /// </summary>
    private void CreateDefaultDeathParticles(Vector3 position)
    {
        GameObject particleObj = new GameObject("DeathParticles");
        particleObj.transform.position = position;
        
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 5f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0.3f, 0f, 1f); // Orange/red color
        main.gravityModifier = 0.5f;
        main.maxParticles = 50;
        
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 0.5f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        ps.Play();
        
        // Add a light for extra effect
        Light light = particleObj.AddComponent<Light>();
        light.color = new Color(1f, 0.5f, 0f);
        light.intensity = 3f;
        light.range = 5f;
        StartCoroutine(FadeLight(light));
        
        Destroy(particleObj, 3f);
        Debug.Log($"✨ Created default death particles at {position}");
    }
    
    /// <summary>
    /// Fade out light effect
    /// </summary>
    private System.Collections.IEnumerator FadeLight(Light light)
    {
        float duration = 1f;
        float elapsed = 0f;
        float startIntensity = light.intensity;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / duration);
            yield return null;
        }
    }
    
    /// <summary>
    /// Death sequence for enemies: play animation, fade out, then destroy
    /// </summary>
    private System.Collections.IEnumerator DeathSequence()
    {
        // Wait for death animation to play (adjust timing based on your animation length)
        yield return new WaitForSeconds(1.5f);
        
        // Fade out the enemy over 0.5 seconds
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float fadeTime = 0.5f;
        float elapsed = 0f;
        
        // Store original colors
        Color[][] originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            originalColors[i] = new Color[materials.Length];
            for (int j = 0; j < materials.Length; j++)
            {
                originalColors[i][j] = materials[j].color;
                
                // Set material to transparent mode
                Material mat = materials[j];
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
        
        // Fade out
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeTime);
            
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    Color col = originalColors[i][j];
                    col.a = alpha;
                    materials[j].color = col;
                }
            }
            
            yield return null;
        }
        
        // Destroy the enemy
        Destroy(gameObject);
    }

    
    /// <summary>
    /// Reset health to full (used for respawn)
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    /// <summary>
    /// Flash red when taking damage (improved to avoid material issues)
    /// </summary>
    private System.Collections.IEnumerator FlashRed()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        
        // Store original colors (don't change materials, just colors)
        Color[][] originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            originalColors[i] = new Color[materials.Length];
            for (int j = 0; j < materials.Length; j++)
            {
                originalColors[i][j] = materials[j].color;
            }
        }
        
        // Flash red by tinting the color
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j].color = Color.red;
            }
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                Material[] materials = renderers[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j].color = originalColors[i][j];
                }
            }
        }
    }
}
