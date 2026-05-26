// using System.Collections.Generic;
// using UnityEngine;
//
// public static class VNStoryGraphViewModelBuilder
// {
//     public static VNStoryGraphViewModel Build(
//         VNStoryGraphSO graph,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryGraphConditionSet conditionSet)
//     {
//         VNStoryGraphViewModel model = new VNStoryGraphViewModel();
//
//         if (graph == null)
//             return model;
//
//         model.graphId = graph.graphId;
//         model.chapterKey = graph.chapterKey;
//         model.startNodeId = graph.startNodeId;
//
//         Dictionary<string, VNStoryGraphNode> graphNodeMap =
//             new Dictionary<string, VNStoryGraphNode>();
//
//         Dictionary<string, VNStoryGraphNodeViewModel> nodeViewMap =
//             new Dictionary<string, VNStoryGraphNodeViewModel>();
//
//         BuildGraphNodeMap(graph, graphNodeMap);
//         BuildNodes(graph, viewData, nodeViewMap, model);
//         BuildNextLinks(graph, viewData, conditionSet, model);
//         ResolveNodeVisibilityAndLockState(
//             graph,
//             conditionSet,
//             graphNodeMap,
//             nodeViewMap,
//             model);
//
//         return model;
//     }
//
//     private static void BuildGraphNodeMap(
//         VNStoryGraphSO graph,
//         Dictionary<string, VNStoryGraphNode> graphNodeMap)
//     {
//         graphNodeMap.Clear();
//
//         if (graph == null || graph.nodes == null)
//             return;
//
//         for (int i = 0; i < graph.nodes.Count; i++)
//         {
//             VNStoryGraphNode node = graph.nodes[i];
//             if (node == null)
//                 continue;
//
//             if (string.IsNullOrWhiteSpace(node.nodeId))
//                 continue;
//
//             if (graphNodeMap.ContainsKey(node.nodeId))
//                 continue;
//
//             graphNodeMap.Add(node.nodeId, node);
//         }
//     }
//
//     private static void BuildNodes(
//         VNStoryGraphSO graph,
//         VNStoryGraphViewDataSO viewData,
//         Dictionary<string, VNStoryGraphNodeViewModel> nodeViewMap,
//         VNStoryGraphViewModel model)
//     {
//         if (graph.nodes == null)
//             return;
//
//         for (int i = 0; i < graph.nodes.Count; i++)
//         {
//             VNStoryGraphNode graphNode = graph.nodes[i];
//             if (graphNode == null)
//                 continue;
//
//             if (string.IsNullOrWhiteSpace(graphNode.nodeId))
//                 continue;
//
//             if (nodeViewMap.ContainsKey(graphNode.nodeId))
//                 continue;
//
//             VNStoryGraphNodeViewModel nodeVm =
//                 BuildNodeViewModel(graphNode, viewData, i);
//
//             nodeViewMap.Add(nodeVm.nodeId, nodeVm);
//             model.nodes.Add(nodeVm);
//         }
//     }
//
//     private static VNStoryGraphNodeViewModel BuildNodeViewModel(
//         VNStoryGraphNode graphNode,
//         VNStoryGraphViewDataSO viewData,
//         int fallbackIndex)
//     {
//         VNStoryGraphNodeViewModel vm = new VNStoryGraphNodeViewModel();
//
//         vm.nodeId = graphNode.nodeId;
//         vm.payloadKey = graphNode.payloadKey;
//         vm.nodeKind = graphNode.nodeKind;
//
//         vm.endingKey = graphNode.endingKey;
//         vm.opensNextChapter = graphNode.opensNextChapter;
//         vm.nextChapterKey = graphNode.nextChapterKey;
//
//         VNStoryNodeViewPatch patch = null;
//         if (viewData != null)
//             patch = viewData.FindNodePatch(graphNode.nodeId);
//
//         vm.position = patch != null
//             ? patch.position
//             : GetFallbackPosition(fallbackIndex);
//
//         vm.size = ResolveNodeSize(graphNode, viewData, patch);
//         vm.sprite = ResolveNodeSprite(graphNode, viewData, patch);
//         vm.color = ResolveNodeColor(graphNode, viewData, patch);
//
//         vm.labelKey = patch != null && !string.IsNullOrWhiteSpace(patch.labelKey)
//             ? patch.labelKey
//             : graphNode.payloadKey;
//
//         vm.displayText = viewData != null
//             ? viewData.ResolveText(vm.labelKey, graphNode.nodeId)
//             : graphNode.nodeId;
//
//         vm.actionKey = patch != null && !string.IsNullOrWhiteSpace(patch.actionKey)
//             ? patch.actionKey
//             : graphNode.payloadKey;
//
//         vm.visible = true;
//         vm.unlocked = true;
//         vm.clickable = true;
//
//         vm.state = graphNode.IsTerminal
//             ? VNStoryGraphNodeViewState.Terminal
//             : VNStoryGraphNodeViewState.Normal;
//
//         return vm;
//     }
//
//     private static void BuildNextLinks(
//         VNStoryGraphSO graph,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryGraphConditionSet conditionSet,
//         VNStoryGraphViewModel model)
//     {
//         if (graph.nodes == null)
//             return;
//
//         for (int i = 0; i < graph.nodes.Count; i++)
//         {
//             VNStoryGraphNode fromNode = graph.nodes[i];
//             if (fromNode == null || fromNode.nextLinks == null)
//                 continue;
//
//             for (int n = 0; n < fromNode.nextLinks.Count; n++)
//             {
//                 VNStoryNextLink link = fromNode.nextLinks[n];
//                 if (link == null || !link.HasTarget)
//                     continue;
//
//                 VNStoryGraphLinkViewModel linkVm = BuildNextLinkViewModel(
//                     fromNode,
//                     link,
//                     viewData,
//                     conditionSet);
//
//                 model.links.Add(linkVm);
//             }
//         }
//     }
//
//     private static VNStoryGraphLinkViewModel BuildNextLinkViewModel(
//         VNStoryGraphNode fromNode,
//         VNStoryNextLink link,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryGraphConditionSet conditionSet)
//     {
//         VNStoryGraphLinkViewModel vm = new VNStoryGraphLinkViewModel();
//
//         vm.linkKind = VNStoryGraphLinkKind.Next;
//
//         vm.linkKey = link.linkKey;
//         vm.fromNodeId = fromNode.nodeId;
//         vm.toNodeId = link.toNodeId;
//
//         vm.labelKey = link.labelKey;
//         vm.displayText = ResolveLinkText(viewData, link.labelKey, link.linkKey);
//
//         vm.visibleConditionKey = link.visibleConditionKey;
//         vm.unlockConditionKey = link.unlockConditionKey;
//
//         vm.visible = conditionSet.Evaluate(link.visibleConditionKey);
//         vm.unlocked = conditionSet.Evaluate(link.unlockConditionKey);
//         vm.clickable = vm.visible && vm.unlocked;
//
//         vm.hasAttachmentSlot = false;
//
//         ResolveLinkVisual(
//             vm,
//             viewData,
//             VNStoryGraphLinkKind.Next,
//             link.linkKey);
//
//         return vm;
//     }
//
//     private static void ResolveNodeVisibilityAndLockState(
//         VNStoryGraphSO graph,
//         VNStoryGraphConditionSet conditionSet,
//         Dictionary<string, VNStoryGraphNode> graphNodeMap,
//         Dictionary<string, VNStoryGraphNodeViewModel> nodeViewMap,
//         VNStoryGraphViewModel model)
//     {
//         Dictionary<string, List<VNStoryGraphLinkViewModel>> incomingNextLinks =
//             BuildIncomingNextLinkMap(model);
//
//         Dictionary<string, AttachmentIncomingState> incomingAttachmentStates =
//             BuildIncomingAttachmentStateMap(
//                 graph,
//                 conditionSet,
//                 graphNodeMap);
//
//         for (int i = 0; i < model.nodes.Count; i++)
//         {
//             VNStoryGraphNodeViewModel node = model.nodes[i];
//             if (node == null)
//                 continue;
//
//             VNStoryGraphNode graphNode = null;
//             graphNodeMap.TryGetValue(node.nodeId, out graphNode);
//
//             if (node.nodeId == graph.startNodeId)
//             {
//                 node.visible = true;
//                 node.unlocked = true;
//                 node.clickable = true;
//                 ApplyFinalNodeState(node);
//                 continue;
//             }
//
//             if (node.nodeKind == VNStoryNodeKind.Attachment)
//             {
//                 ResolveAttachmentNodeState(
//                     node,
//                     incomingAttachmentStates);
//
//                 continue;
//             }
//
//             ResolveMainNodeState(
//                 node,
//                 incomingNextLinks);
//
//             ApplyFinalNodeState(node);
//         }
//     }
//
//     private static Dictionary<string, List<VNStoryGraphLinkViewModel>> BuildIncomingNextLinkMap(
//         VNStoryGraphViewModel model)
//     {
//         Dictionary<string, List<VNStoryGraphLinkViewModel>> incoming =
//             new Dictionary<string, List<VNStoryGraphLinkViewModel>>();
//
//         if (model == null || model.links == null)
//             return incoming;
//
//         for (int i = 0; i < model.links.Count; i++)
//         {
//             VNStoryGraphLinkViewModel link = model.links[i];
//             if (link == null)
//                 continue;
//
//             if (string.IsNullOrWhiteSpace(link.toNodeId))
//                 continue;
//
//             if (!incoming.TryGetValue(link.toNodeId, out List<VNStoryGraphLinkViewModel> list))
//             {
//                 list = new List<VNStoryGraphLinkViewModel>();
//                 incoming.Add(link.toNodeId, list);
//             }
//
//             list.Add(link);
//         }
//
//         return incoming;
//     }
//
//     private static Dictionary<string, AttachmentIncomingState> BuildIncomingAttachmentStateMap(
//         VNStoryGraphSO graph,
//         VNStoryGraphConditionSet conditionSet,
//         Dictionary<string, VNStoryGraphNode> graphNodeMap)
//     {
//         Dictionary<string, AttachmentIncomingState> result =
//             new Dictionary<string, AttachmentIncomingState>();
//
//         if (graph == null || graph.nodes == null)
//             return result;
//
//         for (int i = 0; i < graph.nodes.Count; i++)
//         {
//             VNStoryGraphNode ownerNode = graph.nodes[i];
//             if (ownerNode == null || ownerNode.attachments == null)
//                 continue;
//
//             CollectAttachmentIncomingState(
//                 ownerNode.attachments.up,
//                 conditionSet,
//                 result);
//
//             CollectAttachmentIncomingState(
//                 ownerNode.attachments.right,
//                 conditionSet,
//                 result);
//
//             CollectAttachmentIncomingState(
//                 ownerNode.attachments.down,
//                 conditionSet,
//                 result);
//         }
//
//         return result;
//     }
//
//     private static void CollectAttachmentIncomingState(
//         VNStoryAttachmentLink link,
//         VNStoryGraphConditionSet conditionSet,
//         Dictionary<string, AttachmentIncomingState> result)
//     {
//         if (link == null || !link.HasTarget)
//             return;
//         bool visible = conditionSet.Evaluate(link.visibleConditionKey);
//         bool unlocked = conditionSet.Evaluate(link.unlockConditionKey);
//
//         AttachmentIncomingState state;
//         if (!result.TryGetValue(link.toNodeId, out state))
//         {
//             state = new AttachmentIncomingState();
//             result.Add(link.toNodeId, state);
//         }
//
//         state.referenceCount++;
//
//         if (visible)
//             state.hasVisibleIncoming = true;
//
//         if (visible && unlocked)
//             state.hasUnlockedIncoming = true;
//     }
//
//     private static void ResolveAttachmentNodeState(
//         VNStoryGraphNodeViewModel node,
//         Dictionary<string, AttachmentIncomingState> incomingAttachmentStates)
//     {
//         AttachmentIncomingState state;
//         if (!incomingAttachmentStates.TryGetValue(node.nodeId, out state))
//         {
//             node.visible = false;
//             node.unlocked = false;
//             node.clickable = false;
//             node.state = VNStoryGraphNodeViewState.Hidden;
//             return;
//         }
//
//         node.visible = state.hasVisibleIncoming;
//         node.unlocked = state.hasUnlockedIncoming;
//         node.clickable = node.visible && node.unlocked;
//
//         ApplyFinalNodeState(node);
//     }
//
//     private static void ResolveMainNodeState(
//         VNStoryGraphNodeViewModel node,
//         Dictionary<string, List<VNStoryGraphLinkViewModel>> incomingNextLinks)
//     {
//         List<VNStoryGraphLinkViewModel> incomingLinks;
//         if (!incomingNextLinks.TryGetValue(node.nodeId, out incomingLinks))
//         {
//             node.visible = true;
//             node.unlocked = true;
//             node.clickable = true;
//             return;
//         }
//
//         bool hasVisibleIncoming = false;
//         bool hasUnlockedIncoming = false;
//
//         for (int i = 0; i < incomingLinks.Count; i++)
//         {
//             VNStoryGraphLinkViewModel link = incomingLinks[i];
//
//             if (link.visible)
//                 hasVisibleIncoming = true;
//
//             if (link.visible && link.unlocked)
//                 hasUnlockedIncoming = true;
//         }
//
//         node.visible = true;
//         node.unlocked = incomingLinks.Count == 0 || hasUnlockedIncoming;
//         node.clickable = node.unlocked;
//
//         if (!hasVisibleIncoming)
//         {
//             node.visible = false;
//             node.unlocked = false;
//             node.clickable = false;
//         }
//     }
//
//     private static void ApplyFinalNodeState(
//         VNStoryGraphNodeViewModel node)
//     {
//         if (node == null)
//             return;
//
//         if (!node.visible)
//         {
//             node.state = VNStoryGraphNodeViewState.Hidden;
//             return;
//         }
//
//         if (!node.unlocked)
//         {
//             node.state = VNStoryGraphNodeViewState.Locked;
//             return;
//         }
//
//         if (!string.IsNullOrWhiteSpace(node.endingKey))
//         {
//             node.state = VNStoryGraphNodeViewState.Terminal;
//             return;
//         }
//
//         node.state = VNStoryGraphNodeViewState.Normal;
//     }
//
//     private static string ResolveLinkText(
//         VNStoryGraphViewDataSO viewData,
//         string labelKey,
//         string fallback)
//     {
//         if (viewData == null)
//             return fallback;
//
//         return viewData.ResolveText(labelKey, fallback);
//     }
//
//     private static Vector2 ResolveNodeSize(
//         VNStoryGraphNode graphNode,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryNodeViewPatch patch)
//     {
//         if (patch != null && patch.size != Vector2.zero)
//             return patch.size;
//
//         if (viewData == null)
//         {
//             if (graphNode.nodeKind == VNStoryNodeKind.Attachment)
//                 return new Vector2(300f, 110f);
//
//             return new Vector2(350f, 136f);
//         }
//
//         if (graphNode.nodeKind == VNStoryNodeKind.Attachment)
//             return viewData.attachmentNodeSize;
//
//         return viewData.mainNodeSize;
//     }
//
//     private static Sprite ResolveNodeSprite(
//         VNStoryGraphNode graphNode,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryNodeViewPatch patch)
//     {
//         if (patch != null && patch.sprite != null)
//             return patch.sprite;
//
//         if (viewData == null)
//             return null;
//
//         if (graphNode.nodeKind == VNStoryNodeKind.Attachment)
//             return viewData.defaultAttachmentSprite;
//
//         return viewData.defaultMainSprite;
//     }
//
//     private static Color ResolveNodeColor(
//         VNStoryGraphNode graphNode,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryNodeViewPatch patch)
//     {
//         if (patch != null && patch.overrideColor)
//             return patch.color;
//
//         if (viewData == null)
//         {
//             if (graphNode.nodeKind == VNStoryNodeKind.Attachment)
//                 return new Color(0.32f, 0.42f, 0.72f, 0.96f);
//
//             return new Color(0.92f, 0.96f, 1f, 0.92f);
//         }
//
//         if (graphNode.IsTerminal)
//             return viewData.defaultTerminalColor;
//
//         if (graphNode.nodeKind == VNStoryNodeKind.Attachment)
//             return viewData.defaultAttachmentColor;
//
//         return viewData.defaultMainColor;
//     }
//
//     private static void ResolveLinkVisual(
//         VNStoryGraphLinkViewModel vm,
//         VNStoryGraphViewDataSO viewData,
//         VNStoryGraphLinkKind kind,
//         string linkKey)
//     {
//         VNStoryLinkViewPatch patch = null;
//
//         if (viewData != null)
//             patch = viewData.FindLinkPatch(linkKey);
//
//         if (patch != null && patch.overrideColor)
//         {
//             vm.color = patch.color;
//         }
//         else if (!vm.unlocked && viewData != null)
//         {
//             vm.color = viewData.defaultLockedLineColor;
//         }
//         else if (viewData != null)
//         {
//             vm.color = viewData.defaultNextLineColor;
//         }
//         else
//         {
//             vm.color = new Color(0.55f, 0.78f, 1f, 0.75f);
//         }
//
//         if (patch != null && patch.overrideThickness)
//         {
//             vm.thickness = patch.thickness;
//         }
//         else if (viewData != null)
//         {
//             vm.thickness = viewData.defaultNextLineThickness;
//         }
//         else
//         {
//             vm.thickness = 5f;
//         }
//     }
//
//     private static Vector2 GetFallbackPosition(int index)
//     {
//         int col = index % 4;
//         int row = index / 4;
//
//         return new Vector2(col * 400f, -row * 200f);
//     }
//
//     private sealed class AttachmentIncomingState
//     {
//         public int referenceCount;
//         public bool hasVisibleIncoming;
//         public bool hasUnlockedIncoming;
//     }
// }