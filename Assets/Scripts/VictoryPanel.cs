using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple helper script for Victory panel
/// Attach this to your Victory Panel
/// </summary>
public class VictoryPanel : MonoBehaviour
{
    [Header("Optional - Auto-find buttons")]
    public Button restartButton;
    public Button mainMenuButton;
    public Button creditsButton;
    
    [Header("Optional - Text elements")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    
    [Header("Credits Settings")]
    [Tooltip("Name of the credits scene to load")]
    public string creditsSceneName = "Credits";
    
    [Tooltip("Only show credits button for these difficulties")]
    public bool onlyShowCreditsForHardAndDefault = false;
    
    private UIManager uiManager;

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
    
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClick);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClick);
        }
        
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(OnCreditsClick);
        }
    }

    void OnEnable()
    {
        // Show cursor and unlock (critical for UI interaction)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Debug: Check if credits button is assigned
        if (creditsButton != null)
        {
            Debug.Log("Credits button found and will be shown");
        }
        else
        {
            Debug.LogWarning("⚠️ Credits button is NOT assigned in VictoryPanel! Please assign it in the Inspector.");
        }
        
        // Update stats when panel shows
        if (GameManager.Instance != null)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Final Score: {GameManager.Instance.currentScore}";
            }
            
            if (timeText != null)
            {
                float time = GameManager.Instance.elapsedTime;
                int minutes = Mathf.FloorToInt(time / 60F);
                int seconds = Mathf.FloorToInt(time % 60F);
                timeText.text = $"Time: {minutes:0}:{seconds:00}";
            }
        }
        
        // Set message
        if (messageText != null)
        {
            messageText.text = "Victory!";
        }
        
        // Show/hide credits button based on difficulty
        UpdateCreditsButtonVisibility();
    }

    public void OnNextLevelClick()
    {
        if (uiManager != null)
        {
            uiManager.ContinueToNextLevel();
        }
        else
        {
            Debug.LogWarning("UIManager not found! Loading default next level.");
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.LoadSceneWithLoading("Map2_AngkorWat");
        }
    }

    public void OnRestartClick()
    {
        if (uiManager != null)
        {
            uiManager.RestartGame();
        }
        else
        {
            Time.timeScale = 1f;
            SceneTransitionManager.Instance.LoadSceneWithLoading(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    public void OnMainMenuClick()
    {
        Debug.Log("VictoryPanel: Going to main menu...");
        
        // Ensure cursor is visible and unlocked before transitioning
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        
        if (uiManager != null)
        {
            uiManager.BackToMainMenu();
        }
        else
        {
            SceneTransitionManager.Instance.LoadSceneDirect("MenuScence");
        }
    }
    
    public void OnCreditsClick()
    {
        Debug.Log("VictoryPanel: Loading credits scene...");
        
        // Ensure cursor is visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        
        // Load credits scene directly without loading screen
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneDirect(creditsSceneName);
        }
        else
        {
            // Fallback if no transition manager
            UnityEngine.SceneManagement.SceneManager.LoadScene(creditsSceneName);
        }
    }
    
    /// <summary>
    /// Show or hide the credits button based on current difficulty
    /// </summary>
    private void UpdateCreditsButtonVisibility()
    {
        if (creditsButton == null)
        {
            Debug.LogWarning("⚠️ Credits button is NULL! Please assign it in the VictoryPanel Inspector.");
            return;
        }
        
        // If not restricting by difficulty, always show
        if (!onlyShowCreditsForHardAndDefault)
        {
            creditsButton.gameObject.SetActive(true);
            Debug.Log("✅ Credits button shown for all difficulties");
            return;
        }
        
        // Check current difficulty from GameSettings
        if (GameSettings.Instance != null)
        {
            GameSettings.Difficulty currentDifficulty = GameSettings.Instance.currentDifficulty;
            
            // Only show for Hard and Default difficulties
            bool shouldShow = (currentDifficulty == GameSettings.Difficulty.Hard || 
                              currentDifficulty == GameSettings.Difficulty.Default);
            
            creditsButton.gameObject.SetActive(shouldShow);
            
            if (shouldShow)
            {
                Debug.Log($"✅ Credits button enabled for {currentDifficulty} difficulty");
            }
            else
            {
                Debug.Log($"🚫 Credits button hidden for {currentDifficulty} difficulty");
            }
        }
        else
        {
            // If no GameSettings instance, show the button by default
            creditsButton.gameObject.SetActive(true);
            Debug.LogWarning("GameSettings not found, showing credits button by default");
        }
    }
}
