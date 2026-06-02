using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

// Owns the option item pool, the selection source, and cancellation lifetime.
// All transaction ordering, the selection decision, and the choice commit live in VNOptionsPresentationFlow;
// this presenter only supplies the pool/UI work the flow delegates back to it.
public sealed partial class VNOptionsPresenter : DialoguePresenterBase
{
    [Header("References")]
    [SerializeField] private VNOptionItem optionItemPrefab;
    [SerializeField] private VNOptionsBoxPresentationController boxPresentation;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private VNOptionsPresentationFlow _flow;

    private readonly List<VNOptionItem> _pool = new ();

    private string _currentNodeName;

    private YarnTaskCompletionSource<VNOptionViewModel> _selectionSource;
    private CancellationToken _completionToken;

    public void Initialize(DialogueRunner dialogueRunner, RollbackController rollbackHistory, ChoiceHistory choiceHistory, VNLinePresentationState linePresentationState)
    {
        _flow = new VNOptionsPresentationFlow(
            boxPresentation,
            linePresentationState,
            choiceHistory,
            rollbackHistory);

        dialogueRunner.onNodeStart?.AddListener((nodeName) => _currentNodeName = nodeName);
    }

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
        var ctx = new VNOptionsPresentationContext {
            SourceOptions = dialogueOptions,
            Token = cancellationToken,
            NodeName = _currentNodeName,
        };

        var internalCancel = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken.NextContentToken);

        _completionToken = internalCancel.Token;

        try
        {
            DialogueOption selected = await _flow.RunAsync(
                ctx,
                prepareItems: PrepareInteractiveItems,
                awaitSelection: AwaitInteractiveSelectionAsync,
                cleanup: CleanupInteractiveAsync,
                shouldFastForward: () => false);

            if (selected == null)
                return await DialogueRunner.NoOptionSelected;

            return selected;
        }
        finally
        {
            internalCancel.Dispose();
            _completionToken = default(CancellationToken);
        }
    }

    private void PrepareInteractiveItems(VNOptionsPresentationContext ctx)
    {
        List<VNOptionViewModel> viewModels = ctx.ViewModels;

        EnsurePoolCapacity(viewModels.Count, ctx.BoxResult.ItemContainer);

        _selectionSource = new YarnTaskCompletionSource<VNOptionViewModel>();
        MonitorExternalCancellation(ctx.Token, _selectionSource, _completionToken).Forget();

        BindItems(viewModels);

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        for (int i = 0; i < viewModels.Count; i++)
        {
            _pool[i].SetRevealAlpha(1f);
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        SelectFirstAvailableItem(viewModels.Count);
    }

    private async YarnTask<VNOptionViewModel> AwaitInteractiveSelectionAsync(VNOptionsPresentationContext ctx)
    {
        VNOptionViewModel selected = await _selectionSource.Task;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        return selected;
    }

    private async YarnTask CleanupInteractiveAsync(VNOptionsPresentationContext ctx)
    {
        _selectionSource = null;

        HideAllItems();
        HidePanelImmediate();

        await YarnTask.Yield();
    }

    private void EnsurePoolCapacity(int requiredCount, RectTransform container)
    {
        while (_pool.Count < requiredCount)
        {
            VNOptionItem item = Instantiate(optionItemPrefab, container);

            item.gameObject.SetActive(false);
            item.ResetView();

            _pool.Add(item);
        }
    }

    private void BindItems(List<VNOptionViewModel> viewModels)
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

        _selectionSource.TrySetResult(item.ViewModel);
    }

    private async YarnTask MonitorExternalCancellation(
        LineCancellationToken token,
        YarnTaskCompletionSource<VNOptionViewModel> source,
        CancellationToken internalToken)
    {
        await YarnTask.WaitUntilCanceled(internalToken);

        if (token.IsNextContentRequested)
            source.TrySetResult(null);
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

    private void HidePanelImmediate()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        SetAccumulatedStatusText(string.Empty);
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
}