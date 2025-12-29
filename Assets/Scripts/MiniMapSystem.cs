using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mini Map System - Shows player (blue) and enemies (red) on a radar-style map
/// Similar to GTA minimap
/// </summary>
public class MiniMapSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The UI canvas where minimap is drawn")]
    public RectTransform miniMapPanel;
    
    [Tooltip("Player transform to track")]
    public Transform playerTransform;
    
    [Header("Icon Prefabs")]
    [Tooltip("UI Image prefab for player icon (blue circle)")]
    public GameObject playerIconPrefab;
    
    [Tooltip("UI Image prefab for enemy icons (red circles)")]
    public GameObject enemyIconPrefab;
    
    [Header("Map Settings")]
    [Tooltip("Size of the map area in world units")]
    public float mapRange = 50f;
    
    [Tooltip("Size of player icon")]
    public float playerIconSize = 30f;
    
    [Tooltip("Size of enemy icons")]
    public float enemyIconSize = 22f;
    
    [Tooltip("Update rate (times per second)")]
    public float updateRate = 10f;
    
    [Tooltip("Auto-create icons if prefabs not assigned")]
    public bool autoCreateIcons = true;
    
    [Header("Colors")]
    public Color playerColor = Color.cyan;
    public Color enemyColor = Color.red;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Internal
    private GameObject playerIcon;
    private Dictionary<GameObject, GameObject> enemyIcons = new Dictionary<GameObject, GameObject>();
    private float updateTimer = 0f;
    private float updateInterval;
    private EnemySpawner enemySpawner;

    void Start()
    {
        updateInterval = 1f / updateRate;
        
        // Auto-find player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
        
        // Find enemy spawner
        enemySpawner = FindObjectOfType<EnemySpawner>();
        
        // Auto-create minimap panel if not assigned
        if (miniMapPanel == null)
        {
            CreateMiniMapPanel();
        }
        
        // Create icon prefabs if needed
        if (autoCreateIcons)
        {
            if (playerIconPrefab == null)
                playerIconPrefab = CreateArrowIconPrefab(playerColor, "PlayerIconPrefab");
                
            if (enemyIconPrefab == null)
                enemyIconPrefab = CreateIconPrefab(enemyColor, "EnemyIconPrefab");
        }
        
        // Create player icon
        CreatePlayerIcon();
        
        Debug.Log("✅ MiniMap System initialized!");
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateMiniMap();
        }
    }
    
    /// <summary>
    /// Create the minimap panel UI if not assigned
    /// </summary>
    void CreateMiniMapPanel()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MiniMapSystem: No Canvas found in scene!");
            return;
        }
        
        // Create minimap panel
        GameObject panelObj = new GameObject("MiniMapPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        miniMapPanel = panelObj.AddComponent<RectTransform>();
        
        // Position in top-right corner
        miniMapPanel.anchorMin = new Vector2(1, 1);
        miniMapPanel.anchorMax = new Vector2(1, 1);
        miniMapPanel.pivot = new Vector2(1, 1);
        miniMapPanel.anchoredPosition = new Vector2(-20, -20);
        miniMapPanel.sizeDelta = new Vector2(200, 200);
        
        // Add background
        Image bgImage = panelObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        
        // Make circular by adding a circular mask
        Mask mask = panelObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;
        
        // Create circular sprite for mask
        Texture2D circleTex = CreateCircleTexture(200, Color.white);
        Sprite circleSprite = Sprite.Create(circleTex, new Rect(0, 0, 200, 200), new Vector2(0.5f, 0.5f));
        bgImage.sprite = circleSprite;
        
        // Add border
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3, 3);
        
        Debug.Log("🗺️ Auto-created circular MiniMap panel in top-right corner");
    }
    
    /// <summary>
    /// Create an arrow-shaped icon for the player
    /// </summary>
    GameObject CreateArrowIconPrefab(Color color, string name)
    {
        GameObject icon = new GameObject(name);
        icon.SetActive(false); // Keep as prefab
        
        Image img = icon.AddComponent<Image>();
        
        // Create arrow sprite pointing up
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        // Fill with transparent
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
        
        // Draw arrow shape (pointing up)
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Triangle pointing up (apex at top)
                // Top triangle part
                if (y >= 16)
                {
                    int halfY = y - 16;
                    int leftEdge = 16 - halfY;
                    int rightEdge = 16 + halfY;
                    if (x >= leftEdge && x <= rightEdge)
                        pixels[y * 32 + x] = color;
                }
                // Bottom rectangle (tail)
                else if (y < 16 && x >= 12 && x <= 20)
                {
                    pixels[y * 32 + x] = color;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        img.sprite = sprite;
        img.color = color;
        
        RectTransform rt = icon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(16, 16);
        
        return icon;
    }
    
    /// <summary>
    /// Create a circular texture for minimap mask
    /// </summary>
    Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        float center = size / 2f;
        float radius = center;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (dist <= radius)
                    pixels[y * size + x] = color;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    /// <summary>
    /// Create a simple circular icon prefab
    /// </summary>
    GameObject CreateIconPrefab(Color color, string name)
    {
        GameObject icon = new GameObject(name);
        icon.SetActive(false); // Keep as prefab
        
        Image img = icon.AddComponent<Image>();
        
        // Create circular sprite
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 16) / 16f;
                float dy = (y - 16) / 16f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (dist <= 1f)
                    pixels[y * 32 + x] = color;
                else
                    pixels[y * 32 + x] = Color.clear;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        img.sprite = sprite;
        img.color = color;
        
        RectTransform rt = icon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(12, 12);
        
        return icon;
    }
    
    /// <summary>
    /// Create player icon on minimap
    /// </summary>
    void CreatePlayerIcon()
    {
        if (miniMapPanel == null || playerIconPrefab == null) return;
        
        playerIcon = Instantiate(playerIconPrefab, miniMapPanel);
        playerIcon.SetActive(true);
        playerIcon.name = "PlayerIcon";
        
        RectTransform rt = playerIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(playerIconSize, playerIconSize);
        rt.anchoredPosition = Vector2.zero; // Center (player is always centered)
    }
    
    /// <summary>
    /// Update all icons on the minimap
    /// </summary>
    void UpdateMiniMap()
    {
        if (playerTransform == null || miniMapPanel == null) return;
        
        // Update player icon rotation (optional - shows facing direction)
        if (playerIcon != null)
        {
            RectTransform rt = playerIcon.GetComponent<RectTransform>();
            rt.rotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y);
        }
        
        // Get all active enemies
        List<GameObject> activeEnemies = new List<GameObject>();
        
        if (enemySpawner != null && enemySpawner.activeEnemies != null)
        {
            activeEnemies.AddRange(enemySpawner.activeEnemies);
        }
        else
        {
            // Fallback: Find by tag
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            activeEnemies.AddRange(enemies);
        }
        
        // Remove icons for dead/missing enemies
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in enemyIcons)
        {
            if (kvp.Key == null || !activeEnemies.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in toRemove)
        {
            enemyIcons.Remove(key);
        }
        
        // Update or create enemy icons
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy == null) continue;
            
            // Check if enemy is alive
            HealthSystem health = enemy.GetComponent<HealthSystem>();
            if (health != null && health.IsDead) continue;
            
            // Get or create icon
            GameObject icon;
            if (!enemyIcons.ContainsKey(enemy))
            {
                icon = CreateEnemyIcon(enemy);
                enemyIcons[enemy] = icon;
            }
            else
            {
                icon = enemyIcons[enemy];
            }
            
            // Update position
            UpdateIconPosition(icon, enemy.transform);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"🗺️ MiniMap: Tracking {enemyIcons.Count} enemies");
        }
    }
    
    /// <summary>
    /// Create an icon for an enemy
    /// </summary>
    GameObject CreateEnemyIcon(GameObject enemy)
    {
        if (miniMapPanel == null || enemyIconPrefab == null) return null;
        
        GameObject icon = Instantiate(enemyIconPrefab, miniMapPanel);
        icon.SetActive(true);
        icon.name = $"EnemyIcon_{enemy.name}";
        
        RectTransform rt = icon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(enemyIconSize, enemyIconSize);
        
        return icon;
    }
    
    /// <summary>
    /// Update icon position relative to player
    /// </summary>
    void UpdateIconPosition(GameObject icon, Transform target)
    {
        if (icon == null || target == null || playerTransform == null) return;
        
        RectTransform rt = icon.GetComponent<RectTransform>();
        
        // Calculate relative position (target - player)
        Vector3 offset = target.position - playerTransform.position;
        
        // Only use horizontal plane (X, Z)
        Vector2 offset2D = new Vector2(offset.x, offset.z);
        
        // Scale to minimap size
        float mapHalfSize = miniMapPanel.rect.width / 2f;
        Vector2 iconPos = offset2D * (mapHalfSize / mapRange);
        
        // Clamp to minimap bounds
        iconPos = Vector2.ClampMagnitude(iconPos, mapHalfSize - 10f);
        
        rt.anchoredPosition = iconPos;
        
        // Hide if too far
        bool isInRange = offset2D.magnitude <= mapRange;
        icon.SetActive(isInRange);
    }
    
    /// <summary>
    /// Public method to adjust map range
    /// </summary>
    public void SetMapRange(float range)
    {
        mapRange = Mathf.Max(10f, range);
    }
}
