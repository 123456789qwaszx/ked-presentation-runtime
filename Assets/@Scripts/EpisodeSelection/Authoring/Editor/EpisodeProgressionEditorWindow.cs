// ============================================================
// EpisodeProgressionEditorWindow.cs
// 경로: Assets/_Project/Scripts/VN/Episode/Progression/Editor/
//
// 메뉴: Tools / VN / Episode Progression Editor
//
// 레이아웃:
//   상단 툴바 : SO 선택 / Create / Validate / Auto-Fill
//   좌측 패널 : Node 목록 (Add / Duplicate / Delete)
//   중앙 패널 : 선택된 Node 상세 편집
//   우측 패널 : EndingRules / Validation 결과
//
// 안정화 버전:
//   - SerializedProperty arraySize 방어
//   - 선택 index 보정
//   - GUILayout Begin/End 불일치 방지
//   - null SerializedProperty 방어
//   - 리스트 삭제 지연 처리
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class EpisodeProgressionEditorWindow : EditorWindow
{
    // ─── 상수 ───────────────────────────────────────────────

    private const float LEFT_PANEL_WIDTH  = 220f;
    private const float RIGHT_PANEL_WIDTH = 280f;
    private const float TOOLBAR_HEIGHT    = 36f;
    private const float SECTION_GAP       = 6f;
    private const float NODE_ROW_HEIGHT   = 28f;
    private const float LABEL_W           = 140f;

    private const string NONE_LABEL = "(none)";

    // ─── 상태 ───────────────────────────────────────────────

    private ChapterEpisodeProgressionSO _target;
    private SerializedObject _serializedObject;

    private int _selectedNodeIndex = -1;
    private int _selectedEndingIndex = -1;

    private Vector2 _leftScroll;
    private Vector2 _centerScroll;
    private Vector2 _rightScroll;
    private Vector2 _validationScroll;

    private EpisodeProgressionValidationResult _lastValidationResult;
    private bool _validationDirty;

    // 조건 편집 펼침 상태
    private bool _foldVisible;
    private bool _foldUnlock;
    private bool _foldNext;
    private bool _foldAttachments;

    // ─── GUI 스타일 ─────────────────────────────────────────

    private GUIStyle _headerStyle;
    private GUIStyle _sectionStyle;
    private GUIStyle _nodeRowStyle;
    private GUIStyle _nodeRowSelectedStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _infoStyle;
    private bool _stylesInitialized;

    // ─── 메뉴 진입점 ────────────────────────────────────────

    [MenuItem("Tools/VN/Episode Progression Editor")]
    public static void Open()
    {
        EpisodeProgressionEditorWindow window =
            GetWindow<EpisodeProgressionEditorWindow>("Episode Progression");

        window.minSize = new Vector2(900f, 580f);
        window.Show();
    }

    // ─── Unity 이벤트 ───────────────────────────────────────

    private void OnGUI()
    {
        EnsureStyles();

        DrawToolbar();

        if (_target == null)
        {
            EditorGUILayout.HelpBox("Select or create a ChapterEpisodeProgressionSO.", MessageType.Info);
            return;
        }

        EnsureTargetLists();

        if (_serializedObject == null || _serializedObject.targetObject != _target)
            _serializedObject = new SerializedObject(_target);

        _serializedObject.Update();
        ClampSelectionIndices();

        Rect body = new Rect(0f, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);
        DrawBody(body);

        _serializedObject.ApplyModifiedProperties();

        if (_validationDirty)
        {
            _validationDirty = false;
            RunValidation();
        }
    }

    private void OnSelectionChange()
    {
        ChapterEpisodeProgressionSO selected =
            Selection.activeObject as ChapterEpisodeProgressionSO;

        if (selected != null && selected != _target)
            SetTarget(selected);
    }

    // ─── 툴바 ───────────────────────────────────────────────

    private void DrawToolbar()
    {
        Rect toolbarRect = new Rect(0f, 0f, position.width, TOOLBAR_HEIGHT);
        EditorGUI.DrawRect(toolbarRect, new Color(0.18f, 0.18f, 0.18f, 1f));

        using (new GUILayout.AreaScope(new Rect(8f, 6f, position.width - 16f, TOOLBAR_HEIGHT - 12f)))
        using (new GUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();

            ChapterEpisodeProgressionSO picked =
                (ChapterEpisodeProgressionSO)EditorGUILayout.ObjectField(
                    _target,
                    typeof(ChapterEpisodeProgressionSO),
                    false,
                    GUILayout.Width(280f));

            if (EditorGUI.EndChangeCheck())
                SetTarget(picked);

            if (GUILayout.Button("Create New", GUILayout.Width(90f)))
                CreateNewAsset();

            GUILayout.FlexibleSpace();

            if (_target == null)
                return;

            if (GUILayout.Button("Auto-Fill Parents", GUILayout.Width(120f)))
            {
                Undo.RecordObject(_target, "Auto-Fill Attachment Parents");
                EpisodeProgressionValidator.AutoFillAttachmentParents(_target);
                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                RunValidation();
            }

            Color oldBackground = GUI.backgroundColor;

            GUI.backgroundColor = _lastValidationResult != null && _lastValidationResult.HasErrors
                ? new Color(1f, 0.4f, 0.4f)
                : Color.white;

            if (GUILayout.Button("Validate", GUILayout.Width(80f)))
                RunValidation();

            GUI.backgroundColor = oldBackground;

            if (_lastValidationResult == null)
                return;

            int errors = _lastValidationResult.ErrorCount;
            int warnings = _lastValidationResult.WarningCount;

            string summary = errors > 0
                ? $"  ✕ {errors} error(s)"
                : warnings > 0
                    ? $"  ⚠ {warnings} warning(s)"
                    : "  ✓ OK";

            Color prevColor = GUI.contentColor;

            GUI.contentColor = errors > 0
                ? new Color(1f, 0.45f, 0.45f)
                : warnings > 0
                    ? new Color(1f, 0.85f, 0.3f)
                    : new Color(0.5f, 1f, 0.5f);

            GUILayout.Label(summary, GUILayout.Width(140f));

            GUI.contentColor = prevColor;
        }
    }

    // ─── 본문 ───────────────────────────────────────────────

    private void DrawBody(Rect body)
    {
        float centerW = body.width - LEFT_PANEL_WIDTH - RIGHT_PANEL_WIDTH;

        if (centerW < 200f)
            centerW = 200f;

        Rect leftRect = new Rect(
            body.x,
            body.y,
            LEFT_PANEL_WIDTH,
            body.height);

        Rect centerRect = new Rect(
            body.x + LEFT_PANEL_WIDTH,
            body.y,
            centerW,
            body.height);

        Rect rightRect = new Rect(
            body.x + LEFT_PANEL_WIDTH + centerW,
            body.y,
            RIGHT_PANEL_WIDTH,
            body.height);

        EditorGUI.DrawRect(new Rect(leftRect.xMax, body.y, 1f, body.height), new Color(0.1f, 0.1f, 0.1f));
        EditorGUI.DrawRect(new Rect(rightRect.x, body.y, 1f, body.height), new Color(0.1f, 0.1f, 0.1f));

        DrawLeftPanel(leftRect);
        DrawCenterPanel(centerRect);
        DrawRightPanel(rightRect);
    }

    // ─────────────────────────────────────────────────────────
    // 좌측: Node 목록
    // ─────────────────────────────────────────────────────────

    private void DrawLeftPanel(Rect rect)
    {
        using (new GUILayout.AreaScope(rect))
        using (new GUILayout.VerticalScope())
        {
            int nodeCount = GetNodeCount();

            EditorGUILayout.LabelField(
                $"Episode Nodes  ({nodeCount})",
                _headerStyle,
                GUILayout.Height(22f));

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+", GUILayout.Width(28f)))
                    AddNode();

                bool hasSelectedNode =
                    _selectedNodeIndex >= 0 &&
                    _selectedNodeIndex < GetNodeCount();

                GUI.enabled = hasSelectedNode;

                if (GUILayout.Button("Dup", GUILayout.Width(40f)))
                    DuplicateNode(_selectedNodeIndex);

                Color oldBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

                if (GUILayout.Button("Del", GUILayout.Width(36f)))
                {
                    EpisodeNodeDefinition selected = GetSelectedNode();

                    string id = selected != null ? selected.EpisodeId : "";
                    if (EditorUtility.DisplayDialog("Delete Node", $"Delete '{id}'?", "Delete", "Cancel"))
                        DeleteNode(_selectedNodeIndex);
                }

                GUI.backgroundColor = oldBackground;
                GUI.enabled = true;
            }

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_leftScroll, GUILayout.ExpandHeight(true)))
            {
                _leftScroll = scroll.scrollPosition;

                if (_target == null || _target.Nodes == null)
                    return;

                for (int i = 0; i < _target.Nodes.Count; i++)
                {
                    EpisodeNodeDefinition node = _target.Nodes[i];

                    if (node == null)
                    {
                        DrawNodeRow(i, $"[{i}] (null)", false);
                        continue;
                    }

                    bool selected = i == _selectedNodeIndex;

                    string label = string.IsNullOrWhiteSpace(node.EpisodeId)
                        ? $"[{i}] (empty id)"
                        : $"[{i}] {node.EpisodeId}";

                    if (node.IsChapterEndingCandidate)
                        label += " ★";

                    DrawNodeRow(i, label, selected);
                }
            }
        }
    }

    private void DrawNodeRow(int index, string label, bool selected)
    {
        GUIStyle rowStyle = selected ? _nodeRowSelectedStyle : _nodeRowStyle;

        Rect rowRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            rowStyle,
            GUILayout.Height(NODE_ROW_HEIGHT));

        if (GUI.Button(rowRect, label, rowStyle))
            SelectNode(index);
    }

    // ─────────────────────────────────────────────────────────
    // 중앙: 선택된 Node 상세
    // ─────────────────────────────────────────────────────────

    private void DrawCenterPanel(Rect rect)
    {
        using (new GUILayout.AreaScope(rect))
        using (new GUILayout.VerticalScope())
        {
            if (_target == null || _serializedObject == null)
            {
                EditorGUILayout.HelpBox("No target selected.", MessageType.Info);
                return;
            }

            SerializedProperty nodesProp = _serializedObject.FindProperty("Nodes");

            if (nodesProp == null || !nodesProp.isArray)
            {
                EditorGUILayout.HelpBox("Serialized property 'Nodes' was not found or is not an array.", MessageType.Error);
                return;
            }

            if (_selectedNodeIndex < 0 || _selectedNodeIndex >= _target.Nodes.Count)
            {
                EditorGUILayout.HelpBox("← Select a node to edit.", MessageType.None);
                return;
            }

            if (_selectedNodeIndex >= nodesProp.arraySize)
            {
                _selectedNodeIndex = Mathf.Clamp(_selectedNodeIndex, -1, nodesProp.arraySize - 1);
                EditorGUILayout.HelpBox("Selected node index was out of sync. Select a node again.", MessageType.Warning);
                return;
            }

            SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(_selectedNodeIndex);

            if (nodeProp == null)
            {
                EditorGUILayout.HelpBox("Selected node property is null.", MessageType.Warning);
                return;
            }

            EpisodeNodeDefinition node = _target.Nodes[_selectedNodeIndex];

            if (node == null)
            {
                EditorGUILayout.HelpBox("Selected node data is null.", MessageType.Warning);
                return;
            }

            EnsureNodeLists(node);

            EditorGUILayout.LabelField("Node Detail", _headerStyle, GUILayout.Height(22f));

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_centerScroll, GUILayout.ExpandHeight(true)))
            {
                _centerScroll = scroll.scrollPosition;

                EditorGUI.BeginChangeCheck();

                DrawSectionLabel("Identity");

                DrawPropField(nodeProp, "EpisodeId", "Episode ID");
                DrawPropField(nodeProp, "Title", "Title");
                DrawPropField(nodeProp, "IndexText", "Index Text");
                DrawPropField(nodeProp, "Kind", "Kind");
                DrawPropField(nodeProp, "DialogueEntryId", "Dialogue Entry ID");
                DrawPropField(nodeProp, "DesignerNote", "Designer Note");

                EditorGUILayout.Space(SECTION_GAP);

                DrawSectionLabel("Ending");

                DrawPropField(nodeProp, "IsChapterEndingCandidate", "Is Chapter Ending Candidate");

                if (node.IsChapterEndingCandidate)
                    DrawEndingKeyPopup(nodeProp, node);

                EditorGUILayout.Space(SECTION_GAP);

                _foldVisible = EditorGUILayout.Foldout(
                    _foldVisible,
                    $"Visible Conditions  ({node.VisibleConditions.Count})",
                    true);

                if (_foldVisible)
                {
                    DrawConditionList(
                        nodeProp.FindPropertyRelative("VisibleConditions"),
                        node.VisibleConditions,
                        "VisibleConditions");
                }

                _foldUnlock = EditorGUILayout.Foldout(
                    _foldUnlock,
                    $"Unlock Conditions  ({node.UnlockConditions.Count})",
                    true);

                if (_foldUnlock)
                {
                    DrawConditionList(
                        nodeProp.FindPropertyRelative("UnlockConditions"),
                        node.UnlockConditions,
                        "UnlockConditions");
                }

                EditorGUILayout.Space(SECTION_GAP);

                _foldNext = EditorGUILayout.Foldout(
                    _foldNext,
                    $"Next Options  ({node.NextOptions.Count})",
                    true);

                if (_foldNext)
                {
                    DrawNextOptionList(
                        nodeProp.FindPropertyRelative("NextOptions"),
                        node.NextOptions,
                        node.EpisodeId);
                }

                EditorGUILayout.Space(SECTION_GAP);

                _foldAttachments = EditorGUILayout.Foldout(
                    _foldAttachments,
                    $"Attachments  ({node.Attachments.Count})",
                    true);

                if (_foldAttachments)
                {
                    DrawAttachmentList(
                        nodeProp.FindPropertyRelative("Attachments"),
                        node.Attachments,
                        node.EpisodeId);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_target);
                    _validationDirty = true;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // 우측: EndingRules + Validation 결과
    // ─────────────────────────────────────────────────────────

    private void DrawRightPanel(Rect rect)
    {
        using (new GUILayout.AreaScope(rect))
        using (new GUILayout.VerticalScope())
        {
            float halfH = rect.height * 0.45f;

            EditorGUILayout.LabelField("Ending Rules", _headerStyle, GUILayout.Height(22f));

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Ending Rule", GUILayout.ExpandWidth(true)))
                    AddEndingRule();

                bool hasSelectedEnding =
                    _selectedEndingIndex >= 0 &&
                    _selectedEndingIndex < GetEndingCount();

                GUI.enabled = hasSelectedEnding;

                Color oldBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

                if (GUILayout.Button("Del", GUILayout.Width(36f)))
                {
                    ChapterEndingRule selected = GetSelectedEndingRule();

                    string key = selected != null ? selected.EndingKey : "";
                    if (EditorUtility.DisplayDialog("Delete Ending Rule", $"Delete '{key}'?", "Delete", "Cancel"))
                        DeleteEndingRule(_selectedEndingIndex);
                }

                GUI.backgroundColor = oldBackground;
                GUI.enabled = true;
            }

            SerializedProperty endingsProp = _serializedObject != null
                ? _serializedObject.FindProperty("EndingRules")
                : null;

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_rightScroll, GUILayout.Height(halfH)))
            {
                _rightScroll = scroll.scrollPosition;

                if (_target != null && _target.EndingRules != null)
                {
                    for (int i = 0; i < _target.EndingRules.Count; i++)
                    {
                        ChapterEndingRule rule = _target.EndingRules[i];

                        bool selected = i == _selectedEndingIndex;
                        GUIStyle rowStyle = selected ? _nodeRowSelectedStyle : _nodeRowStyle;

                        string label = rule == null || string.IsNullOrWhiteSpace(rule.EndingKey)
                            ? $"[{i}] (empty)"
                            : $"[{i}] {rule.EndingKey}";

                        Rect rowRect = GUILayoutUtility.GetRect(
                            GUIContent.none,
                            rowStyle,
                            GUILayout.Height(NODE_ROW_HEIGHT));

                        if (GUI.Button(rowRect, label, rowStyle))
                            _selectedEndingIndex = i;
                    }
                }
            }

            DrawSelectedEndingRule(endingsProp);

            EditorGUILayout.Space(SECTION_GAP);

            EditorGUILayout.LabelField("Validation", _headerStyle, GUILayout.Height(22f));

            using (GUILayout.ScrollViewScope scroll = new GUILayout.ScrollViewScope(_validationScroll, GUILayout.ExpandHeight(true)))
            {
                _validationScroll = scroll.scrollPosition;
                DrawValidationResult();
            }
        }
    }

    private void DrawSelectedEndingRule(SerializedProperty endingsProp)
    {
        if (_target == null || _target.EndingRules == null)
            return;

        if (endingsProp == null || !endingsProp.isArray)
            return;

        if (_selectedEndingIndex < 0)
            return;

        if (_selectedEndingIndex >= _target.EndingRules.Count || _selectedEndingIndex >= endingsProp.arraySize)
        {
            _selectedEndingIndex = -1;
            return;
        }

        ChapterEndingRule rule = _target.EndingRules[_selectedEndingIndex];

        if (rule == null)
        {
            EditorGUILayout.HelpBox("Selected EndingRule is null.", MessageType.Warning);
            return;
        }

        if (rule.Conditions == null)
            rule.Conditions = new List<EpisodeCondition>();

        SerializedProperty ruleProp = endingsProp.GetArrayElementAtIndex(_selectedEndingIndex);

        if (ruleProp == null)
            return;

        EditorGUI.BeginChangeCheck();

        DrawPropField(ruleProp, "EndingKey", "Ending Key");
        DrawPropField(ruleProp, "DisplayName", "Display Name");
        DrawPropField(ruleProp, "UnlockNextChapter", "Unlock Next Chapter");

        if (rule.UnlockNextChapter)
            DrawPropField(ruleProp, "NextChapterId", "Next Chapter ID");

        DrawPropField(ruleProp, "DesignerNote", "Designer Note");

        EditorGUILayout.Space(4f);

        SerializedProperty condProp = ruleProp.FindPropertyRelative("Conditions");
        DrawConditionList(condProp, rule.Conditions, "EndingConditions");

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_target);
            _validationDirty = true;
        }
    }

    private void DrawValidationResult()
    {
        if (_lastValidationResult == null)
        {
            EditorGUILayout.LabelField("Press 'Validate' to check.", EditorStyles.miniLabel);
            return;
        }

        if (_lastValidationResult.Issues.Count == 0)
        {
            GUILayout.Label("✓ No issues found.", _infoStyle);
            return;
        }

        for (int i = 0; i < _lastValidationResult.Issues.Count; i++)
        {
            EpisodeProgressionValidationIssue issue = _lastValidationResult.Issues[i];

            GUIStyle style = issue.Severity == EpisodeProgressionIssueSeverity.Error
                ? _errorStyle
                : issue.Severity == EpisodeProgressionIssueSeverity.Warning
                    ? _warningStyle
                    : _infoStyle;

            string prefix = issue.Severity == EpisodeProgressionIssueSeverity.Error
                ? "✕"
                : issue.Severity == EpisodeProgressionIssueSeverity.Warning
                    ? "⚠"
                    : "ℹ";

            string text = string.IsNullOrEmpty(issue.ContextId)
                ? $"{prefix} {issue.Message}"
                : $"{prefix} [{issue.ContextId}] {issue.Message}";

            GUILayout.Label(text, style);
        }
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: Condition 목록
    // ─────────────────────────────────────────────────────────

    private void DrawConditionList(
        SerializedProperty listProp,
        List<EpisodeCondition> list,
        string context)
    {
        if (listProp == null || list == null)
            return;

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            int removeIndex = -1;
            int count = Mathf.Min(list.Count, listProp.arraySize);

            for (int i = 0; i < count; i++)
            {
                if (list[i] == null)
                {
                    Undo.RecordObject(_target, "Repair Null Condition");
                    list[i] = new EpisodeCondition();
                    EditorUtility.SetDirty(_target);
                    SyncSerializedObject();
                }

                SerializedProperty cond = listProp.GetArrayElementAtIndex(i);

                using (new GUILayout.HorizontalScope())
                {
                    DrawEnumField(cond, "Kind", 70f);
                    DrawTextField(cond, "Key", 90f);
                    DrawEnumField(cond, "Op", 80f);

                    SerializedProperty kindProp = cond != null ? cond.FindPropertyRelative("Kind") : null;
                    SerializedProperty opProp = cond != null ? cond.FindPropertyRelative("Op") : null;

                    if (kindProp != null && opProp != null)
                    {
                        EpisodeConditionKind kind = (EpisodeConditionKind)kindProp.enumValueIndex;
                        EpisodeCompareOp op = (EpisodeCompareOp)opProp.enumValueIndex;

                        if (op != EpisodeCompareOp.Exists && op != EpisodeCompareOp.NotExists)
                        {
                            if (kind == EpisodeConditionKind.Stat)
                            {
                                SerializedProperty intProp = cond.FindPropertyRelative("IntValue");

                                if (intProp != null)
                                    EditorGUILayout.PropertyField(intProp, GUIContent.none, GUILayout.Width(48f));
                            }
                            else if (kind == EpisodeConditionKind.Flag)
                            {
                                SerializedProperty boolProp = cond.FindPropertyRelative("BoolValue");

                                if (boolProp != null)
                                    EditorGUILayout.PropertyField(boolProp, GUIContent.none, GUILayout.Width(18f));
                            }
                            else
                            {
                                DrawTextField(cond, "StringValue", 70f);
                            }
                        }
                    }

                    Color oldBackground = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

                    if (GUILayout.Button("x", GUILayout.Width(20f)))
                        removeIndex = i;

                    GUI.backgroundColor = oldBackground;
                }
            }

            if (list.Count != listProp.arraySize)
            {
                EditorGUILayout.HelpBox(
                    $"List and SerializedProperty size are out of sync. list={list.Count}, prop={listProp.arraySize}",
                    MessageType.Warning);
            }

            if (removeIndex >= 0 && removeIndex < list.Count)
            {
                Undo.RecordObject(_target, "Remove Condition");
                list.RemoveAt(removeIndex);
                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                _validationDirty = true;
            }

            if (GUILayout.Button($"+ Add Condition ({context})", GUILayout.ExpandWidth(true)))
            {
                Undo.RecordObject(_target, "Add Condition");
                list.Add(new EpisodeCondition());
                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                _validationDirty = true;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: NextOption 목록
    // ─────────────────────────────────────────────────────────

    private void DrawNextOptionList(
        SerializedProperty listProp,
        List<EpisodeNextOption> list,
        string ownerEpisodeId)
    {
        if (listProp == null || list == null)
            return;

        string[] allIds = CollectAllEpisodeIds();

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            int removeIndex = -1;
            int count = Mathf.Min(list.Count, listProp.arraySize);

            for (int i = 0; i < count; i++)
            {
                if (list[i] == null)
                {
                    Undo.RecordObject(_target, "Repair Null NextOption");
                    list[i] = new EpisodeNextOption();
                    EditorUtility.SetDirty(_target);
                    SyncSerializedObject();
                }

                SerializedProperty optProp = listProp.GetArrayElementAtIndex(i);
                EpisodeNextOption opt = list[i];

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Option [{i}]", EditorStyles.boldLabel, GUILayout.Width(80f));
                        GUILayout.FlexibleSpace();

                        Color oldBackground = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

                        if (GUILayout.Button("Remove", GUILayout.Width(60f)))
                            removeIndex = i;

                        GUI.backgroundColor = oldBackground;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Target Episode", GUILayout.Width(LABEL_W));

                        int currentIdx = Array.IndexOf(allIds, opt.TargetEpisodeId);
                        int popupIndex = Mathf.Max(0, currentIdx);

                        int newIdx = EditorGUILayout.Popup(popupIndex, allIds);

                        if (newIdx >= 0 && newIdx < allIds.Length)
                        {
                            string selected = allIds[newIdx] == NONE_LABEL ? "" : allIds[newIdx];

                            if (selected != opt.TargetEpisodeId)
                            {
                                Undo.RecordObject(_target, "Set NextOption Target");

                                SerializedProperty targetProp = optProp.FindPropertyRelative("TargetEpisodeId");

                                if (targetProp != null)
                                    targetProp.stringValue = selected;

                                opt.TargetEpisodeId = selected;

                                EditorUtility.SetDirty(_target);
                                _validationDirty = true;
                            }
                        }

                        SerializedProperty directTargetProp = optProp.FindPropertyRelative("TargetEpisodeId");

                        if (directTargetProp != null)
                        {
                            string typed = EditorGUILayout.TextField(directTargetProp.stringValue, GUILayout.Width(100f));

                            if (typed != directTargetProp.stringValue)
                            {
                                Undo.RecordObject(_target, "Set NextOption Target");
                                directTargetProp.stringValue = typed;
                                opt.TargetEpisodeId = typed;
                                EditorUtility.SetDirty(_target);
                                _validationDirty = true;
                            }
                        }
                    }

                    DrawPropField(optProp, "ChoiceLabel", "Choice Label");
                    DrawPropField(optProp, "HideWhenLocked", "Hide When Locked");

                    if (!opt.HideWhenLocked)
                        DrawPropField(optProp, "LockedReasonText", "Locked Reason Text");

                    if (opt.Conditions == null)
                        opt.Conditions = new List<EpisodeCondition>();

                    SerializedProperty condProp = optProp.FindPropertyRelative("Conditions");
                    DrawConditionList(condProp, opt.Conditions, $"NextOption[{i}]");
                }

                GUILayout.Space(2f);
            }

            if (list.Count != listProp.arraySize)
            {
                EditorGUILayout.HelpBox(
                    $"NextOptions list and SerializedProperty size are out of sync. list={list.Count}, prop={listProp.arraySize}",
                    MessageType.Warning);
            }

            if (removeIndex >= 0 && removeIndex < list.Count)
            {
                Undo.RecordObject(_target, "Remove NextOption");
                list.RemoveAt(removeIndex);
                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                _validationDirty = true;
            }

            if (GUILayout.Button("+ Add Next Option", GUILayout.ExpandWidth(true)))
            {
                Undo.RecordObject(_target, "Add NextOption");
                list.Add(new EpisodeNextOption());
                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                _validationDirty = true;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: Attachment 목록
    // ─────────────────────────────────────────────────────────

    private void DrawAttachmentList(
        SerializedProperty listProp,
        List<EpisodeAttachmentDefinition> list,
        string ownerEpisodeId)
    {
        if (listProp == null || list == null)
            return;

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            int removeIndex = -1;
            int count = Mathf.Min(list.Count, listProp.arraySize);

            for (int i = 0; i < count; i++)
            {
                if (list[i] == null)
                {
                    Undo.RecordObject(_target, "Repair Null Attachment");
                    list[i] = new EpisodeAttachmentDefinition
                    {
                        ParentEpisodeId = ownerEpisodeId
                    };

                    EditorUtility.SetDirty(_target);
                    SyncSerializedObject();
                }

                SerializedProperty attProp = listProp.GetArrayElementAtIndex(i);
                EpisodeAttachmentDefinition att = list[i];

                EnsureAttachmentLists(att);

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Attachment [{i}]", EditorStyles.boldLabel, GUILayout.Width(120f));
                        GUILayout.FlexibleSpace();

                        Color oldBackground = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

                        if (GUILayout.Button("Remove", GUILayout.Width(60f)))
                            removeIndex = i;

                        GUI.backgroundColor = oldBackground;
                    }

                    DrawPropField(attProp, "AttachmentId", "Attachment ID");
                    DrawPropField(attProp, "ParentEpisodeId", "Parent Episode ID");
                    DrawPropField(attProp, "Title", "Title");
                    DrawPropField(attProp, "IndexText", "Index Text");
                    DrawPropField(attProp, "Kind", "Kind");
                    DrawPropField(attProp, "DialogueEntryId", "Dialogue Entry ID");
                    DrawPropField(attProp, "IsRepeatable", "Is Repeatable");
                    DrawPropField(attProp, "DesignerNote", "Designer Note");

                    SerializedProperty visibleProp = attProp.FindPropertyRelative("VisibleConditions");
                    SerializedProperty unlockProp = attProp.FindPropertyRelative("UnlockConditions");

                    DrawConditionList(visibleProp, att.VisibleConditions, $"Att[{i}].Visible");
                    DrawConditionList(unlockProp, att.UnlockConditions, $"Att[{i}].Unlock");
                }

                GUILayout.Space(2f);
            }

            if (list.Count != listProp.arraySize)
            {
                EditorGUILayout.HelpBox(
                    $"Attachments list and SerializedProperty size are out of sync. list={list.Count}, prop={listProp.arraySize}",
                    MessageType.Warning);
            }

            if (removeIndex >= 0 && removeIndex < list.Count)
            {
                Undo.RecordObject(_target, "Remove Attachment");
                list.RemoveAt(removeIndex);
                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                _validationDirty = true;
            }

            if (GUILayout.Button("+ Add Attachment", GUILayout.ExpandWidth(true)))
            {
                Undo.RecordObject(_target, "Add Attachment");

                list.Add(new EpisodeAttachmentDefinition
                {
                    ParentEpisodeId = ownerEpisodeId
                });

                EditorUtility.SetDirty(_target);
                SyncSerializedObject();
                _validationDirty = true;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: EndingKey Popup
    // ─────────────────────────────────────────────────────────

    private void DrawEndingKeyPopup(SerializedProperty nodeProp, EpisodeNodeDefinition node)
    {
        if (nodeProp == null || node == null)
            return;

        string[] endingKeys = CollectEndingKeys();

        using (new GUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Ending Key", GUILayout.Width(LABEL_W));

            int currentIdx = Array.IndexOf(endingKeys, node.EndingKey);
            int popupIndex = Mathf.Max(0, currentIdx);

            int newIdx = EditorGUILayout.Popup(popupIndex, endingKeys);

            if (newIdx >= 0 && newIdx < endingKeys.Length)
            {
                string selected = endingKeys[newIdx] == NONE_LABEL ? "" : endingKeys[newIdx];

                if (selected != node.EndingKey)
                {
                    Undo.RecordObject(_target, "Set EndingKey");

                    SerializedProperty endingKeyProp = nodeProp.FindPropertyRelative("EndingKey");

                    if (endingKeyProp != null)
                        endingKeyProp.stringValue = selected;

                    node.EndingKey = selected;

                    EditorUtility.SetDirty(_target);
                    _validationDirty = true;
                }
            }

            SerializedProperty directEndingKeyProp = nodeProp.FindPropertyRelative("EndingKey");

            if (directEndingKeyProp != null)
            {
                string typed = EditorGUILayout.TextField(directEndingKeyProp.stringValue, GUILayout.Width(100f));

                if (typed != directEndingKeyProp.stringValue)
                {
                    Undo.RecordObject(_target, "Set EndingKey");
                    directEndingKeyProp.stringValue = typed;
                    node.EndingKey = typed;
                    EditorUtility.SetDirty(_target);
                    _validationDirty = true;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // Node CRUD
    // ─────────────────────────────────────────────────────────

    private void AddNode()
    {
        if (_target == null)
            return;

        EnsureTargetLists();

        Undo.RecordObject(_target, "Add Episode Node");

        _target.Nodes.Add(new EpisodeNodeDefinition
        {
            EpisodeId = GenerateUniqueEpisodeId("new_episode"),
            Title = "New Episode",
            DialogueEntryId = ""
        });

        EditorUtility.SetDirty(_target);
        SyncSerializedObject();

        SelectNode(_target.Nodes.Count - 1);
        _validationDirty = true;
    }

    private void DuplicateNode(int index)
    {
        if (_target == null || _target.Nodes == null)
            return;

        if (index < 0 || index >= _target.Nodes.Count)
            return;

        EpisodeNodeDefinition original = _target.Nodes[index];

        if (original == null)
            return;

        EnsureNodeLists(original);

        Undo.RecordObject(_target, "Duplicate Episode Node");

        EpisodeNodeDefinition copy = new EpisodeNodeDefinition
        {
            EpisodeId = GenerateUniqueEpisodeId(original.EpisodeId + "_copy"),
            Title = original.Title,
            IndexText = original.IndexText,
            Kind = original.Kind,
            DialogueEntryId = original.DialogueEntryId,
            IsChapterEndingCandidate = original.IsChapterEndingCandidate,
            EndingKey = original.EndingKey,
            DesignerNote = original.DesignerNote
        };

        foreach (EpisodeCondition c in original.VisibleConditions)
        {
            if (c != null)
                copy.VisibleConditions.Add(CloneCondition(c));
        }

        foreach (EpisodeCondition c in original.UnlockConditions)
        {
            if (c != null)
                copy.UnlockConditions.Add(CloneCondition(c));
        }

        foreach (EpisodeNextOption o in original.NextOptions)
        {
            if (o != null)
                copy.NextOptions.Add(CloneNextOption(o));
        }

        foreach (EpisodeAttachmentDefinition a in original.Attachments)
        {
            if (a != null)
                copy.Attachments.Add(CloneAttachment(a, copy.EpisodeId));
        }

        _target.Nodes.Insert(index + 1, copy);

        EditorUtility.SetDirty(_target);
        SyncSerializedObject();

        SelectNode(index + 1);
        _validationDirty = true;
    }

    private void DeleteNode(int index)
    {
        if (_target == null || _target.Nodes == null)
            return;

        if (index < 0 || index >= _target.Nodes.Count)
            return;

        Undo.RecordObject(_target, "Delete Episode Node");

        _target.Nodes.RemoveAt(index);

        EditorUtility.SetDirty(_target);
        SyncSerializedObject();

        int count = GetNodeCount();
        _selectedNodeIndex = count > 0 ? Mathf.Clamp(index - 1, 0, count - 1) : -1;

        _validationDirty = true;
    }

    private void SelectNode(int index)
    {
        int count = GetNodeCount();

        if (count <= 0)
            _selectedNodeIndex = -1;
        else
            _selectedNodeIndex = Mathf.Clamp(index, 0, count - 1);

        _foldVisible = true;
        _foldUnlock = true;
        _foldNext = true;
        _foldAttachments = true;

        Repaint();
    }

    // ─────────────────────────────────────────────────────────
    // EndingRule CRUD
    // ─────────────────────────────────────────────────────────

    private void AddEndingRule()
    {
        if (_target == null)
            return;

        EnsureTargetLists();

        Undo.RecordObject(_target, "Add Ending Rule");

        _target.EndingRules.Add(new ChapterEndingRule
        {
            EndingKey = GenerateUniqueEndingKey("new_ending"),
            DisplayName = "New Ending"
        });

        EditorUtility.SetDirty(_target);
        SyncSerializedObject();

        _selectedEndingIndex = _target.EndingRules.Count - 1;
        _validationDirty = true;
    }

    private void DeleteEndingRule(int index)
    {
        if (_target == null || _target.EndingRules == null)
            return;

        if (index < 0 || index >= _target.EndingRules.Count)
            return;

        Undo.RecordObject(_target, "Delete Ending Rule");

        _target.EndingRules.RemoveAt(index);

        EditorUtility.SetDirty(_target);
        SyncSerializedObject();

        int count = GetEndingCount();
        _selectedEndingIndex = count > 0 ? Mathf.Clamp(index - 1, 0, count - 1) : -1;

        _validationDirty = true;
    }

    // ─────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────

    private void RunValidation()
    {
        if (_target == null)
            return;

        if (_serializedObject != null)
            _serializedObject.ApplyModifiedProperties();

        _lastValidationResult = EpisodeProgressionValidator.Validate(_target);

        Repaint();
    }

    // ─────────────────────────────────────────────────────────
    // Asset 관리
    // ─────────────────────────────────────────────────────────

    private void SetTarget(ChapterEpisodeProgressionSO target)
    {
        _target = target;
        _serializedObject = target != null ? new SerializedObject(target) : null;

        if (_target != null)
            EnsureTargetLists();

        _selectedNodeIndex = -1;
        _selectedEndingIndex = -1;
        _lastValidationResult = null;

        if (_target != null && _target.Nodes != null && _target.Nodes.Count > 0)
            SelectNode(0);

        Repaint();
    }

    private void CreateNewAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Chapter Episode Progression",
            "ChapterProgression",
            "asset",
            "Choose save location",
            "Assets");

        if (string.IsNullOrEmpty(path))
            return;

        ChapterEpisodeProgressionSO asset =
            CreateInstance<ChapterEpisodeProgressionSO>();

        asset.ChapterId = "ch_new";
        asset.DisplayName = "New Chapter";

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        SetTarget(asset);

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    // ─────────────────────────────────────────────────────────
    // 상태 보정 / 동기화
    // ─────────────────────────────────────────────────────────

    private void EnsureTargetLists()
    {
        if (_target == null)
            return;

        bool dirty = false;

        if (_target.Nodes == null)
        {
            _target.Nodes = new List<EpisodeNodeDefinition>();
            dirty = true;
        }

        if (_target.EndingRules == null)
        {
            _target.EndingRules = new List<ChapterEndingRule>();
            dirty = true;
        }

        if (dirty)
            EditorUtility.SetDirty(_target);
    }

    private static void EnsureNodeLists(EpisodeNodeDefinition node)
    {
        if (node == null)
            return;

        if (node.VisibleConditions == null)
            node.VisibleConditions = new List<EpisodeCondition>();

        if (node.UnlockConditions == null)
            node.UnlockConditions = new List<EpisodeCondition>();

        if (node.NextOptions == null)
            node.NextOptions = new List<EpisodeNextOption>();

        if (node.Attachments == null)
            node.Attachments = new List<EpisodeAttachmentDefinition>();
    }

    private static void EnsureAttachmentLists(EpisodeAttachmentDefinition attachment)
    {
        if (attachment == null)
            return;

        if (attachment.VisibleConditions == null)
            attachment.VisibleConditions = new List<EpisodeCondition>();

        if (attachment.UnlockConditions == null)
            attachment.UnlockConditions = new List<EpisodeCondition>();
    }

    private void SyncSerializedObject()
    {
        if (_target == null)
            return;

        if (_serializedObject == null || _serializedObject.targetObject != _target)
            _serializedObject = new SerializedObject(_target);

        _serializedObject.Update();
        ClampSelectionIndices();
        Repaint();
    }

    private void ClampSelectionIndices()
    {
        if (_target == null)
        {
            _selectedNodeIndex = -1;
            _selectedEndingIndex = -1;
            return;
        }

        int nodeCount = GetNodeCount();

        if (nodeCount <= 0)
            _selectedNodeIndex = -1;
        else if (_selectedNodeIndex >= nodeCount)
            _selectedNodeIndex = nodeCount - 1;
        else if (_selectedNodeIndex < -1)
            _selectedNodeIndex = -1;

        int endingCount = GetEndingCount();

        if (endingCount <= 0)
            _selectedEndingIndex = -1;
        else if (_selectedEndingIndex >= endingCount)
            _selectedEndingIndex = endingCount - 1;
        else if (_selectedEndingIndex < -1)
            _selectedEndingIndex = -1;
    }

    private int GetNodeCount()
    {
        return _target != null && _target.Nodes != null
            ? _target.Nodes.Count
            : 0;
    }

    private int GetEndingCount()
    {
        return _target != null && _target.EndingRules != null
            ? _target.EndingRules.Count
            : 0;
    }

    private EpisodeNodeDefinition GetSelectedNode()
    {
        if (_target == null || _target.Nodes == null)
            return null;

        if (_selectedNodeIndex < 0 || _selectedNodeIndex >= _target.Nodes.Count)
            return null;

        return _target.Nodes[_selectedNodeIndex];
    }

    private ChapterEndingRule GetSelectedEndingRule()
    {
        if (_target == null || _target.EndingRules == null)
            return null;

        if (_selectedEndingIndex < 0 || _selectedEndingIndex >= _target.EndingRules.Count)
            return null;

        return _target.EndingRules[_selectedEndingIndex];
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 - 고유 ID 생성
    // ─────────────────────────────────────────────────────────

    private string GenerateUniqueEpisodeId(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            seed = "episode";

        if (!EpisodeIdExists(seed))
            return seed;

        for (int i = 2; i < 100; i++)
        {
            string candidate = $"{seed}_{i}";

            if (!EpisodeIdExists(candidate))
                return candidate;
        }

        return seed + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private bool EpisodeIdExists(string id)
    {
        if (_target == null || _target.Nodes == null)
            return false;

        for (int i = 0; i < _target.Nodes.Count; i++)
        {
            EpisodeNodeDefinition n = _target.Nodes[i];

            if (n != null && string.Equals(n.EpisodeId, id, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private string GenerateUniqueEndingKey(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            seed = "ending";

        if (_target == null || _target.EndingRules == null)
            return seed;

        if (!EndingKeyExists(seed))
            return seed;

        for (int i = 2; i < 100; i++)
        {
            string candidate = $"{seed}_{i}";

            if (!EndingKeyExists(candidate))
                return candidate;
        }

        return seed + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private bool EndingKeyExists(string key)
    {
        if (_target == null || _target.EndingRules == null)
            return false;

        for (int i = 0; i < _target.EndingRules.Count; i++)
        {
            ChapterEndingRule rule = _target.EndingRules[i];

            if (rule != null && string.Equals(rule.EndingKey, key, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 - 목록 수집
    // ─────────────────────────────────────────────────────────

    private string[] CollectAllEpisodeIds()
    {
        if (_target == null || _target.Nodes == null)
            return new[] { NONE_LABEL };

        List<string> ids = new List<string> { NONE_LABEL };

        for (int i = 0; i < _target.Nodes.Count; i++)
        {
            EpisodeNodeDefinition n = _target.Nodes[i];

            if (n != null && !string.IsNullOrWhiteSpace(n.EpisodeId))
                ids.Add(n.EpisodeId);
        }

        return ids.ToArray();
    }

    private string[] CollectEndingKeys()
    {
        if (_target == null || _target.EndingRules == null)
            return new[] { NONE_LABEL };

        List<string> keys = new List<string> { NONE_LABEL };

        for (int i = 0; i < _target.EndingRules.Count; i++)
        {
            ChapterEndingRule r = _target.EndingRules[i];

            if (r != null && !string.IsNullOrWhiteSpace(r.EndingKey))
                keys.Add(r.EndingKey);
        }

        return keys.ToArray();
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 - 딥 클론
    // ─────────────────────────────────────────────────────────

    private static EpisodeCondition CloneCondition(EpisodeCondition src)
    {
        if (src == null)
            return new EpisodeCondition();

        return new EpisodeCondition
        {
            Kind = src.Kind,
            Key = src.Key,
            Op = src.Op,
            IntValue = src.IntValue,
            BoolValue = src.BoolValue,
            StringValue = src.StringValue
        };
    }

    private static EpisodeNextOption CloneNextOption(EpisodeNextOption src)
    {
        EpisodeNextOption copy = new EpisodeNextOption();

        if (src == null)
            return copy;

        copy.TargetEpisodeId = src.TargetEpisodeId;
        copy.ChoiceLabel = src.ChoiceLabel;
        copy.HideWhenLocked = src.HideWhenLocked;
        copy.LockedReasonText = src.LockedReasonText;

        if (src.Conditions != null)
        {
            foreach (EpisodeCondition c in src.Conditions)
            {
                if (c != null)
                    copy.Conditions.Add(CloneCondition(c));
            }
        }

        return copy;
    }

    private static EpisodeAttachmentDefinition CloneAttachment(
        EpisodeAttachmentDefinition src,
        string newParentEpisodeId)
    {
        EpisodeAttachmentDefinition copy = new EpisodeAttachmentDefinition
        {
            ParentEpisodeId = newParentEpisodeId
        };

        if (src == null)
            return copy;

        copy.AttachmentId = src.AttachmentId + "_copy";
        copy.Title = src.Title;
        copy.IndexText = src.IndexText;
        copy.Kind = src.Kind;
        copy.DialogueEntryId = src.DialogueEntryId;
        copy.IsRepeatable = src.IsRepeatable;
        copy.DesignerNote = src.DesignerNote;

        if (src.VisibleConditions != null)
        {
            foreach (EpisodeCondition c in src.VisibleConditions)
            {
                if (c != null)
                    copy.VisibleConditions.Add(CloneCondition(c));
            }
        }

        if (src.UnlockConditions != null)
        {
            foreach (EpisodeCondition c in src.UnlockConditions)
            {
                if (c != null)
                    copy.UnlockConditions.Add(CloneCondition(c));
            }
        }

        return copy;
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 - 필드 드로어
    // ─────────────────────────────────────────────────────────

    private static void DrawPropField(SerializedProperty parent, string propName, string label)
    {
        if (parent == null)
        {
            EditorGUILayout.LabelField($"[Missing parent: {propName}]", EditorStyles.miniLabel);
            return;
        }

        SerializedProperty prop = parent.FindPropertyRelative(propName);

        if (prop == null)
        {
            EditorGUILayout.LabelField($"[Missing prop: {propName}]", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.PropertyField(prop, new GUIContent(label));
    }

    private static void DrawTextField(SerializedProperty parent, string propName, float width)
    {
        if (parent == null)
            return;

        SerializedProperty prop = parent.FindPropertyRelative(propName);

        if (prop == null)
            return;

        prop.stringValue = EditorGUILayout.TextField(prop.stringValue, GUILayout.Width(width));
    }

    private static void DrawEnumField(SerializedProperty parent, string propName, float width)
    {
        if (parent == null)
            return;

        SerializedProperty prop = parent.FindPropertyRelative(propName);

        if (prop == null)
            return;

        EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(width));
    }

    // ─────────────────────────────────────────────────────────
    // 스타일 초기화
    // ─────────────────────────────────────────────────────────

    private void EnsureStyles()
    {
        if (_stylesInitialized)
            return;

        _stylesInitialized = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };
        _headerStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        _sectionStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Italic
        };
        _sectionStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);

        _nodeRowStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 4, 0, 0)
        };
        _nodeRowStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        _nodeRowSelectedStyle = new GUIStyle(_nodeRowStyle);
        _nodeRowSelectedStyle.normal.background = MakeTex(1, 1, new Color(0.22f, 0.45f, 0.7f, 0.6f));
        _nodeRowSelectedStyle.normal.textColor = Color.white;

        _errorStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
        _errorStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
        _errorStyle.wordWrap = true;

        _warningStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
        _warningStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        _warningStyle.wordWrap = true;

        _infoStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
        _infoStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);
        _infoStyle.wordWrap = true;
    }

    private static void DrawSectionLabel(string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}
#endif