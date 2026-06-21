using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RollbackHistoryDebugView))]
public sealed class RollbackHistoryDebugViewEditor : Editor
{
    private bool showSummary = true;
    private bool showPoints = true;
    private Vector2 scroll;

    public override void OnInspectorGUI()
    {
        RollbackHistoryDebugView view = (RollbackHistoryDebugView)target;

        serializedObject.Update();

        showSummary = EditorGUILayout.Foldout(showSummary, "Summary", true);
        if (showSummary)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Count", view.Count.ToString());
            EditorGUILayout.LabelField("Can Rollback One Step", view.CanRollbackOneStep.ToString());

            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(4);

        showPoints = EditorGUILayout.Foldout(showPoints, "Rollback Points", true);
        if (showPoints)
        {
            EditorGUI.indentLevel++;

            SerializedProperty points = serializedObject.FindProperty("points");

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160), GUILayout.MaxHeight(420));

            for (int i = 0; i < points.arraySize; i++)
            {
                SerializedProperty point = points.GetArrayElementAtIndex(i);

                SerializedProperty historyIndex = point.FindPropertyRelative("historyIndex");
                SerializedProperty rawText = point.FindPropertyRelative("rawText");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("History Index", historyIndex.intValue.ToString());
                EditorGUILayout.TextArea(rawText.stringValue, GUILayout.MinHeight(36));

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}