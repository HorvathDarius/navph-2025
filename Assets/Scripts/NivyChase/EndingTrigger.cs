using UnityEngine;

public class EndingTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player reached the end of Nivy Chase!");
            GameManager.Instance.OnMinigameComplete();
        }
    }
}
