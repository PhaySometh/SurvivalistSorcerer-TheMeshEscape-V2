using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current difficulty setting on UI
/// Attach this to any UI text element that should show the difficulty
/// </summary>
public class DifficultyDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Text component to display difficulty (auto-finds if not set)")]
    public TextMeshProUGUI difficultyText;
    
    [Header("Display Settings")]
    [Tooltip("Show full difficulty name or abbreviated")]
    public bool showFullName = true;
    
    [Tooltip("Prefix text before difficulty name")]
    public string prefixText = "Difficulty: ";
    
    [Tooltip("Update every frame (use if difficulty can change during gameplay)")]
    public bool updateContinuously = false;
    
    [Header("Color Coding (optional)")]
    [Tooltip("Enable color coding based on difficulty")]
    public bool useColorCoding = false;
    
    public Color easyColor = new Color(0.2f, 1f, 0.2f);      // Green
    public Color mediumColor = new Color(1f, 0.8f, 0f);      // Yellow
    public Color hardColor = new Color(1f, 0.2f, 0.2f);      // Red
    public Color defaultColor = new Color(0.5f, 0.5f, 1f);   // Blue

    void Start()
    {
        // Auto-find TextMeshProUGUI if not assigned
        if (difficultyText == null)
        {
            difficultyText = GetComponent<TextMeshProUGUI>();
        }
        
        // Initial update
        UpdateDifficultyDisplay();
    }

    void Update()
    {
        if (updateContinuously)
        {
            UpdateDifficultyDisplay();
        }
    }
    
    /// <summary>
    /// Update the difficulty text display
    /// </summary>
    public void UpdateDifficultyDisplay()
    {
        if (difficultyText == null) return;
        
        // Get current difficulty from GameSettings
        if (GameSettings.Instance != null)
        {
            GameSettings.Difficulty currentDifficulty = GameSettings.Instance.currentDifficulty;
            
            // Format the difficulty name
            string difficultyName = GetDifficultyDisplayName(currentDifficulty);
            
            // Set the text
            difficultyText.text = prefixText + difficultyName;
            
            // Apply color if enabled
            if (useColorCoding)
            {
                difficultyText.color = GetDifficultyColor(currentDifficulty);
            }
        }
        else
        {
            // Fallback if GameSettings not found
            difficultyText.text = prefixText + "Medium";
            Debug.LogWarning("GameSettings instance not found! Using default difficulty.");
        }
    }
    
    /// <summary>
    /// Get display name for difficulty
    /// </summary>
    private string GetDifficultyDisplayName(GameSettings.Difficulty difficulty)
    {
        if (!showFullName)
        {
            // Abbreviated names
            switch (difficulty)
            {
                case GameSettings.Difficulty.Easy: return "Easy";
                case GameSettings.Difficulty.Medium: return "Med";
                case GameSettings.Difficulty.Hard: return "Hard";
                case GameSettings.Difficulty.Default: return "Def";
                default: return "Med";
            }
        }
        else
        {
            // Full names
            switch (difficulty)
            {
                case GameSettings.Difficulty.Easy: return "Easy";
                case GameSettings.Difficulty.Medium: return "Medium";
                case GameSettings.Difficulty.Hard: return "Hard";
                case GameSettings.Difficulty.Default: return "Default";
                default: return "Medium";
            }
        }
    }
    
    /// <summary>
    /// Get color based on difficulty
    /// </summary>
    private Color GetDifficultyColor(GameSettings.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case GameSettings.Difficulty.Easy: return easyColor;
            case GameSettings.Difficulty.Medium: return mediumColor;
            case GameSettings.Difficulty.Hard: return hardColor;
            case GameSettings.Difficulty.Default: return defaultColor;
            default: return mediumColor;
        }
    }
    
    /// <summary>
    /// Force update from external script
    /// </summary>
    public void ForceUpdate()
    {
        UpdateDifficultyDisplay();
    }
}
