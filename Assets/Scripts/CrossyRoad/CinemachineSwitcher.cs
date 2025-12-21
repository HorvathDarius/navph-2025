using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Script used to switch between different Cinemachine cameras
public class CinemachineSwitcher : MonoBehaviour
{
    [SerializeField] GameObject NpcTrigger;
    [SerializeField] private Image speechBubbleImage;
    private Animator animator;
    private string currentCamera = "FirstCamera";
    private Coroutine switchCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
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
                // Switch to camera 2 after 2 second ddelay
                NpcTrigger.SetActive(true);
                switchCoroutine = StartCoroutine(SwitchAfterDelay(2f, "SecondCamera"));
                break;
            case "SecondCamera":
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

        // If switched to second camera, set another switch after 7 seconds
        if (currentCamera == "SecondCamera")
        {
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
