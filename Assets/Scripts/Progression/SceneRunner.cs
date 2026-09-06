using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

#region Scene 동작 규칙

// 1) 장면 상태 규칙
// - EntryState는 장면 진입 시점의 확정 상태다.
// - 장면 안의 진행 선택은 History에 미확정 상태로 쌓인다.
// - 판정할 때는 EntryState + 현재까지의 pending으로 작업 상태를 만든다.
// - 장면을 나갈 때만 pending을 실제 상태에 반영한다.
// - 외부 중단은 OperationCanceledException으로 상위 Driver에 전달한다.
// - 중단된 장면의 pending은 확정하거나 보고하지 않는다.
//
// 2) 리플레이 규칙
// - ReplayRequested는 현재 SceneRunContext가 소유한다.
// - 리플레이는 항상 장면 루트에서 시작한다.
// - 롤백 표적 뒤에서 발생한 진행 선택과 시청 기록은 제거한다.
// - 표적까지는 이전 진행 선택과 Yarn 선택을 자동 응답한다.
// - 표적에 도착한 뒤의 남은 선택 기록은 버린다.
// - Via 안에서 롤백하면 Via 실행 전의 진행 선택도 되돌릴 수 있다.
// - ScenePlaybackSession은 Replay라는 의미를 모르고 Play / Stop / Restore만 담당한다.
//
// 3) 로드 규칙
// - Scene checkpoint로 장면 진입 상태를 복원한다.
// - SavedLoadPlan은 장면 루트에서 표적 라인까지 재생하는 계획이다.
// - 저장된 경로가 현재 챕터와 맞지 않으면 루트부터 다시 일반 재생한다.
// - 로드 표적을 찾지 못하고 선택지에 도착하면 Seek를 해제한다.
// - 로드 계획은 첫 장면에서 한 번 소비한다.
//
// 4) 커밋 규칙
// - 진행 선택을 원래 순서대로 상태에 반영한다.
// - 시청 기록은 동일 에피소드를 중복 보고하지 않는다.
// - Yarn 변수와 백로그는 다음 장면 진입 전에 캡처한다.
// - ReportSceneCommitted()는 장면당 최대 한 번 호출한다.

#endregion

public sealed class SceneRunner
{
    private readonly ScenePlaybackSession _playback;
    private readonly IChapterOptionsView _options;
    private readonly VNLinePresentationState _seek;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IProgressionReporter _reporter;
    private readonly BacklogRecorder _backlog;
    private readonly ChoiceHistory _choiceHistory;
    private readonly Func<YarnVariableSnapshot> _captureVariables;

    public SceneRunner(
        ScenePlaybackSession playback,
        IChapterOptionsView options,
        VNLinePresentationState seek,
        RollbackHistory rollbackHistory,
        IProgressionReporter reporter,
        BacklogRecorder backlog,
        ChoiceHistory choiceHistory,
        Func<YarnVariableSnapshot> captureVariables)
    {
        _playback = playback;
        _options = options;
        _seek = seek;
        _rollbackHistory = rollbackHistory;
        _reporter = reporter;
        _backlog = backlog;
        _choiceHistory = choiceHistory;
        _captureVariables = captureVariables;
    }

    public async Task<SceneRunResult> RunAsync(
        SceneRunContext ctx,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ScenePendingHistory history = ctx.History;

        try
        {
            // Phase: SceneEntering -> EntryReported
            SetPhase(ctx, SceneRunPhase.SceneEntering);

            await _playback.BeginSceneAsync();

            _backlog.MarkSceneStart();

            cancellationToken.ThrowIfCancellationRequested();

            SetPhase(ctx, SceneRunPhase.SceneEntered);

            _reporter.ReportSceneEntered(
                new SceneEntryReport(
                    ctx.Chapter.ChapterId,
                    ctx.EntryState,
                    _captureVariables(),
                    _backlog.NextSerial));

            SetPhase(ctx, SceneRunPhase.EntryReported);

            // Phase: LoadPlanApplied
            ApplyLoadPlan(ctx, history);

            SetPhase(ctx, SceneRunPhase.LoadPlanApplied);

            while (true)
            {
                EpisodeNode episode = ctx.CurrentEpisode;

                // Phase: EpisodePlaying -> EpisodeCompleted
                SetPhase(ctx, SceneRunPhase.EpisodePlaying);

                await PlayNodeAsync(
                    episode.DialogueEntryId,
                    "대사",
                    cancellationToken);

                if (ctx.ReplayRequested)
                {
                    await RestartReplayAsync(
                        ctx,
                        history,
                        cancellationToken);

                    continue;
                }

                history.NoteWatched(episode, _rollbackHistory.LastHistoryIndex);

                SetPhase(ctx, SceneRunPhase.EpisodeCompleted);

                // Phase: ChoiceResolving
                SetPhase(ctx, SceneRunPhase.ChoiceResolving);

                SceneChoiceResolution resolution;

                // Load / Replay 중 기존 progression 경로가 있으면 자동 응답한다.
                if (history.HasRecordedChoice && _seek.IsSeekingActive)
                {
                    SceneChoice recorded = history.TakeRecordedChoice(_rollbackHistory.LastHistoryIndex);
                    
                    resolution =
                        SceneChoiceResolution.FromChoice(recorded);
                }
                else
                {
                    // Seek 표적에 도착한 뒤 남아 있는 과거 선택은 버린다.
                    if (history.HasRecordedChoice)
                        history.DiscardUnconsumedChoices();

                    // 기록을 모두 소비했는데 Seek가 남아 있다면
                    // 표적을 찾지 못한 것이다.
                    if (_seek.IsSeekingActive)
                    {
                        Debug.LogWarning("[장면] 시크 표적을 못 찾은 채 선택지에 닿았다 -시크를 끄고 일반 재생으로.");
                        _seek.ClearSeek();
                    }

                    resolution = 
                        await ResolveNextChoiceAsync(ctx, history, episode, cancellationToken);
                }

                // 선택을 구한 직후 Replay가 들어왔을 수도 있다.
                // 이 경우 선택을 pending에 기록하거나 Chapter를 끝내지 않는다.
                if (ctx.ReplayRequested 
                    || resolution.Kind == SceneChoiceResolutionKind.ReplayRequested)
                {
                    await RestartReplayAsync(ctx, history, cancellationToken);
                    
                    continue;
                }

                if (resolution.Kind == SceneChoiceResolutionKind.ChapterEnded)
                {
                    return CommitScene(ctx, history, SceneRunOutcome.ChapterEnded);
                }

                SceneChoice choice = resolution.Choice;

                // Recorded 선택은 이미 history에 있다.
                // 새 선택은 Via 전에 기록해야 Via 안 rollback으로 되돌릴 수 있다.
                if (choice.Source != SceneChoiceSource.Recorded)
                    history.RecordChoice(choice, _rollbackHistory.LastHistoryIndex);

                SetPhase(ctx, SceneRunPhase.ChoiceResolved);

                // Phase: ViaPlaying
                if (choice.Option.HasVia)
                {
                    SetPhase(ctx, SceneRunPhase.ViaPlaying);
                    
                    await PlayNodeAsync(choice.Option.ViaNodeId, "연출", cancellationToken);

                    if (ctx.ReplayRequested)
                    {
                        await RestartReplayAsync(ctx, history, cancellationToken);
                        continue;
                    }
                }

                // Phase: TargetMoved
                ctx.CurrentEpisodeId = choice.Option.TargetEpisodeId;

                SetPhase(ctx, SceneRunPhase.TargetMoved);

                if (!ctx.Chapter.IsSameScene(choice.FromEpisodeId, ctx.CurrentEpisodeId))
                {
                    return CommitScene(ctx, history, SceneRunOutcome.SceneEnded);
                }
            }
        }
        catch (OperationCanceledException)
        {
            SetPhase(ctx, SceneRunPhase.Cancelled);
            throw;
        }
        catch
        {
            SetPhase(ctx, SceneRunPhase.Faulted);
            throw;
        }
    }

    // 현재 Scene transaction에 rollback replay를 요청한다.
    //
    // Replay 상태는 SceneRunContext가 소유한다.
    // Playback은 이유를 모르고 현재 재생을 중단할 뿐이다.
    public async Task RequestReplayAsync(SceneRunContext ctx)
    {
        if (ctx.ReplayRequested)
            return;

        ctx.ReplayRequested = true;

        // Node 재생 중이면 playback을 중단한다.
        // 선택지 대기 중이면 ShowAsync도 깨운다.
        Task stopTask = _playback.StopAsync();

        _options.Cancel();

        await stopTask;
    }

    // 전체 progression Cancellation은 Driver 책임.
    // 여기서는 현재 progression UI와 playback을 실제로 깨운다.
    public async Task StopAsync()
    {
        _options.Cancel();

        await _playback.StopAsync();
    }

    private async Task PlayNodeAsync(
        string nodeName,
        string description,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.Log($"[진행] {description} 시작 — \"{nodeName}\"");

        await _playback.PlayNodeAsync(nodeName);
    }

    private async Task<SceneChoiceResolution> ResolveNextChoiceAsync(
        SceneRunContext ctx,
        ScenePendingHistory history,
        EpisodeNode episode,
        CancellationToken cancellationToken)
    {
        if (ctx.ReplayRequested)
            return SceneChoiceResolution.ReplayRequested();

        ProgressionState working = 
            ctx.EntryState.FoldChoices(ctx.Chapter, history.PendingOptions());

        ChapterAdvance advance = 
            ChapterTransition.Resolve(ctx.Chapter, working);

        if (ctx.ReplayRequested)
            return SceneChoiceResolution.ReplayRequested();

        if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
            return SceneChoiceResolution.ChapterEnded();

        if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
        {
            ResolvedOption resolved = advance.Options[0];

            Debug.Log($"[장면] 자동 간선 - {resolved.Option}");

            return SceneChoiceResolution.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    episode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.AutoAdvance));
        }

        if (ctx.ReplayRequested)
            return SceneChoiceResolution.ReplayRequested();

        // 선택지 도중 Replay 요청 시, 기다리는 걸 그만두고 Replay 진행.
        try
        {
            ResolvedOption resolved = 
                await PickAsync(advance, cancellationToken);

            if (ctx.ReplayRequested)
                return SceneChoiceResolution.ReplayRequested();

            return SceneChoiceResolution.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    episode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.User));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested
                  && ctx.ReplayRequested)
        {
            return SceneChoiceResolution.ReplayRequested();
        }
    }

    private async Task<ResolvedOption> PickAsync(
        ChapterAdvance advance,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int picked = await _options.ShowAsync(
            advance.Options,
            advance.HiddenCount);

        cancellationToken.ThrowIfCancellationRequested();

        if (picked < 0 || picked >= advance.Options.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(picked),
                $"선택지는 {advance.Options.Count}개인데 {picked}번이 왔다.");
        }

        ResolvedOption resolved = advance.Options[picked];

        if (!resolved.IsSelectable)
        {
            throw new InvalidOperationException(
                $"잠긴 선택지다: [{resolved.Option.ChoiceLabel}] — " +
                $"{resolved.BlockingCondition}");
        }

        return resolved;
    }

    private async Task RestartReplayAsync(
        SceneRunContext ctx,
        ScenePendingHistory history,
        CancellationToken cancellationToken)
    {
        SetPhase(ctx, SceneRunPhase.Replaying);

        cancellationToken.ThrowIfCancellationRequested();

        // RequestReplayAsync에서 시작된 Stop이 아직 진행 중이면
        // ScenePlaybackSession이 내부에서 완료를 기다린 뒤 복원한다.
        await _playback.PrepareReplayAsync();

        cancellationToken.ThrowIfCancellationRequested();

        if (_rollbackHistory.TakeRollbackTarget(out RollbackPoint target))
            history.RewindAfter(target.historyIndex);

        history.RestartReplay();
        ctx.CurrentEpisodeId = ctx.RootEpisodeId;

        // 현재 Replay 요청을 모두 소비했다.
        ctx.ReplayRequested = false;

        Debug.Log(
            $"[장면] 리플레이 — 루트부터. " +
            $"자동 응답할 선택 {history.RecordedChoiceCount}개");
    }

    private void ApplyLoadPlan(
        SceneRunContext ctx,
        ScenePendingHistory history)
    {
        SavedLoadPlan plan = ctx.LoadPlan;

        if (plan == null)
            return;

        if (plan.Target == null || string.IsNullOrEmpty(plan.Target.NodeName))
        {
            Debug.LogWarning("[장면] 로드 계획에 표적이 없다 - 루트에서 시작.");
            return;
        }

        string cursor = ctx.RootEpisodeId;

        for (int i = 0; i < plan.Path.Count; i++)
        {
            SavedChoice step = plan.Path[i];

            if (!TryResolveSavedChoice(
                    ctx.Chapter,
                    cursor,
                    step,
                    out EpisodeOption option))
            {
                Debug.LogWarning(
                    $"[장면] 로드 경로가 챕터와 안 맞는다" +
                    $"({i}번째, {step.FromEpisodeId}[{step.OptionIndex}]) — " +
                    "계획을 버리고 루트에서 시작.");

                history.ClearChoices();

                return;
            }

            history.RestoreChoice(option, cursor, step.OptionIndex);

            cursor = option.TargetEpisodeId;
        }

        _choiceHistory.RestoreChoices(plan.YarnChoices);

        _seek.BeginLoadSeek(
            plan.Target.NodeName,
            plan.Target.LineId,
            plan.Target.Occurrence);

        Debug.Log(
            $"[장면] 로드 — 루트 {ctx.RootEpisodeId}에서 " +
            $"{plan.Target.NodeName}/{plan.Target.LineId}#{plan.Target.Occurrence}까지. " +
            $"경로 {history.RecordedChoiceCount}개, " +
            $"Yarn 선택 {plan.YarnChoices.Count}개");
    }

    private static bool TryResolveSavedChoice(
        ChapterProgression chapter,
        string cursor,
        SavedChoice step,
        out EpisodeOption option)
    {
        option = null;

        if (!string.Equals(step.FromEpisodeId, cursor, StringComparison.Ordinal))
            return false;

        if (!chapter.TryGetNode(cursor, out EpisodeNode episode))
            return false;

        if (step.OptionIndex < 0 || step.OptionIndex >= episode.NextOptions.Count)
            return false;

        option = episode.NextOptions[step.OptionIndex];

        return true;
    }

    private SceneRunResult CommitScene(
        SceneRunContext ctx,
        ScenePendingHistory history,
        SceneRunOutcome outcome)
    {
        SetPhase(ctx, SceneRunPhase.SceneCommitting);

        ProgressionState state = 
            history.FoldInto(ctx.Chapter, ctx.EntryState);

        List<CommittedChoice> choices =
            history.CreateCommittedChoices();

        List<string> watched =
            history.CreateWatchedEpisodeIds();

        YarnVariableSnapshot variables =
            _captureVariables();

        Debug.Log(
            $"[장면] 확정 — 선택 {choices.Count}개, " +
            $"시청 {watched.Count}개 → {state.CurrentEpisodeId}");

        _reporter.ReportSceneCommitted(
            new SceneCommitReport(
                ctx.Chapter.ChapterId,
                choices,
                _choiceHistory.CreateChoiceSnapshot(),
                watched,
                state,
                variables,
                new List<DialogueLogEntry>(_backlog.Entries),
                _backlog.NextSerial,
                outcome == SceneRunOutcome.ChapterEnded));

        SetPhase(ctx, SceneRunPhase.Completed);

        return new SceneRunResult(outcome, state);
    }

    private static void SetPhase(SceneRunContext ctx, SceneRunPhase phase)
    {
        ctx.Phase = phase;
    }
}