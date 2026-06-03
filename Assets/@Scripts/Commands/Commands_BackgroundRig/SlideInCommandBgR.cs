using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Background Rig Motion", "Slide In", Order = -771)]
public sealed class SlideInCommandSpecBgR : BackgroundRigCommandSpecBase
{
    [Header("Target")]
    public BackgroundRigTarget target = BackgroundRigTarget.Background_Track;

    [Header("Slide")]
    public CharRigDirection direction = CharRigDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("(overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class SlideInCommandBgR : CommandBase
{
    private readonly SlideInCommandSpecBgR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _destPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SlideInCommandBgR(SlideInCommandSpecBgR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            yield break;

        if (_spec.killTween)
            _rect.DOKill(true);

        _canCommitFinalState = true;

        if (scope.IsRollbackSeeking)
        {
            _rect.anchoredPosition = _destPos;
            ClearRuntimeRefs();
            yield break;
        }

        Vector2 dest = _destPos;
        Vector2 fromDir = GetDir(_spec.direction);
        Vector2 start = dest + fromDir * _spec.distance;

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = dest;
            ClearRuntimeRefs();
            yield break;
        }

        Vector2 slideDir = dest - start;
        slideDir = slideDir.sqrMagnitude > 0f ? slideDir.normalized : -fromDir;

        _rect.anchoredPosition = start;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);
                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);
                    float bump = JuicyBumpEnd(e);
                    Vector2 offset = slideDir * (_spec.punch * bump);

                    _rect.anchoredPosition = basePos + offset;
                },
                1f,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = dest;
                ClearRuntimeRefs();
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_rect == null)
            return;

        _rect.anchoredPosition = _destPos;
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.anchoredPosition = _destPos;

        ClearRuntimeRefs();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        BackgroundRigRefs rigRefs =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, _spec.rigKey);

        _rect = rigRefs.GetRect(_spec.target);

        if (_rect != null)
            _destPos = _rect.anchoredPosition;
    }

    private void ClearRuntimeRefs()
    {
        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private static Vector2 GetDir(CharRigDirection from)
    {
        return from switch
        {
            CharRigDirection.Right => new Vector2(+1f, 0f),
            CharRigDirection.Up => new Vector2(0f, +1f),
            CharRigDirection.Down => new Vector2(0f, -1f),
            _ => new Vector2(-1f, 0f),
        };
    }

    private static float JuicyBumpEnd(float e)
    {
        e = Mathf.Clamp01(e);
        return Mathf.Sin(Mathf.PI * e) * (e * e);
    }
}