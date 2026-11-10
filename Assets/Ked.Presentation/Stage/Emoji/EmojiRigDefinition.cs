using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────
// 이모지 리그 계층의 유일한 소유자(docs/PLAN_emoji.md §3.1). 캐릭터 리그와 같은 방식이다 —
// 프리팹이 아니라 이 배열이 진실이고, EmojiRigBuilder가 이것으로 세우고 검사한다.
// 모션 툴은 이 계층을 ExportedTuning/rig-schemas.json(rigKind "emoji")으로 받는다.
//
//   Emoji_Anchor            자리 — 런타임이 뜰 때 한 번 놓는다. 모션은 건드리지 않는다. 불투명도(CanvasGroup)
//     Emoji_Body            묶음 전체의 위치 · 회전 · 배율 — 모션
//       Emoji_PartN         조각 — 모션
//         Emoji_PartN_Image 조각 그림(Image) — 놓임(정의의 x · y · scale · rotation). 모션은 건드리지 않는다
//
// · 한 노드가 위치 · 회전 · 배율을 모두 받는다. 캐릭터 리그의 "한 노드 = 한 축"은 여러 커맨드가
//   같은 인물을 나눠 잡기 때문에 있는 규약이고, 이모지 리그는 재생기 하나만 잡는다.
// · 놓임과 움직임은 다른 노드다 — 같은 노드면 모션의 첫 키가 놓임을 지운다.
// · 전부 가운데 앵커 · 가운데 피벗이다. 이모지는 제 중심에서 튀어나오고 줄어든다.
// ─────────────────────────────────────────────────────────────────────
public static class EmojiRigSchema
{
    /// <summary>rig-schemas.json과 motions.json이 쓰는 리그 이름.</summary>
    public const string RigKind = "emoji";

    /// <summary>조각 칸 수. 리그 종류 하나에 스키마 하나라 고정이다 — 바꾸면 덤프를 다시 내보낸다.</summary>
    public const int PartCount = 6;

    /// <summary>조각 노드의 한 변(px). 그림 노드는 그림이 꽂힐 때 원본 픽셀 크기 × 정의의 배율로 다시 잡힌다.</summary>
    public const float PartSize = 160f;

    public enum Refs
    {
        Emoji_Anchor,
        Emoji_Body,

        Emoji_Part1,
        Emoji_Part1_Image,
        Emoji_Part2,
        Emoji_Part2_Image,
        Emoji_Part3,
        Emoji_Part3_Image,
        Emoji_Part4,
        Emoji_Part4_Image,
        Emoji_Part5,
        Emoji_Part5_Image,
        Emoji_Part6,
        Emoji_Part6_Image,
    }

    public sealed class NodeDef
    {
        public Refs  Id;
        public Refs? Parent;

        /// <summary>고정 크기(px). 0이면 크기 없는 축 노드다.</summary>
        public float Size;

        public bool  NeedsImage;
        public bool  NeedsCanvasGroup;
        public float InitialCanvasGroupAlpha = 1f;

        /// <summary>모션 채널이 겨눌 수 있는가. 자리(Anchor)와 그림(_Image)은 런타임이 놓는다.</summary>
        public bool  AcceptsMotion;
    }

    public static readonly NodeDef[] Nodes = BuildNodes();

    /// <summary>스키마 노드 id 전부(순서 그대로).</summary>
    public static string[] NodeIds() => Array.ConvertAll(Nodes, n => n.Id.ToString());

    /// <summary>모션 채널이 겨눌 수 있는 노드 id — 모션 로더가 emoji 클립의 채널 노드를 이걸로 검증한다.</summary>
    public static string[] MotionNodeIds()
    {
        List<string> ids = new(Nodes.Length);

        foreach (NodeDef node in Nodes)
        {
            if (node.AcceptsMotion)
                ids.Add(node.Id.ToString());
        }

        return ids.ToArray();
    }

    /// <summary>조각 노드(1부터).</summary>
    public static Refs PartOf(int number) => ParseRef($"Emoji_Part{number}");

    /// <summary>조각의 그림 노드(1부터).</summary>
    public static Refs PartImageOf(int number) => ParseRef($"Emoji_Part{number}_Image");

    private static Refs ParseRef(string name) => (Refs)Enum.Parse(typeof(Refs), name);

    private static NodeDef[] BuildNodes()
    {
        List<NodeDef> nodes = new()
        {
            // 뜨기 전에는 안 보인다 — 불투명도 0으로 태어난다.
            new() { Id = Refs.Emoji_Anchor, Parent = null, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
            new() { Id = Refs.Emoji_Body, Parent = Refs.Emoji_Anchor, AcceptsMotion = true },
        };

        // 배열 순서가 같은 부모 밑의 그리는 순서다 — 뒤의 조각이 위에 그려진다.
        for (int number = 1; number <= PartCount; number++)
        {
            nodes.Add(new() { Id = PartOf(number), Parent = Refs.Emoji_Body, Size = PartSize, AcceptsMotion = true });
            nodes.Add(new() { Id = PartImageOf(number), Parent = PartOf(number), Size = PartSize, NeedsImage = true });
        }

        return nodes.ToArray();
    }
}

public sealed class EmojiRigRefs
{
    private readonly Dictionary<string, RectTransform> _byNodeId = new(StringComparer.Ordinal);

    public RectTransform RigRoot { get; }

    public EmojiRigRefs(RectTransform rigRoot) => RigRoot = rigRoot;

    public RectTransform Emoji_Anchor;
    public CanvasGroup   Emoji_Anchor_CanvasGroup;
    public RectTransform Emoji_Body;

    /// <summary>조각 노드 — [0]이 Emoji_Part1.</summary>
    public readonly RectTransform[] Parts = new RectTransform[EmojiRigSchema.PartCount];

    /// <summary>조각의 그림 — [0]이 Emoji_Part1_Image.</summary>
    public readonly Image[] PartImages = new Image[EmojiRigSchema.PartCount];

    /// <summary>스키마 노드 id(접두사 없는 이름)로 찾는다 — 리그를 모르는 모션 재생기가 쓰는 조회다.</summary>
    public bool TryGetRect(string nodeId, out RectTransform rect)
        => _byNodeId.TryGetValue(nodeId ?? "", out rect);

    internal void Register(EmojiRigSchema.Refs id, RectTransform rect) => _byNodeId[id.ToString()] = rect;
}
