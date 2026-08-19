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
    private readonly IPresentationOptionsBoxView _optionsBox;
    private readonly VNChoiceBoundary _choiceBoundary;
    private readonly VNLinePresentationState _advanceState;

    private readonly float _fadeDuration;

    public VNOptionsPresentationFlow(
        IPresentationOptionsBoxView optionsBox,
        VNChoiceBoundary choiceBoundary,
        VNLinePresentationState advanceState,
        float fadeDuration = 0.12f)
    {
        _optionsBox = optionsBox;
        _choiceBoundary = choiceBoundary;
        _advanceState = advanceState;
        _fadeDuration = fadeDuration;
    }

    public async YarnTask<VNOptionsPresentationBeginResult> BeginAsync(
        VNOptionsPresentationContext ctx)
    {
        ctx.ChoiceIndexInNode = _choiceBoundary.ReserveChoiceIndex();

        if (!ctx.HasAnyAvailableOption)
            return VNOptionsPresentationBeginResult.NoOption;

        if (_advanceState.IsSeekingActive) { 
            bool resolved = _choiceBoundary.TryResolveReplayOption(
                ctx.ChoiceIndexInNode,
                ctx.SourceOptions,
                out DialogueOption replayOption);

            ctx.SelectedOption = resolved
                ? replayOption 
                : null;

            return resolved
                ? VNOptionsPresentationBeginResult.ReplayResolved
                : VNOptionsPresentationBeginResult.NoOption;
        }

        ctx.ViewModels = BuildViewModels(
            ctx.SourceOptions,
            ctx.ChoiceIndexInNode);

        if (ctx.ViewModels.Count == 0)
            return VNOptionsPresentationBeginResult.NoOption;

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
        _optionsBox.SetInputEnabled(false);
        _optionsBox.SetVisibleImmediate(false);
    }

    private async YarnTask<IPresentationOptionsBoxView> ShowOptionsBoxAsync(
        bool useImmediateTransition,
        VNOptionsPresentationContext ctx)
    {
        _optionsBox.ResetPresentationTransform();
        _optionsBox.PrepareHidden();
        _optionsBox.SetInputEnabled(false);

        if (useImmediateTransition)
        {
            _optionsBox.SetVisibleImmediate(true);
            _optionsBox.SetInputEnabled(false);
            return _optionsBox;
        }

        await _optionsBox
            .FadeInAsync(_fadeDuration, ctx.Token.NextContentToken)
            .SuppressCancellationThrow();

        if (ctx.Token.IsNextContentRequested)
            return null;

        _optionsBox.SetInputEnabled(false);

        return _optionsBox;
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

        return new VNOptionViewModel(
            sourceOption: option,
            sourceOptionIndex: sourceOptionIndex,
            choiceIndexInNode: choiceIndexInNode,
            label: label,
            isAvailable: option.IsAvailable);
    }
}