using System.Collections.Generic;
using Yarn.Unity;
using UnityEngine;

// Owns option item instances, interactive selection session, and DialoguePresenterBase adapter work.
// Transaction ordering, seek replay, and choice commit live in VNOptionsPresentationFlow.
public sealed partial class VNOptionsPresenter : DialoguePresenterBase
{
    [Header("References")]
    [SerializeField] private VNOptionItem _optionItemPrefab;
    private VNOptionsBoxPresentationController _boxPresentation;

    private VNOptionsPresentationFlow _flow;

    private readonly List<VNOptionItem> _activeItems = new();

    private VNOptionSelectionSession _selectionSession;
    private string _currentNodeName;

    public void Initialize(
        DialogueRunner dialogueRunner,
        VNOptionsPresentationFlow flow,
        VNOptionItem optionItem,
        VNOptionsBoxPresentationController boxPresentation)
    {
        _flow = flow;
        _optionItemPrefab = optionItem;
        _boxPresentation = boxPresentation;
        dialogueRunner.onNodeStart?.AddListener(HandleNodeStarted);
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        EndSelectionSession();
        DestroyActiveItems();
        _boxPresentation.HideImmediate();

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        EndSelectionSession();
        DestroyActiveItems();
        _boxPresentation.HideImmediate();

        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token) 
    { return YarnTask.CompletedTask; }

    public override async YarnTask<DialogueOption> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken cancellationToken)
    {
        VNOptionsPresentationContext ctx = new()
        {
            SourceOptions = dialogueOptions,
            Token = cancellationToken,
            NodeName = _currentNodeName,
        };

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
            EndSelectionSession();
        }
    }

    private void HandleNodeStarted(string nodeName)
    {
        _currentNodeName = nodeName;
    }

    private void PrepareInteractiveItems(VNOptionsPresentationContext ctx)
    {
        EndSelectionSession();
        DestroyActiveItems();

        if (ctx.BoxResult == null || !ctx.BoxResult.IsValid)
            return;

        IPresentationOptionsBoxView boxView = ctx.BoxResult.View;
        boxView.SetInputEnabled(false);

        _selectionSession = new VNOptionSelectionSession(ctx.Token);

        CreateItems(ctx.ViewModels, ctx.BoxResult.ItemContainer);

        boxView.SetInputEnabled(true);
        SelectFirstAvailableItem();
    }

    private async YarnTask<VNOptionViewModel> AwaitInteractiveSelectionAsync(VNOptionsPresentationContext ctx)
    {
        if (_selectionSession == null)
            return null;

        VNOptionViewModel selected = await _selectionSession.Task;

        if (ctx.BoxResult != null && ctx.BoxResult.View != null)
            ctx.BoxResult.View.SetInputEnabled(false);

        return selected;
    }

    private async YarnTask CleanupInteractiveAsync(VNOptionsPresentationContext ctx)
    {
        EndSelectionSession();
        DestroyActiveItems();

        if (ctx.BoxResult != null && ctx.BoxResult.View != null)
        {
            ctx.BoxResult.View.SetInputEnabled(false);
            ctx.BoxResult.View.SetVisibleImmediate(false);
        }

        await YarnTask.Yield();
    }

    private void CreateItems(List<VNOptionViewModel> viewModels, RectTransform container)
    {
        for (int i = 0; i < viewModels.Count; i++)
        {
            VNOptionItem item = Instantiate(_optionItemPrefab, container);

            item.Submitted += HandleItemSubmitted;
            item.Bind(viewModels[i]);
            item.SetRevealAlpha(1f);

            _activeItems.Add(item);
        }
    }

    private void DestroyActiveItems()
    {
        for (int i = 0; i < _activeItems.Count; i++)
        {
            VNOptionItem item = _activeItems[i];

            if (!item)
                continue;

            item.Submitted -= HandleItemSubmitted;
            item.ResetView();

            Destroy(item.gameObject);
        }

        _activeItems.Clear();
    }

    private void HandleItemSubmitted(VNOptionItem item)
    {
        if (item == null)
            return;

        if (!item.HasViewModel)
            return;

        _selectionSession?.TrySubmit(item.ViewModel);
    }

    private void EndSelectionSession()
    {
        _selectionSession?.Dispose();
        _selectionSession = null;
    }

    private void SelectFirstAvailableItem()
    {
        for (int i = 0; i < _activeItems.Count; i++)
        {
            VNOptionItem item = _activeItems[i];

            if (item != null && item.isActiveAndEnabled && item.IsInteractable())
            {
                item.Select();
                return;
            }
        }
    }
}