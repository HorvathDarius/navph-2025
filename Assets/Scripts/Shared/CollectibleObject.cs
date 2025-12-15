using UnityEngine;

public class CollectibleObject : MonoBehaviour, IInteractable
{
    [SerializeField]
    private GameObject pickupArrow;
    public CollectibleItem itemData;

    private SpriteRenderer pickupArrowSpriteRenderer;

    private void Start()
    {
        if (pickupArrow != null)
        {
            pickupArrowSpriteRenderer = pickupArrow.GetComponent<SpriteRenderer>();
            pickupArrowSpriteRenderer.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("Player entered pickup range of: " + itemData.itemName);
            if (pickupArrowSpriteRenderer != null)
                pickupArrowSpriteRenderer.enabled = true;

            playerController.SetNearbyInteractable(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("Player exited pickup range of: " + itemData.itemName);
            if (pickupArrowSpriteRenderer != null)
                pickupArrowSpriteRenderer.enabled = false;

            playerController.ClearNearbyInteractable();
        }
    }

    public void Interact(PlayerController playerController)
    {
        if (itemData == null) return;

        Debug.Log("Picking up: " + itemData.itemName);
        playerController.AddItemToInvetory(itemData);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.HandleItemPickedUp(itemData);
        }

        Destroy(gameObject);
    }

    public string GetDebugName() => itemData != null ? itemData.itemName : name;
}