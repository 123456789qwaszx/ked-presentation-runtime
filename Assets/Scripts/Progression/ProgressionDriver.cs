using System;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 순서를 쥐는 유일한 자리. 진행 층과 대사 층을 잇는다.
//
// 코어는 "무엇이 참인가"만 답한다(ChapterTransition·Commit 전부 순수 함수).
// "어떤 순서로 부르는가"는 여기 있고, 그것이 이 루프가 지는 세 규칙이다:
//   · 판정은 대사 뒤 한 번 — 화면에 뜬 것과 실제가 갈릴 수 없다
//   · 연출은 커밋보다 앞 — 지나가는 자리라 상태를 안 바꾼다
//   · 스탯 반영과 이동이 한 연산 — "스탯만 오르고 안 옮겨 간" 상태가 없다
//
// 진행 상태([2])의 수명이 챕터다. 시작 상태(새 게임의 진입 상태든 저장에서 복원한 것이든)는
// 호출자가 만들어 준다 — 저장 파일을 콘텐츠와 대조하는 일은 런처의 것이고, 여기서는
// 받은 챕터와 상태가 짝이 맞는다고 믿는다.
//
// 나중에 이 순서를 쓰는 두 번째 소비자(툴의 걷기 모드 등)가 생기면, 이 루프를 코어로
// 올리고 await를 pull로 되접어야 한다. 순서를 두 곳에 적으면 작가가 미리보는 게임과
// 플레이어가 하는 게임이 갈린다.
public sealed class ProgressionDriver
{
    private readonly EpisodePlayer _player;
    private readonly IChapterOptionsView _options;
    private readonly ProgressionYarnBridge _yarnBridge;
    private readonly IProgressionReporter _reporter;

    private ChapterProgression _chapter;
    private ProgressionState _state;
    private bool _stopRequested;
    private bool _firstEpisode;

    private YarnProject _yarnProject;

    // Yarn 저장소가 지금 어느 챕터의 것인지. 이것이 _chapter와 갈리면 [3]을 다시 세운다.
    private string _yarnChapterId;

    public bool IsRunning { get; private set; }

    public ProgressionDriver(
        EpisodePlayer player,
        IChapterOptionsView options,
        ProgressionYarnBridge yarnBridge,
        IProgressionReporter reporter)
    {
        _player = player;
        _options = options;
        _yarnBridge = yarnBridge;
        _reporter = reporter;
    }

    public async Task RunAsync(YarnProject project, ChapterProgression chapter, ProgressionState entryState)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[진행] 이미 돌고 있다. 새 요청을 무시한다.");
            return;
        }

        IsRunning = true;
        _stopRequested = false;
        _firstEpisode = true;
        _chapter = chapter;
        _state = entryState;
        _yarnProject = project;
        _yarnChapterId = null;

        try
        {
            Debug.Log($"[진행] 챕터 시작 — {Describe()}");

            if (await RunChapterAsync())
                Debug.Log($"[진행] 챕터 끝 — {Describe()}");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[진행] 취소됨.");
        }
        catch (Exception error)
        {
            Debug.LogError($"[진행] 멈췄다 — {Describe()}\n{error}");
        }
        finally
        {
            IsRunning = false;
            _chapter = null;
            _state = null;
            _yarnProject = null;
            _yarnChapterId = null;
        }
    }

    // 에피소드 루프. 챕터가 끝나면 true, 멈춤 요청이면 false.
    private async Task<bool> RunChapterAsync()
    {
        while (true)
        {
            _chapter.TryGetNode(_state.CurrentEpisodeId, out EpisodeNode node);

            if (!await PlayNodeAsync(node.DialogueEntryId, "대사"))
                return false;

            if (node.EventKey.Length != 0)
            {
                _reporter.ReportEpisodeWatched(
                    new EpisodeWatchReport(_chapter.ChapterId, node.EpisodeId, node.EventKey));
            }

            ChapterAdvance advance = 
                ChapterTransition.Resolve(_chapter, _state);

            if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                return true;

            ResolvedOption? picked = await PickAsync(advance);

            if (!picked.HasValue)
                return false;

            ResolvedOption resolved = picked.Value;
            EpisodeOption chosen = resolved.Option;

            // 연출도 Story 노드. 별도 경로를 만들지 않는다.
            if (chosen.HasVia &&
                !await PlayNodeAsync(chosen.ViaNodeId, "연출"))
            {
                return false;
            }

            _state = _state.Commit(_chapter, chosen);

            _reporter.ReportChoiceCommitted(
                new ChoiceCommitReport(
                    _chapter.ChapterId, 
                    node.EpisodeId,
                    resolved.SourceIndex,
                    chosen, 
                    _state));
        }
    }

    private async Task<ResolvedOption?> PickAsync(ChapterAdvance advance)
    {
        int picked = await _options.ShowAsync(
            advance.Options,
            advance.HiddenCount);

        if (_stopRequested)
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

    private async Task<bool> PlayNodeAsync(string nodeName, string what)
    {
        Debug.Log($"[진행] {what} 시작 — \"{nodeName}\"");

        SyncYarnVariables();

        // 첫 진입 이후로는 백로그를 유지해야 하기 때문에 구분.
        if (_firstEpisode)
        {
            _firstEpisode = false;
            await _player.StartGameAsync(nodeName);
        }
        else
        {
            await _player.ContinueEpisodeAsync(nodeName);
        }

        Debug.Log($"[진행] {what} 끝 — \"{nodeName}\"");

        return !_stopRequested;
    }
    
    // 타이틀로 나가기 등.
    public void RequestStop()
    {
        _stopRequested = true;
        _options.Cancel();
    }

    // Yarn 변수의 두 계층을 이 회차의 상태로 복원.
    //
    // 순서 중요.
    // BeginChapter의 Clear()는 계층을 가리지 않으므로 [2]를 먼저 심으면 바로 지워짐.
    // 둘 다 YarnVariableCheckpoint.Capture()보다 먼저 실행되어야 리플레이 정상 재생.
    private void SyncYarnVariables()
    {
        // "[3] 연출 실행 상태"는 챕터 수명.
        // 챕터가 바뀌는 이 자리에서만 초기값 선언.(다른 챕터의 것은 무시)
        if (!string.Equals(_yarnChapterId, _chapter.ChapterId, StringComparison.Ordinal))
        {
            _yarnBridge.BeginChapter(_yarnProject);
            _yarnChapterId = _chapter.ChapterId;

            Debug.Log($"[진행] Yarn 변수 초기화 — 챕터 \"{_chapter.ChapterId}\"");
        }

        // "[2] 에피소드 상태"를 대사가 읽을 수 있게 심는다. 진행 코어가 쥔 값이라
        // 매 노드마다 다시 심고, Yarn에서 바뀐 값은 돌려받지 않음.
        //
        // 정의를 함께 넘기는 이유: 깃발을 숫자로 심으면 Yarn 저장소가 그 변수를
        // float으로 도장해, bool로 선언된 변수가 그 뒤로 안 읽힘.
        _yarnBridge.PublishStats(_chapter.Stats, _state.Stats);
    }

    private string Describe() =>
        _chapter == null
            ? "(시작 전)"
            : $"{_chapter.ChapterId}/{_state?.CurrentEpisodeId}";
}