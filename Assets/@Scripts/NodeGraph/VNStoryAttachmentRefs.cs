using System;
using System.Collections.Generic;

[Serializable]
public sealed class VNStoryAttachmentRefs
{
    public VNStoryAttachmentLink up = new VNStoryAttachmentLink();
    public VNStoryAttachmentLink right = new VNStoryAttachmentLink();
    public VNStoryAttachmentLink down = new VNStoryAttachmentLink();

    public VNStoryAttachmentLink Get(VNStoryAttachmentSlot slot)
    {
        switch (slot)
        {
            case VNStoryAttachmentSlot.Up:
                return up;

            case VNStoryAttachmentSlot.Right:
                return right;

            case VNStoryAttachmentSlot.Down:
                return down;

            default:
                return null;
        }
    }

    public void Set(VNStoryAttachmentSlot slot, VNStoryAttachmentLink link)
    {
        if (link == null)
            link = new VNStoryAttachmentLink();

        switch (slot)
        {
            case VNStoryAttachmentSlot.Up:
                up = link;
                break;

            case VNStoryAttachmentSlot.Right:
                right = link;
                break;

            case VNStoryAttachmentSlot.Down:
                down = link;
                break;
        }
    }

    public string GetNodeId(VNStoryAttachmentSlot slot)
    {
        VNStoryAttachmentLink link = Get(slot);
        if (link == null)
            return null;

        return link.toNodeId;
    }

    public bool HasAny()
    {
        return HasLink(up) || HasLink(right) || HasLink(down);
    }

    public IEnumerable<VNStoryAttachmentLink> EnumerateLinks()
    {
        if (HasLink(up))
            yield return up;

        if (HasLink(right))
            yield return right;

        if (HasLink(down))
            yield return down;
    }

    public IEnumerable<string> EnumerateNodeIds()
    {
        if (HasLink(up))
            yield return up.toNodeId;

        if (HasLink(right))
            yield return right.toNodeId;

        if (HasLink(down))
            yield return down.toNodeId;
    }

    private static bool HasLink(VNStoryAttachmentLink link)
    {
        return link != null && link.HasTarget;
    }
}