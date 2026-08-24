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

    //"DialogueRunner.YarnProject"를 꺼내 대조 및 검사.
    public ProgressionLauncher(
        ProgressionDriver driver,
        DialogueRunner dialogueRunner,
        TextAsset chapterJson)
    {
        _driver = driver;
        _dialogueRunner = dialogueRunner;
        _chapterJson = chapterJson;
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

        // Yarn 변수 두 계층을 세우는 일은 드라이버가 한다 — 챕터가 바뀔 때마다 다시
        // 세워야 하고, 챕터가 바뀌는 것을 아는 쪽은 흐름을 쥔 드라이버뿐이다.
        await _driver.RunAsync(scenario, _dialogueRunner.YarnProject);
    }
}