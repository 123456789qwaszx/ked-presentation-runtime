using System;
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

    private readonly IScenePresentation _presentation;
    private readonly IChapterOptionsView _options;
    private readonly ProgressionReplayState _replayState;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IScenePersistence _persistence;
    private readonly BacklogRecorder _backlog;

    // Replay 요청과 실행 루프가 같은 Stop 완료를 기다린다.
    private Task _playbackStopTask = Task.CompletedTask;

    public SceneRunner(
        IScenePresentation presentation,
        IChapterOptionsView options,
        ProgressionReplayState replayState,
        RollbackHistory rollbackHistory,
        IScenePersistence persistence,
        BacklogRecorder backlog)
    {
        _presentation = presentation;
        _options = options;
        _replayState = replayState;
        _rollbackHistory = rollbackHistory;
        _persistence = persistence;
        _backlog = backlog;
    }

    public async Task<SceneRunResult> RunAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SceneProgress progress = session.Progress;

        await EnterSceneAsync(progress, cancellationToken);

        if (session.StartsFromRestore)
            _replayState.BeginLoadReplay();

        while (true)
        {
            SceneStepKind step =
                await RunEpisodeStepAsync(session, cancellationToken);

            switch (step)
            {
                case SceneStepKind.Continue:
                    continue;

                case SceneStepKind.Replay:
                    await RestartReplayAsync(session, cancellationToken);
                    continue;

                case SceneStepKind.SceneEnded:
                    return CommitScene(progress, SceneRunOutcome.SceneEnded);

                case SceneStepKind.ChapterEnded:
                    return CommitScene(progress, SceneRunOutcome.ChapterEnded);

                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }
        }
    }

    public async Task RequestReplayAsync(SceneRunSession scene)
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
        SceneProgress progress,
        CancellationToken cancellationToken)
    {
        await StopPlaybackAsync();

        cancellationToken.ThrowIfCancellationRequested();

        _presentation.BeginScene();

        _backlog.MarkSceneStart();

        _persistence.EnterScene(
            progress.Definition.ChapterId,
            progress.SceneId,
            progress.EntryState);
    }

    private async Task<SceneStepKind> RunEpisodeStepAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        SceneProgress progress = session.Progress;

        cancellationToken.ThrowIfCancellationRequested();

        await _presentation.PlayEpisodeAsync(progress.CurrentEpisode.DialogueEntryId);

        cancellationToken.ThrowIfCancellationRequested();

        if (session.ReplayPending)
            return SceneStepKind.Replay;

        progress.NoteCurrentEpisodeWatched(
            _rollbackHistory.LastHistoryIndex);

        SceneChoiceResolution resolution;

        if (progress.HasRecordedChoice && _replayState.IsSeekingActive)
        {
            SceneChoice recorded =
                progress.TakeRecordedChoice(
                    _rollbackHistory.LastHistoryIndex);

            resolution =
                SceneChoiceResolution.FromChoice(recorded);
        }
        else
        {
            if (progress.HasRecordedChoice)
                progress.DiscardUnconsumedChoices();

            if (_replayState.IsSeekingActive)
            {
                Debug.LogWarning(
                    "[장면] 시크 표적을 못 찾은 채 선택지에 닿았다 - 시크를 끄고 일반 재생으로 전환한다.");

                _replayState.ClearSeek();
            }

            resolution =
                await ResolveNextChoiceAsync(
                    session,
                    cancellationToken);
        }

        if (session.ReplayPending ||
            resolution.Kind == SceneChoiceResolutionKind.ReplayRequested)
        {
            return SceneStepKind.Replay;
        }

        if (resolution.Kind == SceneChoiceResolutionKind.ChapterEnded)
            return SceneStepKind.ChapterEnded;

        SceneChoice choice = resolution.Choice;

        if (choice.Source != SceneChoiceSource.Recorded)
        {
            progress.RecordChoice(
                choice,
                _rollbackHistory.LastHistoryIndex);
        }

        progress.MoveTo(choice.Option.TargetEpisodeId);

        if (!progress.Definition.IsSameScene(
                choice.FromEpisodeId,
                progress.CurrentEpisodeId))
        {
            return SceneStepKind.SceneEnded;
        }

        return SceneStepKind.Continue;
    }

    private async Task<SceneChoiceResolution> ResolveNextChoiceAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        if (session.ReplayPending)
            return SceneChoiceResolution.ReplayRequested();

        ChapterAdvance advance =
            ChapterTransition.Resolve(
                session.Progress.Definition,
                session.Progress.WorkingState);

        if (session.ReplayPending)
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
                    session.Progress.CurrentEpisode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.AutoAdvance));
        }

        if (session.ReplayPending)
            return SceneChoiceResolution.ReplayRequested();

        try
        {
            ResolvedOption resolved =
                await PickAsync(advance, cancellationToken);

            if (session.ReplayPending)
                return SceneChoiceResolution.ReplayRequested();

            return SceneChoiceResolution.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    session.Progress.CurrentEpisode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.User));
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested &&
                  session.ReplayPending)
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
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // RequestReplayAsync가 시작한 Stop이 끝날 때까지 기다린다.
        await _playbackStopTask;

        // Stop을 기다리는 동안 New Game/Load/Exit가 요청됐을 수 있다.
        cancellationToken.ThrowIfCancellationRequested();

        _presentation.PrepareReplay();

        if (_rollbackHistory.TakeRollbackTarget(out RollbackPoint target))
            session.Progress.RewindAfter(target.historyIndex);

        session.Progress.RestartReplay();
        session.ClearReplayRequest();
    }

    private Task StopPlaybackAsync()
    {
        if (!_playbackStopTask.IsCompleted)
            return _playbackStopTask;

        _playbackStopTask = _presentation.StopAsync();
        return _playbackStopTask;
    }

    private SceneRunResult CommitScene(
        SceneProgress progress,
        SceneRunOutcome outcome)
    {
        SceneCommitResult commit = progress.CreateCommitResult();

        _persistence.CommitScene(
            progress.Definition.ChapterId,
            progress.SceneId,
            commit,
            outcome);

        return new SceneRunResult(
            outcome,
            commit.State);
    }
}
