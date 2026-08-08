using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Motion",
    "Fade Out",
    Order = -815
)]
public class FadeOutCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.RigRoot;

    [Tooltip("페이드 시간(초). 0 이하이면 즉시 스냅합니다.")]
    public float duration = 0.38f;

    public Ease ease = Ease.OutCubic;
}

public sealed class FadeOutCommandCharR : CanvasFadeCommandBase
{
    private readonly FadeOutCommandSpecCharR _spec;

    public FadeOutCommandCharR(FadeOutCommandSpecCharR spec)
    {
        _spec = spec;
    }

    public override bool WaitForCompletion => _spec.wait;

    protected override float TweenDuration => _spec.duration;
    protected override Ease FadeEase => _spec.ease;

    // 목표 alpha의 원천은 코어 리덕션이다 — 트윈 종점과 정지 프레임 폴드가 같은 값을 본다.
    protected override float TargetAlpha => Ked.Presentation.Core.FadeOutReduction.TargetAlpha;

    protected override bool InteractableAfterCommit => false;

    protected override RectTransform ResolveFadeRect(CommandRunScope scope)
        => CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey)?.GetRect(_spec.target);
}
