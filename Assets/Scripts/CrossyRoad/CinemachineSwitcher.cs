using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Script used to switch between different Cinemachine cameras
public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] GameObject NpcTrigger;
    [SerializeField] private Image speechBubbleImage;
    [SerializeField] private PlayerController playerController;
    private Animator animator;
    private string currentCamera = "FirstCamera";
    private Coroutine switchCoroutine;

    /// <summary>
    /// Current camera state name (e.g. "FirstCamera", "SecondCamera", "ThirdCamera", "FourthCamera").
    /// </summary>
    public string CurrentCamera => currentCamera;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    private void Start()
    {
        // If not assigned in inspector, try to find the player
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }
    }

    // Function used to switch between camers
    public void SwitchState()
    {
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }

        // Based on current camera, decide to which one to switch
        switch (currentCamera)
        {
            case "FirstCamera":
                // Lock movement during transition, block rightward movement
                if (playerController != null)
                {
                    playerController.SetMovementLocked(true);
                    playerController.SetRightMovementBlocked(true);
                }
                // Switch to camera 2 after 2 second delay
                NpcTrigger.SetActive(true);
                switchCoroutine = StartCoroutine(SwitchAfterDelay(2f, "SecondCamera"));
                break;
            case "SecondCamera":
                // Unblock right movement when proceeding to ThirdCamera
                if (playerController != null)
                {
                    playerController.SetRightMovementBlocked(false);
                }
                animator.Play("ThirdCamera");
                currentCamera = "ThirdCamera";
                break;
            case "ThirdCamera":
                animator.Play("FourthCamera");
                currentCamera = "FourthCamera";
                StartCoroutine(DisplayDialogueAfterDelay(2f));
                break;
            case "FourthCamera":
                // MINIGAME OVER
                Debug.Log("MINIGAME OVER - GOING TO NEXT LEVEL");
                GameManager.Instance.OnMinigameComplete();
                break;
        }

    }

    private IEnumerator DisplayDialogueAfterDelay(float delay)
    {

        yield return new WaitForSeconds(delay);
        speechBubbleImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(delay);
        speechBubbleImage.gameObject.SetActive(false);
    }

    // Helper function used to switch to camera 2 after a delay
    private IEnumerator SwitchAfterDelay(float delay, string nextCamera)
    {
        // Wait X seconds
        yield return new WaitForSeconds(delay);

        // Switch to next state, reassign current camera
        animator.Play(nextCamera);
        currentCamera = nextCamera;

        // If switched to second camera, unlock movement (but right is still blocked) and set another switch after 7 seconds
        if (currentCamera == "SecondCamera")
        {
            if (playerController != null)
            {
                playerController.SetMovementLocked(false);
                // Right movement stays blocked - player must go left
            }
            switchCoroutine = StartCoroutine(SwitchAfterDelay(7f));
        }
    }

    // After camera runs for 7 seconds, switch to the next one 
    private IEnumerator SwitchAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SwitchState();
    }
}
