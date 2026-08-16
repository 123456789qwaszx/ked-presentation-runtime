// using UnityEngine;
// using Yarn.Unity;
//
// // One-shot 레인용 프레젠터. 페이싱 레인과 달리 per-line 조율이 없어 사실상 비어 있다.
// // 오케스트레이션/flush는 OneShotPresentationLane이 담당.
// public sealed class OneShotPresentationPresenter : DialoguePresenterBase
// {
//     private IYarnLaneDebugSink _debugSink;
//     private string _currentNodeName;
//
//     public void Initialize(DialogueRunner dialogueRunner, IYarnLaneDebugSink debugSink = null)
//     {
//         if (dialogueRunner != null)
//             dialogueRunner.onNodeStart?.AddListener(nodeName => _currentNodeName = nodeName);
//
//         _debugSink = debugSink;
//     }
//
//     public override YarnTask OnDialogueStartedAsync()
//     {
//         return YarnTask.CompletedTask;
//     }
//
//     public override YarnTask OnDialogueCompleteAsync()
//     {
//         _debugSink?.ClearOneShot();
//         return YarnTask.CompletedTask;
//     }
//
//     // one-shot 노드는 command-only여야 한다. 라인이 있으면 멈추지 않고 즉시 통과(+경고).
//     public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
//     {
//         _debugSink?.SetOneShot(
//             _currentNodeName,
//             line.TextWithoutCharacterName.Text);
//
//         Debug.LogWarning("[OneShotPresentationPresenter] one-shot 노드는 command-only여야 합니다. 라인을 건너뜁니다.");
//         return YarnTask.CompletedTask;
//     }
// }