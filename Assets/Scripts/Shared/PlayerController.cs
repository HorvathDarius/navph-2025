using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    private float moveSpeed = 3f;

    [SerializeField] private float sprintSpeed = 6f;

    [Header("Input")] [SerializeField] InputAction moveAction;
    [SerializeField] InputAction interactAction;
    [SerializeField] InputAction runningAction;

    public List<CollectibleItem> inventoryItems = new();
    private Rigidbody2D rb2d;
    private Vector2 moveDirection;

    private InteractiveObject nearbyObject;
    private String obstacleType;
    private CheckoutTrigger nearbyCheckout;

    private Animator m_Animator;
    private bool m_IsMoving;
    private bool m_IsRunning;

    public bool facingEast { get; private set; }

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Assign input actions
        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
        runningAction = InputSystem.actions.FindAction("Sprint");

        // Initialize Rigidbody2D
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        m_Animator.SetBool("FacingEast", true);
        facingEast = true;
    }

    void Update()
    {
        // Handle movement input
        Vector2 input = moveAction.ReadValue<Vector2>();
        float horizontalInput = input.x;
        float verticalInput = input.y;

        moveDirection = new Vector2(horizontalInput, verticalInput);

        // Aktuálny pohyb
        bool isMoving = moveDirection.magnitude > 0.01f;
        m_Animator.SetBool("Moving", isMoving);

        // Smerovanie (Facing East/West)
        switch (horizontalInput)
        {
            case > 0:
                m_Animator.SetBool("FacingEast", true);
                facingEast = true;
                break;
            case < 0:
                m_Animator.SetBool("FacingEast", false);
                facingEast = false;
                break;
        }

        if (runningAction.IsPressed())
        {
            m_Animator.SetBool("Running", true);
            m_IsRunning = true;
        }
        else
        {
            m_Animator.SetBool("Running", false);
            m_IsRunning = false;
        }

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
                Debug.Log("Pickup triggered in PlayerController");
                nearbyObject.pickUpItem(this);
            }

            if (nearbyCheckout != null)
            {
                Debug.Log("Checkout triggered in PlayerController");
                YemeMazeManager.Instance.TryHandleCheckout(this);
            }
        }
    }

    void FixedUpdate()
    {
        if (m_IsRunning)
        {
            rb2d.linearVelocity = moveDirection * sprintSpeed;
        }
        else
        {
            rb2d.linearVelocity = moveDirection * moveSpeed;
        }
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

    public void SetNearbyCheckout(CheckoutTrigger checkout)
    {
        nearbyCheckout = checkout;
    }

    // Add item to player inventory
    public void AddItemToInvetory(CollectibleItem item)
    {
        Debug.Log("Adding to inventory: " + item.itemName);
        inventoryItems.Add(item);
        GameManager.Instance.CollectItem(item.itemName);
        Debug.Log("Item added to inventory: " + item.itemName);
    }

    // Handle player taking damage
    public void TakeDamage(int damage)
    {
        currentPlayerHealth -= damage;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        obstacleType = collision.gameObject.tag;
        if (obstacleType == "BigVehicle")
        {
            Debug.Log("Collision detected with BIG VEHICLE");
            GameManager.Instance.ChangeHealth(-100);
        }
        else
        {
            Debug.Log("Collision detected with SCOOTER");
            GameManager.Instance.ChangeHealth(-50);
        }
    }
}