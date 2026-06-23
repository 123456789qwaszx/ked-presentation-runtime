using System.Collections.Generic;
using Yarn.Unity;
using UnityEngine;

// Owns option item instances, the interactive selection session,
// and DialoguePresenterBase adapter work.
// Transaction ordering lives in VNOptionsPresentationFlow.
public sealed partial class VNOptionsPresenter
    : DialoguePresenterBase
{
    private VNOptionItem _optionItemPrefab;
    private VNOptionsPresentationFlow _flow;

    private readonly List<VNOptionItem> _activeItems = new();

    private VNOptionSelectionSession _selectionSession;
    private string _currentNodeName;

    public void Initialize(
        DialogueRunner dialogueRunner,
        VNOptionsPresentationFlow flow,
        VNOptionItem optionItem)
    {
        _flow = flow;
        _optionItemPrefab = optionItem;

        dialogueRunner.onNodeStart?.AddListener(HandleNodeStarted);
    }
    
    private void HandleNodeStarted(string nodeName) => _currentNodeName = nodeName;

    public override YarnTask OnDialogueStartedAsync()
    {
        EndSelectionSession();
        DestroyActiveItems();
        _flow?.EndInteractiveImmediate();

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        EndSelectionSession();
        DestroyActiveItems();
        _flow?.EndInteractiveImmediate();

        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(
        LocalizedLine line,
        LineCancellationToken token)
    {
        return YarnTask.CompletedTask;
    }

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

        VNOptionsPresentationBeginResult beginResult = await _flow.BeginAsync(ctx);

        if (beginResult == VNOptionsPresentationBeginResult.NoOption)
            return await DialogueRunner.NoOptionSelected;

        if (beginResult == VNOptionsPresentationBeginResult.ReplayResolved)
            return ctx.SelectedOption ?? await DialogueRunner.NoOptionSelected;

        try
        {
            if (!PrepareInteractiveItems(ctx))
                return await DialogueRunner.NoOptionSelected;

            VNOptionViewModel selected = await AwaitSelectionAsync(ctx);

            if (cancellationToken.IsNextContentRequested || selected == null)
                return await DialogueRunner.NoOptionSelected;

            _flow.CommitSelection(ctx, selected);

            return ctx.SelectedOption ?? await DialogueRunner.NoOptionSelected;
        }
        finally
        {
            await CleanupInteractiveAsync(ctx);
            _flow.EndInteractiveImmediate();
        }
    }

    private bool PrepareInteractiveItems(VNOptionsPresentationContext ctx)
    {
        EndSelectionSession();
        DestroyActiveItems();

        IPresentationOptionsBoxView boxView = ctx.OptionsBoxView;

        boxView.SetInputEnabled(false);

        _selectionSession = new VNOptionSelectionSession(ctx.Token);

        CreateItems(ctx.ViewModels, boxView.ItemContainer);

        boxView.SetInputEnabled(true);
        SelectFirstAvailableItem();

        return true;
    }

    private async YarnTask<VNOptionViewModel> AwaitSelectionAsync(VNOptionsPresentationContext ctx)
    {
        if (_selectionSession == null)
            return null;

        VNOptionViewModel selected = await _selectionSession.Task;

        ctx.OptionsBoxView?.SetInputEnabled(false);

        return selected;
    }

    private async YarnTask CleanupInteractiveAsync(VNOptionsPresentationContext ctx)
    {
        ctx.OptionsBoxView?.SetInputEnabled(false);

        EndSelectionSession();
        DestroyActiveItems();

        await YarnTask.Yield();
    }

    private bool CreateItems(List<VNOptionViewModel> viewModels, RectTransform container)
    {
        for (int i = 0; i < viewModels.Count; i++)
        {
            VNOptionItem item = Instantiate(_optionItemPrefab, container);

            item.Submitted += HandleItemSubmitted;
            item.Bind(viewModels[i]);
            item.SetRevealAlpha(1f);

            _activeItems.Add(item);
        }

        return _activeItems.Count > 0;
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