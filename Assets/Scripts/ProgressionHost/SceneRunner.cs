using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 게임에서 Scene 실행 순서를 소유하는 유일한 runner.
// 진행 상태 계산은 SceneProgress에 위임하고 playback/replay/lifecycle 순서만 조립한다.
public sealed class SceneRunner : ISceneRunner
{
    private enum SceneStepKind
    {
        Continue,
        Replay,
        SceneEnded,
        ChapterEnded,
    }

    private readonly IScenePlayback _playback;
    private readonly IChapterOptionsView _options;
    private readonly ProgressionReplayState _replayState;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IScenePersistence _persistence;
    private readonly BacklogRecorder _backlog;

    // Replay 요청과 실행 루프가 같은 Stop 완료를 기다린다.
    private Task _playbackStopTask = Task.CompletedTask;

    public SceneRunner(
        IScenePlayback playback,
        IChapterOptionsView options,
        ProgressionReplayState replayState,
        RollbackHistory rollbackHistory,
        IScenePersistence persistence,
        BacklogRecorder backlog)
    {
        _playback = playback;
        _options = options;
        _replayState = replayState;
        _rollbackHistory = rollbackHistory;
        _persistence = persistence;
        _backlog = backlog;
    }

    public async Task<SceneRunResult> RunAsync(
        SceneRunContext ctx,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SceneProgress progression = ctx.Progress;

        try
        {
            await EnterSceneAsync(ctx, cancellationToken);

            ApplyRestorePath(ctx, progression);

            while (true)
            {
                SceneStepKind step =
                    await RunEpisodeStepAsync(ctx, progression, cancellationToken);

                switch (step)
                {
                    case SceneStepKind.Continue:
                        continue;

                    case SceneStepKind.Replay:
                        await RestartReplayAsync(ctx, progression, cancellationToken);
                        continue;

                    case SceneStepKind.SceneEnded:
                        return CommitScene(ctx, progression, SceneRunOutcome.SceneEnded);

                    case SceneStepKind.ChapterEnded:
                        return CommitScene(ctx, progression, SceneRunOutcome.ChapterEnded);

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(step),
                            step,
                            "알 수 없는 장면 실행 결과다.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task RequestReplayAsync(SceneRunContext scene)
    {
        if (!scene.RequestReplay())
            return;

        Task stopTask = StopPlaybackAsync();
        _options.Cancel();
        await stopTask;
    }

    public async Task StopAsync()
    {
        _options.Cancel();
        await StopPlaybackAsync();
    }

    private async Task EnterSceneAsync(
        SceneRunContext ctx,
        CancellationToken cancellationToken)
    {
        await StopPlaybackAsync();

        cancellationToken.ThrowIfCancellationRequested();

        await _playback.BeginSceneAsync();

        cancellationToken.ThrowIfCancellationRequested();

        _backlog.MarkSceneStart();

        _persistence.EnterScene(
            ctx.Progress.Definition.ChapterId,
            ctx.Progress.SceneId,
            ctx.Progress.EntryState);
    }

    private async Task<SceneStepKind> RunEpisodeStepAsync(
        SceneRunContext scene,
        SceneProgress progression,
        CancellationToken cancellationToken)
    {
        EpisodeNode episode = progression.CurrentEpisode;

        await PlayNodeAsync(
            episode.DialogueEntryId,
            "대사",
            cancellationToken);

        if (scene.ReplayPending)
            return SceneStepKind.Replay;

        progression.NoteCurrentEpisodeWatched(_rollbackHistory.LastHistoryIndex);

        SceneChoiceResolution resolution;

        if (progression.HasRecordedChoice && _replayState.IsSeekingActive)
        {
            SceneChoice recorded =
                progression.TakeRecordedChoice(_rollbackHistory.LastHistoryIndex);

            resolution = SceneChoiceResolution.FromChoice(recorded);
        }
        else
        {
            if (progression.HasRecordedChoice)
                progression.DiscardUnconsumedChoices();

            if (_replayState.IsSeekingActive)
            {
                Debug.LogWarning(
                    "[장면] 시크 표적을 못 찾은 채 선택지에 닿았다 - 시크를 끄고 일반 재생으로 전환한다.");

                _replayState.ClearSeek();
            }

            resolution =
                await ResolveNextChoiceAsync(
                    scene,
                    progression,
                    episode,
                    cancellationToken);
        }

        if (scene.ReplayPending ||
            resolution.Kind == SceneChoiceResolutionKind.ReplayRequested)
        {
            return SceneStepKind.Replay;
        }

        if (resolution.Kind == SceneChoiceResolutionKind.ChapterEnded)
            return SceneStepKind.ChapterEnded;

        SceneChoice choice = resolution.Choice;

        // 커서를 옮기기 전에 기록한다 — 기록과 이동 사이에서 replay가 걸려도
        // 이미 고른 경로를 그대로 다시 따라갈 수 있어야 한다.
        if (choice.Source != SceneChoiceSource.Recorded)
            progression.RecordChoice(choice, _rollbackHistory.LastHistoryIndex);

        progression.MoveTo(choice.Option.TargetEpisodeId);

        if (!scene.Progress.Definition.IsSameScene(choice.FromEpisodeId, progression.CurrentEpisodeId))
            return SceneStepKind.SceneEnded;

        return SceneStepKind.Continue;
    }

    private async Task PlayNodeAsync(
        string nodeName,
        string description,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Debug.LogWarning($"[진행] {description} 시작 — \"{nodeName}\"");

        await _playback.PlayNodeAsync(nodeName);

        // Stop/New Game/Manual Load처럼 run 자체를 폐기하는 요청이
        // playback 대기를 깨운 직후 정상 progression으로 이어지지 않게 한다.
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<SceneChoiceResolution> ResolveNextChoiceAsync(
        SceneRunContext ctx,
        SceneProgress progression,
        EpisodeNode episode,
        CancellationToken cancellationToken)
    {
        if (ctx.ReplayPending)
            return SceneChoiceResolution.ReplayRequested();

        ChapterAdvance advance =
            ChapterTransition.Resolve(
                ctx.Progress.Definition,
                progression.WorkingState);

        if (ctx.ReplayPending)
            return SceneChoiceResolution.ReplayRequested();

        if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
            return SceneChoiceResolution.ChapterEnded();

        if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
        {
            ResolvedOption resolved = advance.Options[0];

            Debug.LogWarning($"[장면] 자동 간선 - {resolved.Option}");

            return SceneChoiceResolution.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    episode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.AutoAdvance));
        }

        if (ctx.ReplayPending)
            return SceneChoiceResolution.ReplayRequested();

        try
        {
            ResolvedOption resolved =
                await PickAsync(advance, cancellationToken);

            if (ctx.ReplayPending)
                return SceneChoiceResolution.ReplayRequested();

            return SceneChoiceResolution.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    episode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.User));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested &&
                  ctx.ReplayPending)
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
        SceneProgress progression,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _playbackStopTask;

        cancellationToken.ThrowIfCancellationRequested();

        await _playback.PrepareReplayAsync();

        cancellationToken.ThrowIfCancellationRequested();

        if (_rollbackHistory.TakeRollbackTarget(out RollbackPoint target))
            progression.RewindAfter(target.historyIndex);

        progression.RestartReplay();
        ctx.ClearReplayRequest();

        Debug.LogWarning(
            $"[장면] 리플레이 — 루트부터. " +
            $"자동 응답할 선택 {progression.RecordedChoiceCount}개");
    }

    private Task StopPlaybackAsync()
    {
        if (!_playbackStopTask.IsCompleted)
            return _playbackStopTask;

        _playbackStopTask = _playback.StopAsync();
        return _playbackStopTask;
    }

    private void ApplyRestorePath(
        SceneRunContext ctx,
        SceneProgress progression)
    {
        IReadOnlyList<ScenePathStep> path = ctx.RestorePath;

        // null : NewGame
        if (path == null)
            return;

        progression.RestorePath(path);
        _replayState.BeginLoadReplay();
    }

    private SceneRunResult CommitScene(
        SceneRunContext ctx,
        SceneProgress progression,
        SceneRunOutcome outcome)
    {
        SceneCommitResult commitResult = progression.CreateCommitResult();

        Debug.LogWarning(
            $"[장면] 확정 — 선택 {commitResult.Choices.Count}개, " +
            $"시청 {commitResult.WatchedEpisodeIds.Count}개 → {commitResult.State.CurrentEpisodeId}");

        _persistence.CommitScene(
            ctx.Progress.Definition.ChapterId,
            ctx.Progress.SceneId,
            commitResult,
            outcome);

        return new SceneRunResult(outcome, commitResult.State);
    }
}
