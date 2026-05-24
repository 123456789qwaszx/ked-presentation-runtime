using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RollbackHistoryDebugView))]
public sealed class RollbackHistoryDebugViewEditor : Editor
{
    private bool showSummary = true;
    private bool showLatest = true;
    private bool showNextTarget = true;
    private bool showPoints = true;
    private Vector2 scroll;

    public override void OnInspectorGUI()
    {
        RollbackHistoryDebugView view = (RollbackHistoryDebugView)target;

        serializedObject.Update();

        DrawDefaultControls(view);

        EditorGUILayout.Space(8);

        showSummary = EditorGUILayout.Foldout(showSummary, "Summary", true);
        if (showSummary)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Is Bound", view.IsBound.ToString());
            EditorGUILayout.LabelField("Count", view.Count.ToString());
            EditorGUILayout.LabelField("Can Rollback One Step", view.CanRollbackOneStep.ToString());
            EditorGUILayout.LabelField("Latest List Index", view.LatestListIndex.ToString());
            EditorGUILayout.LabelField("Next Rollback Target Index", view.NextRollbackTargetListIndex.ToString());

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        SerializedProperty latestPoint = serializedObject.FindProperty("latestPoint");
        SerializedProperty nextRollbackTarget = serializedObject.FindProperty("nextRollbackTarget");

        showLatest = EditorGUILayout.Foldout(showLatest, "Latest Point", true);
        if (showLatest)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(latestPoint, true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        showNextTarget = EditorGUILayout.Foldout(showNextTarget, "Next Rollback Target", true);
        if (showNextTarget)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(nextRollbackTarget, true);
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

                SerializedProperty listIndex = point.FindPropertyRelative("listIndex");
                SerializedProperty historyIndex = point.FindPropertyRelative("historyIndex");
                SerializedProperty nodeName = point.FindPropertyRelative("nodeName");
                SerializedProperty lineId = point.FindPropertyRelative("lineId");
                SerializedProperty rawText = point.FindPropertyRelative("rawText");

                string title = BuildPointTitle(
                    view,
                    listIndex.intValue,
                    historyIndex.intValue,
                    nodeName.stringValue,
                    lineId.stringValue);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("List Index", listIndex.intValue.ToString());
                EditorGUILayout.LabelField("History Index", historyIndex.intValue.ToString());
                EditorGUILayout.LabelField("Node", nodeName.stringValue);
                EditorGUILayout.LabelField("Line", lineId.stringValue);

                EditorGUILayout.LabelField("Raw Text");
                EditorGUILayout.TextArea(rawText.stringValue, GUILayout.MinHeight(36));

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefaultControls(RollbackHistoryDebugView view)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh"))
        {
            view.RefreshSnapshot();
            EditorUtility.SetDirty(view);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Clear History"))
            {
                if (EditorUtility.DisplayDialog(
                        "Clear Rollback History",
                        "RollbackHistory를 비울까요?",
                        "Clear",
                        "Cancel"))
                {
                    view.ClearHistory();
                    EditorUtility.SetDirty(view);
                }
            }
        }

        if (GUILayout.Button("Dump"))
        {
            view.DumpToConsole();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        SerializedProperty autoRefresh = serializedObject.FindProperty("autoRefresh");
        SerializedProperty refreshInterval = serializedObject.FindProperty("refreshInterval");

        EditorGUILayout.PropertyField(autoRefresh);
        EditorGUILayout.PropertyField(refreshInterval);
    }

    private static string BuildPointTitle(
        RollbackHistoryDebugView view,
        int listIndex,
        int historyIndex,
        string nodeName,
        string lineId)
    {
        string marker = "";

        if (listIndex == view.LatestListIndex)
            marker += " [Latest]";

        if (listIndex == view.NextRollbackTargetListIndex)
            marker += " [Next Target]";

        return $"[{listIndex}] historyIndex={historyIndex} / {nodeName}/{lineId}{marker}";
    }
}