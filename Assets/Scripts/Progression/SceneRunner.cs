using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 장면(Scene)진입에서 장면 끝(또는 챕터 끝, 멈춤)까지 진행.
//
// 흐름:
// 노드 재생 -> (리플레이면 루트로) -> 시청 기록 -> 판정 -> 선택 -> Via -> 다음 에피소드 … -> 장면 끝에 fold.
// 챕터 루프(어느 장면 다음에 어느 장면인가, 챕터 변수, 상태 소유)는 ProgressionDriver.
//
// 커밋 유예: 장면 안의 선택은 전부 미확정(pending)이다. 판정은 "진입 상태 + pending"을 접은
// 작업 상태로 하고, 장면을 나가는 순간 한 번에 확정·보고한다. 롤백은 pending을 자르는 것.
// 그래서 장면 안에서는 어디로든 돌아갈 수 있고, 장면 중간 멈춤은 pending을 버린다.
//
// 롤백 리플레이는 장면 루트부터 다시 진행. 그 길에 진행 선택지가 있었다면 기록대로 자동 선택 —
// 시크가 살아 있는 동안만. 표적에 닿아 시크가 꺼졌으면 그 뒤 선택은 사람이 다시 고른다.
public enum SceneRunOutcome
{
    SceneEnded = 0,   // 장면을 나가는 간선을 탔다. State.CurrentEpisodeId가 다음 장면의 루트.
    ChapterEnded = 1, // 나갈 길이 없다.
    Stopped = 2,      // 멈춤 요청. State는 진입 상태 그대로 — pending은 버렸다.
}

public readonly struct SceneRunResult
{
    public SceneRunOutcome Outcome { get; }
    public ProgressionState State { get; }

    public SceneRunResult(SceneRunOutcome outcome, ProgressionState state)
    {
        Outcome = outcome;
        State = state;
    }
}

public sealed class SceneRunner
{
    // 장면 안에서 지나온 진행 선택 하나 — 미확정. 리플레이의 자동 응답 근거이자 fold의 입력.
    private sealed class ProgressionPick
    {
        public EpisodeOption Option;
        public string FromEpisodeId;
        public int SourceIndex;

        // 선택 시점의 마지막 롤백 포인트. 롤백 표적이 이 앞이면 이 선택은 물린 것.
        public int Anchor;
    }

    // 다 본 EventKey 에피소드 — 보고는 fold에서. 물린 시청은 보고하지 않는다.
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
    private readonly Func<bool> _isStopRequested;

    private readonly List<ProgressionPick> _picks = new();
    private readonly List<WatchedEpisode> _watched = new();
    private readonly List<EpisodeOption> _foldBuffer = new();

    // 리플레이 자동 응답 커서. _picks.Count라면 자동 응답할 것 없음.
    private int _replayCursor;

    // 지금 장면에서 지나온 미확정 경로 — 즐겨찾기가 찍은 순간의 경로로 가져간다.
    public IReadOnlyList<CommittedChoice> PendingPath
    {
        get
        {
            var path = new List<CommittedChoice>(_replayCursor);

            for (int i = 0; i < _replayCursor; i++)
                path.Add(new CommittedChoice(_picks[i].FromEpisodeId, _picks[i].SourceIndex));

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
        Func<YarnVariableSnapshot> captureVariables,
        Func<bool> isStopRequested)
    {
        _backlog = backlog;
        _choiceHistory = choiceHistory;
        _captureVariables = captureVariables;
        _player = player;
        _options = options;
        _seek = seek;
        _rollbackHistory = rollbackHistory;
        _reporter = reporter;
        _isStopRequested = isStopRequested;

        // 선택지를 기다리는 중에 롤백이 오면 박스를 접는다. 기다리는 중이 아니면 아무 일도 없다.
        // 접힌 ShowAsync는 취소 예외로 깨어나고, RunAsync가 그것을 리플레이로 읽는다.
        _player.ReplayRequestedWhileIdle += _options.Cancel;
    }

    // 로드 중인가 — 표적에 닿을 때까지. 시간 실측용.
    private bool _loading;
    private float _loadStartedAt;

    // loadPlan이 있으면 루트에서 표적 라인까지 경로대로 달린다(Load 시크). 리플레이 자동 응답과 같은 기전.
    public async Task<SceneRunResult> RunAsync(
        ChapterProgression chapter, ProgressionState entryState, bool isNewSession, SavedLoadPlan loadPlan = null)
    {
        string rootEpisodeId = entryState.CurrentEpisodeId;

        chapter.TryGetNode(rootEpisodeId, out EpisodeNode root);

        _picks.Clear();
        _watched.Clear();
        _replayCursor = 0;
        _loading = false;

        Debug.Log($"[장면] 진입 — {chapter.SceneIdOf(rootEpisodeId)} @ {rootEpisodeId}");

        await _player.EnterSceneAsync(root.DialogueEntryId, isNewSession);

        // 진입 스냅샷 — 장면 기록의 앞부분. 아직 라인이 없으니 지금 [3]이 곧 진입값이다.
        _reporter.ReportSceneEntered(new SceneEntryReport(
            chapter.ChapterId, entryState, _captureVariables(), _backlog.NextSerial));

        if (loadPlan != null)
            BeginLoad(chapter, rootEpisodeId, loadPlan);

        string episodeId = rootEpisodeId;

        while (true)
        {
            chapter.TryGetNode(episodeId, out EpisodeNode node);

            if (!await PlayAsync(node.DialogueEntryId, "대사"))
            {
                if (_isStopRequested())
                    return Discard(entryState);

                episodeId = rootEpisodeId;
                continue;
            }

            NoteLoadProgress();
            NoteWatched(node);

            // 리플레이 자동 응답 — 커서만 옮긴다. 상태는 fold가 만든다.
            if (_replayCursor < _picks.Count && _seek.IsSeekingActive)
            {
                ProgressionPick pick = _picks[_replayCursor++];

                // 앵커는 이 노드의 마지막 라인 — 리플레이면 원래 값 그대로고, 로드로 미리 실린 기록은 여기서 처음 받는다.
                pick.Anchor = _rollbackHistory.LastHistoryIndex;

                Debug.Log($"[장면] 자동 응답 — {pick.Option}");

                if (pick.Option.HasVia && !await PlayAsync(pick.Option.ViaNodeId, "연출"))
                {
                    if (_isStopRequested())
                        return Discard(entryState);

                    episodeId = rootEpisodeId;
                    continue;
                }

                episodeId = pick.Option.TargetEpisodeId;

                if (!chapter.IsSameScene(pick.FromEpisodeId, episodeId))
                    return FinalizeScene(chapter, entryState, SceneRunOutcome.SceneEnded);

                continue;
            }

            // 시크가 꺼졌는데 기록이 남았다 - 표적이 선택 직전 라인인 것. 그 뒤는 사람이 고른다.
            if (_replayCursor < _picks.Count)
                _picks.RemoveRange(_replayCursor, _picks.Count - _replayCursor);

            // 기록이 없는데 시크가 살아 있다 - 표적을 못 찾은 채 노드가 끝남.
            // 끝까지 passThrough 대신, 멈추고 일반 재생으로. 로드였다면 정직한 퇴행 — 표적이 콘텐츠에서 사라진 것.
            if (_seek.IsSeekingActive)
            {
                Debug.LogWarning(_loading
                    ? "[장면] 로드 표적을 못 찾은 채 선택지에 닿았다 — 시크를 끄고 여기서 일반 재생으로. (콘텐츠가 바뀌었을 수 있다)"
                    : "[장면] 롤백 표적을 못 찾은 채 선택지에 닿았다 — 시크를 끄고 일반 재생으로.");
                _seek.ClearSeek();
                _loading = false;
            }

            // 판정은 작업 상태로 — 진입 상태에 지금까지의 선택을 접은 것.
            ProgressionState working = entryState.FoldChoices(chapter, PendingOptions());

            ChapterAdvance advance = ChapterTransition.Resolve(chapter, working);

            if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                return FinalizeScene(chapter, entryState, SceneRunOutcome.ChapterEnded);

            ResolvedOption? picked;

            if (advance.Kind == ChapterAdvanceKind.AutoAdvance)
            {
                // 자동 간선 — 묻지 않는다. 기록·pending·리플레이는 보통 선택과 똑같이 지나간다.
                picked = advance.Options[0];
                Debug.Log($"[장면] 자동 간선 — {picked.Value.Option}");
            }
            else
            {
                try
                {
                    picked = await PickAsync(advance);
                }
                catch (OperationCanceledException) when (_player.IsReplayPending)
                {
                    // 선택지 대기 중 롤백 — 박스가 접혔다. 멈춤(RequestStop)의 취소는 그대로 던져진다.
                    await BeginReplayAsync();

                    episodeId = rootEpisodeId;
                    continue;
                }
            }

            if (!picked.HasValue)
                return Discard(entryState);

            var chosen = new ProgressionPick
            {
                Option = picked.Value.Option,
                FromEpisodeId = node.EpisodeId,
                SourceIndex = picked.Value.SourceIndex,
                Anchor = _rollbackHistory.LastHistoryIndex,
            };

            _picks.Add(chosen);
            _replayCursor = _picks.Count;

            // 연출도 Story 노드. 아직 미확정 — Via 안에서 롤백하면 이 선택도 물릴 수 있다.
            if (chosen.Option.HasVia && !await PlayAsync(chosen.Option.ViaNodeId, "연출"))
            {
                if (_isStopRequested())
                    return Discard(entryState);

                episodeId = rootEpisodeId;
                continue;
            }

            episodeId = chosen.Option.TargetEpisodeId;

            if (!chapter.IsSameScene(chosen.FromEpisodeId, episodeId))
                return FinalizeScene(chapter, entryState, SceneRunOutcome.SceneEnded);
        }
    }

    // 로드 계획을 장면에 싣는다 — 경로를 미리 실린 기록으로, Yarn 선택을 ChoiceHistory로, 표적을 Load 시크로.
    // 경로가 챕터와 안 맞으면(콘텐츠 변경) 계획을 버리고 루트에서 일반 재생한다.
    private void BeginLoad(ChapterProgression chapter, string rootEpisodeId, SavedLoadPlan plan)
    {
        if (plan.Target == null || string.IsNullOrEmpty(plan.Target.NodeName))
        {
            Debug.LogWarning("[장면] 로드 계획에 표적이 없다 — 루트에서 시작.");
            return;
        }

        string cursor = rootEpisodeId;

        for (int i = 0; i < plan.Path.Count; i++)
        {
            SavedChoice step = plan.Path[i];

            if (!string.Equals(step.FromEpisodeId, cursor, StringComparison.Ordinal) ||
                !chapter.TryGetNode(cursor, out EpisodeNode node) ||
                step.OptionIndex < 0 || step.OptionIndex >= node.NextOptions.Count)
            {
                Debug.LogWarning(
                    $"[장면] 로드 경로가 챕터와 안 맞는다({i}번째, {step.FromEpisodeId}[{step.OptionIndex}]) — " +
                    "계획을 버리고 루트에서 시작.");
                _picks.Clear();
                return;
            }

            EpisodeOption option = node.NextOptions[step.OptionIndex];

            _picks.Add(new ProgressionPick
            {
                Option = option,
                FromEpisodeId = cursor,
                SourceIndex = step.OptionIndex,
                Anchor = -1, // 자동 응답 시점에 받는다.
            });

            cursor = option.TargetEpisodeId;
        }

        _replayCursor = 0;
        _choiceHistory.RestoreChoices(plan.YarnChoices);
        _seek.BeginLoadSeek(plan.Target.NodeName, plan.Target.LineId, plan.Target.Occurrence);

        _loading = true;
        _loadStartedAt = Time.realtimeSinceStartup;

        Debug.Log(
            $"[장면] 로드 — 루트 {rootEpisodeId}에서 {plan.Target.NodeName}/{plan.Target.LineId}#{plan.Target.Occurrence}까지. " +
            $"경로 {_picks.Count}개, Yarn 선택 {plan.YarnChoices.Count}개");
    }

    // 표적에 닿았는가(노드 하나가 끝난 뒤 확인). 로딩 화면이 필요한지 판단하는 실측.
    private void NoteLoadProgress()
    {
        if (!_loading || _seek.IsSeekingActive)
            return;

        _loading = false;

        Debug.Log($"[장면] 로드 도착 — {(Time.realtimeSinceStartup - _loadStartedAt) * 1000f:F0}ms");
    }

    // 노드 하나 진행. 끝까지 갔으면 true. 리플레이가 요청됐으면 되감아 두고 false
    // 호출자는 루트로 돌아간다. 멈춤이면 false이고 호출자가 멈춤 깃발을 본다.
    private async Task<bool> PlayAsync(string nodeName, string what)
    {
        Debug.Log($"[진행] {what} 시작 — \"{nodeName}\"");

        NodePlayOutcome outcome = await _player.PlayNodeAsync(nodeName);

        if (_isStopRequested())
            return false;

        if (outcome == NodePlayOutcome.ReplayRequested)
        {
            await BeginReplayAsync();
            return false;
        }

        Debug.Log($"[진행] {what} 끝 — \"{nodeName}\"");
        return true;
    }

    // 리플레이 직전. 표적 뒤의 선택·시청 기록을 물린다 - ChoiceHistory가 Yarn 선택에 하는 것과 같은 규칙.
    private async Task BeginReplayAsync()
    {
        await _player.PrepareReplayAsync();

        if (_rollbackHistory.TakeRollbackTarget(out RollbackPoint target))
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

        Debug.Log($"[장면] 리플레이 — 루트부터. 자동 응답할 선택 {_picks.Count}개");
    }

    // 장면 끝 — 여기가 커밋이다. pending을 순서대로 접어 확정 상태를 만들고, 그 순서대로 보고한다.
    // 스탯 반영과 이동이 한 연산이라는 규칙이 장면 단위로 선다.
    private SceneRunResult FinalizeScene(
        ChapterProgression chapter,
        ProgressionState entryState,
        SceneRunOutcome outcome)
    {
        ProgressionState state = entryState;

        var choices = new List<CommittedChoice>(_replayCursor);

        for (int i = 0; i < _replayCursor; i++)
        {
            ProgressionPick pick = _picks[i];

            state = state.ApplyChoice(chapter, pick.Option);
            choices.Add(new CommittedChoice(pick.FromEpisodeId, pick.SourceIndex));
        }

        var watched = new List<string>(_watched.Count);

        for (int i = 0; i < _watched.Count; i++)
            watched.Add(_watched[i].EpisodeId);

        // [3]은 여기서 굽는다 — 다음 장면 Enter 전이라 이 장면의 <<set>>까지만 든다.
        YarnVariableSnapshot variables = _captureVariables();

        Debug.Log($"[장면] 확정 — 선택 {choices.Count}개, 시청 {watched.Count}개 → {state.CurrentEpisodeId}");

        // 백로그도 여기서 굽는다 — 다음 장면 입장에서 "이전 장면들"이고, 다음 장면의 첫 순번은 지금 NextSerial.
        _reporter.ReportSceneCommitted(new SceneCommitReport(
            chapter.ChapterId,
            choices,
            _choiceHistory.CreateChoiceSnapshot(),
            watched,
            state,
            variables,
            backlog: new List<DialogueLogEntry>(_backlog.Entries),
            backlogSerialStart: _backlog.NextSerial,
            chapterCompleted: outcome == SceneRunOutcome.ChapterEnded));

        return new SceneRunResult(outcome, state);
    }

    // 장면 중간 멈춤 — pending은 확정하지도 보고하지도 않는다. 이어하기는 장면 처음부터.
    private SceneRunResult Discard(ProgressionState entryState)
    {
        if (_picks.Count > 0 || _watched.Count > 0)
            Debug.Log($"[장면] 멈춤 — 미확정 선택 {_picks.Count}개, 시청 {_watched.Count}개 버림");

        return new SceneRunResult(SceneRunOutcome.Stopped, entryState);
    }

    private IReadOnlyList<EpisodeOption> PendingOptions()
    {
        _foldBuffer.Clear();

        for (int i = 0; i < _replayCursor; i++)
            _foldBuffer.Add(_picks[i].Option);

        return _foldBuffer;
    }

    // 다 본 EventKey 에피소드를 적어 둔다. 같은 노드를 리플레이로 다시 끝내도 한 번만.
    private void NoteWatched(EpisodeNode node)
    {
        if (node.EventKey.Length == 0)
            return;

        for (int i = 0; i < _watched.Count; i++)
        {
            if (string.Equals(_watched[i].EpisodeId, node.EpisodeId, StringComparison.Ordinal))
                return;
        }

        _watched.Add(new WatchedEpisode
        {
            EpisodeId = node.EpisodeId,
            EventKey = node.EventKey,
            Anchor = _rollbackHistory.LastHistoryIndex,
        });
    }

    private async Task<ResolvedOption?> PickAsync(ChapterAdvance advance)
    {
        int picked = await _options.ShowAsync(
            advance.Options,
            advance.HiddenCount);

        if (_isStopRequested())
            return null;

        if (picked < 0 || picked >= advance.Options.Count)
            throw new ArgumentOutOfRangeException(
                nameof(picked),
                $"선택지는 {advance.Options.Count}개인데 {picked}번이 왔다.");

        ResolvedOption resolved = advance.Options[picked];

        if (!resolved.IsSelectable)
            throw new InvalidOperationException(
                $"잠긴 선택지다: [{resolved.Option.ChoiceLabel}] — {resolved.BlockingCondition}");

        return resolved;
    }
}
