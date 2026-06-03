using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class VNOptionItem : Selectable, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
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
    private bool _hasViewModel;
    private bool _hasSubmitted;

    public event Action<VNOptionItem> Submitted;

    public bool HasViewModel
    {
        get { return _hasViewModel; }
    }

    public VNOptionViewModel ViewModel
    {
        get
        {
            if (!_hasViewModel)
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
        _viewModel = viewModel;
        _hasViewModel = true;
        _hasSubmitted = false;

        if (_label != null)
            _label.text = viewModel.Label;

        ApplyEffectText(viewModel);

        interactable = viewModel.IsAvailable;

        SetSelectionIndicator(false);
        ApplyStateAlpha();
    }

    public void ResetView()
    {
        _hasViewModel = false;
        _hasSubmitted = false;
        interactable = false;

        if (_label != null)
            _label.text = string.Empty;

        if (_effectText != null)
        {
            _effectText.text = string.Empty;
            _effectText.gameObject.SetActive(false);
        }

        SetSelectionIndicator(false);
        SetAlpha(0f);
    }

    public void SetRevealAlpha(float alpha)
    {
        SetAlpha(alpha);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);

        if (_hasViewModel && ViewModel.IsAvailable)
        {
            SetAlpha(_hoveredAlpha);
            SetSelectionIndicator(true);
        }
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);

        ApplyStateAlpha();
        SetSelectionIndicator(false);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        Select();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        TrySubmit();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TrySubmit();
    }

    private void TrySubmit()
    {
        if (!IsInteractable())
            return;

        if (!_hasViewModel)
            return;

        if (_hasSubmitted)
            return;

        _hasSubmitted = true;

        if (Submitted != null)
            Submitted.Invoke(this);
    }

    private void ApplyEffectText(VNOptionViewModel viewModel)
    {
        if (_effectText == null)
            return;

        string effectText = BuildEffectText(viewModel);

        _effectText.text = effectText;
        _effectText.gameObject.SetActive(!string.IsNullOrEmpty(effectText));
    }

    private static string BuildEffectText(VNOptionViewModel viewModel)
    {
        if (viewModel == null)
            return string.Empty;

        if (viewModel.Effects == null || viewModel.Effects.Count == 0)
            return string.Empty;

        var parts = new List<string>();

        for (int i = 0; i < viewModel.Effects.Count; i++)
        {
            string text = viewModel.Effects[i].ToDisplayText();

            if (!string.IsNullOrEmpty(text))
                parts.Add(text);
        }

        return string.Join(" / ", parts);
    }

    private void ApplyStateAlpha()
    {
        if (!_hasViewModel)
        {
            SetAlpha(0f);
            return;
        }

        SetAlpha(ViewModel.IsAvailable ? _normalAlpha : _disabledAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    private void SetSelectionIndicator(bool active)
    {
        if (_selectionIndicator != null)
            _selectionIndicator.SetActive(active);
    }
}