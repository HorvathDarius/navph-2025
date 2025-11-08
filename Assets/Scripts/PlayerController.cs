using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] InputAction moveAction;
    [SerializeField] InputAction interactAction;
    [SerializeField] InputAction jumpAction;
    
    public int maxPlayerHealth = 100;
    public int currentPlayerHealth;
    private List<GameObject> inventoryItems = new List<GameObject>();
    private Rigidbody2D rb2d;
    private Vector2 moveDirection;
    private InteractiveObject nearbyObject;

    public HealthBar healthBar;


    void Start()
    {
        currentPlayerHealth = maxPlayerHealth;
        healthBar.setMaxHealth(maxPlayerHealth);

        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
        jumpAction = InputSystem.actions.FindAction("Jump");
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
        interactAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
        interactAction.Disable();
        jumpAction.Disable();
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        float horizontalInput = input.x;
        float verticalInput = input.y;

        moveDirection = new Vector2(horizontalInput, verticalInput);
        rb2d.linearVelocity = moveDirection * moveSpeed;
        RotatePlayer(horizontalInput, verticalInput);


        if (interactAction.WasPressedThisFrame())
        {
            Debug.Log("Interact action triggered in PlayerController");
            foreach (var item in inventoryItems)
            {
                Debug.Log("Inventory contains: " + item.name);
            }
        }

        if (jumpAction.WasPressedThisFrame())
        {
            Debug.Log("- 10 damage to player health");
            TakeDamage(10);
        }

        if (nearbyObject != null && interactAction.WasPressedThisFrame())
        {
            Debug.Log("Interact action triggered in PlayerController");
            nearbyObject.pickUpItem(this);
        }
    }

    // void FixedUpdate()
    // {
    //     rb2d.linearVelocity = moveDirection * moveSpeed;
    // }

    void RotatePlayer(float x, float y)
    {
        if (x == 0 && y == 0) return;

        float angle = 0f;

        if (x == 1)
        {
            angle = 0f;
        }
        else if (x == -1)
        {
            angle = 180f;
        }

        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    public void SetNearbyObject(InteractiveObject obj)
    {
        nearbyObject = obj;
    }

    public void ClearNearbyObject()
    {
        nearbyObject = null;
    }

    public void AddItemToInvetory(GameObject item)
    {
        Debug.Log("Adding to inventory: " + item.name);
        inventoryItems.Add(item);
        Debug.Log("Item added to inventory: " + item.name);
    }

    public void TakeDamage(int damage)
    {
        currentPlayerHealth -= damage;
        healthBar.setHealth(currentPlayerHealth);
    }
}