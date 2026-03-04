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
    private bool isRightMovementBlocked;
    private PlayerController controller;
    private CapsuleCollider2D capsuleCollider;
    private bool isInteracting;

    public bool FacingEast { get; private set; } = true;

    void OnEnable()
    {
        moveAction.Enable();
        interactAction.Enable();
        runningAction.Enable();
        pickUpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        interactAction.Disable();
        runningAction.Disable();
        pickUpAction.Disable();
    }
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        
        moveAction = InputSystem.actions.FindAction("Move");
        interactAction = InputSystem.actions.FindAction("Interact");
        runningAction = InputSystem.actions.FindAction("Sprint");
        pickUpAction = InputSystem.actions.FindAction("PickUp");
    }

    private void Start()
    {
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
        // ak prebieha interakcia, hráč sa nesmie hýbať
        if (isMovementLocked || isInteracting)
        {
            moveDirection = Vector2.zero;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsRunning", false);
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        
        // Block rightward movement if flag is set
        if (isRightMovementBlocked && input.x > 0f)
        {
            input.x = 0f;
        }
        
        moveDirection = input;

        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        animator.SetBool("IsMoving", isMoving);

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

        isRunning = runningAction.IsPressed();
        animator.SetBool("IsRunning", isRunning);
    }

    private void HandleNonCombatActions()
    {
        bool isCurrentlyMoving = animator.GetBool("IsMoving");

        if (interactAction.WasPressedThisFrame() && !isInteracting)
        {
            if (!isCurrentlyMoving)
            {
                StartCoroutine(PlayInteractSequence());
            }
            else
            {
                Debug.Log("[PlayerController] Interact ignored, player is moving.");
            }
        }

        if (pickUpAction != null && pickUpAction.WasPressedThisFrame() && !isInteracting)
        {
            if (!isCurrentlyMoving)
            {
                StartCoroutine(PlayPickUpSequence());
            }
            else
            {
                Debug.Log("[PlayerController] PickUp ignored, player is moving.");
            }
        }
    }
    
    private IEnumerator PlayInteractSequence()
    {
        isInteracting = true;
        SetMovementLocked(true);

        animator.SetTrigger("Interact");
        Debug.Log("[PlayerController] Interact animation trigger.");

        // zisti dlzku aktualnej interact animacie
        yield return null; // pocka, kym Animator prepne state
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float interactLength = state.length > 0 ? state.length : 0.5f;

        // vykona gameplay logiku interakcie
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

        yield return new WaitForSeconds(interactLength);

        SetMovementLocked(false);
        isInteracting = false;
        Debug.Log("[PlayerController] Interact finished, movement unlocked.");
    }

    private IEnumerator PlayPickUpSequence()
    {
        isInteracting = true;
        SetMovementLocked(true);

        animator.SetTrigger("PickUp");
        Debug.Log("[PlayerController] PickUp animation trigger.");

        yield return null;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float pickUpLength = state.length > 0 ? state.length : 0.5f;

        if (nearbyInteractable != null)
        {
            Debug.Log("[PlayerController] Interacting with: " + nearbyInteractable.GetDebugName());
            nearbyInteractable.Interact(this);
        }
        
        yield return new WaitForSeconds(pickUpLength);

        SetMovementLocked(false);
        isInteracting = false;
        Debug.Log("[PlayerController] PickUp finished, movement unlocked.");
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

    public void SetRightMovementBlocked(bool blocked)
    {
        isRightMovementBlocked = blocked;
        Debug.Log($"[PlayerController] SetRightMovementBlocked({blocked})");
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
        
        // Skip damage during checkpoint invincibility
        if (CrossyRoadCheckpoint.IsPlayerInvincible)
        {
            return;
        }

        obstacleType = collision.gameObject.tag;
        switch (obstacleType)
        {
            case "BigVehicle":
                Debug.Log("Collision detected with BIG VEHICLE");
                GameManager.Instance.ChangeHealth(-100);
                HandleDeath();
                break;
            case "Scooter":
                Debug.Log("Collision detected with SCOOTER");
                GameManager.Instance.ChangeHealth(-50);
                HandleDeath();
                break;
        }
    }

    /// <summary>
    /// Handles death logic - if a checkpoint is active, respawn there instead of full game over.
    /// </summary>
    private void HandleDeath()
    {
        if (GameManager.Instance.Health > 0)
            return;
            
        // Check if a CrossyRoad checkpoint is active - respawn there instead of dying
        if (CrossyRoadCheckpoint.TryRespawnPlayer(this))
        {
            Debug.Log("Player died but checkpoint active - respawning at checkpoint.");
            // Play death animation briefly, checkpoint handles the rest
            animator.SetTrigger("IsDead");
            SetMovementLocked(true);
            
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.bodyType = RigidbodyType2D.Kinematic;
            }
            
            // Re-activate game (prevent GameOver from showing)
            GameManager.Instance.CancelGameOver();
            return;
        }

        // No checkpoint - original death behavior
        KillPlayer();
    }

    public void KillPlayer()
    {
        if (GameManager.Instance.Health <= 0)
        {
            Debug.Log("Player health <= 0, triggering death.");

            animator.SetTrigger("IsDead");
            SetMovementLocked(true);

            // Vypni input úplne — hráč sa nesmie hýbať po smrti
            moveAction.Disable();
            interactAction.Disable();
            runningAction.Disable();
            pickUpAction.Disable();

            // Zmraz Rigidbody — kinematic zabraňuje posúvaniu ale fyzika okolia pokračuje
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }

    public Vector2 GetCurrentMoveDirection() => moveDirection;
}