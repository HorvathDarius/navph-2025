using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;

    [Header("Input")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction interactAction;
    [SerializeField] private InputAction runningAction;
    [SerializeField] private InputAction pickUpAction;

    public List<CollectibleItem> inventoryItems = new();
    private Rigidbody2D rb2d;
    private Vector2 moveDirection;

    private IInteractable nearbyInteractable;
    private string obstacleType;
    private CheckoutTrigger nearbyCheckout;
    private Animator animator;
    private bool isRunning;
    private bool isMovementLocked;
    private PlayerController controller;
    private CapsuleCollider2D capsuleCollider;

    public bool FacingEast { get; private set; } = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
        runningAction = InputSystem.actions.FindAction("Sprint");
        pickUpAction = InputSystem.actions.FindAction("PickUp");

        rb2d = GetComponent<Rigidbody2D>();
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

        animator.SetFloat("DirectionX", 1f);
    }

    private void Update()
    {
        HandleMovementInput();
        HandleNonCombatActions();
    }

    private void FixedUpdate()
    {
        if (isMovementLocked)
        {
            rb2d.linearVelocity = Vector2.zero;
            return;
        }

        float speed = isRunning ? sprintSpeed : moveSpeed;
        rb2d.linearVelocity = moveDirection * speed;
    }

    private void HandleMovementInput()
    {
        if (isMovementLocked)
        {
            moveDirection = Vector2.zero;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        moveDirection = input;

        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        animator.SetBool("IsMoving", isMoving);

        // smer len podľa horizontálu
        if (input.x > 0.01f)
        {
            animator.SetFloat("DirectionX", 1f);
            FacingEast = true;
        }
        else if (input.x < -0.01f)
        {
            animator.SetFloat("DirectionX", -1f);
            FacingEast = false;
        }

        // beh
        isRunning = runningAction.IsPressed();
        animator.SetBool("IsRunning", isRunning);
    }

    private void HandleNonCombatActions()
    {
        bool isCurrentlyMoving = animator.GetBool("IsMoving");

        // Interact – iba ak stojí
        if (interactAction.WasPressedThisFrame())
        {
            if (!isCurrentlyMoving)
            {
                animator.SetTrigger("Interact");
                Debug.Log("[PlayerController] Interact animation trigger.");

                if (nearbyInteractable != null)
                {
                    Debug.Log("[PlayerController] Interacting with: " + nearbyInteractable.GetDebugName());
                    nearbyInteractable.Interact(this);
                }

                if (nearbyCheckout != null)
                {
                    Debug.Log("[PlayerController] Checkout triggered in PlayerController");
                    YemeMazeManager.Instance.TryHandleCheckout(this);
                }
            }
            else
            {
                Debug.Log("[PlayerController] Interact ignored, player is moving.");
            }
        }

        // PickUp – iba ak stojí (napr. tlačidlo F)
        if (pickUpAction != null && pickUpAction.WasPressedThisFrame())
        {
            if (!isCurrentlyMoving)
            {
                animator.SetTrigger("PickUp");
                Debug.Log("[PlayerController] PickUp animation trigger.");
                // samotné pridanie itemu rieši Collectible skript pri kolízii
            }
            else
            {
                Debug.Log("[PlayerController] PickUp ignored, player is moving.");
            }
        }
    }

    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
        if (locked)
        {
            moveDirection = Vector2.zero;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
        }

        Debug.Log($"[PlayerController] SetMovementLocked({locked})");
    }

    // Interactions
    public void SetNearbyInteractable(IInteractable interactable) => nearbyInteractable = interactable;
    public void ClearNearbyInteractable() => nearbyInteractable = null;
    public void SetNearbyCheckout(CheckoutTrigger checkout) => nearbyCheckout = checkout;

    // Inventory
    public void AddItemToInvetory(CollectibleItem item)
    {
        Debug.Log("Adding to inventory: " + item.itemName);
        inventoryItems.Add(item);
        if (GameManager.Instance == null) return;
        GameManager.Instance.CollectItem(item.itemName);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance.Health <= 0)
        {
            return;
        }

        obstacleType = collision.gameObject.tag;
        switch (obstacleType)
        {
            case "BigVehicle":
                Debug.Log("Collision detected with BIG VEHICLE");
                GameManager.Instance.ChangeHealth(-100);
                KillPlayer();
                break;
            case "Scooter":
                Debug.Log("Collision detected with SCOOTER");
                GameManager.Instance.ChangeHealth(-50);
                KillPlayer();
                break;
        }


    }

    public void KillPlayer()
    {
        if (GameManager.Instance.Health <= 0)
        {
            Debug.Log("Player health <= 0, triggering death.");

            animator.SetTrigger("IsDead");
            controller.SetMovementLocked(true);
        }
    }

    public Vector2 GetCurrentMoveDirection() => moveDirection;
}