using System.Collections.Generic;
using UnityEngine;

public sealed class VNStoryGraphViewModel
{
    public string graphId;
    public string chapterKey;
    public string startNodeId;

    public readonly List<VNStoryGraphNodeViewModel> nodes =
        new List<VNStoryGraphNodeViewModel>();

    public readonly List<VNStoryGraphLinkViewModel> links =
        new List<VNStoryGraphLinkViewModel>();

    public VNStoryGraphNodeViewModel FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return null;

        for (int i = 0; i < nodes.Count; i++)
        {
            VNStoryGraphNodeViewModel node = nodes[i];
            if (node == null)
                continue;

            if (node.nodeId == nodeId)
                return node;
        }

        return null;
    }
}

public sealed class VNStoryGraphNodeViewModel
{
    public string nodeId;
    public string payloadKey;
    public VNStoryNodeKind nodeKind;

    public string labelKey;
    public string displayText;
    public string actionKey;

    public string endingKey;
    public bool opensNextChapter;
    public string nextChapterKey;

    public Vector2 position;
    public Vector2 size;

    public Sprite sprite;
    public Color color;

    public bool visible;
    public bool unlocked;
    public bool clickable;

    public VNStoryGraphNodeViewState state;

    public bool IsMainNode
    {
        get { return nodeKind == VNStoryNodeKind.Main; }
    }

    public bool IsAttachmentNode
    {
        get { return nodeKind == VNStoryNodeKind.Attachment; }
    }
}

public sealed class VNStoryGraphLinkViewModel
{
    public string linkKey;

    public VNStoryGraphLinkKind linkKind;

    public string fromNodeId;
    public string toNodeId;

    public string labelKey;
    public string displayText;

    public string visibleConditionKey;
    public string unlockConditionKey;

    public bool visible;
    public bool unlocked;
    public bool clickable;

    public VNStoryAttachmentSlot attachmentSlot;
    public bool hasAttachmentSlot;

    public Color color;
    public float thickness;
}