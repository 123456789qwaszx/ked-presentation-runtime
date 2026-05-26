// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class VNStoryGraphNode
// {
//     public const int MaxNextLinkCount = 3;
//
//     public string nodeId;
//     public string payloadKey;
//
//     public VNStoryNodeKind nodeKind = VNStoryNodeKind.Main;
//
//     public List<VNStoryNextLink> nextLinks = new (MaxNextLinkCount);
//     public VNStoryAttachmentRefs attachments = new ();
//
//     public string endingKey;
//     
//     public bool opensNextChapter;
//     public string nextChapterKey;
//
//     public bool IsMainNode
//     {
//         get { return nodeKind == VNStoryNodeKind.Main; }
//     }
//
//     public bool IsAttachmentNode
//     {
//         get { return nodeKind == VNStoryNodeKind.Attachment; }
//     }
//
//     public bool HasEnding
//     {
//         get { return !string.IsNullOrWhiteSpace(endingKey); }
//     }
//
//     public bool HasNextChapterUnlock
//     {
//         get
//         {
//             return opensNextChapter &&
//                    !string.IsNullOrWhiteSpace(nextChapterKey);
//         }
//     }
//
//     public bool IsTerminal
//     {
//         get
//         {
//             if (IsAttachmentNode)
//                 return true;
//
//             return CountValidNextLinks() == 0;
//         }
//     }
//
//     public bool HasAnyAttachment()
//     {
//         return attachments != null && attachments.HasAny();
//     }
//
//     public int CountValidNextLinks()
//     {
//         if (nextLinks == null)
//             return 0;
//
//         int count = 0;
//
//         for (int i = 0; i < nextLinks.Count; i++)
//         {
//             VNStoryNextLink link = nextLinks[i];
//             if (link != null && link.HasTarget)
//                 count++;
//         }
//
//         return count;
//     }
//
//     public IEnumerable<VNStoryNextLink> EnumerateValidNextLinks()
//     {
//         if (nextLinks == null)
//             yield break;
//
//         for (int i = 0; i < nextLinks.Count; i++)
//         {
//             VNStoryNextLink link = nextLinks[i];
//             if (link != null && link.HasTarget)
//                 yield return link;
//         }
//     }
// }