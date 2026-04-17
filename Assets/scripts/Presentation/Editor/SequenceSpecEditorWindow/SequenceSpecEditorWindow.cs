#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// =================================================================================================
// SequenceSpecEditorWindow (EditorWindow) — 역할/책임/확장 포인트 가이드
//
// 이 파일은 "SequenceSpecSO를 편집하는 메인 에디터 창의 뼈대"다.
// - Unity EditorWindow 생명주기(OnEnable/OnDisable/OnGUI/OnSelectionChange)와
// - 대상 SO 바인딩(SerializedObject/SerializedProperty) 및
// - 3열 네비게이션(노드/스텝/커맨드) + 트랙 탭 UI 상태,
// - 타입 캐시/단축키/컴파일 트리거 같은 '전역 공통 동작'을 한 곳에서 관리한다.
//
// 이 클래스에서 하는 일 (큰 책임)
// 1) 대상 시퀀스 바인딩
//    - targetSequence(SequenceSpecSO)를 선택/할당하면 _so(SerializedObject)와 주요 프로퍼티(_sequenceKeyProp/_nodesProp)를 구성.
//    - RebuildIfNeeded(force)로 리스트/프로퍼티 경로를 최신 상태로 유지.
//
// 2) UI 프레임(레이아웃) + 네비게이션의 권위자
//    - 좌측: Nodes 패널, 우측: Steps/Commands/Compiled 등 패널을 배치.
//    - _navColumn(Nodes/Steps/Commands), _selectedNode/_selectedStep, 스크롤 위치 등 "현재 사용자가 어디를 보고/선택했는지" 상태를 들고 있음.
//
// 3) 트랙 기반 커맨드 편집 상태 관리
//    - _activeTrack + TrackTabs/TrackFieldNames/TrackTypes로 "현재 트랙(Interaction/Setup/Motion/Dialogue/FX)"을 결정.
//    - 트랙별로 commands 리스트를 바인딩하는 경로(_commandsPropPath)를 결정하는 기반 데이터가 여기 있음.
//
// 4) 단축키/삭제/클립보드 같은 '전역 입력' 처리
//    - HandleArrowNavigation(), HandleDeleteByActiveColumnShortcut(), HandleGlobalCommandDeleteShortcut()
//      같은 전역 단축키 처리 진입점이 OnGUI 루프에서 호출됨.
//    - CommandClipboardPrefix/StepClipboardPrefix/NodeClipboardPrefix 같은 클립보드 포맷 상수는 Clipboard partial에 있음.
//
// 5) 변경 적용 & 컴파일 트리거
//    - _so.ApplyModifiedProperties()가 true면 EditorUtility.SetDirty(targetSequence) 후 ForceCompileAll() 호출.
//    - 즉, "편집기에서 값이 바뀌면 컴파일을 갱신"하는 정책을 이 클래스가 결정한다.
//
// -------------------------------------------------------------------------------------------------
// "뭘 건드리고 싶을 때 어디를 보면 되나" (빠른 찾아보기)
//
// A) 창의 기본 동작/생명주기/초기화 순서 바꾸기
//    → OnEnable / OnDisable / OnSelectionChange / OnGUI
//      - 초기 세팅(캐시, 프리퍼런스 로드, 리빌드, 폴드아웃 로드) 순서 조정도 여기.
//
// B) 레이아웃(좌/우 비율, 최소 크기, 스크롤, 패널 배치) 바꾸기
//    → OnGUI의 _nodesW/_stepsW 계산, DrawToolbar(), DrawHeader(),
//      DrawNodesPanel(), DrawRightPanel() 호출 순서/구성
//
// C) 단축키/키보드 네비게이션/삭제 정책 바꾸기
//    → ToolbarShortcutsTooltip(문구), HandleArrowNavigation(),
//      HandleDeleteByActiveColumnShortcut(), HandleGlobalCommandDeleteShortcut(),
//      + _navColumn / _lastCommandNavDelta / _isDraggingSteps 등 상태 필드
//
// D) "트랙"을 추가/삭제/이름 변경하거나 트랙 필드 연결 바꾸기
//    → TrackTabs / TrackFieldNames / TrackTypes
//      - TrackFieldNames는 실제 SerializedProperty 필드명(예: "dialogue")과 1:1로 맞아야 함.
//      - TrackToIndex / IndexToTrack은 탭 인덱스 매핑.
//      - OnEnable에서 length mismatch 체크가 터지면 여기부터 확인.
//
// E) 선택 상태(노드/스텝/커맨드)와 바인딩 경로가 꼬일 때
//    → _selectedNode / _selectedStep / _stepsPropPath / _commandsPropPath
//      + RebuildIfNeeded(force) 구현 쪽(다른 partial)에서 경로 구성 로직 확인.
//
// F) 커맨드 타입 목록(추가 메뉴/Quick Add에 뜨는 타입) 바꾸기
//    → CacheCommandTypes()
//      - TypeCache.GetTypesDerivedFrom<CommandSpecBase>()로 수집하며
//        abstract/generic 제외 + 이름 정렬.
//      - 필터링/정렬/그룹핑을 바꾸고 싶으면 여기.
//
// G) 자동 컴파일 정책을 바꾸기(너무 자주 컴파일된다/조건부로 하고 싶다)
///   → OnGUI 마지막의 ApplyModifiedProperties() 처리 블록 + ForceCompileAll()
//      - 예: 특정 변경에서만 컴파일, 디바운스, 예외 처리 정책 강화 등.
//
// H) 멀티 삭제(체크박스 기반 삭제 UX) 관련 손보기
//    → _deleteMultiMode / _checkedNodes
//
// -------------------------------------------------------------------------------------------------
// 이 파일이 "하지 않는 일" (다른 partial에서 찾을 확률이 큼)
//
// - DrawToolbar/DrawHeader/DrawNodesPanel/DrawRightPanel 구현 상세
// - ReorderableList(_nodesList/_stepsList/_commandsList) 생성/바인딩/드로잉
// - Foldout 저장/로드 구현(LoadFoldouts/SaveFoldouts)
// - Preferences 저장/로드(LoadPreferences/SaveRoleSlotsPrefs 등)
// - 실제 "노드/스텝/커맨드" CRUD/복사/붙여넣기/중복 로직
//
// 위 내용들은 보통 같은 클래스의 다른 partial 파일로 빠져 있을 가능성이 높다.
//
// -------------------------------------------------------------------------------------------------
// 유지보수 팁(이 클래스의 안정성 포인트)
// - TrackFieldNames/TrackTypes/TrackTabs는 '동일 개수 + 동일 순서'가 핵심 계약.
// - OnGUI는 "전역 입력 처리 → 대상 체크 → _so.Update → 그리기 → ApplyModifiedProperties → 컴파일"
//   순서를 유지하면 버그가 덜 난다.
// - selection/propPath는 RebuildIfNeeded()에서만 갱신되도록 '권위'를 단일화하면 꼬임이 줄어든다.
// =================================================================================================
public sealed partial class SequenceSpecEditorWindow : EditorWindow
{
    [MenuItem("Tools/Sequence/Sequence Editor")]
    public static void Open()
    {
        var w = GetWindow<SequenceSpecEditorWindow>();
        w.titleContent = new GUIContent("SequenceSpec Editor");
        w.Show();
    }

    // ------------------------------
    // Constants
    // ------------------------------
    private const string ToolbarShortcutsTooltip =
        "Shortcuts\n" +
        "--------------------------------\n" +
        "Arrow Keys : Navigate column/list\n" +
        "  - Left/Right : Move column\n" +
        "  - Up/Down    : Move selection\n" +
        "\n" +
        "Track (Commands)\n" +
        "  - Left/Right : Move track (Interaction/Setup/Motion/Dialogue/FX)\n" +
        "  - Note: Move to Steps column only when track is at the left-most.\n" +
        "\n" +
        "Enter : Quick Add\n" +
        "  - Commands : +Command (open add menu)\n" +
        "\n" +
        "Space : Toggle foldout\n" +
        "  - Commands : Toggle selected command\n" +
        "Shift+Space  : Toggle all foldouts (current list)\n" +
        "\n" +
        "Space : Quick Add\n" +
        "  - Nodes    : +Node\n" +
        "  - Steps    : +Step\n" +
        "\n" +
        "F2 : Rename label\n" +
        "  - Nodes : Rename selected Node label\n" +
        "  - Steps : Rename selected Step label\n" +
        "\n" +
        "] : Apply Gate Defaults to current Step\n" +
        "\n" +
        "Delete (Commands) : Delete selected command\n" +
        "Backspace (Steps) : Delete selected step\n" +
        "\n" +
        "Role Slots (1~9)\n" +
        "  - 1~9         : Apply RoleKey slot to current Step (scope applies)\n" +
        "  - Shift + 1~9 : Set Auto slot index (auto-fill target)\n" +
        "\n" +
        "Ctrl/Cmd + C : Copy (Node/Step/Command)\n" +
        "Ctrl/Cmd + X : Cut  (Command)\n" +
        "Ctrl/Cmd + V : Paste (Node/Step/Command)\n" +
        "Ctrl/Cmd + D : Duplicate (Node/Step/Command)\n" +
        "\n" +
        "Ctrl/Cmd + E : Delete by column (Node/Step/Command)\n";

    private const int RoleSlotBaseCount = 1;
    private const int RoleSlotMaxCount = 9;

    // ------------------------------
    // Target Sequence
    // ------------------------------
    [SerializeField] private SequenceSpecSO targetSequence;
    private SerializedObject _so;
    private SerializedProperty _sequenceKeyProp;
    private SerializedProperty _nodesProp;

    // ------------------------------
    // Reorderable Lists
    // ------------------------------
    private ReorderableList _nodesList;
    private ReorderableList _stepsList;
    private ReorderableList _commandsList;

    // ------------------------------
    // Selection State
    // ------------------------------
    private int _selectedNode = -1;
    private int _selectedStep = -1;
    private string _stepsPropPath;
    private string _commandsPropPath;

    // ------------------------------
    // UI State
    // ------------------------------
    [SerializeField] private bool _shortcutsPopupOpen = false;
    private SearchField _searchField;
    private string _search = "";
    private Vector2 _nodesScroll;
    private Vector2 _compiledScroll;
    private bool _isDraggingSteps;
    private int _pendingCommandIndex = -1;
    private bool _scrollToCommandIndex;
    private int _scrollTargetCommandIndex = -1;
    
    private bool _compiledFoldout = true;
    private float _compiledHeight = 200f;
    
    private float _nodesW;
    private float _stepsW;
    
    private Vector2 _commandsScroll;
    private Rect _commandsViewportRect; // viewport 실측 저장
    private float _commandsViewportHeight = 260f;
    
    private bool _scrollToStepIndex;
    private int _scrollTargetStepIndex = -1;
    private float _stepsViewportHeight = 260f; // viewport 실축 저장

    // Step name rename (F2)
    private bool _requestFocusStepNameField;
    private const string StepNameFieldControl = "SeqEditor.StepName";

    // ------------------------------
    // Multi-Delete Mode
    // ------------------------------
    private bool _deleteMultiMode;
    private readonly HashSet<int> _checkedNodes = new();

    // ------------------------------
    // Navigation Column
    // ------------------------------
    private enum NavColumn
    {
        Nodes,
        Steps,
        Commands
    }

    [SerializeField] private NavColumn _navColumn = NavColumn.Nodes;

    // ------------------------------
    // Command Type Cache
    // ------------------------------
    private static List<Type> _cachedCommandTypes;

    // ------------------------------
    // Unity Lifecycle
    // ------------------------------
    private void OnEnable()
    {
        minSize = new Vector2(879f, 0f);
        wantsMouseMove = true;

        _searchField = new SearchField();
        CacheCommandTypes();

        LoadPreferences();
        RebuildIfNeeded(force: true);
        LoadFoldouts();
    }

    private void OnDisable()
    {
        SaveRoleSlotsPrefs();
        SaveFoldouts();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            SaveRoleSlotsPrefs();
            SaveFoldouts();
        }
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is SequenceSpecSO so)
        {
            targetSequence = so;
            RebuildIfNeeded(force: true);
            LoadFoldouts();
            Repaint();
        }
    }

    private void OnGUI()
    {
        _nodesW = Mathf.Clamp(position.width * 0.27f, 200f, 300f);
        _stepsW = Mathf.Clamp(position.width * 0.27f, 210f, 300f);

        DrawToolbar();
        HandleRoleSlotHotkeys();
        
        HandleArrowNavigation();
        HandleRenameShortcuts();
        HandleDeleteByActiveColumnShortcut();
        HandleApplyGateDefaultsShortcut(); // ] 키로 Gate Defaults 적용
        
        // Node 단축키 처리 (Ctrl+C/V/D)
        if (_nodesList != null && _navColumn == NavColumn.Nodes)
            HandleNodeShortcuts(_nodesProp);

        if (Event.current.type == EventType.MouseUp)
            _isDraggingSteps = false;

        if (targetSequence == null)
        {
            EditorGUILayout.HelpBox("Assign a SequenceSpecSO or select one in Project.", MessageType.Info);
            return;
        }

        RebuildIfNeeded(force: false);

        if (_so == null)
        {
            EditorGUILayout.HelpBox("Failed to create SerializedObject.", MessageType.Error);
            return;
        }

        _so.Update();

        HandleGlobalCommandDeleteShortcut();

        DrawHeader();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawNodesPanel();
            DrawRightPanel();
        }

        bool changed = _so.ApplyModifiedProperties();
        if (changed)
        {
            EditorUtility.SetDirty(targetSequence);
            ForceCompileAll();
        }
    }

    // ------------------------------
    // Utility Methods
    // ------------------------------
    private void ForceCompileAll()
    {
        // if (targetSequence == null) return;
        //
        // try
        // {
        //     targetSequence.CompileAllSteps();
        // }
        // catch
        // {
        //     // fallback: do nothing
        // }
    }

    private static void CacheCommandTypes()
    {
        if (_cachedCommandTypes != null) return;

        var types = TypeCache.GetTypesDerivedFrom<CommandSpecBase>();
        _cachedCommandTypes = types
            .Where(t => t != null && !t.IsAbstract && !t.IsGenericType)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
#endif