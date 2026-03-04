using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class YemeMazeManager : MonoBehaviour
{
    public static YemeMazeManager Instance { get; private set; }

    [Header("Supermarket Shelves")]
    public List<Transform> shelfTransforms;   // Manuálne referencie v Inspector
    public GameObject drinkPrefab;
    public GameObject drinkPrefabVoid;
    public float spawnDrinkDelay = 10f;       // čas do spawnu

    private Transform spawnedShelf;
    private GameObject spawnedDrink;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(SpawnDrinkRoutine());
    }

    IEnumerator SpawnDrinkRoutine()
    {
        yield return new WaitForSeconds(spawnDrinkDelay);

        spawnedShelf = shelfTransforms[Random.Range(0, shelfTransforms.Count)];
        spawnedDrink = Instantiate(drinkPrefab, spawnedShelf.position + new Vector3(1.5f, 1.51f, 0), Quaternion.identity);
        Instantiate(drinkPrefabVoid, spawnedShelf.position + new Vector3(1.5f, 1.51f, 0), Quaternion.identity);
    }

    public void TryHandleCheckout(PlayerController player)
    {
        if (player.inventoryItems.Exists(x => x.itemName == "YemeDrink"))
        {
            GameManager.Instance.AddScore(100);
            GameManager.Instance.OnMinigameComplete();
        }
        else
        {
            // TODO: Nenašiel som este item, neúspešná platba,zobraziť správu v UI
        }
    }

    public void OnEscapeStore()
    {
        Debug.Log("Player escaped from store without payment");
        // Skóre penalizácia, ale peniaze ti ostanú
        GameManager.Instance.AddScore(-50);
        GameManager.Instance.OnMinigameComplete();
    }
}
