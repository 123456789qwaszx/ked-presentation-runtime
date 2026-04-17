using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StepSpec
{
    // Debug/authoring marker
    public string editorName;// BeatResolver.TryResolve()가 이걸 찾아서 (nodeIndex, stepIndex)로 변환

    // 편집 원본: 트랙별 커맨드
    //public StepTracks tracks = new();

    // 실행 입력: 에디터에서 컴파일(flatten)된 결과 (런타임이 읽음)
    [SerializeReference] public List<CommandSpecBase> compiled = new();

    // 게이트는 기존 그대로 (원하면 List<GateToken>으로 확장 가능)
    public GateToken gate;
    
#if UNITY_EDITOR
    public bool editorImportedCompiledOnly;
#endif
}

[System.Serializable]
public class NodeSpec
{
    public string editorName;
    
    public List<StepSpec> steps = new();
}

[CreateAssetMenu(fileName = "SequenceSpec", menuName = "CPS/Command/SequenceSpec")]
public class SequenceSpecSO : ScriptableObject
{
    /// <summary>
    /// Key used to locate this sequence inside a SequenceCatalogSO.
    /// Must match (RouteCatalogSO).(RouteDefinition).StartKey(string).
    /// </summary>
    public string sequenceKey;

    public List<NodeSpec> nodes = new();
 
// #if UNITY_EDITOR
//     private void OnValidate()
//     {
//         CompileAllSteps();
//     }
//
//     public void CompileAllSteps()
//     {
//         if (nodes == null)
//             return;
//
//         for (int n = 0; n < nodes.Count; n++)
//         {
//             var node = nodes[n];
//             if (node?.steps == null)
//                 continue;
//
//             for (int s = 0; s < node.steps.Count; s++)
//             {
//                 var step = node.steps[s];
//                 if (step == null)
//                     continue;
//
//                 if (step.editorImportedCompiledOnly)
//                     continue;
//
//                 StepCompiler.CompileInto(step);
//             }
//         }
//     }
// #endif
}