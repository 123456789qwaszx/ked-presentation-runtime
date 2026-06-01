using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public sealed class VNOptionsPresenter : DialoguePresenterBase
{
    [Header("Accumulated Status")] [SerializeField]
    private TextMeshProUGUI _accumulatedStatusText;

    [Tooltip("If true, the accumulated status text is hidden when there are no tracked stats.")] [SerializeField]
    private bool _hideAccumulatedStatusWhenEmpty = true;

    [Tooltip(
        "Optional. If assigned, this runner is used to read Yarn variables. Otherwise the presenter tries to resolve it from option.Line.Source.")]
    [SerializeField]
    private DialogueRunner _dialogueRunner;

    [Header("References")] [SerializeField]
    private VNOptionItem _optionItemPrefab;

    [SerializeField] private RectTransform _optionContainer;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Options")] [SerializeField] private bool _showUnavailableOptions = false;

    [Header("Sequential Reveal")] [SerializeField]
    private float _revealStaggerSeconds = 0.12f;

    [SerializeField] private float _itemFadeInSeconds = 0.18f;

    [Header("Dismiss")] [SerializeField] private float _dismissFadeSeconds = 0.15f;

    private readonly List<VNOptionItem> _pool = new List<VNOptionItem>();

    private YarnTaskCompletionSource<DialogueOption> _selectionSource;
    private CancellationToken _completionToken;

    public override YarnTask OnDialogueStartedAsync()
    {
        HidePanelImmediate();
        HideAllItems();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        HidePanelImmediate();
        HideAllItems();
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        return YarnTask.CompletedTask;
    }

    public override async YarnTask<DialogueOption> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken cancellationToken)
    {
        if (!AnyOptionAvailable(dialogueOptions))
            return await DialogueRunner.NoOptionSelected;

        List<VNOptionViewModel> viewModels = BuildViewModels(dialogueOptions);

        if (viewModels.Count == 0)
            return await DialogueRunner.NoOptionSelected;

        RefreshAccumulatedStatus(viewModels);

        EnsurePoolSize(viewModels.Count);

        var internalCancel = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken.NextContentToken);

        _selectionSource = new YarnTaskCompletionSource<DialogueOption>();
        _completionToken = internalCancel.Token;

        try
        {
            MonitorExternalCancellation(cancellationToken, _selectionSource, internalCancel.Token).Forget();

            PrepareItems(viewModels);

            ShowPanelForReveal();
            SetPanelInteractable(false);

            await RevealItemsSequentiallyAsync(viewModels.Count, cancellationToken.HurryUpToken);

            SetPanelInteractable(true);
            SelectFirstAvailableItem(viewModels.Count);

            DialogueOption selected = await _selectionSource.Task;

            internalCancel.Cancel();

            SetPanelInteractable(false);
            await FadePanelAsync(1f, 0f, _dismissFadeSeconds, cancellationToken.HurryUpToken);

            if (cancellationToken.IsNextContentRequested)
                return await DialogueRunner.NoOptionSelected;

            return selected;
        }
        finally
        {
            internalCancel.Dispose();

            _selectionSource = null;
            _completionToken = default(CancellationToken);

            HideAllItems();
            HidePanelImmediate();

            await YarnTask.Yield();
        }
    }

    private List<VNOptionViewModel> BuildViewModels(DialogueOption[] dialogueOptions)
    {
        var result = new List<VNOptionViewModel>();

        for (int i = 0; i < dialogueOptions.Length; i++)
        {
            DialogueOption option = dialogueOptions[i];

            if (!option.IsAvailable && !_showUnavailableOptions)
                continue;

            result.Add(VNOptionViewModelBuilder.Build(option));
        }

        return result;
    }

    private void PrepareItems(List<VNOptionViewModel> viewModels)
    {
        for (int i = 0; i < viewModels.Count; i++)
        {
            VNOptionItem item = _pool[i];

            item.gameObject.SetActive(true);
            item.Submitted -= HandleItemSubmitted;
            item.Submitted += HandleItemSubmitted;

            item.Bind(viewModels[i]);
            item.SetRevealAlpha(0f);
        }

        for (int i = viewModels.Count; i < _pool.Count; i++)
        {
            _pool[i].Submitted -= HandleItemSubmitted;
            _pool[i].ResetView();
            _pool[i].gameObject.SetActive(false);
        }
    }

    private void HandleItemSubmitted(VNOptionItem item)
    {
        if (_completionToken.IsCancellationRequested)
            return;

        if (item == null || !item.HasViewModel)
            return;

        if (_selectionSource == null)
            return;

        _selectionSource.TrySetResult(item.ViewModel.SourceOption);
    }

    private async YarnTask RevealItemsSequentiallyAsync(int count, CancellationToken hurryUp)
    {
        for (int i = 0; i < count; i++)
        {
            if (hurryUp.IsCancellationRequested)
            {
                for (int j = i; j < count; j++)
                    _pool[j].SetRevealAlpha(1f);

                return;
            }

            await FadeItemAsync(_pool[i], 0f, 1f, _itemFadeInSeconds, hurryUp);

            if (i < count - 1)
                await WaitAsync(_revealStaggerSeconds, hurryUp);
        }
    }

    private async YarnTask FadeItemAsync(
        VNOptionItem item,
        float from,
        float to,
        float duration,
        CancellationToken cancel)
    {
        if (duration <= 0f)
        {
            item.SetRevealAlpha(to);
            return;
        }

        float elapsed = 0f;
        item.SetRevealAlpha(from);

        while (elapsed < duration)
        {
            if (cancel.IsCancellationRequested)
            {
                item.SetRevealAlpha(to);
                return;
            }

            elapsed += Time.deltaTime;
            item.SetRevealAlpha(Mathf.Lerp(from, to, elapsed / duration));
            await YarnTask.Yield();
        }

        item.SetRevealAlpha(to);
    }

    private async YarnTask FadePanelAsync(
        float from,
        float to,
        float duration,
        CancellationToken cancel)
    {
        if (_canvasGroup == null)
            return;

        if (duration <= 0f)
        {
            _canvasGroup.alpha = to;
            return;
        }

        float elapsed = 0f;
        _canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            if (cancel.IsCancellationRequested)
            {
                _canvasGroup.alpha = to;
                return;
            }

            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            await YarnTask.Yield();
        }

        _canvasGroup.alpha = to;
    }

    private async YarnTask WaitAsync(float seconds, CancellationToken cancel)
    {
        if (seconds <= 0f)
            return;

        float elapsed = 0f;

        while (elapsed < seconds)
        {
            if (cancel.IsCancellationRequested)
                return;

            elapsed += Time.deltaTime;
            await YarnTask.Yield();
        }
    }

    private async YarnTask MonitorExternalCancellation(
        LineCancellationToken token,
        YarnTaskCompletionSource<DialogueOption> source,
        CancellationToken internalToken)
    {
        await YarnTask.WaitUntilCanceled(internalToken);

        if (token.IsNextContentRequested)
            source.TrySetResult(null);
    }

    private void EnsurePoolSize(int required)
    {
        while (_pool.Count < required)
        {
            if (_optionItemPrefab == null)
            {
                Debug.LogError($"{nameof(VNOptionsPresenter)}: option item prefab is not assigned.", this);
                return;
            }

            Transform parent = _optionContainer != null ? _optionContainer : transform;
            VNOptionItem item = Instantiate(_optionItemPrefab, parent);

            item.gameObject.SetActive(false);
            item.ResetView();

            _pool.Add(item);
        }
    }

    private void HideAllItems()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            VNOptionItem item = _pool[i];

            item.Submitted -= HandleItemSubmitted;
            item.ResetView();
            item.gameObject.SetActive(false);
        }
    }

    private void ShowPanelForReveal()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void HidePanelImmediate()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        SetAccumulatedStatusText(string.Empty);
    }

    private void SetPanelInteractable(bool interactable)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.interactable = interactable;
        _canvasGroup.blocksRaycasts = interactable;
    }

    private void SelectFirstAvailableItem(int activeCount)
    {
        for (int i = 0; i < activeCount; i++)
        {
            VNOptionItem item = _pool[i];

            if (item.isActiveAndEnabled && item.IsInteractable())
            {
                item.Select();
                return;
            }
        }
    }

    private static bool AnyOptionAvailable(DialogueOption[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i].IsAvailable)
                return true;
        }

        return false;
    }

    private void RefreshAccumulatedStatus(List<VNOptionViewModel> viewModels)
    {
        if (_accumulatedStatusText == null)
            return;

        DialogueRunner runner = ResolveDialogueRunner(viewModels);

        if (runner == null || runner.VariableStorage == null)
        {
            SetAccumulatedStatusText(string.Empty);
            return;
        }

        List<string> statKeys = CollectStatKeys(viewModels);

        if (statKeys.Count == 0)
        {
            SetAccumulatedStatusText(string.Empty);
            return;
        }

        List<string> parts = new List<string>();

        for (int i = 0; i < statKeys.Count; i++)
        {
            string statKey = statKeys[i];
            float value = ReadYarnNumber(runner, statKey);

            string displayName = VNOptionEffectDisplayNameResolver.Resolve(statKey);
            parts.Add(string.Format("{0} {1}", displayName, FormatNumber(value)));
        }

        SetAccumulatedStatusText("현재 누적  " + string.Join(" / ", parts));
    }

    private DialogueRunner ResolveDialogueRunner(List<VNOptionViewModel> viewModels)
    {
        if (_dialogueRunner != null)
            return _dialogueRunner;

        if (viewModels != null)
        {
            for (int i = 0; i < viewModels.Count; i++)
            {
                VNOptionViewModel viewModel = viewModels[i];

                if (viewModel == null)
                    continue;

                if (viewModel.SourceOption == null)
                    continue;

                if (viewModel.SourceOption.Line == null)
                    continue;

                DialogueRunner runner = viewModel.SourceOption.Line.Source as DialogueRunner;

                if (runner != null)
                    return runner;
            }
        }

        return null;
    }

    private static List<string> CollectStatKeys(List<VNOptionViewModel> viewModels)
    {
        var result = new List<string>();

        if (viewModels == null)
            return result;

        for (int i = 0; i < viewModels.Count; i++)
        {
            VNOptionViewModel viewModel = viewModels[i];

            if (viewModel == null || viewModel.Effects == null)
                continue;

            for (int j = 0; j < viewModel.Effects.Count; j++)
            {
                string statKey = viewModel.Effects[j].StatKey;

                if (string.IsNullOrEmpty(statKey))
                    continue;

                if (!result.Contains(statKey))
                    result.Add(statKey);
            }
        }

        return result;
    }

    private static float ReadYarnNumber(DialogueRunner runner, string statKey)
    {
        if (runner == null || runner.VariableStorage == null)
            return 0f;

        string variableName = statKey.StartsWith("$") ? statKey : "$" + statKey;

        float floatValue;
        if (runner.VariableStorage.TryGetValue<float>(variableName, out floatValue))
            return floatValue;

        return 0f;
    }

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }

    private void SetAccumulatedStatusText(string text)
    {
        if (_accumulatedStatusText == null)
            return;

        _accumulatedStatusText.text = text ?? string.Empty;

        if (_hideAccumulatedStatusWhenEmpty)
            _accumulatedStatusText.gameObject.SetActive(!string.IsNullOrEmpty(_accumulatedStatusText.text));
    }
}