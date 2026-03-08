#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// 데이터 생성(constructors)과 깊은 복제(deep cloning) 유틸리티.
/// 
/// 이 Partial이 하는 일:
/// - 에디터에서 "새로 추가"되는 Step/Node의 기본 데이터 구조를 만들어준다.
///   - CreateBlankNode(): 빈 NodeSpec 생성 (editorName="", steps=빈 리스트)
///   - CreateBlankStep(): 빈 StepSpec 생성 (editorName="", gate, tracks, compiled 초기화)
/// - 기본 GateToken을 “안전한 형태”로 정리해준다.
///   - SanitizeGate(): 타입에 맞지 않는 필드(seconds/signalKey)를 정리하고 null 방지
/// - 복제(duplicate) 기능에서 원본을 망가뜨리지 않도록 깊은 복제를 제공한다.
///   - CloneNodeDeep(): NodeSpec + 모든 Step/Command를 깊은 복제
///   - CloneStepDeep(): StepSpec + 모든 Track 리스트의 Command를 깊은 복제
///   - CloneCommandDeep(): CommandSpecBase를 JSON round-trip으로 복제 (SerializeReference 대응)
/// 
/// 설계/정책 포인트:
/// - GateToken 정리 정책:
///   - Delay가 아니면 seconds=0, Signal이 아니면 signalKey=""로 정리.
///   - signalKey는 항상 null이 아닌 문자열로 유지.
/// - Duplicate 시 gate가 Immediately이면 “복제본의 gate를 defaultGate로 교체”하는 규칙:
///   - 원본이 즉시 진행(Immediately)인 경우, 복제본은 기본 게이트(_defaultNewStepGate)를 적용해
///     편집 흐름에서 일관된 기본값을 유지하도록 한다.
/// - tracks/compiled:
///   - tracks는 항상 새 StepTracks()로 초기화(원본 참조 공유 방지)
///   - compiled는 런타임용 캐시 성격이므로, 복제 시에도 "빈 리스트"로 새로 만들고
///     실제 내용은 ForceCompileAll() 등 컴파일 경로에서 다시 채우는 것을 기대한다.
/// 
/// 여기부터 보면 좋은 경우(수정 포인트):
/// - “새 Step/Node 만들 때 기본값을 바꾸고 싶다”
///   → CreateBlankStep / CreateBlankNode
/// - “기본 Gate의 정책(Immediate/Delay/Signal 초기값, null 처리)을 바꾸고 싶다”
///   → SanitizeGate, CloneStepDeep의 Immediately 처리 분기
/// - “Duplicate/Copy가 특정 필드를 복제하지 않게 하거나, 추가 후처리를 하고 싶다”
///   → CloneStepDeep / CloneNodeDeep / CloneListInto
/// - “Command 복제 방식(JSON round-trip)을 바꾸고 싶다(성능/호환성/커스텀 복제)”
///   → CloneCommandDeep
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    private StepSpec CreateBlankStep()
    {
        return CreateBlankStep(SanitizeGate(_defaultNewStepGate));
    }

    private static StepSpec CreateBlankStep(GateToken gate)
    {
        return new StepSpec
        {
            editorName = "",
            gate = gate,
            tracks = new StepTracks(),
            compiled = new List<CommandSpecBase>(),
        };
    }

    private static GateToken SanitizeGate(GateToken g)
    {
        if (g.type != GateTokenType.Delay) g.seconds = 0f;
        if (g.type != GateTokenType.Signal) g.signalKey = "";
        g.signalKey = g.signalKey ?? "";
        return g;
    }

    private static NodeSpec CreateBlankNode()
    {
        return new NodeSpec
        {
            editorName = "",
            steps = new List<StepSpec>()
        };
    }

    private StepSpec CloneStepDeep(StepSpec src)
    {
        return CloneStepDeep(src, _defaultNewStepGate);
    }

    private static StepSpec CloneStepDeep(StepSpec src, GateToken defaultGate)
    {
        if (src == null) return CreateBlankStep(defaultGate);

        var dst = new StepSpec
        {
            editorName = src.editorName,
            gate = src.gate,
            tracks = new StepTracks(),
            compiled = new List<CommandSpecBase>()
        };

        if (dst.gate.type == GateTokenType.Immediately)
            dst.gate = SanitizeGate(defaultGate);

        if (src.tracks != null)
        {
            CloneListInto(src.tracks.interaction, dst.tracks.interaction);
            CloneListInto(src.tracks.setup, dst.tracks.setup);
            CloneListInto(src.tracks.motion, dst.tracks.motion);
            CloneListInto(src.tracks.dialogue, dst.tracks.dialogue);
            CloneListInto(src.tracks.fx, dst.tracks.fx);
        }

        return dst;
    }

    private static void CloneListInto(List<CommandSpecBase> src, List<CommandSpecBase> dst)
    {
        if (dst == null) return;
        dst.Clear();

        if (src == null) return;
        foreach (var c in src)
            dst.Add(CloneCommandDeep(c));
    }

    private static CommandSpecBase CloneCommandDeep(CommandSpecBase src)
    {
        if (src == null) return null;

        var t = src.GetType();
        var clone = (CommandSpecBase)System.Activator.CreateInstance(t);

        string json = EditorJsonUtility.ToJson(src);
        EditorJsonUtility.FromJsonOverwrite(json, clone);

        return clone;
    }

    private NodeSpec CloneNodeDeep(NodeSpec src)
    {
        return CloneNodeDeep(src, _defaultNewStepGate);
    }

    private static NodeSpec CloneNodeDeep(NodeSpec src, GateToken defaultGate)
    {
        if (src == null) return CreateBlankNode();

        var dst = new NodeSpec
        {
            editorName = src.editorName,
            steps = new List<StepSpec>()
        };

        if (src.steps != null)
        {
            for (int i = 0; i < src.steps.Count; i++)
                dst.steps.Add(CloneStepDeep(src.steps[i], defaultGate));
        }

        return dst;
    }
}
#endif
