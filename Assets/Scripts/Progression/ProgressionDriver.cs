using System;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 순서를 쥐는 유일한 자리. 진행 층과 대사 층을 잇는다.
//
// 코어는 "무엇이 참인가"만 답한다(ChapterTransition·ScenarioTransition·Commit 전부 순수 함수).
// "어떤 순서로 부르는가"는 여기 있고, 그것이 이 루프가 지는 세 규칙이다:
//   · 판정은 대사 뒤 한 번 — 화면에 뜬 것과 실제가 갈릴 수 없다
//   · 연출은 커밋보다 앞 — 지나가는 자리라 상태를 안 바꾼다
//   · 스탯 반영과 이동이 한 연산 — "스탯만 오르고 안 옮겨 간" 상태가 없다
//
// 나중에 이 순서를 쓰는 두 번째 소비자(툴의 걷기 모드 등)가 생기면, 이 루프를 코어로
// 올리고 await를 pull로 되접어야 한다. 순서를 두 곳에 적으면 작가가 미리보는 게임과
// 플레이어가 하는 게임이 갈린다.
public sealed class ProgressionDriver
{
    private readonly EpisodePlayer _player;
    private readonly IChapterOptionsView _options;
    private readonly ProgressionYarnBridge _yarnBridge;

    private ScenarioProgression _scenario;
    private ProgressionState _state;
    private bool _stopRequested;
    private bool _firstEpisode;

    private YarnProject _yarnProject;

    // Yarn 저장소가 지금 어느 챕터의 것인지. 이것이 상태와 갈리면 [3]을 다시 세운다.
    private string _yarnChapterId;

    public bool IsRunning { get; private set; }

    public ProgressionDriver(
        EpisodePlayer player, IChapterOptionsView options, ProgressionYarnBridge yarnBridge)
    {
        _player = player;
        _options = options;
        _yarnBridge = yarnBridge;
    }

    public async Task RunAsync(ScenarioProgression scenario, YarnProject project)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[진행] 이미 돌고 있다. 새 요청을 무시한다.");
            return;
        }

        IsRunning = true;
        _stopRequested = false;
        _firstEpisode = true;
        _scenario = scenario;
        _yarnProject = project;
        _yarnChapterId = null;

        try
        {
            // 시작값은 시작 챕터가 세운다 — 스탯의 수명이 챕터다.
            _state = scenario.StartChapter.CreateEntryState();

            Debug.Log($"[진행] 시작 — {Describe()}");

            await PumpAsync();
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
            _scenario = null;
            _state = null;
            _yarnProject = null;
            _yarnChapterId = null;
        }
    }

    // 타이틀로 나가기 등.
    public void RequestStop()
    {
        _stopRequested = true;
        _options.Cancel();
    }

    private async Task PumpAsync()
    {
        while (true)
        {
            ChapterProgression chapter = CurrentChapter();
            chapter.TryGetNode(_state.CurrentEpisodeId, out EpisodeNode node);

            if (!await PlayNodeAsync(node.DialogueEntryId, "대사"))
                return;

            // 이 회차의 판단은 여기서 확정된다. 아래는 이미 정해진 목록에서 고르기만 한다.
            ChapterAdvance advance = ChapterTransition.Resolve(chapter, _state);

            if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
            {
                if (!CrossChapterBoundary())
                    return;

                continue;
            }

            EpisodeOption chosen = advance.Kind == ChapterAdvanceKind.AutoAdvance
                ? advance.AutoOption
                : await PickAsync(advance);

            if (chosen == null)
                return;

            // 연출도 Story 노드. 별도 경로를 만들지 않는다.
            if (chosen.HasVia && !await PlayNodeAsync(chosen.ViaNodeId, "연출"))
                return;

            _state = _state.Commit(chapter, chosen);
        }
    }

    // 고르지 못했으면(멈춤 요청) null.
    private async Task<EpisodeOption> PickAsync(ChapterAdvance advance)
    {
        int picked = await _options.ShowAsync(advance.Options, advance.HiddenCount);

        if (_stopRequested)
            return null;

        if (picked < 0 || picked >= advance.Options.Count)
            throw new ArgumentOutOfRangeException(
                nameof(picked), $"선택지는 {advance.Options.Count}개인데 {picked}번이 왔다.");

        ResolvedOption resolved = advance.Options[picked];

        // 화면이 잠긴 것을 못 고르게 하지만, 뚫렸을 때 무엇 때문에 잠겼는지를 지목한다.
        if (!resolved.IsSelectable)
            throw new InvalidOperationException(
                $"잠긴 선택지다: [{resolved.Option.ChoiceLabel}] — {resolved.BlockingCondition}");

        Debug.Log($"[진행] 골랐다 — {resolved.Option.ChoiceLabel}");

        return resolved.Option;
    }

    // 에피소드 층과 시나리오 층이 만나는 유일한 자리. 건너가는 것은 엔딩키 하나다.
    // 계속 갈 수 있으면 true.
    private bool CrossChapterBoundary()
    {
        ScenarioAdvance next = ScenarioTransition.Resolve(_scenario, _state);

        if (next.Kind != ScenarioAdvanceKind.NextChapter)
        {
            ShowEnding(next);
            return false;
        }

        // 스탯은 새 챕터의 초기값에서 다시 선다 — 수명이 챕터다.
        _state = _state.CommitChapterEnding(_scenario, next);

        return true;
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

    // Yarn 변수의 두 계층을 이 회차의 상태로 맞춘다.
    //
    // 순서가 뜻을 가진다. BeginChapter의 Clear()는 계층을 가리지 않으므로 [2]를 먼저
    // 심으면 곧바로 지워진다. 그리고 둘 다 YarnVariableCheckpoint.Capture()보다 앞이어야
    // 롤백 리플레이가 같은 값에서 다시 출발한다.
    private void SyncYarnVariables()
    {
        if (_yarnBridge == null)
            return;

        string chapterId = _state.CurrentChapterId;

        // "[3] 연출 실행 상태"는 챕터 수명이다. 챕터가 바뀌는 이 자리에서 선언 초기값으로
        // 되돌린다 — 이전 챕터가 남긴 값도, 이 챕터를 아까 한 번 돌린 값도 안 물려받는다.
        if (!string.Equals(_yarnChapterId, chapterId, StringComparison.Ordinal))
        {
            _yarnBridge.BeginChapter(_yarnProject);
            _yarnChapterId = chapterId;

            Debug.Log($"[진행] Yarn 변수 초기화 — 챕터 \"{chapterId}\"");
        }

        // "[2] 에피소드 상태"를 대사가 읽을 수 있게 심는다. 진행 코어가 쥔 값이라
        // 매 노드마다 다시 심고, Yarn에서 바뀐 값은 돌려받지 않는다.
        //
        // 정의를 함께 넘기는 이유: 깃발을 숫자로 심으면 Yarn 저장소가 그 변수를
        // float으로 도장해, bool로 선언된 변수가 그 뒤로 읽히지 않는다.
        _yarnBridge.PublishStats(CurrentChapter().Stats, _state.Stats);
    }

    // 로더가 참조를 다 검증했고 Commit은 검증된 곳으로만 옮긴다 — 전체 함수다.
    // 여기에 방어 코드가 생기면 경계가 샜다는 신호다.
    private ChapterProgression CurrentChapter()
    {
        _scenario.TryGetChapter(_state.CurrentChapterId, out ChapterProgression chapter);

        return chapter;
    }

    private static void ShowEnding(in ScenarioAdvance outcome)
    {
        // 의도한 종착과 막다른 곳을 섞지 않는다. 화면에서 구별되어야 한다.
        if (outcome.Kind == ScenarioAdvanceKind.ScenarioEnded)
        {
            Debug.Log($"[진행] 엔딩 — \"{outcome.EndingKey}\"");
            return;
        }

        Debug.LogWarning(
            "[진행] 미완성 — 엔딩키가 없는 노드에서 멈췄다. " +
            "나가는 길이 하나도 없는데 엔딩도 아니다(작가가 아직 안 이은 자리).");
    }

    private string Describe() =>
        _state == null
            ? "(시작 전)"
            : $"{_state.CurrentChapterId}/{_state.CurrentEpisodeId}";
}
