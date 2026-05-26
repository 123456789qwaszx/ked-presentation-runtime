// using System.Collections.Generic;
// using UnityEngine;
//
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// [CreateAssetMenu(
//     fileName = "VNStoryGraph",
//     menuName = "VN/Story Graph/VN Story Graph")]
// public sealed class VNStoryGraphSO : ScriptableObject
// {
//     public string graphId;
//     public string chapterKey;
//     public string startNodeId;
//
//     public List<VNStoryGraphNode> nodes = new List<VNStoryGraphNode>();
//
//     public IReadOnlyList<VNStoryGraphNode> Nodes
//     {
//         get { return nodes; }
//     }
//
//     public VNStoryGraphNode FindNode(string nodeId)
//     {
//         if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
//             return null;
//
//         for (int i = 0; i < nodes.Count; i++)
//         {
//             VNStoryGraphNode node = nodes[i];
//             if (node == null)
//                 continue;
//
//             if (node.nodeId == nodeId)
//                 return node;
//         }
//
//         return null;
//     }
//
//     public bool TryFindNode(string nodeId, out VNStoryGraphNode node)
//     {
//         node = FindNode(nodeId);
//         return node != null;
//     }
//
//     public VNStoryGraphNode GetStartNode()
//     {
//         return FindNode(startNodeId);
//     }
//
//     [ContextMenu("VN Story Graph/Test Data/Clear")]
//     public void ClearGraph()
//     {
//         graphId = "";
//         chapterKey = "";
//         startNodeId = "";
//
//         if (nodes == null)
//             nodes = new List<VNStoryGraphNode>();
//         else
//             nodes.Clear();
//
//         MarkDirty();
//     }
//
//     [ContextMenu("VN Story Graph/Test Data/Create Chapter 01 Sample")]
//     public void CreateChapter01Sample()
//     {
//         graphId = "graph.chapter01.sample";
//         chapterKey = "chapter.01";
//         startNodeId = "ch01.ep01";
//
//         nodes = new List<VNStoryGraphNode>();
//
//         VNStoryGraphNode ep01 = Main(
//             "ch01.ep01",
//             "episode.ch01.ep01");
//
//         AddNext(
//             ep01,
//             "ch01.ep01.to.ep02",
//             "ch01.ep02",
//             "choice.ch01.ep01.to.ep02");
//
//         VNStoryGraphNode ep02 = Main(
//             "ch01.ep02",
//             "episode.ch01.ep02");
//
//         AddNext(
//             ep02,
//             "ch01.ep02.to.ep03",
//             "ch01.ep03",
//             "choice.ch01.ep02.to.ep03");
//
//         SetAttachment(
//             ep02,
//             VNStoryAttachmentSlot.Right,
//             "ch01.ep02.attach.hidden_if",
//             "ch01.attach.if_after_ep02",
//             "attach_label.ch01.if_after_ep02",
//             "viewed:ch01.ep02",
//             "");
//
//         VNStoryGraphNode hiddenIf = Attachment(
//             "ch01.attach.if_after_ep02",
//             "ifroute.ch01.after_ep02");
//
//         hiddenIf.endingKey = "ending.ch01.if_after_ep02";
//
//         VNStoryGraphNode ep03 = Main(
//             "ch01.ep03",
//             "episode.ch01.ep03");
//
//         AddNext(
//             ep03,
//             "ch01.ep03.to.ep04",
//             "ch01.ep04",
//             "choice.ch01.ep03.to.ep04");
//
//         VNStoryGraphNode ep04 = Main(
//             "ch01.ep04",
//             "episode.ch01.ep04");
//
//         AddNext(
//             ep04,
//             "ch01.ep04.choice.a",
//             "ch01.route_a.ep05",
//             "choice.ch01.ep04.a");
//
//         AddNext(
//             ep04,
//             "ch01.ep04.choice.b",
//             "ch01.route_b.ep05",
//             "choice.ch01.ep04.b");
//
//         VNStoryGraphNode routeA = Main(
//             "ch01.route_a.ep05",
//             "episode.ch01.route_a.ep05");
//
//         AddNext(
//             routeA,
//             "ch01.route_a.ep05.to.end",
//             "ch01.route_a.end",
//             "choice.ch01.route_a.ep05.to.end");
//
//         VNStoryGraphNode routeAEnd = Main(
//             "ch01.route_a.end",
//             "episode.ch01.route_a.end");
//
//         routeAEnd.endingKey = "ending.ch01.route_a";
//         routeAEnd.opensNextChapter = true;
//         routeAEnd.nextChapterKey = "chapter.02";
//
//         VNStoryGraphNode routeB = Main(
//             "ch01.route_b.ep05",
//             "episode.ch01.route_b.ep05");
//
//         AddNext(
//             routeB,
//             "ch01.route_b.ep05.to.end",
//             "ch01.route_b.end",
//             "choice.ch01.route_b.ep05.to.end");
//
//         VNStoryGraphNode routeBEnd = Main(
//             "ch01.route_b.end",
//             "episode.ch01.route_b.end");
//
//         routeBEnd.endingKey = "ending.ch01.route_b";
//         routeBEnd.opensNextChapter = false;
//         routeBEnd.nextChapterKey = "";
//
//         AddNodes(
//             ep01,
//             ep02,
//             hiddenIf,
//             ep03,
//             ep04,
//             routeA,
//             routeAEnd,
//             routeB,
//             routeBEnd);
//
//         MarkDirty();
//     }
//
//     [ContextMenu("VN Story Graph/Test Data/Create Chapter 02 Sample")]
//     public void CreateChapter02Sample()
//     {
//         graphId = "graph.chapter02.sample";
//         chapterKey = "chapter.02";
//         startNodeId = "ch02.floor01.ep01";
//
//         nodes = new List<VNStoryGraphNode>();
//
//         VNStoryGraphNode f01 = Main(
//             "ch02.floor01.ep01",
//             "episode.ch02.floor01.ep01");
//
//         AddNext(
//             f01,
//             "ch02.f01.to.f02",
//             "ch02.floor02.ep02",
//             "choice.ch02.f01.to.f02");
//
//         VNStoryGraphNode f02 = Main(
//             "ch02.floor02.ep02",
//             "episode.ch02.floor02.ep02");
//
//         AddNext(
//             f02,
//             "ch02.f02.to.f03",
//             "ch02.floor03.ep03",
//             "choice.ch02.f02.to.f03");
//
//         VNStoryGraphNode f03 = Main(
//             "ch02.floor03.ep03",
//             "episode.ch02.floor03.ep03");
//
//         AddNext(
//             f03,
//             "ch02.f03.choice.upper",
//             "ch02.floor04.upper_ep04",
//             "choice.ch02.f03.upper");
//
//         AddNext(
//             f03,
//             "ch02.f03.choice.lower",
//             "ch02.floor04.lower_ep04",
//             "choice.ch02.f03.lower");
//
//         VNStoryGraphNode lower = Main(
//             "ch02.floor04.lower_ep04",
//             "episode.ch02.floor04.lower_ep04");
//
//         SetAttachment(
//             lower,
//             VNStoryAttachmentSlot.Up,
//             "ch02.lower.attach.up",
//             "ch02.attach.lower.up",
//             "attach_label.ch02.lower.up",
//             "",
//             "");
//
//         SetAttachment(
//             lower,
//             VNStoryAttachmentSlot.Right,
//             "ch02.lower.attach.right",
//             "ch02.attach.lower.right",
//             "attach_label.ch02.lower.right",
//             "",
//             "");
//
//         SetAttachment(
//             lower,
//             VNStoryAttachmentSlot.Down,
//             "ch02.lower.attach.down",
//             "ch02.attach.lower.down",
//             "attach_label.ch02.lower.down",
//             "",
//             "");
//
//         VNStoryGraphNode lowerAttachUp = Attachment(
//             "ch02.attach.lower.up",
//             "ending.ch02.lower.up");
//
//         lowerAttachUp.endingKey = "ending.ch02.lower.up";
//
//         VNStoryGraphNode lowerAttachRight = Attachment(
//             "ch02.attach.lower.right",
//             "ending.ch02.lower.right");
//
//         lowerAttachRight.endingKey = "ending.ch02.lower.right";
//
//         VNStoryGraphNode lowerAttachDown = Attachment(
//             "ch02.attach.lower.down",
//             "ending.ch02.lower.down");
//
//         lowerAttachDown.endingKey = "ending.ch02.lower.down";
//
//         VNStoryGraphNode upper = Main(
//             "ch02.floor04.upper_ep04",
//             "episode.ch02.floor04.upper_ep04");
//
//         AddNext(
//             upper,
//             "ch02.upper.choice.skip_to_f06",
//             "ch02.floor06.upper_ending",
//             "choice.ch02.upper.skip_to_f06");
//
//         AddNext(
//             upper,
//             "ch02.upper.choice.center_to_f05",
//             "ch02.floor05.center_ep05",
//             "choice.ch02.upper.center_to_f05");
//
//         AddNext(
//             upper,
//             "ch02.upper.choice.locked_lower",
//             "ch02.floor05.locked_lower",
//             "choice.ch02.upper.locked_lower",
//             "",
//             "locked:ch02.upper.lower_choice");
//
//         VNStoryGraphNode upperSkipEnd = Main(
//             "ch02.floor06.upper_ending",
//             "episode.ch02.floor06.upper_ending");
//
//         upperSkipEnd.endingKey = "ending.ch02.upper_skip";
//         upperSkipEnd.opensNextChapter = false;
//         upperSkipEnd.nextChapterKey = "";
//
//         VNStoryGraphNode centerF05 = Main(
//             "ch02.floor05.center_ep05",
//             "episode.ch02.floor05.center_ep05");
//
//         AddNext(
//             centerF05,
//             "ch02.f05.to.f06.clear",
//             "ch02.floor06.clear_ending",
//             "choice.ch02.f05.to_f06_clear");
//
//         VNStoryGraphNode clearEnd = Main(
//             "ch02.floor06.clear_ending",
//             "episode.ch02.floor06.clear_ending");
//
//         clearEnd.endingKey = "ending.ch02.clear";
//         clearEnd.opensNextChapter = false;
//         clearEnd.nextChapterKey = "";
//
//         VNStoryGraphNode lockedLower = Main(
//             "ch02.floor05.locked_lower",
//             "episode.ch02.floor05.locked_lower");
//
//         lockedLower.endingKey = "ending.ch02.locked_lower";
//         lockedLower.opensNextChapter = false;
//         lockedLower.nextChapterKey = "";
//
//         AddNodes(
//             f01,
//             f02,
//             f03,
//             lower,
//             lowerAttachUp,
//             lowerAttachRight,
//             lowerAttachDown,
//             upper,
//             upperSkipEnd,
//             centerF05,
//             clearEnd,
//             lockedLower);
//
//         MarkDirty();
//     }
//
//     [ContextMenu("VN Story Graph/Test Data/Create All Samples As Chapter 01")]
//     public void CreateAllSamplesAsChapter01()
//     {
//         CreateChapter01Sample();
//     }
//
//     private void AddNodes(params VNStoryGraphNode[] newNodes)
//     {
//         if (nodes == null)
//             nodes = new List<VNStoryGraphNode>();
//
//         for (int i = 0; i < newNodes.Length; i++)
//         {
//             if (newNodes[i] != null)
//                 nodes.Add(newNodes[i]);
//         }
//     }
//
//     private static VNStoryGraphNode Main(
//         string nodeId,
//         string payloadKey)
//     {
//         return new VNStoryGraphNode
//         {
//             nodeId = nodeId,
//             payloadKey = payloadKey,
//             nodeKind = VNStoryNodeKind.Main,
//             nextLinks = new List<VNStoryNextLink>(VNStoryGraphNode.MaxNextLinkCount),
//             attachments = new VNStoryAttachmentRefs()
//         };
//     }
//
//     private static VNStoryGraphNode Attachment(
//         string nodeId,
//         string payloadKey)
//     {
//         return new VNStoryGraphNode
//         {
//             nodeId = nodeId,
//             payloadKey = payloadKey,
//             nodeKind = VNStoryNodeKind.Attachment,
//             nextLinks = new List<VNStoryNextLink>(VNStoryGraphNode.MaxNextLinkCount),
//             attachments = new VNStoryAttachmentRefs()
//         };
//     }
//
//     private static void AddNext(
//         VNStoryGraphNode from,
//         string linkKey,
//         string toNodeId,
//         string labelKey)
//     {
//         AddNext(
//             from,
//             linkKey,
//             toNodeId,
//             labelKey,
//             "",
//             "");
//     }
//
//     private static void AddNext(
//         VNStoryGraphNode from,
//         string linkKey,
//         string toNodeId,
//         string labelKey,
//         string visibleConditionKey,
//         string unlockConditionKey)
//     {
//         if (from == null)
//             return;
//
//         if (from.nextLinks == null)
//             from.nextLinks = new List<VNStoryNextLink>(VNStoryGraphNode.MaxNextLinkCount);
//
//         from.nextLinks.Add(new VNStoryNextLink
//         {
//             linkKey = linkKey,
//             toNodeId = toNodeId,
//             labelKey = labelKey,
//             visibleConditionKey = visibleConditionKey,
//             unlockConditionKey = unlockConditionKey
//         });
//     }
//
//     private static void SetAttachment(
//         VNStoryGraphNode owner,
//         VNStoryAttachmentSlot slot,
//         string linkKey,
//         string toNodeId,
//         string labelKey,
//         string visibleConditionKey,
//         string unlockConditionKey)
//     {
//         if (owner == null)
//             return;
//
//         if (owner.attachments == null)
//             owner.attachments = new VNStoryAttachmentRefs();
//
//         owner.attachments.Set(slot, new VNStoryAttachmentLink
//         {
//             linkKey = linkKey,
//             toNodeId = toNodeId,
//             labelKey = labelKey,
//             visibleConditionKey = visibleConditionKey,
//             unlockConditionKey = unlockConditionKey
//         });
//     }
//
//     private void MarkDirty()
//     {
// #if UNITY_EDITOR
//         EditorUtility.SetDirty(this);
// #endif
//     }
// }