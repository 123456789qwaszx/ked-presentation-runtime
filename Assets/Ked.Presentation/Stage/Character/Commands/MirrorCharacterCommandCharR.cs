using System;
using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public enum CharacterMirrorMode
{
    Toggle,
    Left,
    Right,
}

[Serializable]
public sealed class MirrorCharacterCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Mirror")]
    public CharacterMirrorMode mode = CharacterMirrorMode.Toggle;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_ActingScale_X;

    [Header("Tween")]
    public float duration = 0f;
    public Ease ease = Ease.OutCubic;
}

public sealed class MirrorCharacterCommandCharR : ClaimTweenCommandBase
{
    private readonly MirrorCharacterCommandSpecCharR _spec;

    private RectTransform _rect;
    private Vector3 _targetScale;
    private float _startScaleX;

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;

    private readonly PresentationStage _stage;
    private CharacterRigRefs _rig;

    public MirrorCharacterCommandCharR(PresentationStage stage, MirrorCharacterCommandSpecCharR spec)
    {
        _stage = stage;
        _spec = spec;
    }

    /// <summary>
    /// 해석과 동시에 방향을 정해 대장에 기록한다 — 대장이 곧 다음 Toggle의 입력이라
    /// 커맨드가 여러 번 불려도 방향이 한 번만 뒤집히도록 여기(1회 호출)에 둔다.
    /// </summary>
    protected override void ResolveTargets()
    {
        _rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(_stage, _spec.slotKey);

        string resolvedRigKey =
            CharacterRigTargetResolver.ResolveRigKeyByPolicy(_stage, _spec.slotKey);

        _rect = _rig.GetRect(_spec.target);

        CharacterFacing facing = ResolveTargetFacing(resolvedRigKey);

        _stage.castRegistry.SetFacing(resolvedRigKey, facing);

        Vector3 baseScale = _rect.localScale;

        _targetScale = baseScale;
        _targetScale.x = Mathf.Abs(baseScale.x) * facing.Sign();
    }

    private CharacterFacing ResolveTargetFacing(string resolvedRigKey)
    {
        switch (_spec.mode)
        {
            case CharacterMirrorMode.Left:
                return CharacterFacing.Left;

            case CharacterMirrorMode.Right:
                return CharacterFacing.Right;

            case CharacterMirrorMode.Toggle:
            default:
            {
                if (_stage.castRegistry.TryGetFacing(resolvedRigKey, out CharacterFacing current))
                    return current.Opposite();

                return CharacterFacing.Left;
            }
        }
    }

    protected override void ClaimTarget()
    {
        _rect.DOKill(true);
        _startScaleX = _rect.localScale.x;

        _rig.PlacementTargets.Claim(_rect, StageNodeClaim.LocalScaleXY(_rect.name, new Vec2(_targetScale.x, _targetScale.y)), Releases);
    }

    protected override Tween CreateTween(float duration)
        => _rect
            .DOScale(_targetScale, duration)
            .SetEase(_spec.ease)
            .SetTarget(_rect);

    protected override void OnCommitFinalState()
        => _rect.localScale = _targetScale;

    // 반전은 x 배율의 부호를 뒤집는 것이라 x만 잰다(y는 그대로다).
    // 대본의 mirror는 시간이 0이라 트윈이 없지만, 시간을 주면 스텝 경계에서 남은 만큼 가속한다.
    protected override float MeasureRemainingRatio()
        => RemainingRatio(
            Mathf.Abs(_targetScale.x - _startScaleX),
            Mathf.Abs(_targetScale.x - _rect.localScale.x));
}
