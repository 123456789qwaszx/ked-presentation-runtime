// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
//
// /// <summary>
// /// Step 편집 UI(오른쪽 패널) 전담 파트.
// /// 
// /// 역할
// /// - 현재 선택된 Step(StepSpec)의 디테일을 그린다:
// ///   - Step Label(editorName)
// ///   - GateToken(gate) 편집 UI + Default Gate 드롭다운 연동
// ///   - Track 탭(Interaction/Setup/Motion/Dialogue/FX) 전환
// ///   - 선택된 Track의 Commands 리스트(SerializeReference ManagedReference) 렌더링 및 단축키 처리
// /// 
// /// 여기를 보면 좋은 경우(무엇을 고칠 때?)
// /// - Gate UI 접기/펼치기 정책, 기본값 드롭다운 동작 수정
// /// - 트랙 탭 UX(전환 시 선택 유지/초기화, 스크롤 정책 등) 변경
// /// - Commands 리스트 표시 흐름(헤더 문구, 리스트 topY 캐시, 단축키 호출 타이밍) 조정
// /// - Compiled 미리보기 포맷/높이/경고 표시(drift/missing) 기준 변경
// /// - Compiled 항목 클릭 시 Jump 정책(어느 트랙으로 이동, 어느 인덱스를 선택) 변경
// /// 
// /// 관련 의존(다른 partial에서 제공)
// /// - EnsureCommandsList(), HandleCommandShortcuts(): 커맨드 리스트 구성/단축키
// /// - DrawGateHeaderRow_WithDefaultDropdown(), DrawGateInline(): Gate UI
// /// - BuildOriginMapForStep(), TryGetOrigin(), SummarizeCompiledLine(): Compiled ↔ Origin 매핑/표시
// /// - TrackToIndex()/IndexToTrack(): 탭 인덱스 변환
// /// 
// /// 성능 최적화:
// /// - GUIStyle 캐싱: 매 프레임 생성 방지
// /// - Color 상수 캐싱: 반복적인 Color 생성 방지
// /// - SerializedProperty 조회 최소화: tracks 프로퍼티 한 번만 조회
// /// </summary>
// public sealed partial class SequenceSpecEditorWindow
// {
//     private static GUIStyle _cachedCompiledLabelStyle;
//     private static GUIStyle _cachedCompiledWarningStyle;
//
//     private static readonly Color _warningColorPro = new Color(1f, 0.78f, 0.25f);
//     private static readonly Color _warningColorLight = new Color(0.65f, 0.35f, 0.0f);
//
//     private void DrawCommandsSingleColumn(SerializedProperty stepProp)
//     {
//         if (stepProp == null)
//         {
//             EditorGUILayout.HelpBox("Step is null.", MessageType.Warning);
//             return;
//         }
//
//         var commandsProp = FindUnifiedCommandsProp(stepProp);
//         if (commandsProp == null || !commandsProp.isArray)
//         {
//             EditorGUILayout.HelpBox(
//                 "Unified commands list not found. Expected StepSpec.tracks.commands or StepSpec.compiled.",
//                 MessageType.Warning);
//             return;
//         }
//
//         EnsureCommandsList(commandsProp);
//         HandleCommandShortcuts(commandsProp);
//
//         if (_commandsList != null)
//             _commandsList.DoLayoutList();
//     }
//
//     private void DrawCompiledPreview(SerializedProperty stepProp)
//     {
//         var compiledProp = stepProp.FindPropertyRelative("compiled");
//         if (compiledProp == null || !compiledProp.isArray)
//         {
//             EditorGUILayout.HelpBox(
//                 "StepSpec.compiled missing. (It should exist as [SerializeReference] List<CommandSpecBase> compiled)",
//                 MessageType.Warning);
//             return;
//         }
//
//         int count = compiledProp.arraySize;
//
//         using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
//         {
//             _compiledFoldout = EditorGUILayout.Foldout(
//                 _compiledFoldout,
//                 $"Compiled (Runtime Order)  ({count})",
//                 true);
//
//             if (!_compiledFoldout)
//             {
//                 if (count == 0)
//                     EditorGUILayout.LabelField("- empty -", EditorStyles.centeredGreyMiniLabel);
//                 return;
//             }
//
//             if (count == 0)
//             {
//                 EditorGUILayout.LabelField("- empty -", EditorStyles.centeredGreyMiniLabel);
//                 return;
//             }
//
//             var origin = BuildOriginMapForStep(stepProp);
//
//             var importedCompiledOnlyProp = stepProp.FindPropertyRelative("editorImportedCompiledOnly");
//             bool importedCompiledOnly = importedCompiledOnlyProp != null && importedCompiledOnlyProp.boolValue;
//
//             if (_cachedCompiledLabelStyle == null)
//                 _cachedCompiledLabelStyle = new GUIStyle(EditorStyles.label);
//
//             if (_cachedCompiledWarningStyle == null)
//             {
//                 _cachedCompiledWarningStyle = new GUIStyle(EditorStyles.label);
//                 _cachedCompiledWarningStyle.normal.textColor =
//                     EditorGUIUtility.isProSkin ? _warningColorPro : _warningColorLight;
//             }
//
//             using (var scroll = new EditorGUILayout.ScrollViewScope(_compiledScroll, GUILayout.Height(_compiledHeight)))
//             {
//                 _compiledScroll = scroll.scrollPosition;
//
//                 for (int i = 0; i < count; i++)
//                 {
//                     var el = compiledProp.GetArrayElementAtIndex(i);
//                     if (el == null)
//                         continue;
//
//                     string line = SummarizeCompiledLine(
//                         el,
//                         i,
//                         origin,
//                         importedCompiledOnly,
//                         out bool hasDrift,
//                         out bool missingOrigin);
//
//                     using (new EditorGUILayout.HorizontalScope())
//                     {
//                         var style = (hasDrift || missingOrigin)
//                             ? _cachedCompiledWarningStyle
//                             : _cachedCompiledLabelStyle;
//
//                         if (GUILayout.Button(line, style))
//                         {
//                             if (TryGetOrigin(el, origin, out var o))
//                                 JumpToOrigin(stepProp, o.index);
//                         }
//
//                         if (missingOrigin)
//                             GUILayout.Label("(! missing)", EditorStyles.miniLabel, GUILayout.Width(70));
//                         else if (hasDrift)
//                             GUILayout.Label("(! drift)", EditorStyles.miniLabel, GUILayout.Width(50));
//                     }
//                 }
//             }
//         }
//     }
//
//     private void JumpToOrigin(SerializedProperty stepProp, int index)
//     {
//         _navColumn = NavColumn.Commands;
//
//         _commandsList = null;
//         _commandsPropPath = null;
//
//         _pendingCommandIndex = Mathf.Max(0, index);
//
//         _scrollToCommandIndex = true;
//         _scrollTargetCommandIndex = _pendingCommandIndex;
//
//         Repaint();
//     }
//
//     private static SerializedProperty FindUnifiedCommandsProp(SerializedProperty stepProp)
//     {
//         if (stepProp == null)
//             return null;
//
//         var compiledProp = stepProp.FindPropertyRelative("compiled");
//         if (compiledProp != null && compiledProp.isArray)
//             return compiledProp;
//
//         return null;
//     }
// }
// #endif