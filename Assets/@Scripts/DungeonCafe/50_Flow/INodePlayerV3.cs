using Yarn.Unity;

// 시스템 플로우가 시나리오 노드를 재생하는 단일 접점.
// 실제 게임은 DialogueRunner 또는 ScenarioNodeRunner 어댑터를 사용한다.
public interface INodePlayerV3
{
    YarnTask PlayNodeAsync(string nodeName);
}