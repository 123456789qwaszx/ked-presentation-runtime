// using System.Collections.Generic;
//
// public sealed class VNStoryGraphRuntimeReader
// {
//     private readonly VNStoryGraphSO _graph;
//     private readonly Dictionary<string, VNStoryGraphNode> _nodeById =
//         new Dictionary<string, VNStoryGraphNode>();
//
//     public VNStoryGraphRuntimeReader(VNStoryGraphSO graph)
//     {
//         _graph = graph;
//         BuildIndex();
//     }
//
//     public VNStoryGraphNode GetNode(string nodeId)
//     {
//         if (string.IsNullOrWhiteSpace(nodeId))
//             return null;
//
//         VNStoryGraphNode node;
//         if (_nodeById.TryGetValue(nodeId, out node))
//             return node;
//
//         return null;
//     }
//
//     public VNStoryGraphNode GetStartNode()
//     {
//         if (_graph == null)
//             return null;
//
//         return GetNode(_graph.startNodeId);
//     }
//
//     public List<VNStoryNextLink> GetNextLinks(string nodeId)
//     {
//         List<VNStoryNextLink> result = new List<VNStoryNextLink>();
//
//         VNStoryGraphNode node = GetNode(nodeId);
//         if (node == null)
//             return result;
//
//         foreach (VNStoryNextLink link in node.EnumerateValidNextLinks())
//             result.Add(link);
//
//         return result;
//     }
//
//     public List<VNStoryGraphNode> GetNextNodes(string nodeId)
//     {
//         List<VNStoryGraphNode> result = new List<VNStoryGraphNode>();
//
//         List<VNStoryNextLink> links = GetNextLinks(nodeId);
//         for (int i = 0; i < links.Count; i++)
//         {
//             VNStoryGraphNode next = GetNode(links[i].toNodeId);
//             if (next != null)
//                 result.Add(next);
//         }
//
//         return result;
//     }
//
//     public VNStoryAttachmentLink GetAttachmentLink(
//         string ownerNodeId,
//         VNStoryAttachmentSlot slot)
//     {
//         VNStoryGraphNode owner = GetNode(ownerNodeId);
//         if (owner == null || owner.attachments == null)
//             return null;
//
//         return owner.attachments.Get(slot);
//     }
//
//     public VNStoryGraphNode GetAttachmentNode(
//         string ownerNodeId,
//         VNStoryAttachmentSlot slot)
//     {
//         VNStoryAttachmentLink link = GetAttachmentLink(ownerNodeId, slot);
//         if (link == null || !link.HasTarget)
//             return null;
//
//         return GetNode(link.toNodeId);
//     }
//
//     public bool IsTerminal(string nodeId)
//     {
//         VNStoryGraphNode node = GetNode(nodeId);
//         return node != null && node.IsTerminal;
//     }
//
//     public bool CanOpenNextChapter(string nodeId, out string nextChapterKey)
//     {
//         nextChapterKey = null;
//
//         VNStoryGraphNode node = GetNode(nodeId);
//         if (node == null)
//             return false;
//
//         if (!node.HasNextChapterUnlock)
//             return false;
//
//         nextChapterKey = node.nextChapterKey;
//         return true;
//     }
//
//     public bool TryGetEndingKey(string nodeId, out string endingKey)
//     {
//         endingKey = null;
//
//         VNStoryGraphNode node = GetNode(nodeId);
//         if (node == null || !node.HasEnding)
//             return false;
//
//         endingKey = node.endingKey;
//         return true;
//     }
//
//     private void BuildIndex()
//     {
//         _nodeById.Clear();
//
//         if (_graph == null || _graph.nodes == null)
//             return;
//
//         for (int i = 0; i < _graph.nodes.Count; i++)
//         {
//             VNStoryGraphNode node = _graph.nodes[i];
//             if (node == null)
//                 continue;
//
//             if (string.IsNullOrWhiteSpace(node.nodeId))
//                 continue;
//
//             if (_nodeById.ContainsKey(node.nodeId))
//                 continue;
//
//             _nodeById.Add(node.nodeId, node);
//         }
//     }
// }