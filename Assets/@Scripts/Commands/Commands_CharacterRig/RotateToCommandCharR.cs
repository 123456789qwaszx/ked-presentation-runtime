using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Rotate (From → To)",
    Order = -180
)]
public class RotateToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharSlot_Rotation;

    [Header("Rotation (localEulerAngles)")]
    public Vector3 toEuler = Vector3.zero;

    [Header("From")]
    public bool overrideFromEuler = false;
    public Vector3 fromEuler = Vector3.zero;

    [Header("Tween")]
    public float duration = 0.4f;
    public Ease ease = Ease.OutCubic;
}

public sealed class RotateToCommandCharR : CommandBase
{
    private readonly RotateToCommandSpecCharR _spec;

    private RectTransform _rect;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public RotateToCommandCharR(RotateToCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.overrideFromEuler)
            _rect.localEulerAngles = _spec.fromEuler;

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = _rect
            .DOLocalRotate(_spec.toEuler, _spec.duration, RotateMode.Fast)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rig.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _rect.localEulerAngles = _spec.toEuler;

        HasClaimedTarget = false;
    }
}