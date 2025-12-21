using UnityEngine;
using System.Collections;

public class FoodSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] goodFoodPrefabs;
    [SerializeField] private GameObject[] badFoodPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRangeX = 8f;
    [SerializeField] private float spawnY = 6f;
    [SerializeField, Range(0f, 1f)] private float badFoodChance = 0.85f;
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
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }

    private void SpawnFood()
    {
        Vector2 spawnPosition = new Vector2(
            Random.Range(-spawnRangeX, spawnRangeX),
            spawnY
        );

        bool spawnBadFood = Random.value < badFoodChance;

        GameObject prefabToSpawn = spawnBadFood
            ? GetRandomPrefab(badFoodPrefabs)
            : GetRandomPrefab(goodFoodPrefabs);

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }
    }

    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        return prefabs[Random.Range(0, prefabs.Length)];
    }
}
