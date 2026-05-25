using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "VNStoryGraph",
    menuName = "VN/Story Graph/VN Story Graph")]
public sealed class VNStoryGraphSO : ScriptableObject
{
    [Header("Graph Identity")]
    public string graphId;
    public string chapterKey;

    [Header("Editor")]
    public Vector2 canvasSize = new Vector2(2600f, 1200f);
    public float gridSize = 40f;

    [Header("Nodes")]
    public List<VNStoryGraphNode> nodes = new();

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

    public List<VNStoryGraphNode> GetNextNodes(VNStoryGraphNode from)
    {
        List<VNStoryGraphNode> result = new List<VNStoryGraphNode>();

        if (from == null || from.nextNodeIds == null)
            return result;

        for (int i = 0; i < from.nextNodeIds.Count; i++)
        {
            VNStoryGraphNode next = FindNode(from.nextNodeIds[i]);
            if (next != null)
                result.Add(next);
        }

        return result;
    }

    public VNStoryGraphNode GetAttachment(VNStoryGraphNode owner, VNStoryAttachmentSlot slot)
    {
        if (owner == null || owner.attachments == null)
            return null;

        string nodeId = owner.attachments.Get(slot);
        return FindNode(nodeId);
    }

    public bool IsTerminal(string nodeId)
    {
        VNStoryGraphNode node = FindNode(nodeId);
        return node != null && node.IsTerminal;
    }

    public VNStoryGraphNode CreateNode(string nodeId, VNStoryNodeKind kind, Vector2 position)
    {
        VNStoryGraphNode node = new VNStoryGraphNode
        {
            nodeId = nodeId,
            nodeKind = kind,
            position = position
        };

        nodes.Add(node);
        return node;
    }

    public bool RemoveNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
            return false;

        bool removed = false;

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] != null && nodes[i].nodeId == nodeId)
            {
                nodes.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
            RemoveReferencesTo(nodeId);

        return removed;
    }

    public void RemoveReferencesTo(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            VNStoryGraphNode node = nodes[i];
            if (node == null)
                continue;

            for (int n = node.nextNodeIds.Count - 1; n >= 0; n--)
            {
                if (node.nextNodeIds[n] == nodeId)
                    node.nextNodeIds.RemoveAt(n);
            }

            if (node.attachments.upNodeId == nodeId)
                node.attachments.upNodeId = "";

            if (node.attachments.rightNodeId == nodeId)
                node.attachments.rightNodeId = "";

            if (node.attachments.downNodeId == nodeId)
                node.attachments.downNodeId = "";
        }
    }
}