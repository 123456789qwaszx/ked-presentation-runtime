using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class VNOptionsPresentationFlow
{
    private readonly VNOptionsBoxPresentationController _boxPresentation;
    private readonly VNLinePresentationState _advanceState;
    private readonly VNChoiceBoundary _choiceBoundary;
    private readonly VnUxState _uxState;

    public VNOptionsPresentationPhase CurrentPhase { get; private set; } = VNOptionsPresentationPhase.None;

    public VNOptionsPresentationFlow(
        VNOptionsBoxPresentationController boxPresentation,
        VNLinePresentationState advanceState,
        VNChoiceBoundary choiceBoundary,
        VnUxState uxState)
    {
        _boxPresentation = boxPresentation;
        _advanceState = advanceState;
        _choiceBoundary = choiceBoundary;
        _uxState = uxState;
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
                ? VNOptionSelectionDecision.ReplayRecordedChoiceDuringSeek()
                : VNOptionSelectionDecision.PresentInteractive();
        }
        else enteredDecision = VNOptionSelectionDecision.NoOptionAvailable();

        ctx.SelectionDecision = enteredDecision;
        SetPhase(ctx, VNOptionsPresentationPhase.SelectionPolicyResolved);

        if (ctx.NoOptionsAvailable) {
            Debug.Log("no option selected");
            SetPhase(ctx, VNOptionsPresentationPhase.Completed);
            return null;
        }

        if (ctx.ShouldReplayRecordedChoice) {
            ctx.IsReplay = _choiceBoundary.TryResolveReplayOption(
                ctx.ChoiceIndexInNode,
                ctx.SourceOptions,
                out DialogueOption replayOption);

            ctx.ReplayOption = replayOption;
            ctx.SelectedOption = ctx.IsReplay ? ctx.ReplayOption : null;

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
            _uxState.SetChoicesVisible(true);

            // Phase: BoxTransitioning -> BoxReady
            SetPhase(ctx, VNOptionsPresentationPhase.BoxTransitioning);

            ctx.BoxResult = await _boxPresentation.ShowOptionsAsync(
                new VNOptionsBoxPresentationOptions {
                    UseImmediateTransition = shouldFastForward(),
                    Style = VNOptionsBoxStyle.Default,
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
            
            _choiceBoundary.CommitSelection(
                ctx.NodeName,
                selected.ChoiceIndexInNode,
                selected.SourceOptionIndex,
                selected.SourceOption.Line.TextID);

            ctx.SelectedOption = selected.SourceOption;
            
            SetPhase(ctx, VNOptionsPresentationPhase.SelectionCommitted);

            SetPhase(ctx, VNOptionsPresentationPhase.Completed);
            return ctx.SelectedOption;
        }
        catch {
            SetPhase(ctx, VNOptionsPresentationPhase.Aborted);
            throw;
        }
        finally {
            _uxState.SetChoicesVisible(false);
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