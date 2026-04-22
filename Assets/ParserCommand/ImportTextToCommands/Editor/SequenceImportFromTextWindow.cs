#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class SequenceImportFromTextWindow : EditorWindow
{
    private SequenceSpecSO _target;
    private YarnCommandBridge _bridge;
    private string _text;
    private bool _replaceCurrentNodes;
    private Vector2 _scroll;

    private List<NodeSpec> _rollbackNodes;
    private bool _hasRollbackSnapshot;

    private const string RecipeTextControlName = "SequenceImportFromTextWindow_RecipeText";

    private readonly List<string> _recipeTextHistory = new();
    private int _recipeTextHistoryIndex = -1;
    private bool _suppressRecipeHistoryPush;

    [MenuItem("Tools/CPS/Import Sequence From Text")]
    public static void Open()
    {
        GetWindow<SequenceImportFromTextWindow>("Import Sequence From Text");
    }

    private void OnEnable()
    {
        minSize = new Vector2(430f, 420f);
        ResetRecipeTextHistory(_text ?? string.Empty);
    }

    private void OnGUI()
    {
        DrawHeader();

        _target = (SequenceSpecSO)EditorGUILayout.ObjectField(
            "Target Sequence",
            _target,
            typeof(SequenceSpecSO),
            false);

        _bridge = (YarnCommandBridge)EditorGUILayout.ObjectField(
            "Bridge",
            _bridge,
            typeof(YarnCommandBridge),
            true);

        if (_replaceCurrentNodes)
        {
            EditorGUILayout.HelpBox(
                "Replace Mode is ON.\nImport will clear existing nodes in Target Sequence before importing.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Recipe Text");

        Rect textRect = GUILayoutUtility.GetRect(
            10f,
            Mathf.Max(200f, position.height - 180f),
            GUILayout.ExpandWidth(true));

        float contentWidth = Mathf.Max(0f, textRect.width - 16f);
        float contentHeight = GetTextHeight(contentWidth);

        _scroll = GUI.BeginScrollView(
            textRect,
            _scroll,
            new Rect(0, 0, contentWidth, contentHeight));

        GUI.SetNextControlName(RecipeTextControlName);

        string beforeText = _text ?? string.Empty;

        _text = EditorGUI.TextArea(
            new Rect(0, 0, contentWidth, contentHeight),
            beforeText);

        HandleRecipeTextShortcuts();

        if (_text != beforeText)
            RecordRecipeTextHistory(_text ?? string.Empty);

        GUI.EndScrollView();

        EditorGUILayout.Space(8);

        bool canImport =
            _target != null &&
            _bridge != null &&
            !string.IsNullOrWhiteSpace(_text);

        DrawImportRow(canImport);
    }

    private float GetTextHeight(float width)
    {
        GUIStyle style = EditorStyles.textArea;
        float minHeight = Mathf.Max(10f, position.height - 190f);
        float contentHeight = style.CalcHeight(new GUIContent(_text ?? string.Empty), width);
        return Mathf.Max(minHeight, contentHeight + 8f);
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        Color prevBg = GUI.backgroundColor;
        Color prevContent = GUI.contentColor;

        if (_replaceCurrentNodes)
        {
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            GUI.contentColor = Color.white;
        }

        bool clickedValue = GUILayout.Toggle(
            _replaceCurrentNodes,
            _replaceCurrentNodes ? "Replace ON" : "Replace OFF",
            EditorStyles.toolbarButton,
            GUILayout.Width(110));

        GUI.backgroundColor = prevBg;
        GUI.contentColor = prevContent;

        if (clickedValue != _replaceCurrentNodes)
        {
            if (clickedValue)
            {
                _replaceCurrentNodes = true;
            }
            else
            {
                _replaceCurrentNodes = false;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawImportRow(bool canImport)
    {
        EditorGUILayout.BeginHorizontal();

        bool canRollback = _target != null && _hasRollbackSnapshot;

        Color prevBg = GUI.backgroundColor;
        Color prevContent = GUI.contentColor;
        bool prevEnabled = GUI.enabled;

        GUI.enabled = canImport;

        if (_replaceCurrentNodes)
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);

        if (GUILayout.Button("Import", GUILayout.Height(28), GUILayout.Width(140)))
        {
            RunImport();
        }

        GUI.backgroundColor = prevBg;

        GUILayout.Space(8);

        DrawReplaceModeBadgeSlot(110);

        GUILayout.Space(8);

        GUI.enabled = canRollback;
        if (GUILayout.Button("Rollback Last Import", GUILayout.Height(28), GUILayout.Width(150)))
        {
            RollbackLastImport();
        }

        GUI.backgroundColor = prevBg;
        GUI.contentColor = prevContent;
        GUI.enabled = prevEnabled;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawReplaceModeBadgeSlot(float width)
    {
        Rect rect = GUILayoutUtility.GetRect(width, 22, GUILayout.Width(width));

        if (!_replaceCurrentNodes)
            return;

        Color prev = GUI.color;
        GUI.color = new Color(1f, 0.85f, 0.85f);
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        GUI.color = prev;

        GUIStyle badge = new GUIStyle(EditorStyles.miniBoldLabel);
        badge.alignment = TextAnchor.MiddleCenter;
        badge.normal.textColor = new Color(0.75f, 0.15f, 0.15f);

        GUI.Label(rect, "REPLACE MODE", badge);
    }

    private void RunImport()
    {
        if (_target == null || _bridge == null)
            return;

        bool usedReplaceMode = _replaceCurrentNodes;

        SaveRollbackSnapshot(_target);

        var importer = new SequenceTextImporter(_bridge);
        ImportResult result = importer.ImportToSequence(_text, _target, usedReplaceMode);

        Debug.Log(
            $"[SequenceImportFromText] replace={usedReplaceMode}, parsed={result.parsedLineCount}, imported={result.importedCommandCount}, warnings={result.warnings.Count}, errors={result.errors.Count}",
            _target);

        for (int i = 0; i < result.warnings.Count; i++)
            Debug.LogWarning(result.warnings[i], _target);

        for (int i = 0; i < result.errors.Count; i++)
            Debug.LogError(result.errors[i], _target);

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();

        _replaceCurrentNodes = false;
    }

    private void RollbackLastImport()
    {
        if (_target == null || !_hasRollbackSnapshot || _rollbackNodes == null)
            return;

        _target.nodes = CloneNodes(_rollbackNodes);

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[SequenceImportFromText] rollback restored {_target.nodes.Count} node(s).",
            _target);

        _hasRollbackSnapshot = false;
        _rollbackNodes = null;
    }

    private void SaveRollbackSnapshot(SequenceSpecSO target)
    {
        if (target == null)
            return;

        _rollbackNodes = CloneNodes(target.nodes);
        _hasRollbackSnapshot = true;
    }

    private static List<NodeSpec> CloneNodes(List<NodeSpec> source)
    {
        if (source == null)
            return new List<NodeSpec>();

        var clone = new List<NodeSpec>(source.Count);

        for (int i = 0; i < source.Count; i++)
        {
            NodeSpec srcNode = source[i];

            if (srcNode == null)
            {
                clone.Add(null);
                continue;
            }

            var dstNode = new NodeSpec
            {
                editorName = srcNode.editorName,
                steps = new List<StepSpec>()
            };

            if (srcNode.steps != null)
            {
                for (int s = 0; s < srcNode.steps.Count; s++)
                {
                    StepSpec srcStep = srcNode.steps[s];

                    if (srcStep == null)
                    {
                        dstNode.steps.Add(null);
                        continue;
                    }

                    var dstStep = new StepSpec
                    {
                        editorName = srcStep.editorName,
                        gate = srcStep.gate,
                        compiled = CloneCompiledSpecs(srcStep.compiled)
                    };

                    dstNode.steps.Add(dstStep);
                }
            }

            clone.Add(dstNode);
        }

        return clone;
    }

    private static List<CommandSpecBase> CloneCompiledSpecs(List<CommandSpecBase> source)
    {
        if (source == null)
            return new List<CommandSpecBase>();

        var clone = new List<CommandSpecBase>(source.Count);

        for (int i = 0; i < source.Count; i++)
        {
            CommandSpecBase src = source[i];

            if (src == null)
            {
                clone.Add(null);
                continue;
            }

            clone.Add(CloneCommandSpec(src));
        }

        return clone;
    }

    private static CommandSpecBase CloneCommandSpec(CommandSpecBase src)
    {
        string json = JsonUtility.ToJson(src);
        Type type = src.GetType();

        var copied = (CommandSpecBase)Activator.CreateInstance(type);
        JsonUtility.FromJsonOverwrite(json, copied);

        return copied;
    }

    private void HandleRecipeTextShortcuts()
    {
        Event e = Event.current;
        if (e == null)
            return;

        if (GUI.GetNameOfFocusedControl() != RecipeTextControlName)
            return;

        TextEditor editor = GetFocusedRecipeTextEditor();
        if (editor == null)
            return;

        if (e.type == EventType.ValidateCommand)
        {
            switch (e.commandName)
            {
                case "Copy":
                case "Paste":
                case "Cut":
                case "SelectAll":
                    e.Use();
                    return;
            }
        }

        if (e.type == EventType.ExecuteCommand)
        {
            switch (e.commandName)
            {
                case "Copy":
                    editor.Copy();
                    e.Use();
                    return;

                case "Paste":
                    editor.Paste();
                    _text = editor.text;
                    RecordRecipeTextHistory(_text ?? string.Empty);
                    GUI.changed = true;
                    e.Use();
                    return;

                case "Cut":
                    editor.Cut();
                    _text = editor.text;
                    RecordRecipeTextHistory(_text ?? string.Empty);
                    GUI.changed = true;
                    e.Use();
                    return;

                case "SelectAll":
                    editor.SelectAll();
                    e.Use();
                    return;
            }
        }

        if (e.type == EventType.KeyDown && IsActionKeyPressed(e))
        {
            if (!e.shift && e.keyCode == KeyCode.Z)
            {
                ApplyRecipeTextUndo();
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Y || (e.shift && e.keyCode == KeyCode.Z))
            {
                ApplyRecipeTextRedo();
                e.Use();
                return;
            }
        }
    }

    private static bool IsActionKeyPressed(Event e)
    {
        if (e == null)
            return false;

#if UNITY_EDITOR_OSX
        return e.command;
#else
        return e.control;
#endif
    }

    private TextEditor GetFocusedRecipeTextEditor()
    {
        if (GUI.GetNameOfFocusedControl() != RecipeTextControlName)
            return null;

        return GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
    }

    private void ResetRecipeTextHistory(string text)
    {
        _recipeTextHistory.Clear();
        _recipeTextHistory.Add(text ?? string.Empty);
        _recipeTextHistoryIndex = 0;
        _suppressRecipeHistoryPush = false;
    }

    private void RecordRecipeTextHistory(string text)
    {
        if (_suppressRecipeHistoryPush)
        {
            _suppressRecipeHistoryPush = false;
            return;
        }

        text ??= string.Empty;

        if (_recipeTextHistoryIndex >= 0 &&
            _recipeTextHistoryIndex < _recipeTextHistory.Count &&
            _recipeTextHistory[_recipeTextHistoryIndex] == text)
        {
            return;
        }

        if (_recipeTextHistoryIndex < _recipeTextHistory.Count - 1)
        {
            _recipeTextHistory.RemoveRange(
                _recipeTextHistoryIndex + 1,
                _recipeTextHistory.Count - (_recipeTextHistoryIndex + 1));
        }

        _recipeTextHistory.Add(text);
        _recipeTextHistoryIndex = _recipeTextHistory.Count - 1;
    }

    private void ApplyRecipeTextUndo()
    {
        if (_recipeTextHistoryIndex <= 0)
            return;

        _recipeTextHistoryIndex--;
        _text = _recipeTextHistory[_recipeTextHistoryIndex];
        _suppressRecipeHistoryPush = true;
        SyncRecipeTextEditorBuffer();
        Repaint();
    }

    private void ApplyRecipeTextRedo()
    {
        if (_recipeTextHistoryIndex >= _recipeTextHistory.Count - 1)
            return;

        _recipeTextHistoryIndex++;
        _text = _recipeTextHistory[_recipeTextHistoryIndex];
        _suppressRecipeHistoryPush = true;
        SyncRecipeTextEditorBuffer();
        Repaint();
    }

    private void SyncRecipeTextEditorBuffer()
    {
        TextEditor editor = GetFocusedRecipeTextEditor();
        if (editor == null)
            return;

        string value = _text ?? string.Empty;

        editor.text = value;
        editor.cursorIndex = value.Length;
        editor.selectIndex = value.Length;
        editor.graphicalCursorPos = Vector2.zero;
        editor.graphicalSelectCursorPos = Vector2.zero;

        GUI.changed = true;
    }
}
#endif