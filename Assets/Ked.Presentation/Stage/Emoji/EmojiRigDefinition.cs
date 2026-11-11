using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────
// 이모지 리그 계층의 유일한 소유자. 프리팹이 없다 — EmojiRigBuilder가 이 배열로 세운다.
//
// 이모지 리그는 **스프라이트 하나의 뼈대**다(EmojiRig_Place › EmojiRig_Image, 두 단).
// 이모지는 그 리그 여러 개를 Emoji_Offset 밑에 묶은 것이고, 캐릭터를 모른다 —
// 설 자리(Emoji_Locator)만 받는다.
//
//   CharSlot_Scale               (캐릭터 리그 — 이 스키마의 것이 아니다)
//    └ Emoji_Locator             슬롯에 하나.   캐릭터 + 바리에이션이 정하는 RootSpace
//       └ Emoji_Offset           이모지에 하나. 이모지별 오프셋 + 이모지 전체를 움직이는 키  [CanvasGroup]
//          └ EmojiRig_Place      리그마다.      기본 자리 — 키를 받지 않는다
//             └ EmojiRig_Image   리그마다.      스프라이트를 들고 키를 받는다
//
// 캐릭터 리그와 달리 축을 노드로 쪼개지 않는다 — 커맨드마다 도는 트윈이 아니라
// 타임라인 하나가 움직이므로 한 노드가 채널을 전부 받아도 서로 덮지 않는다.
// 대신 **값을 든 노드와 키를 받는 노드를 나눈다**(Place / Image).
//
// 고치기 전에 읽을 것: Documentation~/emoji-rig.md, docs/emoji-rig.md
// ─────────────────────────────────────────────────────────────────────
public static class EmojiRigSchema
{
    /// <summary>덤프(rig-schemas.json)의 rigKind.</summary>
    public const string RigKind = "emoji";

    public enum Refs
    {
        Emoji_Locator,
        Emoji_Offset,
        EmojiRig_Place,
        EmojiRig_Image,
    }

    /// <summary>이 노드가 몇 번 서는가. 틀은 하나인데 서는 수가 노드마다 다르다.</summary>
    public enum Repeat
    {
        /// <summary>슬롯에 하나.</summary>
        PerSlot,

        /// <summary>이모지에 하나.</summary>
        PerEmoji,

        /// <summary>이모지 안의 리그마다 하나 — 리그의 수와 이름은 이모지의 구성(툴)이 정한다.</summary>
        PerRig,
    }

    public sealed class NodeDef
    {
        public Refs  Id;
        public Refs? Parent;
        public Repeat Repeat;

        /// <summary>타임라인의 키를 받는 노드인가. 받지 않는 노드는 값만 든다.</summary>
        public bool ReceivesKeys;

        public bool NeedsImage;
        public bool NeedsCanvasGroup;
        public float InitialCanvasGroupAlpha = 1f;

        /// <summary>부모 rect 안에서 붙는 점(anchorMin = anchorMax). 이 리그의 노드는 전부 점에 붙는다 — 늘어나지 않는다.</summary>
        public Vector2 Anchor = new(0.5f, 0.5f);

        /// <summary>노드의 크기. 그림 노드만 크기가 있다(스프라이트를 넣을 때 원본 픽셀로 바뀐다).</summary>
        public Vector2 Size = Vector2.zero;
    }

    /// <summary>스프라이트를 넣기 전 그림 노드의 크기. 넣으면 스프라이트의 원본 픽셀이 된다.</summary>
    public static readonly Vector2 DefaultImageSize = new(100f, 100f);

    public static readonly NodeDef[] Nodes =
    {
        // 슬롯의 CharSlot_Scale은 무대 크기로 늘어난 rect이고 피벗이 발(아래 가운데)이다.
        // 로케이터는 그 발에 붙는다 — RootSpace 값은 발에서 잰다.
        new()
        {
            Id = Refs.Emoji_Locator, Parent = null, Repeat = Repeat.PerSlot,
            Anchor = new Vector2(0.5f, 0f),
        },

        new()
        {
            Id = Refs.Emoji_Offset, Parent = Refs.Emoji_Locator, Repeat = Repeat.PerEmoji,
            ReceivesKeys = true,

            // 이모지 전체의 불투명도(함께 흐려진다) — 리그마다 따로 잡지 않아도 되게.
            NeedsCanvasGroup = true,
        },

        new()
        {
            Id = Refs.EmojiRig_Place, Parent = Refs.Emoji_Offset, Repeat = Repeat.PerRig,
        },

        new()
        {
            Id = Refs.EmojiRig_Image, Parent = Refs.EmojiRig_Place, Repeat = Repeat.PerRig,
            ReceivesKeys = true,
            NeedsImage = true,
            NeedsCanvasGroup = true,
            Size = DefaultImageSize,
        },
    };

    /// <summary>스키마 노드 id 전부(순서 그대로).</summary>
    public static string[] NodeIds() => Array.ConvertAll(Nodes, n => n.Id.ToString());

    public static NodeDef Get(Refs id)
    {
        foreach (NodeDef node in Nodes)
        {
            if (node.Id == id)
                return node;
        }

        throw new ArgumentOutOfRangeException(nameof(id), id, "EmojiRigSchema.Nodes에 없는 노드다.");
    }
}

/// <summary>세운 리그 하나 — 스프라이트 하나의 두 단.</summary>
public sealed class EmojiRigRefs
{
    /// <summary>이모지 안에서 이 리그를 가리키는 이름. 툴이 붙인다 — 키는 이 이름으로 리그를 찾는다.</summary>
    public string Name { get; }

    public RectTransform EmojiRig_Place;
    public RectTransform EmojiRig_Image;

    public Image Image;
    public CanvasGroup CanvasGroup;

    public EmojiRigRefs(string name) => Name = name;
}

/// <summary>세운 이모지 하나 — 오프셋과 그 밑의 리그들.</summary>
public sealed class EmojiRefs
{
    public string EmojiName { get; }

    /// <summary>이 이모지가 선 로케이터. 슬롯의 것이다 — 이모지를 부숴도 남는다.</summary>
    public RectTransform Emoji_Locator;

    public RectTransform Emoji_Offset;

    /// <summary>이모지 전체의 불투명도.</summary>
    public CanvasGroup OffsetCanvasGroup;

    private readonly List<EmojiRigRefs> _rigs = new();
    private readonly Dictionary<string, EmojiRigRefs> _rigsByName = new(StringComparer.Ordinal);

    public EmojiRefs(string emojiName) => EmojiName = emojiName;

    /// <summary>리그들 — 세운 순서(= 그려지는 순서, 뒤가 위).</summary>
    public IReadOnlyList<EmojiRigRefs> Rigs => _rigs;

    public bool TryGetRig(string rigName, out EmojiRigRefs rig)
        => _rigsByName.TryGetValue(rigName ?? "", out rig);

    internal void Add(EmojiRigRefs rig)
    {
        _rigs.Add(rig);

        // 틀의 견본은 리그에 이름이 없다 — 이름으로 찾는 표에는 싣지 않는다.
        if (!string.IsNullOrEmpty(rig.Name))
            _rigsByName.Add(rig.Name, rig);
    }
}
