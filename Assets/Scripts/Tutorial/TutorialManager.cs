using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("References")] [SerializeField]
    private PlayerController player;

    [SerializeField] private Animator playerAnimator;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip doorCloseClip;
    [SerializeField] private AudioClip handDryerClip;

    [Header("Manager Speech Audio")]
    [SerializeField] private AudioSource managerSpeechSource;
    [SerializeField] private AudioClip movementHintClip;
    [SerializeField] private AudioClip pickupHintClip;
    [SerializeField] private AudioClip hudInfoClip;

    [Header("Tutorial UI")] [SerializeField]
    private GameObject speechBubbleRoot;

    [SerializeField] private TMPro.TMP_Text speechText;

    [Header("Timings")]
    [SerializeField] private float walkFromToiletDuration = 1.2f;
    [SerializeField] private float handDryerDuration = 1.8f;
    [SerializeField] private float delayAfterControlsHint = 1f;

    private enum TutorialState
    {
        IntroCutscene,
        ShowMovementHint,
        WaitForMovement,
        ShowPickupHint,
        WaitForPickup,
        ExplainHud,
        Finished
    }

    private TutorialState currentState = TutorialState.IntroCutscene;
    private bool playerHasMoved = false;
    private bool firstTutorialItemPicked = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (playerAnimator == null && player != null)
            playerAnimator = player.GetComponent<Animator>();

        // vypne cely PlayerController kvoli prepisu animatora
        if (player != null)
            player.enabled = false;

        DisablePlayerInput();
        if (speechBubbleRoot != null)
            speechBubbleRoot.SetActive(false);

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // 1) sušič rúk
        if (sfxSource != null && handDryerClip != null)
            sfxSource.PlayOneShot(handDryerClip);

        yield return new WaitForSeconds(handDryerDuration);

        // 2) výjdenie zľava doprava na štartovú pozíciu
        playerAnimator.SetFloat("DirectionX", 1f);
        playerAnimator.SetBool("IsMoving", true);

        Vector3 startPos = player.transform.position;
        Vector3 finalPos = startPos + new Vector3(2f, 0f, 0f);
        float t = 0f;
        while (t < walkFromToiletDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / walkFromToiletDuration);
            player.transform.position = Vector3.Lerp(startPos, finalPos, alpha);
            yield return null;
        }

        playerAnimator.SetBool("IsMoving", false);

        // 3) zvuk zavretia dverí
        if (sfxSource != null && doorCloseClip != null)
            sfxSource.PlayOneShot(doorCloseClip);

        // 4) hint na pohyb
        currentState = TutorialState.ShowMovementHint;
        StartCoroutine(MovementHintFlow());

        if (player != null)
            player.enabled = true;
    }

    /// <summary>
    /// Step 1: Show movement hint bubble + play audio, enable controls,
    /// then wait for BOTH: player moved AND audio finished.
    /// Then transition to pickup hint.
    /// </summary>
    private IEnumerator MovementHintFlow()
    {
        // Show bubble
        if (speechBubbleRoot != null && speechText != null)
        {
            speechBubbleRoot.SetActive(true);
            speechText.text =
                "Beží ti obedná pauza! Máš čas sa prechádzať po chodbe [↑←↓→ / WASD]? Šprintuj na obed [SHIFT]!";
        }

        PlayManagerSpeech(movementHintClip);

        // Enable controls after short delay
        yield return new WaitForSeconds(delayAfterControlsHint);
        EnablePlayerInput();
        currentState = TutorialState.WaitForMovement;

        // Wait until player has moved (bubble stays visible)
        yield return new WaitUntil(() => playerHasMoved);

        // Player moved - now wait for audio to finish too (don't cut it)
        yield return new WaitWhile(() => IsManagerSpeechPlaying());

        // Both conditions met - proceed to pickup hint
        StartCoroutine(PickupHintFlow());
    }

    /// <summary>
    /// Step 2: Show pickup hint bubble + play audio,
    /// bubble stays until player picks up wallet/phone.
    /// Audio must finish playing even if player picks up early.
    /// </summary>
    private IEnumerator PickupHintFlow()
    {
        currentState = TutorialState.ShowPickupHint;

        if (speechBubbleRoot != null && speechText != null)
        {
            speechBubbleRoot.SetActive(true);
            speechText.text =
                "Nezdržuj sa moc na chodbe, skoč do kanclu a zober si peňaženku [E], nechal si si ju na stole.";
        }

        PlayManagerSpeech(pickupHintClip);

        currentState = TutorialState.WaitForPickup;

        // Wait until player picks up the item (bubble stays visible)
        yield return new WaitUntil(() => firstTutorialItemPicked);

        // Player picked up - wait for audio to finish (don't cut it)
        yield return new WaitWhile(() => IsManagerSpeechPlaying());

        // Both conditions met - proceed to HUD explanation
        StartCoroutine(HudInfoFlow());
    }

    /// <summary>
    /// Step 3: Show HUD info bubble + play audio,
    /// wait for audio to finish, then hide bubble and finish tutorial.
    /// </summary>
    private IEnumerator HudInfoFlow()
    {
        currentState = TutorialState.ExplainHud;

        if (speechBubbleRoot != null && speechText != null)
        {
            speechBubbleRoot.SetActive(true);
            speechText.text =
                "Hore vidíš svoj zdravotný stav, čas a skóre. Tu sa ti nič nestane, ale vonku ti to neviem garantovať.";
        }

        PlayManagerSpeech(hudInfoClip);

        // Wait for audio to finish playing completely
        yield return new WaitWhile(() => IsManagerSpeechPlaying());

        // Audio finished - hide bubble
        if (speechBubbleRoot != null)
        {
            speechBubbleRoot.SetActive(false);
            speechText.text = "";
        }

        currentState = TutorialState.Finished;
    }

    private void Update()
    {
        if (currentState == TutorialState.WaitForMovement && !playerHasMoved && player != null)
        {
            // sleduj, či hráč začal pohybovať postavou
            var moveAction = InputSystem.actions.FindAction("Move");
            if (moveAction != null && moveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f)
            {
                playerHasMoved = true;
            }
        }
    }

    public void HandleItemPickedUp(CollectibleItem item)
    {
        // reaguj len v tutorial scéne a len raz
        if (firstTutorialItemPicked || currentState != TutorialState.WaitForPickup)
            return;

        // skontroluj, či je to peňaženka alebo mobil
        if (item.itemName is "Wallet" or "Phone")
        {
            firstTutorialItemPicked = true;
            // coroutine in PickupHintFlow will detect this via WaitUntil
        }
    }

    public void OnFirstItemPickedUp()
    {
        // Already handled by HandleItemPickedUp - kept for backward compatibility
        if (!firstTutorialItemPicked)
        {
            firstTutorialItemPicked = true;
        }
    }

    private void DisablePlayerInput()
    {
        var move = InputSystem.actions.FindAction("Move");
        var interact = InputSystem.actions.FindAction("Interact");
        var pickUp = InputSystem.actions.FindAction("PickUp");
        var sprint = InputSystem.actions.FindAction("Sprint");

        move?.Disable();
        interact?.Disable();
        pickUp?.Disable();
        sprint?.Disable();
    }

    private void EnablePlayerInput()
    {
        var move = InputSystem.actions.FindAction("Move");
        var interact = InputSystem.actions.FindAction("Interact");
        var pickUp = InputSystem.actions.FindAction("PickUp");
        var sprint = InputSystem.actions.FindAction("Sprint");

        move?.Enable();
        interact?.Enable();
        pickUp?.Enable();
        sprint?.Enable();
    }

    public bool HasRequiredTutorialItem(PlayerController player)
    {
        return player.inventoryItems.Exists(i => i.itemName == "Wallet" || i.itemName == "Phone");
    }
    
    public bool IsFinished => currentState == TutorialState.Finished;

    private bool IsManagerSpeechPlaying()
    {
        return managerSpeechSource != null && managerSpeechSource.isPlaying;
    }

    private void PlayManagerSpeech(AudioClip clip)
    {
        if (managerSpeechSource != null && managerSpeechSource.isPlaying)
        {
            managerSpeechSource.Stop();
        }

        if (managerSpeechSource != null && clip != null)
        {
            managerSpeechSource.clip = clip;
            managerSpeechSource.Play();
        }
    }
}

