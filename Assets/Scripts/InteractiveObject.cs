using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveObject : MonoBehaviour
{
    [SerializeField] private GameObject pickupArrow;

    private SpriteRenderer pickupArrowSpriteRenderer;
    private PlayerController playerController;

    void Start()
    {
        pickupArrowSpriteRenderer = pickupArrow.GetComponent<SpriteRenderer>();
        pickupArrowSpriteRenderer.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("Player entered pickup range of: " + gameObject.name);
            pickupArrowSpriteRenderer.enabled = true;
            playerController.SetNearbyObject(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Player exited pickup range of: " + gameObject.name);
        pickupArrowSpriteRenderer.enabled = false;
    }

    public void pickUpItem(PlayerController playerController)
    {
        Debug.Log("Calling AddItemToInventory");
        playerController.AddItemToInvetory(gameObject);
        Debug.Log("Destroying GameObject: " + gameObject.name);
        Destroy(gameObject);
    }
}
