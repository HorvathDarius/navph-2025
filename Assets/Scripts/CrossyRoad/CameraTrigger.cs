using UnityEngine;

// Script used to trigger camera switches and dialogues when the player enters a specific area
public class CameraTrigger : MonoBehaviour
{
    [SerializeField] private CinemachineSwitcher cameraSwitcher;
    [SerializeField] private DialogueController dialogueController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player check
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Move to next camera
        cameraSwitcher.SwitchState();

        // If dialogue exists, start conversation
        if (dialogueController != null)
        {
            dialogueController.StartDialogue();
        }
    }
}
