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

    [Header("Tutorial UI")] [SerializeField]
    private GameObject speechBubbleRoot;

    [SerializeField] private TMPro.TMP_Text speechText;

    [Header("Timings")]
    [SerializeField] private float walkFromToiletDuration = 1.2f;
    [SerializeField] private float handDryerDuration = 1.8f;
    [SerializeField] private float delayAfterControlsHint = 1f;
    [SerializeField] private float delayBeforePickupHint = 2.0f;
    [SerializeField] private float delayAfterLootInfo = 7f;
    [SerializeField] private float delayHudInfo = 7f;

    private enum TutorialState
    {
        IntroCutscene,
        ShowMovementHint,
        WaitForMovement,
        ShowPickupHint,
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
        ShowMovementHint();

        if (player != null)
            player.enabled = true;
    }

    private void ShowMovementHint()
    {
        if (speechBubbleRoot != null && speechText != null)
        {
            speechBubbleRoot.SetActive(true);
            speechText.text =
                "Beží ti obedná pauza! Máš čas sa prechádzať po chodbe [↑←↓→ / WASD]? Šprintuj na obed [SHIFT]!";
        }

        // počká malý moment a potom pustí vstup
        StartCoroutine(EnableControlsAfterDelay());
    }

    private IEnumerator EnableControlsAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterControlsHint);
        EnablePlayerInput();
        currentState = TutorialState.WaitForMovement;
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
                StartCoroutine(OnPlayerStartedMoving());
            }
        }
    }

    private IEnumerator OnPlayerStartedMoving()
    {
        yield return new WaitForSeconds(delayBeforePickupHint);

        if (speechBubbleRoot != null && speechText != null)
        {
            speechBubbleRoot.SetActive(true);
            speechText.text =
                "Nezdržuj sa moc na chodbe, skoč do kanclu a zober si peňaženku [E], nechal si si ju na stole.";
        }

        currentState = TutorialState.ShowPickupHint;
    }

    public void HandleItemPickedUp(CollectibleItem item)
    {
        // reaguj len v tutorial scéne a len raz
        if (firstTutorialItemPicked || currentState != TutorialState.ShowPickupHint)
            return;

        // skontroluj, či je to peňaženka alebo mobil
        if (item.itemName is "Wallet" or "Phone")
        {
            firstTutorialItemPicked = true;
            OnFirstItemPickedUp();
        }
    }

    public void OnFirstItemPickedUp()
    {
        if (speechBubbleRoot == null || speechText == null)
            return;

        speechBubbleRoot.SetActive(true);
        speechText.text =
            "Všetko, čo zodvihneš [F], ti skončí v inventári [I] a môžno sa ti to neskôr zíde.";

        currentState = TutorialState.ExplainHud;
        StartCoroutine(ShowHudInfoSequence());
    }

    private IEnumerator ShowHudInfoSequence()
    {
        yield return new WaitForSeconds(delayAfterLootInfo);

        if (speechBubbleRoot != null && speechText != null)
        {
            speechText.text =
                "Hore vidíš svoj zdravotný stav, čas a skóre. Tu sa ti nič nestane, ale vonku ti to neviem garantovať.";
        }

        yield return new WaitForSeconds(delayHudInfo);

        if (speechBubbleRoot != null)
        {
            speechBubbleRoot.SetActive(false);
            speechText.text = "";
        }

        currentState = TutorialState.Finished;
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
}