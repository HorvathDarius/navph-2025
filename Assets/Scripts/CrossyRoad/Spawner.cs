using UnityEngine;

// Script used to spawn vehicles at set intervals
public class Spawner : MonoBehaviour
{

    [SerializeField] private GameObject objectToSpawn;
    [SerializeField] private int spawnInterval = 2;
    [SerializeField] private bool vehicleIsMovingDown = true;

    private float timer;
    private float minSpeed;
    private float maxSpeed;

    // Spawn vehicles at set intervals
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnObject();
            timer = 0f;
            spawnInterval = UnityEngine.Random.Range(2, 5);
        }
    }

    // Create object and set its parameters
    private void SpawnObject()
    {
        GameObject spawnedObject = objectToSpawn;
        spawnedObject.GetComponent<CarMovement>().vehicleIsMovingDown = vehicleIsMovingDown;
        minSpeed = spawnedObject.GetComponent<CarMovement>().minSpeed;
        maxSpeed = spawnedObject.GetComponent<CarMovement>().maxSpeed;
        spawnedObject.GetComponent<CarMovement>().speed = UnityEngine.Random.Range(minSpeed, maxSpeed);

        // Instantiate the object
        GameObject spawnedObj = Instantiate(spawnedObject, transform.position, Quaternion.identity);

        // If vehicle is moving up, set its back sprite
        if (!vehicleIsMovingDown)
        {
            spawnedObj.GetComponent<SpriteRenderer>().sprite = spawnedObj.GetComponent<CarMovement>().vehicleSpriteBack;
        }
    }
}
