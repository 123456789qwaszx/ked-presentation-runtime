using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Dip InOut", Order = -735)]
public sealed class DipInOutCommandSpecCharR : CommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Move")]
    public CharRDirection dir = CharRDirection.Down;

    [Tooltip("How far to dip (px).")]
    public float distance = 24f;

    [Tooltip("Total duration for enter + tiny hold + return. <=0 => snap.")]
    public float duration = 0.4f;

    [Tooltip("Base ease used as a hint. Enter will use an Out-ish ease, return will use an In-ish ease.")]
    public Ease ease = Ease.InCubic;
    
    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class DipInOutCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly DipInOutCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _restPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public DipInOutCommandCharR(DipInOutCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.
        
        _canCommitFinalState = true;

        float total = _spec.duration;
        float dist = _spec.distance;

        if (total <= 0f || Mathf.Approximately(dist, 0f))
        {
            _rect.anchoredPosition = _restPos;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector2 rest = _restPos;
        Vector2 dipped = rest + GetOffset(_spec.dir, dist);

        float tEnter = total * 0.32f;
        float tHold = total * 0.24f;
        float tReturn = Mathf.Max(0.0001f, total - tEnter - tHold);

        float holdStart = tEnter;
        float returnStart = tEnter + tHold;

        Ease enterEase = ToOutEase(_spec.ease);
        Ease returnEase = ToInEase(_spec.ease);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;
                    
                    if (t <= holdStart)
                    {
                        float localT = tEnter <= 0.0001f ? 1f : t / tEnter;
                        float e = DOVirtual.EasedValue(0f, 1f, localT, enterEase);
                        _rect.anchoredPosition = Vector2.LerpUnclamped(rest, dipped, e);
                        return;
                    }

                    if (t <= returnStart)
                    {
                        _rect.anchoredPosition = dipped;
                        return;
                    }

                    float localReturnT = tReturn <= 0.0001f ? 1f : (t - returnStart) / tReturn;
                    float eReturn = DOVirtual.EasedValue(0f, 1f, localReturnT, returnEase);
                    _rect.anchoredPosition = Vector2.LerpUnclamped(dipped, rest, eReturn);
                },
                total,
                total
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                _rect.anchoredPosition = rest;
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
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

        _rect.anchoredPosition = _restPos;
        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
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
        _rect.anchoredPosition = _restPos;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        _rect = rig.GetRect(_spec.target);
        _restPos = _rect.anchoredPosition;
    }

    private static Vector2 GetOffset(CharRDirection dir, float distance) => dir switch
    {
        CharRDirection.Right => new Vector2(+distance, 0f),
        CharRDirection.Up => new Vector2(0f, +distance),
        CharRDirection.Down => new Vector2(0f, -distance),
        _ => new Vector2(-distance, 0f),
    };

    private static Ease ToOutEase(Ease baseEase) => baseEase switch
    {
        Ease.InQuad => Ease.OutQuad,
        Ease.InCubic => Ease.OutCubic,
        Ease.InQuart => Ease.OutQuart,
        Ease.InQuint => Ease.OutQuint,
        Ease.InSine => Ease.OutSine,
        Ease.InExpo => Ease.OutExpo,
        Ease.InCirc => Ease.OutCirc,
        Ease.InBack => Ease.OutBack,
        Ease.InOutQuad => Ease.OutQuad,
        Ease.InOutCubic => Ease.OutCubic,
        Ease.InOutQuart => Ease.OutQuart,
        Ease.InOutQuint => Ease.OutQuint,
        Ease.InOutSine => Ease.OutSine,
        Ease.InOutExpo => Ease.OutExpo,
        Ease.InOutCirc => Ease.OutCirc,
        Ease.InOutBack => Ease.OutBack,
        _ => baseEase,
    };

    private static Ease ToInEase(Ease baseEase) => baseEase switch
    {
        Ease.OutQuad => Ease.InQuad,
        Ease.OutCubic => Ease.InCubic,
        Ease.OutQuart => Ease.InQuart,
        Ease.OutQuint => Ease.InQuint,
        Ease.OutSine => Ease.InSine,
        Ease.OutExpo => Ease.InExpo,
        Ease.OutCirc => Ease.InCirc,
        Ease.OutBack => Ease.InBack,
        Ease.InOutQuad => Ease.InQuad,
        Ease.InOutCubic => Ease.InCubic,
        Ease.InOutQuart => Ease.InQuart,
        Ease.InOutQuint => Ease.InQuint,
        Ease.InOutSine => Ease.InSine,
        Ease.InOutExpo => Ease.InExpo,
        Ease.InOutCirc => Ease.InCirc,
        Ease.InOutBack => Ease.InBack,
        _ => baseEase,
    };
}