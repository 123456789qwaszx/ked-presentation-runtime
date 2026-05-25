using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VNStoryGraphNode
{
    [Header("Identity")]
    public string nodeId;
    public string payloadKey;

    [Header("Display")]
    public string title;
    [TextArea(2, 4)]
    public string memo;

    [Header("Kind")]
    public VNStoryNodeKind nodeKind = VNStoryNodeKind.Main;
    public VNStoryAttachmentKind attachmentKind = VNStoryAttachmentKind.None;

    [Header("Editor Layout")]
    public Vector2 position;

    [Header("Main Route")]
    public List<string> nextNodeIds = new List<string>(3);

    [Header("Attachment Slots")]
    public VNStoryAttachmentRefs attachments = new VNStoryAttachmentRefs();

    [Header("Ending / Terminal")]
    public VNStoryEndingInfo ending = new VNStoryEndingInfo();

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

    public bool HasEnding
    {
        get
        {
            return ending != null && ending.endingKind != VNStoryEndingKind.None;
        }
    }

    public void EnsureLists()
    {
        if (nextNodeIds == null)
            nextNodeIds = new List<string>(3);

        if (attachments == null)
            attachments = new VNStoryAttachmentRefs();

        if (ending == null)
            ending = new VNStoryEndingInfo();
    }

    public bool HasAnyAttachment()
    {
        if (attachments == null)
            return false;

        return attachments.HasAny();
    }
}

[Serializable]
public sealed class VNStoryAttachmentRefs
{
    public string upNodeId;
    public string rightNodeId;
    public string downNodeId;

    public string Get(VNStoryAttachmentSlot slot)
    {
        switch (slot)
        {
            case VNStoryAttachmentSlot.Up:
                return upNodeId;

            case VNStoryAttachmentSlot.Right:
                return rightNodeId;

            case VNStoryAttachmentSlot.Down:
                return downNodeId;

            default:
                return null;
        }
    }

    public void Set(VNStoryAttachmentSlot slot, string nodeId)
    {
        switch (slot)
        {
            case VNStoryAttachmentSlot.Up:
                upNodeId = nodeId;
                break;

            case VNStoryAttachmentSlot.Right:
                rightNodeId = nodeId;
                break;

            case VNStoryAttachmentSlot.Down:
                downNodeId = nodeId;
                break;
        }
    }

    public bool HasAny()
    {
        return !string.IsNullOrWhiteSpace(upNodeId)
            || !string.IsNullOrWhiteSpace(rightNodeId)
            || !string.IsNullOrWhiteSpace(downNodeId);
    }

    public IEnumerable<string> EnumerateNodeIds()
    {
        if (!string.IsNullOrWhiteSpace(upNodeId))
            yield return upNodeId;

        if (!string.IsNullOrWhiteSpace(rightNodeId))
            yield return rightNodeId;

        if (!string.IsNullOrWhiteSpace(downNodeId))
            yield return downNodeId;
    }
}

[Serializable]
public sealed class VNStoryEndingInfo
{
    public VNStoryEndingKind endingKind = VNStoryEndingKind.None;
    public string endingKey;

    [Header("Chapter Unlock")]
    public bool opensNextChapter;
    public string nextChapterKey;

    [Header("Optional Flags")]
    public bool countsAsClear;
    public bool isReplayable = true;
}