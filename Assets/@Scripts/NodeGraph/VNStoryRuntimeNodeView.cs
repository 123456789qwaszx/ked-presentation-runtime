using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class VNStoryRuntimeNodeView : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Button button;

    private VNStoryGraphNodeViewModel _model;
    private VNStoryGraphRuntimeUIBuilder _owner;

    public VNStoryGraphNodeViewModel Model
    {
        get { return _model; }
    }

    public void Bind(
        VNStoryGraphNodeViewModel model,
        VNStoryGraphRuntimeUIBuilder owner)
    {
        _model = model;
        _owner = owner;

        EnsureRefs();

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = model.position;
            rectTransform.sizeDelta = model.size;
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = model.sprite;
            backgroundImage.color = model.color;
            backgroundImage.raycastTarget = true;
        }

        if (labelText != null)
            labelText.text = model.displayText;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.interactable = model.clickable;
        }

        gameObject.SetActive(model.visible);
    }

    private void HandleClick()
    {
        if (_owner == null || _model == null)
            return;

        _owner.NotifyNodeClicked(_model);
    }

    private void EnsureRefs()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>(true);
    }
}