using TMPro;
using UnityEngine;
using Yarn.Unity.Samples;

public sealed class MessengerBubbleView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject typingIndicator;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Policy")]
    [SerializeField] private bool showSpeakerName = false;

    public bool HasIndicator => typingIndicator != null;

    public void ShowTyping(string speaker)
    {
        if (typingIndicator != null)
            typingIndicator.SetActive(true);

        if (speakerText != null)
        {
            bool visible = showSpeakerName && !string.IsNullOrEmpty(speaker);
            speakerText.gameObject.SetActive(visible);

            if (visible)
                speakerText.text = speaker;
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }
    }

    public void ShowText(string speaker, string text)
    {
        if (typingIndicator != null)
            typingIndicator.SetActive(false);

        if (speakerText != null)
        {
            bool visible = showSpeakerName && !string.IsNullOrEmpty(speaker);
            speakerText.gameObject.SetActive(visible);

            if (visible)
                speakerText.text = speaker;
        }

        if (bodyText != null)
        {
            bodyText.SetTextWrapping(true);
            bodyText.text = text ?? string.Empty;
        }
    }
}