using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed class VNOptionsPresenter : DialoguePresenterBase
{
    [Header("References")]
    [SerializeField] private VNOptionItem _optionItemPrefab;
    [SerializeField] private RectTransform _optionContainer;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Options")]
    [SerializeField] private bool _showUnavailableOptions = false;

    [Header("Sequential Reveal")]
    [SerializeField] private float _revealStaggerSeconds = 0.12f;
    [SerializeField] private float _itemFadeInSeconds = 0.18f;

    [Header("Dismiss")]
    [SerializeField] private float _dismissFadeSeconds = 0.15f;

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
}