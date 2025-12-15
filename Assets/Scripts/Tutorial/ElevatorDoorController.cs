using System.Collections;
using UnityEngine;
using TMPro;

public class ElevatorDoorController : MonoBehaviour
{
    [Header("Animator")] [SerializeField] private Animator doorAnimator;

    [Header("Audio")] [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip arriveDingClip;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;

    [Header("Floors")] [SerializeField] private int minFloor = -2;
    [SerializeField] private int maxFloor = 10;
    [SerializeField] private int startFloor = 7;
    [SerializeField] private int introExitFloor = -1;
    [SerializeField] private float timePerFloor = 1.5f;

    [Header("Display")] [SerializeField] private TMP_Text floorDisplayText;

    [Header("Door timings")] [SerializeField]
    private float firstCloseDelay = 1f;

    [SerializeField] private float doorOpenHoldTime = 5f;

    [Header("Operation")] [SerializeField] private bool npcCalls = true;

    private const float DOOR_ANIMATION_DURATION = 2f;

    private int currentFloor;
    private int targetFloor;
    private bool callInProgress = false;
    private bool elevatorOpen = false;
    private bool introFinished = false;

    private bool playerPendingCall = false;
    private int playerPendingFloor = 0;

    private bool npcPendingCall = false;
    private int npcPendingFloor = 0;

    private Coroutine npcRoutine;

    private void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        currentFloor = Mathf.Clamp(startFloor, minFloor, maxFloor);
        targetFloor = currentFloor;
        UpdateFloorDisplay();

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(firstCloseDelay);

        // zatvor dvere s osobou
        SetDoorCloseIfPlayerFloor(withPerson: true);
        yield return new WaitForSeconds(DOOR_ANIMATION_DURATION);

        // jazda na introExitFloor
        int destFloor = Mathf.Clamp(introExitFloor, minFloor, maxFloor);
        yield return MoveBetweenFloors(currentFloor, destFloor);

        currentFloor = destFloor;
        UpdateFloorDisplay();

        yield return new WaitForSeconds(doorOpenHoldTime + DOOR_ANIMATION_DURATION * 2);

        introFinished = true;
        targetFloor = currentFloor;

        npcRoutine = StartCoroutine(RandomNpcCallsRoutine());
        TryProcessPendingCall();
    }

    // ==== VOLANIE OD HRÁČA ====

    public void CallElevatorToPlayer()
    {
        Debug.Log("[Elevator] PLAYER pressed call button on floor " + startFloor);
        CallElevatorToFloor(startFloor, isPlayer: true);
    }

    // ==== INTERNÁ LOGIKA VOLANÍ ====

    private void CallElevatorToFloor(int callerFloor, bool isPlayer)
    {
        if (!introFinished)
        {
            if (isPlayer)
            {
                playerPendingCall = true;
                playerPendingFloor = Mathf.Clamp(callerFloor, minFloor, maxFloor);
                Debug.Log("[Elevator] PLAYER call pending during intro to floor " + playerPendingFloor);
            }

            return;
        }

        callerFloor = Mathf.Clamp(callerFloor, minFloor, maxFloor);

        if (!isPlayer && callerFloor == startFloor)
            return;

        Debug.Log($"[Elevator] {(isPlayer ? "PLAYER" : "NPC")} call to floor {callerFloor}. " +
                  $"CurrentFloor={currentFloor}, callInProgress={callInProgress}, elevatorOpen={elevatorOpen}");
        
        if (callInProgress && targetFloor == callerFloor) return;
        if (!callInProgress && currentFloor == callerFloor && elevatorOpen) return;

        if (callInProgress)
        {
            if (isPlayer)
            {
                playerPendingCall = true;
                playerPendingFloor = callerFloor;
                Debug.Log("[Elevator] PLAYER call queued to floor " + callerFloor);
            }
            else if (!playerPendingCall)
            {
                npcPendingCall = true;
                npcPendingFloor = callerFloor;
                Debug.Log("[Elevator] NPC call queued to floor " + callerFloor);
            }

            return;
        }

        targetFloor = callerFloor;
        StartCoroutine(TravelToTargetFloor());
    }

    private IEnumerator TravelToTargetFloor()
    {
        callInProgress = true;
        Debug.Log("[Elevator] Travel start: from " + currentFloor + " to " + targetFloor);
        
        // zatvor dvere, ak sú otvorené
        if (elevatorOpen)
        {
            SetDoorCloseIfPlayerFloor();
            yield return new WaitForSeconds(DOOR_ANIMATION_DURATION);
        }

        int startFloorBeforeMove = currentFloor;

        // jazda z currentFloor na targetFloor
        yield return MoveBetweenFloors(currentFloor, targetFloor);

        currentFloor = targetFloor;
        UpdateFloorDisplay();

        // príchod – ding len keď reálne prichádzame na hráčske poschodie,
        // nie keď už sme tam a len otvárame dvere
        if (currentFloor == startFloor && startFloorBeforeMove != startFloor)
        {
            if (sfxSource != null && arriveDingClip != null)
                sfxSource.PlayOneShot(arriveDingClip);
        }

        Debug.Log("[Elevator] Opening doors at floor " + currentFloor);
        SetDoorOpenIfPlayerFloor();
        yield return new WaitForSeconds(doorOpenHoldTime + DOOR_ANIMATION_DURATION);
        
        Debug.Log("[Elevator] Closing doors at floor " + currentFloor);
        SetDoorCloseIfPlayerFloor();
        yield return new WaitForSeconds(DOOR_ANIMATION_DURATION);

        callInProgress = false;

        Debug.Log("[Elevator] Travel finished at floor " + currentFloor);
        TryProcessPendingCall();
    }

    private void TryProcessPendingCall()
    {
        if (playerPendingCall)
        {
            playerPendingCall = false;
            targetFloor = Mathf.Clamp(playerPendingFloor, minFloor, maxFloor);
            Debug.Log("[Elevator] Processing queued PLAYER call to floor " + targetFloor);
            StartCoroutine(TravelToTargetFloor());
            return;
        }

        if (npcPendingCall)
        {
            npcPendingCall = false;
            targetFloor = Mathf.Clamp(npcPendingFloor, minFloor, maxFloor);
            Debug.Log("[Elevator] Processing queued NPC call to floor " + targetFloor);
            StartCoroutine(TravelToTargetFloor());
        }
    }

    // ==== RANDOM NPC VOLANIA ====

    private IEnumerator RandomNpcCallsRoutine()
    {
        while (npcCalls)
        {
            float wait = Random.Range(5f, 15f);
            yield return new WaitForSeconds(wait);

            if (playerPendingCall || !introFinished)
                continue;

            int randomFloor;
            do
            {
                randomFloor = Random.Range(minFloor, maxFloor + 1);
            } while (randomFloor == startFloor);

            Debug.Log("[Elevator] NPC randomly calling elevator to floor " + randomFloor);
            CallElevatorToFloor(randomFloor, isPlayer: false);
        }
    }

    // ==== POHYB A DISPLAY ====

    private IEnumerator MoveBetweenFloors(int fromFloor, int toFloor)
    {
        int floorDelta = Mathf.Abs(toFloor - fromFloor);
        float travelTime = floorDelta * timePerFloor;

        if (travelTime <= 0f)
        {
            currentFloor = toFloor;
            UpdateFloorDisplay();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);

            int approxFloor = Mathf.RoundToInt(Mathf.Lerp(fromFloor, toFloor, t));
            approxFloor = Mathf.Clamp(approxFloor, minFloor, maxFloor);

            if (approxFloor != currentFloor)
            {
                currentFloor = approxFloor;
                UpdateFloorDisplay();
            }

            yield return null;
        }

        currentFloor = toFloor;
        UpdateFloorDisplay();
    }

    private void UpdateFloorDisplay()
    {
        if (floorDisplayText != null)
            floorDisplayText.text = currentFloor.ToString();
    }

    // ==== AUDIO & ANIM – len na hráčskom poschodí ====

    private void SetDoorOpenIfPlayerFloor()
    {
        if (currentFloor == startFloor && doorAnimator != null)
        {
            doorAnimator.SetTrigger("OpenEmpty");
        }
        if (currentFloor == startFloor && sfxSource != null && doorOpenClip != null)
        {
            sfxSource.PlayOneShot(doorOpenClip);
        }
        elevatorOpen = true;
    }

    private void SetDoorCloseIfPlayerFloor(bool withPerson = false)
    {
        if (currentFloor == startFloor && doorAnimator != null)
        {
            doorAnimator.SetTrigger(withPerson ? "CloseWithPerson" : "CloseEmpty");
        }
        if (currentFloor == startFloor && sfxSource != null && doorCloseClip != null)
        {
            sfxSource.PlayOneShot(doorCloseClip);
        }
        elevatorOpen = false;
    }

    public bool IsElevatorOpen => elevatorOpen;
}