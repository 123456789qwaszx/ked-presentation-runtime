#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keyboard-driven editing layer for the SequenceSpec Editor.
/// 
/// 역할
/// - "마우스 없이도" 편집을 빠르게 하기 위한 단축키/키보드 액션을 한 곳에 모아둔 파셜.
/// - Commands / Steps 리스트에 대한 공통 키 조작을 처리하고, 그 결과를 실제 CRUD(추가/삭제/복제/붙여넣기)로 연결한다.
/// - 특히 아래 두 축을 담당한다:
///   1) 글로벌 키(현재 선택된 스텝의 커맨드 삭제)
///   2) 리스트 컨텍스트별 키(Commands/Steps에서 Enter/Space/Delete/Ctrl+C/V/D/X 등)
/// 
/// 이 파일을 보면 좋을 때 (무엇을 건드리고 싶을 때)
/// - Delete 키 동작이 이상하다 / 특정 상황에서 삭제가 안 된다
///   - HandleGlobalCommandDeleteShortcut(): 모디파이어 없는 Delete로 "현재 Step의 현재 선택 커맨드" 삭제
///   - HandleCommandShortcuts(): Commands 리스트 내 Delete 처리 + 다른 단축키
///   - DeleteSelectedStep(): Steps 리스트 내 삭제(Undo/리빌드/컴파일 갱신 포함)
/// 
/// - Enter(커맨드 생성) / Space(접기/펼치기) / Shift+Space(전체 접기/펼치기) 동작을 바꾸고 싶다
///   - HandleCommandShortcuts()의 Return/Space 분기:
///     - Return/Space: +Command 메뉴 열기 (TryClickPlusCommand_ByMenu)
///   - HandleCommandShortcuts()의 KeypadEnter 분기:
///     - KeypadEnter: 단일 커맨드 foldout 토글
///     - Shift+KeypadEnter: "현재 리스트 전체" foldout을 일괄 토글
///   - foldout 저장/복구는 FoldoutState 파셜(Load/SaveFoldouts, SetAllCommandFoldouts 등)과 연동됨
/// 
/// - 복사/잘라내기/붙여넣기/복제(Ctrl+C/X/V/D) 정책을 바꾸고 싶다
///   - Commands:
///     - Ctrl+C: CopyCommandToClipboard()
///     - Ctrl+X: Copy 후 Delete
///     - Ctrl+V: 클립보드 JSON → CreateCommandFromJson()로 생성 후 삽입
///     - Ctrl+D: Copy→Paste로 "한 칸 아래 복제"
///   - Steps:
///     - Ctrl+C: CopyStepToClipboard(step)
///     - Ctrl+V: CreateStepFromJson(json)로 생성 후 해당 노드 steps에 삽입
///     - Ctrl+D: CloneStepDeep()로 복제
///     - Backspace: (커맨드 선택 중이 아닐 때) Step 삭제
///   - Nodes:
///     - Ctrl+C: CopyNodeToClipboard(node)
///     - Ctrl+V: CreateNodeFromJson(json)로 생성 후 삽입
///     - Ctrl+D: CloneNodeDeep()로 복제
/// 
/// - "선택 상태/스크롤/리스트 리빌드 타이밍"이 어색할 때
///   - _commandsList / _commandsPropPath 를 null로 만들어 ReorderableList를 재생성시키는 패턴이 많다.
///     (foldout 상태, selection, scroll 동기화를 안전하게 하기 위한 전략)
///   - DelayModify() 기반 비동기 수정 이후, 리스트/선택을 어떻게 갱신하는지 이 파일에서 확인하면 된다.
/// 
/// 참고: 이 파셜은 "키 입력을 CRUD 호출로 라우팅"하는 역할이고,
/// 실제 데이터 변경(Undo/SerializedObject 갱신/ForceCompileAll/SetDirty)은
/// DelayModify/DeleteCommandAt/DeleteArrayElementByPath 같은 CRUD 파셜 쪽이 권위자다.
/// </summary>

public sealed partial class SequenceSpecEditorWindow
{
    private void HandleGlobalCommandDeleteShortcut()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        bool mod = e.control || e.command;
        if (mod) return;
        if (e.keyCode != KeyCode.Delete) return;

        if (_nodesProp == null) return;
        if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return;

        var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
        var stepsProp = nodeProp.FindPropertyRelative("steps");
        if (stepsProp == null || !stepsProp.isArray) return;
        if (_selectedStep < 0 || _selectedStep >= stepsProp.arraySize) return;

        var stepProp = stepsProp.GetArrayElementAtIndex(_selectedStep);
        var commandsProp = FindUnifiedCommandsProp(stepProp);
        if (commandsProp == null || !commandsProp.isArray) return;

        if (_commandsList == null) return;

        int idx = _commandsList.index;
        if (idx < 0 || idx >= commandsProp.arraySize) return;

        string commandsPath = commandsProp.propertyPath;

        DeleteCommandAt(commandsPath, idx, after: () =>
        {
            _commandsList = null;
            _commandsPropPath = null;
        });

        e.Use();
    }

    private void HandleCommandShortcuts(SerializedProperty commandsProp)
    {
        if (commandsProp == null || !commandsProp.isArray) return;
        if (_commandsList == null) return;

        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        bool mod = e.control || e.command;

        // Enter/Space: 커맨드 추가 메뉴 열기
        if (!mod && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space))
        {
            if (_navColumn == NavColumn.Commands)
            {
                if (TryClickPlusCommand_ByMenu(commandsProp))
                {
                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                    return;
                }
            }
        }

        // KeypadEnter: 펼치기/접기 토글
        if (!mod && e.keyCode == KeyCode.KeypadEnter)
        {
            string path = commandsProp.propertyPath;
            var map = GetFoldoutMap(path);

            int keepIndex = (_commandsList != null) ? _commandsList.index : -1;
            if (keepIndex >= 0) _pendingCommandIndex = keepIndex;

            // Shift+KeypadEnter: 전체 펼치기/접기
            if (e.shift)
            {
                bool anyCollapsed = false;

                for (int i = 0; i < commandsProp.arraySize; i++)
                {
                    var el = commandsProp.GetArrayElementAtIndex(i);
                    if (el == null) continue;
                    if (el.propertyType != SerializedPropertyType.ManagedReference) continue;

                    long id = el.managedReferenceId;
                    if (id == 0) continue;

                    bool expanded = false;
                    if (map != null && map.TryGetValue(id, out bool saved))
                        expanded = saved;

                    if (!expanded)
                    {
                        anyCollapsed = true;
                        break;
                    }
                }

                bool nextAll = anyCollapsed;
                SetAllCommandFoldouts(commandsProp, expanded: nextAll);
                SaveFoldouts();

                _commandsList = null;
                _commandsPropPath = null;

                GUI.FocusControl(null);
                GUI.changed = true;
                e.Use();
                Repaint();
                GUIUtility.ExitGUI();
                return;
            }

            // KeypadEnter: 단일 아이템 펼치기/접기
            int idx = keepIndex;
            if (idx >= 0 && idx < commandsProp.arraySize)
            {
                var el = commandsProp.GetArrayElementAtIndex(idx);
                if (el != null && el.propertyType == SerializedPropertyType.ManagedReference)
                {
                    long id = el.managedReferenceId;

                    bool cur = false;
                    if (map != null && id != 0 && map.TryGetValue(id, out bool saved))
                        cur = saved;

                    bool next = !cur;

                    if (map != null && id != 0)
                        map[id] = next;

                    SaveFoldouts();

                    _commandsList = null;
                    _commandsPropPath = null;

                    GUI.changed = true;
                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                    GUIUtility.ExitGUI();
                    return;
                }
            }
        }

        if (!mod && e.keyCode == KeyCode.Delete)
        {
            int idx = _commandsList.index;
            if (idx >= 0 && idx < commandsProp.arraySize)
            {
                DeleteCommandAt(commandsProp.propertyPath, idx, after: () =>
                {
                    _commandsList = null;
                    _commandsPropPath = null;
                });
                e.Use();
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.X)
        {
            int idx = _commandsList.index;
            if (idx >= 0 && idx < commandsProp.arraySize)
            {
                var el = commandsProp.GetArrayElementAtIndex(idx);
                if (el != null && el.propertyType == SerializedPropertyType.ManagedReference)
                {
                    CopyCommandToClipboard(el.managedReferenceValue as CommandSpecBase);
                    DeleteCommandAt(commandsProp.propertyPath, idx, after: () =>
                    {
                        _commandsList = null;
                        _commandsPropPath = null;
                    });
                    e.Use();
                }
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.C)
        {
            int idx = _commandsList.index;
            if (idx >= 0 && idx < commandsProp.arraySize)
            {
                var el = commandsProp.GetArrayElementAtIndex(idx);
                if (el != null && el.propertyType == SerializedPropertyType.ManagedReference)
                {
                    CopyCommandToClipboard(el.managedReferenceValue as CommandSpecBase);
                    e.Use();
                }
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.V)
        {
            if (!TryGetClipboardJson(out string json))
                return;

            int insertAt = commandsProp.arraySize;
            int sel = _commandsList.index;
            if (sel >= 0 && sel < commandsProp.arraySize)
                insertAt = sel + 1;

            InsertCommandFactoryAt(
                commandsProp.propertyPath,
                insertAt,
                factory: () => CreateCommandFromJson(json),
                scroll: false,
                expandNew: false
            );

            e.Use();
            return;
        }

        if (mod && e.keyCode == KeyCode.D)
        {
            int idx = _commandsList.index;
            if (idx >= 0 && idx < commandsProp.arraySize)
            {
                var el = commandsProp.GetArrayElementAtIndex(idx);
                if (el != null && el.propertyType == SerializedPropertyType.ManagedReference)
                {
                    CopyCommandToClipboard(el.managedReferenceValue as CommandSpecBase);

                    if (TryGetClipboardJson(out string json))
                    {
                        int insertAt = idx + 1;
                        string propPath = commandsProp.propertyPath;

                        DelayModify("Duplicate Command", so =>
                        {
                            var fresh = so.FindProperty(propPath);
                            if (fresh == null || !fresh.isArray) return;

                            insertAt = Mathf.Clamp(insertAt, 0, fresh.arraySize);
                            fresh.InsertArrayElementAtIndex(insertAt);

                            var pastedEl = fresh.GetArrayElementAtIndex(insertAt);
                            pastedEl.managedReferenceValue = CreateCommandFromJson(json);

                            _pendingCommandIndex = insertAt;
                            _commandsList = null;
                            _commandsPropPath = null;
                        });

                        e.Use();
                    }
                }
            }

            return;
        }
    }

    private void HandleStepShortcuts(SerializedProperty stepsProp)
    {
        if (stepsProp == null || !stepsProp.isArray) return;
        if (_stepsList == null) return;

        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        bool mod = e.control || e.command;

        if (!mod && e.keyCode == KeyCode.Backspace)
        {
            if (_commandsList != null && _commandsList.index >= 0)
                return;

            int idx = _stepsList.index;
            if (idx >= 0 && idx < stepsProp.arraySize)
            {
                DeleteSelectedStep(stepsProp);
                e.Use();
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.C)
        {
            if (_commandsList != null && _commandsList.index >= 0)
                return;

            int idx = _stepsList.index;
            if (idx >= 0 && idx < stepsProp.arraySize)
            {
                var step = targetSequence.nodes[_selectedNode].steps[idx];
                CopyStepToClipboard(step);
                e.Use();
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.D)
        {
            if (_commandsList != null && _commandsList.index >= 0)
                return;

            int idx = _stepsList.index;
            if (idx >= 0 && idx < stepsProp.arraySize)
            {
                int nodeIndex = _selectedNode;
                int srcIndex = idx;
                int insertAt = idx + 1;

                DelayModify("Duplicate Step", so =>
                {
                    var seq = (SequenceSpecSO)so.targetObject;
                    if (seq == null) return;
                    if (nodeIndex < 0 || nodeIndex >= seq.nodes.Count) return;

                    var node = seq.nodes[nodeIndex];
                    node.steps ??= new System.Collections.Generic.List<StepSpec>();
                    if (srcIndex < 0 || srcIndex >= node.steps.Count) return;

                    insertAt = Mathf.Clamp(insertAt, 0, node.steps.Count);
                    node.steps.Insert(insertAt, CloneStepDeep(node.steps[srcIndex]));

                    _selectedStep = insertAt;
                    _stepsList = null;
                    _commandsList = null;
                });

                e.Use();
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.V)
        {
            if (_commandsList != null && _commandsList.index >= 0)
                return;

            if (!TryGetStepClipboardJson(out string json))
                return;

            int insertAt = stepsProp.arraySize;
            int sel = _stepsList.index;
            if (sel >= 0 && sel < stepsProp.arraySize)
                insertAt = sel + 1;

            int nodeIndex = _selectedNode;

            DelayModify("Paste Step", so =>
            {
                var seq = (SequenceSpecSO)so.targetObject;
                if (seq == null) return;
                if (nodeIndex < 0 || nodeIndex >= seq.nodes.Count) return;

                var pasted = CreateStepFromJson(json);
                if (pasted == null) return;

                seq.nodes[nodeIndex].steps.Insert(Mathf.Clamp(insertAt, 0, seq.nodes[nodeIndex].steps.Count), pasted);

                _selectedStep = insertAt;
                _stepsList = null;
                _commandsList = null;
            });

            e.Use();
            return;
        }
    }

    private void HandleNodeShortcuts(SerializedProperty nodesProp)
    {
        if (nodesProp == null || !nodesProp.isArray) return;
        if (_nodesList == null) return;

        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        bool mod = e.control || e.command;

        if (mod && e.keyCode == KeyCode.C)
        {
            int idx = _nodesList.index;
            if (idx >= 0 && idx < nodesProp.arraySize)
            {
                var node = targetSequence.nodes[idx];
                CopyNodeToClipboard(node);
                e.Use();
            }

            return;
        }

        if (mod && e.keyCode == KeyCode.V)
        {
            if (!TryGetNodeClipboardJson(out string json))
                return;

            int insertAt = nodesProp.arraySize;
            int sel = _nodesList.index;
            if (sel >= 0 && sel < nodesProp.arraySize)
                insertAt = sel + 1;

            DelayModify("Paste Node", so =>
            {
                var seq = (SequenceSpecSO)so.targetObject;
                if (seq == null) return;

                var pasted = CreateNodeFromJson(json);
                if (pasted == null) return;

                seq.nodes ??= new System.Collections.Generic.List<NodeSpec>();
                insertAt = Mathf.Clamp(insertAt, 0, seq.nodes.Count);
                seq.nodes.Insert(insertAt, pasted);

                _selectedNode = insertAt;
                _selectedStep = -1;
                _nodesList = null;
                _stepsList = null;
                _commandsList = null;

                ForceCompileAll();
            }, forceRebuild: true);

            e.Use();
            return;
        }

        if (mod && e.keyCode == KeyCode.D)
        {
            int idx = _nodesList.index;
            if (idx >= 0 && idx < nodesProp.arraySize)
            {
                int srcIndex = idx;
                int insertAt = idx + 1;

                DelayModify("Duplicate Node", so =>
                {
                    var seq = (SequenceSpecSO)so.targetObject;
                    if (seq == null) return;
                    if (srcIndex < 0 || srcIndex >= seq.nodes.Count) return;

                    seq.nodes ??= new System.Collections.Generic.List<NodeSpec>();
                    insertAt = Mathf.Clamp(insertAt, 0, seq.nodes.Count);
                    seq.nodes.Insert(insertAt, CloneNodeDeep(seq.nodes[srcIndex]));

                    _selectedNode = insertAt;
                    _selectedStep = -1;
                    _nodesList = null;
                    _stepsList = null;
                    _commandsList = null;

                    ForceCompileAll();
                }, forceRebuild: true);

                e.Use();
            }

            return;
        }
    }

    private void DeleteSelectedStep(SerializedProperty stepsProp)
    {
        if (stepsProp == null || !stepsProp.isArray) return;
        if (_stepsList == null) return;

        int idx = _stepsList.index;
        if (idx < 0 || idx >= stepsProp.arraySize) return;

        string stepsPath = stepsProp.propertyPath;

        DeleteArrayElementByPath("Delete Step", stepsPath, idx, after: () =>
        {
            _selectedStep = Mathf.Clamp(idx - 1, 0, stepsProp.arraySize - 2);
            _stepsList = null;
            _commandsList = null;

            ForceCompileAll();
        });
    }

    private bool TryClickPlusCommand_ByMenu(SerializedProperty commandsProp)
    {
        if (commandsProp == null || !commandsProp.isArray) return false;
        if (!IsSerializeReferenceCommandList(commandsProp)) return false;

        string commandsPath = commandsProp.propertyPath;

        int insertAt = commandsProp.arraySize;

        if (_commandsList != null)
        {
            int sel = _commandsList.index;
            if (sel >= 0 && sel < commandsProp.arraySize)
                insertAt = sel + 1;
        }

        ShowCommandAddMenu(
            commandsPath,
            insertAt: insertAt,
            onSingle: t => InsertSingleAt(commandsPath, insertAt, t, scroll: true),
            onBatch: types => InsertBatchAt(commandsPath, insertAt, types, scroll: true)
        );

        return true;
    }

    private bool TryAddNode_ByDelayModify()
    {
        if (targetSequence == null) return false;

        int insertAt = (_selectedNode >= 0) ? (_selectedNode + 1) : int.MaxValue;

        DelayModify("Add Node", so =>
        {
            var seq = (SequenceSpecSO)so.targetObject;
            if (seq == null) return;

            seq.nodes ??= new System.Collections.Generic.List<NodeSpec>();

            insertAt = Mathf.Clamp(insertAt, 0, seq.nodes.Count);

            seq.nodes.Insert(insertAt, CreateBlankNode());

            _selectedNode = insertAt;
            _selectedStep = -1;

            _nodesList = null;
            _stepsList = null;
            _commandsList = null;

            ForceCompileAll();
        }, forceRebuild: true);

        return true;
    }

    private bool TryAddStep_ByDelayModify()
    {
        if (targetSequence == null) return false;

        if (_nodesProp == null || !_nodesProp.isArray || _nodesProp.arraySize <= 0 || _selectedNode < 0)
        {
            AddNode();

            if (_selectedNode < 0) _selectedNode = 0;
        }

        int nodeIndex = _selectedNode;
        if (nodeIndex < 0) return false;

        int insertAt = (_selectedStep >= 0) ? (_selectedStep + 1) : int.MaxValue;

        DelayModify("Add Step", so =>
        {
            var seq = (SequenceSpecSO)so.targetObject;
            if (seq == null) return;

            seq.nodes ??= new System.Collections.Generic.List<NodeSpec>();
            if (nodeIndex < 0 || nodeIndex >= seq.nodes.Count) return;

            var node = seq.nodes[nodeIndex];
            node.steps ??= new System.Collections.Generic.List<StepSpec>();

            insertAt = Mathf.Clamp(insertAt, 0, node.steps.Count);
            node.steps.Insert(insertAt, CreateBlankStep());

            _selectedStep = insertAt;

            _commandsList = null;
            _stepsList = null;
            _nodesList = null;

            ForceCompileAll();
        }, forceRebuild: true);

        return true;
    }

    private void HandleRenameShortcuts()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        if (e.keyCode == KeyCode.F2)
        {
            if (GetCurrentStepProp() != null)
            {
                if (_navColumn == NavColumn.Steps || _navColumn == NavColumn.Commands)
                {
                    _requestFocusStepNameField = true;

                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                }
            }
        }
    }

    private void HandleRoleSlotHotkeys()
    {
        var e = Event.current;
        if (e == null) return;
        if (EditorGUIUtility.editingTextField) return;
        if (e.type != EventType.KeyDown) return;

        int slotIndex = -1;

        if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9)
            slotIndex = (int)e.keyCode - (int)KeyCode.Alpha1;

        if (slotIndex < 0 && e.keyCode >= KeyCode.Keypad1 && e.keyCode <= KeyCode.Keypad9)
            slotIndex = (int)e.keyCode - (int)KeyCode.Keypad1;

        if (slotIndex < 0) return;

        EnsureRoleSlotsCapacity();

        if (slotIndex >= _roleSlotCount) return;

        if (e.shift)
        {
            if (!CanApplyRoleToCurrentStep(slotIndex)) return;

            ApplyRoleToCurrentStep(slotIndex);
            e.Use();
            GUIUtility.ExitGUI();
            return;
        }

        if (_autoFillIdsOnAdd)
        {
            if (_autoFillRoleSlotIndex != slotIndex)
            {
                _autoFillRoleSlotIndex = slotIndex;
                EditorPrefs.SetInt(PrefKey_AutoFillRoleSlotIndex, _autoFillRoleSlotIndex);

                GUI.FocusControl(null);
            }

            e.Use();
            GUIUtility.ExitGUI();
            return;
        }
    }
}
#endif