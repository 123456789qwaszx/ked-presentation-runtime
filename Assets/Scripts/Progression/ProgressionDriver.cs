using System;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 순서를 쥐는 자리. 진행 층과 대사 층을 잇는다.
//
// 코어는 "무엇이 참인가"만 답한다(ChapterTransition·Commit 전부 순수 함수).
// "어떤 순서로 부르는가"는 여기와 SceneRunner에 있다:
//   · 챕터 루프 — 어느 장면 다음에 어느 장면인가, 챕터 변수, [2] 상태 소유 (여기)
//   · 장면 루프 — 노드 재생·판정·선택·Via·커밋, 롤백 리플레이 (SceneRunner)
// 이 루프가 지는 세 규칙은 장면 루프가 지킨다:
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
    private readonly SceneRunner _scenes;
    private readonly IChapterOptionsView _options;
    private readonly ProgressionYarnBridge _yarnBridge;

    private ChapterProgression _chapter;
    private ProgressionState _state;
    private bool _stopRequested;

    private YarnProject _yarnProject;

    // Yarn 저장소가 지금 어느 챕터의 것인지. 이것이 _chapter와 갈리면 [3]을 다시 세운다.
    private string _yarnChapterId;

    public bool IsRunning { get; private set; }

    public ProgressionDriver(
        EpisodePlayer player,
        IChapterOptionsView options,
        VNLinePresentationState seek,
        RollbackHistory rollbackHistory,
        ProgressionYarnBridge yarnBridge,
        IProgressionReporter reporter)
    {
        _options = options;
        _yarnBridge = yarnBridge;

        _scenes = new SceneRunner(
            player, options, seek, rollbackHistory, reporter, yarnBridge.Capture, () => _stopRequested);
    }

    // 이어하기의 [3] 덤프 — 첫 BeginChapter 뒤에 한 번 덮고 버린다.
    private YarnVariableSnapshot _restoreVariables;

    public async Task RunAsync(
        YarnProject project, ChapterProgression chapter, ProgressionState entryState,
        YarnVariableSnapshot restoreVariables = null)
    {
        if (IsRunning)
        {
            Debug.LogWarning("[진행] 이미 돌고 있다. 새 요청을 무시한다.");
            return;
        }

        IsRunning = true;
        _stopRequested = false;
        _chapter = chapter;
        _state = entryState;
        _yarnProject = project;
        _yarnChapterId = null;
        _restoreVariables = restoreVariables;

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

    // 장면 루프. 챕터가 끝나면 true, 멈춤 요청이면 false.
    private async Task<bool> RunChapterAsync()
    {
        // 백로그는 회차 스코프 — 첫 장면 진입에서만 비운다.
        bool isNewSession = true;

        while (true)
        {
            // 장면 진입의 체크포인트 Capture보다 앞이어야 리플레이가 초기화된 [3]에서 출발한다.
            SyncChapterVariables();

            SceneRunResult result = await _scenes.RunAsync(_chapter, _state, isNewSession);

            isNewSession = false;
            _state = result.State;

            switch (result.Outcome)
            {
                case SceneRunOutcome.ChapterEnded: return true;
                case SceneRunOutcome.Stopped: return false;
            }
        }
    }

    // 타이틀로 나가기 등.
    public void RequestStop()
    {
        _stopRequested = true;
        _options.Cancel();
    }

    // "[3] 연출 실행 상태"는 챕터 수명 — 챕터가 바뀔 때만 초기값으로 되돌린다.
    //
    // [2]는 여기 오지 않는다. 진행 코어만 알고, 대사에서 읽는 것도 금지 —
    // 스탯 분기는 그래프 간선으로 올린다.
    private void SyncChapterVariables()
    {
        if (string.Equals(_yarnChapterId, _chapter.ChapterId, StringComparison.Ordinal))
            return;

        _yarnBridge.BeginChapter(_yarnProject);
        _yarnChapterId = _chapter.ChapterId;

        Debug.Log($"[진행] Yarn 변수 초기화 — 챕터 \"{_chapter.ChapterId}\"");

        // 이어하기 — 초기값 위에 덤프를 덮는다. 장면 진입의 Capture보다 앞이라 리플레이도 이 값에서 출발.
        if (_restoreVariables != null)
        {
            _yarnBridge.Restore(_restoreVariables);
            Debug.Log($"[진행] [3] 복원 — {_restoreVariables.Count}개");
            _restoreVariables = null;
        }
    }

    private string Describe() =>
        _chapter == null
            ? "(시작 전)"
            : $"{_chapter.ChapterId}/{_state?.CurrentEpisodeId}";
}