using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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

public sealed class RevealCharacterEmojiCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 2f;

    private readonly RevealCharacterEmojiCommandSpecCharR _spec;

    private Image _image;
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
            .SetTarget(_image)
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

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        _image = rig.GetImage(_spec.target);
        _material = _image.material;
    }

    private void ClaimTarget()
    {
        DOTween.Kill(_image, true);

        _material = _image.material;

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

    private void SetReveal(float reveal)
    {
        _material.SetFloat(CharacterEmojiShaderIds.Reveal, reveal);
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween.Kill(false);

        float currentReveal =  _material.GetFloat(CharacterEmojiShaderIds.Reveal);
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
            .SetTarget(_image)
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
}