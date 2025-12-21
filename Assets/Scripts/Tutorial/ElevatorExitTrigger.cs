using UnityEngine;

public class ElevatorExitTrigger : MonoBehaviour
{
    [SerializeField] private ElevatorDoorController elevator;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // výťah musí byť otvorený + tutorial dokončený + hráč má peňaženku/mobil
        if (elevator != null 
            && elevator.IsElevatorOpen 
            && TutorialManager.Instance != null 
            && TutorialManager.Instance.IsFinished 
            && TutorialManager.Instance.HasRequiredTutorialItem(player))
        {
            Debug.Log("[ElevatorExitTrigger] Tutorial complete, exiting minigame.");
            GameManager.Instance.OnMinigameComplete();
        }
        else
        {
            Debug.Log("[ElevatorExitTrigger] Conditions not met, cannot exit tutorial yet.");
        }
    }
}