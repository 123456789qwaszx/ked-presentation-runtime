using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 한 Scene transaction의 진행 순서를 조율한다.
//
// Episode
// → Choice
// → Via
// → Target
// → Replay 또는 Scene/Chapter Commit
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

    private ScenePendingHistory _currentHistory;

    public SceneRunPhase CurrentPhase { get; private set; } = SceneRunPhase.None;

    public IReadOnlyList<CommittedChoice> PendingPath =>
        _currentHistory?.CreatePendingPath() ?? Array.Empty<CommittedChoice>();

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

        _playback.ReplayRequested += _options.Cancel;
    }

    public async Task<SceneRunResult> RunAsync(
        SceneRunContext ctx,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var history = new ScenePendingHistory();
        _currentHistory = history;

        // null이면 현재 Load Seek가 아니다.
        float? loadStartedAt = null;

        try
        {
            // Phase: SceneEntering -> EntryReported
            SetPhase(ctx, SceneRunPhase.SceneEntering);

            await _playback.BeginSceneAsync();

            // Backlog의 Scene 경계는 progression transaction이 소유한다.
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
            loadStartedAt = ApplyLoadPlan(ctx, history);

            SetPhase(ctx, SceneRunPhase.LoadPlanApplied);

            while (true)
            {
                EpisodeNode episode = ctx.CurrentEpisode;

                // Phase: EpisodePlaying -> EpisodeCompleted
                SetPhase(ctx, SceneRunPhase.EpisodePlaying);

                NodePlayOutcome episodeOutcome = await PlayNodeAsync(
                    episode.DialogueEntryId,
                    "대사",
                    cancellationToken);

                if (episodeOutcome == NodePlayOutcome.ReplayRequested)
                {
                    loadStartedAt = null;

                    await RestartReplayAsync(ctx, history, cancellationToken);
                    continue;
                }

                if (loadStartedAt.HasValue && !_seek.IsSeekingActive)
                {
                    float elapsed = (Time.realtimeSinceStartup - loadStartedAt.Value) * 1000f;

                    Debug.Log($"[장면] 로드 도착 - {elapsed:F0}ms");

                    loadStartedAt = null;
                }

                history.NoteWatched(
                    episode,
                    _rollbackHistory.LastHistoryIndex);

                SetPhase(ctx, SceneRunPhase.EpisodeCompleted);

                // Phase: ChoiceResolving
                SetPhase(ctx, SceneRunPhase.ChoiceResolving);

                SceneChoiceResolution resolution;

                // Load / Replay 중 이전 progression 선택이 있으면 자동 응답한다.
                if (history.HasRecordedChoice && _seek.IsSeekingActive)
                {
                    SceneChoice recorded = history.TakeRecordedChoice(
                        _rollbackHistory.LastHistoryIndex);

                    Debug.Log($"[장면] 자동 응답 — {recorded.Option}");

                    resolution = SceneChoiceResolution.FromChoice(recorded);
                }
                else
                {
                    // Seek 표적 이후의 과거 경로는 새 진행에서 사용하지 않는다.
                    if (history.HasRecordedChoice)
                        history.DiscardUnconsumedChoices();

                    // 기록을 모두 소비했는데 Seek가 남아 있다면 표적을 찾지 못했다.
                    if (_seek.IsSeekingActive)
                    {
                        Debug.LogWarning(
                            loadStartedAt.HasValue
                                ? "[장면] 로드 표적을 못 찾은 채 선택지에 닿았다 — " +
                                  "시크를 끄고 여기서 일반 재생으로. (콘텐츠가 바뀌었을 수 있다)"
                                : "[장면] 롤백 표적을 못 찾은 채 선택지에 닿았다 — " +
                                  "시크를 끄고 일반 재생으로.");

                        _seek.ClearSeek();
                        loadStartedAt = null;
                    }

                    resolution = await ResolveNextChoiceAsync(
                        ctx,
                        history,
                        episode,
                        cancellationToken);
                }

                if (resolution.Kind == SceneChoiceResolutionKind.ReplayRequested)
                {
                    loadStartedAt = null;

                    await RestartReplayAsync(ctx, history, cancellationToken);
                    continue;
                }

                if (resolution.Kind == SceneChoiceResolutionKind.ChapterEnded)
                    return CommitScene(ctx, history, SceneRunOutcome.ChapterEnded);

                SceneChoice choice = resolution.Choice;

                // Recorded 선택은 이미 history에 들어 있다.
                //
                // 새 선택은 Via 전에 기록해야
                // Via 내부 rollback에서 선택 자체도 되돌릴 수 있다.
                if (choice.Source != SceneChoiceSource.Recorded)
                {
                    history.RecordChoice(
                        choice,
                        _rollbackHistory.LastHistoryIndex);
                }

                SetPhase(ctx, SceneRunPhase.ChoiceResolved);

                // Phase: ViaPlaying
                if (choice.Option.HasVia)
                {
                    SetPhase(ctx, SceneRunPhase.ViaPlaying);

                    NodePlayOutcome viaOutcome = await PlayNodeAsync(
                        choice.Option.ViaNodeId,
                        "연출",
                        cancellationToken);

                    if (viaOutcome == NodePlayOutcome.ReplayRequested)
                    {
                        loadStartedAt = null;

                        await RestartReplayAsync(ctx, history, cancellationToken);
                        continue;
                    }
                }

                // Phase: TargetMoved
                ctx.CurrentEpisodeId = choice.Option.TargetEpisodeId;

                SetPhase(ctx, SceneRunPhase.TargetMoved);

                if (!ctx.Chapter.IsSameScene(choice.FromEpisodeId, ctx.CurrentEpisodeId))
                    return CommitScene(ctx, history, SceneRunOutcome.SceneEnded);
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
        finally
        {
            if (ReferenceEquals(_currentHistory, history))
                _currentHistory = null;
        }
    }

    // 전체 progression Cancellation은 Driver 책임.
    // 여기서는 현재 UI와 playback을 실제로 깨운다.
    public async Task StopAsync()
    {
        _options.Cancel();
        await _playback.StopAsync();
    }

    private async Task<NodePlayOutcome> PlayNodeAsync(
        string nodeName,
        string description,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.Log($"[진행] {description} 시작 — \"{nodeName}\"");

        NodePlayOutcome outcome = await _playback.PlayNodeAsync(nodeName);

        // 외부 Stop으로 Yarn이 끝났다면 정상 node 완료로 해석하지 않는다.
        cancellationToken.ThrowIfCancellationRequested();

        if (outcome == NodePlayOutcome.ReplayRequested)
            return NodePlayOutcome.ReplayRequested;

        Debug.Log($"[진행] {description} 끝 — \"{nodeName}\"");

        return NodePlayOutcome.Completed;
    }

    private async Task<SceneChoiceResolution> ResolveNextChoiceAsync(
        SceneRunContext ctx,
        ScenePendingHistory history,
        EpisodeNode episode,
        CancellationToken cancellationToken)
    {
        ProgressionState working = ctx.EntryState.FoldChoices(
            ctx.Chapter,
            history.PendingOptions());

        ChapterAdvance advance = ChapterTransition.Resolve(
            ctx.Chapter,
            working);

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

        try
        {
            ResolvedOption resolved = await PickAsync(
                advance,
                cancellationToken);

            return SceneChoiceResolution.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    episode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.User));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested
                  && _playback.IsReplayPending)
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

        await _playback.PrepareReplayAsync();

        cancellationToken.ThrowIfCancellationRequested();

        if (_rollbackHistory.TakeRollbackTarget(out RollbackPoint target))
            history.RewindAfter(target.historyIndex);

        history.RestartReplay();
        ctx.CurrentEpisodeId = ctx.RootEpisodeId;

        Debug.Log(
            $"[장면] 리플레이 — 루트부터. " +
            $"자동 응답할 선택 {history.RecordedChoiceCount}개");
    }

    // null이면 Load Seek가 시작되지 않았다.
    private float? ApplyLoadPlan(
        SceneRunContext ctx,
        ScenePendingHistory history)
    {
        SavedLoadPlan plan = ctx.LoadPlan;

        if (plan == null)
            return null;

        if (plan.Target == null || string.IsNullOrEmpty(plan.Target.NodeName))
        {
            Debug.LogWarning("[장면] 로드 계획에 표적이 없다 - 루트에서 시작.");
            return null;
        }

        string cursor = ctx.RootEpisodeId;

        for (int i = 0; i < plan.Path.Count; i++)
        {
            SavedChoice step = plan.Path[i];

            if (!TryResolveSavedChoice(ctx.Chapter, cursor, step, out EpisodeOption option))
            {
                Debug.LogWarning(
                    $"[장면] 로드 경로가 챕터와 안 맞는다" +
                    $"({i}번째, {step.FromEpisodeId}[{step.OptionIndex}]) — " +
                    "계획을 버리고 루트에서 시작.");

                history.ClearChoices();
                return null;
            }

            history.RestoreChoice(
                option,
                cursor,
                step.OptionIndex);

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
            $"경로 {history.RecordedChoiceCount}개, Yarn 선택 {plan.YarnChoices.Count}개");

        return Time.realtimeSinceStartup;
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

        ProgressionState state = history.FoldInto(
            ctx.Chapter,
            ctx.EntryState);

        List<CommittedChoice> choices = history.CreateCommittedChoices();
        List<string> watched = history.CreateWatchedEpisodeIds();
        YarnVariableSnapshot variables = _captureVariables();

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

    private void SetPhase(
        SceneRunContext ctx,
        SceneRunPhase phase)
    {
        ctx.Phase = phase;
        CurrentPhase = phase;
    }
}