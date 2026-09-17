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
        LaunchCore();
    }

    public void Resume()
    {
        if (_isTransitioning)
            return;

        ProgressionResumePoint resume =
            _saveCoordinator.LoadActiveResumePoint();

        LaunchCore(resume);
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

            ProgressionResumePoint resume = _saveCoordinator.LoadActiveResumePoint();
            LaunchCore(resume);
        }
        finally
        {
            _isTransitioning = false;
        }
    }
    
    private void LaunchCore(ProgressionResumePoint resume = null)
    {
        ScenarioDefinition scenarioDef =
            ProgressionContentLoader.LoadSingleChapter(_chapterJson);

        if (!ProgressionContentPreflight.CheckAndLog(scenarioDef, _dialogueRunner.YarnProject))
            return;

        ChapterDefinition chapterDef = scenarioDef.StartChapter;
        ChapterState chapterState = chapterDef.CreateEntryState();

        IReadOnlyList<DialogueLogEntry> backlog = null;
        SavedLoadPlan loadPlan = null;

        if (resume != null)
        {
            if (TryValidateResume(scenarioDef, resume, 
                    out ChapterDefinition savedChapter))
            {
                chapterDef = savedChapter;
                chapterState = ChapterState.Restore(
                    savedChapter,
                    resume.EpisodeId,
                    resume.Stats);

                backlog = resume.Backlog;
                loadPlan = resume.LoadPlan;
            }
            else { _saveCoordinator.BeginNewPlaythrough(); }
        }

        _backlog.Restore(backlog);
        _replayState.Stage(
            loadPlan?.YarnChoices,
            loadPlan?.Target);

        _driver.Start(
            chapterDef,
            chapterState,
            BuildRestorePath(loadPlan));
    }

    private bool TryValidateResume(
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

    // SavedLoadPlan에서 진행 좌표만 잘라냄
    // - null: 일반 진입. 복원을 시작하지 않는다.
    // - 빈 목록: 유효한 복원 진입. 장면 루트 자체가 저장 위치일 수 있다.
    //
    // 표적이 없으면 재생할 라인이 없으므로 복원 자체를 하지 않는다(null).
    private IReadOnlyList<ScenePathStep> BuildRestorePath(SavedLoadPlan plan)
    {
        if (plan?.Target == null || string.IsNullOrEmpty(plan.Target.NodeName))
        {
            if (plan != null)
                Debug.LogWarning("[진행] 로드 계획에 표적이 없다 - 장면 루트에서 시작.");

            return null;
        }

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
}
