using UnityEngine;
using UnityEngine.UI;

public sealed class EmojiPopupView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image emojiImage;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        Hide();
    }

    public void Show(Sprite sprite)
    {
        if (emojiImage != null)
            emojiImage.sprite = sprite;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }
}