using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DisclaimerScreen : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float minDisplayDuration = 4f;
    [SerializeField] private float afterAudioDelay = 2f;

    [Header("Scene References")]
    [SerializeField] private GameObject disclaimerCanvas;
    [SerializeField] private TextMeshProUGUI skipHintLabel;

    [Header("Audio (optional - voiceover reading the disclaimer)")]
    [SerializeField] private AudioClip disclaimerVoiceover;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuRoot;

    private InputAction skipAction;
    private AudioSource audioSource;

    private void Start()
    {
        skipAction = new InputAction("Skip", type: InputActionType.PassThrough);
        skipAction.AddBinding("<Keyboard>/anyKey");
        skipAction.AddBinding("<Mouse>/press");
        skipAction.AddBinding("<Gamepad>/*");
        skipAction.Enable();

        if (skipHintLabel != null)
            skipHintLabel.gameObject.SetActive(false);

        if (disclaimerCanvas != null)
            disclaimerCanvas.SetActive(true);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        StartCoroutine(ShowDisclaimer());
    }

    private void OnDestroy()
    {
        skipAction?.Disable();
        skipAction?.Dispose();
    }

    private IEnumerator ShowDisclaimer()
    {
        float waited = 0f;

        if (disclaimerVoiceover != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = disclaimerVoiceover;
            audioSource.playOnAwake = false;
        }

        float totalDuration = minDisplayDuration;
        if (audioSource != null && disclaimerVoiceover != null)
            totalDuration = Mathf.Max(totalDuration, 1f + disclaimerVoiceover.length + afterAudioDelay);

        bool audioStarted = false;

        while (waited < totalDuration)
        {
            waited += Time.unscaledDeltaTime;

            if (!audioStarted && waited >= 1f && audioSource != null)
            {
                audioSource.Play();
                audioStarted = true;
            }

            if (waited >= minDisplayDuration && skipHintLabel != null && !skipHintLabel.gameObject.activeSelf)
                skipHintLabel.gameObject.SetActive(true);

            if (waited >= minDisplayDuration && skipAction.WasPressedThisFrame())
                break;

            yield return null;
        }

        if (audioSource != null)
            audioSource.Stop();

        if (disclaimerCanvas != null)
            disclaimerCanvas.SetActive(false);

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);
    }
}
