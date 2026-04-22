#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// ReorderableList 빌드/수명주기 담당 파트.
/// Nodes/Steps 리스트 UI(ReorderableList) 생성/갱신 + 선택 상태 유지”를 책임진다.
///
/// 주요 책임
/// - RebuildIfNeeded(force):
///   - targetSequence가 바뀌거나(force=true) 리빌드가 필요할 때 SerializedObject/_sequenceKeyProp/_nodesProp를 재구성한다.
///   - 이전 선택(_selectedNode/_selectedStep)을 가능한 범위에서 보존하고, 유효하지 않으면 -1로 정리한다.
///   - 리스트/프로퍼티 경로 캐시(_nodesList/_stepsList/_commandsList, _stepsPropPath/_commandsPropPath)를 무효화하여
///     다음 OnGUI에서 자연스럽게 재빌드되게 만든다.
///   - 커맨드 폴드아웃 캐시(_commandFoldoutsByPath)를 초기화한다(구조/경로 변경에 따른 스테일 방지).
///
/// - BuildNodesList():
///   - 좌측 Nodes 리스트(ReorderableList)를 만든다.
///   - 멀티 삭제 모드(_deleteMultiMode)일 때 체크박스 UI와 선택 집합(_checkedNodes)을 처리한다.
///   - 검색(_search) 필터링 시 시각적 디밍 처리(NodeMatchesSearch).
///   - 선택/네비게이션 강조(_selectedNode, _navColumn) 및 우클릭 컨텍스트 메뉴(추가/복제/삭제)를 구성한다.
///   - Nodes 선택 변경 시 Steps/Commands 리스트 캐시를 무효화하여 우측 패널이 올바르게 갱신되게 한다.
///
/// - EnsureStepsList(nodeProp, stepsProp):
///   - 우측 Steps 리스트(ReorderableList)를 만든다(선택된 Node 기준).
///   - _stepsPropPath 캐싱으로 “같은 stepsProp”이면 재생성 없이 인덱스만 동기화한다.
///   - 각 Step의 요약 표시(이름/editorName, gate 요약, compiled 커맨드 수)를 책임진다.
///   - 드래그 중 플래그(_isDraggingSteps) 세팅(커맨드 패널의 상호작용 제어에 사용).
///   - 우클릭 컨텍스트 메뉴(추가/복제/삭제) 및 Reorder 콜백에서 Dirty/Compile/리페인트 흐름을 만든다.
///
/// - NodeMatchesSearch():
///   - 검색 문자열이 들어오면 해당 Node 내부 Step의 compiled 커맨드 요약(SummarizeCommand)을 훑어 매칭 여부를 판단한다.
///   - “Nodes 리스트에서 어떤 노드를 dim 처리할지”를 결정하는 검색 로직의 단일 진입점이다.
///
/// 이 파일을 보면 좋은 경우(무엇을 건드리고 싶을 때)
/// - “노드/스텝 리스트의 표시 형식(라벨, 요약, 카운트)”, 선택 강조 방식, 검색/필터 UX를 바꾸고 싶을 때
/// - 노드/스텝 우클릭 메뉴(추가/복제/삭제) 동작, 삽입 위치 정책, 선택 유지 정책을 바꾸고 싶을 때
/// - targetSequence 교체/선택 변경 시 리빌드 조건(force 정책, 캐시 무효화 범위)을 조정하고 싶을 때
/// - 멀티 삭제 모드의 체크 UI/동작(드래그 방지, 토글 UX)을 손보고 싶을 때
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    private static readonly Color _searchDimPro = new Color(0, 0, 0, 0.28f);
    private static readonly Color _searchDimLight = new Color(1, 1, 1, 0.38f);

    private void RebuildIfNeeded(bool force)
    {
        if (!force && _so != null && _so.targetObject == targetSequence && _nodesList != null)
            return;

        if (targetSequence == null)
        {
            _so = null;
            _nodesList = null;
            _stepsList = null;
            _commandsList = null;

            _stepsPropPath = null;
            _commandsPropPath = null;

            _selectedNode = -1;
            _selectedStep = -1;
            return;
        }

        int prevNode = _selectedNode;
        int prevStep = _selectedStep;

        _so = new SerializedObject(targetSequence);
        _sequenceKeyProp = _so.FindProperty("sequenceKey");
        _nodesProp = _so.FindProperty("nodes");

        int nodeCount = _nodesProp?.arraySize ?? 0;
        _selectedNode = (nodeCount <= 0) ? -1 : Mathf.Clamp(prevNode, 0, nodeCount - 1);

        if (_selectedNode >= 0)
        {
            var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
            var stepsProp = nodeProp.FindPropertyRelative("steps");
            int stepCount = (stepsProp != null && stepsProp.isArray) ? stepsProp.arraySize : 0;
            _selectedStep = (stepCount <= 0) ? -1 : Mathf.Clamp(prevStep, 0, stepCount - 1);
        }
        else
        {
            _selectedStep = -1;
        }

        _commandFoldoutsByPath.Clear();

        BuildNodesList();

        _stepsList = null;
        _commandsList = null;

        _stepsPropPath = null;
        _commandsPropPath = null;
    }

    private void BuildNodesList()
    {
        if (_nodesProp == null) return;

        _nodesList = new ReorderableList(_so, _nodesProp,
            draggable: true,
            displayHeader: true,
            displayAddButton: false,
            displayRemoveButton: false);

        _nodesList.draggable = !_deleteMultiMode;

        _nodesList.drawHeaderCallback = rect =>
        {
            int count = _nodesProp != null ? _nodesProp.arraySize : 0;
            EditorGUI.LabelField(rect, $"Nodes({count})");
        };

        _nodesList.onSelectCallback = list =>
        {
            _navColumn = NavColumn.Nodes; // 마우스 선택 → 컬럼 포커스도 Nodes로

            _selectedNode = list.index;
            _selectedStep = -1;

            _stepsList = null;
            _commandsList = null;

            // Commands 쪽 선택/스크롤 예약도 같이 초기화
            _pendingCommandIndex = -1;
            _scrollToCommandIndex = false;
            _scrollTargetCommandIndex = -1;

            Repaint();
        };

        bool isPro = EditorGUIUtility.isProSkin;

        _nodesList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= _nodesProp.arraySize) return;

            const float checkW = 18f;
            var checkRect = new Rect(rect.x + 2f, rect.y + 2f, checkW, rect.height - 4f);

            if (_deleteMultiMode)
            {
                var leftEvent = Event.current;

                int hint = unchecked(0x51E9_0000 + index);
                int id = GUIUtility.GetControlID(hint, FocusType.Passive, checkRect);

                bool checkedNow = _checkedNodes.Contains(index);

                if (leftEvent.type == EventType.Repaint)
                    GUI.Toggle(checkRect, checkedNow, GUIContent.none);

                if (leftEvent.type == EventType.MouseDown && leftEvent.button == 0 &&
                    checkRect.Contains(leftEvent.mousePosition))
                {
                    GUIUtility.hotControl = id;
                    leftEvent.Use();
                    GUI.FocusControl(null);
                    return;
                }

                if (leftEvent.type == EventType.MouseDrag && GUIUtility.hotControl == id)
                {
                    leftEvent.Use();
                    return;
                }

                if (leftEvent.type == EventType.MouseUp && leftEvent.button == 0 && GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;

                    if (checkRect.Contains(leftEvent.mousePosition))
                    {
                        if (checkedNow) _checkedNodes.Remove(index);
                        else _checkedNodes.Add(index);

                        GUI.changed = true;
                    }

                    leftEvent.Use();
                    Repaint();
                    return;
                }

                rect.xMin += checkW + 4f;
            }

            var nodeProp = _nodesProp.GetArrayElementAtIndex(index);
            var stepsProp = nodeProp.FindPropertyRelative("steps");
            int stepCount = (stepsProp != null && stepsProp.isArray) ? stepsProp.arraySize : 0;

            var nameProp = nodeProp.FindPropertyRelative("editorName");

            bool hit = true;
            if (!string.IsNullOrWhiteSpace(_search))
                hit = NodeMatchesSearch(nodeProp, _search);

            if (!hit && Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, isPro ? _searchDimPro : _searchDimLight);
            }

            if (index == _selectedNode)
            {
                bool strong = (_navColumn == NavColumn.Nodes);
                DrawNavSelectionBg(rect, strong);
            }

            const float labelW = 52f;
            const float countW = 44f;

            var labelRect = new Rect(rect.x, rect.y + 1f, labelW, rect.height - 2f);
            var fieldRect = new Rect(rect.x + labelW + 2f, rect.y + 1f, rect.width - labelW - countW - 4f,
                rect.height - 2f);
            var countRect = new Rect(rect.x + rect.width - countW, rect.y, countW, rect.height);

            EditorGUI.LabelField(labelRect, $"Node {index}", EditorStyles.miniLabel);

            if (nameProp != null)
            {
                string controlName = GetNodeNameControlName(index);
                GUI.SetNextControlName(controlName);
                
                EditorGUI.BeginChangeCheck();
                string newName = EditorGUI.TextField(fieldRect, nameProp.stringValue ?? "");
                if (EditorGUI.EndChangeCheck())
                    nameProp.stringValue = newName;

                if (string.IsNullOrWhiteSpace(nameProp.stringValue))
                {
                    var ph = fieldRect;
                    ph.x += 4f;
                    EditorGUI.LabelField(ph, $"Node {index}", EditorStyles.centeredGreyMiniLabel);
                }
            }
            else
            {
                EditorGUI.LabelField(fieldRect, $"Node {index}");
            }

            EditorGUI.LabelField(countRect, $"({stepCount})", EditorStyles.miniLabel);

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
            {
                _selectedNode = index;
                _selectedStep = -1;
                _stepsList = null;
                _commandsList = null;
                Repaint();

                string nodesPath = _nodesProp.propertyPath;

                ShowContextMenu(menu =>
                {
                    menu.AddItem(new GUIContent("Add Node (Below)"), false, () =>
                    {
                        int insertAt = index + 1;

                        DelayModify("Add Node", so =>
                        {
                            var seq = (SequenceSpecSO)so.targetObject;
                            if (seq == null) return;

                            seq.nodes ??= new List<NodeSpec>();
                            insertAt = Mathf.Clamp(insertAt, 0, seq.nodes.Count);
                            seq.nodes.Insert(insertAt, CreateBlankNode());

                            _selectedNode = insertAt;
                            _selectedStep = -1;
                            _nodesList = null;
                            _stepsList = null;
                            _commandsList = null;

                            AfterSequenceChanged();
                        });
                    });

                    menu.AddItem(new GUIContent("Duplicate Node"), false, () =>
                    {
                        int srcIndex = index;
                        int insertAt = index + 1;

                        DelayModify("Duplicate Node", so =>
                        {
                            var seq = (SequenceSpecSO)so.targetObject;
                            if (seq == null) return;

                            seq.nodes ??= new List<NodeSpec>();
                            if (srcIndex < 0 || srcIndex >= seq.nodes.Count) return;

                            insertAt = Mathf.Clamp(insertAt, 0, seq.nodes.Count);
                            seq.nodes.Insert(insertAt, CloneNodeDeep(seq.nodes[srcIndex]));

                            _selectedNode = insertAt;
                            _selectedStep = -1;

                            _nodesList = null;
                            _stepsList = null;
                            _commandsList = null;

                            AfterSequenceChanged();
                        });
                    });

                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Delete Node"), false, () =>
                    {
                        DeleteArrayElementByPath("Delete Node", nodesPath, index, after: () =>
                        {
                            int newNode = Mathf.Clamp(_selectedNode, 0, _nodesProp.arraySize - 2);
                            _selectedNode = newNode;
                            _selectedStep = -1;

                            _stepsList = null;
                            _commandsList = null;

                            AfterSequenceChanged();
                        });
                    });
                });

                e.Use();
            }
        };

        SyncNodeSelectionToList();
    }

    private void EnsureStepsList(SerializedProperty nodeProp, SerializedProperty stepsProp)
    {
        if (_stepsList != null && _stepsPropPath == stepsProp.propertyPath)
        {
            _stepsList.index = Mathf.Clamp(_selectedStep, 0, stepsProp.arraySize - 1);
            return;
        }

        _stepsPropPath = stepsProp.propertyPath;

        _selectedStep = (stepsProp.arraySize <= 0) ? -1 : Mathf.Clamp(_selectedStep, 0, stepsProp.arraySize - 1);

        _stepsList = new ReorderableList(_so, stepsProp,
            draggable: true,
            displayHeader: true,
            displayAddButton: false,
            displayRemoveButton: false);

        _stepsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Steps");

        _stepsList.onSelectCallback = list =>
        {
            _navColumn = NavColumn.Steps; // 마우스 선택 → 컬럼 포커스도 Steps로

            _selectedStep = list.index;
            _stepsList.index = _selectedStep;

            _commandsList = null;

            // Commands 예약/스크롤 플래그도 정리
            _pendingCommandIndex = -1;
            _scrollToCommandIndex = false;
            _scrollTargetCommandIndex = -1;
            
            _requestFocusStepNameField = false;
            
            _scrollToStepIndex = true;
            _scrollTargetStepIndex = _selectedStep;
            
            Repaint();
        };

        _stepsList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 10f;

        _stepsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= stepsProp.arraySize) return;

            rect.y += 2f;
            rect.height -= 2f;

            bool selected = (_selectedStep == index);
            if (selected)
            {
                bool strong = (_navColumn == NavColumn.Steps);
                DrawNavSelectionBg(rect, strong);
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
                _isDraggingSteps = true;

            var stepProp = stepsProp.GetArrayElementAtIndex(index);

            var gateProp = stepProp.FindPropertyRelative("gate");
            string gateSummary = gateProp != null ? SummarizeGate(gateProp) : "(no gate)";

            var compiledProp = stepProp.FindPropertyRelative("compiled");
            int cmdCount = (compiledProp != null && compiledProp.isArray) ? compiledProp.arraySize : 0;

            var nameProp = stepProp.FindPropertyRelative("editorName");
            string stepName = (nameProp != null) ? (nameProp.stringValue ?? "") : "";
            stepName = stepName.Trim();
            string title = string.IsNullOrEmpty(stepName) ? $"Step {index}" : stepName;

            const float leftPad = 2f;
            var contentRect = new Rect(rect.x + leftPad, rect.y, rect.width - leftPad, rect.height);
            
            EditorGUI.LabelField(contentRect, $"{title} | {gateSummary} | ({cmdCount})");

            if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
            {
                _selectedStep = index;
                _stepsList.index = index;
                _commandsList = null;
                Repaint();

                string stepsPath = stepsProp.propertyPath;

                ShowContextMenu(menu =>
                {
                    menu.AddItem(new GUIContent("Add Step (below)"), false, () =>
                    {
                        int nodeIndex = _selectedNode;
                        int insertAt = index + 1;

                        DelayModify("Add Step", so =>
                        {
                            var seq = (SequenceSpecSO)so.targetObject;
                            if (seq == null) return;
                            if (nodeIndex < 0 || nodeIndex >= seq.nodes.Count) return;

                            var node = seq.nodes[nodeIndex];
                            node.steps ??= new List<StepSpec>();

                            insertAt = Mathf.Clamp(insertAt, 0, node.steps.Count);
                            node.steps.Insert(insertAt, CreateBlankStep());

                            _selectedStep = insertAt;
                            _stepsList = null;
                            _commandsList = null;

                            AfterSequenceChanged();
                        });
                    });

                    menu.AddItem(new GUIContent("Duplicate Step"), false, () =>
                    {
                        int nodeIndex = _selectedNode;
                        int srcIndex = index;
                        int insertAt = index + 1;

                        DelayModify("Duplicate Step", so =>
                        {
                            var seq = (SequenceSpecSO)so.targetObject;
                            if (seq == null) return;
                            if (nodeIndex < 0 || nodeIndex >= seq.nodes.Count) return;

                            var node = seq.nodes[nodeIndex];
                            node.steps ??= new List<StepSpec>();

                            if (srcIndex < 0 || srcIndex >= node.steps.Count) return;

                            insertAt = Mathf.Clamp(insertAt, 0, node.steps.Count);
                            node.steps.Insert(insertAt, CloneStepDeep(node.steps[srcIndex]));

                            _selectedStep = insertAt;
                            _stepsList = null;
                            _commandsList = null;

                            AfterSequenceChanged();
                        });
                    });

                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Delete Step"), false, () =>
                    {
                        DeleteArrayElementByPath("Delete Step", stepsPath, index, after: () =>
                        {
                            _selectedStep = Mathf.Clamp(_selectedStep, 0, stepsProp.arraySize - 2);
                            _stepsList = null;
                            _commandsList = null;

                            AfterSequenceChanged();
                        });
                    });
                });

                e.Use();
            }
        };

        _stepsList.onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
        {
            _selectedStep = newIndex;
            _commandsList = null;

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetSequence);

            AfterSequenceChanged();
            Repaint();
        };

        _stepsList.index = (_selectedStep < 0) ? -1 : Mathf.Clamp(_selectedStep, 0, stepsProp.arraySize - 1);
    }

    private void SyncNodeSelectionToList()
    {
        if (_nodesList == null) return;

        int count = _nodesProp?.arraySize ?? 0;
        if (count <= 0)
        {
            _nodesList.index = -1;
            return;
        }

        _selectedNode = Mathf.Clamp(_selectedNode, 0, count - 1);
        _nodesList.index = _selectedNode;
    }

    private bool NodeMatchesSearch(SerializedProperty nodeProp, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        query = query.Trim();

        var stepsProp = nodeProp.FindPropertyRelative("steps");
        if (stepsProp == null || !stepsProp.isArray) return false;

        for (int si = 0; si < stepsProp.arraySize; si++)
        {
            var step = stepsProp.GetArrayElementAtIndex(si);

            var compiled = step.FindPropertyRelative("compiled");
            if (compiled == null || !compiled.isArray) continue;

            for (int ci = 0; ci < compiled.arraySize; ci++)
            {
                var cmd = compiled.GetArrayElementAtIndex(ci);
                string summary = SummarizeCommand(cmd, ci);
                if (!string.IsNullOrEmpty(summary) && summary.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }
    
    private static string GetNodeNameControlName(int nodeIndex)
        => $"SeqNodeName_{nodeIndex}";
}
#endif