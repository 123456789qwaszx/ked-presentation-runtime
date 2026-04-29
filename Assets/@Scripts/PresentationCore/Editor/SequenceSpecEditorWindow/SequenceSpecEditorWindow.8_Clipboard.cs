#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 클립보드(Copy/Paste) 기능 구현 파트.
/// 
/// 목적:
/// - CommandSpecBase / StepSpec / NodeSpec 를 JSON으로 직렬화해서 시스템 클립보드(EditorGUIUtility.systemCopyBuffer)에 넣고,
///   다시 붙여넣을 때 역직렬화하여 새 인스턴스로 복원한다.
/// - Unity의 직렬화 제약(특히 [SerializeReference] 다형 타입)을 안전하게 다루기 위해
///   "Box ScriptableObject"를 중간 래퍼로 사용한다.
/// 
/// 동작 개요:
/// - Copy:
///   1) CommandClipboardBox / StepClipboardBox / NodeClipboardBox 를 임시로 CreateInstance
///   2) box.spec / box.step / box.node 에 원본 참조를 넣음
///   3) EditorJsonUtility.ToJson(box) 로 JSON 생성
///   4) 접두사(prefix) + json 을 systemCopyBuffer 에 기록
///   5) box DestroyImmediate 로 정리
/// 
/// - Paste:
///   1) systemCopyBuffer 에서 prefix 로 "우리 포맷"인지 판별
///   2) prefix 제거 후 json 추출
///   3) 임시 Box를 CreateInstance
///   4) EditorJsonUtility.FromJsonOverwrite(json, box) 로 역직렬화
///   5) box.spec / box.step / box.node 을 반환
///   6) box DestroyImmediate 로 정리
/// 
/// 핵심 포인트:
/// - Prefix(CommandClipboardPrefix / StepClipboardPrefix / NodeClipboardPrefix)는 "클립보드 문자열이 우리 데이터인지"를 구분하는 매직 태그.
/// - Box(ScriptableObject) 래핑은 Unity EditorJsonUtility가 managed reference/Unity-serialization 컨텍스트에서
///   안정적으로 직렬화/역직렬화를 하도록 돕는 안전장치.
/// - 여기서는 "복사/붙여넣기 데이터 포맷"만 책임지고,
///   실제 삽입 위치/인덱스/폴드아웃/스크롤 등 UI 후처리는 다른 Partial(예: HandleCommandShortcuts, InsertCommandFactoryAt 등)에서 한다.
/// 
/// 여기(이 Partial)를 보면 좋은 변경 포인트:
/// - 클립보드 포맷을 바꾸고 싶을 때
///   - Prefix 정책 변경(버전 태그 추가, 호환성 처리 등)
///   - json 저장 방식 변경(압축, 최소화, 검증 필드 추가 등)
/// 
/// - 붙여넣기 유효성/안전성 강화하고 싶을 때
///   - JSON 파싱 실패/예외 처리, Debug 로그 강화
///   - spec/step/node null 방어, 타입 검증(허용 타입 화이트리스트 등)
/// 
/// - "복사 시 함께 담아야 하는 추가 메타"가 생겼을 때
///   - CommandClipboardBox / StepClipboardBox / NodeClipboardBox 구조 확장(예: track/phase 힌트, editorName, custom flags)
/// 
/// 주의:
/// - EditorJsonUtility는 런타임이 아니라 에디터 전용이며, 이 코드는 UNITY_EDITOR에서만 동작한다.
/// - systemCopyBuffer는 전역 문자열이므로, prefix 충돌을 피하려면 충분히 고유한 문자열을 유지하는 게 좋다.
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    // Clipboard prefixes
    private const string CommandClipboardPrefix = "CPS_CMD_SPEC::";
    private const string StepClipboardPrefix = "CPS_STEP_SPEC::";
    private const string NodeClipboardPrefix = "CPS_NODE_SPEC::";

    // Clipboard Box Classes
    private sealed class CommandClipboardBox : ScriptableObject
    {
        [SerializeReference] public CommandSpecBase spec;
    }

    private sealed class StepClipboardBox : ScriptableObject
    {
        public StepSpec step;
    }

    private sealed class NodeClipboardBox : ScriptableObject
    {
        public NodeSpec node;
    }

    private static void CopyCommandToClipboard(CommandSpecBase spec)
    {
        if (spec == null) return;

        var box = ScriptableObject.CreateInstance<CommandClipboardBox>();
        try
        {
            box.spec = spec;

            string json = EditorJsonUtility.ToJson(box);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SequenceSpecEditor] Copy failed: json is empty");
                return;
            }

            EditorGUIUtility.systemCopyBuffer = CommandClipboardPrefix + json;
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }

    private static bool TryGetClipboardJson(out string json)
    {
        json = null;

        string buf = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(buf)) return false;
        if (!buf.StartsWith(CommandClipboardPrefix, System.StringComparison.Ordinal)) return false;

        json = buf.Substring(CommandClipboardPrefix.Length);
        return !string.IsNullOrEmpty(json);
    }

    private static CommandSpecBase CreateCommandFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var box = ScriptableObject.CreateInstance<CommandClipboardBox>();
        try
        {
            EditorJsonUtility.FromJsonOverwrite(json, box);
            return box.spec;
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }

    private static void CopyStepToClipboard(StepSpec step)
    {
        if (step == null) return;

        var box = ScriptableObject.CreateInstance<StepClipboardBox>();
        try
        {
            box.step = step;
            string json = EditorJsonUtility.ToJson(box);
            EditorGUIUtility.systemCopyBuffer = StepClipboardPrefix + json;
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }

    private static bool TryGetStepClipboardJson(out string json)
    {
        json = null;

        string buf = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(buf)) return false;
        if (!buf.StartsWith(StepClipboardPrefix, System.StringComparison.Ordinal)) return false;

        json = buf.Substring(StepClipboardPrefix.Length);
        return !string.IsNullOrEmpty(json);
    }

    private static StepSpec CreateStepFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var box = ScriptableObject.CreateInstance<StepClipboardBox>();
        try
        {
            EditorJsonUtility.FromJsonOverwrite(json, box);
            return box.step;
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }

    private static void CopyNodeToClipboard(NodeSpec node)
    {
        if (node == null) return;

        var box = ScriptableObject.CreateInstance<NodeClipboardBox>();
        try
        {
            box.node = node;

            string json = EditorJsonUtility.ToJson(box);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SequenceSpecEditor] Copy Node failed: json is empty");
                return;
            }

            EditorGUIUtility.systemCopyBuffer = NodeClipboardPrefix + json;
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }

    private static bool TryGetNodeClipboardJson(out string json)
    {
        json = null;

        string buf = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(buf)) return false;
        if (!buf.StartsWith(NodeClipboardPrefix, System.StringComparison.Ordinal)) return false;

        json = buf.Substring(NodeClipboardPrefix.Length);
        return !string.IsNullOrEmpty(json);
    }

    private static NodeSpec CreateNodeFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var box = ScriptableObject.CreateInstance<NodeClipboardBox>();
        try
        {
            EditorJsonUtility.FromJsonOverwrite(json, box);
            return box.node;
        }
        finally
        {
            Object.DestroyImmediate(box);
        }
    }
}
#endif