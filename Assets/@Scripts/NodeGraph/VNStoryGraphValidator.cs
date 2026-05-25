using System.Collections.Generic;

public static class VNStoryGraphValidator
{
    private const int MaxNextNodeCount = 3;

    public static VNStoryGraphValidationResult Validate(VNStoryGraphSO graph)
    {
        VNStoryGraphValidationResult result = new VNStoryGraphValidationResult();

        if (graph == null)
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                "",
                "Graph is null.");

            return result;
        }

        graph.Ensure();

        Dictionary<string, VNStoryGraphNode> lookup = BuildLookup(graph, result);

        ValidateNodes(graph, lookup, result);
        ValidateAttachmentReferences(graph, lookup, result);

        return result;
    }

    private static Dictionary<string, VNStoryGraphNode> BuildLookup(
        VNStoryGraphSO graph,
        VNStoryGraphValidationResult result)
    {
        Dictionary<string, VNStoryGraphNode> lookup =
            new Dictionary<string, VNStoryGraphNode>();

        if (graph.nodes == null)
            return lookup;

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            VNStoryGraphNode node = graph.nodes[i];

            if (node == null)
            {
                result.Add(
                    VNStoryValidationSeverity.Error,
                    "",
                    "Node at index " + i + " is null.");

                continue;
            }

            node.EnsureLists();

            if (string.IsNullOrWhiteSpace(node.nodeId))
            {
                result.Add(
                    VNStoryValidationSeverity.Error,
                    "",
                    "Node at index " + i + " has empty nodeId.");

                continue;
            }

            if (lookup.ContainsKey(node.nodeId))
            {
                result.Add(
                    VNStoryValidationSeverity.Error,
                    node.nodeId,
                    "Duplicate nodeId.");

                continue;
            }

            lookup.Add(node.nodeId, node);
        }

        return lookup;
    }

    private static void ValidateNodes(
        VNStoryGraphSO graph,
        Dictionary<string, VNStoryGraphNode> lookup,
        VNStoryGraphValidationResult result)
    {
        if (graph.nodes == null)
            return;

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            VNStoryGraphNode node = graph.nodes[i];
            if (node == null)
                continue;

            ValidateCommonNode(node, result);
            ValidateNextNodes(node, lookup, result);
            ValidateAttachmentSlots(node, lookup, result);
            ValidateEnding(node, result);
        }
    }

    private static void ValidateCommonNode(
        VNStoryGraphNode node,
        VNStoryGraphValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(node.payloadKey))
        {
            result.Add(
                VNStoryValidationSeverity.Warning,
                node.nodeId,
                "payloadKey is empty.");
        }

        if (node.IsMainNode && node.attachmentKind != VNStoryAttachmentKind.None)
        {
            result.Add(
                VNStoryValidationSeverity.Warning,
                node.nodeId,
                "Main node has attachmentKind. attachmentKind is intended for Attachment nodes.");
        }

        if (node.IsAttachmentNode && node.attachmentKind == VNStoryAttachmentKind.None)
        {
            result.Add(
                VNStoryValidationSeverity.Warning,
                node.nodeId,
                "Attachment node has attachmentKind None.");
        }
    }

    private static void ValidateNextNodes(
        VNStoryGraphNode node,
        Dictionary<string, VNStoryGraphNode> lookup,
        VNStoryGraphValidationResult result)
    {
        if (node.nextNodeIds == null)
            return;

        if (node.IsAttachmentNode && node.nextNodeIds.Count > 0)
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                node.nodeId,
                "Attachment node cannot have nextNodeIds.");
        }

        if (node.IsMainNode && node.nextNodeIds.Count > MaxNextNodeCount)
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                node.nodeId,
                "Main node can have at most " + MaxNextNodeCount + " next nodes.");
        }

        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < node.nextNodeIds.Count; i++)
        {
            string nextId = node.nextNodeIds[i];

            if (string.IsNullOrWhiteSpace(nextId))
            {
                result.Add(
                    VNStoryValidationSeverity.Warning,
                    node.nodeId,
                    "nextNodeIds contains empty id at index " + i + ".");

                continue;
            }

            if (!seen.Add(nextId))
            {
                result.Add(
                    VNStoryValidationSeverity.Error,
                    node.nodeId,
                    "Duplicate next node id: " + nextId);
            }

            if (!lookup.ContainsKey(nextId))
            {
                result.Add(
                    VNStoryValidationSeverity.Error,
                    node.nodeId,
                    "nextNodeId references missing node: " + nextId);
            }
        }
    }

    private static void ValidateAttachmentSlots(
        VNStoryGraphNode node,
        Dictionary<string, VNStoryGraphNode> lookup,
        VNStoryGraphValidationResult result)
    {
        if (node.attachments == null)
            return;

        if (node.IsAttachmentNode && node.attachments.HasAny())
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                node.nodeId,
                "Attachment node cannot own attachment slots.");
        }

        if (!node.IsMainNode)
            return;

        ValidateSingleAttachmentSlot(
            node,
            VNStoryAttachmentSlot.Up,
            lookup,
            result);

        ValidateSingleAttachmentSlot(
            node,
            VNStoryAttachmentSlot.Right,
            lookup,
            result);

        ValidateSingleAttachmentSlot(
            node,
            VNStoryAttachmentSlot.Down,
            lookup,
            result);
    }

    private static void ValidateSingleAttachmentSlot(
        VNStoryGraphNode owner,
        VNStoryAttachmentSlot slot,
        Dictionary<string, VNStoryGraphNode> lookup,
        VNStoryGraphValidationResult result)
    {
        string attachmentId = owner.attachments.Get(slot);

        if (string.IsNullOrWhiteSpace(attachmentId))
            return;

        VNStoryGraphNode attachmentNode;
        if (!lookup.TryGetValue(attachmentId, out attachmentNode))
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                owner.nodeId,
                "Attachment slot " + slot + " references missing node: " + attachmentId);

            return;
        }

        if (!attachmentNode.IsAttachmentNode)
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                owner.nodeId,
                "Attachment slot " + slot + " must reference Attachment node. Referenced node: " + attachmentId);
        }
    }

    private static void ValidateAttachmentReferences(
        VNStoryGraphSO graph,
        Dictionary<string, VNStoryGraphNode> lookup,
        VNStoryGraphValidationResult result)
    {
        Dictionary<string, int> referenceCountByAttachment =
            new Dictionary<string, int>();

        foreach (KeyValuePair<string, VNStoryGraphNode> kv in lookup)
        {
            VNStoryGraphNode node = kv.Value;
            if (!node.IsMainNode || node.attachments == null)
                continue;

            CountAttachmentRef(node.attachments.upNodeId, referenceCountByAttachment);
            CountAttachmentRef(node.attachments.rightNodeId, referenceCountByAttachment);
            CountAttachmentRef(node.attachments.downNodeId, referenceCountByAttachment);
        }

        foreach (KeyValuePair<string, VNStoryGraphNode> kv in lookup)
        {
            VNStoryGraphNode node = kv.Value;
            if (!node.IsAttachmentNode)
                continue;

            int count;
            referenceCountByAttachment.TryGetValue(node.nodeId, out count);

            if (count == 0)
            {
                result.Add(
                    VNStoryValidationSeverity.Warning,
                    node.nodeId,
                    "Attachment node is not attached to any Main node.");
            }
            else if (count > 1)
            {
                result.Add(
                    VNStoryValidationSeverity.Warning,
                    node.nodeId,
                    "Attachment node is attached by multiple Main nodes. Count=" + count);
            }
        }
    }

    private static void CountAttachmentRef(
        string nodeId,
        Dictionary<string, int> counts)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return;

        int count;
        counts.TryGetValue(nodeId, out count);
        counts[nodeId] = count + 1;
    }

    private static void ValidateEnding(
        VNStoryGraphNode node,
        VNStoryGraphValidationResult result)
    {
        if (node.ending == null)
            return;

        if (node.ending.endingKind != VNStoryEndingKind.None &&
            string.IsNullOrWhiteSpace(node.ending.endingKey))
        {
            result.Add(
                VNStoryValidationSeverity.Warning,
                node.nodeId,
                "endingKind is set but endingKey is empty.");
        }

        if (node.ending.opensNextChapter &&
            string.IsNullOrWhiteSpace(node.ending.nextChapterKey))
        {
            result.Add(
                VNStoryValidationSeverity.Error,
                node.nodeId,
                "opensNextChapter is true but nextChapterKey is empty.");
        }

        if (!node.IsTerminal && node.ending.endingKind != VNStoryEndingKind.None)
        {
            result.Add(
                VNStoryValidationSeverity.Warning,
                node.nodeId,
                "Non-terminal node has endingKind. Check if this is intended.");
        }
    }
}