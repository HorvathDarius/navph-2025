using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TextMeshProUGUI winText;

    private int goodFoodCount = 0;
    private int badFoodCount = 0;
    private bool gameOver = false;

    private void Awake()
    {
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

        if (goodFoodCount >= 1)
        {
            gameOver = true;
            ShowWinMessage();
        }
    }

    public void AddBadFood()
    {
        if (gameOver) return;

        badFoodCount++;
        Debug.Log("Bad food caught: " + badFoodCount);
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
            winText.gameObject.SetActive(true);
            winText.text = "12,90 poplosím!";
        }

        Debug.Log("🎉 You caught 10 good foods! Game Over!");
        ClearAllFood();

        Time.timeScale = 0f;
    }
}
