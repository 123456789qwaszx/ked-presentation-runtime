using System;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────
// 리그 계층의 유일한 소유자. 프리팹이 아니라 이 배열이 진실이다 —
// CharacterRigBuilder가 프리팹을 검사해서 여기와 다르면 부수고 다시 세운다.
//
// 아래 Nodes의 **순서는 취향이 아니라 규약이다.** 한 노드 = 한 축이고,
// 자리가 그 노드의 뜻을 정한다. 리그는 세 덩어리다(docs/PLAN_rig-layers.md):
//   · 포커스(CharFocus_*) — 대화할 때만 서는 자리. 실제 무대로는 표현하기 힘든 작가 허용의 공간이라
//     이동 · 회전 · 배율을 모두 갖고, 쓰고 나면 0으로 되돌린다. 이동은 맨 위(무대 값 그대로),
//     회전 · 배율은 슬롯의 움직임 아래(중심이 그 인물의 발밑)다.
//   · 슬롯(CharSlot_*) — 무대 위의 본래 자리(Track — place)와 그 위의 움직임(Move_X · Move_Y —
//     left · right · up · down, docs/PLAN_move-axes.md). 가로와 세로를 따로 걸 수 있게 노드가 둘이다.
//   · 초상(CharacterPortrait_*) — 그 사람의 크기(깊이)와 배역 앵커, 연기, 그림.
//     깊이는 초상만 곱한다 — 슬롯의 이동은 어느 크기에서나 화면 값 그대로다.
//
// 한 노드에는 한 번에 트윈 하나다. 겹쳐야 하는 움직임은 다른 노드에 걸거나 클립 하나로 짠다.
//
// 순서를 바꾸거나 노드를 합치면 컴파일도 되고 예외도 안 나지만 연출이
// 조용히 망가진다. 고치기 전에 반드시 읽을 것:
//   Documentation~/rig-axis-order.md
// ─────────────────────────────────────────────────────────────────────
public static class CharacterRigSchema
{
    public enum Refs
    {
        // Focus axis - 대화할 때만 서는 자리 (이동)
        CharFocus_Track,

        // Slot axis - 무대 위의 본래 자리
        CharSlot_Track,

        // Slot axis - 본래 자리 위의 움직임 (가로 · 세로)
        CharSlot_Move_X,
        CharSlot_Move_Y,

        // Focus axis - 발밑을 중심으로 (회전 · 배율)
        CharFocus_Rotation,
        CharFocus_Scale,

        // Slot axis - 회전
        CharSlot_Rotation,
        CharSlot_SwayPivot,

        // Portrait depth axis - 그 사람의 크기
        CharacterPortrait_DepthOffset,
        CharacterPortrait_DepthScale,

        // Character casting axis - per-character defaults
        CharacterPortrait_VisualOffset,

        // Portrait acting axis
        CharacterPortrait_Track,
        CharacterPortrait_Rotation,
        CharacterPortrait_Track_Move,
        CharacterPortrait_SwayPivot,
        CharacterPortrait_Shake,
        CharacterPortrait_ActingScale,

        // Portrait mirror - 좌우 반전(mirror)만 쓴다
        CharacterPortrait_Mirror,

        // Portrait sprite
        CharacterPortraitSprite_Root,
        CharacterPortraitSprite_Image,

        // Portrait sprite overlay
        CharacterPortraitSpriteOverlay_Root,
        CharacterPortraitSpriteOverlay_Image,
    }

    public sealed class NodeDef
    {
        public Refs  Id;
        public Refs? Parent;

        public bool  NeedsImage;
        public bool  NeedsCanvasGroup;
        public bool  NeedsBottomPivot;

        public float InitialCanvasGroupAlpha = 1f;
    }

    /// <summary>스키마 노드 id 전부(순서 그대로). 모션 로더가 채널 노드를 이걸로 검증한다.</summary>
    public static string[] NodeIds() => Array.ConvertAll(Nodes, n => n.Id.ToString());

    public static readonly NodeDef[] Nodes =
    {
        // Focus axis - 대화할 때만 서는 자리 (이동)
        new() { Id = Refs.CharFocus_Track, Parent = null },

        // Slot axis - 무대 위의 본래 자리
        new() { Id = Refs.CharSlot_Track, Parent = Refs.CharFocus_Track },

        // Slot axis - 본래 자리 위의 움직임. 회전 · 깊이보다 위라 어느 기울기 · 크기에서나 화면 값 그대로다
        new() { Id = Refs.CharSlot_Move_X, Parent = Refs.CharSlot_Track },
        new() { Id = Refs.CharSlot_Move_Y, Parent = Refs.CharSlot_Move_X },

        // Focus axis - 발밑을 중심으로 (회전 · 배율)
        new() { Id = Refs.CharFocus_Rotation, Parent = Refs.CharSlot_Move_Y, NeedsBottomPivot = true },
        new() { Id = Refs.CharFocus_Scale, Parent = Refs.CharFocus_Rotation, NeedsBottomPivot = true },

        // Slot axis - 회전
        new() { Id = Refs.CharSlot_Rotation, Parent = Refs.CharFocus_Scale },
        new() { Id = Refs.CharSlot_SwayPivot, Parent = Refs.CharSlot_Rotation, NeedsBottomPivot = true },

        // Portrait depth axis - 그 사람의 크기. 슬롯의 이동은 곱하지 않는다
        new() { Id = Refs.CharacterPortrait_DepthOffset, Parent = Refs.CharSlot_SwayPivot },
        new() { Id = Refs.CharacterPortrait_DepthScale, Parent = Refs.CharacterPortrait_DepthOffset, NeedsBottomPivot = true },

        // Character casting axis - per-character defaults
        new() { Id = Refs.CharacterPortrait_VisualOffset, Parent = Refs.CharacterPortrait_DepthScale, NeedsBottomPivot = true},

        // Portrait acting axis
        new() { Id = Refs.CharacterPortrait_Track, Parent = Refs.CharacterPortrait_VisualOffset },
        new() { Id = Refs.CharacterPortrait_Rotation, Parent = Refs.CharacterPortrait_Track },
        new() { Id = Refs.CharacterPortrait_Track_Move, Parent = Refs.CharacterPortrait_Rotation },
        new() { Id = Refs.CharacterPortrait_SwayPivot, Parent = Refs.CharacterPortrait_Track_Move, NeedsBottomPivot = true },
        new() { Id = Refs.CharacterPortrait_Shake, Parent = Refs.CharacterPortrait_SwayPivot },
        new() { Id = Refs.CharacterPortrait_ActingScale, Parent = Refs.CharacterPortrait_Shake },

        // Portrait mirror - 배율 연기(ActingScale)와 합치지 않는다: 합치면 배율 모션이 뒤집힌 부호를 덮는다
        new() { Id = Refs.CharacterPortrait_Mirror, Parent = Refs.CharacterPortrait_ActingScale },

        // Portrait sprite
        new() { Id = Refs.CharacterPortraitSprite_Root, Parent = Refs.CharacterPortrait_Mirror, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSprite_Image, Parent = Refs.CharacterPortraitSprite_Root, NeedsImage = true },

        // Portrait sprite overlay
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Root, Parent = Refs.CharacterPortrait_Mirror, NeedsCanvasGroup = true, InitialCanvasGroupAlpha = 0f },
        new() { Id = Refs.CharacterPortraitSpriteOverlay_Image, Parent = Refs.CharacterPortraitSpriteOverlay_Root, NeedsImage = true },
    };
}

public enum CharacterRigNode
{
    RigRoot,

    // Focus axis - 대화할 때만 서는 자리 (이동)
    CharFocus_Track,

    // Slot axis - 무대 위의 본래 자리
    CharSlot_Track,

    // Slot axis - 본래 자리 위의 움직임 (가로 · 세로)
    CharSlot_Move_X,
    CharSlot_Move_Y,

    // Focus axis - 발밑을 중심으로 (회전 · 배율)
    CharFocus_Rotation,
    CharFocus_Scale,

    // Slot axis - 회전
    CharSlot_Rotation,
    CharSlot_SwayPivot,

    // Portrait depth axis - 그 사람의 크기
    CharacterPortrait_DepthOffset,
    CharacterPortrait_DepthScale,

    // Character casting axis - per-character defaults
    CharacterPortrait_VisualOffset,

    // Portrait acting axis
    CharacterPortrait_Track,
    CharacterPortrait_Rotation,
    CharacterPortrait_Track_Move,
    CharacterPortrait_SwayPivot,
    CharacterPortrait_Shake,
    CharacterPortrait_ActingScale,

    // Portrait mirror - 좌우 반전(mirror)만 쓴다
    CharacterPortrait_Mirror,

    // Portrait sprite
    CharacterPortraitSprite_Root,
    CharacterPortraitSprite_Image,

    // Portrait sprite overlay
    CharacterPortraitSpriteOverlay_Root,
    CharacterPortraitSpriteOverlay_Image,
}

public sealed class CharacterRigRefs
{
    public RectTransform RigRoot { get; private set; }
    public CharacterRigRefs(RectTransform rigRoot) => RigRoot = rigRoot;

    // Visual effect: CharacterPortraitSprite_Image에 바인딩된 runtime material 소유자.
    // SetupCharRigCommand가 생성, CharacterRigRegistry.DestroyRig가 Dispose.
    public RigVisualEffectController VisualEffect;

    public CharacterPlacementTargetLedger PlacementTargets { get; } = new();

    // 이 리그가 선 무대와 층. 리그 좌표 계산의 경계(RigSpaceRoot)는 Stage.Root이고, 리그 루트의 부모는 Layer.Content다.
    // slot(SetupCharRig)이 한 번 정한다. 리그는 무대 · 층을 옮기지 않는다 — 다른 자리에 서려면 fade_out 뒤 새 slot
    // (char_to는 2026-11-06에 걷었다. 무대마다 카메라가 다를 수 있어서 옮기는 순간 화면에서 튄다).
    public StageRigRefs Stage { get; private set; }
    public StageLayerRefs Layer { get; private set; }

    public void StandOn(StageRigRefs stage, StageLayerRefs layer)
    {
        Stage = stage;
        Layer = layer;
    }

    // 정지 프레임의 깊이 스케일. SetDepthCommandCharR가 끝값을 확정할 때 기록한다.
    // 트윈이 도는 중에도 "끝나면 어디인가"를 들고 있어야 착란원 계산이 흔들리지 않는다.
    // 열거형(CharacterDepthKey)이 아니라 스케일 값인 이유: useLevel=true 경로는 프리셋 키가 없고,
    // CoC 공식이 필요로 하는 것도 스케일이다. Far 0.68 ~ Close 1.38로 단조증가한다.
    public float SettledDepthScale = 1f;

    // Focus axis - 대화할 때만 서는 자리
    public RectTransform CharFocus_Track;
    public RectTransform CharFocus_Rotation;
    public RectTransform CharFocus_Scale;

    // Slot axis - 무대 위의 본래 자리와 그 위의 움직임
    public RectTransform CharSlot_Track;
    public RectTransform CharSlot_Move_X;
    public RectTransform CharSlot_Move_Y;
    public RectTransform CharSlot_Rotation;
    public RectTransform CharSlot_SwayPivot;

    // Portrait depth axis - 그 사람의 크기
    public RectTransform CharacterPortrait_DepthOffset;
    public RectTransform CharacterPortrait_DepthScale;

    // Character casting axis - per-character defaults
    public RectTransform CharacterPortrait_VisualOffset;

    // Portrait acting axis
    public RectTransform CharacterPortrait_Track;
    public RectTransform CharacterPortrait_Rotation;
    public RectTransform CharacterPortrait_Track_Move;
    public RectTransform CharacterPortrait_SwayPivot;
    public RectTransform CharacterPortrait_Shake;
    public RectTransform CharacterPortrait_ActingScale;

    // Portrait mirror
    public RectTransform CharacterPortrait_Mirror;

    // Portrait sprite
    public RectTransform CharacterPortraitSprite_Root;
    public Image         CharacterPortraitSprite_Image;

    // Portrait sprite overlay — face_swap이 페이드로 겹치는 위 층.
    // 정지 프레임에서는 alpha 0에 Image가 비활성이라 코어 폴드에 남지 않는다.
    public RectTransform CharacterPortraitSpriteOverlay_Root;
    public Image         CharacterPortraitSpriteOverlay_Image;
}

public static class CharacterRigRefsExtensions
{
    public static RectTransform GetRect(this CharacterRigRefs refs, CharacterRigNode node)
    {
        Component component = refs?.GetComponent(node);
        return component != null ? component.transform as RectTransform : null;
    }

    public static Image GetImage(this CharacterRigRefs refs, CharacterRigNode node)
    {
        return refs?.GetComponent(node) as Image;
    }

    private static Component GetComponent(this CharacterRigRefs refs, CharacterRigNode node)
    {
        if (refs == null)
            return null;

        return node switch
        {
            CharacterRigNode.RigRoot => refs.RigRoot,
            
            // Focus axis
            CharacterRigNode.CharFocus_Track => refs.CharFocus_Track,
            CharacterRigNode.CharFocus_Rotation => refs.CharFocus_Rotation,
            CharacterRigNode.CharFocus_Scale => refs.CharFocus_Scale,

            // Slot axis
            CharacterRigNode.CharSlot_Track => refs.CharSlot_Track,
            CharacterRigNode.CharSlot_Move_X => refs.CharSlot_Move_X,
            CharacterRigNode.CharSlot_Move_Y => refs.CharSlot_Move_Y,
            CharacterRigNode.CharSlot_Rotation => refs.CharSlot_Rotation,
            CharacterRigNode.CharSlot_SwayPivot => refs.CharSlot_SwayPivot,

            // Portrait depth axis
            CharacterRigNode.CharacterPortrait_DepthOffset => refs.CharacterPortrait_DepthOffset,
            CharacterRigNode.CharacterPortrait_DepthScale => refs.CharacterPortrait_DepthScale,

            // Character casting axis - per-character defaults
            CharacterRigNode.CharacterPortrait_VisualOffset => refs.CharacterPortrait_VisualOffset,

            // Portrait acting axis
            CharacterRigNode.CharacterPortrait_Track => refs.CharacterPortrait_Track,
            CharacterRigNode.CharacterPortrait_Rotation => refs.CharacterPortrait_Rotation,
            CharacterRigNode.CharacterPortrait_Track_Move => refs.CharacterPortrait_Track_Move,
            CharacterRigNode.CharacterPortrait_SwayPivot => refs.CharacterPortrait_SwayPivot,
            CharacterRigNode.CharacterPortrait_Shake => refs.CharacterPortrait_Shake,
            CharacterRigNode.CharacterPortrait_ActingScale => refs.CharacterPortrait_ActingScale,

            // Portrait mirror
            CharacterRigNode.CharacterPortrait_Mirror => refs.CharacterPortrait_Mirror,

            // Portrait sprite
            CharacterRigNode.CharacterPortraitSprite_Root => refs.CharacterPortraitSprite_Root,
            CharacterRigNode.CharacterPortraitSprite_Image => refs.CharacterPortraitSprite_Image,

            // Portrait sprite overlay
            CharacterRigNode.CharacterPortraitSpriteOverlay_Root => refs.CharacterPortraitSpriteOverlay_Root,
            CharacterRigNode.CharacterPortraitSpriteOverlay_Image => refs.CharacterPortraitSpriteOverlay_Image,

            _ => null
        };
    }
}