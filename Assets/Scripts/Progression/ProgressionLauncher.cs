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
        ProgressionDriver driver, DialogueRunner dialogueRunner, TextAsset chapterJson)
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

        await _driver.RunAsync(scenario);
    }
}