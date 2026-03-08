#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Step 편집 UI(오른쪽 패널) 전담 파트.
/// 
/// 역할
/// - 현재 선택된 Step(StepSpec)의 디테일을 그린다:
///   - Step Label(editorName)
///   - GateToken(gate) 편집 UI + Default Gate 드롭다운 연동
///   - Track 탭(Interaction/Setup/Motion/Dialogue/FX) 전환
///   - 선택된 Track의 Commands 리스트(SerializeReference ManagedReference) 렌더링 및 단축키 처리
/// 
/// - Step의 "Compiled(런타임 실행 순서)" 결과를 미리보기로 보여준다:
///   - StepSpec.compiled 리스트를 스크롤 뷰로 출력
///   - OriginMap(트랙/인덱스 매핑)을 이용해 컴파일 결과가 어느 원본 커맨드에서 왔는지 추적
///   - drift/missing 표시로 "원본 ↔ 컴파일 결과" 불일치/유실을 시각적으로 경고
///   - 미리보기 항목 클릭 시 원본 커맨드 위치로 점프(트랙 전환 + 선택/스크롤 동기화)
/// 
/// 여기를 보면 좋은 경우(무엇을 고칠 때?)
/// - Step 디테일 UI 레이아웃/필드 추가(예: Step 메타, 주석, 태그 등)
/// - Gate UI 접기/펼치기 정책, 기본값 드롭다운 동작 수정
/// - 트랙 탭 UX(전환 시 선택 유지/초기화, 스크롤 정책 등) 변경
/// - Commands 리스트 표시 흐름(헤더 문구, 리스트 topY 캐시, 단축키 호출 타이밍) 조정
/// - Compiled 미리보기 포맷/높이/경고 표시(drift/missing) 기준 변경
/// - Compiled 항목 클릭 시 Jump 정책(어느 트랙으로 이동, 어느 인덱스를 선택) 변경
/// 
/// 관련 의존(다른 partial에서 제공)
/// - EnsureCommandsList(), HandleCommandShortcuts(): 커맨드 리스트 구성/단축키
/// - DrawGateHeaderRow_WithDefaultDropdown(), DrawGateInline(): Gate UI
/// - BuildOriginMapForStep(), TryGetOrigin(), SummarizeCompiledLine(): Compiled ↔ Origin 매핑/표시
/// - TrackToIndex()/IndexToTrack(): 탭 인덱스 변환
/// 
/// 성능 최적화:
/// - GUIStyle 캐싱: 매 프레임 생성 방지
/// - Color 상수 캐싱: 반복적인 Color 생성 방지
/// - SerializedProperty 조회 최소화: tracks 프로퍼티 한 번만 조회
/// </summary>
public sealed partial class SequenceSpecEditorWindow
{
    // ------------------------------
    // Cached styles for performance
    // ------------------------------
    private static GUIStyle _cachedTabButtonStyle;
    private static GUIStyle _cachedTabCountStyle;
    private static GUIStyle _cachedCompiledLabelStyle;
    private static GUIStyle _cachedCompiledWarningStyle;

    // ------------------------------
    // Cached colors for performance
    // ------------------------------
    private static readonly Color _inactiveOverlayPro = new Color(0f, 0f, 0f, 0.18f);
    private static readonly Color _inactiveOverlayLight = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color _emptyOverlayPro = new Color(0f, 0f, 0f, 0.28f);
    private static readonly Color _emptyOverlayLight = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color _selectedOverlayPro = new Color(0.22f, 0.48f, 0.92f, 1f);
    private static readonly Color _selectedOverlayLight = new Color(0.22f, 0.48f, 0.92f, 1f);
    private static readonly Color _indicatorColorPro = new Color(0.25f, 0.55f, 1.0f, 1f);
    private static readonly Color _indicatorColorLight = new Color(0.15f, 0.40f, 0.95f, 1f);
    private static readonly Color _warningColorPro = new Color(1f, 0.78f, 0.25f);
    private static readonly Color _warningColorLight = new Color(0.65f, 0.35f, 0.0f);

    private void DrawTrackTabs()
    {
        int current = TrackToIndex(_activeTrack);
        bool tracksActive = (_navColumn == NavColumn.Commands);

        // "비활성일 때 확 연하게"
        float inactiveAlpha = 0.28f;
        float activeAlpha = 1.0f;

        // 현재 Step에서 각 트랙별 count를 구한다
        var stepProp = GetCurrentStepProp();
        var tracksProp = stepProp?.FindPropertyRelative("tracks"); // 한 번만 조회

        // ProSkin 여부 캐싱
        bool isPro = EditorGUIUtility.isProSkin;

        using (new EditorGUILayout.HorizontalScope())
        {
            for (int i = 0; i < TrackTabs.Length; i++)
            {
                var track = IndexToTrack(i);
                bool isSelected = (i == current);

                // tracks 프로퍼티 재사용
                int count = GetTrackCommandCountCached(tracksProp, track);
                bool empty = (count <= 0);

                // 1) 탭 Rect 확보 (버튼 라벨은 "트랙 이름만")
                var tabText = TrackTabs[i];
                var rect = GUILayoutUtility.GetRect(
                    new GUIContent(tabText),
                    EditorStyles.toolbarButton,
                    GUILayout.Height(22f)
                );

                // 2) 배경 처리 (Repaint에서만)
                if (Event.current.type == EventType.Repaint)
                {
                    // 전체 디샛(컬럼 비활성) - 캐싱된 Color 사용
                    if (!tracksActive)
                    {
                        EditorGUI.DrawRect(rect, isPro ? _inactiveOverlayPro : _inactiveOverlayLight);
                    }

                    // empty면 추가로 더 눌러서 "거의 회색" - 캐싱된 Color 사용
                    if (empty)
                    {
                        EditorGUI.DrawRect(rect, isPro ? _emptyOverlayPro : _emptyOverlayLight);
                    }

                    // 선택 강조 + 하단 인디케이터
                    if (isSelected)
                    {
                        float a = tracksActive ? 0.18f : 0.08f;
                        if (empty) a *= 0.55f;

                        // 캐싱된 Color에 알파만 조정
                        var baseColor = isPro ? _selectedOverlayPro : _selectedOverlayLight;
                        var overlayColor = baseColor;
                        overlayColor.a = isPro ? a : a * 0.9f;

                        EditorGUI.DrawRect(rect, overlayColor);

                        DrawSelectedTabIndicator(rect, strong: tracksActive && !empty);
                    }
                }

                // 3) 텍스트 알파/굵기 - GUIStyle 캐싱
                float alpha = tracksActive ? activeAlpha : inactiveAlpha;
                if (isSelected) alpha = Mathf.Max(alpha, tracksActive ? 1.0f : 0.55f);
                if (empty) alpha *= 0.35f;

                int tabLeftPad = 3;
                
                if (_cachedTabButtonStyle == null)
                {
                    _cachedTabButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                    _cachedTabButtonStyle.alignment = TextAnchor.MiddleLeft;
                    
                    _cachedTabButtonStyle.padding.left = tabLeftPad;
                }

                var style = _cachedTabButtonStyle;
                style.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;

                Color baseText = style.normal.textColor;
                if (baseText.a <= 0f) baseText = EditorStyles.label.normal.textColor;

                Color t = baseText;
                t.a = Mathf.Clamp01(alpha);

                style.normal.textColor = t;
                style.hover.textColor = t;
                style.active.textColor = t;
                style.focused.textColor = t;

                // 4) 버튼 처리
                bool canClick = true;

                using (new EditorGUI.DisabledScope(!canClick))
                {
                    if (GUI.Button(rect, tabText, style))
                    {
                        if (i != current)
                        {
                            _navColumn = NavColumn.Commands;

                            _activeTrack = track;

                            _commandsList = null;
                            _commandsPropPath = null;

                            Repaint();
                        }
                    }
                }

                // count는 우측에 미니 라벨로 오버레이 (버튼 밖이 아니라 rect 안에)
                if (Event.current.type == EventType.Repaint)
                {
                    // GUIStyle 캐싱
                    if (_cachedTabCountStyle == null)
                    {
                        _cachedTabCountStyle = new GUIStyle(EditorStyles.miniLabel);
                        _cachedTabCountStyle.alignment = TextAnchor.MiddleRight;
                    }

                    var countStyle = _cachedTabCountStyle;

                    var ct = countStyle.normal.textColor;
                    ct.a = empty ? 0.18f : (tracksActive ? 0.65f : 0.35f);
                    countStyle.normal.textColor = ct;

                    var countRect = rect;
                    countRect.xMin += 4f;
                    countRect.xMax -= 1f;

                    GUI.Label(countRect, count.ToString(), countStyle);
                }
            }
        }
    }
    
    private void DrawCompiledPreview(SerializedProperty stepProp)
    {
        var compiledProp = stepProp.FindPropertyRelative("compiled");
        if (compiledProp == null || !compiledProp.isArray)
        {
            EditorGUILayout.HelpBox(
                "StepSpec.compiled missing. (It should exist as [SerializeReference] List<CommandSpecBase> compiled)",
                MessageType.Warning);
            return;
        }

        int count = compiledProp.arraySize;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _compiledFoldout = EditorGUILayout.Foldout(
                _compiledFoldout,
                $"Compiled (Runtime Order)  ({count})",
                true);

            if (!_compiledFoldout)
            {
                if (count == 0)
                    EditorGUILayout.LabelField("— empty —", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (count == 0)
            {
                EditorGUILayout.LabelField("— empty —", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var origin = BuildOriginMapForStep(stepProp);

            // GUIStyle 캐싱 - 루프 밖에서 준비
            if (_cachedCompiledLabelStyle == null)
                _cachedCompiledLabelStyle = new GUIStyle(EditorStyles.label);

            if (_cachedCompiledWarningStyle == null)
            {
                _cachedCompiledWarningStyle = new GUIStyle(EditorStyles.label);
                _cachedCompiledWarningStyle.normal.textColor = 
                    EditorGUIUtility.isProSkin ? _warningColorPro : _warningColorLight;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_compiledScroll, GUILayout.Height(_compiledHeight)))
            {
                _compiledScroll = scroll.scrollPosition;

                for (int i = 0; i < count; i++)
                {
                    var el = compiledProp.GetArrayElementAtIndex(i);
                    if (el == null) continue;

                    string line = SummarizeCompiledLine(el, i, origin, out bool hasDrift, out bool missingOrigin);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // 캐싱된 스타일 재사용
                        var style = (hasDrift || missingOrigin) ? _cachedCompiledWarningStyle : _cachedCompiledLabelStyle;

                        if (GUILayout.Button(line, style))
                        {
                            if (TryGetOrigin(el, origin, out var o))
                                JumpToOrigin(stepProp, o.track, o.index);
                        }

                        if (missingOrigin)
                            GUILayout.Label("(! missing)", EditorStyles.miniLabel, GUILayout.Width(70));
                        else if (hasDrift)
                            GUILayout.Label("(! drift)", EditorStyles.miniLabel, GUILayout.Width(50));
                    }
                }
            }
        }
    }

    private void JumpToOrigin(SerializedProperty stepProp, CommandTrackType track, int index)
    {
        _navColumn = NavColumn.Commands;

        _activeTrack = track;

        _commandsList = null;
        _commandsPropPath = null;

        _pendingCommandIndex = Mathf.Max(0, index);

        _scrollToCommandIndex = true;
        _scrollTargetCommandIndex = _pendingCommandIndex;

        Repaint();
    }
    

    private static int GetTrackCommandCountCached(SerializedProperty tracksProp, CommandTrackType track)
    {
        if (tracksProp == null) return 0;

        SerializedProperty listProp = track switch
        {
            CommandTrackType.Interaction => tracksProp.FindPropertyRelative("interaction"),
            CommandTrackType.Setup => tracksProp.FindPropertyRelative("setup"),
            CommandTrackType.Motion => tracksProp.FindPropertyRelative("motion"),
            CommandTrackType.Dialogue => tracksProp.FindPropertyRelative("dialogue"),
            CommandTrackType.FX => tracksProp.FindPropertyRelative("fx"),
            _ => null
        };

        return (listProp != null && listProp.isArray) ? listProp.arraySize : 0;
    }

    private static SerializedProperty FindTrackListProp(SerializedProperty stepProp, CommandTrackType track)
    {
        var tracks = stepProp.FindPropertyRelative("tracks");
        if (tracks == null) return null;

        switch (track)
        {
            case CommandTrackType.Interaction: return tracks.FindPropertyRelative("interaction");
            case CommandTrackType.Setup:       return tracks.FindPropertyRelative("setup");
            case CommandTrackType.Motion:      return tracks.FindPropertyRelative("motion");
            case CommandTrackType.Dialogue:    return tracks.FindPropertyRelative("dialogue");
            case CommandTrackType.FX:          return tracks.FindPropertyRelative("fx");
            default: return null;
        }
    }

    private static void DrawSelectedTabIndicator(Rect rect, bool strong)
    {
        if (Event.current.type != EventType.Repaint) return;

        float h = strong ? 3f : 2f;
        float padX = 6f;
        var bar = new Rect(rect.x + padX, rect.yMax - h, rect.width - padX * 2f, h);

        // 캐싱된 Color 사용
        bool isPro = EditorGUIUtility.isProSkin;
        var baseColor = isPro ? _indicatorColorPro : _indicatorColorLight;
        var c = baseColor;
        c.a = strong ? (isPro ? 0.95f : 0.90f) : (isPro ? 0.75f : 0.70f);

        EditorGUI.DrawRect(bar, c);
    }
}
#endif