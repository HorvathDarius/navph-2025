using UnityEngine;
using UnityEngine.InputSystem;

public class PokeBowlMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 20f;

    private Rigidbody2D rb2d;
    private bool isMovingHorizontally = true;
    private Vector2 moveDirection;

    [SerializeField] InputAction moveAction;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        float horizontalInput = input.x;
        if (horizontalInput != 0)
        {
            isMovingHorizontally = true;
        }

        if (isMovingHorizontally)
        {
            moveDirection = new Vector2(horizontalInput, 0);
        }
        
    }

    void FixedUpdate()
    {
        rb2d.linearVelocity = moveDirection * moveSpeed;
    }
}