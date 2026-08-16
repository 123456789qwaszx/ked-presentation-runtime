// using System;
//
// [Serializable]
// public sealed class SequenceProgressState
// {
//     public int NodeIndex;
//     public StepGateState StepGate;
//
//     public SequenceProgressState()
//     {
//         NodeIndex = 0;
//         StepGate = default;
//     }
//
//     public bool IsNodeCompleted => StepGate.IsCompleted;
// }