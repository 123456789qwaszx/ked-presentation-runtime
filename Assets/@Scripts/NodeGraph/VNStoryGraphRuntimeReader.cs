using System.Collections.Generic;

public sealed class VNStoryGraphRuntimeReader
{
    private readonly VNStoryGraphSO _graph;

    public VNStoryGraphRuntimeReader(VNStoryGraphSO graph)
    {
        _graph = graph;
        if (_graph != null)
            _graph.Ensure();
    }

    public VNStoryGraphNode GetNode(string nodeId)
    {
        if (_graph == null)
            return null;

        return _graph.FindNode(nodeId);
    }

    public bool TryGetNode(string nodeId, out VNStoryGraphNode node)
    {
        node = null;

        if (_graph == null)
            return false;

        return _graph.TryFindNode(nodeId, out node);
    }

    public List<VNStoryGraphNode> GetNextNodes(string nodeId)
    {
        List<VNStoryGraphNode> result = new List<VNStoryGraphNode>();

        VNStoryGraphNode node = GetNode(nodeId);
        if (node == null || node.nextNodeIds == null)
            return result;

        for (int i = 0; i < node.nextNodeIds.Count; i++)
        {
            VNStoryGraphNode next = GetNode(node.nextNodeIds[i]);
            if (next != null)
                result.Add(next);
        }

        return result;
    }

    public bool IsTerminal(string nodeId)
    {
        VNStoryGraphNode node = GetNode(nodeId);
        return node != null && node.IsTerminal;
    }

    public VNStoryGraphNode GetAttachment(
        string ownerNodeId,
        VNStoryAttachmentSlot slot)
    {
        VNStoryGraphNode owner = GetNode(ownerNodeId);
        if (owner == null || owner.attachments == null)
            return null;

        string attachmentId = owner.attachments.Get(slot);
        return GetNode(attachmentId);
    }

    public bool CanOpenNextChapter(string nodeId, out string nextChapterKey)
    {
        nextChapterKey = null;

        VNStoryGraphNode node = GetNode(nodeId);
        if (node == null || node.ending == null)
            return false;

        if (!node.ending.opensNextChapter)
            return false;

        if (string.IsNullOrWhiteSpace(node.ending.nextChapterKey))
            return false;

        nextChapterKey = node.ending.nextChapterKey;
        return true;
    }
}