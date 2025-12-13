using UnityEngine;
using UnityEngine.InputSystem;

public class CinemachineSwitcher : MonoBehaviour
{

    [SerializeField] private InputAction action;

    private Animator animator;
    private bool firstCamera = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        action.performed += _ => SwitchState();
    }

    private void SwitchState()
    {
        Debug.Log("Switching Camera");
        if (firstCamera)
        {
            animator.Play("SecondCamera");
        }
        else
        {
            animator.Play("FirstCamera");
        }
        firstCamera = !firstCamera;
    }
}
