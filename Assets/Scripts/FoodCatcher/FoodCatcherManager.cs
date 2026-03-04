using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FoodCatcherManager : MonoBehaviour
{
    public static FoodCatcherManager Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private GameObject bubble;
    [SerializeField] private GameObject foodSpawner;
    [SerializeField] private TextMeshProUGUI foodCounter;

    [Header("Price Audio")]
    [SerializeField] private AudioSource priceAudioSource;
    [SerializeField] private AudioClip priceAudioClip;

    private int goodFoodCount = 0;
    private int badFoodCount = 0;
    private bool gameOver = false;

    private void Awake()
    {
        bubble.gameObject.SetActive(false);
        winText.gameObject.SetActive(false);

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // FoodCatcher has no death animation - game over should be instant
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameOverDelay(0f);
    }

    public void AddGoodFood()
    {
        if (gameOver) return;

        goodFoodCount++;
        foodCounter.text = string.Format("{0} / 10", goodFoodCount.ToString());
        Debug.Log("Good food caught: " + goodFoodCount);
        GameManager.Instance.AddScore(10);

        if (goodFoodCount > 10)
        {
            GameManager.Instance.ChangeHealth(-2);
        }

        if (goodFoodCount >= 10)
        {
            gameOver = true;
            StartCoroutine(ShowWinMessage());
        }
    }

    public void AddBadFood()
    {
        if (gameOver) return;

        badFoodCount++;
        GameManager.Instance.ChangeHealth(-5);
        Debug.Log("Bad food caught: " + badFoodCount);
        Debug.Log("Health: " + GameManager.Instance.Health);
    }

    private void ClearAllFood()
    {
        FoodFall[] allFood = FindObjectsByType<FoodFall>(FindObjectsSortMode.None);
        foreach (var food in allFood)
        {
            Destroy(food.gameObject);
        }
    }

    private IEnumerator ShowWinMessage()
    {
        if (winText != null)
        {
            bubble.gameObject.SetActive(true);
            winText.gameObject.SetActive(true);
            winText.text = "12,90 poplosím!";
        }

        // Play price audio
        if (priceAudioSource != null && priceAudioClip != null)
        {
            priceAudioSource.PlayOneShot(priceAudioClip);
        }

        Debug.Log("🎉 You caught 10 good foods! Game Over!");
        ClearAllFood();
        foodSpawner.SetActive(false);

        yield return new WaitForSeconds(2.5f);
        
        GameManager.Instance.OnMinigameComplete();
    }
}
