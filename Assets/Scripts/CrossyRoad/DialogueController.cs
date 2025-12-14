using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Script used to control dialogues in the game
public class DialogueController : MonoBehaviour
{
    [SerializeField] private Image firstSpeechBubbleImage;
    [SerializeField] private TextMeshProUGUI firstBubbleText;
    [SerializeField] private Image speechBubbleImage;
    [SerializeField] private TextMeshProUGUI bubbleText;

    public string[] messages;
    public float waitTime = 1f;
    private int currentMessage = 0;
    private bool firstLoad = true;

    // Initialize conversation
    public void StartDialogue()
    {
        // Activate game objects
        firstSpeechBubbleImage.gameObject.SetActive(true);
        firstBubbleText.gameObject.SetActive(true);
        StartCoroutine(DisplayConversation());
    }

    IEnumerator DisplayConversation()
    {
        // If starting conversation for the first time, wait for 4 seconds before proceeding
        if (firstLoad)
        {
            firstLoad = false;
            yield return new WaitForSeconds(2f);
            // Hide first speech bubble
            firstSpeechBubbleImage.enabled = false;
            firstBubbleText.enabled = false;
            yield return new WaitForSeconds(2f);
        }

        // Activate second text bubble 
        speechBubbleImage.gameObject.SetActive(true);
        bubbleText.gameObject.SetActive(true);

        bubbleText.text = messages[currentMessage];

        yield return new WaitForSeconds(waitTime);
        NextMessage();
    }

    // Load next message to the text bubble
    public void NextMessage()
    {
        if (currentMessage < messages.Length - 1)
        {
            currentMessage++;
            StartCoroutine(DisplayConversation());
        }
        // If no more messages, hide speech bubble
        else
        {
            bubbleText.enabled = false;
            speechBubbleImage.enabled = false;
        }
    }
}
