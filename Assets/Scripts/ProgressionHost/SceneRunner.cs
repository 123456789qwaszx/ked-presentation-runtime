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
    private Task _stopPlaybackTask = Task.CompletedTask;

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
        if (!scene.TryRequestReplay())
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

        _backlog.MarkSceneBoundary();

        _persistence.EnterScene(
            progress.Definition.ChapterId,
            progress.EntryState);
    }

    private SceneRunResult CommitScene(
        SceneProgress progress,
        SceneRunOutcome outcome)
    {
        SceneCommitResult commit = progress.CreateCommitResult();

        _persistence.CommitScene(
            progress.Definition.ChapterId,
            commit,
            outcome);

        return new SceneRunResult(
            outcome,
            commit.State);
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
        {
            return SceneStepKind.Replay;
        }

        progress.NoteCurrentEpisodeWatched(_rollbackHistory.LastHistoryIndex);

        SceneChoiceResult choiceResult =
            await ResolveSceneChoiceAsync(session, cancellationToken);

        if (session.ReplayPending 
            || choiceResult.Kind == SceneChoiceResultKind.ReplayRequested)
        {
            return SceneStepKind.Replay;
        }

        if (choiceResult.Kind == SceneChoiceResultKind.ChapterEnded)
        {
            return SceneStepKind.ChapterEnded;
        }

        SceneChoice choice = choiceResult.Choice;

        // 새로 결정된 선택만 기록
        // (만약 Recorded라면 Load나 Replay 중 기존 선택 기록을 다시 소비한 것임)
        if (choice.Source != SceneChoiceSource.Recorded)
            progress.RecordChoice(choice, _rollbackHistory.LastHistoryIndex);

        // 선택 기록과 별개로 실제 진행 위치를 이동한다.
        progress.AdvanceTo(choice.Option.TargetEpisodeId);

        // 씬 경계를 넘었는 지 확인
        if (!progress.Definition.IsSameScene(choice.FromEpisodeId, progress.CurrentEpisodeId))
        {
            return SceneStepKind.SceneEnded;
        }

        return SceneStepKind.Continue;
    }

    private async Task<SceneChoiceResult> ResolveSceneChoiceAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        SceneProgress progress = session.Progress;

        // Seeking 중이고 기존 선택 기록이 있다면
        // 새 선택을 결정하지 않고 기록된 선택을 그대로 소비한다.
        if (_replayState.IsSeekingActive && progress.HasRecordedChoice)
        {
            SceneChoice recorded =
                progress.TakeRecordedChoice(_rollbackHistory.LastHistoryIndex);

            return SceneChoiceResult.FromChoice(recorded);
        }

        // 여기부터는 일반 진행 경로.
        // Seeking으로 소비하지 못한 과거 선택 기록은 더 이상 유효하지 않다.
        if (progress.HasRecordedChoice)
            progress.DiscardUnconsumedChoices();

        if (_replayState.IsSeekingActive)
        {
            Debug.LogError(
                "[장면] Seeking 중 선택지에 닿았으나, 기록된 선택이 없다" +
                " - 시크를 끄고 일반 재생으로 전환.");

            _replayState.ClearSeek();
        }

        return await ResolveNextChoiceAsync(session, cancellationToken);
    }

    // 세 가지 중 하나 반환
    // 1. 자동 간선 또는 사용자 선택
    // 2. Chapter 종료
    // 3. 선택 UI를 기다리는 중 들어온 Replay 요청
    private async Task<SceneChoiceResult> ResolveNextChoiceAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        if (session.ReplayPending)
            return SceneChoiceResult.ReplayRequested();

        ChapterAdvance advance =
            ChapterTransition.Resolve(
                session.Progress.Definition,
                session.Progress.EffectiveState);

        if (session.ReplayPending)
            return SceneChoiceResult.ReplayRequested();

        if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
            return SceneChoiceResult.ChapterEnded();

        if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
        {
            ResolvedOption resolved = advance.Options[0];

            Debug.Log($"[장면] 자동 간선 - {resolved.Option}");

            return SceneChoiceResult.FromChoice(
                new SceneChoice(
                    resolved.Option,
                    session.Progress.CurrentEpisode.EpisodeId,
                    resolved.SourceIndex,
                    SceneChoiceSource.AutoAdvance));
        }

        if (session.ReplayPending)
            return SceneChoiceResult.ReplayRequested();

        try
        {
            ResolvedOption resolved =
                await PickAsync(advance, cancellationToken);

            if (session.ReplayPending)
                return SceneChoiceResult.ReplayRequested();

            return SceneChoiceResult.FromChoice(
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
            return SceneChoiceResult.ReplayRequested();
        }
    }

    private async Task<ResolvedOption> PickAsync(
        ChapterAdvance advance,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int picked = 
            await _options.ShowAsync(advance.Options, advance.HiddenCount);

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
        await _stopPlaybackTask;

        cancellationToken.ThrowIfCancellationRequested();

        _presentation.PrepareReplay();

        if (_rollbackHistory.TakeRollbackTarget(out RollbackPoint target))
            session.Progress.RollbackTo(target.historyIndex);

        session.Progress.ResetForReplay();
        session.CompleteReplayRequest();
    }

    private Task StopPlaybackAsync()
    {
        if (!_stopPlaybackTask.IsCompleted)
            return _stopPlaybackTask;

        _stopPlaybackTask = _presentation.StopAsync();
        return _stopPlaybackTask;
    }
}
