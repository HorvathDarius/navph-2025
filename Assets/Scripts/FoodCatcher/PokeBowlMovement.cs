using UnityEngine;
using UnityEngine.InputSystem;

public class PokeBowlMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float maxX = 24f;

    private Rigidbody2D rb2d;
    private Vector2 moveDirection;

    [SerializeField] private InputAction moveAction;

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        moveAction = InputSystem.actions.FindAction("Move");
    }


    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        moveDirection = new Vector2(input.x, 0);
    }

    void FixedUpdate()
    {
        rb2d.linearVelocity = moveDirection * moveSpeed;

        Vector2 position = rb2d.position;
        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        rb2d.position = position;
    }
}
