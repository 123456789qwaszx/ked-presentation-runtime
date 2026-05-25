using System;
using System.Collections.Generic;

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