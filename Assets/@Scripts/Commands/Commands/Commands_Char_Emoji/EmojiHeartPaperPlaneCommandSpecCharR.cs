using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Emoji",
    "Emoji Heart Paper Plane",
    Order = -698)]
public sealed class EmojiHeartPaperPlaneCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Emoji Identity")]
    public string emojiKey;

    [Header("Targets")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
    public CharacterRigTarget moveTarget = CharacterRigTarget.EmojiSlot00_Track_Move;
    public CharacterRigTarget scaleTarget = CharacterRigTarget.EmojiSlot00_Scale;
    public CharacterRigTarget rotationTarget = CharacterRigTarget.EmojiSlot00_Rotation;

    [Header("Flight")]
    public CharRigDirection direction = CharRigDirection.Right;
    public Vector2 startOffset = new(0f, 0f);
    public float travelDistance = 168f;
    public float endYOffset = 24f;
    public float arcHeight = 58f;
    public float controlForwardRatio = 0.46f;

    [Header("Visual")]
    public float startScale = 0.72f;
    public float cruiseScale = 1.02f;
    public float endScale = 0.82f;

    [Tooltip("하트 이미지 자체의 기본 기울기입니다.")]
    public float baseTiltDegrees = -8f;

    [Tooltip("비행 궤적의 tangent가 회전에 얼마나 반영되는지입니다.")]
    [Range(0f, 1f)]
    public float tangentTiltWeight = 0.55f;

    [Header("Fade")]
    public float fadeInPortion = 0.14f;

    [Range(0f, 1f)]
    public float fadeOutStart = 0.68f;

    [Header("Tween")]
    public float duration = 0.92f;
    public Ease ease = Ease.InOutSine;
}

public sealed class EmojiHeartPaperPlaneCommandCharR : CharacterEmojiCommandBase
{
    private readonly EmojiHeartPaperPlaneCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private CanvasGroup _rootCanvasGroup;
    private RectTransform _moveRect;
    private RectTransform _scaleRect;
    private RectTransform _rotationRect;

    private Vector2 _baseMovePos;
    private Vector3 _baseScale;
    private Quaternion _baseRotation;

    private Tween _tween;
    private CharacterEmojiMirrorContext _mirrorContext;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiHeartPaperPlaneCommandCharR(
        EmojiHeartPaperPlaneCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        _mirrorContext = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        ClaimTarget();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => 0f,
                ApplyProgress,
                1f,
                _spec.duration)
            .SetEase(_spec.ease)
            .SetUpdate(true)
            .SetTarget(_moveRect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
        {
            _mirrorContext = ResolveEmojiMirrorContext(
                scope,
                _resolver,
                _spec.slotKey,
                _spec.emojiKey);

            ClaimTarget();
        }

        CommitFinalState();
    }

    public override void RegisterStepLifetime(
        CommandRunScope scope,
        MonoBehaviour host,
        IEnumerator routine)
    {
        scope.TrackStep(
            cancel: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                CommitFinalState();
            },
            finish: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                OnStepLifetimeFinished(scope);
            });
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        RectTransform root = rigRefs.GetRect(_spec.rootTarget);
        _rootCanvasGroup = root.GetComponent<CanvasGroup>();

        if (_rootCanvasGroup == null)
            _rootCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();

        _moveRect = rigRefs.GetRect(_spec.moveTarget);
        _scaleRect = rigRefs.GetRect(_spec.scaleTarget);
        _rotationRect = rigRefs.GetRect(_spec.rotationTarget);
    }

    private void ClaimTarget()
    {
        _moveRect.DOKill(true);
        _scaleRect.DOKill(true);
        _rotationRect.DOKill(true);
        DOTween.Kill(_moveRect, false);

        _baseMovePos = _moveRect.anchoredPosition;
        _baseScale = _scaleRect.localScale;
        _baseRotation = _rotationRect.localRotation;

        _rootCanvasGroup.alpha = 0f;

        _moveRect.anchoredPosition = _baseMovePos + _mirrorContext.MirrorMotionVector(_spec.startOffset);
        _scaleRect.localScale = _baseScale * _spec.startScale;
        _rotationRect.localRotation = _baseRotation;

        HasClaimedTarget = true;
    }

    private void ApplyProgress(float u)
    {
        u = Mathf.Clamp01(u);

        CharRigDirection effectiveDirection = _mirrorContext.MirrorDirection(_spec.direction);
        float dir = effectiveDirection == CharRigDirection.Left ? -1f : 1f;
        Vector2 startOffset = _mirrorContext.MirrorMotionVector(_spec.startOffset);

        Vector2 p0 = _baseMovePos + startOffset;

        Vector2 p2 =
            _baseMovePos +
            startOffset +
            new Vector2(dir * _spec.travelDistance, _spec.endYOffset);

        Vector2 p1 =
            _baseMovePos +
            startOffset +
            new Vector2(
                dir * _spec.travelDistance * _spec.controlForwardRatio,
                _spec.arcHeight);

        Vector2 pos = QuadraticBezier(p0, p1, p2, u);
        Vector2 tangent = QuadraticBezierTangent(p0, p1, p2, u);

        _moveRect.anchoredPosition = pos;

        float appear = SmoothStep01(SafeInverseLerp(0f, _spec.fadeInPortion, u));
        float disappear = 1f - SmoothStep01(SafeInverseLerp(_spec.fadeOutStart, 1f, u));
        _rootCanvasGroup.alpha = appear * disappear;

        float scaleIn = SmoothStep01(SafeInverseLerp(0f, 0.24f, u));
        float scaleOut = SmoothStep01(SafeInverseLerp(0.62f, 1f, u));

        float scale =
            Mathf.Lerp(_spec.startScale, _spec.cruiseScale, scaleIn);

        scale =
            Mathf.Lerp(scale, _spec.endScale, scaleOut);

        _scaleRect.localScale = _baseScale * scale;

        float pathTilt =
            Mathf.Atan2(tangent.y, Mathf.Abs(tangent.x)) *
            Mathf.Rad2Deg *
            dir *
            _spec.tangentTiltWeight;

        float z = (_spec.baseTiltDegrees * dir) + pathTilt;

        _rotationRect.localRotation =
            _baseRotation * Quaternion.Euler(0f, 0f, z);
    }

    private void CommitFinalState()
    {
        KillTween();

        if (_rootCanvasGroup != null)
            _rootCanvasGroup.alpha = 0f;

        if (_moveRect != null)
            _moveRect.anchoredPosition = _baseMovePos;

        if (_scaleRect != null)
            _scaleRect.localScale = _baseScale;

        if (_rotationRect != null)
            _rotationRect.localRotation = _baseRotation;

        HasClaimedTarget = false;
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        _tween = null;
    }

    private static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float u)
    {
        float inv = 1f - u;

        return
            (inv * inv * p0) +
            (2f * inv * u * p1) +
            (u * u * p2);
    }

    private static Vector2 QuadraticBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, float u)
    {
        return
            (2f * (1f - u) * (p1 - p0)) +
            (2f * u * (p2 - p1));
    }

    private static float SafeInverseLerp(float a, float b, float value)
    {
        if (Mathf.Approximately(a, b))
            return value >= b ? 1f : 0f;

        return Mathf.Clamp01((value - a) / (b - a));
    }

    private static float SmoothStep01(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }
}