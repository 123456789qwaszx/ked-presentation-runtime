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
    private readonly ProgressionYarnBridge _yarnBridge;

    //"DialogueRunner.YarnProject"를 꺼내 대조 및 검사.
    public ProgressionLauncher(
        ProgressionDriver driver,
        DialogueRunner dialogueRunner,
        TextAsset chapterJson,
        ProgressionYarnBridge yarnBridge)
    {
        _driver = driver;
        _dialogueRunner = dialogueRunner;
        _chapterJson = chapterJson;
        _yarnBridge = yarnBridge;
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

        // "[3] 연출 실행 상태"는 챕터 수명이다. 챕터를 시작하는 이 자리에서 선언 초기값으로 되돌린다
        // 다른 챕터가 남긴 값도, 이 챕터를 아까 한 번 돌린 값도 안 물려받는다.
        _yarnBridge?.BeginChapter(_dialogueRunner.YarnProject);

        await _driver.RunAsync(scenario);
    }
}