// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// [CreateAssetMenu(
//     fileName = "VNStoryGraphViewData",
//     menuName = "VN/Story Graph/VN Story Graph View Data")]
// public sealed class VNStoryGraphViewDataSO : ScriptableObject
// {
//     [Header("Node Size")]
//     public Vector2 mainNodeSize = new Vector2(350f, 136f);
//     public Vector2 attachmentNodeSize = new Vector2(300f, 110f);
//
//     [Header("Layout Rule")]
//     public float horizontalStep = 400f;
//     public float branchVerticalStep = 200f;
//     public float attachmentVerticalStep = 220f;
//     public float attachmentHorizontalStep = 360f;
//
//     [Header("Default Node Sprite")]
//     public Sprite defaultMainSprite;
//     public Sprite defaultAttachmentSprite;
//
//     [Header("Default Node Color")]
//     public Color defaultMainColor = new Color(0.92f, 0.96f, 1f, 0.92f);
//     public Color defaultAttachmentColor = new Color(0.32f, 0.42f, 0.72f, 0.96f);
//     public Color defaultTerminalColor = new Color(0.95f, 0.86f, 0.48f, 0.96f);
//     public Color defaultLockedColor = new Color(0.18f, 0.18f, 0.2f, 0.85f);
//
//     [Header("Default Link")]
//     public Color defaultNextLineColor = new Color(0.55f, 0.78f, 1f, 0.75f);
//     public Color defaultAttachmentLineColor = new Color(1f, 0.48f, 0.72f, 0.8f);
//     public Color defaultLockedLineColor = new Color(0.35f, 0.35f, 0.38f, 0.75f);
//
//     public float defaultNextLineThickness = 5f;
//     public float defaultAttachmentLineThickness = 4f;
//
//     [Header("Node Patches")]
//     public List<VNStoryNodeViewPatch> nodePatches = new List<VNStoryNodeViewPatch>();
//
//     [Header("Link Patches")]
//     public List<VNStoryLinkViewPatch> linkPatches = new List<VNStoryLinkViewPatch>();
//
//     [Header("Text Table")]
//     public List<VNStoryGraphTextEntry> textEntries = new List<VNStoryGraphTextEntry>();
//
//     public VNStoryNodeViewPatch FindNodePatch(string nodeId)
//     {
//         if (string.IsNullOrWhiteSpace(nodeId) || nodePatches == null)
//             return null;
//
//         for (int i = 0; i < nodePatches.Count; i++)
//         {
//             VNStoryNodeViewPatch patch = nodePatches[i];
//             if (patch == null)
//                 continue;
//
//             if (patch.nodeId == nodeId)
//                 return patch;
//         }
//
//         return null;
//     }
//
//     public VNStoryLinkViewPatch FindLinkPatch(string linkKey)
//     {
//         if (string.IsNullOrWhiteSpace(linkKey) || linkPatches == null)
//             return null;
//
//         for (int i = 0; i < linkPatches.Count; i++)
//         {
//             VNStoryLinkViewPatch patch = linkPatches[i];
//             if (patch == null)
//                 continue;
//
//             if (patch.linkKey == linkKey)
//                 return patch;
//         }
//
//         return null;
//     }
//
//     public string ResolveText(string key, string fallback)
//     {
//         if (string.IsNullOrWhiteSpace(key))
//             return fallback;
//
//         if (textEntries == null)
//             return fallback;
//
//         for (int i = 0; i < textEntries.Count; i++)
//         {
//             VNStoryGraphTextEntry entry = textEntries[i];
//             if (entry == null)
//                 continue;
//
//             if (entry.key == key)
//                 return entry.text;
//         }
//
//         return fallback;
//     }
//
//     [ContextMenu("VN Story Graph ViewData/Clear")]
//     public void Clear()
//     {
//         nodePatches = new List<VNStoryNodeViewPatch>();
//         linkPatches = new List<VNStoryLinkViewPatch>();
//         textEntries = new List<VNStoryGraphTextEntry>();
//
//         MarkDirty();
//     }
//
//     [ContextMenu("VN Story Graph ViewData/Create Chapter 01 Layout Sample")]
//     public void CreateChapter01LayoutSample()
//     {
//         Clear();
//
//         mainNodeSize = new Vector2(350f, 136f);
//         attachmentNodeSize = new Vector2(300f, 110f);
//
//         horizontalStep = 400f;
//         branchVerticalStep = 200f;
//         attachmentVerticalStep = 220f;
//         attachmentHorizontalStep = 360f;
//
//         AddNodePatch(
//             "ch01.ep01",
//             new Vector2(-600f, 0f),
//             "label.ch01.ep01",
//             "action.ch01.ep01");
//
//         AddNodePatch(
//             "ch01.ep02",
//             new Vector2(-200f, 0f),
//             "label.ch01.ep02",
//             "action.ch01.ep02");
//
//         AddNodePatch(
//             "ch01.attach.if_after_ep02",
//             new Vector2(-200f, attachmentVerticalStep),
//             "label.ch01.attach.if_after_ep02",
//             "action.ch01.attach.if_after_ep02");
//
//         AddNodePatch(
//             "ch01.ep03",
//             new Vector2(200f, 0f),
//             "label.ch01.ep03",
//             "action.ch01.ep03");
//
//         AddNodePatch(
//             "ch01.ep04",
//             new Vector2(600f, 0f),
//             "label.ch01.ep04",
//             "action.ch01.ep04");
//
//         AddNodePatch(
//             "ch01.route_a.ep05",
//             new Vector2(1000f, branchVerticalStep * 0.5f),
//             "label.ch01.route_a.ep05",
//             "action.ch01.route_a.ep05");
//
//         AddNodePatch(
//             "ch01.route_a.end",
//             new Vector2(1400f, branchVerticalStep * 0.5f),
//             "label.ch01.route_a.end",
//             "action.ch01.route_a.end");
//
//         AddNodePatch(
//             "ch01.route_b.ep05",
//             new Vector2(1000f, -branchVerticalStep * 0.5f),
//             "label.ch01.route_b.ep05",
//             "action.ch01.route_b.ep05");
//
//         AddNodePatch(
//             "ch01.route_b.end",
//             new Vector2(1400f, -branchVerticalStep * 0.5f),
//             "label.ch01.route_b.end",
//             "action.ch01.route_b.end");
//
//         AddText("label.ch01.ep01", "01\n컴퍼니와 오아시스");
//         AddText("label.ch01.ep02", "02\n시청 후 변화");
//         AddText("label.ch01.ep03", "03\n낯선 시선");
//         AddText("label.ch01.ep04", "04\n갈림길");
//         AddText("label.ch01.route_a.ep05", "05A\n심야잠입");
//         AddText("label.ch01.route_a.end", "END A\n다음 장으로");
//         AddText("label.ch01.route_b.ep05", "05B\n변장 수사");
//         AddText("label.ch01.route_b.end", "END B\n닫힌 결말");
//         AddText("label.ch01.attach.if_after_ep02", "IF\n보이지 않던 길");
//
//         MarkDirty();
//     }
//
//     [ContextMenu("VN Story Graph ViewData/Create Chapter 02 Layout Sample")]
//     public void CreateChapter02LayoutSample()
//     {
//         Clear();
//
//         mainNodeSize = new Vector2(350f, 136f);
//         attachmentNodeSize = new Vector2(300f, 110f);
//
//         horizontalStep = 400f;
//         branchVerticalStep = 200f;
//         attachmentVerticalStep = 220f;
//         attachmentHorizontalStep = 360f;
//
//         AddNodePatch(
//             "ch02.floor01.ep01",
//             new Vector2(-800f, 0f),
//             "label.ch02.floor01.ep01",
//             "action.ch02.floor01.ep01");
//
//         AddNodePatch(
//             "ch02.floor02.ep02",
//             new Vector2(-400f, 0f),
//             "label.ch02.floor02.ep02",
//             "action.ch02.floor02.ep02");
//
//         AddNodePatch(
//             "ch02.floor03.ep03",
//             new Vector2(0f, 0f),
//             "label.ch02.floor03.ep03",
//             "action.ch02.floor03.ep03");
//
//         AddNodePatch(
//             "ch02.floor04.upper_ep04",
//             new Vector2(400f, branchVerticalStep * 0.5f),
//             "label.ch02.floor04.upper_ep04",
//             "action.ch02.floor04.upper_ep04");
//
//         AddNodePatch(
//             "ch02.floor04.lower_ep04",
//             new Vector2(400f, -branchVerticalStep * 0.5f),
//             "label.ch02.floor04.lower_ep04",
//             "action.ch02.floor04.lower_ep04");
//
//         AddNodePatch(
//             "ch02.attach.lower.up",
//             new Vector2(400f, -branchVerticalStep * 0.5f + attachmentVerticalStep),
//             "label.ch02.attach.lower.up",
//             "action.ch02.attach.lower.up");
//
//         AddNodePatch(
//             "ch02.attach.lower.right",
//             new Vector2(400f + attachmentHorizontalStep, -branchVerticalStep * 0.5f),
//             "label.ch02.attach.lower.right",
//             "action.ch02.attach.lower.right");
//
//         AddNodePatch(
//             "ch02.attach.lower.down",
//             new Vector2(400f, -branchVerticalStep * 0.5f - attachmentVerticalStep),
//             "label.ch02.attach.lower.down",
//             "action.ch02.attach.lower.down");
//
//         AddNodePatch(
//             "ch02.floor06.upper_ending",
//             new Vector2(1200f, branchVerticalStep),
//             "label.ch02.floor06.upper_ending",
//             "action.ch02.floor06.upper_ending");
//
//         AddNodePatch(
//             "ch02.floor05.center_ep05",
//             new Vector2(800f, branchVerticalStep * 0.5f),
//             "label.ch02.floor05.center_ep05",
//             "action.ch02.floor05.center_ep05");
//
//         AddNodePatch(
//             "ch02.floor06.clear_ending",
//             new Vector2(1200f, branchVerticalStep * 0.5f),
//             "label.ch02.floor06.clear_ending",
//             "action.ch02.floor06.clear_ending");
//
//         AddNodePatch(
//             "ch02.floor05.locked_lower",
//             new Vector2(800f, -branchVerticalStep * 0.5f),
//             "label.ch02.floor05.locked_lower",
//             "action.ch02.floor05.locked_lower");
//
//         AddText("label.ch02.floor01.ep01", "01\n첫 번째 층");
//         AddText("label.ch02.floor02.ep02", "02\n두 번째 층");
//         AddText("label.ch02.floor03.ep03", "03\n분기점");
//         AddText("label.ch02.floor04.upper_ep04", "04A\n위쪽 루트");
//         AddText("label.ch02.floor04.lower_ep04", "04B\n아래쪽 루트");
//
//         AddText("label.ch02.attach.lower.up", "IF\n위쪽 잔향");
//         AddText("label.ch02.attach.lower.right", "IF\n막힌 오른쪽");
//         AddText("label.ch02.attach.lower.down", "IF\n아래쪽 결말");
//
//         AddText("label.ch02.floor06.upper_ending", "06A\n건너뛴 엔딩");
//         AddText("label.ch02.floor05.center_ep05", "05\n중앙 루트");
//         AddText("label.ch02.floor06.clear_ending", "06\n클리어 엔딩");
//         AddText("label.ch02.floor05.locked_lower", "05?\n잠긴 선택지");
//
//         MarkDirty();
//     }
//
//     private void AddNodePatch(
//         string nodeId,
//         Vector2 position,
//         string labelKey,
//         string actionKey)
//     {
//         if (nodePatches == null)
//             nodePatches = new List<VNStoryNodeViewPatch>();
//
//         nodePatches.Add(new VNStoryNodeViewPatch
//         {
//             nodeId = nodeId,
//             position = position,
//             size = Vector2.zero,
//             sprite = null,
//             overrideColor = false,
//             color = Color.white,
//             labelKey = labelKey,
//             actionKey = actionKey
//         });
//     }
//
//     private void AddText(string key, string text)
//     {
//         if (textEntries == null)
//             textEntries = new List<VNStoryGraphTextEntry>();
//
//         textEntries.Add(new VNStoryGraphTextEntry
//         {
//             key = key,
//             text = text
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
//
// [Serializable]
// public sealed class VNStoryNodeViewPatch
// {
//     public string nodeId;
//
//     [Header("Layout")]
//     public Vector2 position;
//     public Vector2 size;
//
//     [Header("Visual")]
//     public Sprite sprite;
//
//     public bool overrideColor;
//     public Color color = Color.white;
//
//     [Header("Text / Action")]
//     public string labelKey;
//     public string actionKey;
// }
//
// [Serializable]
// public sealed class VNStoryLinkViewPatch
// {
//     public string linkKey;
//
//     [Header("Visual")]
//     public bool overrideColor;
//     public Color color = Color.white;
//
//     public bool overrideThickness;
//     public float thickness = 4f;
//
//     [Header("Text")]
//     public string labelKey;
// }
//
// [Serializable]
// public sealed class VNStoryGraphTextEntry
// {
//     public string key;
//     public string text;
// }