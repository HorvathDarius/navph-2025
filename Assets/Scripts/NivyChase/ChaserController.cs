using UnityEngine;

public class ChaserController : MonoBehaviour
{
    [SerializeField] private GameObject chasedPlayer;
    [SerializeField] private float chaseSpeed = 1f;
    public bool isChasing = true;

    void Update()
    {
        if (!isChasing)
        {
            return;
        }

        Vector2 direction = (chasedPlayer.transform.position - transform.position).normalized;
        transform.position += (Vector3)(direction * chaseSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == chasedPlayer)
        {
            isChasing = false;
            Debug.Log("Chaser caught the player!");
            Debug.Log("Start quick-time event");
            StartCoroutine(NivyChaseManager.Instance.StartQuickTimeEvent());
        }
    }
}
