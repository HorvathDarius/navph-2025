using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // Speed of the pointer movement
    [SerializeField] private bool oppositeDirection = false;
    private int direction;

    void Start()
    {
        Debug.Log("---");
        Debug.Log(Random.Range(0, 3));
        direction = Random.Range(0, 3) > 1 ? 1 : -1;
        Debug.Log(direction);
    }

    void Update()
    {
        // Move the pointer towards the target position
        Vector3 moveDirection = oppositeDirection ? Vector3.left : Vector3.right;
        transform.position += moveSpeed * Time.deltaTime * direction * Vector3.up;
        transform.position += 1f * Time.deltaTime * moveDirection;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        direction *= -1;
    }
}