using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 챕터 진행:
// - ProgressionState 소유
// - Scene 반복 실행
// - 챕터 단위 Yarn 변수 초기화/복원
// - 회차 Backlog 초기화/복원
public sealed class ProgressionDriver
{
    private readonly SceneRunner _sceneRunner;
    private readonly BacklogRecorder _backlog;
    private readonly ProgressionYarnBridge _yarnBridge;

    private ChapterProgression _chapter;
    private ProgressionState _state;
    private YarnProject _yarnProject;

    private string _yarnChapterId;

    private YarnVariableSnapshot _restoreVariables;
    private IReadOnlyList<DialogueLogEntry> _restoreBacklog;
    private SavedLoadPlan _loadPlan;

    private CancellationTokenSource _runCancellation;

    public bool IsRunning { get; private set; }

    public IReadOnlyList<CommittedChoice> PendingPath =>
        _sceneRunner.PendingPath;

    public ProgressionDriver(
        SceneRunner sceneRunner,
        BacklogRecorder backlog,
        ProgressionYarnBridge yarnBridge)
    {
        _sceneRunner = sceneRunner;
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

            PrepareBacklog();

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

    private async Task RunChapterAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Scene checkpoint보다 먼저 Yarn 변수를 복원해야
            // replay도 복원된 변수 상태에서 시작한다.
            SyncChapterVariables();

            SavedLoadPlan loadPlan = _loadPlan;
            _loadPlan = null;

            var context = new SceneRunContext(
                _chapter,
                _state,
                loadPlan);

            SceneRunResult result = await _sceneRunner.RunAsync(
                context,
                cancellationToken);

            _state = result.State;

            switch (result.Outcome)
            {
                case SceneRunOutcome.SceneEnded:
                    continue;

                case SceneRunOutcome.ChapterEnded:
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(result.Outcome),
                        result.Outcome,
                        "알 수 없는 장면 실행 결과다.");
            }
        }
    }

    // 회차 Backlog는 Scene playback이 아니라 Driver가 준비한다.
    private void PrepareBacklog()
    {
        if (_restoreBacklog == null)
        {
            _backlog.ClearBacklog();
            return;
        }

        _backlog.Restore(_restoreBacklog);

        Debug.Log($"[진행] 백로그 복원 — {_restoreBacklog.Count}개");

        _restoreBacklog = null;
    }

    // 전체 progression을 중단한다.
    //
    // Driver가 cancellation을 기록하고,
    // SceneRunner가 현재 UI/playback을 실제로 깨운다.
    public async Task StopAsync()
    {
        CancellationTokenSource cancellation = _runCancellation;

        if (!IsRunning || cancellation == null)
            return;

        cancellation.Cancel();

        await _sceneRunner.StopAsync();
    }

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

        Debug.Log(
            $"[진행] Yarn 변수 초기화 — 챕터 \"{_chapter.ChapterId}\"");

        if (_restoreVariables == null)
            return;

        _yarnBridge.Restore(_restoreVariables);

        Debug.Log(
            $"[진행] Yarn 변수 복원 — {_restoreVariables.Count}개");

        _restoreVariables = null;
    }

    private string Describe() =>
        _chapter == null
            ? "(시작 전)"
            : $"{_chapter.ChapterId}/{_state?.CurrentEpisodeId}";
}