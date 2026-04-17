#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class SequenceImportFromTextWindow : EditorWindow
{
    private SequenceSpecSO _target;
    private YarnCommandBridge _bridge;
    private string _text;
    private bool _replaceCurrentNodes = true;
    private Vector2 _scroll;

    [MenuItem("Tools/CPS/Import Sequence From Text")]
    public static void Open()
    {
        GetWindow<SequenceImportFromTextWindow>("Import Sequence From Text");
    }

    private void OnGUI()
    {
        _target = (SequenceSpecSO)EditorGUILayout.ObjectField("Target Sequence", _target, typeof(SequenceSpecSO), false);
        _bridge = (YarnCommandBridge)EditorGUILayout.ObjectField("Bridge", _bridge, typeof(YarnCommandBridge), true);
        _replaceCurrentNodes = EditorGUILayout.Toggle("Replace Current Nodes", _replaceCurrentNodes);

        EditorGUILayout.LabelField("Recipe Text");
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(300));
        _text = EditorGUILayout.TextArea(_text, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUI.enabled = _target != null && _bridge != null && !string.IsNullOrWhiteSpace(_text);

        if (GUILayout.Button("Import"))
        {
            var importer = new SequenceTextImporter(_bridge);
            ImportResult result = importer.ImportToSequence(_text, _target, _replaceCurrentNodes);

            Debug.Log(
                $"[SequenceImportFromText] parsed={result.parsedLineCount}, imported={result.importedCommandCount}, warnings={result.warnings.Count}, errors={result.errors.Count}",
                _target);

            for (int i = 0; i < result.warnings.Count; i++)
                Debug.LogWarning(result.warnings[i], _target);

            for (int i = 0; i < result.errors.Count; i++)
                Debug.LogError(result.errors[i], _target);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(_target);
        }

        GUI.enabled = true;
    }
}
#endif