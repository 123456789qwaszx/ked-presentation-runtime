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
    private readonly DialogueRunner _dialogueRunner; //"DialogueRunner.YarnProject"를 꺼내 대조 및 검사.
    private readonly TextAsset _chapterJson;
    private readonly Func<ProgressionResumePoint> _resumeProvider;
    private readonly Func<Task> _prepareNewPlaythrough;
    private bool _isTransitioning;

    public ProgressionLauncher(
        ProgressionDriver driver,
        DialogueRunner dialogueRunner,
        TextAsset chapterJson,
        Func<ProgressionResumePoint> resumeProvider,
        Func<Task> prepareNewPlaythrough)
    {
        _driver = driver;
        _dialogueRunner = dialogueRunner;
        _chapterJson = chapterJson;
        _resumeProvider = resumeProvider;
        _prepareNewPlaythrough = prepareNewPlaythrough;
    }

    public bool IsRunning => _driver.IsRunning;

    public IReadOnlyList<CommittedChoice> PendingPath => _driver.PendingPath;

    public async Task TransitionAsync(Func<Task> change)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        try
        {
            await _driver.StopAsync(); // 1. 현재 재생 중단
            await change();            // 2. 새 게임/로드/포크 등 상태 변경
            await LaunchCoreAsync();   // 3. 변경된 상태로 다시 재생
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    // 현재 진행 가능한 회차를 이어서 재생
    public async Task ResumeAsync()
    {
        if (IsRunning || _isTransitioning)
            return;

        _isTransitioning = true;

        try
        {
            await LaunchCoreAsync();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public Task RequestReplayAsync() => _driver.RequestReplayAsync();

    private async Task LaunchCoreAsync()
    {
        ScenarioProgression scenario =
            ProgressionContentLoader.LoadSingleChapter(_chapterJson);

        if (scenario == null)
            return;

        if (!ProgressionContentPreflight.CheckAndLog(scenario, _dialogueRunner.YarnProject))
            return;

        // 기본값은 새 게임 시작.
        ChapterProgression chapter = scenario.StartChapter;
        ProgressionState state = chapter.CreateEntryState();

        YarnVariableSnapshot variables = null;
        IReadOnlyList<DialogueLogEntry> backlog = null;
        SavedLoadPlan loadPlan = null;

        ProgressionResumePoint resume = _resumeProvider();

        bool resumeAccepted = false;
        if (resume != null)
        {
            if (resume.ChapterCompleted)
            {
                Debug.Log($"[진행] 완료된 챕터의 세이브({resume.ChapterId}). 새로 시작.");
            }
            else if (!scenario.TryGetChapter(resume.ChapterId, out ChapterProgression savedChapter)
                     || !savedChapter.TryGetNode(resume.EpisodeId, out _))
            {
                Debug.LogWarning(
                    $"[진행] 저장 지점 {resume.ChapterId}/{resume.EpisodeId}가 현재 콘텐츠에 없다. 새로 시작.");
            }
            else if (!savedChapter.IsSceneRoot(resume.EpisodeId))
            {
                Debug.LogWarning(
                    $"[진행] 저장 지점 {resume.ChapterId}/{resume.EpisodeId}가 장면 중간(구형식 세이브). 새로 시작.");
            }
            else
            {
                resumeAccepted = true;
                chapter = savedChapter;
                state = ProgressionState.Restore(savedChapter, resume.EpisodeId, resume.Stats);
                variables = resume.Variables;
                backlog = resume.Backlog;
                loadPlan = resume.LoadPlan;

                Debug.Log(
                    $"[진행] 재개 - {resume.ChapterId}/{resume.EpisodeId}, Yarn 변수 {variables?.Count ?? 0}개");
            }
        }

        if (resume != null && !resumeAccepted)
            await _prepareNewPlaythrough();

        _driver.Start(
            _dialogueRunner.YarnProject,
            chapter,
            state,
            variables,
            backlog,
            loadPlan);
    }

    // 현재 진행을 끝내고 idle 상태로 빠진다.
    // 새 진행을 시작하지 않는다.
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
}
