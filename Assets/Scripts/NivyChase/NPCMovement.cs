using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // Speed of the pointer movement

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
        transform.position += moveSpeed * Time.deltaTime * direction * Vector3.up;
        transform.position += 1f * Time.deltaTime * Vector3.right;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        direction *= -1;
    }
}