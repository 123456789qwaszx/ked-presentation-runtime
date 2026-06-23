using System.Collections.Generic;
using Yarn.Unity;

public enum VNOptionsPresentationBeginResult
{
    NoOption = 0,
    ReplayResolved = 1,
    InteractiveReady = 2,
}

public sealed class VNOptionsPresentationFlow
{
    private readonly DialogueBoxHost _dialogueBoxHost;
    private readonly VNChoiceBoundary _choiceBoundary;
    private readonly VNLinePresentationState _advanceState;
    private readonly VnUxState _uxState;

    private readonly VNOptionEffectPreviewResolver _effectResolver = new();

    private readonly OptionsBoxKind _defaultBoxKind;
    private readonly float _fadeDuration;

    private IPresentationOptionsBoxView _currentView;

    public VNOptionsPresentationFlow(
        DialogueBoxHost dialogueBoxHost,
        VNChoiceBoundary choiceBoundary,
        VNLinePresentationState advanceState,
        VnUxState uxState,
        OptionsBoxKind defaultBoxKind = OptionsBoxKind.Default,
        float fadeDuration = 0.12f)
    {
        _dialogueBoxHost = dialogueBoxHost;
        _choiceBoundary = choiceBoundary;
        _advanceState = advanceState;
        _uxState = uxState;
        _defaultBoxKind = defaultBoxKind;
        _fadeDuration = fadeDuration;
    }

    public async YarnTask<VNOptionsPresentationBeginResult> BeginAsync(
        VNOptionsPresentationContext ctx)
    {
        ctx.ChoiceIndexInNode = _choiceBoundary.ReserveChoiceIndex();

        if (!ctx.HasAnyAvailableOption)
            return VNOptionsPresentationBeginResult.NoOption;

        if (_advanceState.IsSeekingActive)
        {
            bool resolved = _choiceBoundary.TryResolveReplayOption(
                ctx.ChoiceIndexInNode,
                ctx.SourceOptions,
                out DialogueOption replayOption);

            ctx.SelectedOption = resolved ? replayOption : null;

            return resolved
                ? VNOptionsPresentationBeginResult.ReplayResolved
                : VNOptionsPresentationBeginResult.NoOption;
        }

        ctx.ViewModels = BuildViewModels(
            ctx.SourceOptions,
            ctx.ChoiceIndexInNode);

        if (ctx.ViewModels.Count == 0)
            return VNOptionsPresentationBeginResult.NoOption;

        _uxState.SetChoicesVisible(true);

        ctx.OptionsBoxView = await ShowOptionsBoxAsync(
            useImmediateTransition: false,
            ctx);

        if (ctx.OptionsBoxView == null || ctx.OptionsBoxView.ItemContainer == null)
        {
            EndInteractiveImmediate();
            return VNOptionsPresentationBeginResult.NoOption;
        }

        return VNOptionsPresentationBeginResult.InteractiveReady;
    }

    public void CommitSelection(VNOptionsPresentationContext ctx, VNOptionViewModel selected)
    {
        _choiceBoundary.CommitSelection(
            ctx.NodeName,
            selected.ChoiceIndexInNode,
            selected.SourceOptionIndex,
            selected.SourceOption.Line.TextID);

        ctx.SelectedOption = selected.SourceOption;
    }

    public void EndInteractiveImmediate()
    {
        _uxState.SetChoicesVisible(false);

        if (_currentView != null)
        {
            _currentView.SetInputEnabled(false);
            _currentView.SetVisibleImmediate(false);
        }

        _currentView = null;
    }

    private async YarnTask<IPresentationOptionsBoxView> ShowOptionsBoxAsync(
        bool useImmediateTransition,
        VNOptionsPresentationContext ctx)
    {
        IPresentationOptionsBoxView nextView = _dialogueBoxHost.ResolveOptionsTarget(_defaultBoxKind);

        _dialogueBoxHost.HideAllOptionsBoxesExcept(nextView);
        _currentView = nextView;

        _currentView.ResetPresentationTransform();
        _currentView.PrepareHidden();
        _currentView.SetInputEnabled(false);

        if (useImmediateTransition)
        {
            _currentView.SetVisibleImmediate(true);
            _currentView.SetInputEnabled(false);
            return _currentView;
        }

        await _currentView
            .FadeInAsync(_fadeDuration, ctx.Token.NextContentToken)
            .SuppressCancellationThrow();

        if (ctx.Token.IsNextContentRequested)
            return null;

        _currentView.SetInputEnabled(false);

        return _currentView;
    }

    private List<VNOptionViewModel> BuildViewModels(
        DialogueOption[] options,
        int choiceIndexInNode)
    {
        var result = new List<VNOptionViewModel>();

        if (options == null)
            return result;

        for (int i = 0; i < options.Length; i++)
        {
            DialogueOption option = options[i];

            if (option == null)
                continue;

            if (!option.IsAvailable)
                continue;

            result.Add(BuildViewModel(
                option,
                sourceOptionIndex: i,
                choiceIndexInNode: choiceIndexInNode));
        }

        return result;
    }

    private VNOptionViewModel BuildViewModel(
        DialogueOption option,
        int sourceOptionIndex,
        int choiceIndexInNode)
    {
        string label = option.Line.TextWithoutCharacterName.Text;
        List<VNOptionEffectPreview> effects = _effectResolver.Resolve(option.Line.Metadata);

        return new VNOptionViewModel(
            sourceOption: option,
            sourceOptionIndex: sourceOptionIndex,
            choiceIndexInNode: choiceIndexInNode,
            label: label,
            isAvailable: option.IsAvailable,
            effects: effects);
    }
}