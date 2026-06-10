using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Color (Z)",
    Order = 870
)]
public class ColorToCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortraitSprite_Image;

    [Header("Color")]
    public Color color = Color.white;

    [Tooltip("체크하면 현재 알파(A)는 그대로 두고 색상(RGB)만 변경합니다.")]
    public bool keepAlpha = true;
    
    [Header("Tween")]
    [Tooltip("트윈 시간. <= 0이면 즉시 color로 스냅")]
    public float duration = 0f;

    public Ease ease = Ease.OutCubic;
}

public sealed class ColorToCommandCharR : CommandBase
{
    private readonly ColorToCommandSpecCharR _spec;

    private Image _image;
    private Color _destColor;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public ColorToCommandCharR(ColorToCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        Tween tween = _image
            .DOColor(_destColor, _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_image)
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
        else
            _image.DOKill(false);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _image = rig.GetImage(_spec.target);
    }

    private void ClaimTarget()
    {
        _image.DOKill(true);

        _destColor = _spec.color;
        if (_spec.keepAlpha)
            _destColor.a = _image.color.a;

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _image.color = _destColor;

        HasClaimedTarget = false;
    }
}