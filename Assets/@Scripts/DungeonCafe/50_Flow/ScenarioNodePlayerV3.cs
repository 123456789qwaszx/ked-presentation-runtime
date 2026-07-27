// using Yarn.Unity;
//
// // 기존 ScenarioNodeRunner를 게스트하우스 v3 노드 재생기로 감싼다.
// // EpisodePlayer 파이프라인을 그대로 사용할 때 선택한다.
// public sealed class ScenarioNodePlayerV3 : INodePlayerV3
// {
//     private readonly ScenarioNodeRunner _runner;
//
//     public ScenarioNodePlayerV3(ScenarioNodeRunner runner)
//     {
//         _runner = runner;
//     }
//
//     public YarnTask PlayNodeAsync(string nodeName)
//         => _runner.PlayNodeAsync(nodeName);
// }