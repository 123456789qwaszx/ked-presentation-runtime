using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Reveal Character Emoji", Order = -698)]
public sealed class RevealCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Reveal")]
    [Range(0f, 1f)]
    public float fromReveal = 0f;

    [Range(0f, 1f)]
    public float toReveal = 1f;

    public float duration = 0.12f;
    public Ease ease = Ease.OutCubic;
}

// Responsibility:
// - 이미 준비된 Emoji runtime material의 _Reveal 값만 tween한다.
// - sprite/placement/alpha/scale/hop/sway 등은 다른 command가 담당한다.
public sealed class RevealCharacterEmojiCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 2f;

    private readonly RevealCharacterEmojiCommandSpecCharR _spec;

    private CharacterEmojiMaterialRuntime _materialRuntime;
    private Material _material;

    private float _fromReveal;
    private float _toReveal;
    private float _duration;
    private Ease _ease;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public RevealCharacterEmojiCommandCharR(RevealCharacterEmojiCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();

        if (_duration <= 0f || Mathf.Approximately(_fromReveal, _toReveal))
        {
            CommitFinalState();
            yield break;
        }

        _tween = DOTween
            .To(
                () => _fromReveal,
                SetReveal,
                _toReveal,
                _duration)
            .SetEase(_ease)
            .SetUpdate(true)
            .SetTarget(_materialRuntime)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _materialRuntime = rigRefs.GetEmojiMaterialRuntime(_spec.target);
        _material = _materialRuntime.RuntimeMaterial;
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_materialRuntime, true);

        _fromReveal = _spec.fromReveal;
        _toReveal = _spec.toReveal;
        _duration = _spec.duration;
        _ease = _spec.ease;

        SetReveal(_fromReveal);

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        SetReveal(_toReveal);

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween.Kill(false);

        float currentReveal = _material.GetFloat(CharacterEmojiShaderIds.Reveal);
        float duration = CalculateAcceleratedRemainingDuration(currentReveal);

        if (duration <= 0f)
        {
            CommitFinalState();
            return;
        }

        _fromReveal = currentReveal;

        _tween = DOTween
            .To(
                () => _fromReveal,
                SetReveal,
                _toReveal,
                duration)
            .SetEase(_ease)
            .SetUpdate(true)
            .SetTarget(_materialRuntime)
            .OnComplete(CommitFinalState);
    }

    private float CalculateAcceleratedRemainingDuration(float currentReveal)
    {
        float originalDistance = Mathf.Abs(_toReveal - _fromReveal);
        float remainingDistance = Mathf.Abs(_toReveal - currentReveal);

        if (originalDistance <= 0.001f || remainingDistance <= 0.001f)
            return 0f;

        float remainingRatio = Mathf.Clamp01(remainingDistance / originalDistance);
        float remainingDuration = _duration * remainingRatio;

        return Mathf.Max(0.01f, remainingDuration / StepFinishSpeedUpMultiplier);
    }
    
    #endregion

    private void SetReveal(float reveal)
    {
        _material.SetFloat(CharacterEmojiShaderIds.Reveal, reveal);
    }
}