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
    
    [Header("Dev Scene Selection Buttons")]
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button crossyButton;
    [SerializeField] private Button chaseButton;
    [SerializeField] private Button yemeButton;
    [SerializeField] private Button foodButton;
    [SerializeField] private Button bossButton;
    
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
        
        // Register dev buttons for direct scene access
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(() => LoadSceneDirectly("TutorialScene"));
            
        if (crossyButton != null)
            crossyButton.onClick.AddListener(() => LoadSceneDirectly("CrossyRoadsScene"));
            
        if (chaseButton != null)
            chaseButton.onClick.AddListener(() => LoadSceneDirectly("NivyChaseScene"));
            
        if (yemeButton != null)
            yemeButton.onClick.AddListener(() => LoadSceneDirectly("YemeMazeScene"));
            
        if (foodButton != null)
            foodButton.onClick.AddListener(() => LoadSceneDirectly("FoodCatcherScene"));
            
        if (bossButton != null)
            bossButton.onClick.AddListener(() => LoadSceneDirectly("FinalBossFightScene"));
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
    
    // ===== DEV SHORTCUTS =====
    
    private void LoadSceneDirectly(string sceneName)
    {
        Debug.Log($"Loading scene directly: {sceneName}");
        
        // Pre dev testing - načíta scénu priamo bez GameManager
        Time.timeScale = 1; // Reset time scale
        SceneManager.LoadScene(sceneName);
    }
}
