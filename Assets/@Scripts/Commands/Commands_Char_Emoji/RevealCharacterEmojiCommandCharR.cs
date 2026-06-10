using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Reveal Character Emoji", Order = -699)]
public sealed class RevealCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Rig Targets")]
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Visual Fallback")]
    [Tooltip("Runtime material이 없을 때만 baseMaterial 확보용으로 사용합니다.")]
    public CharacterEmojiVisualPresetSO visualPreset;

    [Header("Reveal")]
    public bool usePresetReveal = false;

    [Range(0f, 1f)]
    public float fromReveal = 0f;

    [Range(0f, 1f)]
    public float toReveal = 1f;

    [Min(0f)]
    public float duration = 0.12f;

    public Ease ease = Ease.OutCubic;
}

public sealed class RevealCharacterEmojiCommandCharR : CommandBase
{
    private const float StepFinishSpeedUpMultiplier = 2f;

    private readonly RevealCharacterEmojiCommandSpecCharR _spec;

    private Image _image;
    private CharacterEmojiMaterialRuntime _materialRuntime;

    private float _fromReveal;
    private float _toReveal;
    private float _duration;
    private Ease _ease;

    private Tween _tween;

    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RevealCharacterEmojiCommandCharR(RevealCharacterEmojiCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasValidRefs())
            yield break;

        if (!PrepareMaterial())
            yield break;

        ResolveTweenValues();
        ClaimTarget();

        if (_duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _tween = _materialRuntime.TweenReveal(
            _fromReveal,
            _toReveal,
            _duration,
            _ease,
            useUnscaledTime: true);

        if (_tween == null)
        {
            CommitFinalState();
            yield break;
        }

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasValidRefs())
            return;

        if (!PrepareMaterial())
            return;

        ResolveTweenValues();

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _image = rigRefs.GetImage(_spec.imageTarget);
        _materialRuntime = rigRefs.GetEmojiMaterialRuntime(_spec.imageTarget);
    }

    private void ClaimTarget()
    {
        _materialRuntime.KillTween(true);

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _materialRuntime.SetReveal(_toReveal);

        HasClaimedTarget = false;
        _tween = null;
    }

    #region StepLifetimeHook

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;
        
        _materialRuntime.KillTween(false);

        float currentReveal = CaptureCurrentReveal();
        float duration = CalculateAcceleratedRemainingDuration(currentReveal);

        _fromReveal = currentReveal;

        _tween = _materialRuntime.TweenReveal(
            _fromReveal,
            _toReveal,
            duration,
            _ease,
            useUnscaledTime: true);

        if (_tween == null)
            CommitFinalState();
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

    private float CaptureCurrentReveal()
    {
        if (_materialRuntime == null || _materialRuntime.RuntimeMaterial == null)
            return _toReveal;

        return _materialRuntime.RuntimeMaterial.GetFloat(CharacterEmojiShaderIds.Reveal);
    }

    #endregion

    private bool PrepareMaterial()
    {
        if (_materialRuntime == null)
        {
            Debug.LogWarning(
                $"[RevealCharacterEmojiCommandCharR] Failed to resolve emoji material runtime. " +
                $"targetKey='{_spec.slotKey}', imageTarget='{_spec.imageTarget}'.");
            return false;
        }

        if (_materialRuntime.RuntimeMaterial != null)
            return true;

        if (_spec.visualPreset == null || _spec.visualPreset.baseMaterial == null)
        {
            Debug.LogWarning(
                $"[RevealCharacterEmojiCommandCharR] No runtime material and no visualPreset/baseMaterial. " +
                $"Run emoji set command first or provide visualPreset. " +
                $"targetKey='{_spec.slotKey}', imageTarget='{_spec.imageTarget}'.");
            return false;
        }

        return _materialRuntime.EnsureMaterial(_spec.visualPreset.baseMaterial);
    }

    private void ResolveTweenValues()
    {
        if (_spec.usePresetReveal && _spec.visualPreset != null)
        {
            _fromReveal = _spec.visualPreset.startReveal;
            _toReveal = _spec.visualPreset.endReveal;
            _duration = _spec.visualPreset.revealDuration;
            _ease = _spec.visualPreset.revealEase;
            return;
        }

        _fromReveal = _spec.fromReveal;
        _toReveal = _spec.toReveal;
        _duration = _spec.duration;
        _ease = _spec.ease;
    }

    private bool HasValidRefs()
    {
        return _image != null && _materialRuntime != null;
    }
}