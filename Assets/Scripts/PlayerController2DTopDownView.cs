using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2DTopDownView : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool canMoveDiagonally = true;

    private Rigidbody2D rb2d;
    private Vector2 moveDirection;
    private bool isMovingHorizontally = true;

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
        float verticalInput = input.y;

        if (canMoveDiagonally)
        {
            moveDirection = new Vector2(horizontalInput, verticalInput);
            RotatePlayer(horizontalInput, verticalInput);
        }
        else
        {
            if (horizontalInput != 0)
            {
                isMovingHorizontally = true;
            }
            else if (verticalInput != 0)
            {
                isMovingHorizontally = false;
            }

            if (isMovingHorizontally)
            {
                moveDirection = new Vector2(horizontalInput, 0);
                RotatePlayer(horizontalInput, 0);
            }
            else
            {
                moveDirection = new Vector2(0, verticalInput);
                RotatePlayer(0, verticalInput);
            }
        }
    }

    void FixedUpdate()
    {
        rb2d.linearVelocity = moveDirection * moveSpeed;
    }

    void RotatePlayer(float x, float y)
    {
        if (x == 0 && y == 0) return;

        float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}