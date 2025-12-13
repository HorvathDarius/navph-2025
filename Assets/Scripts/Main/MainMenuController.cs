using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    
    [Header("GameManager Prefab")]
    [SerializeField] private GameObject gameManagerPrefab;
    
    private void Start()
    {
        // Register main menu buttons
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
            
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(OnLeaderboard);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);
    }
    
    private void OnDestroy()
    {
        // Cleanup listeners
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartGame);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(OnLeaderboard);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettings);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExit);
    }
    
    // ===== MAIN MENU CALLBACKS =====
    
    private void OnStartGame()
    {
        Debug.Log("Start clicked - creating GameManager");
        
        // Vytvor GameManager (ak neexistuje)
        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            Instantiate(gameManagerPrefab);
        }
    }
    
    private void OnLeaderboard()
    {
        Debug.Log("Leaderboard clicked");
        // TODO: Implementuj leaderboard
    }
    
    private void OnSettings()
    {
        Debug.Log("Settings clicked");
        // TODO: Implementuj settings
    }
    
    private void OnExit()
    {
        Debug.Log("Exit clicked");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
