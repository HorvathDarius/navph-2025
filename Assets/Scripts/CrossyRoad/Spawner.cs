using System;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    [SerializeField] private GameObject objectToSpawn;
    [SerializeField] private int spawnInterval = 2;
    [SerializeField] private bool vehicleIsMovingDown = true;
    private float timer;
    private float minSpeed;
    private float maxSpeed;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnObject();
            timer = 0f;
            spawnInterval = UnityEngine.Random.Range(1, 4);
        }
    }

    private void SpawnObject()
    {
        GameObject spawnedObject = objectToSpawn;
        spawnedObject.GetComponent<CarMovement>().vehicleIsMovingDown = vehicleIsMovingDown;
        minSpeed = spawnedObject.GetComponent<CarMovement>().minSpeed;
        maxSpeed = spawnedObject.GetComponent<CarMovement>().maxSpeed;
        spawnedObject.GetComponent<CarMovement>().speed = UnityEngine.Random.Range(minSpeed, maxSpeed);

        GameObject spawnedObj = Instantiate(spawnedObject, transform.position, Quaternion.identity);

        if (!vehicleIsMovingDown)
        {
            spawnedObj.GetComponent<SpriteRenderer>().sprite = spawnedObj.GetComponent<CarMovement>().vehicleSpriteBack;
        }
        Debug.Log("Sprite:" + spawnedObj.GetComponent<SpriteRenderer>().sprite.name);
    }
}
