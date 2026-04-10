using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Juicy Slide In", Order = -771)]
public sealed class JuicySlideInCommandSpecCharR : CommandSpecBase
{
    [Header("Target (Track)")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Slide")]
    public CharRDirection direction = CharRDirection.Left;
    public float distance = 480f;

    [Header("Tween")]
    public float duration = 0.55f;
    public Ease ease = Ease.OutCubic;

    [Header("Juice (overshoot that settles back)")]
    [Tooltip("0이면 일반 SlideIn에 가까워짐.")]
    public float punch = 24f;

    [Header("Wait")]
    public bool wait = false;
    
    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class JuicySlideInCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly JuicySlideInCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _destPos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public JuicySlideInCommandCharR(JuicySlideInCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.killTween)
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.
        
        _canCommitFinalState = true;

        Vector2 dest = _destPos;
        Vector2 fromDir = GetDir(_spec.direction);
        Vector2 start = dest + fromDir * _spec.distance;

        if (_spec.duration <= 0f)
        {
            _rect.anchoredPosition = dest;
            _canCommitFinalState = false;
            _rect = null;
            _tween = null;
            yield break;
        }

        Vector2 slideDir = dest - start;
        slideDir = slideDir.sqrMagnitude > 0f
            ? slideDir.normalized
            : -fromDir;

        _rect.anchoredPosition = start;

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float e = DOVirtual.EasedValue(0f, 1f, t, _spec.ease);

                    Vector2 basePos = Vector2.LerpUnclamped(start, dest, e);
                    float bump = JuicyBump_End(e);
                    Vector2 offset = slideDir * (_spec.punch * bump);

                    _rect.anchoredPosition = basePos + offset;
                },
                1f,
                _spec.duration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState)
                    return;

                _rect.anchoredPosition = dest;
                _canCommitFinalState = false;
                _rect = null;
                _tween = null;
            });

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);
        _rect.anchoredPosition = _destPos;

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
        _destPos = _rect.anchoredPosition;
    }

    private static Vector2 GetDir(CharRDirection from) => from switch
    {
        CharRDirection.Right => new Vector2(+1f, 0f),
        CharRDirection.Up => new Vector2(0f, +1f),
        CharRDirection.Down => new Vector2(0f, -1f),
        _ => new Vector2(-1f, 0f),
    };

    private static float JuicyBump_End(float e)
    {
        e = Mathf.Clamp01(e);
        return Mathf.Sin(Mathf.PI * e) * (e * e);
    }
}