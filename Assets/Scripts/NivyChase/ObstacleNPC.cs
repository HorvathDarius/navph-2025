using UnityEngine;

public class VerticalFollowNPC : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float damping = 5f; // higher = faster follow
    [SerializeField] private float xSpeed = 2f;

    private float velocityY;

    void Update()
    {
        float targetY = player.position.y;

        float newY = Mathf.SmoothDamp(
            transform.position.y,
            targetY,
            ref velocityY,
            1f / damping
        );
        float newX = transform.position.x + xSpeed * Time.deltaTime;

        transform.position = new Vector3(
            newX,
            newY,
            transform.position.z
        );
    }
}
