using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed partial class VNOptionsPresenter : DialoguePresenterBase
{
    private RollbackController _rollbackHistory;
    private ChoiceHistory _choiceHistory;
    private VNLinePresentationState _linePresentationState;

    [Header("References")]
    [SerializeField] private VNOptionItem optionItemPrefab;
    [SerializeField] private RectTransform optionContainer;
    [SerializeField] private CanvasGroup canvasGroup;

    private readonly List<VNOptionItem> _pool = new ();
    
    private string _currentNodeName;

    private YarnTaskCompletionSource<DialogueOption> _selectionSource;
    private CancellationToken _completionToken;

    public void Initialize(DialogueRunner dialogueRunner, RollbackController rollbackHistory, ChoiceHistory choiceHistory, VNLinePresentationState linePresentationState)
    {
        _rollbackHistory = rollbackHistory;
        _choiceHistory = choiceHistory;
        _linePresentationState = linePresentationState;
        
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
    { return YarnTask.CompletedTask; }

    public override async YarnTask<DialogueOption> RunOptionsAsync(
        DialogueOption[] dialogueOptions,
        LineCancellationToken cancellationToken)
    {
        #region NoOptions
        bool AnyOptionAvailable(DialogueOption[] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].IsAvailable)
                    return true;
            }
            
            return false;
        }
        
        if (!AnyOptionAvailable(dialogueOptions))
            return await DialogueRunner.NoOptionSelected;
        #endregion

        int targetChoiceIndex = _choiceHistory.NextChoiceIndex;
        _choiceHistory.NextChoiceIndex++;

        #region IfSeeking
        if (_linePresentationState.IsSeekingActive)
            return ResolveReplayOptionOrNoOption(dialogueOptions, targetChoiceIndex);
        #endregion

        List<VNOptionViewModel> viewModels = BuildViewModels(dialogueOptions, targetChoiceIndex);

        if (viewModels.Count == 0)
            return await DialogueRunner.NoOptionSelected;

        
        while (_pool.Count < viewModels.Count)
        {
            Transform parent = optionContainer;
            VNOptionItem item = Instantiate(optionItemPrefab, parent);

            item.gameObject.SetActive(false);
            item.ResetView();

            _pool.Add(item);
        }

        var internalCancel = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken.NextContentToken);

        _selectionSource = new YarnTaskCompletionSource<DialogueOption>();
        _completionToken = internalCancel.Token;

        try
        {
            MonitorExternalCancellation(cancellationToken, _selectionSource, internalCancel.Token).Forget();

            PrepareItems(viewModels);
            
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

            DialogueOption selected = await _selectionSource.Task;

            internalCancel.Cancel();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

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

    private List<VNOptionViewModel> BuildViewModels(DialogueOption[] dialogueOptions, int choiceIndexInNode)
    {
        var result = new List<VNOptionViewModel>();

        for (int i = 0; i < dialogueOptions.Length; i++)
        {
            DialogueOption option = dialogueOptions[i];

            if (!option.IsAvailable)
                continue;

            result.Add(VNOptionViewModelBuilder.Build(
                option,
                sourceOptionIndex: i,
                choiceIndexInNode: choiceIndexInNode));
        }

        return result;
    }

    private DialogueOption ResolveReplayOptionOrNoOption(DialogueOption[] dialogueOptions, int choiceIndexInNode)
    {
        if (!_choiceHistory.TryGetChoiceRecord(choiceIndexInNode, out VNChoiceRecord record))
            return null;

        if (record.selectedOptionIndex < 0 || record.selectedOptionIndex >= dialogueOptions.Length)
            return null;
        
        DialogueOption option = dialogueOptions[record.selectedOptionIndex];

        if (option == null || !option.IsAvailable)
            return null;
        
        return option;
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

        VNOptionViewModel viewModel = item.ViewModel;

        _choiceHistory.AddChoiceRecord(
            _rollbackHistory.Points,
            _currentNodeName,
            viewModel.ChoiceIndexInNode,
            viewModel.SourceOptionIndex);
        

        _selectionSource.TrySetResult(viewModel.SourceOption);
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