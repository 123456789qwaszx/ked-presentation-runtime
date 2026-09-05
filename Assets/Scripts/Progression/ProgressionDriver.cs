using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 챕터 진행:
// - ProgressionState 소유
// - 장면 반복 실행
// - 챕터 단위 Yarn 변수 초기화 및 복원
// - SceneRunner 호출
public sealed class ProgressionDriver
{
    private readonly SceneRunner _sceneRunner;
    private readonly BacklogRecorder _backlog;
    private readonly ProgressionYarnBridge _yarnBridge;

    private ChapterProgression _chapter;
    private ProgressionState _state;
    private YarnProject _yarnProject;

    // Yarn 저장소가 어느 챕터 기준으로 초기화되었는지 기록.
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

    private CancellationTokenSource _runCancellation;

    public bool IsRunning { get; private set; }

    // 현재 장면의 미확정 경로.
    // 즐겨찾기 캡처에 사용한다.
    public IReadOnlyList<CommittedChoice> PendingPath => _sceneRunner.PendingPath;

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

        var cancellation = new CancellationTokenSource();

        _runCancellation = cancellation;
        IsRunning = true;

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

            await RunChapterAsync(cancellation.Token);

            Debug.Log($"[진행] 챕터 끝 — {Describe()}");
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
            Debug.Log("[진행] 취소됨.");
        }
        catch (Exception error)
        {
            Debug.LogError($"[진행] 멈췄다 — {Describe()}\n{error}");
        }
        finally
        {
            if (ReferenceEquals(_runCancellation, cancellation))
                _runCancellation = null;
            
            IsRunning = false;

            _chapter = null;
            _state = null;
            _yarnProject = null;
            _yarnChapterId = null;

            _restoreVariables = null;
            _restoreBacklog = null;
            _loadPlan = null;

            cancellation.Dispose();
        }
    }

    // Chapter 안의 Scene들을 순서대로 실행한다.
    //
    // 외부 중단은 반환값으로 표현하지 않고
    // OperationCanceledException으로 RunAsync까지 전달한다.
    private async Task RunChapterAsync(CancellationToken cancellation)
    {
        bool isNewSession = true;

        if (_restoreBacklog != null)
        {
            _backlog.Restore(_restoreBacklog);
            
            isNewSession = false;
            _restoreBacklog = null;
        }

        while (true)
        {
            cancellation.ThrowIfCancellationRequested();

            // 장면 checkpoint를 잡기 전에 복원해야
            // replay가 복원된 Yarn 변수에서 출발한다.
            SyncChapterVariables();

            SavedLoadPlan loadPlan = _loadPlan;

            _loadPlan = null;

            var context = 
                new SceneRunContext(_chapter, _state, isNewSession, loadPlan);

            SceneRunResult result = 
                await _sceneRunner.RunAsync(
                    context, 
                    cancellation);
            
            isNewSession = false;
            _state = result.State;

            switch (result.Outcome)
            {
                case SceneRunOutcome.SceneEnded:
                    continue;

                case SceneRunOutcome.ChapterEnded:
                    return;

                default: 
                    throw new ArgumentOutOfRangeException(
                        nameof(result.Outcome), result.Outcome, "알 수 없는 장면 실행 결과다.");
            }
        }
    }

    // 전체 progression 실행을 중단한다.
    //
    // Driver가 CancellationToken을 취소해 중단 의도를 기록하고,
    // SceneRunner가 현재 선택지/Yarn 실행을 실제로 깨운다.
    public async Task StopAsync()
    {
        CancellationTokenSource cancellation = _runCancellation;

        if (!IsRunning || cancellation == null)
            return;
        
        cancellation.Cancel();

        await _sceneRunner.StopAsync();
    }

    // Yarn presentation 변수는 Chapter 수명이다.
    // Chapter가 바뀔 때만 초기 상태로 다시 시작.
    private void SyncChapterVariables()
    {
        if (string.Equals(_yarnChapterId, _chapter.ChapterId, StringComparison.Ordinal))
            return;

        _yarnBridge.BeginChapter(_yarnProject);

        _yarnChapterId = _chapter.ChapterId;

        Debug.Log($"[진행] Yarn 변수 초기화 - 챕터 \"{_chapter.ChapterId}\"");

        if (_restoreVariables == null)
            return;

        _yarnBridge.Restore(_restoreVariables);

        Debug.Log(
            $"[진행] Yarn 변수 복원 — " +
            $"{_restoreVariables.Count}개");

        _restoreVariables = null;
    }

    private string Describe() =>
        _chapter == null
            ? "(시작 전)"
            : $"{_chapter.ChapterId}/" +
              $"{_state?.CurrentEpisodeId}";
}