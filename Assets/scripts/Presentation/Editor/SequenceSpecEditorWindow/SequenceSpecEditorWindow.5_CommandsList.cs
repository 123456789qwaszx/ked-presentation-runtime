#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
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
    private static readonly Color _cmdBgEvenPro = new Color(1f, 1f, 1f, 0.04f);
    private static readonly Color _cmdBgOddPro = new Color(1f, 1f, 1f, 0.02f);
    private static readonly Color _cmdBgEvenLight = new Color(0f, 0f, 0f, 0.04f);
    private static readonly Color _cmdBgOddLight = new Color(0f, 0f, 0f, 0.02f);
    private static readonly Color _cmdSelectionPro = new Color(0.20f, 0.45f, 0.80f, 0.16f);
    private static readonly Color _cmdSelectionLight = new Color(0.20f, 0.45f, 0.80f, 0.10f);
    private static readonly Color _cmdLinePro = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color _cmdLineLight = new Color(0f, 0f, 0f, 0.15f);

    private void EnsureCommandsList(SerializedProperty commandsProp)
    {
        if (commandsProp == null || !commandsProp.isArray)
            return;

        if (!IsSerializeReferenceCommandList(commandsProp))
        {
            EditorGUILayout.HelpBox(
                "This editor requires [SerializeReference] polymorphic command lists.",
                MessageType.Error);
            return;
        }

        string commandsPath = commandsProp.propertyPath;
        var foldoutMap = GetFoldoutMap(commandsPath);

        if (_commandsList != null && _commandsPropPath == commandsPath)
        {
            if (_pendingCommandIndex >= 0)
            {
                int next = Mathf.Clamp(_pendingCommandIndex, 0, commandsProp.arraySize - 1);
                _commandsList.index = next;
                _pendingCommandIndex = -1;

                RequestScrollToCommand(next, repaint: false);
            }
            return;
        }

        _commandsPropPath = commandsPath;

        _commandsList = new ReorderableList(
            _so, commandsProp,
            draggable: true,
            displayHeader: true,
            displayAddButton: false,
            displayRemoveButton: false);

        _commandsList.index = -1;

        _commandsList.onSelectCallback = list =>
        {
            _navColumn = NavColumn.Commands;
            RequestScrollToCommand(list.index);
        };

        _commandsList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Commands", EditorStyles.boldLabel);

            var e = Event.current;
            if (e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
            {
                ShowCommandAddMenu(
                    commandsPath,
                    insertAt: 0,
                    onSingle: t => InsertSingleAt(commandsPath, 0, t, scroll: true),
                    onBatch: types => InsertBatchAt(commandsPath, 0, types, scroll: true)
                );
                e.Use();
            }
        };

        _commandsList.drawNoneElementCallback = rect =>
        {
            GUI.Label(rect, "No commands yet. Right-click to add.", EditorStyles.centeredGreyMiniLabel);

            var e = Event.current;
            bool rightClick =
                (e.type == EventType.ContextClick) ||
                (e.type == EventType.MouseDown && e.button == 1);

            if (rightClick && rect.Contains(e.mousePosition))
            {
                ShowCommandAddMenu(
                    commandsPath,
                    insertAt: 0,
                    onSingle: t => InsertSingleAt(commandsPath, 0, t, scroll: true),
                    onBatch: types => InsertBatchAt(commandsPath, 0, types, scroll: true)
                );
                e.Use();
            }
        };

        _commandsList.elementHeightCallback = index =>
        {
            float header = EditorGUIUtility.singleLineHeight;

            if (index < 0 || index >= commandsProp.arraySize)
                return header + 6f;

            var el = commandsProp.GetArrayElementAtIndex(index);
            if (el == null || el.propertyType != SerializedPropertyType.ManagedReference)
                return header + 6f;

            long id = el.managedReferenceId;

            bool expanded = false;
            if (foldoutMap != null && id != 0 && foldoutMap.TryGetValue(id, out bool saved))
                expanded = saved;

            if (!expanded)
                return header + 10f;

            float body = GetManagedRefBodyHeight_IgnoreIsExpanded(el);
            return header + body + 10f;
        };

        bool isPro = EditorGUIUtility.isProSkin;

        _commandsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (index < 0 || index >= commandsProp.arraySize) return;

            var e = Event.current;

            if (e.type == EventType.Repaint)
            {
                bool even = (index % 2) == 0;

                Color bg = isPro
                    ? (even ? _cmdBgEvenPro : _cmdBgOddPro)
                    : (even ? _cmdBgEvenLight : _cmdBgOddLight);
                EditorGUI.DrawRect(rect, bg);

                bool selected = (_commandsList != null && _commandsList.index == index);
                if (selected)
                {
                    EditorGUI.DrawRect(rect, isPro ? _cmdSelectionPro : _cmdSelectionLight);
                }

                var line = new Rect(rect.x, rect.yMax - 1f, rect.width, 1f);
                EditorGUI.DrawRect(line, isPro ? _cmdLinePro : _cmdLineLight);
            }

            if (e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
            {
                if (_commandsList != null)
                {
                    _commandsList.index = index;
                    RequestScrollToCommand(index, repaint: false);
                }

                Repaint();

                int clickedIndex = index;
                int insertAt = clickedIndex + 1;

                ShowCommandAddMenu(
                    commandsPath: commandsPath,
                    insertAt: insertAt,
                    onSingle: t => InsertSingleAt(commandsPath, insertAt, t, scroll: false),
                    onBatch: types => InsertBatchAt(commandsPath, insertAt, types, scroll: false),
                    extendMenu: menu =>
                    {
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Delete"), false, () =>
                        {
                            DeleteCommandAt(commandsPath, clickedIndex, after: () =>
                            {
                                _commandsList = null;
                                _commandsPropPath = null;
                                AfterSequenceChanged();
                            });
                        });
                    }
                );

                e.Use();
                return;
            }

            var element = commandsProp.GetArrayElementAtIndex(index);
            if (element == null) return;

            rect.y += 2f;
            rect.height -= 2f;

            float lineH = EditorGUIUtility.singleLineHeight;
            var headerRect = new Rect(rect.x, rect.y, rect.width, lineH);

            long id = (element.propertyType == SerializedPropertyType.ManagedReference)
                ? element.managedReferenceId
                : 0;

            bool expanded = false;
            if (foldoutMap != null && id != 0 && foldoutMap.TryGetValue(id, out bool saved))
                expanded = saved;

            var arrowRect = new Rect(headerRect.x, headerRect.y, 14f, headerRect.height);
            bool newExpanded = EditorGUI.Foldout(arrowRect, expanded, GUIContent.none, false);

            if (newExpanded != expanded)
            {
                if (_commandsList != null) _commandsList.index = index;
                _pendingCommandIndex = index;

                if (foldoutMap != null && id != 0)
                    foldoutMap[id] = newExpanded;

                SaveFoldouts();

                // 스크롤 예약 (다음 프레임에 정확한 rect 기반으로 처리)
                _scrollToCommandIndex = true;
                _scrollTargetCommandIndex = index;

                _commandsList = null;
                _commandsPropPath = null;

                GUI.changed = true;
                Repaint();
                GUIUtility.ExitGUI();
                return;
            }

            var labelRect = new Rect(headerRect.x + 14f, headerRect.y, headerRect.width - 14f, headerRect.height);
            EditorGUI.LabelField(labelRect, new GUIContent(SummarizeCommand(element, index)));

            if (expanded)
            {
                var bodyRect = new Rect(rect.x, rect.y + lineH + 2f, rect.width, rect.height - lineH - 2f);
                DrawManagedRefBody_IgnoreIsExpanded(bodyRect, element);
            }
        };

        _commandsList.onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
        {
            list.index = newIndex;

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetSequence);

            RequestScrollToCommand(newIndex, repaint: false);

            AfterSequenceChanged();
            Repaint();
        };

        if (_pendingCommandIndex >= 0)
        {
            int next = Mathf.Clamp(_pendingCommandIndex, 0, commandsProp.arraySize - 1);
            _commandsList.index = next;
            _pendingCommandIndex = -1;

            RequestScrollToCommand(next, repaint: false);
        }
    }

    private static bool IsSerializeReferenceCommandList(SerializedProperty commandsProp)
    {
        if (commandsProp == null || !commandsProp.isArray) return false;
        if (commandsProp.arraySize == 0) return true;

        var el = commandsProp.GetArrayElementAtIndex(0);
        return el != null && el.propertyType == SerializedPropertyType.ManagedReference;
    }

    private static float GetManagedRefBodyHeight_IgnoreIsExpanded(SerializedProperty managedRef, float vSpace = 2f)
    {
        if (managedRef == null) return 0f;
        if (managedRef.propertyType != SerializedPropertyType.ManagedReference) return 0f;

        float h = 0f;

        var it = managedRef.Copy();
        var end = it.GetEndProperty();

        bool hasChild = it.NextVisible(true);
        if (!hasChild) return 0f;

        while (!SerializedProperty.EqualContents(it, end))
        {
            h += EditorGUI.GetPropertyHeight(it, includeChildren: true) + vSpace;
            if (!it.NextVisible(false)) break;
        }

        return h;
    }

    private static void DrawManagedRefBody_IgnoreIsExpanded(Rect rect, SerializedProperty managedRef, float vSpace = 2f)
    {
        if (managedRef == null) return;
        if (managedRef.propertyType != SerializedPropertyType.ManagedReference) return;

        var it = managedRef.Copy();
        var end = it.GetEndProperty();

        bool hasChild = it.NextVisible(true);
        if (!hasChild) return;

        float y = rect.y;

        using (new EditorGUI.IndentLevelScope(1))
        {
            while (!SerializedProperty.EqualContents(it, end))
            {
                float ph = EditorGUI.GetPropertyHeight(it, includeChildren: true);
                var r = new Rect(rect.x, y, rect.width, ph);

                EditorGUI.PropertyField(r, it, includeChildren: true);

                y += ph + vSpace;

                if (!it.NextVisible(false))
                    break;
            }
        }
    }
}
#endif