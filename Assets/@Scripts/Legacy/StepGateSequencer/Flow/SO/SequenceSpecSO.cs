// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// [Serializable]
// public sealed class StepSpec
// {
//     // Debug/authoring marker
//     public string editorName;// BeatResolver.TryResolve()가 이걸 찾아서 (nodeIndex, stepIndex)로 변환
//
//     [SerializeReference] public List<CommandSpecBase> compiled = new();
//
//     public GateToken gate;
// }
//
// [System.Serializable]
// public class NodeSpec
// {
//     public string editorName;
//     
//     public List<StepSpec> steps = new();
// }
//
// [CreateAssetMenu(fileName = "SequenceSpec", menuName = "CPS/Command/SequenceSpec")]
// public class SequenceSpecSO : ScriptableObject
// {
//     /// <summary>
//     /// Key used to locate this sequence inside a SequenceCatalogSO.
//     /// Must match (RouteCatalogSO).(RouteDefinition).StartKey(string).
//     /// </summary>
//     public string sequenceKey;
//
//     public List<NodeSpec> nodes = new();
// }