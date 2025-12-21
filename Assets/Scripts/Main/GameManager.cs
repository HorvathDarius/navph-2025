using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Button = UnityEngine.UIElements.Button;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private InputAction pauseAction;
    [SerializeField] private InputAction continueAction; // For Space key

    [Header("Game Settings")]
    public float gameTimeLimit = 900f; // 15 minút
    public int startingHealth = 100;

    [Header("UI Prefab")]
    [SerializeField] private GameObject gameUIPrefab;

    [Header("Lore & Hint Prefabs")]
    [SerializeField] private GameObject[] lorePrefabs;
    [SerializeField] private GameObject[] hintPrefabs;
    [SerializeField] private GameObject endingPrefab;
    private GameObject CurrentEndingPrefab;

    // UI References
    private UIDocument m_UIDocument;
    private Label m_HealthLabel;
    private VisualElement m_HealthBarFill;
    private Label m_ScoreLabel;
    private Label m_TimerLabel;
    private VisualElement m_ItemSelector;
    private VisualElement m_HUD;
    private bool m_WaitingForEndingContinue = false;

    // Game Over stats
    private Label m_FinalScoreLabel;
    private Label m_LevelReachedLabel;
    private Button m_RetryButton;
    private Button m_MainMenuButtonGO;

    // Victory stats
    private Label m_VictoryScoreLabel;
    private Label m_RemainingTimeLabel;
    private Label m_RemainingHealthLabel;
    private Button m_MainMenuButtonVictory;

    // Pause panel buttons
    private Button m_ResumeButton;
    private Button m_MainMenuButtonPause;
    
    // UI Panels
    private VisualElement m_GameOverPanel;
    private VisualElement m_VictoryPanel;
    private VisualElement m_PausePanel;
    private VisualElement m_TransitionPanel;

    [Header("Game State")]
    private int m_CurrentLevel = 0; // 0=Tutorial, 1-5=Minigames
    private int m_Health = 100;
    private int m_Score = 0;
    private float m_TimeRemaining;
    private bool m_IsGameActive = false;
    private bool m_IsPaused = false;

    // Transition State
    private GameObject m_CurrentLorePrefab;
    private GameObject m_CurrentHintPrefab;
    private bool m_ShowingLore = false;
    private bool m_ShowingHint = false;
    private bool m_WaitingForContinue = false;
    private int m_TransitionFromLevel = -1;

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

    void OnEnable()
    {
        pauseAction.Enable();
        continueAction.Enable();
    }

    void OnDisable()
    {
        pauseAction.Disable();
        continueAction.Disable();
    }

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
        continueAction = InputSystem.actions.FindAction("Continue");
        if (continueAction == null)
        {
            continueAction = new InputAction("Continue", binding: "<Keyboard>/space");
            continueAction.Enable();
        }

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

        // Handle Space key for lore/hint transitions
        if (m_WaitingForContinue)
        {
            if (continueAction != null && continueAction.WasPressedThisFrame())
            {
                Debug.Log("SPACE KEY DETECTED in Update!");
                HandleTransitionContinue();
            }
        }

        if (m_WaitingForEndingContinue)
        {
            if (continueAction != null && continueAction.WasPressedThisFrame())
            {
                HandleEndingContinue();
            }
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
        Debug.Log($"=== LoadLevel({levelIndex}) CALLED ===");

        if (levelIndex < 0 || levelIndex >= SCENE_NAMES.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        m_CurrentLevel = levelIndex;
        string sceneName = SCENE_NAMES[levelIndex];

        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
        Debug.Log($"SceneManager.LoadScene({sceneName}) completed");
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
            // Load next level with lore/hint transition
            StartCoroutine(TransitionToNextLevel());
        }
    }

    private IEnumerator TransitionToNextLevel()
    {
        int completedLevel = m_CurrentLevel;
        int nextLevel = m_CurrentLevel + 1;

        Debug.Log($"=== TRANSITION COROUTINE STARTED: Completed level {completedLevel} -> Next level {nextLevel} ===");
        m_TransitionFromLevel = completedLevel;
        ShowLorePrefab(completedLevel);
        m_WaitingForContinue = true;
        Debug.Log($"Waiting for continue... m_WaitingForContinue = {m_WaitingForContinue}");

        yield return new WaitUntil(() =>
        {
            bool shouldContinue = !m_WaitingForContinue;
            if (shouldContinue)
            {
                Debug.Log("WaitUntil condition met - continuing coroutine!");
            }
            return shouldContinue;
        });

        Debug.Log($"=== COROUTINE CONTINUING - Cleaning up prefabs before loading level {nextLevel} ===");
        
        Debug.Log($"Loading level {nextLevel}");
        LoadLevel(nextLevel);
        m_TransitionFromLevel = -1;
        
        yield return new WaitForSeconds(1f);
        HideTransitionPrefabs();
        yield return null;

        Debug.Log($"LoadLevel({nextLevel}) called, waiting 0.5s");
        yield return new WaitForSeconds(0.5f);
        Debug.Log("=== TRANSITION COROUTINE COMPLETE ===");
    }
    private void ShowLorePrefab(int levelIndex)
    {
        if (lorePrefabs == null || levelIndex >= lorePrefabs.Length || lorePrefabs[levelIndex] == null)
        {
            Debug.LogWarning($"No lore prefab for level {levelIndex}");
            m_WaitingForContinue = false;
            return;
        }

        if (m_CurrentLorePrefab != null) Destroy(m_CurrentLorePrefab);

        m_CurrentLorePrefab = Instantiate(lorePrefabs[levelIndex]);

        Canvas canvas = m_CurrentLorePrefab.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            canvas = m_CurrentLorePrefab.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        m_ShowingLore = true;
        m_ShowingHint = false;
        m_WaitingForContinue = true;

        DontDestroyOnLoad(m_CurrentLorePrefab);
    }

    private void ShowHintPrefab(int levelIndex)
    {
        if (hintPrefabs == null || levelIndex >= hintPrefabs.Length || hintPrefabs[levelIndex] == null)
        {
            Debug.LogWarning($"No hint prefab found for level {levelIndex} - skipping to next level");
            m_WaitingForContinue = false;
            return;
        }

        if (m_CurrentHintPrefab != null)
        {
            Destroy(m_CurrentHintPrefab);
            m_CurrentHintPrefab = null;
        }

        if (m_CurrentLorePrefab != null)
        {
            Destroy(m_CurrentLorePrefab);
            m_CurrentLorePrefab = null;
        }

        m_CurrentHintPrefab = Instantiate(hintPrefabs[levelIndex]);

        Canvas canvas = m_CurrentHintPrefab.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            canvas = m_CurrentHintPrefab.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        
        m_ShowingLore = false;
        m_ShowingHint = true;

        DontDestroyOnLoad(m_CurrentHintPrefab);

        Debug.Log($"Hint prefab displayed: {m_CurrentHintPrefab.name}");
    }


    private void HandleTransitionContinue()
    {
        Debug.Log($"HandleTransitionContinue called - ShowingLore: {m_ShowingLore}, ShowingHint: {m_ShowingHint}, TransitionFromLevel: {m_TransitionFromLevel}");

        if (m_ShowingLore)
        {
            Debug.Log($"Space pressed - switching from lore to hint for level {m_TransitionFromLevel}");
            ShowHintPrefab(m_TransitionFromLevel);
        }
        else if (m_ShowingHint)
        {
            Debug.Log("Space pressed on hint - preparing to load next level");
            Debug.Log($"Before: m_WaitingForContinue = {m_WaitingForContinue}");

            m_WaitingForContinue = false;

            Debug.Log($"After: m_WaitingForContinue = {m_WaitingForContinue}");
        }
        else
        {
            Debug.LogWarning("HandleTransitionContinue called but neither lore nor hint is showing!");
        }
    }

    private void HideTransitionPrefabs()
    {
        Debug.Log("HideTransitionPrefabs called");

        if (m_CurrentLorePrefab != null)
        {
            Debug.Log($"Destroying lore prefab: {m_CurrentLorePrefab.name}");
            Destroy(m_CurrentLorePrefab);
            m_CurrentLorePrefab = null;
        }

        if (m_CurrentHintPrefab != null)
        {
            Debug.Log($"Destroying hint prefab: {m_CurrentHintPrefab.name}");
            Destroy(m_CurrentHintPrefab);
            m_CurrentHintPrefab = null;
        }

        m_ShowingLore = false;
        m_ShowingHint = false;

        Debug.Log("All transition prefabs cleaned up");
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

        HideTransitionPrefabs();

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

    public IEnumerator GameOver(string reason, float delaySeconds)
    {
        if (!m_IsGameActive)
            yield break;

        m_IsGameActive = false;

        yield return new WaitForSeconds(delaySeconds);

        Time.timeScale = 0;
        if (m_GameOverPanel != null)
            m_GameOverPanel.style.display = DisplayStyle.Flex;

        if (m_FinalScoreLabel != null)
            m_FinalScoreLabel.text = $"Finálne skóre: {m_Score}";

        if (m_LevelReachedLabel != null)
            m_LevelReachedLabel.text = $"Dosiahnutý level: {m_CurrentLevel + 1}";
        
        Debug.Log($"Game Over (delayed): {reason}");
    }

    private void ShowVictoryPanel()
    {
        if (m_VictoryPanel != null)
            m_VictoryPanel.style.display = DisplayStyle.Flex;

        if (m_VictoryScoreLabel != null)
            m_VictoryScoreLabel.text = $"Finálne skóre: {m_Score}";

        if (m_RemainingTimeLabel != null)
            m_RemainingTimeLabel.text = $"Zostávajúci čas: {FormatTime(m_TimeRemaining)}";

        if (m_RemainingHealthLabel != null)
            m_RemainingHealthLabel.text = $"Zostávajúce zdravie: {m_Health} HP";
    }


    private void HandleEndingContinue()
    {
        if (CurrentEndingPrefab != null)
        {
            Destroy(CurrentEndingPrefab);
            CurrentEndingPrefab = null;
        }

        m_WaitingForEndingContinue = false;

        ShowVictoryPanel();
    }

    private void Victory()
    {
        m_IsGameActive = false;
        Time.timeScale = 0;

        // Show ending prefab first
        if (endingPrefab != null)
        {
            CurrentEndingPrefab = Instantiate(endingPrefab);

            // Ensure Canvas overlay
            Canvas canvas = CurrentEndingPrefab.GetComponentInChildren<Canvas>();
            if (canvas == null)
                canvas = CurrentEndingPrefab.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            DontDestroyOnLoad(CurrentEndingPrefab);

            m_WaitingForEndingContinue = true; // wait for space
            Debug.Log("Ending prefab displayed, waiting for Space...");
        }
        else
        {
            ShowVictoryPanel();
        }
    }



    private void TimeUp()
    {
        StartCoroutine(GameOver(
            "Vypršal ti čas na obedovú pauzu, dnes robíš o hodinu dlhšie!",
            0f));
    }

    // ===== PLAYER STATE =====

    public void ChangeHealth(int amount)
    {
        m_Health = Mathf.Clamp(m_Health + amount, 0, startingHealth);
        UpdateHealthUI();

        if (m_Health <= 0 && m_IsGameActive)
        {
            // Spusti delayed game over – animácia pádu hráča
            StartCoroutine(GameOver("Zomrel si!", 3f));
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

        m_VictoryPanel = root.Q<VisualElement>("VictoryPanel");

        m_PausePanel = root.Q<VisualElement>("PausePanel");

        m_TransitionPanel = root.Q<VisualElement>("TransitionPanel");
        
        // Game Over panel
        m_GameOverPanel = root.Q<VisualElement>("GameOverPanel");
        m_FinalScoreLabel = m_GameOverPanel?.Q<Label>("FinalScoreLabel");
        m_LevelReachedLabel = m_GameOverPanel?.Q<Label>("LevelReachedLabel");
        m_RetryButton = m_GameOverPanel?.Q<Button>("RetryButton");
        m_MainMenuButtonGO = m_GameOverPanel?.Q<Button>("MainMenuButtonGO");

        // Victory panel
        m_VictoryPanel = root.Q<VisualElement>("VictoryPanel");
        m_VictoryScoreLabel = m_VictoryPanel?.Q<Label>("VictoryScoreLabel");
        m_RemainingTimeLabel = m_VictoryPanel?.Q<Label>("RemainingTimeLabel");
        m_RemainingHealthLabel = m_VictoryPanel?.Q<Label>("RemainingHealthLabel");
        m_MainMenuButtonVictory = m_VictoryPanel?.Q<Button>("MainMenuButtonVictory");
        
        m_ResumeButton       = m_PausePanel?.Q<Button>("ResumeButton");
        m_MainMenuButtonPause = m_PausePanel?.Q<Button>("MainMenuButton");
        
        if (m_RetryButton != null)
            m_RetryButton.clicked += OnRetryButtonClicked;

        if (m_MainMenuButtonGO != null)
            m_MainMenuButtonGO.clicked += ReturnToMainMenu;

        if (m_MainMenuButtonVictory != null)
            m_MainMenuButtonVictory.clicked += ReturnToMainMenu;
        
        if (m_ResumeButton != null)
            m_ResumeButton.clicked += TogglePause;

        if (m_MainMenuButtonPause != null)
            m_MainMenuButtonPause.clicked += ReturnToMainMenu;
        
        HideAllPanels();
    }
    
    private void OnRetryButtonClicked()
    {
        m_Health = startingHealth;
        UpdateHealthUI();

        m_IsGameActive = true;
        Time.timeScale = 1f;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);

        if (m_GameOverPanel != null)
            m_GameOverPanel.style.display = DisplayStyle.None;
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

    // ===== GETTERS =====

    public int CurrentLevel => m_CurrentLevel;
    public int Health => m_Health;
    public int Score => m_Score;
    public float TimeRemaining => m_TimeRemaining;
    public bool IsGameActive => m_IsGameActive;
    public bool IsPaused => m_IsPaused;
    public UIDocument GameUI => m_UIDocument;
}