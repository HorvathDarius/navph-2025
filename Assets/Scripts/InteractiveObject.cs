using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveObject : MonoBehaviour
{
    [SerializeField] private GameObject pickupArrow;

    private SpriteRenderer pickupArrowSpriteRenderer;
    private PlayerController playerController;
    public Item itemData;

    // Initialize pickup arrow
    void Start()
    {
        pickupArrowSpriteRenderer = pickupArrow.GetComponent<SpriteRenderer>();
        pickupArrowSpriteRenderer.enabled = false;
    }

    // When player close, display arrow and set nearby object
    private void OnTriggerEnter2D(Collider2D other)
    {
        playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("Player entered pickup range of: " + itemData.itemName);
            pickupArrowSpriteRenderer.enabled = true;
            playerController.SetNearbyObject(this);
        }
    }

    // When player leaves, hide arrow and clear nearby object
    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Player exited pickup range of: " + itemData.itemName);
        pickupArrowSpriteRenderer.enabled = false;
    }

    // Handle item pickup
    public void pickUpItem(PlayerController playerController)
    {
        Debug.Log("Calling AddItemToInventory");
        playerController.AddItemToInvetory(itemData);
        Debug.Log("Destroying GameObject: " + itemData.itemName);
        Destroy(gameObject);
    }
}
