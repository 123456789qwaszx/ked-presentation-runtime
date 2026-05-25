using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public sealed class VNStoryGraphNodeClickEvent : UnityEvent<string, string>
{
}

public sealed class VNStoryGraphRuntimeUIBuilder : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private VNStoryGraphSO graph;
    [SerializeField] private VNStoryGraphViewDataSO viewData;
    [SerializeField] private VNStoryGraphConditionSet conditionSet = new VNStoryGraphConditionSet();

    [Header("Roots")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform lineLayer;
    [SerializeField] private RectTransform nodeLayer;

    [Header("Prefab Optional")]
    [SerializeField] private VNStoryRuntimeNodeView nodePrefab;

    [Header("Events")]
    public VNStoryGraphNodeClickEvent onNodeClicked = new VNStoryGraphNodeClickEvent();

    private VNStoryGraphViewModel _viewModel;
    private readonly Dictionary<string, VNStoryRuntimeNodeView> _nodeViews =
        new Dictionary<string, VNStoryRuntimeNodeView>();

    public VNStoryGraphViewModel CurrentViewModel
    {
        get { return _viewModel; }
    }

    public void SetGraph(VNStoryGraphSO newGraph)
    {
        graph = newGraph;
    }

    public void SetViewData(VNStoryGraphViewDataSO newViewData)
    {
        viewData = newViewData;
    }

    public void SetConditionSet(VNStoryGraphConditionSet newConditionSet)
    {
        conditionSet = newConditionSet;
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        EnsureRoots();
        Clear();

        _viewModel = VNStoryGraphViewModelBuilder.Build(
            graph,
            viewData,
            conditionSet);

        BuildLinks(_viewModel);
        BuildNodes(_viewModel);
    }

    public void NotifyNodeClicked(VNStoryGraphNodeViewModel model)
    {
        if (model == null)
            return;

        if (!model.clickable)
            return;

        onNodeClicked.Invoke(model.nodeId, model.payloadKey);

        Debug.Log(
            "[VNStoryGraphRuntimeUIBuilder] Node clicked: nodeId=" +
            model.nodeId +
            ", payloadKey=" +
            model.payloadKey,
            this);
    }

    private void BuildNodes(VNStoryGraphViewModel model)
    {
        if (model == null || model.nodes == null)
            return;

        for (int i = 0; i < model.nodes.Count; i++)
        {
            VNStoryGraphNodeViewModel nodeVm = model.nodes[i];
            if (nodeVm == null || !nodeVm.visible)
                continue;

            VNStoryRuntimeNodeView view = CreateNodeView(nodeVm);
            if (view == null)
                continue;

            view.Bind(nodeVm, this);

            if (!_nodeViews.ContainsKey(nodeVm.nodeId))
                _nodeViews.Add(nodeVm.nodeId, view);
        }
    }

    private void BuildLinks(VNStoryGraphViewModel model)
    {
        if (model == null || model.links == null)
            return;

        for (int i = 0; i < model.links.Count; i++)
        {
            VNStoryGraphLinkViewModel linkVm = model.links[i];
            if (linkVm == null || !linkVm.visible)
                continue;

            VNStoryGraphNodeViewModel fromNode = model.FindNode(linkVm.fromNodeId);
            VNStoryGraphNodeViewModel toNode = model.FindNode(linkVm.toNodeId);

            if (fromNode == null || toNode == null)
                continue;

            if (!fromNode.visible || !toNode.visible)
                continue;

            CreateLine(
                "Line_" + linkVm.linkKey,
                fromNode.position,
                toNode.position,
                linkVm.thickness,
                linkVm.color);
        }
    }

    private VNStoryRuntimeNodeView CreateNodeView(VNStoryGraphNodeViewModel nodeVm)
    {
        if (nodePrefab != null)
        {
            VNStoryRuntimeNodeView instance =
                Instantiate(nodePrefab, nodeLayer);

            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            return instance;
        }

        return CreateDefaultNodeView(nodeVm);
    }

    private VNStoryRuntimeNodeView CreateDefaultNodeView(
        VNStoryGraphNodeViewModel nodeVm)
    {
        GameObject go = new GameObject(
            "Node_" + nodeVm.nodeId,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(VNStoryRuntimeNodeView));

        go.transform.SetParent(nodeLayer, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = nodeVm.position;
        rect.sizeDelta = nodeVm.size;

        Image image = go.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        CreateLabel(go.transform, nodeVm);

        return go.GetComponent<VNStoryRuntimeNodeView>();
    }

    private void CreateLabel(
        Transform parent,
        VNStoryGraphNodeViewModel nodeVm)
    {
        GameObject labelGo = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        labelGo.transform.SetParent(parent, false);

        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(8f, 6f);
        rect.offsetMax = new Vector2(-8f, -6f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = nodeVm.displayText;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.color = Color.white;
    }

    private void CreateLine(
        string name,
        Vector2 from,
        Vector2 to,
        float thickness,
        Color color)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        go.transform.SetParent(lineLayer, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        Vector2 mid = (from + to) * 0.5f;
        Vector2 delta = to - from;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        rect.anchoredPosition = mid;
        rect.sizeDelta = new Vector2(length, thickness);
        rect.localEulerAngles = new Vector3(0f, 0f, angle);

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void EnsureRoots()
    {
        if (contentRoot == null)
        {
            RectTransform selfRect = GetComponent<RectTransform>();
            if (selfRect != null)
                contentRoot = selfRect;
        }

        if (contentRoot == null)
            return;

        if (lineLayer == null)
            lineLayer = CreateLayer("Lines", contentRoot);

        if (nodeLayer == null)
            nodeLayer = CreateLayer("Nodes", contentRoot);

        lineLayer.SetAsFirstSibling();
        nodeLayer.SetAsLastSibling();
    }

    private RectTransform CreateLayer(string name, RectTransform parent)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform));

        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;

        return rect;
    }

    private void Clear()
    {
        _nodeViews.Clear();

        ClearChildren(lineLayer);
        ClearChildren(nodeLayer);
    }

    private void ClearChildren(RectTransform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}