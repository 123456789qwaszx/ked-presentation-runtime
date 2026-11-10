using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// 이모지 리그를 스키마(<see cref="EmojiRigSchema"/>)로 세우고 검사한다. 프리팹은 없다 — 스키마 베이크가 곧 런타임 경로다.
/// 노드 이름에 접두사(<c>namePrefix</c>)를 붙일 수 있다: 캐릭터 슬롯 밑에 붙을 때 이름이 전역에서 하나뿐이게 한다.
/// </summary>
public sealed class EmojiRigBuilder
{
    public RectTransform BuildRigRoot(string namePrefix = "", string rigRootName = "EmojiRig")
    {
        GameObject rootGo = new(WithPrefix(namePrefix, rigRootName), typeof(RectTransform));
        RectTransform rigRoot = (RectTransform)rootGo.transform;

        Center(rigRoot, 0f);
        EnsureGraph(rigRoot, namePrefix);

        return rigRoot;
    }

    public void BindRefsFromRoot(RectTransform rigRoot, string namePrefix, out EmojiRigRefs refs)
    {
        if (rigRoot == null)
        {
            Debug.LogWarning("[EmojiRigBuilder] rigRoot is null.");
            refs = null;
            return;
        }

        Dictionary<EmojiRigSchema.Refs, RectTransform> map = CollectRefMap(rigRoot, namePrefix);

        if (!IsValidGraphMap(rigRoot, map))
        {
            Debug.LogWarning(
                $"[EmojiRigBuilder] Invalid rig graph. Rebuilding from EmojiRigSchema. " +
                $"rigRoot='{rigRoot.name}', namePrefix='{namePrefix}'.",
                rigRoot);

            // 파괴 전에 떼어 낸다 — Destroy는 프레임 끝까지 계층에 남아 FindByName이 삭제 예정 노드를 다시 쓴다
            // (캐릭터 리그가 치른 값: rig-axis-order.md §1.2).
            for (int i = rigRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = rigRoot.GetChild(i);
                child.SetParent(null, false);

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            EnsureGraph(rigRoot, namePrefix);
            map = CollectRefMap(rigRoot, namePrefix);
        }

        refs = BuildRefs(rigRoot, map);
    }

    private static bool IsValidGraphMap(RectTransform rigRoot, Dictionary<EmojiRigSchema.Refs, RectTransform> map)
    {
        foreach (EmojiRigSchema.NodeDef node in EmojiRigSchema.Nodes)
        {
            if (!map.TryGetValue(node.Id, out RectTransform rect))
                return false;

            Transform expectedParent;

            if (node.Parent.HasValue)
            {
                if (!map.TryGetValue(node.Parent.Value, out RectTransform parentRect))
                    return false;

                expectedParent = parentRect;
            }
            else
            {
                expectedParent = rigRoot;
            }

            if (rect.parent != expectedParent)
                return false;

            if (node.NeedsCanvasGroup && !rect.TryGetComponent(out CanvasGroup _))
                return false;

            if (node.NeedsImage && !rect.TryGetComponent(out Image _))
                return false;
        }

        return true;
    }

    private void EnsureGraph(RectTransform root, string namePrefix)
    {
        foreach (EmojiRigSchema.NodeDef node in EmojiRigSchema.Nodes)
            EnsureNode(root, namePrefix, node);
    }

    private void EnsureNode(RectTransform root, string namePrefix, EmojiRigSchema.NodeDef node)
    {
        RectTransform parent = node.Parent.HasValue
            ? FindByName(root, WithPrefix(namePrefix, node.Parent.Value.ToString())) as RectTransform
            : root;

        if (parent == null)
        {
            Debug.LogWarning(
                $"[EmojiRigBuilder] Parent not found. node='{node.Id}', parent='{node.Parent}'. Attach to root.",
                root);

            parent = root;
        }

        string name = WithPrefix(namePrefix, node.Id.ToString());
        RectTransform rect = FindByName(root, name) as RectTransform;

        if (rect == null)
        {
            GameObject go = new(name, typeof(RectTransform));
            rect = (RectTransform)go.transform;
        }

        if (rect.parent != parent)
            rect.SetParent(parent, false);

        // 스키마 순서가 그리는 순서다 — 세운 차례대로 맨 뒤(위)에 둔다.
        rect.SetAsLastSibling();
        Center(rect, node.Size);

        if (node.NeedsCanvasGroup)
        {
            if (!rect.TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = node.InitialCanvasGroupAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (node.NeedsImage)
        {
            if (!rect.TryGetComponent(out Image image))
                image = rect.gameObject.AddComponent<Image>();

            // 그림이 꽂히기 전에는 그리지 않는다 — 빈 Image는 흰 사각형이다.
            image.raycastTarget = false;
            image.enabled = image.sprite != null;
        }
    }

    private Dictionary<EmojiRigSchema.Refs, RectTransform> CollectRefMap(RectTransform rigRoot, string namePrefix)
    {
        Dictionary<EmojiRigSchema.Refs, RectTransform> map = new();

        foreach (EmojiRigSchema.Refs id in Enum.GetValues(typeof(EmojiRigSchema.Refs)))
        {
            if (FindByName(rigRoot, WithPrefix(namePrefix, id.ToString())) is RectTransform rect)
                map[id] = rect;
        }

        return map;
    }

    private static EmojiRigRefs BuildRefs(RectTransform rigRoot, Dictionary<EmojiRigSchema.Refs, RectTransform> map)
    {
        EmojiRigRefs refs = new(rigRoot);

        foreach (KeyValuePair<EmojiRigSchema.Refs, RectTransform> pair in map)
            refs.Register(pair.Key, pair.Value);

        refs.Emoji_Anchor = map[EmojiRigSchema.Refs.Emoji_Anchor];
        refs.Emoji_Anchor_CanvasGroup = refs.Emoji_Anchor.GetComponent<CanvasGroup>();
        refs.Emoji_Body = map[EmojiRigSchema.Refs.Emoji_Body];

        for (int number = 1; number <= EmojiRigSchema.PartCount; number++)
        {
            refs.Parts[number - 1] = map[EmojiRigSchema.PartOf(number)];
            refs.PartImages[number - 1] = map[EmojiRigSchema.PartImageOf(number)].GetComponent<Image>();
        }

        return refs;
    }

    private static Transform FindByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindByName(root.GetChild(i), name);

            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>가운데 앵커 · 가운데 피벗 · 고정 크기 · 항등.</summary>
    private static void Center(RectTransform rect, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static string WithPrefix(string namePrefix, string baseName)
    {
        if (string.IsNullOrEmpty(namePrefix))
            return baseName;

        if (baseName.StartsWith(namePrefix, StringComparison.Ordinal))
            return baseName;

        return $"{namePrefix}{baseName}";
    }
}
