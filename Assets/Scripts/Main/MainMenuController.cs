using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    
    [Header("GameManager Prefab")]
    [SerializeField] private GameObject gameManagerPrefab;
    
    [Header("UI Bubbles")]
    [SerializeField] private GameObject leaderboardBubble;
    [SerializeField] private GameObject settingsBubble;
    
    [Header("Bubble Settings")]
    [SerializeField] private float bubbleDisplayTime = 3f;
    
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
            
        HideAllBubbles();
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
    
    private void HideAllBubbles()
    {
        if (leaderboardBubble != null)
            leaderboardBubble.SetActive(false);
        if (settingsBubble != null)
            settingsBubble.SetActive(false);
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
        ShowBubble(leaderboardBubble);
    }
    
    private void OnSettings()
    {
        Debug.Log("Settings clicked");
        ShowBubble(settingsBubble);
    }
    
    private void ShowBubble(GameObject bubble)
    {
        if (bubble == null) return;
        
        HideAllBubbles();
        
        bubble.SetActive(true);
        
        StartCoroutine(HideBubbleAfterTime(bubble, bubbleDisplayTime));
    }
    
    private IEnumerator HideBubbleAfterTime(GameObject bubble, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bubble != null)
            bubble.SetActive(false);
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
