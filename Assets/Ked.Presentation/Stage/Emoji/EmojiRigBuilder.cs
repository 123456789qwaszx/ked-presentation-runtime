using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// ─────────────────────────────────────────────────────────────────────
// EmojiRigSchema로 이모지의 계층을 세우고 부순다. 프리팹을 받지 않는다 — 계층의 주인은 스키마다.
//
// 이모지는 캐릭터를 모른다: 이 빌더가 받는 것은 "설 자리"(슬롯의 CharSlot_Scale)와 이름들뿐이다.
// 하이어라키 이름은 접두를 붙여 전역에서 하나뿐이게 한다(캐릭터 리그의 rolePrefix와 같은 까닭):
//
//   {rolePrefix}Emoji_Locator
//   {rolePrefix}{이모지}_Emoji_Offset
//   {rolePrefix}{이모지}_{리그}_EmojiRig_Place
//   {rolePrefix}{이모지}_{리그}_EmojiRig_Image
// ─────────────────────────────────────────────────────────────────────
public sealed class EmojiRigBuilder
{
    /// <summary>
    /// 슬롯의 로케이터를 내준다. 없으면 세우고, 있으면 그것을 다시 쓴다 — 슬롯에 하나다.
    /// </summary>
    /// <param name="charSlotScale">이모지가 설 자리. 캐릭터 리그의 CharSlot_Scale.</param>
    /// <param name="rolePrefix">그 슬롯의 접두(예: "harin_"). 비어도 된다.</param>
    public RectTransform EnsureLocator(RectTransform charSlotScale, string rolePrefix)
    {
        if (charSlotScale == null)
            throw new ArgumentNullException(nameof(charSlotScale));

        string locatorName = NodeName(rolePrefix, null, null, EmojiRigSchema.Refs.Emoji_Locator);

        RectTransform locator = FindDirectChild(charSlotScale, locatorName);

        if (locator == null)
            locator = CreateNode(charSlotScale, locatorName, EmojiRigSchema.Get(EmojiRigSchema.Refs.Emoji_Locator));

        // 초상보다 뒤(= 위)에 그린다.
        locator.SetAsLastSibling();

        return locator;
    }

    /// <summary>
    /// 로케이터 밑에 이모지 하나를 세운다: 오프셋 + 리그 이름마다 두 단.
    /// 같은 이름의 이모지가 이미 서 있으면 부수고 새로 세운다.
    /// </summary>
    /// <param name="rigNames">이모지 안에서 리그를 가리키는 이름들(툴이 붙인다). 순서가 그려지는 순서다 — 뒤가 위.</param>
    public EmojiRefs BuildEmoji(
        RectTransform locator,
        string rolePrefix,
        string emojiName,
        IReadOnlyList<string> rigNames)
    {
        if (locator == null)
            throw new ArgumentNullException(nameof(locator));

        ValidateNames(emojiName, rigNames);

        return Build(locator, rolePrefix, emojiName, rigNames);
    }

    /// <summary>
    /// 틀의 견본 — 이름 없는 로케이터 · 오프셋 · 리그 하나. 덤프(rig-schemas.json)와 그 대조 테스트가 쓴다.
    /// 노드 이름이 스키마의 id 그대로다.
    /// </summary>
    public EmojiRefs BuildTemplate(RectTransform parent)
    {
        RectTransform locator = EnsureLocator(parent, "");

        return Build(locator, "", null, new string[] { null });
    }

    /// <summary>이모지를 부순다. 로케이터는 슬롯의 것이라 남긴다.</summary>
    public void DestroyEmoji(EmojiRefs emoji)
    {
        if (emoji == null || emoji.Emoji_Offset == null)
            return;

        DestroyNode(emoji.Emoji_Offset);
    }

    // ── 세우기 ───────────────────────────────────────────────────────

    private EmojiRefs Build(
        RectTransform locator,
        string rolePrefix,
        string emojiName,
        IReadOnlyList<string> rigNames)
    {
        string offsetName = NodeName(rolePrefix, emojiName, null, EmojiRigSchema.Refs.Emoji_Offset);

        RectTransform existing = FindDirectChild(locator, offsetName);

        if (existing != null)
            DestroyNode(existing);

        EmojiRefs refs = new(emojiName)
        {
            Emoji_Locator = locator,
            Emoji_Offset = CreateNode(locator, offsetName, EmojiRigSchema.Get(EmojiRigSchema.Refs.Emoji_Offset)),
        };

        EmojiRigSchema.NodeDef placeDef = EmojiRigSchema.Get(EmojiRigSchema.Refs.EmojiRig_Place);
        EmojiRigSchema.NodeDef imageDef = EmojiRigSchema.Get(EmojiRigSchema.Refs.EmojiRig_Image);

        foreach (string rigName in rigNames)
        {
            RectTransform place = CreateNode(
                refs.Emoji_Offset,
                NodeName(rolePrefix, emojiName, rigName, EmojiRigSchema.Refs.EmojiRig_Place),
                placeDef);

            RectTransform image = CreateNode(
                place,
                NodeName(rolePrefix, emojiName, rigName, EmojiRigSchema.Refs.EmojiRig_Image),
                imageDef);

            refs.Add(new EmojiRigRefs(rigName)
            {
                EmojiRig_Place = place,
                EmojiRig_Image = image,
                Image = image.GetComponent<Image>(),
                CanvasGroup = image.GetComponent<CanvasGroup>(),
            });
        }

        return refs;
    }

    private static RectTransform CreateNode(RectTransform parent, string name, EmojiRigSchema.NodeDef def)
    {
        GameObject go = new(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        // 점에 붙는다 — 늘어나지 않는다. 피벗은 가운데.
        rt.anchorMin = def.Anchor;
        rt.anchorMax = def.Anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = def.Size;

        if (def.NeedsCanvasGroup)
            go.AddComponent<CanvasGroup>().alpha = def.InitialCanvasGroupAlpha;

        if (def.NeedsImage)
        {
            // 이모지는 클릭을 받지 않는다 — 뒤의 대사창 · 인물을 가리면 안 된다.
            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
        }

        return rt;
    }

    // ── 이름 ─────────────────────────────────────────────────────────

    private static void ValidateNames(string emojiName, IReadOnlyList<string> rigNames)
    {
        if (string.IsNullOrWhiteSpace(emojiName))
            throw new ArgumentException("이모지 이름이 비어 있다.", nameof(emojiName));

        if (rigNames == null || rigNames.Count == 0)
            throw new ArgumentException($"이모지 '{emojiName}'에 리그가 없다 — 리그는 하나 이상이어야 한다.", nameof(rigNames));

        HashSet<string> seen = new(StringComparer.Ordinal);

        for (int i = 0; i < rigNames.Count; i++)
        {
            string rigName = rigNames[i];

            if (string.IsNullOrWhiteSpace(rigName))
                throw new ArgumentException($"이모지 '{emojiName}'의 리그 [{i}]에 이름이 없다.", nameof(rigNames));

            if (!seen.Add(rigName))
                throw new ArgumentException($"이모지 '{emojiName}'에 리그 이름 '{rigName}'이 두 번 나온다 — 키가 리그를 이름으로 가리킨다.", nameof(rigNames));
        }
    }

    /// <summary>{rolePrefix}{이모지}_{리그}_{노드}. 빈 마디는 건너뛴다(틀의 견본은 노드 id만 남는다).</summary>
    public static string NodeName(string rolePrefix, string emojiName, string rigName, EmojiRigSchema.Refs node)
    {
        string name = node.ToString();

        if (!string.IsNullOrEmpty(rigName))
            name = $"{rigName}_{name}";

        if (!string.IsNullOrEmpty(emojiName))
            name = $"{emojiName}_{name}";

        return string.IsNullOrEmpty(rolePrefix) ? name : $"{rolePrefix}{name}";
    }

    // ── 돕는 것 ──────────────────────────────────────────────────────

    private static RectTransform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return child as RectTransform;
        }

        return null;
    }

    private static void DestroyNode(RectTransform node)
    {
        // 부수기 전에 떼어 낸다 — Destroy는 프레임 끝까지 계층에 남아, 같은 프레임에 같은 이름을
        // 다시 세우면 지워질 노드가 검색에 잡힌다(CharacterRigBuilder.EnsureValidGraphMap과 같은 까닭).
        node.SetParent(null, false);

        if (Application.isPlaying)
            Object.Destroy(node.gameObject);
        else
            Object.DestroyImmediate(node.gameObject);
    }
}
