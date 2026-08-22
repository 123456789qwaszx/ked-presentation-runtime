using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public enum VNOptionsPresentationBeginResult
{
    // 고를 수 있는 옵션이 없다 — 정상적인 종료다.
    NoOption = 0,

    // 시크 중이라 기록된 선택을 UI 없이 복원했다. ctx.SelectedOption은 반드시 채워져 있다.
    ReplayResolved = 1,

    // 옵션 박스가 떠 있고 입력을 열 수 있다.
    InteractiveReady = 2,

    // 이 트랜잭션의 전제가 깨졌다 — 리플레이 복원 실패, 또는 옵션 박스 배선 문제.
    // 리플레이 복원 실패로 온 경우 시크는 이미 꺼졌고 일반 재생으로 복귀한 상태다.
    Stale = 3,

    // 옵션 박스를 띄우는 도중 플레이어가 다음 콘텐츠를 요청했다 — 선택 없이 끊긴다.
    Aborted = 4,
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
        // Phase: ChoiceSequenceReserved
        ctx.ChoiceSequence = _choiceBoundary.ReserveChoiceSequence();
        SetPhase(ctx, VNOptionsPresentationPhase.ChoiceSequenceReserved);

        // Phase: ReplayResolved | Stale
        if (_advanceState.IsSeekingActive)
        {
            // 옵션들이 있지만, 조건 제한등으로 선택불가능할 경우.
            if (!ctx.HasAnyAvailableOption)
            {
                SetPhase(ctx, VNOptionsPresentationPhase.NoOption);
                return VNOptionsPresentationBeginResult.NoOption;
            }
            
            if (!_choiceBoundary.TryResolveReplayOption(
                    ctx.ChoiceSequence,
                    ctx.SourceOptions,
                    out DialogueOption replayOption))
            {
                Debug.LogWarning(
                    $"[VNOptionsPresentationFlow] 리플레이에서 기존 선택 복원 실패. " +
                    $"node='{ctx.NodeName}', choiceSequence={ctx.ChoiceSequence}.");

                //시크를 끄고 일반 재생으로 복귀.
                _advanceState.ClearSeek();

                ctx.SelectedOption = null;
                SetPhase(ctx, VNOptionsPresentationPhase.Stale);

                return VNOptionsPresentationBeginResult.Stale;
            }

            // Pass the restored choice to the normal flow without user input.
            ctx.SelectedOption = replayOption;
            SetPhase(ctx, VNOptionsPresentationPhase.ReplayResolved);

            return VNOptionsPresentationBeginResult.ReplayResolved;
        }

        // Phase: ViewModelsBuilt
        ctx.ViewModels = BuildViewModels(ctx.SourceOptions, ctx.ChoiceSequence);

        if (ctx.ViewModels.Count == 0)
        {
            SetPhase(ctx, VNOptionsPresentationPhase.NoOption);
            return VNOptionsPresentationBeginResult.NoOption;
        }

        SetPhase(ctx, VNOptionsPresentationPhase.ViewModelsBuilt);

        // Phase: OptionsBoxShown
        ctx.OptionsBoxView = await ShowOptionsBoxAsync(
            useImmediateTransition: false,
            ctx);

        // 페이드인 도중 다음 콘텐츠 요청이 들어옴(ShowOptionsBoxAsync가 null을 주는 유일한 경우).
        // 옵션이 없었던 게 아니라 플레이어가 선택 전에 끊은 것.
        if (ctx.OptionsBoxView == null)
        {
            EndInteractiveImmediate();

            SetPhase(ctx, VNOptionsPresentationPhase.Aborted);
            return VNOptionsPresentationBeginResult.Aborted;
        }

        // 박스는 떴는데 항목을 붙일 자리가 없음 — 뷰 배선이 깨짐.
        if (ctx.OptionsBoxView.ItemContainer == null)
        {
            Debug.LogError(
                $"[VNOptionsPresentationFlow] 옵션 박스에 ItemContainer가 없어 선택지를 띄우지 못했다. " +
                $"node='{ctx.NodeName}', choiceSequence={ctx.ChoiceSequence}, " +
                $"optionCount={ctx.ViewModels.Count}.");

            EndInteractiveImmediate();

            SetPhase(ctx, VNOptionsPresentationPhase.Stale);
            return VNOptionsPresentationBeginResult.Stale;
        }

        SetPhase(ctx, VNOptionsPresentationPhase.OptionsBoxShown);

        return VNOptionsPresentationBeginResult.InteractiveReady;
    }

    public void CommitSelection(VNOptionsPresentationContext ctx, VNOptionViewModel selected)
    {
        _choiceBoundary.CommitSelection(
            ctx.NodeName,
            selected.ChoiceSequence,
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

    private List<VNOptionViewModel> BuildViewModels(DialogueOption[] options, int choiceSequence)
    {
        var result = new List<VNOptionViewModel>();

        for (int i = 0; i < options.Length; i++)
        {
            DialogueOption option = options[i];

            if (!option.IsAvailable)
                continue;

            result.Add(BuildViewModel(
                option,
                sourceOptionIndex: i,
                choiceSequence: choiceSequence));
        }

        return result;
    }

    private VNOptionViewModel BuildViewModel(
        DialogueOption option,
        int sourceOptionIndex,
        int choiceSequence)
    {
        string label = option.Line.TextWithoutCharacterName.Text;

        return new VNOptionViewModel(
            sourceOption: option,
            sourceOptionIndex: sourceOptionIndex,
            choiceSequence: choiceSequence,
            label: label,
            isAvailable: option.IsAvailable);
    }

    private static void SetPhase(VNOptionsPresentationContext ctx, VNOptionsPresentationPhase phase)
    {
        ctx.Phase = phase;
    }
}