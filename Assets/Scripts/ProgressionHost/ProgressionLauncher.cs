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

    private readonly SaveCoordinator _saveCoordinator;

    // 진행 런타임이 모르는 복원 payload를 실행 전에 준비해 두는 자리들.
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

    public async Task TransitionAsync(Action change)
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        try
        {
            await _driver.StopAsync(); // 1. 현재 재생 중단
            change();                  // 2. 새 게임/로드/포크 등 상태 변경 (로컬 저장이라 기다릴 것이 없음)
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
        ChapterDefinition chapter = scenario.StartChapter;
        ProgressionState state = chapter.CreateEntryState();

        IReadOnlyList<DialogueLogEntry> backlog = null;
        SavedLoadPlan loadPlan = null;

        ProgressionResumePoint resume = _saveCoordinator.LoadActiveResumePoint();

        bool resumeAccepted = false;
        if (resume != null)
        {
            if (resume.ChapterCompleted)
            {
                Debug.Log($"[진행] 완료된 챕터의 세이브({resume.ChapterId}). 새로 시작.");
            }
            else if (!scenario.TryGetChapter(resume.ChapterId, out ChapterDefinition savedChapter)
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
                backlog = resume.Backlog;
                loadPlan = resume.LoadPlan;

                Debug.Log(
                    $"[진행] 재개 - {resume.ChapterId}/{resume.EpisodeId}");
            }
        }

        if (resume != null && !resumeAccepted)
            _saveCoordinator.PrepareNewPlaythrough();

        // 진행 런타임에는 ScenePathStep[]만 들어간다.
        // Yarn 프로젝트/변수, 백로그, Yarn 선택, 라인 표적은 실행 전에 Host가 준비한다.
        _backlog.Restore(backlog);
        _replayState.Stage(loadPlan?.YarnChoices, loadPlan?.Target);

        _driver.Start(chapter, state, BuildRestorePath(loadPlan));
    }

    // SavedLoadPlan에서 진행 좌표만 잘라 낸다.
    //
    // ⚠ null과 빈 목록은 다른 뜻이다.
    //     null      - 일반 진입. 복원을 시작하지 않는다.
    //     빈 목록   - 유효한 복원 진입. 장면 루트 자체가 저장 위치일 수 있다.
    //   표적이 없으면 재생할 라인이 없으므로 복원 자체를 하지 않는다(null).
    private static IReadOnlyList<ScenePathStep> BuildRestorePath(SavedLoadPlan plan)
    {
        if (plan?.Target == null || string.IsNullOrEmpty(plan.Target.NodeName))
        {
            if (plan != null)
                Debug.LogWarning("[진행] 로드 계획에 표적이 없다 - 장면 루트에서 시작.");

            return null;
        }

        var path = new List<ScenePathStep>(plan.Path.Count);

        for (int i = 0; i < plan.Path.Count; i++)
            path.Add(new ScenePathStep(plan.Path[i].FromEpisodeId, plan.Path[i].OptionIndex));

        return path;
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
