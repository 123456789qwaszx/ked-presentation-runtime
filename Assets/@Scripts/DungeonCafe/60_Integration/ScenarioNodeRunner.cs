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

    public async YarnTask PlayNodeAsync(string nodeName)
    {
        if (!_runner.Dialogue.NodeExists(nodeName))
        {
            Debug.LogWarning($"[ScenarioNodeRunner] Node not found. node={nodeName}");
            return;
        }

        if (_runner.IsDialogueRunning)
            await _runner.Stop();

        await _runner.StartDialogue(nodeName);
    }
}
