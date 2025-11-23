using UnityEngine;
using TMPro;

public class FoodCatcherManager : MonoBehaviour
{
    public static FoodCatcherManager Instance;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private GameObject bubble;

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

    public void AddGoodFood()
    {
        if (gameOver) return;

        goodFoodCount++;
        Debug.Log("Good food caught: " + goodFoodCount);
        GameManager.Instance.AddScore(10);

        if (goodFoodCount > 10)
        {
            // gameOver = true;
            // ShowWinMessage();
            GameManager.Instance.ChangeHealth(-2);
        }

        // if (goodFoodCount >= 10)
        // {
        //     gameOver = true;
        //     ShowWinMessage();
        // }
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

    private void ShowWinMessage()
    {
        if (winText != null)
        {
            bubble.gameObject.SetActive(true);
            winText.gameObject.SetActive(true);
            winText.text = "12,90 poplosím!";
        }

        Debug.Log("🎉 You caught 10 good foods! Game Over!");
        ClearAllFood();

        Time.timeScale = 0f;
    }
}
