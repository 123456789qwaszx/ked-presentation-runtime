using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ked.Progression;
using Ked.Progression.Save;
using UnityEngine;

// 진행 시작 자리
// - 새 게임 또는 복원 결정
// - Driver 호출(Chapter 진입)
// 진행·저장 예제의 재생 호스트이기도 하다 — 재생 단위는 챕터의 장면이다.
public sealed class ProgressionLauncher : IPlaybackHost
{
    private readonly ProgressionDriver _driver;
    private readonly ScenarioDefinition _scenarioDef;

    private readonly SaveCoordinator _saveCoordinator;

    private readonly BacklogRecorder _backlog;
    private readonly ProgressionReplayState _replayState;

    private bool _isTransitioning;

    public ProgressionLauncher(
        ProgressionDriver driver,
        ScenarioDefinition scenarioDef,
        SaveCoordinator saveCoordinator,
        BacklogRecorder backlog,
        ProgressionReplayState replayState)
    {
        _driver = driver;
        _scenarioDef = scenarioDef;
        _saveCoordinator = saveCoordinator;
        _backlog = backlog;
        _replayState = replayState;
    }

    public bool IsRunning => _driver.IsRunning;

    public IReadOnlyList<CommittedChoice> PendingPath => _driver.PendingPath;
    
    public void StartNewGame()
    {
        if (_isTransitioning)
            return;

        _saveCoordinator.BeginNewPlaythrough();

        LaunchNewGame();
    }
    
    public void Resume()
    {
        if (_isTransitioning)
            return;

        ResumePoint resume =
            _saveCoordinator.LoadActiveResumePoint();

        LaunchResume(resume);
    }

    public async Task TransitionAndResumeAsync(Action change)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        try
        {
            await _driver.StopAsync();

            change();

            ResumePoint resume =
                _saveCoordinator.LoadActiveResumePoint();

            LaunchResume(resume);
        }
        finally
        {
            _isTransitioning = false;
        }
    }
    
    public async Task ExitAsync()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        try
        {
            await _driver.StopAsync();
        }
        finally
        {
            _isTransitioning = false;
        }
    }
    
    public Task RequestReplayAsync()
    {
        return _driver.RequestReplayAsync();
    }
    
    
    private void LaunchNewGame()
    {
        ChapterDefinition chapterDef = _scenarioDef.StartChapter;
        
        ChapterState chapterState =
            chapterDef.CreateEntryState();

        _backlog.Restore(null);
        _replayState.PrepareLoad(null);

        _driver.Start(
            chapterDef,
            chapterState,
            restorePath: null);
    }
    
    private void LaunchResume(ResumePoint resume)
    {
        if (!TryResolveResumeChapter(resume, out ChapterDefinition chapterDef))
        {
            _saveCoordinator.BeginNewPlaythrough();
            LaunchNewGame();
            return;
        }

        ChapterState chapterState =
            ChapterState.Restore(chapterDef, resume.EpisodeId, resume.Stats);

        // 저장된 연출 상태를 되읽는다. 깨져 있으면 저장 파일 손상과 같은 뜻이다 — 새로 시작한다.
        List<DialogueLogEntry> backlog;
        PresentationResumePoint loadPlan;

        try
        {
            backlog = PresentationMemory.ReadBacklog(resume.HostState);
            loadPlan = resume.LoadPlan == null
                ? null
                : PresentationMemory.ReadResumePoint(resume.LoadPlan.HostState);
        }
        catch (InvalidDataException error)
        {
            Debug.LogWarning($"[진행] 저장된 연출 상태를 읽지 못했다. 새로 시작. — {error.Message}");
            _saveCoordinator.BeginNewPlaythrough();
            LaunchNewGame();
            return;
        }

        _backlog.Restore(backlog);
        _replayState.PrepareLoad(loadPlan);

        _driver.Start(
            chapterDef,
            chapterState,
            restorePath: ToScenePath(resume.LoadPlan));
    }
    
    private bool TryResolveResumeChapter(
        ResumePoint resume,
        out ChapterDefinition savedChapter)
    {
        savedChapter = null;

        if (resume == null)
            return false;

        if (resume.ChapterCompleted)
        {
            Debug.Log($"[진행] 완료된 챕터의 세이브({resume.ChapterId}). 새로 시작.");
            return false;
        }

        if (!_scenarioDef.TryGetChapter(resume.ChapterId, out savedChapter)
            || !savedChapter.TryGetNode(resume.EpisodeId, out _))
        {
            Debug.LogWarning($"[진행] 저장 지점 {resume.ChapterId}/{resume.EpisodeId}가 현재 콘텐츠에 없다. 새로 시작.");
            return false;
        }

        if (!savedChapter.IsSceneRoot(resume.EpisodeId))
        {
            Debug.LogWarning($"[진행] 저장 지점 {resume.ChapterId}/{resume.EpisodeId}가 장면 중간(구형식 세이브). 새로 시작.");
            return false;
        }

        return true;
    }
    
    // 저장된 Scene 경로를 Progression의 복원 경로로 변환한다.
    // - empty: Scene root 복원
    // - non-empty: Scene root부터 저장 위치까지 다시 소비할 선택 경로
    private static IReadOnlyList<ScenePathStep> ToScenePath(SavedLoadPlan plan)
    {
        if (plan == null)
            return null;

        var path = new List<ScenePathStep>(plan.Path.Count);

        for (int i = 0; i < plan.Path.Count; i++)
        {
            path.Add(
                new ScenePathStep(
                    plan.Path[i].FromEpisodeId,
                    plan.Path[i].OptionIndex));
        }

        return path;
    }
}
