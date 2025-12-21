using UnityEngine;

public class EscapeTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        PlayerController player = collider.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player is on escape zone");
            YemeMazeManager.Instance.OnEscapeStore();
        }
    }
}