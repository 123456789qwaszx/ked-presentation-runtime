using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VNStoryGraphNode
{
    [Header("Identity")]
    public string nodeId;
    public string payloadKey;


    [Header("Kind")]
    public VNStoryNodeKind nodeKind = VNStoryNodeKind.Main;

    [Header("Layout")]
    public Vector2 position;

    [Header("Main Route")]
    public List<string> nextNodeIds = new(3);

    [Header("Attachment Slots")]
    public VNStoryAttachmentRefs attachments = new();

    [Header("Ending / Terminal")]
    public VNStoryTerminalResult ending = new();

    public bool IsMainNode
    {
        get { return nodeKind == VNStoryNodeKind.Main; }
    }

    public bool IsAttachmentNode
    {
        get { return nodeKind == VNStoryNodeKind.Attachment; }
    }

    public bool IsTerminal
    {
        get
        {
            if (IsAttachmentNode)
                return true;

            return nextNodeIds == null || nextNodeIds.Count == 0;
        }
    }

    public bool HasAnyAttachment()
    {
        return attachments.HasAny();
    }
}