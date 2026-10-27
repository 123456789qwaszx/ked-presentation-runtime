using System;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 게임에서 Scene 실행 순서를 소유하는 유일한 runner.
// 진행 상태 계산은 SceneProgress에 위임하고, 재생 단위의 수명(열기·정지·루트부터 다시)은 PlaybackUnit에 맡긴다.
// 여기 남는 것은 진행의 판단이다 — 무엇을 재생할지, 선택을 어떻게 정할지, 장면 경계와 확정, 되감을 때 진행 기록을 어디까지 자를지.
//
// 이어하기의 줄 시크는 여기서 열지 않는다. 런처가 연출 기억을 되돌려 두면(PlaybackMemory.Restore)
// 복원 장면을 열 때 PlaybackUnit이 연다. 여기는 "시크 중인가"만 물어 진행 선택을 기록으로 자동 응답한다.
public sealed class SceneRunner : ISceneRunner
{
    /// <summary>
    /// 챕터가 끝났다 — 마지막 장면을 확정(저장)한 뒤, 진행 런타임에 돌려주기 전에 부른다. 끝 카드를 띄우는 자리다.
    /// 진행 런타임(ProgressionDriver)은 끝을 알리지 않는다 — 판단이 여기(장면 실행)서 나므로 여기서 알린다.
    /// 받는 것: 챕터 정의 · 마지막으로 본 에피소드 · 끝난 상태(확정된 스탯).
    /// </summary>
    public event Action<ChapterDefinition, EpisodeNode, ChapterState> ChapterEnded;

    private enum SceneStepKind
    {
        Continue,
        Replay,
        SceneEnded,
        ChapterEnded,
    }

    private readonly PlaybackUnit _unit;
    private readonly IChapterOptionsView _options;
    private readonly IScenePersistence _persistence;

    public SceneRunner(
        PlaybackUnit unit,
        IChapterOptionsView options,
        IScenePersistence persistence)
    {
        _unit = unit;
        _options = options;
        _persistence = persistence;
    }

    #region Runner Control
    
    public async Task<SceneRunResult> RunAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SceneProgress progress = session.Progress;

        await EnterSceneAsync(progress, cancellationToken);

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
                {
                    SceneRunResult result = CommitScene(progress, SceneRunOutcome.ChapterEnded);
                    ChapterEnded?.Invoke(progress.Definition, progress.CurrentEpisode, result.ExitState);
                    return result;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(step), step, null);
            }
        }
    }

    public async Task RequestReplayAsync(SceneRunSession scene)
    {
        if (!scene.TryRequestReplay())
            return;

        Task stopTask = _unit.StopAsync();

        _options.Cancel();

        await stopTask;
    }

    public async Task StopAsync()
    {
        _options.Cancel();
        await _unit.StopAsync();
    }
    
    #endregion
    
    #region Scene Lifecycle
    
    private async Task EnterSceneAsync(
        SceneProgress progress,
        CancellationToken cancellationToken)
    {
        await _unit.OpenAsync(cancellationToken);

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
    
    #endregion
    
    #region Episode Execution
    
    private async Task<SceneStepKind> RunEpisodeStepAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        SceneProgress progress = session.Progress;

        cancellationToken.ThrowIfCancellationRequested();

        await _unit.PlayAsync(progress.CurrentEpisode.DialogueEntryId);

        cancellationToken.ThrowIfCancellationRequested();

        if (session.ReplayPending)
        {
            return SceneStepKind.Replay;
        }

        progress.NoteCurrentEpisodeWatched(_unit.LastHistoryIndex);

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
            progress.RecordChoice(choice, _unit.LastHistoryIndex);

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
        if (_unit.IsSeeking && progress.HasRecordedChoice)
        {
            SceneChoice recorded =
                progress.TakeRecordedChoice(_unit.LastHistoryIndex);

            return SceneChoiceResult.FromChoice(recorded);
        }

        // 여기부터는 일반 진행 경로.
        // Seeking으로 소비하지 못한 과거 선택 기록은 더 이상 유효하지 않다.
        if (progress.HasRecordedChoice)
            progress.DiscardUnconsumedChoices();

        if (_unit.IsSeeking)
        {
            Debug.LogError(
                "[장면] Seeking 중 선택지에 닿았으나, 기록된 선택이 없다" +
                " - 시크를 끄고 일반 재생으로 전환.");

            _unit.AbandonSeek();
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
    
    #endregion
    
    #region Replay
    
    private async Task RestartReplayAsync(
        SceneRunSession session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 재생 단위가 정지 완료를 기다리고 무대를 루트로 되돌린다. 롤백 표적이 있으면 그 색인을 준다.
        int? rollbackTarget = await _unit.RestartFromRootAsync(cancellationToken);

        // 진행의 몫 — 표적 뒤의 진행 선택·시청 기록을 자르고 커서를 장면 루트로.
        if (rollbackTarget.HasValue)
            session.Progress.RollbackTo(rollbackTarget.Value);

        session.Progress.ResetForReplay();
        session.CompleteReplayRequest();
    }
    
    #endregion
}
