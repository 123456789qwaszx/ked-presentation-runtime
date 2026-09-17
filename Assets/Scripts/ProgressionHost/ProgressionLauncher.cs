using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 시작 자리
// - 새 게임 또는 복원 결정
// - Driver 호출(Chapter 진입)
public sealed class ProgressionLauncher
{
    private readonly ProgressionDriver _driver;
    private readonly DialogueRunner _dialogueRunner; // DialogueRunner.YarnProject를 꺼내 대조 및 검사.
    private readonly TextAsset _chapterJson;

    private readonly SaveCoordinator _saveCoordinator;

    private readonly BacklogRecorder _backlog;
    private readonly ProgressionReplayState _replayState;

    private bool _isTransitioning;

    public ProgressionLauncher(
        ProgressionDriver driver,
        DialogueRunner dialogueRunner,
        TextAsset chapterJson,
        SaveCoordinator saveCoordinator,
        BacklogRecorder backlog,
        ProgressionReplayState replayState)
    {
        _driver = driver;
        _dialogueRunner = dialogueRunner;
        _chapterJson = chapterJson;
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

        ScenarioDefinition scenarioDef = LoadScenarioDef();

        LaunchNewGame(scenarioDef.StartChapter);
    }

    
    public void Resume()
    {
        if (_isTransitioning)
            return;

        ProgressionResumePoint resume = 
            _saveCoordinator.LoadActiveResumePoint();
        
        ScenarioDefinition scenarioDef = LoadScenarioDef();

        LaunchResume(scenarioDef, resume);
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

            ProgressionResumePoint resume = 
                _saveCoordinator.LoadActiveResumePoint();
            
            ScenarioDefinition scenarioDef = LoadScenarioDef();

            LaunchResume(scenarioDef, resume);
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
    
    
    private void LaunchNewGame(ChapterDefinition chapterDef)
    {
        ChapterState chapterState =
            chapterDef.CreateEntryState();

        _backlog.Restore(null);
        _replayState.PrepareLoad(null);

        _driver.Start(
            chapterDef,
            chapterState,
            restorePath: null);
    }
    
    private void LaunchResume(
        ScenarioDefinition scenarioDef,
        ProgressionResumePoint resume)
    {
        if (!TryResolveResumeChapter(scenarioDef, resume, out ChapterDefinition chapterDef))
        {
            _saveCoordinator.BeginNewPlaythrough();
            LaunchNewGame(scenarioDef.StartChapter);
            return;
        }

        ChapterState chapterState =
            ChapterState.Restore(chapterDef, resume.EpisodeId, resume.Stats);

        SavedLoadPlan loadPlan = resume.LoadPlan;

        _backlog.Restore(resume.Backlog);
        _replayState.PrepareLoad(loadPlan);

        _driver.Start(
            chapterDef,
            chapterState,
            restorePath: ToScenePath(loadPlan));
    }
    
    private bool TryResolveResumeChapter(
        ScenarioDefinition scenarioDef,
        ProgressionResumePoint resume,
        out ChapterDefinition savedChapter)
    {
        savedChapter = null;

        if (resume.ChapterCompleted)
        {
            Debug.Log($"[진행] 완료된 챕터의 세이브({resume.ChapterId}). 새로 시작.");
            return false;
        }

        if (!scenarioDef.TryGetChapter(resume.ChapterId, out savedChapter)
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
    
    private ScenarioDefinition LoadScenarioDef()
    {
        ScenarioDefinition scenarioDef = ProgressionContentLoader.LoadSingleChapter(_chapterJson);

        if (!ProgressionContentPreflight.CheckAndLog(scenarioDef, _dialogueRunner.YarnProject))
        {
            throw new InvalidOperationException(
                "Progression 콘텐츠 Preflight 검증에 실패했다.");
        }

        return scenarioDef;
    }
    
    // 저장된 Scene 경로를 Progression의 복원 경로로 변환한다.
    // - empty: Scene root 복원
    // - non-empty: Scene root부터 저장 위치까지 다시 소비할 선택 경로
    private static IReadOnlyList<ScenePathStep> ToScenePath(SavedLoadPlan plan)
    {
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
