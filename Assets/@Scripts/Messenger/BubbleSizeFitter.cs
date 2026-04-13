using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class BubbleSizeFitter : MonoBehaviour, ILayoutElement
{
    [Header("Refs")]
    [SerializeField] private TMP_Text targetText;

    [Header("Size")]
    [SerializeField] private float minimumWidth = 100f;
    [SerializeField] private float minimumHeight = 30f;
    [SerializeField] private float maximumWidth = 520f;
    [SerializeField] private float extraWidthPadding = 8f;
    
    private RectTransform _rectTransform;
    private RectTransform RectTransform =>  _rectTransform ??= GetComponent<RectTransform>();

    public float minWidth { get; private set; }
    public float minHeight { get; private set; }

    float ILayoutElement.preferredWidth => minWidth;
    float ILayoutElement.preferredHeight => minHeight;
    float ILayoutElement.flexibleWidth => 0f;
    float ILayoutElement.flexibleHeight => 0f;
    int ILayoutElement.layoutPriority => 0;

    private void OnEnable()
    {
        if (targetText == null)
            return;

        targetText.OnPreRenderText += UpdateLayout;
        UpdateLayout(targetText.textInfo);
    }

    private void OnDisable()
    {
        if (targetText == null)
            return;

        targetText.OnPreRenderText -= UpdateLayout;
    }

    private void UpdateLayout(TMP_TextInfo info)
    {
        if (targetText == null)
        {
            minWidth = minimumWidth;
            minHeight = minimumHeight;
            return;
        }

        if (info == null || info.textComponent == null || string.IsNullOrEmpty(info.textComponent.text))
        {
            minWidth = minimumWidth;
            minHeight = minimumHeight;
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            return;
        }

        SetTextWrapping(targetText, true);

        float parentWidth = maximumWidth;
        RectTransform parentRect = RectTransform.parent as RectTransform;
        if (parentRect != null)
        {
            parentWidth = Mathf.Min(parentRect.rect.width, maximumWidth);
        }

        float xMargin = targetText.margin.x + targetText.margin.z;
        float availableWidth = Mathf.Max(0f, parentWidth - xMargin);

        Vector2 preferred = targetText.GetPreferredValues(
            targetText.text,
            availableWidth,
            float.MaxValue);

        minWidth = Mathf.Max(minimumWidth, preferred.x + extraWidthPadding);
        minHeight = Mathf.Max(minimumHeight, preferred.y);

        LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
    }

    void ILayoutElement.CalculateLayoutInputHorizontal() { }
    void ILayoutElement.CalculateLayoutInputVertical() { }

    private void OnValidate()
    {
        UpdateLayout(targetText != null ? targetText.textInfo : null);
    }
    
    private static void SetTextWrapping(TMP_Text text, bool enabled)
    {
#if UNITY_6000_0_OR_NEWER
        text.textWrappingMode = TextWrappingModes.Normal;
#else
            text.enableWordWrapping = true;
#endif
    }
}