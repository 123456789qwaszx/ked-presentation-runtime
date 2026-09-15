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
// - 챕터 단위 Yarn 변수 초기화 / 복원
// - 회차 Backlog 초기화 / 복원
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

    private SceneTransaction _currentScene;
    private CancellationTokenSource _runCancellation;
    
    private Task _runTask = Task.CompletedTask;

    public bool IsRunning => !_runTask.IsCompleted;

    public IReadOnlyList<CommittedChoice> PendingPath =>
        _currentScene?.PendingPath ?? Array.Empty<CommittedChoice>();

    public ProgressionDriver(
        SceneRunner sceneRunner,
        BacklogRecorder backlog,
        ProgressionYarnBridge yarnBridge)
    {
        _sceneRunner = sceneRunner;
        _backlog = backlog;
        _yarnBridge = yarnBridge;
    }

    public void Start(
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

        _runTask = RunAsync(
            project,
            chapter,
            entryState,
            restoreVariables,
            restoreBacklog,
            loadPlan);
    }

    private async Task RunAsync(
        YarnProject project,
        ChapterProgression chapter,
        ProgressionState entryState,
        YarnVariableSnapshot restoreVariables,
        IReadOnlyList<DialogueLogEntry> restoreBacklog,
        SavedLoadPlan loadPlan)
    {
        var cancellation = new CancellationTokenSource();

        _runCancellation = cancellation;

        _chapter = chapter;
        _state = entryState;
        _yarnProject = project;
        _yarnChapterId = null;

        _restoreVariables = restoreVariables;
        _restoreBacklog = restoreBacklog;
        _loadPlan = loadPlan;

        try
        {
            _backlog.Restore(_restoreBacklog);
            _restoreBacklog = null;
            
            await RunChapterAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
            when (cancellation.IsCancellationRequested)
        {
            Debug.Log("[진행] 취소됨.");
        }
        catch (Exception error)
        {
            Debug.LogError($"[진행] 멈췄다 \n{error}");
        }
        finally
        {
            if (ReferenceEquals(_runCancellation, cancellation))
                _runCancellation = null;

            _currentScene = null;

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

            var scene = new SceneTransaction(_chapter, _state, loadPlan);

            _currentScene = scene;

            try
            {
                SceneRunResult result =
                    await _sceneRunner.RunAsync(scene, cancellationToken);

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
            finally
            {
                if (ReferenceEquals(_currentScene, scene))
                    _currentScene = null;
            }
        }
    }

    public Task RequestReplayAsync()
    {
        SceneTransaction scene = _currentScene;

        if (scene == null)
            return Task.CompletedTask;

        return _sceneRunner.RequestReplayAsync(scene);
    }

    public async Task StopAsync()
    {
        CancellationTokenSource cancellation = _runCancellation;
        Task runTask = _runTask;

        if (!IsRunning || cancellation == null)
            return;

        cancellation.Cancel();

        await Task.WhenAll(
            _sceneRunner.StopAsync(),
            runTask);
    }

    private void SyncChapterVariables()
    {
        if (string.Equals(_yarnChapterId, _chapter.ChapterId))
            return;

        _yarnBridge.BeginChapter(_yarnProject);

        _yarnChapterId = _chapter.ChapterId;

        if (_restoreVariables == null)
            return;

        _yarnBridge.Restore(_restoreVariables);

        _restoreVariables = null;
    }
}
