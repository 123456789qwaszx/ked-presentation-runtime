#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keyboard navigation & shortcut layer for SequenceSpecEditorWindow.
///
/// 역할
/// - 에디터 윈도우의 "키보드 조작 UX"를 한 곳에 모아둔 partial.
/// - 텍스트 필드 편집 중이면(EditingTextField) 키 입력을 무시해서, 입력/네비게이션 충돌을 방지.
/// - Nodes / Steps / Commands 3-컬럼 네비게이션 상태(_navColumn)와 선택 인덱스(_selectedNode/_selectedStep/_commandsList.index)를
///   키 입력(←→↑↓/Space/Ctrl(or Cmd)+E)으로 일관되게 갱신.
/// - Commands 컬럼에서는 트랙(_activeTrack) 이동까지 포함해서 "키보드만으로 편집"이 가능하게 함.
/// - 선택 이동 시 스크롤을 자동 보정(EnsureSelectedCommandVisible)하여 현재 선택이 화면 밖으로 나가지 않게 유지.
/// - "컬럼 단위 삭제" 단축키(Ctrl/Cmd+E)를 처리하고, 실제 삭제는 기존 삭제 유틸(DeleteCommandAt/DeleteSelectedStep/…)
///   로 위임하여 데이터 수정 로직은 분리.
///
/// 여기서 건드리면 좋은 것들(= 키보드 UX를 바꾸고 싶을 때)
/// - 단축키 매핑/정책:
///   - HandleArrowNavigation() : ←→↑↓/Space 처리의 메인 라우터.
///   - HandleDeleteByActiveColumnShortcut() / TryDeleteByActiveColumn() : Ctrl/Cmd+E 정책 변경.
/// - 컬럼 이동 정책:
///   - SyncSelectionAfterColumnChange() : Nodes/Steps/Commands로 이동할 때 선택 인덱스를 어떻게 유지/클램프할지.
///   - CanEnterCommandsColumn_FromCurrentStep() : Steps→Commands 진입 조건(트랙 리스트 존재 여부 등).
/// - Commands 컬럼 세부 UX:
///   - MoveTrack(int delta) : Commands 컬럼에서 트랙 좌/우 이동 규칙, 트랙 바꿀 때 선택 유지 정책.
///   - MoveCommandSelection(int delta) : 커맨드 선택 이동, 즉시 스크롤 보정/예약 플래그 정책.
///   - EnsureSelectedCommandVisible(...) : "선택 행이 보이도록" 스크롤 계산(패딩/데드밴드/뷰 높이).
///   - ClearCommandSelection() : Commands에서 선택 초기화 규칙.
///
/// 건드리지 말고 다른 파일을 봐야 하는 것들(= 이 파일의 범위 밖)
/// - 실제 리스트 생성/표시(높이 계산, foldout, 컨텍스트 메뉴 등): EnsureStepsList / EnsureCommandsList 쪽 partial.
/// - 실제 데이터 수정(Add/Duplicate/Delete, DelayModify, DeleteCommandAt 등): "데이터 변경 유틸/컨텍스트 메뉴" partial.
/// - UI 레이아웃(툴바/패널/헤더): DrawToolbar/DrawNodeEditor/DrawBottomCommandBar 쪽 partial.
/// - foldout 저장/로드(SessionState/EditorPrefs): Foldout state management partial.
///
/// 의존하는 상태/필드(이 파일이 만지는 핵심)
/// - _navColumn, _selectedNode, _selectedStep
/// - _commandsList, _stepsList, _nodesList
/// - _activeTrack, _pendingCommandIndex, _rightScroll, _commandsListTopYInRightScroll
/// - _scrollToCommandIndex/_scrollTargetCommandIndex, _lastCommandNavDelta
///
/// 메모
/// - 이 partial은 "입력 해석 → 선택/스크롤 상태 갱신"까지만 책임지고,
///   실제 생성/삭제/컴파일은 다른 partial의 유틸에 위임하는 구조를 유지하는 게 디버깅이 쉽다.
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    private void HandleArrowNavigation()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        if (e.keyCode == KeyCode.F2)
        {
            if (_navColumn == NavColumn.Nodes &&
                _nodesProp != null && _nodesProp.isArray &&
                _selectedNode >= 0 && _selectedNode < _nodesProp.arraySize)
            {
                GUI.FocusControl(GetNodeNameControlName(_selectedNode));
                e.Use();
                Repaint();
                return;
            }
        }

        if (e.control || e.command || e.alt) return;

        bool enter = (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space);

        if (enter)
        {
            if (_navColumn == NavColumn.Nodes)
            {
                if (TryAddNode_ByDelayModify())
                {
                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                    return;
                }

                GUI.FocusControl(null);
                e.Use();
                Repaint();
                return;
            }

            if (_navColumn == NavColumn.Steps)
            {
                if (TryAddStep_ByDelayModify())
                {
                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                    return;
                }

                GUI.FocusControl(null);
                e.Use();
                Repaint();
                return;
            }

            // Commands 컬럼에서 Enter는 HandleCommandShortcuts()에서 처리
        }

        bool left = e.keyCode == KeyCode.LeftArrow;
        bool right = e.keyCode == KeyCode.RightArrow;
        bool up = e.keyCode == KeyCode.UpArrow;
        bool down = e.keyCode == KeyCode.DownArrow;

        if (!(left || right || up || down)) return;

        if (left || right)
        {
            if (_navColumn == NavColumn.Steps && right)
            {
                if (!CanEnterCommandsColumn_FromCurrentStep())
                {
                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                    return;
                }
            }

            int dir = right ? +1 : -1;
            _navColumn = (NavColumn)Mathf.Clamp((int)_navColumn + dir, 0, 2);

            if (_navColumn == NavColumn.Steps)
            {
                ClearCommandSelection();
                _scrollToCommandIndex = false;
                _scrollTargetCommandIndex = -1;
            }

            SyncSelectionAfterColumnChange();

            GUI.FocusControl(null);
            e.Use();
            Repaint();
            return;
        }

        int delta = down ? +1 : -1;

        switch (_navColumn)
        {
            case NavColumn.Nodes:
                MoveNodeSelection(delta);
                break;

            case NavColumn.Steps:
                MoveStepSelection(delta);
                break;

            case NavColumn.Commands:
                MoveCommandSelection(delta);
                break;
        }

        GUI.FocusControl(null);
        e.Use();
        Repaint();
    }

    private bool CanEnterCommandsColumn_FromCurrentStep()
    {
        var stepProp = GetCurrentStepProp();
        if (stepProp == null) return false;

        var commandsProp = FindUnifiedCommandsProp(stepProp);
        if (commandsProp == null || !commandsProp.isArray) return false;

        return true;
    }

    private void SyncSelectionAfterColumnChange()
    {
        if (_navColumn == NavColumn.Nodes)
        {
            if (_nodesList != null)
                _nodesList.index = Mathf.Clamp(_selectedNode, -1, (_nodesProp?.arraySize ?? 0) - 1);

            return;
        }

        if (_navColumn == NavColumn.Steps)
        {
            if (_stepsList != null)
            {
                int count = _stepsList.count;
                _stepsList.index = (count <= 0) ? -1 : Mathf.Clamp(_selectedStep, 0, count - 1);
            }

            return;
        }

        if (_navColumn == NavColumn.Commands)
        {
            if (_commandsList != null)
            {
                int count = _commandsList.count;
                _commandsList.index = (count <= 0) ? -1 : Mathf.Clamp(_commandsList.index, 0, count - 1);
            }

            return;
        }
    }

    private void MoveNodeSelection(int delta)
    {
        if (_nodesProp == null || !_nodesProp.isArray) return;

        int count = _nodesProp.arraySize;
        if (count <= 0) return;

        int next = Mathf.Clamp(_selectedNode + delta, 0, count - 1);
        if (next == _selectedNode) return;

        _selectedNode = next;

        if (_nodesList != null)
            _nodesList.index = _selectedNode;

        _selectedStep = -1;
        _stepsList = null;
        _commandsList = null;
    }

    private void MoveStepSelection(int delta)
    {
        if (_nodesProp == null || !_nodesProp.isArray) return;
        if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return;

        var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
        var stepsProp = nodeProp.FindPropertyRelative("steps");
        if (stepsProp == null || !stepsProp.isArray) return;

        int count = stepsProp.arraySize;
        if (count <= 0) return;

        int cur = Mathf.Clamp(_selectedStep, 0, count - 1);
        int next = Mathf.Clamp(cur + delta, 0, count - 1);
        if (next == _selectedStep) return;

        _selectedStep = next;

        if (_stepsList != null)
            _stepsList.index = _selectedStep;

        _scrollToStepIndex = true;
        _scrollTargetStepIndex = _selectedStep;

        _commandsList = null;
    }

    private void MoveCommandSelection(int delta)
    {
        if (_commandsList == null) return;

        int count = _commandsList.count;
        if (count <= 0) return;

        int cur = Mathf.Clamp(_commandsList.index, 0, count - 1);
        int next = Mathf.Clamp(cur + delta, 0, count - 1);
        if (next == _commandsList.index) return;

        _commandsList.index = next;

        var stepProp = GetCurrentStepProp();
        if (stepProp != null)
        {
            var commandsProp = FindUnifiedCommandsProp(stepProp);
            if (commandsProp != null && commandsProp.isArray)
                EnsureSelectedCommandVisible(commandsProp, next);
        }

        _scrollToCommandIndex = true;
        _scrollTargetCommandIndex = next;

        Repaint();
    }

    private SerializedProperty GetCurrentStepProp()
    {
        if (_nodesProp == null || !_nodesProp.isArray) return null;
        if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return null;

        var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
        if (nodeProp == null) return null;

        var stepsProp = nodeProp.FindPropertyRelative("steps");
        if (stepsProp == null || !stepsProp.isArray) return null;

        if (_selectedStep < 0 || _selectedStep >= stepsProp.arraySize) return null;

        return stepsProp.GetArrayElementAtIndex(_selectedStep);
    }

    private void ClearCommandSelection()
    {
        _pendingCommandIndex = -1;

        if (_commandsList != null)
            _commandsList.index = -1;

        Repaint();
    }

    private void HandleDeleteByActiveColumnShortcut()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (EditorGUIUtility.editingTextField) return;

        bool mod = e.control || e.command;
        if (!mod) return;

        if (e.keyCode != KeyCode.E) return;

        if (!TryDeleteByActiveColumn())
            return;

        GUI.FocusControl(null);
        e.Use();
        Repaint();
        GUIUtility.ExitGUI();
    }

    private bool TryDeleteByActiveColumn()
    {
        switch (_navColumn)
        {
            case NavColumn.Commands:
            {
                var stepProp = GetCurrentStepProp();
                if (stepProp == null) return false;

                var commandsProp = FindUnifiedCommandsProp(stepProp);
                if (commandsProp == null || !commandsProp.isArray) return false;

                if (_commandsList == null) return false;

                int idx = _commandsList.index;
                if (idx < 0 || idx >= commandsProp.arraySize) return false;

                DeleteCommandAt(commandsProp.propertyPath, idx, after: () =>
                {
                    _commandsList = null;
                    _commandsPropPath = null;
                });

                return true;
            }

            case NavColumn.Steps:
            {
                if (_nodesProp == null || !_nodesProp.isArray) return false;
                if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return false;

                var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
                var stepsProp = nodeProp.FindPropertyRelative("steps");
                if (stepsProp == null || !stepsProp.isArray) return false;

                if (_stepsList == null) return false;

                int idx = _stepsList.index;
                if (idx < 0 || idx >= stepsProp.arraySize) return false;

                DeleteSelectedStep(stepsProp);
                return true;
            }

            case NavColumn.Nodes:
            {
                if (_nodesProp == null || !_nodesProp.isArray) return false;
                if (_nodesProp.arraySize <= 0) return false;

                DeleteSelectedNodeWithoutConfirm();
                return true;
            }

            default:
                return false;
        }
    }

    private void DrawCommandsScrollArea(SerializedProperty stepProp)
    {
        var commandsProp = FindUnifiedCommandsProp(stepProp);
        if (commandsProp == null || !commandsProp.isArray)
        {
            EditorGUILayout.HelpBox("Commands list missing.", MessageType.Error);
            return;
        }

        Rect viewport = GUILayoutUtility.GetRect(
            0f, 100000f,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true),
            GUILayout.MinHeight(180f)
        );

        if (Event.current.type == EventType.Repaint)
        {
            _commandsViewportRect = viewport;
            _commandsViewportHeight = viewport.height;
        }

        bool commandsActive = (_navColumn == NavColumn.Commands);
        bool isEmpty = (commandsProp.arraySize == 0);

        // 좌클릭: Commands 컬럼 활성화
        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 0 &&
            viewport.Contains(Event.current.mousePosition))
        {
            _navColumn = NavColumn.Commands;
            GUI.FocusControl(null);
            Repaint();
        }

        // 우클릭: 비어 있어도 Add Command 메뉴 열기
        if (Event.current.type == EventType.ContextClick &&
            viewport.Contains(Event.current.mousePosition))
        {
            _navColumn = NavColumn.Commands;
            GUI.FocusControl(null);

            string commandsPath = commandsProp.propertyPath;
            int insertAt = commandsProp.arraySize;

            ShowCommandAddMenu(
                commandsPath,
                insertAt: insertAt,
                onSingle: t => InsertSingleAt(commandsPath, insertAt, t, scroll: true),
                onBatch: types => InsertBatchAt(commandsPath, insertAt, types, scroll: true)
            );

            Event.current.Use();
            GUIUtility.ExitGUI();
            return;
        }

        // 빈 상태일 때만 컬럼 배경 강조
        if (commandsActive && isEmpty)
            DrawNavSelectionBg(viewport, strong: true);

        EnsureCommandsList(commandsProp);

        int count = commandsProp.arraySize;

        // 비어 있어도 Commands 컬럼은 "선택 가능한 surface" 로 유지
        if (count == 0)
        {
            var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 12
            };

            string msg = commandsActive
                ? "No Commands yet.\nPress Space or right-click to add command."
                : "No Commands yet.";

            GUI.Label(viewport, msg, emptyStyle);

            // 비어 있어도 Commands 컬럼 단축키는 살아 있어야 함
            HandleCommandShortcuts(commandsProp);
            return;
        }

        if (_commandsList == null)
            return;

        float listH = Mathf.Max(1f, _commandsList.GetHeight());
        bool needScroll = listH > viewport.height + 0.5f;

        float contentW = Mathf.Max(0f, viewport.width);
        Rect contentRect = new Rect(0f, 0f, contentW, listH);

        if (needScroll && _scrollToCommandIndex && Event.current.type == EventType.Repaint)
        {
            EnsureSelectedCommandVisible(commandsProp, _scrollTargetCommandIndex);
            _scrollToCommandIndex = false;
            _scrollTargetCommandIndex = -1;
        }

        _commandsScroll = GUI.BeginScrollView(
            viewport,
            _commandsScroll,
            contentRect,
            false,
            needScroll,
            GUIStyle.none,
            GUIStyle.none
        );

        _commandsList.DoList(new Rect(0f, 0f, contentW, listH));

        GUI.EndScrollView();

        if (!needScroll)
        {
            if (Event.current.type == EventType.Repaint && _commandsScroll.y != 0f)
                _commandsScroll.y = 0f;
        }

        HandleCommandShortcuts(commandsProp);
    }

    private void DrawStepsScrollArea(SerializedProperty stepsProp)
    {
        if (stepsProp == null || !stepsProp.isArray) return;
        if (_stepsList == null) return;

        Rect viewport = GUILayoutUtility.GetRect(
            0f, 100000f,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true),
            GUILayout.MinHeight(180f)
        );

        float listH = Mathf.Max(1f, _stepsList.GetHeight());
        bool needScroll = listH > viewport.height + 0.5f;

        float contentW = Mathf.Max(0f, viewport.width);
        Rect contentRect = new Rect(0f, 0f, contentW, listH);

        _stepsScroll = GUI.BeginScrollView(
            viewport,
            _stepsScroll,
            contentRect,
            false,
            needScroll,
            GUIStyle.none,
            GUIStyle.none
        );

        _stepsList.DoList(new Rect(0f, 0f, contentW, listH));

        GUI.EndScrollView();

        if (!needScroll)
        {
            if (Event.current.type == EventType.Repaint && _stepsScroll.y != 0f)
                _stepsScroll.y = 0f;
        }

        if (needScroll && _scrollToStepIndex && Event.current.type == EventType.Repaint)
        {
            EnsureSelectedStepVisible(stepsProp, _scrollTargetStepIndex);
            _scrollToStepIndex = false;
            _scrollTargetStepIndex = -1;
        }
    }

    private void EnsureSelectedStepVisible(SerializedProperty stepsProp, int index)
    {
        if (stepsProp == null || !stepsProp.isArray) return;
        if (_stepsList == null) return;
        if (index < 0 || index >= stepsProp.arraySize) return;

        float topPad = (_stepsList.headerHeight > 5f) ? 4f : 1f;

        float rowH = (_stepsList.elementHeightCallback != null)
            ? _stepsList.elementHeightCallback(index)
            : _stepsList.elementHeight;

        rowH += 2f;

        float yTop = _stepsList.headerHeight + topPad;
        for (int i = 0; i < index; i++)
        {
            float h = (_stepsList.elementHeightCallback != null)
                ? _stepsList.elementHeightCallback(i)
                : _stepsList.elementHeight;

            yTop += (h + 2f);
        }

        float yBottom = yTop + rowH;

        float viewH = Mathf.Max(120f, _stepsViewportHeight);
        float viewTop = _stepsScroll.y;
        float viewBottom = _stepsScroll.y + viewH;

        float pad = Mathf.Clamp(rowH * 1.1f, 10f, 22f);

        float nextY = _stepsScroll.y;

        bool needUp = yTop < viewTop + pad;
        bool needDown = yBottom > viewBottom - pad;

        if (needUp)
            nextY = Mathf.Max(0f, yTop - pad);
        else if (needDown)
            nextY = Mathf.Max(0f, yBottom - viewH + pad);

        if (Mathf.Abs(_stepsScroll.y - nextY) > 0.5f)
            _stepsScroll.y = nextY;
    }

    private void EnsureSelectedCommandVisible(SerializedProperty commandsProp, int index)
    {
        if (commandsProp == null || !commandsProp.isArray) return;
        if (_commandsList == null) return;
        if (index < 0 || index >= commandsProp.arraySize) return;

        float topPad = (_commandsList.headerHeight > 5f) ? 4f : 1f;

        float GetRowHWithPad(int i)
        {
            float h = (_commandsList.elementHeightCallback != null)
                ? _commandsList.elementHeightCallback(i)
                : _commandsList.elementHeight;

            if (h <= 0f) return 0f;
            return h + 2f;
        }

        float yTop = _commandsList.headerHeight + topPad;
        for (int i = 0; i < index; i++)
            yTop += GetRowHWithPad(i);

        float rowH = GetRowHWithPad(index);
        float yCenter = yTop + (rowH * 0.5f);

        float viewH = _commandsViewportRect.height > 1f ? _commandsViewportRect.height : _commandsViewportHeight;
        viewH = Mathf.Max(120f, viewH);

        float nextY = yCenter - (viewH * 0.5f);

        float contentH = Mathf.Max(1f, _commandsList.GetHeight());
        float maxScroll = Mathf.Max(0f, contentH - viewH);
        nextY = Mathf.Clamp(nextY, 0f, maxScroll);

        const float deadband = 0.5f;
        if (Mathf.Abs(_commandsScroll.y - nextY) > deadband)
            _commandsScroll.y = nextY;
    }

    private void RequestScrollToCommand(int index, bool repaint = true)
    {
        _scrollToCommandIndex = true;
        _scrollTargetCommandIndex = index;

        if (repaint)
            Repaint();
    }
}
#endif