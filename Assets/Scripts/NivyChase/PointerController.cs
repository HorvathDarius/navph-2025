using UnityEngine;
using UnityEngine.InputSystem;

public class PointerController : MonoBehaviour
{
    [SerializeField] private Transform pointA; // Reference to the starting point
    [SerializeField] private Transform pointB; // Reference to the ending point
    [SerializeField] private RectTransform safeZone; // Reference to the safe zone RectTransform
    [SerializeField] private float moveSpeed = 1000f; // Speed of the pointer movement
    [SerializeField] private InputAction inputAction;

    private RectTransform pointerTransform;
    private Vector3 targetPosition;

    void OnEnable()
    {
        inputAction.Enable();
    }

    void OnDisable()
    {
        inputAction.Disable();
    }

    void Start()
    {
        pointerTransform = GetComponent<RectTransform>();
        targetPosition = pointB.position;
    }

    void Update()
    {
        // Move the pointer towards the target position
        pointerTransform.position = Vector3.MoveTowards(pointerTransform.position, targetPosition, moveSpeed * Time.unscaledDeltaTime);

        // Change direction if the pointer reaches one of the points
        if (Vector3.Distance(pointerTransform.position, pointA.position) < 0.1f)
        {
            targetPosition = pointB.position;
        }
        else if (Vector3.Distance(pointerTransform.position, pointB.position) < 0.1f)
        {
            targetPosition = pointA.position;
        }

        // Check for input
        if (inputAction.WasPressedThisFrame())
        {
            CheckSuccess();
        }
    }

    void CheckSuccess()
    {
        // Check if the pointer is within the safe zone
        if (RectTransformUtility.RectangleContainsScreenPoint(safeZone, pointerTransform.position, null))
        {
            Debug.Log("Success!");
            StartCoroutine(NivyChaseManager.Instance.ResumeGame());
        }
        else
        {
            Debug.Log("Fail!");
        }
    }
}