using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class FinalBossFightManager : MonoBehaviour
{
    public static FinalBossFightManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerCombat player;
    [SerializeField] private HomelessBossAI boss;
    [SerializeField] private Transform playerSpawnTop;
    [SerializeField] private Transform bossSpawnBottom;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip fightStartClip;

    [Header("Boss Settings")]
    [SerializeField] private int bossMaxHealth = 100;
    [SerializeField] private float bossRegenAmount = 5f;
    [SerializeField] private float bossRegenInterval = 1.0f;

    [Header("Fight Settings")]
    [SerializeField] private float roundEndDelay = 2.5f;

    private int bossCurrentHealth;
    private bool fightStarted;
    private bool fightEnded;

    /// <summary>HP bossa ako pomer 0–1 (používa HomelessBossAI na retreat trigger)</summary>
    public float BossHpRatio => bossMaxHealth > 0 ? (float)bossCurrentHealth / bossMaxHealth : 0f;

    /// <summary>True ak fight skončil (boss alebo hráč zomrel) – blokuje ďalší damage.</summary>
    public bool IsFightOver => fightEnded;

    // UI Toolkit
    private VisualElement bossHealthBarRoot;
    private VisualElement bossHealthFill;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        if (playerSpawnTop != null)
            player.transform.position = playerSpawnTop.position;

        if (bossSpawnBottom != null)
            boss.transform.position = bossSpawnBottom.position;

        bossCurrentHealth = bossMaxHealth;
        InitBossHealthUI();

        player.InitForBossFight();
        boss.Init(this);

        boss.StartIntro();
    }
    
    private void InitBossHealthUI()
    {
        if (GameManager.Instance == null) return;
        var ui = GameManager.Instance.GameUI;
        if (ui == null) return;

        var root = ui.rootVisualElement;
        bossHealthBarRoot = root.Q<VisualElement>("BossHealthBar");
        if (bossHealthBarRoot != null)
        {
            bossHealthFill = bossHealthBarRoot.Q<VisualElement>("BossHealthFill");
            UpdateBossHealthUI();
        }
    }
    
    public void ShowBossHealthUI(bool show)
    {
        if (bossHealthBarRoot != null)
        {
            bossHealthBarRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void StartCountdownToFight()
    {
        if (sfxSource != null && fightStartClip != null)
            sfxSource.PlayOneShot(fightStartClip);
    }

    public void ApplyDamageToBoss(int amount)
    {
        if (!fightStarted || fightEnded)
        {
            Debug.Log($"[FBFM] ApplyDamageToBoss blocked. fightStarted={fightStarted}, fightEnded={fightEnded}");
            return;
        }

        bossCurrentHealth = Mathf.Clamp(bossCurrentHealth - amount, 0, bossMaxHealth);
        Debug.Log($"[FBFM] Boss took {amount} dmg. HP={bossCurrentHealth}/{bossMaxHealth}");
        UpdateBossHealthUI();

        if (bossCurrentHealth <= 0)
        {
            // Okamžite označ fight ako ukončený – ak boss súčasne trafí hráča,
            // damage sa už neaplikuje a hráč nevyzerá ako mŕtvy.
            fightEnded = true;
            Debug.Log("[FBFM] Boss HP <= 0, starting HandleBossDeath.");
            StartCoroutine(HandleBossDeath());
            return;
        }

        boss.OnHit();
    }

    private void UpdateBossHealthUI()
    {
        if (bossHealthFill == null) return;

        float ratio = (float)bossCurrentHealth / bossMaxHealth;
        bossHealthFill.style.width = new StyleLength(Length.Percent(ratio * 100));
    }

    public IEnumerator StartBossRegeneration()
    {
        // Bezdomovec si vie regenerovať HP, kým ho hráč netrafí.
        while (!fightEnded && boss.IsRegenerating())
        {
            bossCurrentHealth = Mathf.Clamp(bossCurrentHealth + (int)bossRegenAmount, 0, bossMaxHealth);
            UpdateBossHealthUI();
            yield return new WaitForSeconds(bossRegenInterval);
        }
    }

    public void InterruptBossRegeneration()
    {
        boss.StopRegeneration();
    }

    private IEnumerator HandleBossDeath()
    {
        ShowBossHealthUI(false);
        boss.PlayDeath();
        player.LockInput(true);
        player.EnableCombat(false);

        // Skóre za porazenie bezdomovca – 15 bodov.
        if (GameManager.Instance == null) yield break;
        
        GameManager.Instance.AddScore(15);
        yield return new WaitForSeconds(roundEndDelay);

        GameManager.Instance.OnMinigameComplete();
    }

    public IEnumerator HandlePlayerDeath()
    {
        if (fightEnded) yield break;
        fightEnded = true;

        ShowBossHealthUI(false);
        boss.LockAI(true);          // zastaví bossa – nech neútočí na mŕtveho hráča

        yield return new WaitForSeconds(roundEndDelay);
    }
    
    public void NotifyBossReadyToFight()
    {
        Debug.Log("[FBFM] NotifyBossReadyToFight - scheduling fight start.");
        StartCoroutine(StartFightAfterIntro());
    }

    private IEnumerator StartFightAfterIntro()
    {
        yield return new WaitForSeconds(0.5f);
        fightStarted = true;
        Debug.Log("[FBFM] Fight started.");
        boss.StartFightPhase();
        player.EnableCombat(true);
    }
}
