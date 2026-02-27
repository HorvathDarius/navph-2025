using UnityEngine;

// Script used to trigger camera switches and dialogues when the player enters a specific area
public class CameraTrigger : MonoBehaviour
{
    [SerializeField] private CinemachineSwitcher cameraSwitcher;
    [SerializeField] private DialogueController dialogueController;

    // Track which camera state this trigger already fired in, to prevent double-firing
    // within the same state but still allow firing in a later state.
    private string lastTriggeredInState = "";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player check
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        // Prevent re-triggering within the same camera state
        string currentState = cameraSwitcher.CurrentCamera;
        if (lastTriggeredInState == currentState)
        {
            return;
        }
        
        lastTriggeredInState = currentState;

        // Move to next camera
        cameraSwitcher.SwitchState();

        // If dialogue exists, start conversation
        if (dialogueController != null)
        {
            dialogueController.StartDialogue();
        }
    }
}
