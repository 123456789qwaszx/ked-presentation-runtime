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
//
// 연출 기억은 문자열로만 다룬다. 저장에서 꺼낸 두 문자열(회차 백로그, 재개 지점)을
// PlaybackMemory.Restore에 넘기면 복원 장면을 열 때 연출이 알아서 그 줄까지 달린다.
public sealed class ProgressionLauncher : IPlaybackHost
{
    private readonly ProgressionDriver _driver;
    private readonly ScenarioDefinition _scenarioDef;

    private readonly SaveCoordinator _saveCoordinator;

    private readonly PlaybackMemory _memory;

    private bool _isTransitioning;

    public ProgressionLauncher(
        ProgressionDriver driver,
        ScenarioDefinition scenarioDef,
        SaveCoordinator saveCoordinator,
        PlaybackMemory memory)
    {
        _driver = driver;
        _scenarioDef = scenarioDef;
        _saveCoordinator = saveCoordinator;
        _memory = memory;
    }

    public bool IsRunning => _driver.IsRunning;

    public IReadOnlyList<CommittedChoice> PendingPath => _driver.PendingPath;

    // 마지막으로 연 챕터(새 회차든 이어하기든). 아직 아무것도 열지 않았으면 null.
    public ChapterDefinition CurrentChapter { get; private set; }
    
    public void StartNew()
    {
        if (_isTransitioning)
            return;

        _saveCoordinator.BeginNewPlaythrough();

        LaunchNewGame(_scenarioDef.StartChapter);
    }

    // 시나리오의 그 챕터를 새 회차로 처음부터 — 챕터 카드 화면(스토리 예제)이 부른다.
    // 챕터는 서로 독립이다: 스탯은 그 챕터의 초기값에서 시작한다.
    public void StartChapter(string chapterId)
    {
        if (_isTransitioning)
            return;

        if (!_scenarioDef.TryGetChapter(chapterId, out ChapterDefinition chapterDef))
        {
            Debug.LogError($"[진행] 챕터 '{chapterId}'가 시나리오 {_scenarioDef}에 없다.");
            return;
        }

        _saveCoordinator.BeginNewPlaythrough();

        LaunchNewGame(chapterDef);
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
    
    
    private void LaunchNewGame(ChapterDefinition chapterDef)
    {
        CurrentChapter = chapterDef;

        ChapterState chapterState =
            chapterDef.CreateEntryState();

        _memory.Forget();

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
            LaunchNewGame(_scenarioDef.StartChapter);
            return;
        }

        ChapterState chapterState =
            ChapterState.Restore(chapterDef, resume.EpisodeId, resume.Stats);

        // 저장된 연출 기억으로 되돌린다. 깨져 있으면 저장 파일 손상과 같은 뜻이다 — 새로 시작한다.
        // 재생 계획이 없으면(장면 루트 이어하기) 재개 지점도 없다. 계획이 있는데 비어 있으면 깨진 것이다.
        string resumePoint = resume.LoadPlan == null
            ? null
            : resume.LoadPlan.HostState ?? "";

        try
        {
            _memory.Restore(resume.HostState, resumePoint);
        }
        catch (InvalidDataException error)
        {
            Debug.LogWarning($"[진행] 저장된 연출 상태를 읽지 못했다. 새로 시작. — {error.Message}");
            _saveCoordinator.BeginNewPlaythrough();
            LaunchNewGame(_scenarioDef.StartChapter);
            return;
        }

        CurrentChapter = chapterDef;

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
