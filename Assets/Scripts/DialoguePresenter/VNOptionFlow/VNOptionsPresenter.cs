using System.Collections.Generic;
using Yarn.Unity;
using UnityEngine;

// Owns option item instances, the interactive selection session,
// and DialoguePresenterBase adapter work.
// Transaction ordering lives in VNOptionsPresentationFlow.
public sealed class VNOptionsPresenter
    : DialoguePresenterBase
{
    private VNOptionItem _optionItemPrefab;
    private VNOptionsPresentationFlow _flow;

    private readonly List<VNOptionItem> _activeItems = new();

    private VNOptionSelectionSession _selectionSession;
    private string _currentNodeName;

    // 진행 중인 RunOptionsAsync가 실제로 빠져나올 때까지 기다리기 위한 통로.
    // 이게 없으면 정지 이후에도 옵션 태스크가 살아남아,
    // 다음 StartDialogue가 만든 새 대화에 SetSelectedOption/Continue를 씀.
    private YarnTaskCompletionSource _optionsDrain;

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

    // DialogueRunner는 Stop()을 끝내기 전에 이 태스크를 기다림.
    // 먼저 세션을 끝내 대기를 풀고(Dispose가 TrySetResult(null)을 한다),
    // 그 다음 RunOptionsAsync가 빠져나올 때까지 대기.
    public override async YarnTask OnDialogueCompleteAsync()
    {
        EndSelectionSession();
        DestroyActiveItems();
        _flow?.EndInteractiveImmediate();

        YarnTaskCompletionSource drain = _optionsDrain;

        if (drain != null)
            await drain.Task;
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
        var drain = new YarnTaskCompletionSource();
        _optionsDrain = drain;

        try
        {
            VNOptionsPresentationContext ctx = new()
            {
                SourceOptions = dialogueOptions,
                Token = cancellationToken,
                NodeName = _currentNodeName,
            };

            // Phase: ChoiceSequenceReserved -> (ReplayResolved | Stale | Aborted | ViewModelsBuilt -> OptionsBoxShown)
            VNOptionsPresentationBeginResult beginResult = await _flow.BeginAsync(ctx);

            switch (beginResult)
            {
                case VNOptionsPresentationBeginResult.NoOption:
                case VNOptionsPresentationBeginResult.Stale:
                case VNOptionsPresentationBeginResult.Aborted:
                    return await DialogueRunner.NoOptionSelected;

                case VNOptionsPresentationBeginResult.ReplayResolved:
                    return ctx.SelectedOption;
            }

            try
            {
                // Phase: InteractiveReady -> AwaitingSelection
                PrepareInteractiveItems(ctx);
                SetPhase(ctx, VNOptionsPresentationPhase.InteractiveReady);

                VNOptionViewModel selected = await AwaitSelectionAsync(ctx);

                if (cancellationToken.IsNextContentRequested || selected == null)
                {
                    SetPhase(ctx, VNOptionsPresentationPhase.Aborted);
                    return await DialogueRunner.NoOptionSelected;
                }

                // Phase: SelectionCommitted
                _flow.CommitSelection(ctx, selected);
                SetPhase(ctx, VNOptionsPresentationPhase.SelectionCommitted);

                return ctx.SelectedOption ?? await DialogueRunner.NoOptionSelected;
            }
            finally
            {
                await CleanupInteractiveAsync(ctx);
                _flow.EndInteractiveImmediate();
            }
        }
        finally
        {
            drain.TrySetResult();

            if (ReferenceEquals(_optionsDrain, drain))
                _optionsDrain = null;
        }
    }

    private void PrepareInteractiveItems(VNOptionsPresentationContext ctx)
    {
        EndSelectionSession();
        DestroyActiveItems();

        _flow.SetInputEnabled(false);

        _selectionSession = new VNOptionSelectionSession(ctx.Token);

        CreateItems(ctx.ViewModels, ctx.ItemContainer);

        _flow.SetInputEnabled(true);
        SelectFirstAvailableItem();
    }

    private async YarnTask<VNOptionViewModel> AwaitSelectionAsync(VNOptionsPresentationContext ctx)
    {
        if (_selectionSession == null)
            return null;

        // Phase: AwaitingSelection
        SetPhase(ctx, VNOptionsPresentationPhase.AwaitingSelection);

        VNOptionViewModel selected = await _selectionSession.Task;

        _flow.SetInputEnabled(false);

        return selected;
    }

    private async YarnTask CleanupInteractiveAsync(VNOptionsPresentationContext ctx)
    {
        _flow.SetInputEnabled(false);

        EndSelectionSession();
        DestroyActiveItems();

        await YarnTask.Yield();
    }

    private void CreateItems(
        List<VNOptionViewModel> viewModels,
        RectTransform container)
    {
        for (int i = 0; i < viewModels.Count; i++)
        {
            VNOptionItem item = Instantiate(_optionItemPrefab, container);
            
            if (!item.gameObject.activeSelf)
                item.gameObject.SetActive(true);

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

    private static void SetPhase(VNOptionsPresentationContext ctx, VNOptionsPresentationPhase phase)
    {
        ctx.Phase = phase;
    }
}