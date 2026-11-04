using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CustomOptionItem : Selectable, ISubmitHandler, IPointerClickHandler
{
    [Header("Text")]
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private TextMeshProUGUI _effectText;

    [Header("Visual State")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _selectionIndicator;

    [Header("Appearance")]
    [SerializeField] private float _disabledAlpha = 0.35f;
    [SerializeField] private float _normalAlpha = 0.75f;
    [SerializeField] private float _hoveredAlpha = 1.0f;

    // 진행 층의 에피소드 선택지(ChapterOptionsView)가 묶는 값 — 문구와 고를 수 있는지.
    // ⛔ Yarn 옵션 뷰모델(OptionViewModel)은 대사 안 선택지와 함께 2026-11-05에 걷혔다.
    private bool _isBound;
    private bool _isAvailable;
    private bool _hasSubmitted;

    private float _revealAlpha;
    private float _stateAlpha;

    public event Action<CustomOptionItem> Submitted;

    public bool IsBound => _isBound;

    protected override void Awake()
    {
        base.Awake();
        ResetView();
    }

    // 진행 층의 에피소드 선택지(ChapterOptionsView)가 묶는다.
    public void Bind(string label, bool isAvailable, string effectText = null)
    {
        _isBound = true;
        _isAvailable = isAvailable;
        _hasSubmitted = false;

        SetText(_label, label ?? string.Empty);
        SetText(_effectText, effectText);

        interactable = isAvailable;

        _revealAlpha = 1f;
        SetHighlighted(false);
    }

    public void ResetView()
    {
        _isBound = false;
        _isAvailable = false;
        _hasSubmitted = false;

        interactable = false;

        SetText(_label, string.Empty);
        SetText(_effectText, string.Empty);

        _revealAlpha = 0f;
        _stateAlpha = 0f;

        SetSelectionIndicator(false);
        ApplyAlpha();
    }

    public void SetRevealAlpha(float alpha)
    {
        _revealAlpha = Mathf.Clamp01(alpha);
        ApplyAlpha();
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);

        if (!CanInteract())
            return;

        SetHighlighted(true);
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        SetHighlighted(false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);

        if (CanInteract())
            Select();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TrySubmit();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        TrySubmit();
    }

    private void TrySubmit()
    {
        if (!CanInteract())
            return;

        if (_hasSubmitted)
            return;

        _hasSubmitted = true;
        Submitted?.Invoke(this);
    }

    private bool CanInteract()
    {
        return _isBound &&
               _isAvailable &&
               IsInteractable();
    }

    private void SetHighlighted(bool highlighted)
    {
        bool canShowSelection = _isBound && _isAvailable;

        SetSelectionIndicator(highlighted && canShowSelection);

        _stateAlpha = highlighted && canShowSelection
            ? _hoveredAlpha
            : GetRestingStateAlpha();

        ApplyAlpha();
    }

    private float GetRestingStateAlpha()
    {
        if (!_isBound)
            return 0f;

        return _isAvailable
            ? _normalAlpha
            : _disabledAlpha;
    }

    private void ApplyAlpha()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = _revealAlpha * _stateAlpha;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private void SetSelectionIndicator(bool active)
    {
        if (_selectionIndicator != null)
            _selectionIndicator.SetActive(active);
    }
}