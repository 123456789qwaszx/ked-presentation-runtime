using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 챕터 진행:
// - ProgressionState 소유
// - 장면 반복 실행
// - 챕터 단위 Yarn 변수 초기화 및 복원
// - SceneRunner호출(Scene 진입)
public sealed class ProgressionDriver
{
    private readonly SceneRunner _sceneRunner;
    private readonly BacklogRecorder _backlog;
    private readonly ProgressionYarnBridge _yarnBridge;

    private ChapterProgression _chapter;
    private ProgressionState _state;
    private YarnProject _yarnProject;

    private bool _stopRequested;

    // Yarn 저장소가 어느 챕터 기준으로 초기화되었는지 기록.
    // 현재 챕터와 다르면 Yarn 변수를 새 챕터 기준으로 초기화한다.
    private string _yarnChapterId;

    // 이어하기의 Yarn 변수 덤프.
    // 첫 BeginChapter 직후 한 번 복원하고 버린다.
    private YarnVariableSnapshot _restoreVariables;

    // 이어하기의 이전 장면 백로그.
    // 첫 장면 진입 전에 한 번 복원하고 버린다.
    private IReadOnlyList<DialogueLogEntry> _restoreBacklog;

    // 첫 장면에서 저장된 표적 라인까지 달리는 계획.
    // 첫 장면 실행에 한 번 전달하고 버린다.
    private SavedLoadPlan _loadPlan;

    public bool IsRunning { get; private set; }

    // 현재 장면의 미확정 경로. 즐겨찾기 캡처에 사용한다.
    public IReadOnlyList<CommittedChoice> PendingPath =>
        _sceneRunner.PendingPath;

    public ProgressionDriver(
        SceneRunner scenes,
        BacklogRecorder backlog,
        ProgressionYarnBridge yarnBridge)
    {
        _sceneRunner = scenes;
        _backlog = backlog;
        _yarnBridge = yarnBridge;
    }

    public async Task RunAsync(
        YarnProject project,
        ChapterProgression chapter,
        ProgressionState entryState,
        YarnVariableSnapshot restoreVariables = null,
        IReadOnlyList<DialogueLogEntry> restoreBacklog = null,
        SavedLoadPlan loadPlan = null)
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
        _restoreBacklog = restoreBacklog;
        _loadPlan = loadPlan;

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
            _stopRequested = false;

            _chapter = null;
            _state = null;
            _yarnProject = null;
            _yarnChapterId = null;

            _restoreVariables = null;
            _restoreBacklog = null;
            _loadPlan = null;
        }
    }

    // 장면 루프.
    // 챕터가 끝나면 true, 중단 요청이면 false.
    private async Task<bool> RunChapterAsync()
    {
        bool isNewSession = true;

        if (_restoreBacklog != null)
        {
            _backlog.Restore(_restoreBacklog);
            isNewSession = false;

            Debug.Log($"[진행] 백로그 복원 — {_restoreBacklog.Count}개");

            _restoreBacklog = null;
        }

        while (true)
        {
            // 장면 진입 체크포인트보다 먼저 복원해야
            // 리플레이가 복원된 Yarn 변수에서 출발한다.
            SyncChapterVariables();

            SavedLoadPlan loadPlan = _loadPlan;
            _loadPlan = null;

            SceneRunResult result = await _sceneRunner.RunAsync(
                _chapter,
                _state,
                isNewSession,
                () => _stopRequested,
                loadPlan);

            isNewSession = false;
            _state = result.State;

            switch (result.Outcome)
            {
                case SceneRunOutcome.SceneEnded:
                    continue;

                case SceneRunOutcome.ChapterEnded:
                    return true;

                case SceneRunOutcome.Stopped:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result.Outcome),
                        result.Outcome,
                        "알 수 없는 장면 실행 결과다.");
            }
        }
    }

    // 전체 진행을 중단한다.
    //
    // Driver는 중단 의도를 기록하고,
    // SceneRunner는 현재 선택지와 장면을 실제로 멈춘다.
    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        _stopRequested = true;

        await _sceneRunner.StopAsync();
    }

    // Yarn 연출 변수는 챕터 수명이다.
    // 챕터가 바뀔 때만 초기값으로 되돌린다.
    private void SyncChapterVariables()
    {
        if (string.Equals(
                _yarnChapterId,
                _chapter.ChapterId,
                StringComparison.Ordinal))
        {
            return;
        }

        _yarnBridge.BeginChapter(_yarnProject);
        _yarnChapterId = _chapter.ChapterId;

        Debug.Log($"[진행] Yarn 변수 초기화 — 챕터 \"{_chapter.ChapterId}\"");

        if (_restoreVariables == null)
            return;

        _yarnBridge.Restore(_restoreVariables);

        Debug.Log($"[진행] Yarn 변수 복원 — {_restoreVariables.Count}개");

        _restoreVariables = null;
    }

    private string Describe() =>
        _chapter == null
            ? "(시작 전)"
            : $"{_chapter.ChapterId}/{_state?.CurrentEpisodeId}";
}