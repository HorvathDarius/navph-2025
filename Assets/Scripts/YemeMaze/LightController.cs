using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class LightController : MonoBehaviour
{
    [Header("Lights")] [SerializeField] private Light2D lightMobile; // Mobile light attached to MainCharacter
    [SerializeField] private Light2D lightPowerOutage; // Power outage light source
    [SerializeField] private Light2D lightGlobal; // Global light source

    [Header("Mobile Light Movement")] [SerializeField]
    private Transform mobileLightTransform; // Reference to Light 2D - Mobile transform

    [SerializeField] private PlayerController playerController; // Assign in inspector

    [Header("Lights Settings")]
    [Tooltip("If true, lights will go off infinitely. Other setting except intervals are redundant.")]
    [SerializeField]
    private bool outageModeOnetime = true;

    [Tooltip("Minimum time interval (seconds) between outages.")] [SerializeField]
    private float minInterval = 10f;

    [Tooltip("Maximum time interval (seconds) between outages.")] [SerializeField]
    private float maxInterval = 15f;

    [Tooltip("Duration (seconds) of each outage.")] [SerializeField]
    private float outageDuration = 30f;

    [Tooltip("Number of times the warning sign flashes before outage.")] [SerializeField]
    private int warningFlashes = 3;

    [Tooltip("Duration (seconds) of each warning flash ON or OFF.")] [SerializeField]
    private float warningFlashDuration = 0.3f;

    [Tooltip("If -1, lights will go out infinite amount of time.")] [SerializeField]
    private int maxOutages = -1;

    [Tooltip("Simulating player inventory containing mobile")] [SerializeField]
    private bool playerHasMobile = true;

    [Header("Input")] [SerializeField] InputAction mobileLightAction;

    private VisualElement warningSign; // Warning sign UI element

    private int m_outagesDone = 0;
    private bool m_isOutageInProgress = false;

    private bool m_mobileLightOn = false;

    private Coroutine m_outageCoroutine;

    void Start()
    {
        mobileLightAction = InputSystem.actions.FindAction("MobileLight");

        // Zisti, či hráč zobral telefón v tutoriáli
        playerHasMobile = GameManager.Instance != null && GameManager.Instance.HasItem("Phone");
        Debug.Log($"[LightController] playerHasMobile = {playerHasMobile}");

        if (GameManager.Instance != null)
        {
            var uiDocument = GameManager.Instance.GameUI;
            if (uiDocument != null)
            {
                warningSign = uiDocument.rootVisualElement.Q<VisualElement>("MazeFOVOverlay");
                warningSign.style.display = DisplayStyle.None;
            }
        }

        // Initial light state: Global ON, Mobile and Power Outage OFF
        SetLightsGlobalOn();
        StartOutageCycle();
    }

    void Update()
    {
        // Toggle mobile light by input if player has it and outage is in progress
        if (m_isOutageInProgress && playerHasMobile && mobileLightAction.WasPressedThisFrame())
        {
            m_mobileLightOn = !m_mobileLightOn;
            if (lightMobile != null)
                lightMobile.enabled = m_mobileLightOn;
        }

        if (playerController != null && mobileLightTransform != null)
        {
            // If facing east, rotation = 0°, else west, rotation = 180° around Y axis
            mobileLightTransform.localEulerAngles = playerController.FacingEast
                ? new Vector3(0, 0, -90)
                : new Vector3(0, 0, 90);
        }
    }

    void SetLightsGlobalOn()
    {
        if (lightGlobal != null) lightGlobal.enabled = true;
        if (lightMobile != null) lightMobile.enabled = false;
        if (lightPowerOutage != null) lightPowerOutage.enabled = false;
        m_mobileLightOn = false;
        m_isOutageInProgress = false;
    }

    void SetLightsOutageOn()
    {
        if (lightGlobal != null) lightGlobal.enabled = false;
        if (lightPowerOutage != null) lightPowerOutage.enabled = true;
        if (lightMobile != null) lightMobile.enabled = false;
        m_mobileLightOn = false;
        m_isOutageInProgress = true;
    }

    private void StartOutageCycle()
    {
        if (m_outageCoroutine != null) StopCoroutine(m_outageCoroutine);
        m_outagesDone = 0;
        m_outageCoroutine = outageModeOnetime
            ? StartCoroutine(OnetimeLightOutRoutine())
            : StartCoroutine(RandomLightOutRoutine());
    }

    IEnumerator RandomLightOutRoutine()
    {
        while (maxOutages == -1 || m_outagesDone < maxOutages)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            yield return StartCoroutine(FlashWarningSign());

            SetLightsOutageOn();

            yield return new WaitForSeconds(outageDuration);

            SetLightsGlobalOn();

            m_outagesDone++;
        }
    }

    IEnumerator OnetimeLightOutRoutine()
    {
        float waitTime = Random.Range(minInterval, maxInterval);
        yield return new WaitForSeconds(waitTime);

        yield return StartCoroutine(FlashWarningSign());

        SetLightsOutageOn();
    }

    IEnumerator FlashWarningSign()
    {
        if (warningSign == null)
            yield break;

        for (int i = 0; i < warningFlashes; i++)
        {
            warningSign.style.display = DisplayStyle.Flex;
            yield return new WaitForSeconds(warningFlashDuration);
            warningSign.style.display = DisplayStyle.None;
            yield return new WaitForSeconds(warningFlashDuration);
        }
    }
}