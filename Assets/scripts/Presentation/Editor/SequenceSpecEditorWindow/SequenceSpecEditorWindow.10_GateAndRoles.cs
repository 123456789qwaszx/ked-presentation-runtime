#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Step 생성 시 사용할 Gate 기본값과, 커맨드 roleKey 자동 채움(슬롯) 기능을 관리하는 파셜.
/// 
/// 핵심 목표:
/// - "새 Step 추가" 시 적용될 기본 Gate(Delay/Signal/Immediately)를 에디터 전역 프리셋처럼 유지한다.
/// - 자주 쓰는 RoleKey를 5개 슬롯으로 고정해 빠르게 주입/일괄 적용한다.
/// - 입력 UX(Enter로 커밋, 포커스 이동 시 저장)를 통해 EditorPrefs 저장 타이밍을 안정화한다.
/// 
/// 포함 기능(Responsibilities):
/// 1) Gate Defaults
/// - _defaultNewStepGate: 새 Step 생성 시 기본으로 복사되는 GateToken.
/// - DrawDefaultGateCompactBar(): 상단 헤더의 "Gate Defaults" 미니 바 UI.
///   - Type / Seconds / SignalKey 입력 제공
///   - Enter(CommitKey) 또는 포커스 이탈 시 EditorPrefs에 저장
/// - Apply Gate Preset:
///   - 헤더/스텝 디테일에서 "Preset Type + Apply"로 현재 Step gate에 프리셋을 덮어씀
///   - Apply 시: Type은 드롭다운(_applyGateType)에서, Seconds/SignalKey는 Gate Defaults에서 가져옴
///   - 단축키: ] 키로 현재 선택된 Step에 Gate Defaults 적용
/// 
/// 2) RoleKey Slots
/// - _roleKeySlots: RoleKey 텍스트 슬롯(기본 5개) + 자동 채움 대상 슬롯(_autoFillRoleSlotIndex)
/// - DrawRoleKeySlotsBar()/DrawRoleSlot():
///   - 슬롯별 RoleKey 입력
///   - "Assign Role" 버튼으로 RoleKey 일괄 적용
///   - Scope:
///     - CurrentTrack: 현재 Step의 현재 Track만
///     - AllTracks: 현재 Step의 모든 Tracks
///     - AllStepsInNode: 현재 Node의 모든 Steps의 모든 Tracks
///   - "Auto" 토글로 새 커맨드 추가 시 roleKey 자동 채움 대상 슬롯 지정
/// - LoadRoleSlotsPrefs()/SaveRoleSlotsPrefs(): 슬롯 상태를 EditorPrefs(JSON)로 저장/복원
/// 
/// 저장/상태(Policies):
/// - Gate Defaults/Role Slots는 프로젝트 데이터가 아니라 개인 에디터 환경(EditorPrefs)에 저장된다.
/// - CommitDefaultGateIfFocusLost(): 초/시그널키 필드에서 포커스가 빠질 때 저장하여 "값 입력 후 저장 누락"을 방지한다.
/// - EnsureRoleSlotsCapacity(): 슬롯 개수/리스트를 항상 유효 범위로 정규화한다.
/// 
/// 여기서 수정하면 좋은 것(When to edit here):
/// - Gate Defaults의 저장 정책(Enter/FocusLost/즉시 저장) 및 UI 배치/폭
/// - Apply Gate 프리셋 정책(어떤 필드를 덮어쓰는지, type-only 여부 등)
/// - Role 슬롯 개수/표현(UI 스타일, 버튼 라벨), 자동 채움 로직(어떤 커맨드 필드에 주입하는지)
/// - 일괄 적용 범위(현재 Step의 "모든 Track" vs "현재 Track만" vs "현재 Node의 모든 Steps")
/// </summary>

public sealed partial class SequenceSpecEditorWindow
{
    // Gate defaults
    [SerializeField] private GateToken _defaultNewStepGate = new GateToken { type = GateTokenType.Immediately };
    [SerializeField] private bool _gateFoldout = false;
    [SerializeField] private GateTokenType _applyGateType = GateTokenType.Immediately;

    private const string PrefKey_DefaultGateType = "CPS.SequenceEditor.DefaultGate.Type";
    private const string PrefKey_DefaultGateSeconds = "CPS.SequenceEditor.DefaultGate.Seconds";
    private const string PrefKey_DefaultGateSignalKey = "CPS.SequenceEditor.DefaultGate.SignalKey";
    private const string PrefKey_ApplyGateType = "CPS.SequenceEditor.ApplyGate.TypeOnly";

    private const string Ctrl_DefaultGateSeconds = "CPS.SequenceEditor.DefaultGate.SecondsField";
    private const string Ctrl_DefaultGateSignalKey = "CPS.SequenceEditor.DefaultGate.SignalKeyField";
    private string _prevFocusedControlName;

    // RoleKey slots
    [SerializeField] private bool _autoFillIdsOnAdd = true;
    [SerializeField] private int _roleSlotCount = RoleSlotBaseCount;
    [SerializeField] private List<string> _roleKeySlots = new();
    [SerializeField] private int _autoFillRoleSlotIndex = 0;

    private const string PrefKey_AutoFillOnAdd = "CPS.SequenceEditor.AutoFillIdsOnAdd";
    private const string PrefKey_RoleSlotCount = "CPS.SequenceEditor.RoleSlots.Count";
    private const string PrefKey_RoleSlotsJson = "CPS.SequenceEditor.RoleSlots.Json";
    private const string PrefKey_AutoFillRoleSlotIndex = "CPS.SequenceEditor.AutoFillRoleSlotIndex";
    
    private enum RoleKeyApplyScope
    {
        CurrentTrack = 0,
        AllTracks = 1,
        AllStepsInNode = 2,
    }

    [SerializeField] private RoleKeyApplyScope _roleApplyScope = RoleKeyApplyScope.AllTracks;

    private const string PrefKey_RoleApplyScope = "CPS.SequenceEditor.RoleApplyScope";

    [Serializable]
    private sealed class RoleSlotsBox
    {
        public List<string> slots = new();
    }

    private static GUIStyle _roleKeyFieldStyle;

    private static GUIStyle RoleKeyFieldStyle
    {
        get
        {
            if (_roleKeyFieldStyle != null) return _roleKeyFieldStyle;

            _roleKeyFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 16,
                fixedHeight = 24f
            };

            _roleKeyFieldStyle.padding = new RectOffset(4, 2, 2, 2);

            return _roleKeyFieldStyle;
        }
    }

    private void LoadPreferences()
    {
        _roleApplyScope = (RoleKeyApplyScope)EditorPrefs.GetInt(PrefKey_RoleApplyScope, (int)_roleApplyScope);
        _autoFillIdsOnAdd = EditorPrefs.GetBool(PrefKey_AutoFillOnAdd, _autoFillIdsOnAdd);
        _autoFillRoleSlotIndex = EditorPrefs.GetInt(PrefKey_AutoFillRoleSlotIndex, 0);
        _autoFillRoleSlotIndex = Mathf.Clamp(_autoFillRoleSlotIndex, 0, RoleSlotMaxCount - 1);

        LoadDefaultGatePrefs();
        _applyGateType = (GateTokenType)EditorPrefs.GetInt(PrefKey_ApplyGateType, (int)_applyGateType);

        LoadRoleSlotsPrefs();
        EnsureRoleSlotsCapacity();
        LoadRoleSlotsPresetSystemPrefs();
    }

    private void LoadDefaultGatePrefs()
    {
        _defaultNewStepGate.type =
            (GateTokenType)EditorPrefs.GetInt(PrefKey_DefaultGateType, (int)_defaultNewStepGate.type);

        _defaultNewStepGate.seconds =
            EditorPrefs.GetFloat(PrefKey_DefaultGateSeconds, _defaultNewStepGate.seconds);

        _defaultNewStepGate.signalKey =
            EditorPrefs.GetString(PrefKey_DefaultGateSignalKey, _defaultNewStepGate.signalKey ?? "");

        _defaultNewStepGate.signalKey = _defaultNewStepGate.signalKey ?? "";
    }

    private void SaveDefaultGatePrefs()
    {
        EditorPrefs.SetInt(PrefKey_DefaultGateType, (int)_defaultNewStepGate.type);
        EditorPrefs.SetFloat(PrefKey_DefaultGateSeconds, _defaultNewStepGate.seconds);
        EditorPrefs.SetString(PrefKey_DefaultGateSignalKey, _defaultNewStepGate.signalKey ?? "");
    }

    private void DrawDefaultGateCompactBar()
    {
        const float boxW = 180f;
        const float labelW = 80f;
        const float typeW = 90f;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(boxW)))
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20f)))
            {
                GUILayout.Space(2f);

                var defaultsLabel = new GUIContent(
                    "Gate Defaults",
                    "Default gate used when creating NEW steps.\n" +
                    "- Saved in EditorPrefs\n" +
                    "- Affects auto-fill on Add Step\n" +
                    "- Does NOT change existing steps\n" +
                    "- Press ] to apply to current step"
                );

                EditorGUILayout.LabelField(defaultsLabel, EditorStyles.miniBoldLabel, GUILayout.Width(labelW));

                EditorGUI.BeginChangeCheck();
                var nextType =
                    (GateTokenType)EditorGUILayout.EnumPopup(_defaultNewStepGate.type, GUILayout.Width(typeW));
                if (EditorGUI.EndChangeCheck())
                {
                    _defaultNewStepGate.type = nextType;
                    SaveDefaultGatePrefs();
                    GUI.FocusControl(null);
                }
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20f)))
            {
                GUILayout.Space(2f);

                EditorGUILayout.LabelField("Seconds", EditorStyles.miniLabel, GUILayout.Width(labelW));

                GUI.SetNextControlName(Ctrl_DefaultGateSeconds);

                EditorGUI.BeginChangeCheck();
                float nextSec = EditorGUILayout.FloatField(_defaultNewStepGate.seconds);
                if (EditorGUI.EndChangeCheck())
                {
                    _defaultNewStepGate.seconds = nextSec;

                    if (IsCommitKey(Event.current))
                        SaveDefaultGatePrefs();
                }
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20f)))
            {
                GUILayout.Space(2f);

                EditorGUILayout.LabelField("Signal Key", EditorStyles.miniLabel, GUILayout.Width(labelW));

                GUI.SetNextControlName(Ctrl_DefaultGateSignalKey);

                EditorGUI.BeginChangeCheck();
                string nextKey = EditorGUILayout.TextField(_defaultNewStepGate.signalKey ?? "");
                if (EditorGUI.EndChangeCheck())
                {
                    _defaultNewStepGate.signalKey = nextKey ?? "";

                    if (IsCommitKey(Event.current))
                        SaveDefaultGatePrefs();
                }
            }
        }

        CommitDefaultGateIfFocusLost();
    }

    private void DrawRoleKeySlotsBar()
    {
        EnsureRoleSlotsCapacity();
        for (int i = 0; i < _roleSlotCount; i++)
        {
            DrawRoleSlot(i);
            GUILayout.Space(4f);
        }
    }

    private void DrawRoleSlot(int i)
    {
        const float fieldW = 87f;
        const float btnH = 18f;

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(fieldW)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!CanApplyRoleToCurrentStep(i)))
                {
                    string scopeHint = _roleApplyScope switch
                    {
                        RoleKeyApplyScope.CurrentTrack => "Set this RoleKey on all commands in the current step (current track only).",
                        RoleKeyApplyScope.AllTracks => "Set this RoleKey on all commands in the current step (all tracks).",
                        RoleKeyApplyScope.AllStepsInNode => "Set this RoleKey on all commands in all steps of the current node (all tracks).",
                        _ => ""
                    };

                    var content = new GUIContent("Set RoleKey", scopeHint);

                    if (GUILayout.Button(content, GUILayout.Width(76), GUILayout.Height(btnH)))
                        ApplyRoleToCurrentStep(i);
                }

                GUILayout.Label($"#{i + 1}", EditorStyles.miniLabel, GUILayout.Width(15));
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string cur = _roleKeySlots[i] ?? "";
                EditorGUI.BeginChangeCheck();
                string next = EditorGUILayout.TextField(
                    cur,
                    RoleKeyFieldStyle,
                    GUILayout.Width(fieldW),
                    GUILayout.Height(24f)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    _roleKeySlots[i] = next ?? "";
                    SaveRoleSlotsPrefs();
                }
            }

            float autoW = fieldW - 2f;

            using (new EditorGUI.DisabledScope(!_autoFillIdsOnAdd))
            {
                bool isAuto = (_autoFillRoleSlotIndex == i);
                string label = isAuto ? "✓ Auto    " : "   Auto   ";

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(9f);
                    EditorGUI.BeginChangeCheck();

                    bool nextAuto = GUILayout.Toggle(
                        isAuto,
                        label,
                        EditorStyles.miniButton,
                        GUILayout.Width(autoW),
                        GUILayout.Height(20f)
                    );

                    if (EditorGUI.EndChangeCheck() && nextAuto && !isAuto)
                    {
                        _autoFillRoleSlotIndex = i;
                        EditorPrefs.SetInt(PrefKey_AutoFillRoleSlotIndex, _autoFillRoleSlotIndex);
                        GUI.FocusControl(null);
                    }
                }
            }
        }
    }

    private bool HasRoleSlot(int slotIndex)
    {
        EnsureRoleSlotsCapacity();
        if (slotIndex < 0 || slotIndex >= _roleSlotCount) return false;
        if (slotIndex < 0 || slotIndex >= _roleKeySlots.Count) return false;
        return !string.IsNullOrWhiteSpace(_roleKeySlots[slotIndex]);
    }

    private bool CanApplyRoleToCurrentStep(int slotIndex)
    {
        if (!HasRoleSlot(slotIndex)) return false;

        if (_nodesProp == null) return false;
        if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return false;

        var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
        var stepsProp = nodeProp.FindPropertyRelative("steps");
        if (stepsProp == null || !stepsProp.isArray) return false;

        // AllStepsInNode: 노드에 최소 1개 스텝이 있으면 OK
        if (_roleApplyScope == RoleKeyApplyScope.AllStepsInNode)
        {
            return stepsProp.arraySize > 0;
        }

        // CurrentTrack / AllTracks: 현재 Step 선택이 유효해야 함
        if (_selectedStep < 0 || _selectedStep >= stepsProp.arraySize) return false;

        var stepProp = stepsProp.GetArrayElementAtIndex(_selectedStep);

        if (_roleApplyScope == RoleKeyApplyScope.CurrentTrack)
        {
            var trackListProp = FindActiveTrackList(stepProp);
            return trackListProp != null && trackListProp.isArray && trackListProp.arraySize > 0;
        }

        // AllTracks
        var tracksProp = stepProp.FindPropertyRelative("tracks");
        if (tracksProp == null) return false;

        foreach (var name in TrackFieldNames)
        {
            var lp = tracksProp.FindPropertyRelative(name);
            if (lp != null && lp.isArray && lp.arraySize > 0)
                return true;
        }

        return false;
    }

    private void ApplyRoleToCurrentStep(int slotIndex)
    {
        if (!HasRoleSlot(slotIndex)) return;

        int nodeIndex = _selectedNode;
        int stepIndex = _selectedStep;

        string roleKey = _roleKeySlots[slotIndex] ?? string.Empty;
        RoleKeyApplyScope scope = _roleApplyScope; // 캡처

        DelayModify("Apply RoleKey", so =>
        {
            var nodes = so.FindProperty("nodes");
            if (nodes == null || !nodes.isArray) return;
            if (nodeIndex < 0 || nodeIndex >= nodes.arraySize) return;

            var nodeProp = nodes.GetArrayElementAtIndex(nodeIndex);
            var stepsProp = nodeProp.FindPropertyRelative("steps");
            if (stepsProp == null || !stepsProp.isArray) return;

            if (scope == RoleKeyApplyScope.AllStepsInNode)
            {
                // 현재 Node의 모든 Steps에 적용
                for (int si = 0; si < stepsProp.arraySize; si++)
                {
                    var stepProp = stepsProp.GetArrayElementAtIndex(si);
                    var tracksProp = stepProp.FindPropertyRelative("tracks");
                    if (tracksProp == null) continue;

                    foreach (var name in TrackFieldNames)
                    {
                        var lp = tracksProp.FindPropertyRelative(name);
                        ApplyRoleToList(lp, roleKey);
                    }
                }
                return;
            }

            // CurrentTrack / AllTracks: 현재 Step만
            if (stepIndex < 0 || stepIndex >= stepsProp.arraySize) return;

            var currentStepProp = stepsProp.GetArrayElementAtIndex(stepIndex);

            if (scope == RoleKeyApplyScope.CurrentTrack)
            {
                var trackList = FindActiveTrackList(currentStepProp);
                ApplyRoleToList(trackList, roleKey);
                return;
            }

            // AllTracks
            var tracksInStepProp = currentStepProp.FindPropertyRelative("tracks");
            if (tracksInStepProp == null) return;

            foreach (var name in TrackFieldNames)
            {
                var lp = tracksInStepProp.FindPropertyRelative(name);
                ApplyRoleToList(lp, roleKey);
            }
        });
    }

    private void ApplyRoleToList(SerializedProperty listProp, string roleKey)
    {
        if (listProp == null || !listProp.isArray)
            return;

        if (string.IsNullOrWhiteSpace(roleKey))
            return;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var cmdProp = listProp.GetArrayElementAtIndex(i);
            if (cmdProp == null) continue;
            if (cmdProp.propertyType != SerializedPropertyType.ManagedReference) continue;

            var roleProp = cmdProp.FindPropertyRelative("roleKey");
            if (roleProp != null && roleProp.propertyType == SerializedPropertyType.String)
                roleProp.stringValue = roleKey;
        }
    }

    private void EnsureRoleSlotsCapacity()
    {
        if (_roleKeySlots == null)
            _roleKeySlots = new List<string>();

        // 표시 개수는 유저가 조절 (최소/최대)
        _roleSlotCount = Mathf.Clamp(_roleSlotCount, RoleSlotBaseCount, RoleSlotMaxCount);

        // 실제 저장 리스트는 MaxCount까지 유지
        while (_roleKeySlots.Count < RoleSlotMaxCount)
            _roleKeySlots.Add("");

        if (_roleKeySlots.Count > RoleSlotMaxCount)
            _roleKeySlots.RemoveRange(RoleSlotMaxCount, _roleKeySlots.Count - RoleSlotMaxCount);

        // Auto 슬롯도 표시 범위 안으로
        _autoFillRoleSlotIndex = Mathf.Clamp(_autoFillRoleSlotIndex, 0, _roleSlotCount - 1);
    }

    private void LoadRoleSlotsPrefs()
    {
        _roleSlotCount = EditorPrefs.GetInt(PrefKey_RoleSlotCount, RoleSlotBaseCount);

        string json = EditorPrefs.GetString(PrefKey_RoleSlotsJson, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var box = JsonUtility.FromJson<RoleSlotsBox>(json);
                _roleKeySlots = box?.slots ?? new List<string>();
            }
            catch
            {
                _roleKeySlots = new List<string>();
            }
        }
        else
        {
            _roleKeySlots = new List<string>();
        }

        EnsureRoleSlotsCapacity();
    }

    private void SaveRoleSlotsPrefs()
    {
        EnsureRoleSlotsCapacity();

        EditorPrefs.SetInt(PrefKey_RoleSlotCount, _roleSlotCount);

        var box = new RoleSlotsBox { slots = _roleKeySlots ?? new List<string>() };
        string json = JsonUtility.ToJson(box);
        EditorPrefs.SetString(PrefKey_RoleSlotsJson, json);

        // AutoSave preset (overwrite active preset)
        MaybeAutoSaveActiveRoleSlotsPreset();
    }
    
    private void MaybeAutoSaveActiveRoleSlotsPreset()
    {
        if (!_roleSlotsPresetAutoSave) return;
        if (string.IsNullOrWhiteSpace(_roleSlotsPresetActive)) return;

        // Default는 autosave 허용해도 되고, 싫으면 여기서 막기
        // if (string.Equals(_roleSlotsPresetActive, "Default", StringComparison.OrdinalIgnoreCase)) return;

        SaveRoleSlotsPreset(_roleSlotsPresetActive);
    }

    private static bool IsCommitKey(Event e)
    {
        if (e == null) return false;
        if (e.type != EventType.KeyDown) return false;
        return e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter;
    }

    private void CommitDefaultGateIfFocusLost()
    {
        string now = GUI.GetNameOfFocusedControl();

        if (_prevFocusedControlName == null)
            _prevFocusedControlName = "";

        bool wasGateField =
            _prevFocusedControlName == Ctrl_DefaultGateSeconds ||
            _prevFocusedControlName == Ctrl_DefaultGateSignalKey;

        bool lostFocus = wasGateField && now != _prevFocusedControlName;

        if (lostFocus)
            SaveDefaultGatePrefs();

        _prevFocusedControlName = now;
    }

    private void DrawGateInline(SerializedProperty gateProp)
    {
        var typeProp = gateProp.FindPropertyRelative("type");
        var secProp = gateProp.FindPropertyRelative("seconds");
        var keyProp = gateProp.FindPropertyRelative("signalKey");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Gate(after this step)", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(typeProp);

            EditorGUILayout.PropertyField(secProp, new GUIContent("Seconds"));
            EditorGUILayout.PropertyField(keyProp, new GUIContent("SignalKey"));
        }
    }

    private void DrawGateHeaderRow_WithDefaultDropdown(SerializedProperty gateProp)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            _gateFoldout = EditorGUILayout.Foldout(
                _gateFoldout,
                "Gate",
                true
            );

            GUILayout.FlexibleSpace();

            DrawDefaultGateTypeDropdown_Compact(gateProp);
        }
    }

    private void DrawDefaultGateTypeDropdown_Compact(SerializedProperty currentStepGateProp)
    {
        const float labelW = 52f;
        const float typeW = 90f;
        const float applyW = 48f;

        using (new EditorGUILayout.HorizontalScope(GUILayout.Width(labelW + typeW + applyW + 10f)))
        {
            GUILayout.Space(2f);

            var presetTypeLabel = new GUIContent(
                "defaults/",
                "Type to use when applying the Gate preset.\n" +
                "- Does NOT change this step until you click Apply or press ]\n" +
                "- Delay/Signal fields come from Gate Defaults"
            );

            EditorGUILayout.LabelField(presetTypeLabel, GUILayout.Width(labelW));

            EditorGUI.BeginChangeCheck();
            var nextType = (GateTokenType)EditorGUILayout.EnumPopup(
                _applyGateType,
                GUILayout.Width(typeW)
            );
            if (EditorGUI.EndChangeCheck())
            {
                _applyGateType = nextType;
                EditorPrefs.SetInt(PrefKey_ApplyGateType, (int)_applyGateType);
                GUI.FocusControl(null);
            }

            using (new EditorGUI.DisabledScope(currentStepGateProp == null))
            {
                var applyContent = new GUIContent(
                    "Apply",
                    "Apply Gate Defaults to current step (shortcut: ])"
                );

                if (GUILayout.Button(applyContent, GUILayout.Width(applyW)))
                {
                    ApplyDefaultGateToCurrentStep(currentStepGateProp);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void ApplyDefaultGateToCurrentStep(SerializedProperty stepGateProp)
    {
        if (stepGateProp == null) return;

        string gatePath = stepGateProp.propertyPath;

        DelayModify("Apply Gate Preset", so =>
        {
            var gate = so.FindProperty(gatePath);
            if (gate == null) return;

            var g = _defaultNewStepGate;
            g.type = _applyGateType;

            g = SanitizeGate(g);
            WriteGateToSerializedProperty(gate, g);
        });
    }

    private static void WriteGateToSerializedProperty(SerializedProperty gateProp, GateToken g)
    {
        if (gateProp == null) return;

        var typeProp = gateProp.FindPropertyRelative("type");
        var secProp = gateProp.FindPropertyRelative("seconds");
        var keyProp = gateProp.FindPropertyRelative("signalKey");

        if (typeProp != null) typeProp.enumValueIndex = (int)g.type;

        if (secProp != null) secProp.floatValue = g.seconds;

        if (keyProp != null) keyProp.stringValue = g.signalKey ?? "";
    }
    
    /// <summary>
    /// ] 키 단축키: 현재 선택된 Step에 Gate Defaults 적용
    /// </summary>
    private void HandleApplyGateDefaultsShortcut()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;
        
        // ] 키 체크
        if (e.keyCode != KeyCode.RightBracket) return;
        
        // 현재 Step이 선택되어 있는지 확인
        if (_nodesProp == null) return;
        if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return;
        
        var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
        var stepsProp = nodeProp.FindPropertyRelative("steps");
        if (stepsProp == null || !stepsProp.isArray) return;
        if (_selectedStep < 0 || _selectedStep >= stepsProp.arraySize) return;
        
        var stepProp = stepsProp.GetArrayElementAtIndex(_selectedStep);
        var gateProp = stepProp.FindPropertyRelative("gate");
        
        if (gateProp != null)
        {
            ApplyDefaultGateToCurrentStep(gateProp);
            e.Use();
            Repaint();
        }
    }
}
#endif