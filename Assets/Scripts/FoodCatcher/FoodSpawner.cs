using UnityEngine;
using System.Collections;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goodFoodPrefab;
    [SerializeField] private GameObject badFoodPrefab;
    [SerializeField] private float spawnRangeX = 8f;
    [SerializeField] private float spawnY = 6f;
    [SerializeField, Range(0f, 1f)] private float badFoodChance = 0.95f;
    [SerializeField] private float minDelay = 0.05f;
    [SerializeField] private float maxDelay = 0.2f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnFood();
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private void SpawnFood()
    {
        Vector2 spawnPosition = new Vector2(
            Random.Range(-spawnRangeX, spawnRangeX),
            spawnY
        );

        GameObject prefabToSpawn = (Random.value < badFoodChance)
            ? badFoodPrefab
            : goodFoodPrefab;

        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }
}
