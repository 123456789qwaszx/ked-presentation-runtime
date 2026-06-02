using System;
using System.Collections.Generic;
using Yarn.Unity;

// Runs one option-set presentation transaction through its explicit phase sequence.
// This class owns the transaction order, selection decision flow, and the choice commit,
// but not the item pool, UI lifetime, or cancellation tokens.
// VNOptionsPresenter remains the owner of pool lifetime, item binding, and the selection source.
public sealed class VNOptionsPresentationFlow
{
    private readonly VNOptionsBoxPresentationController _boxPresentation;
    private readonly VNLinePresentationState _advanceState;
    private readonly VNChoiceBoundary _choiceBoundary;

    public VNOptionsPresentationPhase CurrentPhase { get; private set; } = VNOptionsPresentationPhase.None;

    public VNOptionsPresentationFlow(
        VNOptionsBoxPresentationController boxPresentation,
        VNLinePresentationState advanceState,
        VNChoiceBoundary choiceBoundary)
    {
        _boxPresentation = boxPresentation;
        _advanceState = advanceState;
        _choiceBoundary = choiceBoundary;
    }

    public async YarnTask<DialogueOption> RunAsync(
        VNOptionsPresentationContext ctx,
        Action<VNOptionsPresentationContext> prepareItems,
        Func<VNOptionsPresentationContext, YarnTask<VNOptionViewModel>> awaitSelection,
        Func<VNOptionsPresentationContext, YarnTask> cleanup,
        Func<bool> shouldFastForward)
    {
        // Phase: OptionsReceived -> OptionSetCommitted
        SetPhase(ctx, VNOptionsPresentationPhase.OptionsReceived);

        ctx.ChoiceIndexInNode = _choiceBoundary.ReserveChoiceIndex();
        SetPhase(ctx, VNOptionsPresentationPhase.OptionSetCommitted);

        // Phase: SelectionPolicyResolved
        VNOptionSelectionDecision enteredDecision;

        if (ctx.HasAnyAvailableOption) {
            enteredDecision = _advanceState.IsSeekingActive
                ? VNOptionSelectionDecision.ReplayDuringSeek()
                : VNOptionSelectionDecision.PresentInteractive();
        }
        else enteredDecision = VNOptionSelectionDecision.NoOptionAvailable();

        ctx.SelectionDecision = enteredDecision;
        SetPhase(ctx, VNOptionsPresentationPhase.SelectionPolicyResolved);

        if (ctx.ShouldReturnNoOption) {
            SetPhase(ctx, VNOptionsPresentationPhase.Completed);
            return null;
        }

        if (ctx.ShouldReplayRecordedSelection) {
            ctx.IsReplay = _choiceBoundary.TryResolveReplayOption(
                ctx.ChoiceIndexInNode,
                ctx.SourceOptions,
                out DialogueOption replayOption);

            ctx.ReplayOption = replayOption;
            ctx.SelectedOption = ctx.IsReplay 
                ? ctx.ReplayOption 
                : null;

            SetPhase(ctx, VNOptionsPresentationPhase.ReplayResolved);
            SetPhase(ctx, VNOptionsPresentationPhase.Completed);
            return ctx.SelectedOption;
        }

        // Phase: ViewModelsBuilt
        ctx.ViewModels = BuildViewModels(ctx);

        if (ctx.ViewModels.Count == 0) {
            SetPhase(ctx, VNOptionsPresentationPhase.Completed);
            return null;
        }
        SetPhase(ctx, VNOptionsPresentationPhase.ViewModelsBuilt);

        try {
            // Phase: BoxTransitioning -> BoxReady
            SetPhase(ctx, VNOptionsPresentationPhase.BoxTransitioning);

            ctx.BoxResult = await _boxPresentation.ShowOptionsAsync(
                new VNOptionsBoxPresentationOptions {
                    UseImmediateTransition = shouldFastForward(),
                    Style = VNOptionsBoxStyle.Default,
                    AnchorCharacterName = null,
                });
            SetPhase(ctx, VNOptionsPresentationPhase.BoxReady);

            if (ctx.BoxResult == null || !ctx.BoxResult.IsValid) {
                await AbortAsync(ctx);
                return null;
            }

            // Phase: ItemsPrepared
            prepareItems(ctx);
            SetPhase(ctx, VNOptionsPresentationPhase.ItemsPrepared);

            // Phase: WaitingForSelection
            SetPhase(ctx, VNOptionsPresentationPhase.WaitingForSelection);
            VNOptionViewModel selected = await awaitSelection(ctx);

            if (ctx.Token.IsNextContentRequested || selected == null) {
                await AbortAsync(ctx);
                return null;
            }

            // Phase: SelectionCommitted
            CommitSelection(ctx, selected);
            SetPhase(ctx, VNOptionsPresentationPhase.SelectionCommitted);

            SetPhase(ctx, VNOptionsPresentationPhase.Completed);
            return ctx.SelectedOption;
        }
        catch {
            SetPhase(ctx, VNOptionsPresentationPhase.Aborted);
            throw;
        }
        finally {
            await cleanup(ctx);
        }
    }

    private List<VNOptionViewModel> BuildViewModels(VNOptionsPresentationContext ctx)
    {
        var result = new List<VNOptionViewModel>();

        for (int i = 0; i < ctx.SourceOptions.Length; i++)
        {
            DialogueOption option = ctx.SourceOptions[i];

            if (!option.IsAvailable)
                continue;

            result.Add(VNOptionViewModelBuilder.Build(
                option,
                sourceOptionIndex: i,
                choiceIndexInNode: ctx.ChoiceIndexInNode));
        }

        return result;
    }

    private void CommitSelection(VNOptionsPresentationContext ctx, VNOptionViewModel selected)
    {
        _choiceBoundary.CommitSelection(
            ctx.NodeName,
            selected.ChoiceIndexInNode,
            selected.SourceOptionIndex,
            selected.SourceOption.Line.TextID);

        ctx.SelectedOption = selected.SourceOption;
    }

    private async YarnTask AbortAsync(VNOptionsPresentationContext ctx)
    {
        SetPhase(ctx, VNOptionsPresentationPhase.Aborted);

        _boxPresentation.CleanupAborted(ctx.BoxResult);

        await YarnTask.CompletedTask;
    }

    private void SetPhase(VNOptionsPresentationContext ctx, VNOptionsPresentationPhase phase)
    {
        ctx.Phase = phase;
        CurrentPhase = phase;
    }
}