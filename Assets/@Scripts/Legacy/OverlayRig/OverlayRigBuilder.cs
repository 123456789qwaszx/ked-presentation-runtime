// using System;
// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using Object = UnityEngine.Object;
//
// public sealed class OverlayRigBuilder
// {
//     public RectTransform BuildOverlayRoot(
//         RectTransform prefab = null,
//         string rolePrefix = "",
//         string rootName = "OverlayRig")
//     {
//         RectTransform root;
//
//         if (prefab != null)
//         {
//             root = Object.Instantiate(prefab);
//             root.name = WithRole(rolePrefix, rootName);
//
//             if (!string.IsNullOrEmpty(rolePrefix))
//                 PrefixAllChildren(root.transform, rolePrefix);
//         }
//         else
//         {
//             GameObject go = new(WithRole(rolePrefix, rootName), typeof(RectTransform));
//             root = (RectTransform)go.transform;
//         }
//
//         StretchFull(root);
//         EnsureGraph(root, rolePrefix);
//
//         return root;
//     }
//
//     public void BindRefsFromRoot(
//         RectTransform root,
//         string rolePrefix,
//         out OverlayRigRefs refs)
//     {
//         Dictionary<OverlayRigSchema.Refs, RectTransform> map =
//             CollectRefMap(root, rolePrefix);
//
//         EnsureValidGraphMap(root, rolePrefix, ref map);
//
//         refs = BuildRefs(root, map);
//     }
//
//     private void EnsureValidGraphMap(
//         RectTransform root,
//         string rolePrefix,
//         ref Dictionary<OverlayRigSchema.Refs, RectTransform> map)
//     {
//         int expectedCount = Enum.GetValues(typeof(OverlayRigSchema.Refs)).Length;
//
//         if (map.Count >= expectedCount)
//             return;
//
//         Debug.LogWarning(
//             $"[OverlayRigBuilder] Invalid graph. " +
//             $"Rebuilding from OverlayRigSchema. " +
//             $"root='{root.name}', rolePrefix='{rolePrefix}'.",
//             root);
//
//         for (int i = root.childCount - 1; i >= 0; i--)
//         {
//             Transform child = root.GetChild(i);
//             child.SetParent(null, false);
//             Object.Destroy(child.gameObject);
//         }
//
//         EnsureGraph(root, rolePrefix);
//         map = CollectRefMap(root, rolePrefix);
//     }
//
//     private void EnsureGraph(RectTransform root, string rolePrefix)
//     {
//         foreach (OverlayRigSchema.NodeDef node in OverlayRigSchema.Nodes)
//             EnsureNode(root, rolePrefix, node);
//
//         NormalizeSiblingOrder(root, rolePrefix);
//     }
//
//     private void EnsureNode(
//         RectTransform root,
//         string rolePrefix,
//         OverlayRigSchema.NodeDef node)
//     {
//         RectTransform parent = node.Parent.HasValue
//             ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
//             : root;
//
//         if (parent == null)
//             parent = root;
//
//         RectTransform rt = EnsureRect(
//             root,
//             parent,
//             WithRole(rolePrefix, node.Id.ToString()),
//             node.StretchFull,
//             node.NeedsBottomPivot,
//             node.NeedsCenterPivot);
//
//         if (node.NeedsCanvasGroup)
//             EnsureCanvasGroup(rt, node.InitialCanvasGroupAlpha);
//
//         if (node.NeedsImage)
//             EnsureImage(rt, node.InitialGraphicColor, node.RaycastTarget);
//
//         if (node.NeedsText)
//             EnsureText(rt, node.InitialGraphicColor, node.RaycastTarget);
//     }
//
//     private static void EnsureCanvasGroup(RectTransform rt, float alpha)
//     {
//         if (!rt.TryGetComponent(out CanvasGroup canvasGroup))
//             canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();
//
//         canvasGroup.alpha = alpha;
//         canvasGroup.interactable = false;
//         canvasGroup.blocksRaycasts = false;
//     }
//
//     private static void EnsureImage(
//         RectTransform rt,
//         Color color,
//         bool raycastTarget)
//     {
//         if (!rt.TryGetComponent(out Image image))
//             image = rt.gameObject.AddComponent<Image>();
//
//         image.color = color;
//         image.raycastTarget = raycastTarget;
//         image.preserveAspect = true;
//     }
//
//     private static void EnsureText(
//         RectTransform rt,
//         Color color,
//         bool raycastTarget)
//     {
//         if (!rt.TryGetComponent(out TextMeshProUGUI text))
//             text = rt.gameObject.AddComponent<TextMeshProUGUI>();
//
//         text.text = string.Empty;
//         text.color = color;
//         text.raycastTarget = raycastTarget;
//         text.alignment = TextAlignmentOptions.Center;
//         text.textWrappingMode = TextWrappingModes.NoWrap;
//     }
//
//     private RectTransform EnsureRect(
//         RectTransform root,
//         RectTransform parent,
//         string name,
//         bool stretchFull,
//         bool bottomPivot,
//         bool centerPivot)
//     {
//         RectTransform existing = FindByName(root, name) as RectTransform;
//         bool created = existing == null;
//
//         if (created)
//         {
//             GameObject go = new(name, typeof(RectTransform));
//             existing = (RectTransform)go.transform;
//         }
//
//         if (existing.parent != parent)
//             existing.SetParent(parent, false);
//
//         if (stretchFull)
//         {
//             StretchFull(existing);
//         }
//         else if (created)
//         {
//             existing.anchorMin = new Vector2(0.5f, 0.5f);
//             existing.anchorMax = new Vector2(0.5f, 0.5f);
//
//             if (bottomPivot)
//                 existing.pivot = new Vector2(0.5f, 0f);
//             else if (centerPivot)
//                 existing.pivot = new Vector2(0.5f, 0.5f);
//
//             existing.anchoredPosition = Vector2.zero;
//             existing.sizeDelta = Vector2.zero;
//             existing.localScale = Vector3.one;
//             existing.localRotation = Quaternion.identity;
//         }
//
//         return existing;
//     }
//
//     private Dictionary<OverlayRigSchema.Refs, RectTransform> CollectRefMap(
//         RectTransform root,
//         string rolePrefix)
//     {
//         Dictionary<OverlayRigSchema.Refs, RectTransform> map = new();
//
//         foreach (OverlayRigSchema.Refs id in
//                  Enum.GetValues(typeof(OverlayRigSchema.Refs)))
//         {
//             string nodeName = WithRole(rolePrefix, id.ToString());
//             Transform t = FindByName(root, nodeName);
//
//             if (t is RectTransform rt)
//                 map[id] = rt;
//         }
//
//         return map;
//     }
//
//     private OverlayRigRefs BuildRefs(
//         RectTransform root,
//         Dictionary<OverlayRigSchema.Refs, RectTransform> map)
//     {
//         OverlayRigRefs refs = new(root);
//
//         RectTransform GetRt(OverlayRigSchema.Refs key)
//         {
//             if (!map.TryGetValue(key, out RectTransform rt) || rt == null)
//             {
//                 Debug.LogWarning($"[OverlayRigBuilder] Missing ref '{key}'.");
//                 return null;
//             }
//
//             return rt;
//         }
//
//         Image GetImg(OverlayRigSchema.Refs key)
//         {
//             RectTransform rt = GetRt(key);
//             return rt != null ? rt.GetComponent<Image>() : null;
//         }
//
//         TextMeshProUGUI GetText(OverlayRigSchema.Refs key)
//         {
//             RectTransform rt = GetRt(key);
//             return rt != null ? rt.GetComponent<TextMeshProUGUI>() : null;
//         }
//
//         refs.Overlay_Root = GetRt(OverlayRigSchema.Refs.Overlay_Root);
//         refs.Overlay_RootCanvasGroup = refs.Overlay_Root != null
//             ? refs.Overlay_Root.GetComponent<CanvasGroup>()
//             : null;
//
//         refs.Overlay_Anchor = GetRt(OverlayRigSchema.Refs.Overlay_Anchor);
//
//         refs.Overlay_Track = GetRt(OverlayRigSchema.Refs.Overlay_Track);
//
//         refs.Overlay_BaseRotation = GetRt(OverlayRigSchema.Refs.Overlay_BaseRotation);
//
//         refs.Overlay_Track_Move = GetRt(OverlayRigSchema.Refs.Overlay_Track_Move);
//         refs.Overlay_Track_X = GetRt(OverlayRigSchema.Refs.Overlay_Track_X);
//         refs.Overlay_Track_X_Offset = GetRt(OverlayRigSchema.Refs.Overlay_Track_X_Offset);
//         refs.Overlay_Track_Y = GetRt(OverlayRigSchema.Refs.Overlay_Track_Y);
//         refs.Overlay_Track_Y_Offset = GetRt(OverlayRigSchema.Refs.Overlay_Track_Y_Offset);
//
//         refs.Overlay_Rotation = GetRt(OverlayRigSchema.Refs.Overlay_Rotation);
//
//         refs.Overlay_Size = GetRt(OverlayRigSchema.Refs.Overlay_Size);
//         refs.Overlay_Scale = GetRt(OverlayRigSchema.Refs.Overlay_Scale);
//
//         refs.Overlay_ActingScale = GetRt(OverlayRigSchema.Refs.Overlay_ActingScale);
//         refs.Overlay_ActingScale_X = GetRt(OverlayRigSchema.Refs.Overlay_ActingScale_X);
//         refs.Overlay_ActingScale_Y = GetRt(OverlayRigSchema.Refs.Overlay_ActingScale_Y);
//
//         refs.Overlay_Content = GetRt(OverlayRigSchema.Refs.Overlay_Content);
//
//         refs.Overlay_ImageBox = GetRt(OverlayRigSchema.Refs.Overlay_ImageBox);
//         refs.Overlay_ImagePad = GetRt(OverlayRigSchema.Refs.Overlay_ImagePad);
//         refs.Overlay_Image = GetImg(OverlayRigSchema.Refs.Overlay_Image);
//
//         refs.Overlay_TextBox = GetRt(OverlayRigSchema.Refs.Overlay_TextBox);
//         refs.Overlay_TextPad = GetRt(OverlayRigSchema.Refs.Overlay_TextPad);
//         refs.Overlay_Text = GetText(OverlayRigSchema.Refs.Overlay_Text);
//
//         return refs;
//     }
//
//     private void NormalizeSiblingOrder(RectTransform root, string rolePrefix)
//     {
//         Dictionary<Transform, int> nextIndexByParent = new();
//
//         foreach (OverlayRigSchema.NodeDef node in OverlayRigSchema.Nodes)
//         {
//             RectTransform rt =
//                 FindByName(root, WithRole(rolePrefix, node.Id.ToString())) as RectTransform;
//
//             if (rt == null)
//                 continue;
//
//             RectTransform parent = node.Parent.HasValue
//                 ? FindByName(root, WithRole(rolePrefix, node.Parent.Value.ToString())) as RectTransform
//                 : root;
//
//             if (parent == null)
//                 continue;
//
//             int index = nextIndexByParent.TryGetValue(parent, out int current)
//                 ? current
//                 : 0;
//
//             if (rt.parent == parent)
//                 rt.SetSiblingIndex(index);
//
//             nextIndexByParent[parent] = index + 1;
//         }
//     }
//
//     private Transform FindByName(Transform root, string name)
//     {
//         if (root == null)
//             return null;
//
//         if (root.name == name)
//             return root;
//
//         for (int i = 0; i < root.childCount; i++)
//         {
//             Transform found = FindByName(root.GetChild(i), name);
//
//             if (found != null)
//                 return found;
//         }
//
//         return null;
//     }
//
//     private void PrefixAllChildren(Transform root, string rolePrefix)
//     {
//         if (root == null || string.IsNullOrEmpty(rolePrefix))
//             return;
//
//         void Walk(Transform t)
//         {
//             t.name = WithRole(rolePrefix, t.name);
//
//             for (int i = 0; i < t.childCount; i++)
//                 Walk(t.GetChild(i));
//         }
//
//         Walk(root);
//     }
//
//     private static void StretchFull(RectTransform rt)
//     {
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.pivot = new Vector2(0.5f, 0.5f);
//         rt.offsetMin = Vector2.zero;
//         rt.offsetMax = Vector2.zero;
//         rt.localScale = Vector3.one;
//         rt.localRotation = Quaternion.identity;
//     }
//
//     private string WithRole(string rolePrefix, string baseName)
//     {
//         if (string.IsNullOrEmpty(rolePrefix))
//             return baseName;
//
//         if (baseName.StartsWith(rolePrefix, StringComparison.Ordinal))
//             return baseName;
//
//         return $"{rolePrefix}{baseName}";
//     }
// }