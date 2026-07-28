// using UnityEngine;
// using Yarn.Unity;
//
// // DialogueRunner를 게스트하우스 v3 노드 재생기로 감싼다.
// // 아직 작성되지 않은 노드는 경고만 남기고 시스템 진행을 계속한다.
// public sealed class DialogueRunnerNodePlayer : IDungeonCafeNodePlayer
// {
//     private readonly DialogueRunner _runner;
//
//     public DialogueRunnerNodePlayer(DialogueRunner runner)
//     {
//         _runner = runner;
//     }
//
//     public async YarnTask PlayNodeAsync(string nodeName)
//     {
//         if (_runner == null || string.IsNullOrEmpty(nodeName))
//             return;
//
//         if (_runner.Dialogue == null
//             || !_runner.Dialogue.NodeExists(nodeName))
//         {
//             Debug.LogWarning(
//                 $"[GuesthouseV3] 미작성 노드 통과: {nodeName}");
//             return;
//         }
//
//         _runner.StartDialogue(nodeName);
//
//         while (_runner.IsDialogueRunning)
//             await YarnTask.Yield();
//     }
// }