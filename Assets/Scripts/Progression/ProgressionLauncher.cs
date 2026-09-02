using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 층을 시작하는 자리

// 로드 후, 데이터와 대조하며 검사함.
// 진행 Json과 yarn 데이터의 일치를 Tool이 보장하게 된다면 차후 검사는 제거 가능.
//
// 저장 파일과 콘텐츠를 대조하는 자리도 여기다 (M7). 저장은 콘텐츠와 따로 사는 데이터라
// 서로 맞는지 한 번은 봐야 하고, 그 한 번이 여기다 — 이 뒤로 드라이버와 코어는
// "그 에피소드는 그 챕터에 있다"를 믿는다.
public sealed class ProgressionLauncher
{
    private readonly ProgressionDriver _driver;
    private readonly DialogueRunner _dialogueRunner;
    private readonly TextAsset _chapterJson;
    private readonly Func<ProgressionResumePoint> _resumeProvider;

    //"DialogueRunner.YarnProject"를 꺼내 대조 및 검사.
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

    public async Task LaunchAsync()
    {
        if (_driver.IsRunning)
            return;

        ScenarioProgression scenario = ProgressionContentLoader.LoadSingleChapter(_chapterJson);

        if (scenario == null)
            return;

        if (!ProgressionContentPreflight.CheckAndLog(scenario, _dialogueRunner.YarnProject))
            return;

        ChapterProgression chapter = scenario.StartChapter;
        ProgressionState state = chapter.CreateEntryState();

        // 세이브가 있고 그 지점이 아직 콘텐츠에 있으면 거기서 (D-017). 없으면 새 게임 —
        // 저장 후 콘텐츠가 바뀐 것이고, 이어 가는 척하지 않는다.
        //
        // 재개할 수 있는 자리는 장면 루트뿐이다 — 무대 기준선이 거기 선다. 장면 중간을 가리키는
        // 세이브(장면 개념 전의 구형식)나 챕터를 끝낸 세이브는 이어갈 것이 없으니 새로 시작한다.
        ProgressionResumePoint resume = _resumeProvider();
        YarnVariableSnapshot variables = null;
        IReadOnlyList<DialogueLogEntry> backlog = null;

        if (resume != null)
        {
            if (resume.ChapterCompleted)
            {
                Debug.Log($"[진행] 끝낸 챕터의 세이브({resume.ChapterId}) — 새로 시작한다.");
            }
            else if (!scenario.TryGetChapter(resume.ChapterId, out ChapterProgression saved)
                     || !saved.TryGetNode(resume.EpisodeId, out _))
            {
                Debug.LogWarning(
                    $"[진행] 저장 지점 {resume.ChapterId}/{resume.EpisodeId}가 콘텐츠에 없다. 새로 시작한다.");
            }
            else if (!saved.IsSceneRoot(resume.EpisodeId))
            {
                Debug.LogWarning(
                    $"[진행] 저장 지점 {resume.ChapterId}/{resume.EpisodeId}가 장면 중간이다(구형식 세이브). " +
                    "이어 가는 척하지 않고 새로 시작한다.");
            }
            else
            {
                chapter = saved;
                state = ProgressionState.Restore(saved, resume.EpisodeId, resume.Stats);
                variables = resume.Variables;
                backlog = resume.Backlog;

                Debug.Log(
                    $"[진행] 재개 — {resume.ChapterId}/{resume.EpisodeId}, [3] {variables?.Count ?? 0}개");
            }
        }

        // Yarn 변수를 세우는 일은 드라이버가 한다 — 챕터가 바뀔 때마다 다시 세워야 하고,
        // 챕터가 바뀌는 것을 아는 쪽은 흐름을 쥔 드라이버뿐이다. 덤프도 그 뒤에 덮는다.
        await _driver.RunAsync(_dialogueRunner.YarnProject, chapter, state, variables, backlog);
    }
}
