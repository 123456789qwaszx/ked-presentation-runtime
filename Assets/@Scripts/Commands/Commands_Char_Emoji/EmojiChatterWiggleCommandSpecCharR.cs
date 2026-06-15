using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Emoji",
    "Emoji Chatter Wiggle",
    Order = -697)]
public sealed class EmojiChatterWiggleCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Targets")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
    public CharacterRigTarget pivotTarget = CharacterRigTarget.EmojiSlot00_SwayPivot;
    public CharacterRigTarget effectTarget = CharacterRigTarget.CharacterEmojiSlot00_Effect;

    [Header("Pivot")]
    [Tooltip("가로 잡담 이모지의 오른쪽 곡선 중심 쪽 pivot입니다. 1보다 큰 값도 가능합니다.")]
    public Vector2 pivot = new(1.08f, 0.52f);

    [Header("Pose")]
    public Vector2 settleOffset = new(4f, 0f);
    public float baseTiltDegrees = 0f;

    [Header("Fade")]
    public float fadeInDuration = 0.18f;

    [Header("Soft Wiggle")]
    [Tooltip("전체 흔들림 시간.")]
    public float duration = 0.62f;

    [Tooltip("2~3회 정도가 귀엽고 덜 시끄럽습니다.")]
    public float cycles = 2.5f;

    [Tooltip("최대 흔들림 각도. 2.5~3.5 권장.")]
    public float amplitude = 3.0f;

    [Tooltip("값이 클수록 뒤로 갈수록 빨리 잦아듭니다.")]
    public float dampingPower = 1.35f;

    [Header("Tween")]
    public Ease ease = Ease.Linear;
}

public sealed class EmojiChatterWiggleCommandCharR : CommandBase
{
    private readonly EmojiChatterWiggleCommandSpecCharR _spec;

    private CanvasGroup _rootCanvasGroup;
    private RectTransform _pivotRect;
    private RectTransform _effectRect;

    private Vector2 _basePivot;
    private Vector2 _baseEffectPos;
    private Quaternion _basePivotRotation;

    private Tween _tween;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiChatterWiggleCommandCharR(EmojiChatterWiggleCommandSpecCharR spec)
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
            SettleVisibleState();
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
            .SetTarget(_pivotRect)
            .OnComplete(SettleVisibleState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        HideAndReset();
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

                HideAndReset();
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

        HideAndReset();
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

        _pivotRect = rigRefs.GetRect(_spec.pivotTarget);
        _effectRect = rigRefs.GetRect(_spec.effectTarget);
    }

    private void ClaimTarget()
    {
        KillTween();

        _pivotRect.DOKill(true);
        _effectRect.DOKill(true);

        _basePivot = _pivotRect.pivot;
        _baseEffectPos = _effectRect.anchoredPosition;
        _basePivotRotation = _pivotRect.localRotation;

        _rootCanvasGroup.alpha = 0f;

        _pivotRect.pivot = _spec.pivot;
        _pivotRect.localRotation =
            _basePivotRotation * Quaternion.Euler(0f, 0f, _spec.baseTiltDegrees);

        _effectRect.anchoredPosition = _baseEffectPos + _spec.settleOffset;

        HasClaimedTarget = true;
    }

    private void ApplyProgress(float u)
    {
        u = Mathf.Clamp01(u);

        float fade =
            _spec.fadeInDuration <= 0f
                ? 1f
                : SmoothStep01(u / Mathf.Clamp01(_spec.fadeInDuration / Mathf.Max(0.0001f, _spec.duration)));

        _rootCanvasGroup.alpha = fade;

        float envelope = Mathf.Pow(1f - u, _spec.dampingPower);
        float swing = Mathf.Sin(2f * Mathf.PI * _spec.cycles * u);

        float angle =
            _spec.baseTiltDegrees +
            (_spec.amplitude * envelope * swing);

        _pivotRect.localRotation =
            _basePivotRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void SettleVisibleState()
    {
        KillTween();

        if (_rootCanvasGroup != null)
            _rootCanvasGroup.alpha = 1f;

        if (_pivotRect != null)
        {
            _pivotRect.pivot = _spec.pivot;
            _pivotRect.localRotation =
                _basePivotRotation * Quaternion.Euler(0f, 0f, _spec.baseTiltDegrees);
        }

        if (_effectRect != null)
            _effectRect.anchoredPosition = _baseEffectPos + _spec.settleOffset;

        // 중요:
        // 자연 완료 후에도 HasClaimedTarget은 true로 유지한다.
        // 그래야 다음 step cleanup에서 HideAndReset()이 호출되어 사라진다.
    }

    private void HideAndReset()
    {
        KillTween();

        if (_rootCanvasGroup != null)
            _rootCanvasGroup.alpha = 0f;

        if (_pivotRect != null)
        {
            _pivotRect.pivot = _basePivot;
            _pivotRect.localRotation = _basePivotRotation;
        }

        if (_effectRect != null)
            _effectRect.anchoredPosition = _baseEffectPos;

        HasClaimedTarget = false;
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill(false);

        _tween = null;
    }

    private static float SmoothStep01(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }
}