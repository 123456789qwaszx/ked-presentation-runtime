using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

public enum SceneRunOutcome
{
    // 장면 밖으로 나가는 간선을 탔다.
    // State.CurrentEpisodeId는 다음 장면의 루트다.
    SceneEnded = 0,

    // 더 이상 진행할 간선이 없다.
    ChapterEnded = 1,

    // 외부 중단 요청.
    // State는 장면 진입 상태 그대로이며 pending은 버린다.
    Stopped = 2,
}

public readonly struct SceneRunResult
{
    public SceneRunOutcome Outcome { get; }
    
    // 장면(Scene)이 아닌 챕터의 상태지만,
    // Scene종류 시 새롭게 만들어서 커밋.
    public ProgressionState State { get; }

    public SceneRunResult(
        SceneRunOutcome outcome,
        ProgressionState state)
    {
        Outcome = outcome;
        State = state;
    }
}

// 챕터 내 한 장면(Scene) 진행
// - checkPoint, pending 초기화.
//
// <Pending = 장면 안의 선택>
// - 장면 안의 선택은 모두 미확정(pending).
// - 장면 나갈 시, 한번에 상태에 반영 및 세이브.
// - 롤백은 pending을 되돌리고, 장면 중간 중단은 pending 전체를 버림.
public sealed class SceneRunner
{
    // 장면 내 하나의 에피소드가 끝날 시, 제시되는 진행 선택지.(계약: 에피소드 1 = 선택지 1)
    private sealed class ProgressionPick
    {
        public EpisodeOption Option;
        public string FromEpisodeId;
        public int SourceIndex;

        // 선택 시점의 마지막 롤백 포인트.
        public int Anchor;
    }

    // 장면 안에서 끝까지 본 EventKey 에피소드. (장면 종료 시 보고)
    private sealed class WatchedEpisode
    {
        public string EpisodeId;
        public string EventKey;
        public int Anchor;
    }

    private readonly EpisodePlayer _player;
    private readonly IChapterOptionsView _options;
    private readonly VNLinePresentationState _seek;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IProgressionReporter _reporter;
    private readonly BacklogRecorder _backlog;
    private readonly ChoiceHistory _choiceHistory;
    private readonly Func<YarnVariableSnapshot> _captureVariables;

    private readonly List<ProgressionPick> _picks = new();
    private readonly List<WatchedEpisode> _watched = new();
    private readonly List<EpisodeOption> _foldBuffer = new();

    // 리플레이 자동 응답 커서.
    // _picks.Count와 같으면 자동 응답할 선택지가 없다.
    private int _replayCursor;

    // 로드 표적까지 이동 중인지 나타낸다.
    // 도착 시간 측정에 사용한다.
    private bool _loading;
    private float _loadStartedAt;

    // 현재 장면에서 지나온 미확정 경로.
    // 즐겨찾기 저장 시 현재 위치를 캡처하는 데 사용한다.
    public IReadOnlyList<CommittedChoice> PendingPath
    {
        get
        {
            var path = new List<CommittedChoice>(_replayCursor);

            for (int i = 0; i < _replayCursor; i++)
            {
                ProgressionPick pick = _picks[i];

                path.Add(new CommittedChoice(
                    pick.FromEpisodeId,
                    pick.SourceIndex));
            }

            return path;
        }
    }

    public SceneRunner(
        EpisodePlayer player,
        IChapterOptionsView options,
        VNLinePresentationState seek,
        RollbackHistory rollbackHistory,
        IProgressionReporter reporter,
        BacklogRecorder backlog,
        ChoiceHistory choiceHistory,
        Func<YarnVariableSnapshot> captureVariables)
    {
        _player = player;
        _options = options;
        _seek = seek;
        _rollbackHistory = rollbackHistory;
        _reporter = reporter;
        _backlog = backlog;
        _choiceHistory = choiceHistory;
        _captureVariables = captureVariables;

        // 선택지를 기다리는 중에 롤백이 요청되면 선택지 박스를 닫는다.
        //
        // ShowAsync는 취소 예외로 깨어나며,
        // RunAsync가 이를 롤백 리플레이 요청으로 처리한다.
        _player.ReplayRequestedWhileIdle += _options.Cancel;
    }

    public async Task<SceneRunResult> RunAsync(
        ChapterProgression chapter,
        ProgressionState entryState,
        bool isNewSession,
        Func<bool> isStopRequested,
        SavedLoadPlan loadPlan = null)
    {
        if (isStopRequested())
            return new SceneRunResult(SceneRunOutcome.Stopped, entryState);

        string rootEpisodeId = entryState.CurrentEpisodeId;

        chapter.TryGetNode(rootEpisodeId, out EpisodeNode root);

        _picks.Clear();
        _watched.Clear();

        _replayCursor = 0;
        _loading = false;

        Debug.Log($"[장면] 진입 - {chapter.SceneIdOf(rootEpisodeId)} @ {rootEpisodeId}");

        await _player.EnterSceneAsync(root.DialogueEntryId, isNewSession);

        if (isStopRequested())
            return new SceneRunResult(SceneRunOutcome.Stopped, entryState);

        _reporter.ReportSceneEntered(
            new SceneEntryReport(
                chapter.ChapterId, 
                entryState, 
                _captureVariables(),
                _backlog.NextSerial));

        if (loadPlan != null)
            BeginLoad(chapter, rootEpisodeId, loadPlan);

        string episodeId = rootEpisodeId;

        while (true)
        {
            chapter.TryGetNode(episodeId, out EpisodeNode node);

            bool dialogueCompleted = await PlayAsync(node.DialogueEntryId, "대사", isStopRequested);

            if (!dialogueCompleted)
            {
                if (isStopRequested())
                    return new SceneRunResult(SceneRunOutcome.Stopped, entryState);

                episodeId = rootEpisodeId;
                continue;
            }

            NoteLoadProgress();
            NoteWatched(node);

            // 리플레이 자동 응답.
            //
            // 이미 기록된 진행 선택이 있고 Seek가 활성 상태라면
            // 사용자에게 다시 묻지 않고 기존 선택을 사용한다.
            if (_replayCursor < _picks.Count &&
                _seek.IsSeekingActive)
            {
                ProgressionPick pick =
                    _picks[_replayCursor++];

                // 현재 노드의 마지막 라인을 선택의 롤백 기준점으로 삼는다.
                //
                // 리플레이에서는 원래 값과 같고,
                // 로드로 미리 넣은 선택은 여기서 처음 기준점을 얻는다.
                pick.Anchor = _rollbackHistory.LastHistoryIndex;

                Debug.Log($"[장면] 자동 응답 — {pick.Option}");

                if (pick.Option.HasVia)
                {
                    bool viaCompleted = await PlayAsync(
                        pick.Option.ViaNodeId,
                        "연출",
                        isStopRequested);

                    if (!viaCompleted)
                    {
                        if (isStopRequested())
                            return new SceneRunResult(SceneRunOutcome.Stopped, entryState);

                        episodeId = rootEpisodeId;
                        continue;
                    }
                }

                episodeId = pick.Option.TargetEpisodeId;

                if (!chapter.IsSameScene(pick.FromEpisodeId, episodeId))
                    return FinalizeScene(chapter, entryState, SceneRunOutcome.SceneEnded);

                continue;
            }

            // Seek는 끝났지만 기록된 선택이 남았다.
            //
            // 표적이 선택 직전 라인이었던 경우이므로
            // 표적 이후의 기존 선택은 버리고 사용자에게 다시 묻는다.
            if (_replayCursor < _picks.Count)
            {
                _picks.RemoveRange(
                    _replayCursor,
                    _picks.Count - _replayCursor);
            }

            // 자동 응답할 기록은 없는데 Seek가 여전히 살아 있다.
            //
            // 저장 또는 롤백 표적을 찾지 못하고 선택지까지 도착한 경우다.
            // 콘텐츠가 바뀌었을 수 있으므로 여기부터 일반 재생으로 전환한다.
            if (_seek.IsSeekingActive)
            {
                Debug.LogWarning(
                    _loading
                        ? "[장면] 로드 표적을 못 찾은 채 선택지에 닿았다 — " +
                          "시크를 끄고 여기서 일반 재생으로. " +
                          "(콘텐츠가 바뀌었을 수 있다)"
                        : "[장면] 롤백 표적을 못 찾은 채 선택지에 닿았다 — " +
                          "시크를 끄고 일반 재생으로.");

                _seek.ClearSeek();
                _loading = false;
            }

            // 아직 확정하지 않은 선택들을 진입 상태에 임시로 접어
            // 현재 선택지를 판정할 작업 상태를 만든다.
            ProgressionState working =
                entryState.FoldChoices(chapter, PendingOptions());

            ChapterAdvance advance =
                ChapterTransition.Resolve(chapter, working);

            if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                return FinalizeScene(chapter, entryState, SceneRunOutcome.ChapterEnded);

            ResolvedOption? picked;

            if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
            {
                picked = advance.Options[0];
                Debug.Log($"[장면] 자동 간선 - {picked.Value.Option}");
            }
            else
            {
                try
                {
                    picked = await PickAsync(advance, isStopRequested);
                }
                catch (OperationCanceledException)when (_player.IsReplayPending)
                {
                    // 선택지를 기다리는 중 롤백이 요청.
                    //
                    // 외부 중단으로 인한 취소는 여기서 잡지 않고
                    // ProgressionDriver까지 전달.
                    await BeginReplayAsync();

                    episodeId = rootEpisodeId;
                    continue;
                }
            }

            if (!picked.HasValue)
                return new SceneRunResult(SceneRunOutcome.Stopped, entryState);

            var chosen = new ProgressionPick
            {
                Option = picked.Value.Option,
                FromEpisodeId = node.EpisodeId,
                SourceIndex = picked.Value.SourceIndex,
                Anchor = _rollbackHistory.LastHistoryIndex,
            };

            _picks.Add(chosen);
            _replayCursor = _picks.Count;

            // Via도 Story 노드이며 아직 미확정이다.
            // Via 안에서 롤백하면 이 선택까지 되돌릴 수 있다.
            if (chosen.Option.HasVia)
            {
                bool viaCompleted = await PlayAsync(
                    chosen.Option.ViaNodeId,
                    "연출",
                    isStopRequested);

                if (!viaCompleted)
                {
                    if (isStopRequested())
                        return new SceneRunResult(SceneRunOutcome.Stopped, entryState);

                    episodeId = rootEpisodeId;
                    continue;
                }
            }

            episodeId =
                chosen.Option.TargetEpisodeId;

            if (!chapter.IsSameScene(
                    chosen.FromEpisodeId,
                    episodeId))
            {
                return FinalizeScene(
                    chapter,
                    entryState,
                    SceneRunOutcome.SceneEnded);
            }
        }
    }

    // 타이틀로 나가기나 갈라지기에서 현재 장면을 중단한다.
    //
    // SceneRunner는 실제 중단 방법을 소유한다.
    // 전체 진행을 중단할지 결정하고 중단 상태를 기록하는 것은
    // ProgressionDriver의 책임이다.
    public async Task StopAsync()
    {
        // 노드와 노드 사이에서 진행 선택지를 기다리고 있을 수 있다.
        _options.Cancel();

        // Yarn 노드, 라인 연출, 롤백 포인트,
        // 샷 응답 및 Presentation Scope를 정리한다.
        await _player.StopSceneAsync();
    }

    // 로드 계획을 장면에 적용한다.
    //
    // 진행 경로를 자동 응답 기록으로 복원하고,
    // Yarn 선택 기록과 표적 라인을 Seek 상태로 설정한다.
    //
    // 경로가 현재 챕터와 맞지 않으면
    // 계획을 버리고 장면 루트부터 일반 재생한다.
    private void BeginLoad(
        ChapterProgression chapter,
        string rootEpisodeId, 
        SavedLoadPlan plan)
    {
        if (plan.Target == null || string.IsNullOrEmpty(plan.Target.NodeName))
        {
            Debug.LogWarning("[장면] 로드 계획에 표적이 없다 - 루트에서 시작.");
            return;
        }

        string cursor = rootEpisodeId;

        for (int i = 0; i < plan.Path.Count; i++)
        {
            SavedChoice step = plan.Path[i];

            if (!string.Equals(step.FromEpisodeId, cursor, StringComparison.Ordinal)
                || !chapter.TryGetNode(cursor, out EpisodeNode node) 
                || step.OptionIndex < 0 
                || step.OptionIndex >= node.NextOptions.Count)
            {
                Debug.LogWarning(
                    $"[장면] 로드 경로가 챕터와 안 맞는다" +
                    $"({i}번째, " +
                    $"{step.FromEpisodeId}[{step.OptionIndex}]) — " +
                    "계획을 버리고 루트에서 시작.");

                _picks.Clear();
                return;
            }

            EpisodeOption option = node.NextOptions[step.OptionIndex];

            _picks.Add(
                new ProgressionPick 
                {
                    Option = option,
                    FromEpisodeId = cursor,
                    SourceIndex = step.OptionIndex,

                    // 실제 자동 응답 시점에 롤백 기준점을 받는다.
                    Anchor = -1,
                });

            cursor = option.TargetEpisodeId;
        }

        _replayCursor = 0;

        _choiceHistory.RestoreChoices(plan.YarnChoices);

        _seek.BeginLoadSeek(
            plan.Target.NodeName,
            plan.Target.LineId,
            plan.Target.Occurrence);

        _loading = true;
        _loadStartedAt = Time.realtimeSinceStartup;

        Debug.Log(
            $"[장면] 로드 — 루트 {rootEpisodeId}에서 " +
            $"{plan.Target.NodeName}/" +
            $"{plan.Target.LineId}#" +
            $"{plan.Target.Occurrence}까지. " +
            $"경로 {_picks.Count}개, " +
            $"Yarn 선택 {plan.YarnChoices.Count}개");
    }

    // 로드 표적에 도착했는지 확인하고 걸린 시간을 기록한다.
    private void NoteLoadProgress()
    {
        if (!_loading || _seek.IsSeekingActive)
            return;

        _loading = false;
        
        float elapsedMilliseconds = (Time.realtimeSinceStartup - _loadStartedAt) * 1000f;
        
        Debug.Log($"[장면] 로드 도착 - {elapsedMilliseconds:F0}ms");
    }

    // 노드 하나를 실행한다.
    //
    // 정상 완료면 true.
    // 리플레이나 외부 중단이면 false를 반환한다.
    private async Task<bool> PlayAsync(
        string nodeName,
        string what,
        Func<bool> isStopRequested)
    {
        Debug.Log($"[진행] {what} 시작 — \"{nodeName}\"");

        NodePlayOutcome outcome =
            await _player.PlayNodeAsync(nodeName);

        if (isStopRequested())
            return false;

        if (outcome == NodePlayOutcome.ReplayRequested)
        {
            await BeginReplayAsync();
            return false;
        }

        Debug.Log($"[진행] {what} 끝 — \"{nodeName}\"");

        return true;
    }

    // 리플레이 직전 준비.
    //
    // 표적 뒤에 만들어진 진행 선택과 시청 기록을 되돌린다.
    // ChoiceHistory가 Yarn 선택 기록을 되돌리는 것과 같은 규칙이다.
    private async Task BeginReplayAsync()
    {
        await _player.PrepareReplayAsync();

        if (_rollbackHistory.TakeRollbackTarget(
                out RollbackPoint target))
        {
            for (int i = _picks.Count - 1; i >= 0; i--)
            {
                if (_picks[i].Anchor > target.historyIndex)
                    _picks.RemoveAt(i);
            }

            for (int i = _watched.Count - 1; i >= 0; i--)
            {
                if (_watched[i].Anchor > target.historyIndex)
                    _watched.RemoveAt(i);
            }
        }

        _replayCursor = 0;

        Debug.Log(
            $"[장면] 리플레이 — 루트부터. " +
            $"자동 응답할 선택 {_picks.Count}개");
    }

    // 장면 종료 커밋.
    //
    // pending을 순서대로 접어 확정 상태를 만들고,
    // 같은 순서로 저장 계층에 보고한다.
    //
    // 여기서 스탯 반영과 다음 에피소드 이동이
    // 하나의 장면 단위 연산으로 확정된다.
    private SceneRunResult FinalizeScene(
        ChapterProgression chapter,
        ProgressionState entryState,
        SceneRunOutcome outcome)
    {
        ProgressionState state = entryState;

        var choices =
            new List<CommittedChoice>(_replayCursor);

        for (int i = 0; i < _replayCursor; i++)
        {
            ProgressionPick pick = _picks[i];

            state = state.ApplyChoice(
                chapter,
                pick.Option);

            choices.Add(
                new CommittedChoice(
                    pick.FromEpisodeId,
                    pick.SourceIndex));
        }

        var watched =
            new List<string>(_watched.Count);

        for (int i = 0; i < _watched.Count; i++)
            watched.Add(_watched[i].EpisodeId);

        // 다음 장면에 진입하기 전에 현재 Yarn 변수를 캡처한다.
        // 따라서 이번 장면에서 수행된 set 명령까지만 포함한다.
        YarnVariableSnapshot variables =
            _captureVariables();

        Debug.Log(
            $"[장면] 확정 — " +
            $"선택 {choices.Count}개, " +
            $"시청 {watched.Count}개 " +
            $"→ {state.CurrentEpisodeId}");

        // 다음 장면 입장에서 현재 백로그는 이전 장면들의 기록이다.
        // NextSerial은 다음 장면의 첫 백로그 순번이 된다.
        _reporter.ReportSceneCommitted(
            new SceneCommitReport(
                chapter.ChapterId,
                choices,
                _choiceHistory.CreateChoiceSnapshot(),
                watched,
                state,
                variables,
                backlog:
                    new List<DialogueLogEntry>(_backlog.Entries),
                backlogSerialStart:
                    _backlog.NextSerial,
                chapterCompleted:
                    outcome == SceneRunOutcome.ChapterEnded));

        return new SceneRunResult(
            outcome,
            state);
    }

    private IReadOnlyList<EpisodeOption> PendingOptions()
    {
        _foldBuffer.Clear();

        for (int i = 0; i < _replayCursor; i++)
            _foldBuffer.Add(_picks[i].Option);

        return _foldBuffer;
    }

    // 끝까지 본 EventKey 에피소드를 기록한다.
    // 같은 노드를 리플레이로 다시 끝내더라도 한 번만 기록한다.
    private void NoteWatched(EpisodeNode node)
    {
        if (node.EventKey.Length == 0)
            return;

        for (int i = 0; i < _watched.Count; i++)
        {
            if (string.Equals(
                    _watched[i].EpisodeId,
                    node.EpisodeId,
                    StringComparison.Ordinal))
            {
                return;
            }
        }

        _watched.Add(
            new WatchedEpisode
            {
                EpisodeId = node.EpisodeId,
                EventKey = node.EventKey,
                Anchor = _rollbackHistory.LastHistoryIndex,
            });
    }

    private async Task<ResolvedOption?> PickAsync(
        ChapterAdvance advance,
        Func<bool> isStopRequested)
    {
        int picked = await _options.ShowAsync(
            advance.Options,
            advance.HiddenCount);

        if (isStopRequested())
            return null;

        if (picked < 0 ||
            picked >= advance.Options.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(picked),
                $"선택지는 {advance.Options.Count}개인데 " +
                $"{picked}번이 왔다.");
        }

        ResolvedOption resolved =
            advance.Options[picked];

        if (!resolved.IsSelectable)
        {
            throw new InvalidOperationException(
                $"잠긴 선택지다: " +
                $"[{resolved.Option.ChoiceLabel}] — " +
                $"{resolved.BlockingCondition}");
        }

        return resolved;
    }
}