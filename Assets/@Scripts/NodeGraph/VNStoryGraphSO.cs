using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "VNStoryGraph",
    menuName = "VN/Story Graph/VN Story Graph")]
public sealed class VNStoryGraphSO : ScriptableObject
{
    public string graphId;
    public string chapterKey;
    public string startNodeId;

    public List<VNStoryGraphNode> nodes = new List<VNStoryGraphNode>();

    public IReadOnlyList<VNStoryGraphNode> Nodes
    {
        get { return nodes; }
    }

    public VNStoryGraphNode FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
            return null;

        for (int i = 0; i < nodes.Count; i++)
        {
            VNStoryGraphNode node = nodes[i];
            if (node == null)
                continue;

            if (node.nodeId == nodeId)
                return node;
        }

        return null;
    }

    public bool TryFindNode(string nodeId, out VNStoryGraphNode node)
    {
        node = FindNode(nodeId);
        return node != null;
    }

    public VNStoryGraphNode GetStartNode()
    {
        return FindNode(startNodeId);
    }
}