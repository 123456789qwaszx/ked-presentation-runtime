#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class VNStoryGraphEditorWindow : EditorWindow
{
    private VNStoryGraphSO graph;
    private Vector2 scroll;
    private string validationReport;

    [MenuItem("Tools/VN/Story Graph Editor")]
    public static void Open()
    {
        VNStoryGraphEditorWindow window =
            GetWindow<VNStoryGraphEditorWindow>();

        window.titleContent = new GUIContent("VN Story Graph");
        window.minSize = new Vector2(640f, 520f);
        window.Show();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (graph == null)
        {
            EditorGUILayout.HelpBox(
                "VNStoryGraphSO를 선택하세요.",
                MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawGraphInfo();
        DrawNodeList();
        DrawValidationReport();

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        graph = (VNStoryGraphSO)EditorGUILayout.ObjectField(
            graph,
            typeof(VNStoryGraphSO),
            false,
            GUILayout.MinWidth(240f));

        if (GUILayout.Button("Create", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            CreateGraphAsset();

        using (new EditorGUI.DisabledScope(graph == null))
        {
            if (GUILayout.Button("Add Main", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                AddNode(VNStoryNodeKind.Main);

            if (GUILayout.Button("Add Attachment", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                AddNode(VNStoryNodeKind.Attachment);

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                Validate();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                Save();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawGraphInfo()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        graph.graphId = EditorGUILayout.TextField("Graph ID", graph.graphId);
        graph.chapterKey = EditorGUILayout.TextField("Chapter Key", graph.chapterKey);
        graph.canvasSize = EditorGUILayout.Vector2Field("Canvas Size", graph.canvasSize);
        graph.gridSize = EditorGUILayout.FloatField("Grid Size", graph.gridSize);

        if (EditorGUI.EndChangeCheck())
            MarkDirty();
    }

    private void DrawNodeList()
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Nodes", EditorStyles.boldLabel);

        if (graph.nodes == null)
            graph.Ensure();

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            VNStoryGraphNode node = graph.nodes[i];
            if (node == null)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                string.IsNullOrWhiteSpace(node.nodeId) ? "(Empty ID)" : node.nodeId,
                EditorStyles.boldLabel);

            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                Undo.RecordObject(graph, "Remove VN Story Node");
                graph.nodes.RemoveAt(i);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            node.nodeId = EditorGUILayout.TextField("Node ID", node.nodeId);
            node.payloadKey = EditorGUILayout.TextField("Payload Key", node.payloadKey);
            node.title = EditorGUILayout.TextField("Title", node.title);
            node.nodeKind = (VNStoryNodeKind)EditorGUILayout.EnumPopup("Kind", node.nodeKind);

            if (node.nodeKind == VNStoryNodeKind.Attachment)
            {
                node.attachmentKind =
                    (VNStoryAttachmentKind)EditorGUILayout.EnumPopup(
                        "Attachment Kind",
                        node.attachmentKind);
            }

            node.position = EditorGUILayout.Vector2Field("Position", node.position);

            DrawNextNodes(node);
            DrawAttachments(node);
            DrawEnding(node);

            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawNextNodes(VNStoryGraphNode node)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Next Nodes", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(node.nodeKind == VNStoryNodeKind.Attachment))
        {
            if (node.nextNodeIds == null)
                node.nextNodeIds = new System.Collections.Generic.List<string>(3);

            int removeIndex = -1;

            for (int i = 0; i < node.nextNodeIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                node.nextNodeIds[i] = EditorGUILayout.TextField("Next " + i, node.nextNodeIds[i]);

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
                node.nextNodeIds.RemoveAt(removeIndex);

            using (new EditorGUI.DisabledScope(node.nextNodeIds.Count >= 3))
            {
                if (GUILayout.Button("+ Add Next Node"))
                    node.nextNodeIds.Add("");
            }
        }
    }

    private void DrawAttachments(VNStoryGraphNode node)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Attachments", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(node.nodeKind == VNStoryNodeKind.Attachment))
        {
            if (node.attachments == null)
                node.attachments = new VNStoryAttachmentRefs();

            node.attachments.upNodeId =
                EditorGUILayout.TextField("Up", node.attachments.upNodeId);

            node.attachments.rightNodeId =
                EditorGUILayout.TextField("Right", node.attachments.rightNodeId);

            node.attachments.downNodeId =
                EditorGUILayout.TextField("Down", node.attachments.downNodeId);
        }
    }

    private void DrawEnding(VNStoryGraphNode node)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Ending", EditorStyles.boldLabel);

        if (node.ending == null)
            node.ending = new VNStoryEndingInfo();

        node.ending.endingKind =
            (VNStoryEndingKind)EditorGUILayout.EnumPopup(
                "Ending Kind",
                node.ending.endingKind);

        node.ending.endingKey =
            EditorGUILayout.TextField("Ending Key", node.ending.endingKey);

        node.ending.opensNextChapter =
            EditorGUILayout.Toggle("Opens Next Chapter", node.ending.opensNextChapter);

        using (new EditorGUI.DisabledScope(!node.ending.opensNextChapter))
        {
            node.ending.nextChapterKey =
                EditorGUILayout.TextField("Next Chapter Key", node.ending.nextChapterKey);
        }

        node.ending.countsAsClear =
            EditorGUILayout.Toggle("Counts As Clear", node.ending.countsAsClear);

        node.ending.isReplayable =
            EditorGUILayout.Toggle("Is Replayable", node.ending.isReplayable);
    }

    private void DrawValidationReport()
    {
        if (string.IsNullOrWhiteSpace(validationReport))
            return;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(validationReport, GUILayout.MinHeight(120f));
    }

    private void AddNode(VNStoryNodeKind kind)
    {
        Undo.RecordObject(graph, "Add VN Story Node");

        graph.Ensure();

        int index = graph.nodes.Count + 1;

        VNStoryGraphNode node = new VNStoryGraphNode
        {
            nodeId = kind == VNStoryNodeKind.Main
                ? "main." + index.ToString("00")
                : "attach." + index.ToString("00"),
            nodeKind = kind,
            title = kind.ToString(),
            position = Vector2.zero
        };

        if (kind == VNStoryNodeKind.Attachment)
            node.attachmentKind = VNStoryAttachmentKind.IfRoute;

        graph.nodes.Add(node);
        MarkDirty();
    }

    private void Validate()
    {
        VNStoryGraphValidationResult result =
            VNStoryGraphValidator.Validate(graph);

        validationReport = result.ToReport();

        if (result.HasError)
            Debug.LogError(validationReport, graph);
        else if (result.HasWarning)
            Debug.LogWarning(validationReport, graph);
        else
            Debug.Log(validationReport, graph);
    }

    private void Save()
    {
        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(graph);
    }

    private void CreateGraphAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create VN Story Graph",
            "VNStoryGraph.asset",
            "asset",
            "Create VN Story Graph asset.");

        if (string.IsNullOrWhiteSpace(path))
            return;

        VNStoryGraphSO asset =
            CreateInstance<VNStoryGraphSO>();

        asset.graphId = "story.graph";
        asset.chapterKey = "chapter";

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        graph = asset;
        Selection.activeObject = asset;
    }
}
#endif