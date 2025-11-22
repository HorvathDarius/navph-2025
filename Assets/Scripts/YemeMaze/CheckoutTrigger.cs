using UnityEngine;

public class CheckoutTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        PlayerController player = collider.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player is on checkout zone");
            player.SetNearbyCheckout(this);
        }
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        PlayerController player = collider.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player left the checkout zone");
            player.SetNearbyCheckout(null);
        }
    }
}
