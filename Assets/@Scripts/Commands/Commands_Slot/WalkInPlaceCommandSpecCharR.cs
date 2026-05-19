using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[Serializable]
[CommandMenuHint("Char Rig Motion", "Walk In Place", Order = -755)]
public sealed class WalkInPlaceCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Track;

    [Header("Timing")]
    [Tooltip("전체 걷기 지속 시간.")]
    public float duration = 99f;

    [Tooltip("초당 걸음 수. 2.5면 1초에 약 2.5번 통통 움직입니다.")]
    public float stepsPerSecond = 1.9f;

    [Header("Motion")]
    [Tooltip("위아래 bob 높이. 픽셀 단위.")]
    public float arcHeight = 18f;

    [Tooltip("좌우 흔들림. 0이면 좌우 움직임 없이 위아래만 움직입니다.")]
    public float sideSway = 0.3f;

    [Range(0.05f, 1f)]
    [Tooltip("각 걸음 구간 중 공중에 떠 있는 비율. 작을수록 톡톡, 클수록 둥글게 걷습니다.")]
    public float airWidth = 0.95f;

    [Header("Blend")]
    [Tooltip("시작할 때 자연스럽게 motion이 켜지는 시간.")]
    public float blendIn = 0.08f;

    [Tooltip("끝날 때 자연스럽게 원래 위치로 돌아오는 시간.")]
    public float blendOut = 0.08f;

    [Header("Options")]
    [Tooltip("체크하면 기존 위치 관련 트윈을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;
}

public sealed class WalkInPlaceCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly WalkInPlaceCommandSpecCharR _spec;

    private RectTransform _rect;
    private Tween _tween;
    private Vector2 _basePos;
    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public WalkInPlaceCommandCharR(WalkInPlaceCommandSpecCharR spec)
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
            _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        _basePos = _rect.anchoredPosition;
        _canCommitFinalState = true;

        if (_spec.duration <= 0f || _spec.stepsPerSecond <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                elapsed =>
                {
                    if (!_canCommitFinalState || _rect == null)
                        return;

                    float phase = Mathf.Repeat(elapsed * _spec.stepsPerSecond, 1f);
                    float envelope = EvaluateEnvelope(elapsed, _spec.duration, _spec.blendIn, _spec.blendOut);

                    float y = HopHeight(phase, _spec.arcHeight, _spec.airWidth);
                    float x = Mathf.Sin(phase * Mathf.PI * 2f) * _spec.sideSway;

                    Vector2 offset = new Vector2(x, y) * envelope;
                    _rect.anchoredPosition = _basePos + offset;
                },
                _spec.duration,
                _spec.duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || _rect == null)
                    return;

                CommitFinalState();
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

        CommitFinalState();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || _rect == null)
            return;

        _tween?.Kill(false);
        _rect.DOKill(false);

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.targetKey);

        _rect = rigRefs.GetRect(_spec.target);

        if (_rect != null)
            _basePos = _rect.anchoredPosition;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            _rect.anchoredPosition = _basePos;

        _canCommitFinalState = false;
        _rect = null;
        _tween = null;
    }

    private static float HopHeight(float u, float height, float airWidth)
    {
        u = Mathf.Clamp01(u);

        if (height == 0f)
            return 0f;

        airWidth = Mathf.Clamp(airWidth, 0.05f, 1f);

        float preT = (1f - airWidth) * 0.5f;
        float airT = airWidth;

        float uPreEnd = preT;
        float uAirEnd = preT + airT;

        if (u < uPreEnd || u > uAirEnd || airT <= 0f)
            return 0f;

        float a = (u - uPreEnd) / airT;
        return Mathf.Sin(Mathf.PI * a) * height;
    }

    private static float EvaluateEnvelope(float elapsed, float duration, float blendIn, float blendOut)
    {
        float inFactor = 1f;
        float outFactor = 1f;

        if (blendIn > 0f)
            inFactor = Mathf.Clamp01(elapsed / blendIn);

        if (blendOut > 0f)
            outFactor = Mathf.Clamp01((duration - elapsed) / blendOut);

        float factor = Mathf.Min(inFactor, outFactor);
        return Mathf.SmoothStep(0f, 1f, factor);
    }
}