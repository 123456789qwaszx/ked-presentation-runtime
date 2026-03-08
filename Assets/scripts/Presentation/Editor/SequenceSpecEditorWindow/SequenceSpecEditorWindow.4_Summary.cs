#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Summary generation & utility helpers for the SequenceSpec Editor.
/// 
/// 역할
/// - 에디터 UI에서 보여줄 "요약 문자열"을 만든다.
///   - GateToken 요약: SummarizeGate()
///   - Command(ManagedReference) 한 줄 요약: SummarizeCommand(), GetManagedRefTypeName()
/// - Step의 Track 리스트(Interaction/Setup/Motion/Dialogue/FX)와 Compiled 리스트 사이의
///   "원본 위치(트랙/인덱스)"를 매핑한다.
///   - BuildOriginMapForStep(): managedReferenceId -> (track, index)
///   - TryGetOrigin(): compiled element가 어느 트랙의 몇 번째인지 찾기
/// - Compiled Preview(런타임 순서)에서 표시할 한 줄 문자열을 만든다.
///   - TryReadMeta(): command의 meta(_meta/meta/Meta)에서 track/phase/blocking/infinite/duration 읽기
///   - SummarizeCompiledLine(): origin(원본)과 meta(컴파일 결과)를 비교해 drift/missing 표시
/// 
/// 언제 이 파일을 보나?
/// - 리스트 라벨/요약 텍스트(예: "Delay(0.5s)", "Signal('X')", "#3 Fade (screen/role)") 포맷을 바꾸고 싶을 때
/// - Compiled Preview의 배지(P/T), 시간 힌트([B]/[INF]/[1.2s]), 경고(!drift/!missing) 기준을 바꾸고 싶을 때
/// - StepTracks 필드명/트랙 순서가 바뀌어서 Origin 매핑이 깨졌을 때
///   (TrackFieldNames/TrackTypes와 BuildOriginMapForStep의 ScanList 로직 확인)
/// - 커맨드 메타 구조가 바뀌었을 때(meta 필드명, track/phase 힌트 프로퍼티명 등)
/// 
/// 주의/전제
/// - Command 리스트는 [SerializeReference] ManagedReference 기반이어야 managedReferenceId로 추적 가능.
/// - Origin 매핑은 managedReferenceId에 의존한다(복사/붙여넣기/재생성 시 id가 달라질 수 있음).
/// - meta 탐색은 "_meta"/"meta"/"Meta" 3가지 이름을 허용하며, 그 내부 필드명(track/phase/힌트들)에 의존한다.
/// </summary>

public sealed partial class SequenceSpecEditorWindow
{
    private readonly struct Origin
    {
        public readonly CommandTrackType track;
        public readonly int index;

        public Origin(CommandTrackType t, int i)
        {
            track = t;
            index = i;
        }
    }

    private string SummarizeGate(SerializedProperty gateProp)
    {
        if (gateProp == null) return "(null)";

        var typeProp = gateProp.FindPropertyRelative("type");
        if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
        {
            string t = typeProp.enumDisplayNames[typeProp.enumValueIndex];

            if (t == "Delay")
            {
                var sec = gateProp.FindPropertyRelative("seconds");
                if (sec != null && sec.propertyType == SerializedPropertyType.Float)
                    return $"Delay({sec.floatValue:0.###}s)";
            }

            if (t == "Signal")
            {
                var key = gateProp.FindPropertyRelative("signalKey");
                if (key != null && key.propertyType == SerializedPropertyType.String)
                    return $"Signal('{key.stringValue}')";
            }

            return t;
        }

        return gateProp.type;
    }

    private string SummarizeCommand(SerializedProperty cmdProp, int index)
    {
        if (cmdProp == null) return $"#{index} (null)";

        if (cmdProp.propertyType != SerializedPropertyType.ManagedReference)
            return $"#{index} (Non-ManagedReference!)";

        var typeName = GetManagedRefTypeName(cmdProp);
        if (string.IsNullOrEmpty(typeName)) typeName = "(null-ref)";

        string screenId = cmdProp.FindPropertyRelative("screenId")?.stringValue ?? "";
        string roleKey = cmdProp.FindPropertyRelative("roleKey")?.stringValue ?? "";

        if (!string.IsNullOrWhiteSpace(screenId) || !string.IsNullOrWhiteSpace(roleKey))
            return $"#{index} {typeName}  ({screenId}/{roleKey})";

        return $"#{index} {typeName}";
    }

    private static string GetManagedRefTypeName(SerializedProperty managedRefProp)
    {
        string full = managedRefProp.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(full)) return null;

        int space = full.IndexOf(' ');
        if (space < 0 || space + 1 >= full.Length) return null;

        string className = full.Substring(space + 1);
        if (string.IsNullOrEmpty(className)) return null;

        int lastDot = className.LastIndexOf('.');
        return lastDot >= 0 ? className.Substring(lastDot + 1) : className;
    }

    private Dictionary<long, Origin> BuildOriginMapForStep(SerializedProperty stepProp)
    {
        var map = new Dictionary<long, Origin>();

        var tracksProp = stepProp.FindPropertyRelative("tracks");
        if (tracksProp == null) return map;

        void ScanList(string name, CommandTrackType track)
        {
            var lp = tracksProp.FindPropertyRelative(name);
            if (lp == null || !lp.isArray) return;

            for (int i = 0; i < lp.arraySize; i++)
            {
                var el = lp.GetArrayElementAtIndex(i);
                if (el == null) continue;
                if (el.propertyType != SerializedPropertyType.ManagedReference) continue;

                long id = el.managedReferenceId;
                if (id == 0) continue;

                if (!map.ContainsKey(id))
                    map[id] = new Origin(track, i);
            }
        }

        int n = UnityEngine.Mathf.Min(TrackFieldNames.Length, TrackTypes.Length);
        for (int i = 0; i < n; i++)
        {
            ScanList(TrackFieldNames[i], TrackTypes[i]);
        }

        return map;
    }

    private bool TryGetOrigin(SerializedProperty compiledEl, Dictionary<long, Origin> originMap, out Origin origin)
    {
        origin = default;

        if (compiledEl == null || compiledEl.propertyType != SerializedPropertyType.ManagedReference)
            return false;

        long id = compiledEl.managedReferenceId;
        if (id == 0) return false;

        return originMap != null && originMap.TryGetValue(id, out origin);
    }

    private static string PhaseShort(CommandPhase p) => p switch
    {
        CommandPhase.Setup => "S",
        CommandPhase.Motion => "M",
        CommandPhase.Dialogue => "D",
        CommandPhase.FX => "F",
        CommandPhase.Teardown => "T",
        _ => "?"
    };

    private static string TrackShort(CommandTrackType t) => t switch
    {
        CommandTrackType.Interaction => "I",
        CommandTrackType.Setup => "S",
        CommandTrackType.Motion => "M",
        CommandTrackType.Dialogue => "D",
        CommandTrackType.FX => "FX",
        _ => "?"
    };

    private static bool TryReadMeta(SerializedProperty cmdProp,
        out CommandTrackType metaTrack,
        out CommandPhase metaPhase,
        out bool blocking,
        out bool infinite,
        out float duration)
    {
        metaTrack = default;
        metaPhase = default;
        blocking = false;
        infinite = false;
        duration = 0f;

        if (cmdProp == null || cmdProp.propertyType != SerializedPropertyType.ManagedReference)
            return false;

        var meta =
            cmdProp.FindPropertyRelative("_meta") ??
            cmdProp.FindPropertyRelative("meta") ??
            cmdProp.FindPropertyRelative("Meta");
        if (meta == null) return false;

        var tr = meta.FindPropertyRelative("track");
        var ph = meta.FindPropertyRelative("phase");
        var bh = meta.FindPropertyRelative("blockingHint");
        var ih = meta.FindPropertyRelative("infiniteHint");
        var dh = meta.FindPropertyRelative("durationHint");

        if (tr != null && tr.propertyType == SerializedPropertyType.Enum)
            metaTrack = (CommandTrackType)tr.intValue;

        if (ph != null && ph.propertyType == SerializedPropertyType.Enum)
            metaPhase = (CommandPhase)ph.intValue;

        if (bh != null && bh.propertyType == SerializedPropertyType.Boolean)
            blocking = bh.boolValue;
        if (ih != null && ih.propertyType == SerializedPropertyType.Boolean)
            infinite = ih.boolValue;
        if (dh != null && dh.propertyType == SerializedPropertyType.Float)
            duration = dh.floatValue;

        return true;
    }

    private string SummarizeCompiledLine(
        SerializedProperty cmdProp,
        int compiledIndex,
        Dictionary<long, Origin> originMap,
        out bool hasDrift,
        out bool missingOrigin)
    {
        hasDrift = false;
        missingOrigin = false;

        string baseLine = SummarizeCommand(cmdProp, compiledIndex);

        bool hasMeta = TryReadMeta(cmdProp, out var metaTrack, out var metaPhase, out bool block, out bool inf,
            out float dur);

        bool hasOrigin = TryGetOrigin(cmdProp, originMap, out var origin);
        if (!hasOrigin)
            missingOrigin = true;

        if (hasMeta && hasOrigin)
        {
            if (metaTrack != origin.track)
                hasDrift = true;
        }

        string phaseBadge = hasMeta ? $"P:{PhaseShort(metaPhase)}" : "P:?";
        string trackBadge = hasMeta ? $"T:{TrackShort(metaTrack)}" : "T:?";

        string time = "";
        if (hasMeta)
        {
            if (block) time += " [B]";
            if (inf) time += " [INF]";
            if (dur > 0f) time += $" [{dur:0.###}s]";
        }

        string originTag = hasOrigin ? $"  -> {TrackShort(origin.track)}#{origin.index}" : "  -> (missing)";
        string driftTag = hasDrift ? "  !!drift" : "";

        return $"#{compiledIndex} [{phaseBadge}][{trackBadge}]{time} {baseLine}{originTag}{driftTag}";
    }
}
#endif
