using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class VNOptionItem : Selectable, ISubmitHandler, IPointerClickHandler
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

    private VNOptionViewModel _viewModel;
    private bool _hasSubmitted;

    private float _revealAlpha;
    private float _stateAlpha;

    public event Action<VNOptionItem> Submitted;

    public bool HasViewModel => _viewModel != null;

    public VNOptionViewModel ViewModel
    {
        get
        {
            if (_viewModel == null)
                throw new InvalidOperationException("VNOptionItem has no bound view model.");

            return _viewModel;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        ResetView();
    }

    public void Bind(VNOptionViewModel viewModel)
    {
        if (viewModel == null)
        {
            ResetView();
            return;
        }

        _viewModel = viewModel;
        _hasSubmitted = false;

        SetText(_label, viewModel.Label);
        SetEffectText(viewModel.EffectText);

        interactable = viewModel.IsAvailable;

        _revealAlpha = 1f;
        SetHighlighted(false);
    }

    public void ResetView()
    {
        _viewModel = null;
        _hasSubmitted = false;

        interactable = false;

        SetText(_label, string.Empty);
        ClearEffectText();

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
        return _viewModel != null &&
               _viewModel.IsAvailable &&
               IsInteractable();
    }

    private void SetHighlighted(bool highlighted)
    {
        bool canShowSelection = _viewModel != null && _viewModel.IsAvailable;

        SetSelectionIndicator(highlighted && canShowSelection);

        _stateAlpha = highlighted && canShowSelection
            ? _hoveredAlpha
            : GetRestingStateAlpha();

        ApplyAlpha();
    }

    private float GetRestingStateAlpha()
    {
        if (_viewModel == null)
            return 0f;

        return _viewModel.IsAvailable
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

    private void SetEffectText(string effectText)
    {
        if (_effectText == null)
            return;

        effectText ??= string.Empty;

        _effectText.text = effectText;
        _effectText.gameObject.SetActive(!string.IsNullOrEmpty(effectText));
    }

    private void ClearEffectText()
    {
        SetEffectText(string.Empty);
    }

    private void SetSelectionIndicator(bool active)
    {
        if (_selectionIndicator != null)
            _selectionIndicator.SetActive(active);
    }
}