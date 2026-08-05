using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Anchor",
    Order = -930,
    Sets = new[]
    {
        CommandMenuSets.SetupChar,
        CommandMenuSets.SetupEmotion
    },
    SetOrder = -930)]
public sealed class SetAnchorCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target (Anchor only)")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_VisualOffset;

    [Header("Reset")]
    [Tooltip("체크하면 Anchor 설정 후 CharSlot_Track / Move / X / Y / Rotation / Scale 축을 기본값으로 초기화합니다.")]
    public bool resetSlotPos = true;

    [Tooltip("체크하면 Anchor 설정 후 CharacterPortrait_Track / Move / X / Y / Rotation / SwayPivot / Shake / ActingScale 축을 기본값으로 초기화합니다.")]
    public bool resetCharacterPos = true;
}

public sealed class SetAnchorCommandCharR : CommandBase
{
    private readonly SetAnchorCommandSpecCharR _spec;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;

    private bool _resolveAttempted;

    public SetAnchorCommandCharR(
        SetAnchorCommandSpecCharR spec,
        RoleAnchorTuningDBSO roleTuningDb)
    {
        _spec = spec;
        _roleTuningDb = roleTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            yield break;

        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!TryResolveRefs(scope))
            return;

        Apply(scope);
    }

    private bool TryResolveRefs(CommandRunScope scope)
    {
        if (_resolveAttempted)
            return _rigRefs != null && _rect != null;

        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
            scope,
            _spec.slotKey);

        if (_rigRefs == null)
            return false;

        _rect = _rigRefs.GetRect(_spec.target);

        return _rect != null;
    }

    private void Apply(CommandRunScope scope)
    {
        // SetAnchor는 즉시 확정 command다.
        // 이전 PlaceTo tween/ledger가 남아 있으면 새 anchor 값과 싸우므로 먼저 정리.
        KillTweenAndClearPlacementTarget(_rect);

        // 리셋 대상 목록 — 코어 클레임 순서(pos → euler → scale)와 같은 순서로 만든다.
        List<RectTransform> resetPositionRects = new();
        List<RectTransform> resetEulerRects = new();
        List<RectTransform> resetScaleRects = new();

        if (_spec.resetSlotPos)
        {
            AddIfNotNull(resetPositionRects, _rigRefs.CharSlot_Track);
            AddIfNotNull(resetPositionRects, _rigRefs.CharSlot_Track_X);
            AddIfNotNull(resetPositionRects, _rigRefs.CharSlot_Track_Y);

            AddIfNotNull(resetEulerRects, _rigRefs.CharSlot_Rotation);

            AddIfNotNull(resetScaleRects, _rigRefs.CharSlot_Scale);
        }

        if (_spec.resetCharacterPos)
        {
            AddIfNotNull(resetPositionRects, _rigRefs.CharacterPortrait_Track);
            AddIfNotNull(resetPositionRects, _rigRefs.CharacterPortrait_Track_Move);
            AddIfNotNull(resetPositionRects, _rigRefs.CharacterPortrait_Track_Move_X);
            AddIfNotNull(resetPositionRects, _rigRefs.CharacterPortrait_Track_Move_Y);
            AddIfNotNull(resetPositionRects, _rigRefs.CharacterPortrait_SwayPivot);
            AddIfNotNull(resetPositionRects, _rigRefs.CharacterPortrait_Shake);

            AddIfNotNull(resetEulerRects, _rigRefs.CharacterPortrait_Rotation);
            AddIfNotNull(resetEulerRects, _rigRefs.CharacterPortrait_SwayPivot);
            AddIfNotNull(resetEulerRects, _rigRefs.CharacterPortrait_Shake);

            AddIfNotNull(resetScaleRects, _rigRefs.CharacterPortrait_SwayPivot);
            AddIfNotNull(resetScaleRects, _rigRefs.CharacterPortrait_Shake);
            AddIfNotNull(resetScaleRects, _rigRefs.CharacterPortrait_ActingScale);
            AddIfNotNull(resetScaleRects, _rigRefs.CharacterPortrait_ActingScale_X);
            AddIfNotNull(resetScaleRects, _rigRefs.CharacterPortrait_ActingScale_Y);
        }

        // 종전처럼 리셋 대상마다 트윈 정리 + 예약 해제.
        KillAndClearAll(resetPositionRects);
        KillAndClearAll(resetEulerRects);
        KillAndClearAll(resetScaleRects);

        // 튜닝 조회는 호스트(DBSO는 유니티 SO다). 값 규약(하한 클램프 포함)은 코어가 안다.
        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, _spec.slotKey);

        Ked.Presentation.Core.SetAnchorReduction.RoleAnchorTuning tuning =
            Ked.Presentation.Core.SetAnchorReduction.RoleAnchorTuning.Default;

        if (_roleTuningDb != null && _roleTuningDb.TryGet(tuningKey, out var entry))
        {
            tuning = new Ked.Presentation.Core.SetAnchorReduction.RoleAnchorTuning(
                new Ked.Presentation.Core.Vec2(entry.offset.x, entry.offset.y),
                entry.visualScale);
        }

        // "스펙 → 목표 상태" 변환은 코어 리덕션이 한다 (U13-b-4 경계).
        Ked.Presentation.Core.StageNodeClaim[] claims =
            Ked.Presentation.Core.SetAnchorReduction.Reduce(
                _rect.name,
                tuning,
                KeysOf(resetPositionRects),
                KeysOf(resetEulerRects),
                KeysOf(resetScaleRects));

        // zip 적용 — 클레임 순서 = [pos 리셋…, euler 리셋…, scale 리셋…, 앵커 위치, 앵커 스케일].
        int claimIndex = 0;

        foreach (RectTransform rect in resetPositionRects)
            ApplyClaim(rect, claims[claimIndex++]);

        foreach (RectTransform rect in resetEulerRects)
            ApplyClaim(rect, claims[claimIndex++]);

        foreach (RectTransform rect in resetScaleRects)
            ApplyClaim(rect, claims[claimIndex++]);

        ApplyClaim(_rect, claims[claimIndex++]);
        ApplyClaim(_rect, claims[claimIndex]);

        // 즉시 적용이므로 Publish하지 않는다.
        // live transform이 이미 settled target.
        ClearPlacementTarget(_rect);
    }

    private static void ApplyClaim(RectTransform rect, in Ked.Presentation.Core.StageNodeClaim claim)
    {
        switch (claim.Kind)
        {
            case Ked.Presentation.Core.StageNodeClaimKind.AnchoredPosition:
                rect.anchoredPosition = new Vector2(claim.Value.X, claim.Value.Y);
                break;

            case Ked.Presentation.Core.StageNodeClaimKind.LocalScaleXY:
                // z 보존 규약 — 리그 노드의 z는 항상 1이라 종전(1f 대입)과 결과가 같다.
                rect.localScale = new Vector3(claim.Value.X, claim.Value.Y, rect.localScale.z);
                break;

            case Ked.Presentation.Core.StageNodeClaimKind.LocalEulerAngles:
                rect.localEulerAngles = new Vector3(claim.Value.X, claim.Value.Y, claim.Value.Z);
                break;

            default:
                throw new InvalidOperationException($"SetAnchor가 다룰 수 없는 클레임: {claim.Kind}");
        }
    }

    private static void AddIfNotNull(List<RectTransform> list, RectTransform rect)
    {
        if (rect != null)
            list.Add(rect);
    }

    private static List<string> KeysOf(List<RectTransform> rects)
    {
        List<string> keys = new(rects.Count);

        for (int i = 0; i < rects.Count; i++)
            keys.Add(rects[i].name);

        return keys;
    }

    private void KillAndClearAll(List<RectTransform> rects)
    {
        for (int i = 0; i < rects.Count; i++)
            KillTweenAndClearPlacementTarget(rects[i]);
    }

    private void KillTweenAndClearPlacementTarget(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.DOKill(true);

        ClearPlacementTarget(rect);
    }

    private void ClearPlacementTarget(RectTransform rect)
    {
        if (_rigRefs == null || rect == null)
            return;

        _rigRefs.PlacementTargets.Clear(rect);
    }
}