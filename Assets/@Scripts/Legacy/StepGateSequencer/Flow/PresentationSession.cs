// using System;
//
// public enum PresentationPointerEndReason
// {
//     Completed,
//     Cancelled
// }
//
// // 완성도 높은 특정 연출(컷인 등)을 재생하는 "포인터" 전용 엔진.
// // 단일 소유자: Play()가 다시 호출되면 진행 중인 이전 재생은 강제 종료(Cancel).
// // 자연 종료(시퀀스 끝에 도달)는 Finish로 정리되어 OnStepLifetimeFinished/
// // OnRunLifetimeFinished를 통한 최종 상태 커밋을 보장.
// public sealed class PresentationSession
// {
//     private readonly StepGatePlanBuilder _gatePlanner;
//     private readonly StepGateAdvancer _gateAdvancer;
//     private readonly CommandExecutor _executor;
//
//     private readonly PresentationSessionContext _context;
//     private readonly ISeekStateQuery _linePresentationAdvanceState;
//     private readonly PresentationStage _stage;
//
//     private CommandRunScope _scope;
//     private SequenceProgressState _state;
//     private SequenceSpecSO _sequence;
//
//     public CommandRunScope CurrentScope => _scope;
//
//     public bool IsRunning => _sequence != null && _state != null && _scope != null;
//
//     public event Action<PresentationPointerEndReason> Ended;
//
//     public PresentationSession(
//         StepGatePlanBuilder gatePlanner,
//         StepGateAdvancer gateAdvancer,
//         CommandExecutor executor,
//         PresentationSessionContext presentationSessionContext,
//         ISeekStateQuery linePresentationAdvanceState,
//         PresentationStage presentationStage)
//     {
//         _gatePlanner = gatePlanner;
//         _gateAdvancer = gateAdvancer;
//         _executor = executor;
//         _context = presentationSessionContext;
//         _linePresentationAdvanceState = linePresentationAdvanceState;
//         _stage = presentationStage;
//     }
//
//     public void Play(SequenceSpecSO sequence)
//     {
//         if (sequence == null)
//             return;
//
//         if (IsRunning)
//             EndImmediately();
//
//         _state = new SequenceProgressState();
//         _sequence = sequence;
//         _scope = new CommandRunScope(_context, _linePresentationAdvanceState, _stage);
//
//         _context.ResetSessionFlagsForStart();
//
//         _gatePlanner.BuildForCurrentNode(_sequence, _state);
//
//         PlayStep(_state.NodeIndex, _state.StepGate.Cursor);
//     }
//
//     public void Tick()
//     {
//         if (!IsRunning)
//             return;
//
//         if (_context != null && _context.CloseRequested)
//         {
//             EndImmediately();
//             return;
//         }
//
//         while (true)
//         {
//             bool advanced = _gateAdvancer.TryAdvanceStepGate(_state, _context);
//             if (!advanced)
//                 break;
//
//             if (_state.IsNodeCompleted)
//             {
//                 _state.NodeIndex++;
//
//                 if (_state.NodeIndex >= _sequence.nodes.Count)
//                 {
//                     Complete();
//                     return;
//                 }
//
//                 if (_context != null && _context.CloseRequested)
//                 {
//                     EndImmediately();
//                     return;
//                 }
//
//                 _gateAdvancer.ClearLatchedSignals();
//                 _gatePlanner.BuildForCurrentNode(_sequence, _state);
//
//                 PlayStep(_state.NodeIndex, _state.StepGate.Cursor);
//                 return;
//             }
//
//             int currentNodeIndex = _state.NodeIndex;
//             int currentStep = _state.StepGate.Cursor;
//             PlayStep(currentNodeIndex, currentStep);
//         }
//     }
//
//     public void RequestEnd() => _context?.RequestClose();
//
//     public void EndImmediately()
//     {
//         if (!IsRunning)
//             return;
//
//         Stop(CleanupPolicy.Cancel);
//         Ended?.Invoke(PresentationPointerEndReason.Cancelled);
//     }
//
//     private void Complete()
//     {
//         Stop(CleanupPolicy.Finish);
//         Ended?.Invoke(PresentationPointerEndReason.Completed);
//     }
//
//     private void Stop(CleanupPolicy policy)
//     {
//         _gateAdvancer.ClearLatchedSignals();
//         _executor.Stop(policy);
//
//         _scope = null;
//         _sequence = null;
//         _state = null;
//     }
//
//     private void PlayStep(int nodeIndex, int stepIndex)
//     {
//         if (nodeIndex < 0 || nodeIndex >= _sequence.nodes.Count)
//             return;
//
//         NodeSpec node = _sequence.nodes[nodeIndex];
//         _executor.PlayStep(node, stepIndex, _scope);
//     }
// }