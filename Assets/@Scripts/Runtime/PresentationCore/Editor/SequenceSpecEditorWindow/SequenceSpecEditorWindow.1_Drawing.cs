#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 하는 일(Responsibilities)
/// - 툴바(DrawToolbar)
///   - SequenceSpecSO 타겟 선택(ObjectField)
///   - 검색 필드(_searchField)
///   - Ping 버튼(에셋 Ping)
///   - 단축키 도움말 팝업(ShortcutsPopup)
/// 
/// - 헤더(DrawHeader)
///   - sequenceKey 표시/편집
///   - Auto-fill(Role) 토글(EditorPrefs 저장)
///   - Default Gate 바 + RoleKey 슬롯 바 배치
///   - 시퀀스 상태 경고(키 비어있음/노드 없음)
/// 
/// - 좌측 패널(DrawNodesPanel)
///   - Node 리스트(스크롤 + ReorderableList)
///   - Add Node 버튼
///   - 멀티 삭제 모드 UI(Delete/Esc/선택 개수)
/// 
/// - 우측 패널(DrawRightPanel / DrawNodeEditor)
///   - 선택된 Node의 Steps 리스트 + Step 단축키 처리
///   - 선택된 Step 상세(게이트, 커맨드 리스트)
///   - 스크롤 보정(신규 커맨드/특정 커맨드 인덱스 점프)
///   - Compiled Preview 출력
///   - 하단 커맨드 바(+Command, Node 단위 Expand/Collapse All)
/// 
/// 여기(이 파일)를 보면 좋은 경우(When to look here)
/// - 전체 레이아웃을 바꾸고 싶을 때(컬럼 폭, 여백, 스크롤 영역, 박스 구성)
/// - 헤더에 컨트롤을 추가/이동하고 싶을 때(토글 위치, 게이트/롤 위젯 배치)
/// - 좌측/우측 패널 버튼 동작을 바꾸고 싶을 때(+Node/+Step/+Command, 멀티삭제 UX)
/// - “Expand/Collapse All” 같은 노드 범위 UI를 수정하고 싶을 때
/// 
/// 참고(Notes)
/// - 데이터/리스트 생성 로직(EnsureStepsList/EnsureCommandsList 등)은 다른 partial에 있음.
/// - 삭제/추가 같은 변경은 보통 DelayModify/Undo/ForceCompileAll 흐름을 따르므로,
///   UI에서 호출만 하고 실제 수정은 CRUD/리스트 관리 파트를 함께 확인하는 게 안전함.
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    private Vector2 _stepsScroll;

    private static readonly Color _navBgStrongPro = new Color(0.22f, 0.48f, 0.92f, 0.45f);
    private static readonly Color _navBgWeakPro = new Color(0.22f, 0.48f, 0.92f, 0.22f);
    private static readonly Color _navBgStrongLight = new Color(0.22f, 0.48f, 0.92f, 0.30f);
    private static readonly Color _navBgWeakLight = new Color(0.22f, 0.48f, 0.92f, 0.14f);

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            targetSequence = (SequenceSpecSO)EditorGUILayout.ObjectField(targetSequence, typeof(SequenceSpecSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildIfNeeded(force: true);
                LoadFoldouts();
            }

            GUILayout.FlexibleSpace();

            _search = _searchField != null ? _searchField.OnToolbarGUI(_search ?? "") : (_search ?? "");

            var pingContent = new GUIContent("Ping");
            if (GUILayout.Button(pingContent, EditorStyles.toolbarButton, GUILayout.Width(50)) &&
                targetSequence != null)
                EditorGUIUtility.PingObject(targetSequence);

            Rect helpRect = GUILayoutUtility.GetRect(
                new GUIContent("?"),
                EditorStyles.toolbarButton,
                GUILayout.Width(18)
            );

            if (GUI.Button(helpRect, new GUIContent("?", "Show shortcuts"), EditorStyles.toolbarButton))
            {
                if (_shortcutsPopupOpen)
                {
                    _shortcutsPopupOpen = false;
                }
                else
                {
                    _shortcutsPopupOpen = true;
                    PopupWindow.Show(helpRect, new ShortcutsPopup(() => ToolbarShortcutsTooltip));
                }

                GUI.FocusControl(null);
                GUIUtility.ExitGUI();
            }
        }
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("sequence", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4f);

                float old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 90f;

                EditorGUILayout.PropertyField(_sequenceKeyProp, new GUIContent("sequenceKey"),
                    GUILayout.MaxWidth(280f));
                EditorGUIUtility.labelWidth = old;

                GUILayout.FlexibleSpace();

                DrawRoleSlotsPresetBar();

                EditorGUI.BeginChangeCheck();
                _roleSlotsPresetAutoSave =
                    EditorGUILayout.ToggleLeft("AutoSave", _roleSlotsPresetAutoSave, GUILayout.Width(84f));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(PrefKey_RoleSlotsPresetAutoSave, _roleSlotsPresetAutoSave);

                    if (_roleSlotsPresetAutoSave)
                        MaybeAutoSaveActiveRoleSlotsPreset();
                }
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDefaultGateCompactBar();

                GUILayout.Space(8f);
                DrawRoleSlotsSettingsMini();
                DrawRoleKeySlotsBar();
            }

            int nodeCount = _nodesProp != null ? _nodesProp.arraySize : 0;

            if (string.IsNullOrWhiteSpace(_sequenceKeyProp?.stringValue))
                EditorGUILayout.HelpBox("sequenceKey is empty. Route resolution will fail.", MessageType.Warning);

            if (nodeCount == 0)
                EditorGUILayout.HelpBox("No nodes. Use 'Add Node'.", MessageType.Warning);
        }
    }

    private void DrawNodesPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(_nodesW)))
        using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandHeight(true)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Node", GUILayout.Height(24)))
                        AddNode();

                    GUILayout.FlexibleSpace();

                    if (_deleteMultiMode)
                    {
                        using (new EditorGUI.DisabledScope(_checkedNodes.Count == 0))
                        {
                            if (GUILayout.Button($"Delete ({_checkedNodes.Count})", GUILayout.Height(24),
                                    GUILayout.Width(70)))
                                DeleteCheckedNodesWithoutConfirm();
                        }

                        if (GUILayout.Button("Esc", GUILayout.Height(24), GUILayout.Width(60)))
                        {
                            _deleteMultiMode = false;
                            _checkedNodes.Clear();

                            if (_nodesList != null)
                                _nodesList.draggable = true;

                            GUI.FocusControl(null);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Delete", GUILayout.Height(24), GUILayout.Width(60)))
                        {
                            _deleteMultiMode = true;

                            if (_nodesList != null)
                                _nodesList.draggable = false;

                            GUI.FocusControl(null);
                        }
                    }
                }
            }

            EditorGUILayout.Space(4f);

            using (var sv = new EditorGUILayout.ScrollViewScope(_nodesScroll, GUILayout.ExpandHeight(true)))
            {
                _nodesScroll = sv.scrollPosition;
                _nodesList?.DoLayoutList();
            }
        }
    }

    private void DrawRightPanel()
    {
        if (_nodesList != null && _nodesList.index != _selectedNode)
            _selectedNode = _nodesList.index;

        using (new EditorGUILayout.VerticalScope())
        {
            if (_nodesProp == null || _nodesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Create at least one node.", MessageType.Info);
                return;
            }

            if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize)
            {
                EditorGUILayout.HelpBox("Select a node to edit.", MessageType.Info);
                return;
            }

            var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
            var stepsProp = nodeProp.FindPropertyRelative("steps");

            if (stepsProp == null || !stepsProp.isArray)
            {
                EditorGUILayout.HelpBox("NodeSpec must have List<StepSpec> steps.", MessageType.Error);
                return;
            }

            DrawNodeEditor(nodeProp, stepsProp);
        }
    }

    private void DrawNodeEditor(SerializedProperty nodeProp, SerializedProperty stepsProp)
    {
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            // =========================
            // LEFT : Steps Column
            // =========================
            using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(_stepsW), GUILayout.ExpandHeight(true)))
            {
                EnsureStepsList(nodeProp, stepsProp);

                DrawStepsScrollArea(stepsProp);

                SerializedProperty stepProp = null;
                bool hasStep = stepsProp.arraySize > 0 && _selectedStep >= 0 && _selectedStep < stepsProp.arraySize;
                if (hasStep)
                    stepProp = stepsProp.GetArrayElementAtIndex(_selectedStep);

                EditorGUILayout.Space(6f);

                if (hasStep && stepProp != null)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawStepLabelAndGateOnly(stepProp);
                    }

                    EditorGUILayout.Space(6f);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawCompiledPreview(stepProp);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a step to edit label/gate/compiled.", MessageType.Info);
                }

                HandleStepShortcuts(stepsProp);

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(34f)))
                {
                    GUILayout.Space(4f);

                    if (GUILayout.Button(" + Step  ", GUILayout.Height(28f)))
                        AddStep(stepsProp);

                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.Space(6);

            // =========================
            // RIGHT : Commands Column
            // =========================
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (stepsProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No steps. Add a step first.", MessageType.Info);
                    return;
                }

                if (_selectedStep < 0 || _selectedStep >= stepsProp.arraySize)
                {
                    EditorGUILayout.HelpBox("Select a step on the left.", MessageType.Info);
                    return;
                }

                var stepProp = stepsProp.GetArrayElementAtIndex(_selectedStep);

                using (new EditorGUI.DisabledScope(_isDraggingSteps))
                {
                    DrawStepHeaderOnly(stepProp);

                    EditorGUILayout.Space(4f);
                    DrawCommandsScrollArea(stepProp);

                    EditorGUILayout.Space(4f);
                    DrawBottomCommandBar(stepProp);
                }
            }
        }
    }

    private void DrawBottomCommandBar(SerializedProperty stepProp)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(40f)))
        {
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4f);

                var commandsProp = FindUnifiedCommandsProp(stepProp);
                bool validCommands = (commandsProp != null && commandsProp.isArray);

                using (new EditorGUI.DisabledScope(!validCommands))
                {
                    if (GUILayout.Button("+ Command", GUILayout.Width(100), GUILayout.Height(34)))
                    {
                        string commandsPath = commandsProp.propertyPath;
                        int insertAt = commandsProp.arraySize;

                        ShowCommandAddMenu(
                            commandsPath,
                            insertAt: insertAt,
                            onSingle: t => InsertSingleAt(commandsPath, insertAt, t, scroll: true),
                            onBatch: types => InsertBatchAt(commandsPath, insertAt, types, scroll: true)
                        );
                    }
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(200f)))
                {
                    GUILayout.Space(4f);
                    GUILayout.FlexibleSpace();

                    bool canToggleNodeAll = TryGetCurrentNodeStepsProp(out var _);

                    using (new EditorGUI.DisabledScope(!canToggleNodeAll))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var expandAll = new GUIContent(
                            "Expand All",
                            "Expand ALL command foldouts in this Node.\n(Affects every Step)"
                        );

                        var collapseAll = new GUIContent(
                            "Collapse All",
                            "Collapse ALL command foldouts in this Node.\n(Affects every Step)"
                        );

                        if (GUILayout.Button(expandAll, GUILayout.Width(96), GUILayout.Height(28)))
                            SetAllCommandFoldouts_ForCurrentNode(true);

                        GUILayout.Space(2f);

                        if (GUILayout.Button(collapseAll, GUILayout.Width(96), GUILayout.Height(28)))
                            SetAllCommandFoldouts_ForCurrentNode(false);
                    }

                    GUILayout.Space(2f);
                }

                GUILayout.Space(2f);
            }
        }
    }

    private static void DrawNavSelectionBg(Rect rect, bool strong)
    {
        if (Event.current.type != EventType.Repaint) return;

        bool isPro = EditorGUIUtility.isProSkin;
        Color c = isPro
            ? (strong ? _navBgStrongPro : _navBgWeakPro)
            : (strong ? _navBgStrongLight : _navBgWeakLight);

        EditorGUI.DrawRect(rect, c);
    }

    private sealed class ShortcutsPopup : PopupWindowContent
    {
        private readonly Func<string> _getText;
        private Vector2 _scroll;

        public ShortcutsPopup(Func<string> getText)
        {
            _getText = getText;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(360f, 420f);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Shortcuts", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Copy", GUILayout.Width(60)))
                {
                    EditorGUIUtility.systemCopyBuffer = _getText?.Invoke() ?? "";
                }

                if (GUILayout.Button("Close", GUILayout.Width(60)))
                {
                    editorWindow?.Close();
                }
            }

            GUILayout.Space(6f);

            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;

                var text = _getText?.Invoke() ?? "";
                EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
            }
        }
    }

    private void DrawRoleSlotsSettingsMini()
    {
        const float boxW = 130f;

        const float labelW = 40f;
        const float countFieldW = 44f;
        const float scopePopupW = 100f;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(boxW)))
        {
            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Scope", EditorStyles.miniLabel, GUILayout.Width(labelW));

                EditorGUI.BeginChangeCheck();
                var nextScope = (RoleKeyApplyScope)EditorGUILayout.EnumPopup(
                    _roleApplyScope,
                    GUILayout.Width(scopePopupW)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    _roleApplyScope = nextScope;
                    EditorPrefs.SetInt(PrefKey_RoleApplyScope, (int)_roleApplyScope);

                    MaybeAutoSaveActiveRoleSlotsPreset();

                    GUI.FocusControl(null);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Slots", EditorStyles.miniLabel, GUILayout.Width(labelW));

                EditorGUI.BeginChangeCheck();
                int nextCount = EditorGUILayout.IntField(_roleSlotCount, GUILayout.Width(countFieldW));
                if (EditorGUI.EndChangeCheck())
                {
                    _roleSlotCount = Mathf.Clamp(nextCount, RoleSlotBaseCount, RoleSlotMaxCount);
                    EditorPrefs.SetInt(PrefKey_RoleSlotCount, _roleSlotCount);
                    EnsureRoleSlotsCapacity();

                    MaybeAutoSaveActiveRoleSlotsPreset();

                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", EditorStyles.miniLabel, GUILayout.Width(labelW));

                EditorGUI.BeginChangeCheck();
                bool next = EditorGUILayout.ToggleLeft(
                    "Auto-fill(Role)",
                    _autoFillIdsOnAdd,
                    GUILayout.Width(scopePopupW)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    _autoFillIdsOnAdd = next;
                    EditorPrefs.SetBool(PrefKey_AutoFillOnAdd, _autoFillIdsOnAdd);

                    MaybeAutoSaveActiveRoleSlotsPreset();

                    GUI.FocusControl(null);
                }
            }

            GUILayout.Space(4f);
        }
    }

    private void DrawStepHeaderOnly(SerializedProperty stepProp)
    {
        EditorGUILayout.Space(1);
    }

    private void DrawStepLabelAndGateOnly(SerializedProperty stepProp)
    {
        var stepNameProp = stepProp.FindPropertyRelative("editorName");
        if (stepNameProp != null)
        {
            GUI.SetNextControlName(StepNameFieldControl);

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField($"Step Label (Jump Marker)", stepNameProp.stringValue ?? "");
            if (EditorGUI.EndChangeCheck())
                stepNameProp.stringValue = newName;

            if (_requestFocusStepNameField && Event.current.type == EventType.Repaint)
            {
                _requestFocusStepNameField = false;
                GUI.FocusControl(StepNameFieldControl);
                EditorGUIUtility.editingTextField = true;
            }
        }

        var gateProp = stepProp.FindPropertyRelative("gate");
        if (gateProp != null)
        {
            DrawGateHeaderRow_WithDefaultDropdown(gateProp);

            if (_gateFoldout)
            {
                using (new EditorGUI.IndentLevelScope(1))
                    DrawGateInline(gateProp);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("StepSpec must have GateToken gate.", MessageType.Error);
        }
    }
}
#endif