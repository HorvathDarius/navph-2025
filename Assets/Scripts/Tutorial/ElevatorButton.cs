using UnityEngine;

public class ElevatorButton : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactArrow;
    [SerializeField] private ElevatorDoorController elevator;

    private SpriteRenderer arrowRenderer;

    private void Start()
    {
        if (interactArrow != null)
        {
            arrowRenderer = interactArrow.GetComponent<SpriteRenderer>();
            arrowRenderer.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player entered elevator button interaction range");
            if (arrowRenderer != null)
                arrowRenderer.enabled = true;

            player.SetNearbyInteractable(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player exited elevator button interaction range");
            if (arrowRenderer != null)
                arrowRenderer.enabled = false;

            player.ClearNearbyInteractable();
        }
    }

    public void Interact(PlayerController player)
    {
        if (elevator != null)
            elevator.CallElevatorToPlayer();
    }

    public string GetDebugName() => "ElevatorButton";
}