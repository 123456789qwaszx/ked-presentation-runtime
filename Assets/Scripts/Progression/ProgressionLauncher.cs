using System;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 층을 시작하는 자리

// 로드 후, 데이터와 대조하며 검사함.
// 진행 Json과 yarn 데이터의 일치를 Tool이 보장하게 된다면 차후 검사는 제거 가능.
public sealed class ProgressionLauncher
{
    private readonly ProgressionDriver _driver;
    private readonly DialogueRunner _dialogueRunner;
    private readonly TextAsset _chapterJson;

    // 재개점 공급자 (M7). null이거나 null을 돌려주면 새 게임.
    // 저장 층(SaveCoordinator)이 로컬 세이브를 읽어 돌려준다 — 로컬 파일이라 동기로 충분하고,
    // 여기가 async가 아니어서 launch 경로에 대기 지점이 늘지 않는다.
    private readonly Func<ProgressionResumePoint> _resumeProvider;

    //"DialogueRunner.YarnProject"를 꺼내 대조 및 검사.
    public ProgressionLauncher(
        ProgressionDriver driver,
        DialogueRunner dialogueRunner,
        TextAsset chapterJson,
        Func<ProgressionResumePoint> resumeProvider = null)
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

        // 세이브가 있으면 잇는다 (M7, D-017). 재개점의 유효성 판단(챕터·에피소드가 아직
        // 있는가)은 드라이버 몫이다 — 여기는 읽어서 건네기만 한다.
        ProgressionResumePoint resumeFrom = _resumeProvider?.Invoke();

        // Yarn 변수 두 계층을 세우는 일은 드라이버가 한다 — 챕터가 바뀔 때마다 다시
        // 세워야 하고, 챕터가 바뀌는 것을 아는 쪽은 흐름을 쥔 드라이버뿐이다.
        await _driver.RunAsync(scenario, _dialogueRunner.YarnProject, resumeFrom);
    }
}
