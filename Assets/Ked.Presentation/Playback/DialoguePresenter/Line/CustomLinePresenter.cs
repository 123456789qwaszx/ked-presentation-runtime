using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

public interface ILineAborter
{
    void AbortCurrentLine();
}

// 노드 끝의 암묵 스텝(붙을 줄이 없이 남은 명령)을 실행한다. 기다릴 것이 없으면 그 자리에서 끝난 태스크를 돌려준다.
// <<step>>은 이것을 부르지 않는다 — 구간을 닫기만 하고, 다음 대사 줄이 앞 앵커로 실행한다(docs/PLAN_line-anchor.md A6).
public interface IStageStepRunner
{
    YarnTask RunStageStepAsync();
}

public sealed class CustomLinePresenter : DialoguePresenterBase, ILineAborter, IStageStepRunner
{
    private LinePresentationFlow _linePresentationFlow;
    private StageStepFlow _stageStepFlow;

    private EllipsisBreathTypewriter _typewriter;
    private PlaybackRuntimeState _playbackRuntimeState;

    private CancellationTokenSource _presenterLifetimeCts = new();
    private CancellationTokenSource _lineVisualCts;

    // 진행 중인 RunLineAsync(· 무대 스텝)가 실제로 빠져나올 때까지 기다리기 위한 통로.
    // 이게 없으면 정지 이후에도 라인 태스크가 살아남아,
    // 다음 StartDialogue가 만든 새 dialogueCancellationSource를 보고
    // Dialogue.Continue()를 한 번 더 씀.
    private YarnTaskCompletionSource _lineDrain;

    [SerializeField] private List<ActionMarkupHandler> eventHandlers = new();

    private List<IActionMarkupHandler> ActionMarkupHandlers
    {
        get
        {
            var list = new List<IActionMarkupHandler>();
            list.AddRange(eventHandlers);
            return list;
        }
    }

    public void Initialize(
        DialogueRunner dialogueRunner,
        LinePresentationFlow linePresentationFlow,
        StageStepFlow stageStepFlow,
        InlineCommandTrack inlineCommands,
        EllipsisBreathTypewriter typewriter,
        PlaybackRuntimeState playbackRuntimeState)
    {
        RegisterPresenter(dialogueRunner);
        
        _linePresentationFlow = linePresentationFlow;
        _stageStepFlow = stageStepFlow;

        _typewriter = typewriter;
        List<IActionMarkupHandler> handlers = ActionMarkupHandlers;
        handlers.Add(inlineCommands);
        _typewriter.ActionMarkupHandlers = handlers;

        _playbackRuntimeState = playbackRuntimeState;
    }

    public override async YarnTask RunLineAsync(
        LocalizedLine line,
        LineCancellationToken token)
    {
        var drain = new YarnTaskCompletionSource();
        _lineDrain = drain;

        try
        {
            var ctx = new LinePresentationContext
            {
                Line = line,
                Token = token,
                ShouldUseImmediateTransition = _playbackRuntimeState.IsSpeedUpMode,
                IsRapidSkip = _playbackRuntimeState.IsRapidSkipMode
            };

            await _linePresentationFlow.RunAsync(
                ctx,
                beginRun: BeginLinePresentationRun,
                waitForAdvance: WaitForLineAdvanceAsync);
        }
        finally
        {
            drain.TrySetResult();

            if (ReferenceEquals(_lineDrain, drain))
                _lineDrain = null;
        }
    }

    // 무대 스텝은 줄과 같은 화면 수명(AbortCurrentLine · 대화 끝이 끊는다)과 같은 배수 통로를 쓴다 —
    // Yarn은 명령의 태스크가 끝나면 Continue()를 쓰므로, 정지 뒤에 늦게 끝나면 줄과 똑같이 새 대화의 줄을 먹는다.
    public YarnTask RunStageStepAsync()
    {
        if (_stageStepFlow.TryPlayWithoutWaiting())
            return YarnTask.CompletedTask;

        return RunStageStepUntilDrainedAsync();
    }

    private async YarnTask RunStageStepUntilDrainedAsync()
    {
        var drain = new YarnTaskCompletionSource();
        _lineDrain = drain;

        try
        {
            await _stageStepFlow.RunAsync(BeginLinePresentationRun());
        }
        finally
        {
            drain.TrySetResult();

            if (ReferenceEquals(_lineDrain, drain))
                _lineDrain = null;
        }
    }

    private LinePresentationRun BeginLinePresentationRun()
    {
        CancelLineVisualToken();

        _lineVisualCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                _presenterLifetimeCts.Token);

        return new LinePresentationRun(_lineVisualCts.Token);
    }

    private async YarnTask WaitForLineAdvanceAsync(LineCancellationToken token)
    {
        CancellationTokenSource cts = null;

        try
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(
                token.NextContentToken,
                _presenterLifetimeCts.Token);

            await YarnTask
                .WaitUntilCanceled(cts.Token)
                .SuppressCancellationThrow();
        }
        finally
        {
            cts?.Dispose();
        }
    }

    // 지금 줄의 화면 작업을 끊는다. 그 줄의 run은 무효가 되어 stale 경로로 정리된다.
    public void AbortCurrentLine()
    {
        CancelLineVisualToken();
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    // DialogueRunner는 Stop()을 끝내기 전에 이 태스크를 기다린 뒤, Task를 닫는다.
    // 순서: 먼저 취소해서 대기를 풀고, 그 다음 Task가 닫히길 기다림..
    public override async YarnTask OnDialogueCompleteAsync()
    {
        CancelLineVisualToken();
        CancelPresenterLifetimeWaiters();

        YarnTaskCompletionSource drain = _lineDrain;

        if (drain != null)
        {
            await drain.Task;

            // 스택 탈출용.
            // drain은 RunLineAsync의 finally에서 닫혀 그 메서드의 SetResult()보다 앞섬.
            // (1) Stop() 반환과 StartDialogue()가 이 스택 안에서 실행,
            // (2) 새 dialogueCancellationSource가 먼저 설치,
            // (3) 뒤늦게 전파된 OnLineReceivedAsync가 그 소스를 보고 Continue() 발사,
            // (4) 새 대화의 라인 하나가 소비됨.
            // 양보하면 그 Continue()는 취소/null 소스를 보고 무시된다.
            await YarnTask.Yield();
        }
    }

    private void CancelPresenterLifetimeWaiters()
    {
        _presenterLifetimeCts?.Cancel();
        _presenterLifetimeCts?.Dispose();
        _presenterLifetimeCts = new CancellationTokenSource();
    }

    private void CancelLineVisualToken()
    {
        if (_lineVisualCts == null)
            return;

        _lineVisualCts.Cancel();
        _lineVisualCts.Dispose();
        _lineVisualCts = null;
    }

    private void RegisterPresenter(DialogueRunner dialogueRunner)
    {
        var presenters =
            new List<DialoguePresenterBase>(dialogueRunner.DialoguePresenters);

        presenters.Remove(this);

        int idx = presenters.FindIndex(x => x is LinePresenter);
        if (idx < 0)
            idx = presenters.Count;

        presenters.Insert(idx, this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    private void OnDestroy()
    {
        CancelLineVisualToken();

        _presenterLifetimeCts?.Cancel();
        _presenterLifetimeCts?.Dispose();
        _presenterLifetimeCts = null;
    }
}