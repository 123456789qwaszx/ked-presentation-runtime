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
// 진행 상태([2])의 수명이 챕터다 — 시작할 때 새로 만든다. 챕터를 넘어 사는 계층([1])은
// 아직 서지 않았고, 그것이 설 때까지 스탯을 바꾸는 자리는 간선 하나뿐이다.
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
    private ChapterProgression _chapter;
    private ProgressionState _state;
    private bool _stopRequested;
    private bool _firstEpisode;

    private YarnProject _yarnProject;

    // Yarn 저장소가 지금 어느 챕터의 것인지. 이것이 _chapter와 갈리면 [3]을 다시 세운다.
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
            _chapter = null;
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
        _chapter = _scenario.StartChapter;

        // 이 챕터의 진행 상태는 챕터가 만든다.
        _state = _chapter.CreateEntryState();

        Debug.Log($"[진행] 챕터 시작 — {Describe()}");

        ScenarioAdvance? outcome = await RunChapterAsync();

        if (outcome != null)
            ShowEnding(outcome.Value);
    }

    // 에피소드 루프. 챕터가 끝나면 그 결말을, 멈춤 요청이면 null을 낸다.
    private async Task<ScenarioAdvance?> RunChapterAsync()
    {
        while (true)
        {
            _chapter.TryGetNode(_state.CurrentEpisodeId, out EpisodeNode node);

            if (!await PlayNodeAsync(node.DialogueEntryId, "대사"))
                return null;

            // 이 회차의 판단은 여기서 확정된다. 아래는 이미 정해진 목록에서 고르기만 한다.
            ChapterAdvance advance = ChapterTransition.Resolve(_chapter, _state);

            if (advance.Kind == ChapterAdvanceKind.ChapterEnded)
                return ScenarioTransition.Resolve(_chapter, _state);

            EpisodeOption chosen = await PickAsync(advance);

            if (chosen == null)
                return null;

            // 연출도 Story 노드. 별도 경로를 만들지 않는다.
            if (chosen.HasVia && !await PlayNodeAsync(chosen.ViaNodeId, "연출"))
                return null;

            _state = _state.Commit(_chapter, chosen);
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

        // "[3] 연출 실행 상태"는 챕터 수명이다. 챕터가 바뀌는 이 자리에서 선언 초기값으로
        // 되돌린다 — 이전 챕터가 남긴 값도, 이 챕터를 아까 한 번 돌린 값도 안 물려받는다.
        if (!string.Equals(_yarnChapterId, _chapter.ChapterId, StringComparison.Ordinal))
        {
            _yarnBridge.BeginChapter(_yarnProject);
            _yarnChapterId = _chapter.ChapterId;

            Debug.Log($"[진행] Yarn 변수 초기화 — 챕터 \"{_chapter.ChapterId}\"");
        }

        // "[2] 에피소드 상태"를 대사가 읽을 수 있게 심는다. 진행 코어가 쥔 값이라
        // 매 노드마다 다시 심고, Yarn에서 바뀐 값은 돌려받지 않는다.
        //
        // 정의를 함께 넘기는 이유: 깃발을 숫자로 심으면 Yarn 저장소가 그 변수를
        // float으로 도장해, bool로 선언된 변수가 그 뒤로 읽히지 않는다.
        _yarnBridge.PublishStats(_chapter.Stats, _state.Stats);
    }

    private static void ShowEnding(in ScenarioAdvance outcome)
    {
        Debug.Log(outcome.EventKey.Length != 0
            ? $"[진행] 챕터 끝 — 이벤트키 \"{outcome.EventKey}\""
            : "[진행] 챕터 끝");
    }

    private string Describe() =>
        _chapter == null
            ? "(시작 전)"
            : $"{_chapter.ChapterId}/{_state?.CurrentEpisodeId}";
}
