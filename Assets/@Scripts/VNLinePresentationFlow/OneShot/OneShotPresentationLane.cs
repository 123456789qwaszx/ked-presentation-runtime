// using System.Collections;
// using Yarn.Unity;
//
// // command-only 노드를 돌려 커맨드를 모은 뒤, 한 번 flush 한 뒤,
// // blockMain일 시, entry까지 (메인을) 블로킹한다.
// public sealed class OneShotPresentationLane
// {
//     private readonly DialogueRunner _oneShotDialogueRunner;
//     private readonly YarnBridgePlaybackDriver _oneShotPlaybackDriver;
//
//     private CommandRunTicket _currentTicket;
//
//     public OneShotPresentationLane(
//         DialogueRunner oneShotDialogueRunner,
//         YarnBridgePlaybackDriver oneShotPlaybackDriver)
//     {
//         _oneShotDialogueRunner = oneShotDialogueRunner;
//         _oneShotPlaybackDriver = oneShotPlaybackDriver;
//     }
//
//     public IEnumerator RunNodeCoroutine(string nodeName, bool blockMain = true)
//     {
//         if (_oneShotDialogueRunner == null || string.IsNullOrEmpty(nodeName))
//             yield break;
//
//         if (_oneShotDialogueRunner.IsDialogueRunning)
//         {
//             YarnTask stopTask = _oneShotDialogueRunner.Stop();
//
//             while (!stopTask.IsCompletedSuccessfully())
//                 yield return null;
//         }
//
//         _oneShotPlaybackDriver.Clear();
//
//         YarnTask startTask = _oneShotDialogueRunner.StartDialogue(nodeName);
//
//         while (!startTask.IsCompletedSuccessfully())
//             yield return null;
//
//         // 노드의 모든 <<command>>가 수집될 때까지(=노드 종료) 대기.
//         // command-only라 RunLineAsync로 멈추지 않으므로 곧바로 끝난다.
//         while (_oneShotDialogueRunner.IsDialogueRunning)
//             yield return null;
//         
//         // 한 번에 flush -> one-shot executor 실행. entry 적용까지 대기(메인 블로킹).
//         _currentTicket = _oneShotPlaybackDriver.PlayCollected();
//
//         // beat_free:
//         // command batch를 시작만 하고 main runner에게 즉시 제어권을 돌려준다.
//         if (!blockMain)
//             yield break;
//
//         // beat:
//         // wait=true 커맨드가 있으면 EntryClosed가 늦게 닫히므로 main도 그만큼 기다린다.
//         while (_currentTicket != null && !_currentTicket.EntryClosed)
//             yield return null;
//     }
// }