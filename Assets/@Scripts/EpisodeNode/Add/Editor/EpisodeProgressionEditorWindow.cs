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
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class EpisodeProgressionEditorWindow : EditorWindow
{
    // ─── 상수 ───────────────────────────────────────────────

    private const float LEFT_PANEL_WIDTH   = 220f;
    private const float RIGHT_PANEL_WIDTH  = 280f;
    private const float TOOLBAR_HEIGHT     = 36f;
    private const float SECTION_GAP        = 6f;
    private const float NODE_ROW_HEIGHT    = 28f;
    private const float CONDITION_ROW_H    = 22f;
    private const float LABEL_W            = 140f;

    // ─── 상태 ───────────────────────────────────────────────

    private ChapterEpisodeProgressionSO _target;
    private SerializedObject            _serializedObject;

    private int _selectedNodeIndex  = -1;
    private int _selectedEndingIndex = -1;

    private Vector2 _leftScroll;
    private Vector2 _centerScroll;
    private Vector2 _rightScroll;
    private Vector2 _validationScroll;

    private EpisodeProgressionValidationResult _lastValidationResult;
    private bool _validationDirty = false;

    // 조건 편집 펼침 상태
    private bool _foldVisible;
    private bool _foldUnlock;
    private bool _foldNext;
    private bool _foldAttachments;

    // ─── GUI 스타일 (지연 초기화) ────────────────────────────

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

    // ─── Unity 이벤트 ────────────────────────────────────────

    private void OnGUI()
    {
        EnsureStyles();

        DrawToolbar();

        if (_target == null)
        {
            EditorGUILayout.HelpBox("Select or create a ChapterEpisodeProgressionSO.", MessageType.Info);
            return;
        }

        _serializedObject.Update();

        Rect body = new Rect(0f, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);
        DrawBody(body);

        _serializedObject.ApplyModifiedProperties();

        if (_validationDirty)
        {
            _validationDirty = false;
            RunValidation();
            Repaint();
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

        GUILayout.BeginArea(new Rect(8f, 6f, position.width - 16f, TOOLBAR_HEIGHT - 12f));
        GUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        ChapterEpisodeProgressionSO picked =
            (ChapterEpisodeProgressionSO)EditorGUILayout.ObjectField(
                _target, typeof(ChapterEpisodeProgressionSO), false,
                GUILayout.Width(280f));

        if (EditorGUI.EndChangeCheck())
            SetTarget(picked);

        if (GUILayout.Button("Create New", GUILayout.Width(90f)))
            CreateNewAsset();

        GUILayout.FlexibleSpace();

        if (_target != null)
        {
            if (GUILayout.Button("Auto-Fill Parents", GUILayout.Width(120f)))
            {
                Undo.RecordObject(_target, "Auto-Fill Attachment Parents");
                EpisodeProgressionValidator.AutoFillAttachmentParents(_target);
                EditorUtility.SetDirty(_target);
                RunValidation();
            }

            GUI.backgroundColor = _lastValidationResult != null && _lastValidationResult.HasErrors
                ? new Color(1f, 0.4f, 0.4f)
                : Color.white;

            if (GUILayout.Button("Validate", GUILayout.Width(80f)))
                RunValidation();

            GUI.backgroundColor = Color.white;

            // 검증 요약 표시
            if (_lastValidationResult != null)
            {
                int errors   = _lastValidationResult.ErrorCount;
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

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ─── 본문 ────────────────────────────────────────────────

    private void DrawBody(Rect body)
    {
        float centerW = body.width - LEFT_PANEL_WIDTH - RIGHT_PANEL_WIDTH;

        Rect leftRect   = new Rect(body.x,                          body.y, LEFT_PANEL_WIDTH,  body.height);
        Rect centerRect = new Rect(body.x + LEFT_PANEL_WIDTH,       body.y, centerW,           body.height);
        Rect rightRect  = new Rect(body.x + LEFT_PANEL_WIDTH + centerW, body.y, RIGHT_PANEL_WIDTH, body.height);

        // 구분선
        EditorGUI.DrawRect(new Rect(leftRect.xMax,  body.y, 1f, body.height), new Color(0.1f, 0.1f, 0.1f));
        EditorGUI.DrawRect(new Rect(rightRect.x,    body.y, 1f, body.height), new Color(0.1f, 0.1f, 0.1f));

        DrawLeftPanel(leftRect);
        DrawCenterPanel(centerRect);
        DrawRightPanel(rightRect);
    }

    // ─────────────────────────────────────────────────────────
    // 좌측: Node 목록
    // ─────────────────────────────────────────────────────────

    private void DrawLeftPanel(Rect rect)
    {
        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical();

        // 헤더
        EditorGUILayout.LabelField(
            $"Episode Nodes  ({_target.Nodes.Count})",
            _headerStyle, GUILayout.Height(22f));

        // Add / Duplicate / Delete 버튼
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("+", GUILayout.Width(28f)))
            AddNode();

        GUI.enabled = _selectedNodeIndex >= 0 && _selectedNodeIndex < _target.Nodes.Count;

        if (GUILayout.Button("Dup", GUILayout.Width(40f)))
            DuplicateNode(_selectedNodeIndex);

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

        if (GUILayout.Button("Del", GUILayout.Width(36f)))
        {
            if (EditorUtility.DisplayDialog("Delete Node",
                $"Delete '{_target.Nodes[_selectedNodeIndex].EpisodeId}'?", "Delete", "Cancel"))
                DeleteNode(_selectedNodeIndex);
        }

        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        GUILayout.EndHorizontal();

        // 목록
        _leftScroll = GUILayout.BeginScrollView(_leftScroll, GUILayout.ExpandHeight(true));

        for (int i = 0; i < _target.Nodes.Count; i++)
        {
            EpisodeNodeDefinition node = _target.Nodes[i];
            bool selected = i == _selectedNodeIndex;

            GUIStyle rowStyle = selected ? _nodeRowSelectedStyle : _nodeRowStyle;

            string label = string.IsNullOrWhiteSpace(node.EpisodeId)
                ? $"[{i}] (empty id)"
                : $"[{i}] {node.EpisodeId}";

            if (node.IsChapterEndingCandidate)
                label += " ★";

            Rect rowRect = GUILayoutUtility.GetRect(
                GUIContent.none, rowStyle, GUILayout.Height(NODE_ROW_HEIGHT));

            if (GUI.Button(rowRect, label, rowStyle))
                SelectNode(i);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // ─────────────────────────────────────────────────────────
    // 중앙: 선택된 Node 상세
    // ─────────────────────────────────────────────────────────

    private void DrawCenterPanel(Rect rect)
    {
        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical();

        if (_selectedNodeIndex < 0 || _selectedNodeIndex >= _target.Nodes.Count)
        {
            EditorGUILayout.HelpBox("← Select a node to edit.", MessageType.None);
            GUILayout.EndVertical();
            GUILayout.EndArea();
            return;
        }

        SerializedProperty nodesProp = _serializedObject.FindProperty("Nodes");
        SerializedProperty nodeProp  = nodesProp.GetArrayElementAtIndex(_selectedNodeIndex);

        if (nodeProp == null)
        {
            GUILayout.EndVertical();
            GUILayout.EndArea();
            return;
        }

        EpisodeNodeDefinition node = _target.Nodes[_selectedNodeIndex];

        EditorGUILayout.LabelField("Node Detail", _headerStyle, GUILayout.Height(22f));

        _centerScroll = GUILayout.BeginScrollView(_centerScroll, GUILayout.ExpandHeight(true));

        // ── 기본 정보 ──
        DrawSectionLabel("Identity");

        DrawPropField(nodeProp, "EpisodeId",       "Episode ID");
        DrawPropField(nodeProp, "Title",            "Title");
        DrawPropField(nodeProp, "IndexText",        "Index Text");
        DrawPropField(nodeProp, "Kind",             "Kind");
        DrawPropField(nodeProp, "DialogueEntryId",  "Dialogue Entry ID");
        DrawPropField(nodeProp, "DesignerNote",     "Designer Note");

        EditorGUILayout.Space(SECTION_GAP);

        // ── 엔딩 정보 ──
        DrawSectionLabel("Ending");

        DrawPropField(nodeProp, "IsChapterEndingCandidate", "Is Chapter Ending Candidate");

        if (node.IsChapterEndingCandidate)
        {
            // EndingKey를 EndingRules 목록에서 Popup으로 선택
            DrawEndingKeyPopup(nodeProp, node);
        }

        EditorGUILayout.Space(SECTION_GAP);

        // ── Visible 조건 ──
        _foldVisible = EditorGUILayout.Foldout(_foldVisible,
            $"Visible Conditions  ({node.VisibleConditions.Count})", true);

        if (_foldVisible)
        {
            DrawConditionList(nodeProp.FindPropertyRelative("VisibleConditions"),
                node.VisibleConditions, "VisibleConditions");
        }

        // ── Unlock 조건 ──
        _foldUnlock = EditorGUILayout.Foldout(_foldUnlock,
            $"Unlock Conditions  ({node.UnlockConditions.Count})", true);

        if (_foldUnlock)
        {
            DrawConditionList(nodeProp.FindPropertyRelative("UnlockConditions"),
                node.UnlockConditions, "UnlockConditions");
        }

        EditorGUILayout.Space(SECTION_GAP);

        // ── Next Options ──
        _foldNext = EditorGUILayout.Foldout(_foldNext,
            $"Next Options  ({node.NextOptions.Count})", true);

        if (_foldNext)
        {
            DrawNextOptionList(nodeProp.FindPropertyRelative("NextOptions"),
                node.NextOptions, node.EpisodeId);
        }

        EditorGUILayout.Space(SECTION_GAP);

        // ── Attachments ──
        _foldAttachments = EditorGUILayout.Foldout(_foldAttachments,
            $"Attachments  ({node.Attachments.Count})", true);

        if (_foldAttachments)
        {
            DrawAttachmentList(nodeProp.FindPropertyRelative("Attachments"),
                node.Attachments, node.EpisodeId);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // ─────────────────────────────────────────────────────────
    // 우측: EndingRules + Validation 결과
    // ─────────────────────────────────────────────────────────

    private void DrawRightPanel(Rect rect)
    {
        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical();

        float halfH = rect.height * 0.45f;

        // ── Ending Rules ──
        EditorGUILayout.LabelField("Ending Rules", _headerStyle, GUILayout.Height(22f));

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Add Ending Rule", GUILayout.ExpandWidth(true)))
            AddEndingRule();

        GUI.enabled = _selectedEndingIndex >= 0 && _selectedEndingIndex < _target.EndingRules.Count;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

        if (GUILayout.Button("Del", GUILayout.Width(36f)))
        {
            if (EditorUtility.DisplayDialog("Delete Ending Rule",
                $"Delete '{_target.EndingRules[_selectedEndingIndex].EndingKey}'?", "Delete", "Cancel"))
                DeleteEndingRule(_selectedEndingIndex);
        }

        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        _rightScroll = GUILayout.BeginScrollView(_rightScroll, GUILayout.Height(halfH));

        SerializedProperty endingsProp = _serializedObject.FindProperty("EndingRules");

        for (int i = 0; i < _target.EndingRules.Count; i++)
        {
            bool selected = i == _selectedEndingIndex;
            GUIStyle rowStyle = selected ? _nodeRowSelectedStyle : _nodeRowStyle;

            ChapterEndingRule rule = _target.EndingRules[i];
            string label = string.IsNullOrWhiteSpace(rule.EndingKey) ? $"[{i}] (empty)" : $"[{i}] {rule.EndingKey}";

            Rect rowRect = GUILayoutUtility.GetRect(
                GUIContent.none, rowStyle, GUILayout.Height(NODE_ROW_HEIGHT));

            if (GUI.Button(rowRect, label, rowStyle))
                _selectedEndingIndex = i;
        }

        GUILayout.EndScrollView();

        // 선택된 EndingRule 인라인 편집
        if (_selectedEndingIndex >= 0 && _selectedEndingIndex < _target.EndingRules.Count)
        {
            SerializedProperty ruleProp =
                endingsProp.GetArrayElementAtIndex(_selectedEndingIndex);

            DrawPropField(ruleProp, "EndingKey",       "Ending Key");
            DrawPropField(ruleProp, "DisplayName",     "Display Name");
            DrawPropField(ruleProp, "UnlockNextChapter", "Unlock Next Chapter");

            if (_target.EndingRules[_selectedEndingIndex].UnlockNextChapter)
                DrawPropField(ruleProp, "NextChapterId", "Next Chapter ID");

            DrawPropField(ruleProp, "DesignerNote", "Designer Note");

            EditorGUILayout.Space(4f);
            SerializedProperty condProp = ruleProp.FindPropertyRelative("Conditions");
            DrawConditionList(condProp, _target.EndingRules[_selectedEndingIndex].Conditions,
                "EndingConditions");
        }

        EditorGUILayout.Space(SECTION_GAP);

        // ── Validation 결과 ──
        EditorGUILayout.LabelField("Validation", _headerStyle, GUILayout.Height(22f));

        _validationScroll = GUILayout.BeginScrollView(_validationScroll, GUILayout.ExpandHeight(true));

        if (_lastValidationResult == null)
        {
            EditorGUILayout.LabelField("Press 'Validate' to check.", EditorStyles.miniLabel);
        }
        else if (_lastValidationResult.Issues.Count == 0)
        {
            GUILayout.Label("✓ No issues found.", _infoStyle);
        }
        else
        {
            for (int i = 0; i < _lastValidationResult.Issues.Count; i++)
            {
                EpisodeProgressionValidationIssue issue = _lastValidationResult.Issues[i];

                GUIStyle style = issue.Severity == EpisodeProgressionIssueSeverity.Error
                    ? _errorStyle
                    : issue.Severity == EpisodeProgressionIssueSeverity.Warning
                        ? _warningStyle
                        : _infoStyle;

                string prefix = issue.Severity == EpisodeProgressionIssueSeverity.Error
                    ? "✕" : issue.Severity == EpisodeProgressionIssueSeverity.Warning
                        ? "⚠" : "ℹ";

                string text = string.IsNullOrEmpty(issue.ContextId)
                    ? $"{prefix} {issue.Message}"
                    : $"{prefix} [{issue.ContextId}] {issue.Message}";

                GUILayout.Label(text, style);
            }
        }

        GUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: Condition 목록
    // ─────────────────────────────────────────────────────────

    private void DrawConditionList(
        SerializedProperty listProp,
        List<EpisodeCondition> list,
        string context)
    {
        if (listProp == null)
            return;

        GUILayout.BeginVertical(EditorStyles.helpBox);

        for (int i = 0; i < list.Count; i++)
        {
            SerializedProperty cond = listProp.GetArrayElementAtIndex(i);

            GUILayout.BeginHorizontal();

            DrawEnumField(cond, "Kind", 70f);
            DrawTextField(cond, "Key", 90f);
            DrawEnumField(cond, "Op", 80f);

            EpisodeConditionKind kind = (EpisodeConditionKind)cond.FindPropertyRelative("Kind").enumValueIndex;
            EpisodeCompareOp     op   = (EpisodeCompareOp)cond.FindPropertyRelative("Op").enumValueIndex;

            if (op != EpisodeCompareOp.Exists && op != EpisodeCompareOp.NotExists)
            {
                if (kind == EpisodeConditionKind.Stat)
                    EditorGUILayout.PropertyField(
                        cond.FindPropertyRelative("IntValue"),
                        GUIContent.none, GUILayout.Width(48f));
                else if (kind == EpisodeConditionKind.Flag)
                    EditorGUILayout.PropertyField(
                        cond.FindPropertyRelative("BoolValue"),
                        GUIContent.none, GUILayout.Width(18f));
                else
                    DrawTextField(cond, "StringValue", 70f);
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

            if (GUILayout.Button("×", GUILayout.Width(20f)))
            {
                Undo.RecordObject(_target, "Remove Condition");
                list.RemoveAt(i);
                EditorUtility.SetDirty(_target);
                _validationDirty = true;
                break;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button($"+ Add Condition ({context})", GUILayout.ExpandWidth(true)))
        {
            Undo.RecordObject(_target, "Add Condition");
            list.Add(new EpisodeCondition());
            EditorUtility.SetDirty(_target);
            _validationDirty = true;
        }

        GUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: NextOption 목록
    // ─────────────────────────────────────────────────────────

    private void DrawNextOptionList(
        SerializedProperty listProp,
        List<EpisodeNextOption> list,
        string ownerEpisodeId)
    {
        if (listProp == null)
            return;

        string[] allIds = CollectAllEpisodeIds();

        GUILayout.BeginVertical(EditorStyles.helpBox);

        for (int i = 0; i < list.Count; i++)
        {
            SerializedProperty optProp = listProp.GetArrayElementAtIndex(i);
            EpisodeNextOption  opt     = list[i];

            GUILayout.BeginVertical(EditorStyles.helpBox);

            // 헤더 행
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Option [{i}]", EditorStyles.boldLabel, GUILayout.Width(80f));
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

            if (GUILayout.Button("Remove", GUILayout.Width(60f)))
            {
                Undo.RecordObject(_target, "Remove NextOption");
                list.RemoveAt(i);
                EditorUtility.SetDirty(_target);
                _validationDirty = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                break;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // TargetEpisodeId — Popup 선택
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Episode", GUILayout.Width(LABEL_W));

            int currentIdx = Array.IndexOf(allIds, opt.TargetEpisodeId);
            int newIdx     = EditorGUILayout.Popup(Mathf.Max(0, currentIdx), allIds);

            if (newIdx >= 0 && newIdx < allIds.Length && allIds[newIdx] != opt.TargetEpisodeId)
            {
                Undo.RecordObject(_target, "Set NextOption Target");
                optProp.FindPropertyRelative("TargetEpisodeId").stringValue = allIds[newIdx];
                opt.TargetEpisodeId = allIds[newIdx];
                EditorUtility.SetDirty(_target);
                _validationDirty = true;
            }

            // 직접 입력도 허용
            string typed = EditorGUILayout.TextField(opt.TargetEpisodeId, GUILayout.Width(100f));

            if (typed != opt.TargetEpisodeId)
            {
                Undo.RecordObject(_target, "Set NextOption Target");
                optProp.FindPropertyRelative("TargetEpisodeId").stringValue = typed;
                EditorUtility.SetDirty(_target);
                _validationDirty = true;
            }

            GUILayout.EndHorizontal();

            DrawPropField(optProp, "ChoiceLabel",     "Choice Label");
            DrawPropField(optProp, "HideWhenLocked",  "Hide When Locked");

            if (!opt.HideWhenLocked)
                DrawPropField(optProp, "LockedReasonText", "Locked Reason Text");

            // 조건
            SerializedProperty condProp = optProp.FindPropertyRelative("Conditions");
            DrawConditionList(condProp, opt.Conditions, $"NextOption[{i}]");

            GUILayout.EndVertical();
            GUILayout.Space(2f);
        }

        if (GUILayout.Button("+ Add Next Option", GUILayout.ExpandWidth(true)))
        {
            Undo.RecordObject(_target, "Add NextOption");
            list.Add(new EpisodeNextOption());
            EditorUtility.SetDirty(_target);
            _validationDirty = true;
        }

        GUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: Attachment 목록
    // ─────────────────────────────────────────────────────────

    private void DrawAttachmentList(
        SerializedProperty listProp,
        List<EpisodeAttachmentDefinition> list,
        string ownerEpisodeId)
    {
        if (listProp == null)
            return;

        GUILayout.BeginVertical(EditorStyles.helpBox);

        for (int i = 0; i < list.Count; i++)
        {
            SerializedProperty attProp = listProp.GetArrayElementAtIndex(i);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Attachment [{i}]", EditorStyles.boldLabel, GUILayout.Width(120f));
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);

            if (GUILayout.Button("Remove", GUILayout.Width(60f)))
            {
                Undo.RecordObject(_target, "Remove Attachment");
                list.RemoveAt(i);
                EditorUtility.SetDirty(_target);
                _validationDirty = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                break;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            DrawPropField(attProp, "AttachmentId",    "Attachment ID");
            DrawPropField(attProp, "ParentEpisodeId", "Parent Episode ID");
            DrawPropField(attProp, "Title",           "Title");
            DrawPropField(attProp, "IndexText",       "Index Text");
            DrawPropField(attProp, "Kind",            "Kind");
            DrawPropField(attProp, "DialogueEntryId", "Dialogue Entry ID");
            DrawPropField(attProp, "IsRepeatable",    "Is Repeatable");
            DrawPropField(attProp, "DesignerNote",    "Designer Note");

            // 조건
            SerializedProperty visibleProp = attProp.FindPropertyRelative("VisibleConditions");
            SerializedProperty unlockProp  = attProp.FindPropertyRelative("UnlockConditions");

            DrawConditionList(visibleProp, list[i].VisibleConditions, $"Att[{i}].Visible");
            DrawConditionList(unlockProp,  list[i].UnlockConditions,  $"Att[{i}].Unlock");

            GUILayout.EndVertical();
            GUILayout.Space(2f);
        }

        if (GUILayout.Button("+ Add Attachment", GUILayout.ExpandWidth(true)))
        {
            Undo.RecordObject(_target, "Add Attachment");

            list.Add(new EpisodeAttachmentDefinition
            {
                // 현재 선택된 EpisodeId를 ParentEpisodeId로 자동 입력
                ParentEpisodeId = ownerEpisodeId
            });

            EditorUtility.SetDirty(_target);
            _validationDirty = true;
        }

        GUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────────────────
    // 서브 편집기: EndingKey Popup
    // ─────────────────────────────────────────────────────────

    private void DrawEndingKeyPopup(SerializedProperty nodeProp, EpisodeNodeDefinition node)
    {
        string[] endingKeys = CollectEndingKeys();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Ending Key", GUILayout.Width(LABEL_W));

        int currentIdx = Array.IndexOf(endingKeys, node.EndingKey);
        int newIdx     = EditorGUILayout.Popup(Mathf.Max(0, currentIdx), endingKeys);

        if (newIdx >= 0 && newIdx < endingKeys.Length)
        {
            string selected = endingKeys[newIdx];

            if (selected != node.EndingKey && selected != "(none)")
            {
                Undo.RecordObject(_target, "Set EndingKey");
                nodeProp.FindPropertyRelative("EndingKey").stringValue = selected;
                EditorUtility.SetDirty(_target);
                _validationDirty = true;
            }
        }

        string typed = EditorGUILayout.TextField(node.EndingKey, GUILayout.Width(100f));

        if (typed != node.EndingKey)
        {
            Undo.RecordObject(_target, "Set EndingKey");
            nodeProp.FindPropertyRelative("EndingKey").stringValue = typed;
            EditorUtility.SetDirty(_target);
            _validationDirty = true;
        }

        GUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────────────────
    // Node CRUD
    // ─────────────────────────────────────────────────────────

    private void AddNode()
    {
        Undo.RecordObject(_target, "Add Episode Node");

        _target.Nodes.Add(new EpisodeNodeDefinition
        {
            EpisodeId      = GenerateUniqueEpisodeId("new_episode"),
            Title          = "New Episode",
            DialogueEntryId = ""
        });

        EditorUtility.SetDirty(_target);
        SelectNode(_target.Nodes.Count - 1);
        _validationDirty = true;
    }

    private void DuplicateNode(int index)
    {
        if (index < 0 || index >= _target.Nodes.Count)
            return;

        Undo.RecordObject(_target, "Duplicate Episode Node");

        EpisodeNodeDefinition original = _target.Nodes[index];

        EpisodeNodeDefinition copy = new EpisodeNodeDefinition
        {
            EpisodeId               = GenerateUniqueEpisodeId(original.EpisodeId + "_copy"),
            Title                   = original.Title,
            IndexText               = original.IndexText,
            Kind                    = original.Kind,
            DialogueEntryId         = original.DialogueEntryId,
            IsChapterEndingCandidate = original.IsChapterEndingCandidate,
            EndingKey               = original.EndingKey,
            DesignerNote            = original.DesignerNote,
        };

        // 리스트 딥 카피
        foreach (EpisodeCondition c in original.VisibleConditions)
            copy.VisibleConditions.Add(CloneCondition(c));

        foreach (EpisodeCondition c in original.UnlockConditions)
            copy.UnlockConditions.Add(CloneCondition(c));

        foreach (EpisodeNextOption o in original.NextOptions)
            copy.NextOptions.Add(CloneNextOption(o));

        // Attachment는 AttachmentId가 unique 해야 하므로 suffix 추가
        foreach (EpisodeAttachmentDefinition a in original.Attachments)
            copy.Attachments.Add(CloneAttachment(a, copy.EpisodeId));

        _target.Nodes.Insert(index + 1, copy);
        EditorUtility.SetDirty(_target);
        SelectNode(index + 1);
        _validationDirty = true;
    }

    private void DeleteNode(int index)
    {
        if (index < 0 || index >= _target.Nodes.Count)
            return;

        Undo.RecordObject(_target, "Delete Episode Node");
        _target.Nodes.RemoveAt(index);
        EditorUtility.SetDirty(_target);

        _selectedNodeIndex = Mathf.Clamp(index - 1, -1, _target.Nodes.Count - 1);
        _validationDirty = true;
    }

    private void SelectNode(int index)
    {
        _selectedNodeIndex = index;
        _foldVisible    = true;
        _foldUnlock     = true;
        _foldNext       = true;
        _foldAttachments = true;
        Repaint();
    }

    // ─────────────────────────────────────────────────────────
    // EndingRule CRUD
    // ─────────────────────────────────────────────────────────

    private void AddEndingRule()
    {
        Undo.RecordObject(_target, "Add Ending Rule");

        _target.EndingRules.Add(new ChapterEndingRule
        {
            EndingKey   = GenerateUniqueEndingKey("new_ending"),
            DisplayName = "New Ending"
        });

        EditorUtility.SetDirty(_target);
        _selectedEndingIndex = _target.EndingRules.Count - 1;
        _validationDirty = true;
    }

    private void DeleteEndingRule(int index)
    {
        if (index < 0 || index >= _target.EndingRules.Count)
            return;

        Undo.RecordObject(_target, "Delete Ending Rule");
        _target.EndingRules.RemoveAt(index);
        EditorUtility.SetDirty(_target);
        _selectedEndingIndex = Mathf.Clamp(index - 1, -1, _target.EndingRules.Count - 1);
        _validationDirty = true;
    }

    // ─────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────

    private void RunValidation()
    {
        _serializedObject.ApplyModifiedProperties();
        _lastValidationResult = EpisodeProgressionValidator.Validate(_target);
        Repaint();
    }

    // ─────────────────────────────────────────────────────────
    // Asset 관리
    // ─────────────────────────────────────────────────────────

    private void SetTarget(ChapterEpisodeProgressionSO target)
    {
        _target              = target;
        _serializedObject    = target != null ? new SerializedObject(target) : null;
        _selectedNodeIndex   = -1;
        _selectedEndingIndex = -1;
        _lastValidationResult = null;

        if (_target != null && _target.Nodes.Count > 0)
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

        asset.ChapterId   = "ch_new";
        asset.DisplayName = "New Chapter";

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        SetTarget(asset);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 — 고유 ID 생성
    // ─────────────────────────────────────────────────────────

    private string GenerateUniqueEpisodeId(string seed)
    {
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
        if (_target == null || _target.EndingRules == null)
            return seed;

        bool exists = false;

        for (int i = 0; i < _target.EndingRules.Count; i++)
        {
            if (_target.EndingRules[i] != null
                && string.Equals(_target.EndingRules[i].EndingKey, seed, StringComparison.Ordinal))
            {
                exists = true;
                break;
            }
        }

        if (!exists)
            return seed;

        for (int i = 2; i < 100; i++)
        {
            string candidate = $"{seed}_{i}";
            bool dup = false;

            for (int j = 0; j < _target.EndingRules.Count; j++)
            {
                if (_target.EndingRules[j] != null
                    && string.Equals(_target.EndingRules[j].EndingKey, candidate, StringComparison.Ordinal))
                {
                    dup = true;
                    break;
                }
            }

            if (!dup)
                return candidate;
        }

        return seed + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 — 목록 수집
    // ─────────────────────────────────────────────────────────

    private string[] CollectAllEpisodeIds()
    {
        if (_target == null || _target.Nodes == null)
            return new[] { "(none)" };

        List<string> ids = new List<string> { "(none)" };

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
            return new[] { "(none)" };

        List<string> keys = new List<string> { "(none)" };

        for (int i = 0; i < _target.EndingRules.Count; i++)
        {
            ChapterEndingRule r = _target.EndingRules[i];

            if (r != null && !string.IsNullOrWhiteSpace(r.EndingKey))
                keys.Add(r.EndingKey);
        }

        return keys.ToArray();
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 — 딥 클론
    // ─────────────────────────────────────────────────────────

    private static EpisodeCondition CloneCondition(EpisodeCondition src)
    {
        return new EpisodeCondition
        {
            Kind        = src.Kind,
            Key         = src.Key,
            Op          = src.Op,
            IntValue    = src.IntValue,
            BoolValue   = src.BoolValue,
            StringValue = src.StringValue
        };
    }

    private static EpisodeNextOption CloneNextOption(EpisodeNextOption src)
    {
        EpisodeNextOption copy = new EpisodeNextOption
        {
            TargetEpisodeId = src.TargetEpisodeId,
            ChoiceLabel     = src.ChoiceLabel,
            HideWhenLocked  = src.HideWhenLocked,
            LockedReasonText = src.LockedReasonText
        };

        foreach (EpisodeCondition c in src.Conditions)
            copy.Conditions.Add(CloneCondition(c));

        return copy;
    }

    private static EpisodeAttachmentDefinition CloneAttachment(
        EpisodeAttachmentDefinition src,
        string newParentEpisodeId)
    {
        EpisodeAttachmentDefinition copy = new EpisodeAttachmentDefinition
        {
            AttachmentId    = src.AttachmentId + "_copy",
            ParentEpisodeId = newParentEpisodeId,
            Title           = src.Title,
            IndexText       = src.IndexText,
            Kind            = src.Kind,
            DialogueEntryId = src.DialogueEntryId,
            IsRepeatable    = src.IsRepeatable,
            DesignerNote    = src.DesignerNote
        };

        foreach (EpisodeCondition c in src.VisibleConditions)
            copy.VisibleConditions.Add(CloneCondition(c));

        foreach (EpisodeCondition c in src.UnlockConditions)
            copy.UnlockConditions.Add(CloneCondition(c));

        return copy;
    }

    // ─────────────────────────────────────────────────────────
    // 헬퍼 — 필드 드로어
    // ─────────────────────────────────────────────────────────

    private static void DrawPropField(SerializedProperty parent, string propName, string label)
    {
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
        SerializedProperty prop = parent.FindPropertyRelative(propName);

        if (prop == null)
            return;

        prop.stringValue = EditorGUILayout.TextField(prop.stringValue, GUILayout.Width(width));
    }

    private static void DrawEnumField(SerializedProperty parent, string propName, float width)
    {
        SerializedProperty prop = parent.FindPropertyRelative(propName);

        if (prop == null)
            return;

        EditorGUILayout.PropertyField(prop, GUIContent.none, GUILayout.Width(width));
    }

    // ─────────────────────────────────────────────────────────
    // 스타일 초기화 (OnGUI에서 지연 호출)
    // ─────────────────────────────────────────────────────────

    private void EnsureStyles()
    {
        if (_stylesInitialized)
            return;

        _stylesInitialized = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 12,
            alignment = TextAnchor.MiddleLeft
        };
        _headerStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        _sectionStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Italic
        };
        _sectionStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);

        _nodeRowStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(8, 4, 0, 0)
        };
        _nodeRowStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        _nodeRowSelectedStyle = new GUIStyle(_nodeRowStyle);
        _nodeRowSelectedStyle.normal.background = MakeTex(1, 1, new Color(0.22f, 0.45f, 0.7f, 0.6f));
        _nodeRowSelectedStyle.normal.textColor  = Color.white;

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
