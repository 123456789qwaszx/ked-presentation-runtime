using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 장면(Scene)진입에서 장면 끝(또는 챕터 끝, 멈춤)까지 진행.
//
// 흐름:
// 노드 재생 -> (리플레이면 루트로) -> 시청 보고 -> 판정 -> 선택 -> Via -> 커밋 -> 다음 에피소드.
// 챕터 루프(어느 장면 다음에 어느 장면인가, 챕터 변수, 상태 소유)는 ProgressionDriver.
//
// 롤백 리플레이는 장면(Scene) 루트부터 다시 진행.
// 그 길에 진행 선택지가 있었다면 기록대로 자동 선택.
//
// 리플레이 중 커밋은 다시 적용하지 않음.
// 커밋되지 않은 채 끊긴 선택(Via 도중 롤백)만 리플레이가 Via를 지난 뒤 커밋.
public enum SceneRunOutcome
{
    SceneEnded = 0,   // 장면을 나가는 간선을 탔다. State.CurrentEpisodeId가 다음 장면의 루트.
    ChapterEnded = 1, // 나갈 길이 없다.
    Stopped = 2,      // 멈춤 요청.
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
    // 장면 안에서 지나온 진행 선택 하나. 리플레이의 자동 응답 근거.
    private sealed class ProgressionPick
    {
        public EpisodeOption Option;
        public string FromEpisodeId;
        public int SourceIndex;

        // 선택 시점의 마지막 롤백 포인트.
        public int Anchor;

        public bool Committed;
    }

    private readonly EpisodePlayer _player;
    private readonly IChapterOptionsView _options;
    private readonly VNLinePresentationState _seek;
    private readonly RollbackHistory _rollbackHistory;
    private readonly IProgressionReporter _reporter;
    private readonly Func<bool> _isStopRequested;

    private readonly List<ProgressionPick> _picks = new();
    private readonly HashSet<string> _watchedReported = new(StringComparer.Ordinal);

    // 리플레이 자동 응답 커서. _picks.Count라면 자동 응답할 것 없음.
    private int _replayCursor;

    public SceneRunner(
        EpisodePlayer player,
        IChapterOptionsView options,
        VNLinePresentationState seek,
        RollbackHistory rollbackHistory,
        IProgressionReporter reporter,
        Func<bool> isStopRequested)
    {
        _player = player;
        _options = options;
        _seek = seek;
        _rollbackHistory = rollbackHistory;
        _reporter = reporter;
        _isStopRequested = isStopRequested;
    }

    public async Task<SceneRunResult> RunAsync(
        ChapterProgression chapter, ProgressionState entryState, bool isNewSession)
    {
        string rootEpisodeId = entryState.CurrentEpisodeId;
        ProgressionState state = entryState;

        chapter.TryGetNode(rootEpisodeId, out EpisodeNode root);

        _picks.Clear();
        _watchedReported.Clear();
        _replayCursor = 0;

        Debug.Log($"[장면] 진입 — {chapter.SceneIdOf(rootEpisodeId)} @ {rootEpisodeId}");

        await _player.EnterSceneAsync(root.DialogueEntryId, isNewSession);
        _rollbackHistory.ResetRollbackFloor();

        string episodeId = rootEpisodeId;

        while (true)
        {
            chapter.TryGetNode(episodeId, out EpisodeNode node);

            if (!await PlayAsync(node.DialogueEntryId, "대사"))
            {
                if (_isStopRequested())
                    return new SceneRunResult(SceneRunOutcome.Stopped, state);

                episodeId = rootEpisodeId;
                continue;
            }

            ReportWatchedOnce(chapter, node);

            // 리플레이 자동 응답
            if (_replayCursor < _picks.Count && _seek.IsSeekingActive)
            {
                ProgressionPick pick = _picks[_replayCursor++];

                Debug.Log($"[장면] 자동 응답 — {pick.Option}");

                if (pick.Option.HasVia && !await PlayAsync(pick.Option.ViaNodeId, "연출"))
                {
                    if (_isStopRequested())
                        return new SceneRunResult(SceneRunOutcome.Stopped, state);

                    episodeId = rootEpisodeId;
                    continue;
                }

                if (!pick.Committed)
                    state = Commit(chapter, state, pick);

                episodeId = pick.Option.TargetEpisodeId;

                if (!chapter.IsSameScene(pick.FromEpisodeId, episodeId))
                    return new SceneRunResult(SceneRunOutcome.SceneEnded, state);

                continue;
            }

            // 시크가 꺼졌는데 기록이 남았다 - 표적이 선택 직전 라인인 것.
            // 그 때부턴 선택.
            if (_replayCursor < _picks.Count)
                _picks.RemoveRange(_replayCursor, _picks.Count - _replayCursor);

            // 기록이 없는데 시크가 살아 있다 - 표적을 못 찾은 채 노드가 끝남.
            // 끝까지 passThrough 대신, 멈추고 일반 재생으로.(차후 룰 고정할 것)
            if (_seek.IsSeekingActive)
            {
                Debug.LogWarning("[장면] 롤백 표적을 못 찾은 채 선택지에 닿았다 — 시크를 끄고 일반 재생으로.");
                _seek.ClearSeek();
            }

            ChapterAdvance advance = ChapterTransition.Resolve(chapter, state);

            if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                return new SceneRunResult(SceneRunOutcome.ChapterEnded, state);

            ResolvedOption? picked = await PickAsync(advance);

            if (!picked.HasValue)
                return new SceneRunResult(SceneRunOutcome.Stopped, state);

            var chosen = new ProgressionPick
            {
                Option = picked.Value.Option,
                FromEpisodeId = node.EpisodeId,
                SourceIndex = picked.Value.SourceIndex,
                Anchor = _rollbackHistory.LastHistoryIndex,
                Committed = false,
            };

            _picks.Add(chosen);
            _replayCursor = _picks.Count;

            // 연출도 Story 노드.
            if (chosen.Option.HasVia && !await PlayAsync(chosen.Option.ViaNodeId, "연출"))
            {
                if (_isStopRequested())
                    return new SceneRunResult(SceneRunOutcome.Stopped, state);

                episodeId = rootEpisodeId;
                continue;
            }

            state = Commit(chapter, state, chosen);

            episodeId = chosen.Option.TargetEpisodeId;

            if (!chapter.IsSameScene(chosen.FromEpisodeId, episodeId))
                return new SceneRunResult(SceneRunOutcome.SceneEnded, state);
        }
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

    // 리플레이 직전. 표적 뒤의 선택 기록을 물린다 - ChoiceHistory가 Yarn 선택에 하는 것과 같은 규칙.
    // 커밋된 기록은 하한 덕에 표적보다 항상 앞이라 물리지 않는다.
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
        }

        _replayCursor = 0;

        Debug.Log($"[장면] 리플레이 — 루트부터. 자동 응답할 선택 {_picks.Count}개");
    }

    private ProgressionState Commit(
        ChapterProgression chapter, ProgressionState state, ProgressionPick pick)
    {
        ProgressionState next = state.Commit(chapter, pick.Option);

        pick.Committed = true;

        // 커밋 앞으로는 못 돌아간다 (잠정 한계 — G3에서 해제).
        _rollbackHistory.SetRollbackFloor(_rollbackHistory.LastHistoryIndex);

        _reporter.ReportChoiceCommitted(
            new ChoiceCommitReport(
                chapter.ChapterId,
                pick.FromEpisodeId,
                pick.SourceIndex,
                pick.Option,
                next));

        return next;
    }

    // 장면 안에서 한 번만. 리플레이로 같은 노드를 다시 끝내도 두 번 보고하지 않음.
    private void ReportWatchedOnce(ChapterProgression chapter, EpisodeNode node)
    {
        if (node.EventKey.Length == 0)
            return;

        if (!_watchedReported.Add(node.EpisodeId))
            return;

        _reporter.ReportEpisodeWatched(
            new EpisodeWatchReport(chapter.ChapterId, node.EpisodeId, node.EventKey));
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