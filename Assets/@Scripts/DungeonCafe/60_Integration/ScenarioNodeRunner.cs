using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Yarn 노드 하나를 끝까지 재생하고 기다린다.
///
/// 접객 플로우는 대사 단위가 아니라 '노드 단위'로만 Yarn 을 사용한다.
/// 라인 연출과 커맨드 처리는 기존 VN 프레젠테이션 레이어가 그대로 담당하고,
/// 여기서는 시작과 종료 경계만 다룬다.
/// </summary>
public sealed class ScenarioNodeRunner
{
    private readonly DialogueRunner _runner;

    public ScenarioNodeRunner(DialogueRunner runner)
    {
        _runner = runner;
    }

    public bool IsRunning => _runner != null && _runner.IsDialogueRunning;

    public async YarnTask PlayNodeAsync(string nodeName)
    {
        if (_runner == null || string.IsNullOrWhiteSpace(nodeName))
            return;

        if (!NodeExists(nodeName))
        {
            // 데모 단계에서는 미작성 노드가 흐름을 끊지 않도록 경고만 남기고 통과시킨다.
            Debug.LogWarning($"[ScenarioNodeRunner] Node not found. node={nodeName}");
            return;
        }

        if (_runner.IsDialogueRunning)
            await _runner.Stop();

        await _runner.StartDialogue(nodeName);

        // 러너가 실제로 기동할 때까지 한 프레임 양보한다.
        await YarnTask.Yield();
        
        await AsyncWait.UntilAsync(() => !_runner.IsDialogueRunning);
    }

    private bool NodeExists(string nodeName)
    {
        return _runner.Dialogue != null && _runner.Dialogue.NodeExists(nodeName);
    }
}
