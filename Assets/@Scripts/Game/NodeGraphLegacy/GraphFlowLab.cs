// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;
//
// public sealed class GraphFlowLab : MonoBehaviour
// {
//     [Header("Runtime")]
//     [SerializeField] private bool buildOnStart = true;
//     [SerializeField] private bool verboseLog = true;
//
//     [Header("Layout")]
//     [SerializeField] private Vector2 nodeSize = new Vector2(180f, 90f);
//     [SerializeField] private float mainGapX = 260f;
//     [SerializeField] private float branchOffsetY = 170f;
//     [SerializeField] private Vector2 contentPadding = new Vector2(180f, 220f);
//
//     [Header("Render")]
//     [SerializeField] private Color contentBackgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
//     [SerializeField] private Color nodeNormalColor = new Color(0.22f, 0.24f, 0.3f, 1f);
//     [SerializeField] private Color nodeSelectedColor = new Color(0.95f, 0.75f, 0.25f, 1f);
//     [SerializeField] private Color nodeClearedColor = new Color(0.25f, 0.55f, 0.35f, 1f);
//     [SerializeField] private Color nodeLockedColor = new Color(0.13f, 0.13f, 0.15f, 1f);
//     [SerializeField] private Color edgeColor = new Color(0.65f, 0.65f, 0.72f, 1f);
//
//     private GraphRepository _repository;
//     private GraphViewModelBuilder _builder;
//     private GraphRenderer _renderer;
//     private GraphFlowController _controller;
//
//     [SerializeField] private Font _font;
//
//     private void Start()
//     {
//         if (!buildOnStart)
//             return;
//
//         Boot();
//     }
//
//     private void Update()
//     {
//         if (_controller == null)
//             return;
//
//         if (Input.GetKeyDown(KeyCode.Space))
//             _controller.RequestAdvanceToNextMainNode();
//
//         if (Input.GetKeyDown(KeyCode.C))
//             _controller.RequestToggleClearSelected();
//
//         if (Input.GetKeyDown(KeyCode.R))
//             _controller.RequestReset();
//
//         if (Input.GetKeyDown(KeyCode.B))
//             _controller.RequestRenderOnly();
//     }
//
//     private void Boot()
//     {
//         EnsureEventSystem();
//
//        // _font = LoadDefaultFont();
//
//         Log("BOOT", "Create sample graph data");
//
//         GraphData graphData = SampleGraphFactory.CreateChapter05Sample();
//
//         _repository = new GraphRepository(graphData, verboseLog);
//         _builder = new GraphViewModelBuilder(verboseLog);
//         _renderer = new GraphRenderer(
//             transform,
//             _font,
//             verboseLog,
//             contentBackgroundColor,
//             nodeNormalColor,
//             nodeSelectedColor,
//             nodeClearedColor,
//             nodeLockedColor,
//             edgeColor);
//
//         GraphLayoutOptions layoutOptions = new GraphLayoutOptions
//         {
//             nodeSize = nodeSize,
//             mainGapX = mainGapX,
//             branchOffsetY = branchOffsetY,
//             contentPadding = contentPadding
//         };
//
//         _controller = new GraphFlowController(
//             _repository,
//             _builder,
//             _renderer,
//             layoutOptions,
//             verboseLog);
//
//         Log("BOOT", "Initial commit + render");
//
//         _controller.RequestInitialRender();
//     }
//
//     private void EnsureEventSystem()
//     {
//         EventSystem existing = FindFirstObjectByType<EventSystem>();
//         if (existing != null)
//             return;
//
//         GameObject eventSystemObject = new GameObject("EventSystem");
//         eventSystemObject.AddComponent<EventSystem>();
//         eventSystemObject.AddComponent<StandaloneInputModule>();
//     }
//
//     private Font LoadDefaultFont()
//     {
//         Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
//
//         if (font == null)
//             font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
//
//         return font;
//     }
//
//     private void Log(string source, string message)
//     {
//         if (!verboseLog)
//             return;
//
//         Debug.Log($"[GraphFlowLab/{source}] {message}", this);
//     }
// }
//
// public sealed class GraphFlowController
// {
//     private readonly GraphRepository _repository;
//     private readonly GraphViewModelBuilder _builder;
//     private readonly GraphRenderer _renderer;
//     private readonly GraphLayoutOptions _layoutOptions;
//     private readonly bool _verboseLog;
//
//     public GraphFlowController(
//         GraphRepository repository,
//         GraphViewModelBuilder builder,
//         GraphRenderer renderer,
//         GraphLayoutOptions layoutOptions,
//         bool verboseLog)
//     {
//         _repository = repository;
//         _builder = builder;
//         _renderer = renderer;
//         _layoutOptions = layoutOptions;
//         _verboseLog = verboseLog;
//     }
//
//     public void RequestInitialRender()
//     {
//         Log("EXEC", "RequestInitialRender");
//
//         GraphSnapshot snapshot = _repository.ReadSnapshot();
//
//         CommitAndRender(
//             "InitialRender",
//             snapshot.graphData,
//             snapshot.runtimeState);
//     }
//
//     public void RequestSelectNode(string nodeId)
//     {
//         Log("EXEC", $"RequestSelectNode nodeId='{nodeId}'");
//
//         GraphSnapshot before = _repository.ReadSnapshot();
//
//         if (!before.graphData.ContainsNode(nodeId))
//         {
//             Log("EXEC", $"Select rejected. Missing nodeId='{nodeId}'");
//             return;
//         }
//
//         GraphRuntimeState nextState = before.runtimeState.Clone();
//         nextState.selectedNodeId = nodeId;
//
//         _repository.CommitRuntimeState(
//             "SelectNode",
//             nextState);
//
//         GraphSnapshot after = _repository.ReadSnapshot();
//
//         CommitAndRender(
//             "SelectNode",
//             after.graphData,
//             after.runtimeState);
//     }
//
//     public void RequestAdvanceToNextMainNode()
//     {
//         Log("EXEC", "RequestAdvanceToNextMainNode");
//
//         GraphSnapshot before = _repository.ReadSnapshot();
//
//         string current = before.runtimeState.selectedNodeId;
//         string next = before.graphData.FindNextMainNode(current);
//
//         if (string.IsNullOrEmpty(next))
//         {
//             Log("EXEC", $"Advance rejected. current='{current}' has no next main node.");
//             return;
//         }
//
//         GraphRuntimeState nextState = before.runtimeState.Clone();
//         nextState.selectedNodeId = next;
//
//         _repository.CommitRuntimeState(
//             "AdvanceToNextMainNode",
//             nextState);
//
//         GraphSnapshot after = _repository.ReadSnapshot();
//
//         CommitAndRender(
//             "AdvanceToNextMainNode",
//             after.graphData,
//             after.runtimeState);
//     }
//
//     public void RequestToggleClearSelected()
//     {
//         Log("EXEC", "RequestToggleClearSelected");
//
//         GraphSnapshot before = _repository.ReadSnapshot();
//
//         string selected = before.runtimeState.selectedNodeId;
//
//         if (string.IsNullOrEmpty(selected))
//         {
//             Log("EXEC", "Toggle clear rejected. No selected node.");
//             return;
//         }
//
//         GraphRuntimeState nextState = before.runtimeState.Clone();
//
//         if (nextState.clearedNodeIds.Contains(selected))
//             nextState.clearedNodeIds.Remove(selected);
//         else
//             nextState.clearedNodeIds.Add(selected);
//
//         _repository.CommitRuntimeState(
//             "ToggleClearSelected",
//             nextState);
//
//         GraphSnapshot after = _repository.ReadSnapshot();
//
//         CommitAndRender(
//             "ToggleClearSelected",
//             after.graphData,
//             after.runtimeState);
//     }
//
//     public void RequestReset()
//     {
//         Log("EXEC", "RequestReset");
//
//         _repository.ResetRuntimeState("Reset");
//
//         GraphSnapshot snapshot = _repository.ReadSnapshot();
//
//         CommitAndRender(
//             "Reset",
//             snapshot.graphData,
//             snapshot.runtimeState);
//     }
//
//     public void RequestRenderOnly()
//     {
//         Log("EXEC", "RequestRenderOnly");
//
//         GraphSnapshot snapshot = _repository.ReadSnapshot();
//
//         CommitAndRender(
//             "RenderOnly",
//             snapshot.graphData,
//             snapshot.runtimeState);
//     }
//
//     private void CommitAndRender(
//         string reason,
//         GraphData graphData,
//         GraphRuntimeState runtimeState)
//     {
//         Log("EXEC", $"CommitAndRender reason='{reason}' revision={runtimeState.revision}");
//
//         Log("DATA", "GraphData + RuntimeState -> GraphViewData");
//
//         GraphViewData viewData = _builder.Build(
//             graphData,
//             runtimeState,
//             _layoutOptions);
//
//         Log("RENDER", "GraphViewData -> Unity UI");
//
//         _renderer.Render(
//             viewData,
//             RequestSelectNode);
//     }
//
//     private void Log(string source, string message)
//     {
//         if (!_verboseLog)
//             return;
//
//         Debug.Log($"[GraphFlow/{source}] {message}");
//     }
// }
//
// public sealed class GraphRepository
// {
//     private readonly GraphData _graphData;
//     private readonly bool _verboseLog;
//
//     private GraphRuntimeState _runtimeState;
//
//     public GraphRepository(GraphData graphData, bool verboseLog)
//     {
//         _graphData = graphData;
//         _verboseLog = verboseLog;
//
//         _runtimeState = GraphRuntimeState.CreateInitial(graphData.GetFirstMainNodeId());
//
//         Log("DATA", $"Repository created. firstSelected='{_runtimeState.selectedNodeId}'");
//     }
//
//     public GraphSnapshot ReadSnapshot()
//     {
//         Log("DATA", $"ReadSnapshot revision={_runtimeState.revision}, selected='{_runtimeState.selectedNodeId}'");
//
//         return new GraphSnapshot(
//             _graphData,
//             _runtimeState.Clone());
//     }
//
//     public void CommitRuntimeState(string reason, GraphRuntimeState nextState)
//     {
//         int previousRevision = _runtimeState.revision;
//
//         nextState.revision = previousRevision + 1;
//
//         Log("COMMIT", $"reason='{reason}', revision {previousRevision} -> {nextState.revision}, selected='{nextState.selectedNodeId}'");
//
//         _runtimeState = nextState;
//     }
//
//     public void ResetRuntimeState(string reason)
//     {
//         GraphRuntimeState nextState = GraphRuntimeState.CreateInitial(_graphData.GetFirstMainNodeId());
//
//         CommitRuntimeState(reason, nextState);
//     }
//
//     private void Log(string source, string message)
//     {
//         if (!_verboseLog)
//             return;
//
//         Debug.Log($"[GraphRepository/{source}] {message}");
//     }
// }
//
// public sealed class GraphViewModelBuilder
// {
//     private readonly bool _verboseLog;
//
//     public GraphViewModelBuilder(bool verboseLog)
//     {
//         _verboseLog = verboseLog;
//     }
//
//     public GraphViewData Build(
//         GraphData graphData,
//         GraphRuntimeState runtimeState,
//         GraphLayoutOptions options)
//     {
//         Log("BUILD", $"Begin Build revision={runtimeState.revision}");
//
//         Dictionary<string, Vector2> positions = CalculatePositions(graphData, options);
//
//         List<GraphNodeViewData> nodeViews = new List<GraphNodeViewData>();
//
//         for (int i = 0; i < graphData.nodes.Count; i++)
//         {
//             GraphNodeData node = graphData.nodes[i];
//
//             Vector2 position = positions[node.nodeId];
//
//             GraphNodeVisualState visualState = ResolveVisualState(
//                 node,
//                 runtimeState);
//
//             GraphNodeViewData nodeView = new GraphNodeViewData
//             {
//                 nodeId = node.nodeId,
//                 title = node.title,
//                 kind = node.kind,
//                 anchoredPosition = position,
//                 size = options.nodeSize,
//                 visualState = visualState
//             };
//
//             nodeViews.Add(nodeView);
//
//             Log("BUILD", $"NodeView nodeId='{node.nodeId}', pos={position}, state={visualState}");
//         }
//
//         List<GraphEdgeViewData> edgeViews = new List<GraphEdgeViewData>();
//
//         for (int i = 0; i < graphData.edges.Count; i++)
//         {
//             GraphEdgeData edge = graphData.edges[i];
//
//             if (!positions.ContainsKey(edge.fromNodeId))
//                 continue;
//
//             if (!positions.ContainsKey(edge.toNodeId))
//                 continue;
//
//             Vector2 from = positions[edge.fromNodeId];
//             Vector2 to = positions[edge.toNodeId];
//
//             GraphEdgeViewData edgeView = new GraphEdgeViewData
//             {
//                 fromNodeId = edge.fromNodeId,
//                 toNodeId = edge.toNodeId,
//                 start = from,
//                 end = to,
//                 thickness = edge.isAttachment ? 0f : 5f,
//                 visible = !edge.isAttachment
//             };
//
//             edgeViews.Add(edgeView);
//
//             Log("BUILD", $"EdgeView {edge.fromNodeId} -> {edge.toNodeId}, visible={edgeView.visible}");
//         }
//
//         Rect bounds = CalculateBounds(nodeViews, options);
//
//         GraphViewData viewData = new GraphViewData
//         {
//             nodes = nodeViews,
//             edges = edgeViews,
//             contentSize = bounds.size,
//             contentOriginOffset = new Vector2(-bounds.xMin, -bounds.yMin)
//         };
//
//         Log("BUILD", $"End Build contentSize={viewData.contentSize}, originOffset={viewData.contentOriginOffset}");
//
//         return viewData;
//     }
//
//     private Dictionary<string, Vector2> CalculatePositions(
//         GraphData graphData,
//         GraphLayoutOptions options)
//     {
//         Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
//
//         int mainIndex = 0;
//
//         for (int i = 0; i < graphData.nodes.Count; i++)
//         {
//             GraphNodeData node = graphData.nodes[i];
//
//             if (node.kind != GraphNodeKind.Main)
//                 continue;
//
//             positions[node.nodeId] = new Vector2(
//                 mainIndex * options.mainGapX,
//                 0f);
//
//             mainIndex++;
//         }
//
//         for (int i = 0; i < graphData.nodes.Count; i++)
//         {
//             GraphNodeData node = graphData.nodes[i];
//
//             if (node.kind == GraphNodeKind.Main)
//                 continue;
//
//             GraphNodeData parent = graphData.FindNode(node.layoutParentNodeId);
//
//             Vector2 parentPosition = Vector2.zero;
//
//             if (parent != null && positions.ContainsKey(parent.nodeId))
//                 parentPosition = positions[parent.nodeId];
//
//             float y = 0f;
//
//             if (node.kind == GraphNodeKind.BranchUpper)
//                 y = options.branchOffsetY;
//
//             if (node.kind == GraphNodeKind.BranchLower)
//                 y = -options.branchOffsetY;
//
//             if (node.kind == GraphNodeKind.Extra)
//                 y = options.branchOffsetY * 1.5f;
//
//             positions[node.nodeId] = new Vector2(
//                 parentPosition.x,
//                 y);
//         }
//
//         return positions;
//     }
//
//     private GraphNodeVisualState ResolveVisualState(
//         GraphNodeData node,
//         GraphRuntimeState runtimeState)
//     {
//         if (node.locked)
//             return GraphNodeVisualState.Locked;
//
//         if (runtimeState.selectedNodeId == node.nodeId)
//             return GraphNodeVisualState.Selected;
//
//         if (runtimeState.clearedNodeIds.Contains(node.nodeId))
//             return GraphNodeVisualState.Cleared;
//
//         return GraphNodeVisualState.Normal;
//     }
//
//     private Rect CalculateBounds(
//         List<GraphNodeViewData> nodes,
//         GraphLayoutOptions options)
//     {
//         if (nodes.Count == 0)
//             return new Rect(0f, 0f, 100f, 100f);
//
//         float minX = float.MaxValue;
//         float maxX = float.MinValue;
//         float minY = float.MaxValue;
//         float maxY = float.MinValue;
//
//         for (int i = 0; i < nodes.Count; i++)
//         {
//             GraphNodeViewData node = nodes[i];
//
//             float halfW = node.size.x * 0.5f;
//             float halfH = node.size.y * 0.5f;
//
//             minX = Mathf.Min(minX, node.anchoredPosition.x - halfW);
//             maxX = Mathf.Max(maxX, node.anchoredPosition.x + halfW);
//             minY = Mathf.Min(minY, node.anchoredPosition.y - halfH);
//             maxY = Mathf.Max(maxY, node.anchoredPosition.y + halfH);
//         }
//
//         minX -= options.contentPadding.x;
//         maxX += options.contentPadding.x;
//         minY -= options.contentPadding.y;
//         maxY += options.contentPadding.y;
//
//         return Rect.MinMaxRect(
//             minX,
//             minY,
//             maxX,
//             maxY);
//     }
//
//     private void Log(string source, string message)
//     {
//         if (!_verboseLog)
//             return;
//
//         Debug.Log($"[GraphViewModelBuilder/{source}] {message}");
//     }
// }
//
// public sealed class GraphRenderer
// {
//     private readonly Transform _owner;
//     private readonly Font _font;
//     private readonly bool _verboseLog;
//
//     private readonly Color _contentBackgroundColor;
//     private readonly Color _nodeNormalColor;
//     private readonly Color _nodeSelectedColor;
//     private readonly Color _nodeClearedColor;
//     private readonly Color _nodeLockedColor;
//     private readonly Color _edgeColor;
//
//     private Canvas _canvas;
//     private RectTransform _contentRoot;
//     private RectTransform _edgeRoot;
//     private RectTransform _nodeRoot;
//
//     private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
//
//     public GraphRenderer(
//         Transform owner,
//         Font font,
//         bool verboseLog,
//         Color contentBackgroundColor,
//         Color nodeNormalColor,
//         Color nodeSelectedColor,
//         Color nodeClearedColor,
//         Color nodeLockedColor,
//         Color edgeColor)
//     {
//         _owner = owner;
//         _font = font;
//         _verboseLog = verboseLog;
//
//         _contentBackgroundColor = contentBackgroundColor;
//         _nodeNormalColor = nodeNormalColor;
//         _nodeSelectedColor = nodeSelectedColor;
//         _nodeClearedColor = nodeClearedColor;
//         _nodeLockedColor = nodeLockedColor;
//         _edgeColor = edgeColor;
//
//         EnsureCanvas();
//     }
//
//     public void Render(
//         GraphViewData viewData,
//         Action<string> onNodeClicked)
//     {
//         Log("RENDER", $"Begin Render nodes={viewData.nodes.Count}, edges={viewData.edges.Count}");
//
//         ClearSpawnedObjects();
//
//         _contentRoot.sizeDelta = viewData.contentSize;
//
//         for (int i = 0; i < viewData.edges.Count; i++)
//         {
//             GraphEdgeViewData edge = viewData.edges[i];
//
//             if (!edge.visible)
//             {
//                 Log("RENDER", $"Skip hidden edge {edge.fromNodeId} -> {edge.toNodeId}");
//                 continue;
//             }
//
//             CreateEdge(edge, viewData.contentOriginOffset);
//         }
//
//         for (int i = 0; i < viewData.nodes.Count; i++)
//         {
//             CreateNode(
//                 viewData.nodes[i],
//                 viewData.contentOriginOffset,
//                 onNodeClicked);
//         }
//
//         Log("RENDER", "End Render");
//     }
//
//     private void EnsureCanvas()
//     {
//         GameObject canvasObject = new GameObject("GraphFlowLab_Canvas");
//         canvasObject.transform.SetParent(_owner, false);
//
//         _canvas = canvasObject.AddComponent<Canvas>();
//         _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//
//         CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
//         scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//         scaler.referenceResolution = new Vector2(1920f, 1080f);
//         scaler.matchWidthOrHeight = 0.5f;
//
//         canvasObject.AddComponent<GraphicRaycaster>();
//
//         RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
//         StretchFull(canvasRect);
//
//         GameObject scrollObject = CreateUIObject("GraphScrollView", canvasRect);
//         RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
//         StretchFull(scrollRectTransform);
//         scrollRectTransform.offsetMin = new Vector2(80f, 80f);
//         scrollRectTransform.offsetMax = new Vector2(-80f, -80f);
//
//         Image scrollBackground = scrollObject.AddComponent<Image>();
//         scrollBackground.color = _contentBackgroundColor;
//
//         ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
//         scrollRect.horizontal = true;
//         scrollRect.vertical = true;
//         scrollRect.movementType = ScrollRect.MovementType.Clamped;
//         scrollRect.scrollSensitivity = 30f;
//
//         GameObject viewportObject = CreateUIObject("Viewport", scrollRectTransform);
//         RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
//         StretchFull(viewportRect);
//
//         Image viewportImage = viewportObject.AddComponent<Image>();
//         viewportImage.color = new Color(1f, 1f, 1f, 0.03f);
//
//         Mask mask = viewportObject.AddComponent<Mask>();
//         mask.showMaskGraphic = false;
//
//         GameObject contentObject = CreateUIObject("Content", viewportRect);
//         _contentRoot = contentObject.GetComponent<RectTransform>();
//         _contentRoot.anchorMin = new Vector2(0f, 0.5f);
//         _contentRoot.anchorMax = new Vector2(0f, 0.5f);
//         _contentRoot.pivot = new Vector2(0f, 0.5f);
//         _contentRoot.anchoredPosition = Vector2.zero;
//         _contentRoot.sizeDelta = new Vector2(1400f, 700f);
//
//         scrollRect.viewport = viewportRect;
//         scrollRect.content = _contentRoot;
//
//         GameObject edgeRootObject = CreateUIObject("Edges", _contentRoot);
//         _edgeRoot = edgeRootObject.GetComponent<RectTransform>();
//         StretchFull(_edgeRoot);
//
//         GameObject nodeRootObject = CreateUIObject("Nodes", _contentRoot);
//         _nodeRoot = nodeRootObject.GetComponent<RectTransform>();
//         StretchFull(_nodeRoot);
//
//         Log("BOOT", "Canvas / ScrollRect / Content created");
//     }
//
//     private void CreateNode(
//         GraphNodeViewData nodeView,
//         Vector2 originOffset,
//         Action<string> onNodeClicked)
//     {
//         GameObject nodeObject = CreateUIObject($"Node_{nodeView.nodeId}", _nodeRoot);
//         RectTransform rect = nodeObject.GetComponent<RectTransform>();
//
//         rect.anchorMin = new Vector2(0f, 0.5f);
//         rect.anchorMax = new Vector2(0f, 0.5f);
//         rect.pivot = new Vector2(0.5f, 0.5f);
//         rect.sizeDelta = nodeView.size;
//         rect.anchoredPosition = nodeView.anchoredPosition + originOffset;
//
//         Image image = nodeObject.AddComponent<Image>();
//         image.color = ResolveNodeColor(nodeView.visualState);
//
//         Button button = nodeObject.AddComponent<Button>();
//         button.onClick.AddListener(() =>
//         {
//             onNodeClicked?.Invoke(nodeView.nodeId);
//         });
//
//         GameObject titleObject = CreateUIObject("Title", rect);
//         RectTransform titleRect = titleObject.GetComponent<RectTransform>();
//         StretchFull(titleRect);
//         titleRect.offsetMin = new Vector2(10f, 8f);
//         titleRect.offsetMax = new Vector2(-10f, -8f);
//
//         Text text = titleObject.AddComponent<Text>();
//         text.font = _font;
//         text.fontSize = 20;
//         text.alignment = TextAnchor.MiddleCenter;
//         text.color = Color.white;
//         text.text = $"{nodeView.title}\n{nodeView.nodeId}\n{nodeView.visualState}";
//
//         _spawnedObjects.Add(nodeObject);
//
//         Log("RENDER", $"CreateNode nodeId='{nodeView.nodeId}', pos={rect.anchoredPosition}, state={nodeView.visualState}");
//     }
//
//     private void CreateEdge(
//         GraphEdgeViewData edgeView,
//         Vector2 originOffset)
//     {
//         Vector2 start = edgeView.start + originOffset;
//         Vector2 end = edgeView.end + originOffset;
//
//         GameObject edgeObject = CreateUIObject($"Edge_{edgeView.fromNodeId}_to_{edgeView.toNodeId}", _edgeRoot);
//         RectTransform rect = edgeObject.GetComponent<RectTransform>();
//
//         rect.anchorMin = new Vector2(0f, 0.5f);
//         rect.anchorMax = new Vector2(0f, 0.5f);
//         rect.pivot = new Vector2(0f, 0.5f);
//
//         Vector2 delta = end - start;
//         float length = delta.magnitude;
//         float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
//
//         rect.anchoredPosition = start;
//         rect.sizeDelta = new Vector2(length, edgeView.thickness);
//         rect.localRotation = Quaternion.Euler(0f, 0f, angle);
//
//         Image image = edgeObject.AddComponent<Image>();
//         image.color = _edgeColor;
//
//         _spawnedObjects.Add(edgeObject);
//
//         Log("RENDER", $"CreateEdge {edgeView.fromNodeId} -> {edgeView.toNodeId}, start={start}, end={end}");
//     }
//
//     private Color ResolveNodeColor(GraphNodeVisualState state)
//     {
//         if (state == GraphNodeVisualState.Selected)
//             return _nodeSelectedColor;
//
//         if (state == GraphNodeVisualState.Cleared)
//             return _nodeClearedColor;
//
//         if (state == GraphNodeVisualState.Locked)
//             return _nodeLockedColor;
//
//         return _nodeNormalColor;
//     }
//
//     private void ClearSpawnedObjects()
//     {
//         for (int i = 0; i < _spawnedObjects.Count; i++)
//         {
//             GameObject obj = _spawnedObjects[i];
//
//             if (obj != null)
//                 UnityEngine.Object.Destroy(obj);
//         }
//
//         _spawnedObjects.Clear();
//
//         Log("RENDER", "Clear previous spawned nodes/edges");
//     }
//
//     private GameObject CreateUIObject(string name, Transform parent)
//     {
//         GameObject obj = new GameObject(name);
//         obj.transform.SetParent(parent, false);
//         obj.AddComponent<RectTransform>();
//         return obj;
//     }
//
//     private void StretchFull(RectTransform rect)
//     {
//         rect.anchorMin = Vector2.zero;
//         rect.anchorMax = Vector2.one;
//         rect.pivot = new Vector2(0.5f, 0.5f);
//         rect.offsetMin = Vector2.zero;
//         rect.offsetMax = Vector2.zero;
//     }
//
//     private void Log(string source, string message)
//     {
//         if (!_verboseLog)
//             return;
//
//         Debug.Log($"[GraphRenderer/{source}] {message}");
//     }
// }
//
// public static class SampleGraphFactory
// {
//     public static GraphData CreateChapter05Sample()
//     {
//         GraphData graph = new GraphData();
//
//         graph.nodes.Add(new GraphNodeData
//         {
//             nodeId = "main05.01",
//             title = "Opening",
//             kind = GraphNodeKind.Main,
//             layoutParentNodeId = ""
//         });
//
//         graph.nodes.Add(new GraphNodeData
//         {
//             nodeId = "main05.02",
//             title = "First Choice",
//             kind = GraphNodeKind.Main,
//             layoutParentNodeId = ""
//         });
//
//         graph.nodes.Add(new GraphNodeData
//         {
//             nodeId = "main05.03",
//             title = "Converge",
//             kind = GraphNodeKind.Main,
//             layoutParentNodeId = ""
//         });
//
//         graph.nodes.Add(new GraphNodeData
//         {
//             nodeId = "main05.04",
//             title = "Ending Gate",
//             kind = GraphNodeKind.Main,
//             layoutParentNodeId = "",
//             locked = true
//         });
//
//         graph.nodes.Add(new GraphNodeData
//         {
//             nodeId = "branch05.02U",
//             title = "Upper Branch",
//             kind = GraphNodeKind.BranchUpper,
//             layoutParentNodeId = "main05.02"
//         });
//
//         graph.nodes.Add(new GraphNodeData
//         {
//             nodeId = "sub05.02A",
//             title = "Lower Sub",
//             kind = GraphNodeKind.BranchLower,
//             layoutParentNodeId = "main05.02"
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "main05.01",
//             toNodeId = "main05.02"
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "main05.02",
//             toNodeId = "main05.03"
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "main05.03",
//             toNodeId = "main05.04"
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "main05.02",
//             toNodeId = "branch05.02U",
//             isAttachment = true
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "main05.02",
//             toNodeId = "sub05.02A",
//             isAttachment = true
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "branch05.02U",
//             toNodeId = "main05.03",
//             isAttachment = true
//         });
//
//         graph.edges.Add(new GraphEdgeData
//         {
//             fromNodeId = "sub05.02A",
//             toNodeId = "main05.03",
//             isAttachment = true
//         });
//
//         return graph;
//     }
// }
//
// [Serializable]
// public sealed class GraphData
// {
//     public List<GraphNodeData> nodes = new List<GraphNodeData>();
//     public List<GraphEdgeData> edges = new List<GraphEdgeData>();
//
//     public bool ContainsNode(string nodeId)
//     {
//         return FindNode(nodeId) != null;
//     }
//
//     public GraphNodeData FindNode(string nodeId)
//     {
//         if (string.IsNullOrEmpty(nodeId))
//             return null;
//
//         for (int i = 0; i < nodes.Count; i++)
//         {
//             if (nodes[i].nodeId == nodeId)
//                 return nodes[i];
//         }
//
//         return null;
//     }
//
//     public string GetFirstMainNodeId()
//     {
//         for (int i = 0; i < nodes.Count; i++)
//         {
//             if (nodes[i].kind == GraphNodeKind.Main)
//                 return nodes[i].nodeId;
//         }
//
//         if (nodes.Count > 0)
//             return nodes[0].nodeId;
//
//         return "";
//     }
//
//     public string FindNextMainNode(string currentNodeId)
//     {
//         for (int i = 0; i < edges.Count; i++)
//         {
//             GraphEdgeData edge = edges[i];
//
//             if (edge.fromNodeId != currentNodeId)
//                 continue;
//
//             GraphNodeData to = FindNode(edge.toNodeId);
//
//             if (to == null)
//                 continue;
//
//             if (to.kind == GraphNodeKind.Main)
//                 return to.nodeId;
//         }
//
//         return "";
//     }
// }
//
// [Serializable]
// public sealed class GraphNodeData
// {
//     public string nodeId;
//     public string title;
//     public GraphNodeKind kind;
//     public string layoutParentNodeId;
//     public bool locked;
// }
//
// [Serializable]
// public sealed class GraphEdgeData
// {
//     public string fromNodeId;
//     public string toNodeId;
//
//     public bool isAttachment;
// }
//
// public sealed class GraphRuntimeState
// {
//     public int revision;
//     public string selectedNodeId;
//     public HashSet<string> clearedNodeIds = new HashSet<string>();
//
//     public static GraphRuntimeState CreateInitial(string firstSelectedNodeId)
//     {
//         return new GraphRuntimeState
//         {
//             revision = 0,
//             selectedNodeId = firstSelectedNodeId,
//             clearedNodeIds = new HashSet<string>()
//         };
//     }
//
//     public GraphRuntimeState Clone()
//     {
//         GraphRuntimeState clone = new GraphRuntimeState();
//         clone.revision = revision;
//         clone.selectedNodeId = selectedNodeId;
//         clone.clearedNodeIds = new HashSet<string>(clearedNodeIds);
//         return clone;
//     }
// }
//
// public readonly struct GraphSnapshot
// {
//     public readonly GraphData graphData;
//     public readonly GraphRuntimeState runtimeState;
//
//     public GraphSnapshot(
//         GraphData graphData,
//         GraphRuntimeState runtimeState)
//     {
//         this.graphData = graphData;
//         this.runtimeState = runtimeState;
//     }
// }
//
// public sealed class GraphViewData
// {
//     public List<GraphNodeViewData> nodes = new List<GraphNodeViewData>();
//     public List<GraphEdgeViewData> edges = new List<GraphEdgeViewData>();
//     public Vector2 contentSize;
//     public Vector2 contentOriginOffset;
// }
//
// public sealed class GraphNodeViewData
// {
//     public string nodeId;
//     public string title;
//     public GraphNodeKind kind;
//     public Vector2 anchoredPosition;
//     public Vector2 size;
//     public GraphNodeVisualState visualState;
// }
//
// public sealed class GraphEdgeViewData
// {
//     public string fromNodeId;
//     public string toNodeId;
//     public Vector2 start;
//     public Vector2 end;
//     public float thickness;
//     public bool visible;
// }
//
// [Serializable]
// public sealed class GraphLayoutOptions
// {
//     public Vector2 nodeSize = new Vector2(180f, 90f);
//     public float mainGapX = 260f;
//     public float branchOffsetY = 170f;
//     public Vector2 contentPadding = new Vector2(180f, 220f);
// }
//
// public enum GraphNodeKind
// {
//     Main,
//     BranchUpper,
//     BranchLower,
//     Extra
// }
//
// public enum GraphNodeVisualState
// {
//     Normal,
//     Selected,
//     Cleared,
//     Locked
// }