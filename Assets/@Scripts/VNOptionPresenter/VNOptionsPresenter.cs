using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public sealed partial class VNOptionsPresenter : DialoguePresenterBase
{
    private RollbackHistory _rollbackHistory;
    private ChoiceHistory _choiceHistory;
    private VNLinePresentationState _linePresentationState;

    [Header("References")]
    [SerializeField] private VNOptionItem _optionItemPrefab;
    [SerializeField] private RectTransform _optionContainer;
    [SerializeField] private CanvasGroup _canvasGroup;

    private readonly List<VNOptionItem> _pool = new ();
    
    private string _currentNodeName = "";

    private YarnTaskCompletionSource<DialogueOption> _selectionSource;
    private CancellationToken _completionToken;

    public void Initialize(DialogueRunner dialogueRunner, RollbackHistory rollbackHistory, ChoiceHistory choiceHistory, VNLinePresentationState linePresentationState)
    {
        _rollbackHistory = rollbackHistory;
        _choiceHistory = choiceHistory;
        _linePresentationState = linePresentationState;
        
        dialogueRunner.onNodeStart?.RemoveListener(OnNodeStart);
        dialogueRunner.onNodeStart?.AddListener(OnNodeStart);
    }

    private void OnNodeStart(string nodeName) => _currentNodeName = nodeName ?? string.Empty;
    public override void OnNodeEnter(string nodeName)
    {
        _currentNodeName = nodeName ?? "";

        _choiceHistory.NotifyNodeStarted(nodeName);
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

        int choiceIndexInNode = _choiceHistory.ConsumeChoiceIndexInCurrentNode(_currentNodeName);

        if (_linePresentationState.IsSeekingActive)
            return ResolveReplayOptionOrNoOption(dialogueOptions, choiceIndexInNode);

        List<VNOptionViewModel> viewModels = BuildViewModels(dialogueOptions, choiceIndexInNode);

        if (viewModels.Count == 0)
            return await DialogueRunner.NoOptionSelected;

        
        while (_pool.Count < viewModels.Count)
        {
            Transform parent = _optionContainer;
            VNOptionItem item = Instantiate(_optionItemPrefab, parent);

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
            
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            for (int i = 0; i < viewModels.Count; i++)
            {
                _pool[i].SetRevealAlpha(1f);
            }

            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            SelectFirstAvailableItem(viewModels.Count);

            DialogueOption selected = await _selectionSource.Task;

            internalCancel.Cancel();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

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
        if (!_choiceHistory.TryGetChoiceRecord(_currentNodeName, choiceIndexInNode, out VNChoiceRecord record))
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
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

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