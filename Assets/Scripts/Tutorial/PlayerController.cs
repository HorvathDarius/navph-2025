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
    public List<Item> inventoryItems = new List<Item>();
    private Rigidbody2D rb2d;
    private Vector2 moveDirection;
    private InteractiveObject nearbyObject;

    public HealthBar healthBar;


    void Start()
    {
        // Initialize player health and health bar
        currentPlayerHealth = maxPlayerHealth;
        healthBar.setMaxHealth(maxPlayerHealth);

        // Assign input actions
        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
        jumpAction = InputSystem.actions.FindAction("Jump");

        // Initialize Rigidbody2D
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // Enable and disable input actions 
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
        // Handle movement input
        Vector2 input = moveAction.ReadValue<Vector2>();
        float horizontalInput = input.x;
        float verticalInput = input.y;

        moveDirection = new Vector2(horizontalInput, verticalInput);
        rb2d.linearVelocity = moveDirection * moveSpeed;
        RotatePlayer(horizontalInput, verticalInput);


        // Handle interaction input
        if (interactAction.WasPressedThisFrame())
        {
            Debug.Log("Interact action triggered in PlayerController");
            // TEST - print out inventory contents
            foreach (var item in inventoryItems)
            {
                Debug.Log("Inventory contains: " + item.name);
            }

            // Interact with nearby object if available
            if (nearbyObject != null)
            {
                Debug.Log("Interact action triggered in PlayerController");
                nearbyObject.pickUpItem(this);
            }
        }

        // Handle jump input (for testing damage)
        if (jumpAction.WasPressedThisFrame())
        {
            Debug.Log("- 10 damage to player health");
            TakeDamage(10);
        }

    }

    // Rotate player to face movement direction
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

    // Set and clear nearby interactive object
    public void SetNearbyObject(InteractiveObject obj)
    {
        nearbyObject = obj;
    }

    public void ClearNearbyObject()
    {
        nearbyObject = null;
    }

    // Add item to player inventory
    public void AddItemToInvetory(Item item)
    {
        Debug.Log("Adding to inventory: " + item.itemName);
        inventoryItems.Add(item);
        Debug.Log("Item added to inventory: " + item.itemName);
    }

    // Handle player taking damage
    public void TakeDamage(int damage)
    {
        currentPlayerHealth -= damage;
        healthBar.setHealth(currentPlayerHealth);
    }
}