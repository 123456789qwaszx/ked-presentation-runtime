using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public enum VNOptionSelectionMode
{
    NoOptionAvailable = 0,
    ReplayRecordedChoiceDuringSeek = 1,
    PresentInteractive = 2,
}

// Runs one option-set presentation transaction.
// Owns choice replay/commit ordering, but not item instances or selection input.
public sealed class VNOptionsPresentationFlow
{
    private readonly VNOptionsBoxPresentationController _boxPresentation;
    private readonly VNLinePresentationState _advanceState;
    private readonly VNChoiceBoundary _choiceBoundary;
    private readonly VnUxState _uxState;

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
        ctx.ChoiceIndexInNode = _choiceBoundary.ReserveChoiceIndex();

        VNOptionSelectionMode mode = ResolveSelectionMode(ctx);

        switch (mode)
        {
            case VNOptionSelectionMode.NoOptionAvailable:
                return null;

            case VNOptionSelectionMode.ReplayRecordedChoiceDuringSeek:
                return ResolveReplayOption(ctx);

            case VNOptionSelectionMode.PresentInteractive:
                return await RunInteractiveAsync(
                    ctx,
                    prepareItems,
                    awaitSelection,
                    cleanup,
                    shouldFastForward);

            default:
                Debug.LogWarning($"[VNOptionsPresentationFlow] Unknown selection mode: {mode}");
                return null;
        }
    }

    private VNOptionSelectionMode ResolveSelectionMode(VNOptionsPresentationContext ctx)
    {
        if (!ctx.HasAnyAvailableOption)
            return VNOptionSelectionMode.NoOptionAvailable;

        if (_advanceState.IsSeekingActive)
            return VNOptionSelectionMode.ReplayRecordedChoiceDuringSeek;

        return VNOptionSelectionMode.PresentInteractive;
    }

    private DialogueOption ResolveReplayOption(VNOptionsPresentationContext ctx)
    {
        bool resolved = _choiceBoundary.TryResolveReplayOption(
            ctx.ChoiceIndexInNode,
            ctx.SourceOptions,
            out DialogueOption replayOption);

        return resolved ? replayOption : null;
    }

    private async YarnTask<DialogueOption> RunInteractiveAsync(
        VNOptionsPresentationContext ctx,
        Action<VNOptionsPresentationContext> prepareItems,
        Func<VNOptionsPresentationContext, YarnTask<VNOptionViewModel>> awaitSelection,
        Func<VNOptionsPresentationContext, YarnTask> cleanup,
        Func<bool> shouldFastForward)
    {
        ctx.ViewModels = BuildViewModels(ctx);

        if (ctx.ViewModels.Count == 0)
            return null;

        try
        {
            _uxState.SetChoicesVisible(true);

            ctx.BoxResult = await _boxPresentation.ShowOptionsAsync(
                new VNOptionsBoxPresentationOptions
                {
                    UseImmediateTransition = shouldFastForward(),
                    Style = VNOptionsBoxStyle.Default,
                });

            if (ctx.BoxResult == null || !ctx.BoxResult.IsValid)
            {
                Abort(ctx);
                return null;
            }

            prepareItems(ctx);

            VNOptionViewModel selected = await awaitSelection(ctx);

            if (ctx.Token.IsNextContentRequested || selected == null)
            {
                Abort(ctx);
                return null;
            }

            CommitSelection(ctx, selected);

            return ctx.SelectedOption;
        }
        finally
        {
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

    private void CommitSelection(VNOptionsPresentationContext ctx, VNOptionViewModel selected)
    {
        _choiceBoundary.CommitSelection(
            ctx.NodeName,
            selected.ChoiceIndexInNode,
            selected.SourceOptionIndex,
            selected.SourceOption.Line.TextID);

        ctx.SelectedOption = selected.SourceOption;
    }

    private void Abort(VNOptionsPresentationContext ctx)
    {
        _boxPresentation.CleanupAborted(ctx.BoxResult);
    }
}