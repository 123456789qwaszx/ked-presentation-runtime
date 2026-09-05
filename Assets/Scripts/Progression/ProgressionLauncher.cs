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

    public ProgressionLauncher(
        ProgressionDriver driver,
        DialogueRunner dialogueRunner,
        TextAsset chapterJson,
        Func<ProgressionResumePoint> resumeProvider)
    {
        _driver = driver;
        _dialogueRunner = dialogueRunner;
        _chapterJson = chapterJson;
        _resumeProvider = resumeProvider;
    }

    public bool IsRunning => _driver.IsRunning;

    public IReadOnlyList<CommittedChoice> PendingPath => _driver.PendingPath;

    private Task _running;
    
    public Task RequestReplayAsync() => _driver.RequestReplayAsync();

    public async Task StopAsync()
    {
        if (!_driver.IsRunning)
            return;

        await _driver.StopAsync();

        if (_running != null)
            await _running;
    }

    public Task LaunchAsync()
    {
        if (_driver.IsRunning)
            return Task.CompletedTask;

        _running = LaunchCoreAsync();
        return _running;
    }

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
                chapter = savedChapter;
                state = ProgressionState.Restore(savedChapter, resume.EpisodeId, resume.Stats);
                variables = resume.Variables;
                backlog = resume.Backlog;
                loadPlan = resume.LoadPlan;

                Debug.Log(
                    $"[진행] 재개 - {resume.ChapterId}/{resume.EpisodeId}, Yarn 변수 {variables?.Count ?? 0}개");
            }
        }

        await _driver.RunAsync(
            _dialogueRunner.YarnProject,
            chapter,
            state,
            variables,
            backlog,
            loadPlan);
    }
}