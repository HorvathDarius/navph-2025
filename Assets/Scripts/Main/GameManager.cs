using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private InputAction pauseAction;

    [Header("Game Settings")]
    public float gameTimeLimit = 900f; // 15 minút
    public int startingHealth = 100;

    [Header("UI Prefab")]
    [SerializeField] private GameObject gameUIPrefab;

    // UI References
    private UIDocument m_UIDocument;
    private Label m_HealthLabel;
    private VisualElement m_HealthBarFill;
    private Label m_ScoreLabel;
    private Label m_TimerLabel;
    private VisualElement m_ItemSelector;
    private VisualElement m_HUD;

    // UI Panels
    private VisualElement m_GameOverPanel;
    private Label m_GameOverMessage;
    private VisualElement m_VictoryPanel;
    private Label m_VictoryMessage;
    private VisualElement m_PausePanel;
    private VisualElement m_TransitionPanel;
    private Label m_TransitionMessage;

    [Header("Game State")]
    private int m_CurrentLevel = 0; // 0=Tutorial, 1-5=Minigames
    private int m_Health = 100;
    private int m_Score = 0;
    private float m_TimeRemaining;
    private bool m_IsGameActive = false;
    private bool m_IsPaused = false;

    [Header("Collected Items")]
    private System.Collections.Generic.List<string> m_CollectedItems = new();

    // Scene names podľa flowchart
    private readonly string[] SCENE_NAMES = 
    {
        "TutorialScene",           // Level 0
        "CrossyRoadsScene",        // Level 1
        "NivyChaseScene",          // Level 2
        "YemeMazeScene",           // Level 3
        "FoodCatcherScene",        // Level 4
        "FinalBossFightScene"      // Level 5
    };

    private void Awake()
    {
        // Singleton - pri druhom vytvorení GameManagera zruš starý
        if (Instance != null && Instance != this)
        {
            Debug.Log("Destroying old GameManager instance");
            Destroy(Instance.gameObject);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("GameManager created");
    }

    private void Start()
    {
        pauseAction = InputSystem.actions.FindAction("Pause");

        // Vytvor UI
        CreateGameUI();

        // Spusti hru
        StartNewGame();
    }

    void Update()
    {
        // Timer update
        if (m_IsGameActive && !m_IsPaused)
        {
            m_TimeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (m_TimeRemaining <= 0)
            {
                TimeUp();
            }
        }

        // Pause handling
        if (pauseAction != null && pauseAction.WasPressedThisFrame() && m_IsGameActive)
        {
            TogglePause();
        }
    }

    // ===== GAME FLOW =====

    private void StartNewGame()
    {
        Debug.Log("Starting new game");

        // Reset state
        m_CurrentLevel = 0;
        m_Health = startingHealth;
        m_Score = 0;
        m_TimeRemaining = gameTimeLimit;
        m_IsGameActive = true;
        m_IsPaused = false;
        m_CollectedItems.Clear();

        Time.timeScale = 1;

        // Update UI
        if (m_UIDocument != null)
        {
            ShowHUD();
            UpdateAllUI();
        }

        // Load first level
        LoadLevel(0);
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= SCENE_NAMES.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        m_CurrentLevel = levelIndex;
        SceneManager.LoadScene(SCENE_NAMES[levelIndex]);
    }

    public void OnMinigameComplete()
    {
        Debug.Log($"Minigame {m_CurrentLevel} completed!");

        // Check if all levels completed
        if (m_CurrentLevel >= SCENE_NAMES.Length - 1)
        {
            Victory();
        }
        else
        {
            // Load next level with transition
            StartCoroutine(TransitionToNextLevel());
        }
    }

    private IEnumerator TransitionToNextLevel()
    {
        ShowTransitionPanel(m_CurrentLevel + 1);
        yield return new WaitForSeconds(2f);
        LoadLevel(m_CurrentLevel + 1);
        yield return new WaitForSeconds(0.5f);
        HideTransitionPanel();
    }

    // ===== PAUSE & MENU =====

    public void TogglePause()
    {
        m_IsPaused = !m_IsPaused;
        Time.timeScale = m_IsPaused ? 0 : 1;

        if (m_PausePanel != null)
        {
            m_PausePanel.style.display = m_IsPaused ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu - destroying GameManager");

        Time.timeScale = 1;
        m_IsGameActive = false;

        // Vyčisti UI
        if (m_UIDocument != null && m_UIDocument.gameObject != null)
        {
            Destroy(m_UIDocument.gameObject);
        }

        // Znič GameManager
        Instance = null;

        // Načítaj menu
        SceneManager.LoadScene("MainMenuScene");

        Destroy(gameObject);
    }

    // ===== GAME OVER & VICTORY =====

    private void GameOver(string reason)
    {
        m_IsGameActive = false;
        Time.timeScale = 0;

        if (m_GameOverPanel != null)
        {
            m_GameOverPanel.style.display = DisplayStyle.Flex;
        }

        if (m_GameOverMessage != null)
        {
            m_GameOverMessage.text = $"GAME OVER\n\n{reason}\n\nSkóre: {m_Score}\nDosiahnutý level: {m_CurrentLevel + 1}";
        }

        Debug.Log($"Game Over: {reason}");
    }

    private void Victory()
    {
        m_IsGameActive = false;
        Time.timeScale = 0;

        if (m_VictoryPanel != null)
        {
            m_VictoryPanel.style.display = DisplayStyle.Flex;
        }

        if (m_VictoryMessage != null)
        {
            m_VictoryMessage.text = $"VÍŤAZSTVO!\n\nPodarilo sa ti prežiť obedovú pauzu!\n\nFinálne skóre: {m_Score}\nZostávajúci čas: {FormatTime(m_TimeRemaining)}\nZostávajúce zdravie: {m_Health} HP";
        }

        Debug.Log("Victory!");
    }

    private void TimeUp()
    {
        GameOver("Vypršal ti čas na obedovú pauzu, dnes robíš o hodinu dlhšie!");
    }

    // ===== PLAYER STATE =====

    public void ChangeHealth(int amount)
    {
        m_Health = Mathf.Clamp(m_Health + amount, 0, startingHealth);
        UpdateHealthUI();

        if (m_Health <= 0)
        {
            GameOver("Zomrel si!");
        }
    }

    public void AddScore(int amount)
    {
        var newScore = m_Score + amount;
        if (newScore < 0)
        {
            newScore = 0;
        }
        m_Score = newScore;
        UpdateScoreUI();
    }

    public void CollectItem(string itemName)
    {
        if (!m_CollectedItems.Contains(itemName))
        {
            m_CollectedItems.Add(itemName);
            Debug.Log($"Collected item: {itemName}");
            UpdateItemSelector();
        }
    }

    public bool HasItem(string itemName) => m_CollectedItems.Contains(itemName);

    // ===== UI =====

    private void CreateGameUI()
    {
        if (gameUIPrefab == null)
        {
            Debug.LogWarning("GameUI prefab not assigned - running without HUD");
            return;
        }

        GameObject uiObj = Instantiate(gameUIPrefab);
        uiObj.name = "GameUI";
        DontDestroyOnLoad(uiObj);

        m_UIDocument = uiObj.GetComponent<UIDocument>();

        if (m_UIDocument != null)
        {
            InitializeUIReferences();
            Debug.Log("GameUI created successfully");
        }
    }

    private void InitializeUIReferences()
    {
        var root = m_UIDocument.rootVisualElement;

        // HUD Elements
        m_HUD = root.Q<VisualElement>("HUD");
        m_HealthLabel = root.Q<Label>("HealthLabel");
        m_HealthBarFill = root.Q<VisualElement>("HealthBarFill");
        m_ScoreLabel = root.Q<Label>("ScoreLabel");
        m_TimerLabel = root.Q<Label>("TimerLabel");
        m_ItemSelector = root.Q<VisualElement>("ItemSelector");

        // Panels
        m_GameOverPanel = root.Q<VisualElement>("GameOverPanel");
        m_GameOverMessage = m_GameOverPanel?.Q<Label>("GameOverMessage");

        m_VictoryPanel = root.Q<VisualElement>("VictoryPanel");
        m_VictoryMessage = m_VictoryPanel?.Q<Label>("VictoryMessage");

        m_PausePanel = root.Q<VisualElement>("PausePanel");

        m_TransitionPanel = root.Q<VisualElement>("TransitionPanel");
        m_TransitionMessage = m_TransitionPanel?.Q<Label>("TransitionMessage");

        HideAllPanels();
    }

    private void HideAllPanels()
    {
        if (m_GameOverPanel != null) m_GameOverPanel.style.display = DisplayStyle.None;
        if (m_VictoryPanel != null) m_VictoryPanel.style.display = DisplayStyle.None;
        if (m_PausePanel != null) m_PausePanel.style.display = DisplayStyle.None;
        if (m_TransitionPanel != null) m_TransitionPanel.style.display = DisplayStyle.None;
    }

    private void ShowHUD()
    {
        if (m_HUD != null) m_HUD.style.display = DisplayStyle.Flex;
    }

    private void HideHUD()
    {
        if (m_HUD != null) m_HUD.style.display = DisplayStyle.None;
    }

    private void ShowTransitionPanel(int nextLevel)
    {
        if (m_TransitionPanel != null)
        {
            m_TransitionPanel.style.display = DisplayStyle.Flex;
            if (m_TransitionMessage != null)
            {
                m_TransitionMessage.text = GetTransitionMessage(nextLevel);
            }
        }
    }

    private void HideTransitionPanel()
    {
        if (m_TransitionPanel != null)
        {
            m_TransitionPanel.style.display = DisplayStyle.None;
        }
    }

    // ===== UI UPDATES =====

    private void UpdateAllUI()
    {
        UpdateHealthUI();
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateItemSelector();
    }

    private void UpdateHealthUI()
    {
        if (m_HealthLabel != null)
        {
            m_HealthLabel.text = $"{m_Health}/{startingHealth}";
        }

        if (m_HealthBarFill != null)
        {
            float healthRatio = (float)m_Health / startingHealth;
            m_HealthBarFill.style.width = new StyleLength(Length.Percent(healthRatio * 100));

            if (healthRatio > 0.5f)
            {
                m_HealthBarFill.style.backgroundColor = new StyleColor(Color.green);
            }
            else if (healthRatio > 0.25f)
            {
                m_HealthBarFill.style.backgroundColor = new StyleColor(Color.yellow);
            }
            else
            {
                m_HealthBarFill.style.backgroundColor = new StyleColor(Color.red);
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (m_ScoreLabel != null)
        {
            m_ScoreLabel.text = $"{m_Score}";
        }
    }

    private void UpdateTimerUI()
    {
        if (m_TimerLabel != null)
        {
            m_TimerLabel.text = $"{FormatTime(m_TimeRemaining)}";
        }
    }

    private void UpdateItemSelector()
    {
        // TODO: Implement item selector visual update
        if (m_ItemSelector != null)
        {
            // Display collected items in UI
        }
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes:00}:{secs:00}";
    }

    private string GetTransitionMessage(int nextLevel)
    {
        switch (nextLevel)
        {
            case 1: return "Poďme cez cestu...";
            case 2: return "Pozor, niekto ťa naháňa!";
            case 3: return "Bludisko v tme...";
            case 4: return "Čas na jedlo!";
            case 5: return "Finálny súboj!";
            default: return "Ďalšia úloha...";
        }
    }

    // ===== GETTERS =====

    public int CurrentLevel => m_CurrentLevel;
    public int Health => m_Health;
    public int Score => m_Score;
    public float TimeRemaining => m_TimeRemaining;
    public bool IsGameActive => m_IsGameActive;
    public bool IsPaused => m_IsPaused;
    public UIDocument GameUI => m_UIDocument;
}
