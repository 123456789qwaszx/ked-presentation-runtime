using System.Collections.Generic;
using Yarn.Unity;

public enum OptionsPresentationBeginResult
{
    
    NoOption = 0,         // 고를 수 있는 옵션이 없다 — 정상적인 종료.
    ReplayResolved = 1,   // 시크 중이라 기록된 선택을 UI 없이 복원했다. ctx.SelectedOption은 반드시 채워진 상태.
    InteractiveReady = 2, // 옵션 박스가 떠 있고 입력을 열 수 있다.
    Stale = 3,            // 시크 중 기록된 선택을 복원하지 못했다 — 시크는 이미 꺼졌고 일반 재생으로 복귀한 상태다.
    Aborted = 4,          // 선택 없이 끊겼다 — 옵션 토큰이 취소된 경우(대화 정지).
}

public sealed class OptionsPresentationFlow
{
    private readonly OptionsBoxPresentationController _boxPresentation;
    private readonly ChoiceBoundary _choiceBoundary;
    private readonly SeekState _seekState;

    public OptionsPresentationFlow(
        OptionsBoxPresentationController boxPresentation,
        ChoiceBoundary choiceBoundary,
        SeekState seekState)
    {
        _boxPresentation = boxPresentation;
        _choiceBoundary = choiceBoundary;
        _seekState = seekState;
    }

    public async YarnTask<OptionsPresentationBeginResult> BeginAsync(
        OptionsPresentationContext ctx)
    {
        // Phase: ChoiceSequenceReserved
        ctx.ChoiceSequence = _choiceBoundary.ReserveChoiceSequence();
        SetPhase(ctx, OptionsPresentationPhase.ChoiceSequenceReserved);

        // Phase: ReplayResolved
        if (_seekState.IsSeeking) {
            // 옵션들이 있지만, 조건 제한등으로 선택 불가능할 경우.
            if (!ctx.HasAnyAvailableOption) {
                SetPhase(ctx, OptionsPresentationPhase.NoOption);
                return OptionsPresentationBeginResult.NoOption;
            }
            
            if (!_choiceBoundary.TryResolveReplayOption(ctx.ChoiceSequence, ctx.SourceOptions,
                    out DialogueOption replayOption)) {
                _seekState.End(SeekEndReason.ReplayChoiceMismatch);
                ctx.SelectedOption = null;
                
                SetPhase(ctx, OptionsPresentationPhase.Stale);
                return OptionsPresentationBeginResult.Stale;
            }

            // Pass the restored choice to the normal flow without user input.
            ctx.SelectedOption = replayOption;
            SetPhase(ctx, OptionsPresentationPhase.ReplayResolved);

            return OptionsPresentationBeginResult.ReplayResolved;
        }

        // Phase: ViewModelsBuilt
        ctx.ViewModels = BuildViewModels(ctx.SourceOptions, ctx.ChoiceSequence);

        if (ctx.ViewModels.Count == 0) {
            SetPhase(ctx, OptionsPresentationPhase.NoOption);
            return OptionsPresentationBeginResult.NoOption;
        }

        SetPhase(ctx, OptionsPresentationPhase.ViewModelsBuilt);

        // Phase: OptionsBoxShown
        if (!await _boxPresentation.ShowAsync(ctx.Token)) {
            EndInteractiveImmediate();

            SetPhase(ctx, OptionsPresentationPhase.Aborted);
            return OptionsPresentationBeginResult.Aborted;
        }

        ctx.ItemContainer = _boxPresentation.ItemContainer;

        SetPhase(ctx, OptionsPresentationPhase.OptionsBoxShown);

        return OptionsPresentationBeginResult.InteractiveReady;
    }

    public void CommitSelection(OptionsPresentationContext ctx, OptionViewModel selected)
    {
        _choiceBoundary.CommitSelection(
            selected.ChoiceSequence,
            selected.SourceOptionIndex,
            selected.SourceOption.Line.TextID);

        ctx.SelectedOption = selected.SourceOption;
    }

    public void SetInputEnabled(bool enabled)
    {
        _boxPresentation.SetInputEnabled(enabled);
    }

    public void EndInteractiveImmediate()
    {
        _boxPresentation.CloseImmediate();
    }

    private List<OptionViewModel> BuildViewModels(DialogueOption[] options, int choiceSequence)
    {
        var result = new List<OptionViewModel>();

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

    private OptionViewModel BuildViewModel(
        DialogueOption option,
        int sourceOptionIndex,
        int choiceSequence)
    {
        string label = option.Line.TextWithoutCharacterName.Text;

        return new OptionViewModel(
            sourceOption: option,
            sourceOptionIndex: sourceOptionIndex,
            choiceSequence: choiceSequence,
            label: label,
            isAvailable: option.IsAvailable);
    }

    private static void SetPhase(OptionsPresentationContext ctx, OptionsPresentationPhase phase)
    {
        ctx.Phase = phase;
    }
}