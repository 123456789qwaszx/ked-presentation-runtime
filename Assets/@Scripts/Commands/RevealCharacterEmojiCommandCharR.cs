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

    [Header("Tween")]
    public bool killTween = true;
}

public sealed class RevealCharacterEmojiCommandCharR : CommandBase
{
    private readonly RevealCharacterEmojiCommandSpecCharR _spec;

    private CharacterRigRefs _rigRefs;
    private Image _image;
    private CharacterEmojiMaterialRuntime _materialRuntime;
    private Tween _tween;

    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    private float _fromReveal;
    private float _toReveal;
    private float _duration;
    private Ease _ease;

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
        {
            ClearRuntimeRefs();
            yield break;
        }

        if (!PrepareMaterial())
        {
            ClearRuntimeRefs();
            yield break;
        }

        ResolveTweenValues();

        if (_spec.killTween)
            _materialRuntime.KillTween(true); // Finish previous reveal so this command starts from a committed state.

        _canCommitFinalState = true;

        if (_duration <= 0f || scope.ShouldCompressTime)
        {
            _materialRuntime.SetReveal(_toReveal);
            ClearRuntimeRefs();
            yield break;
        }

        _tween = _materialRuntime
            .TweenReveal(
                _fromReveal,
                _toReveal,
                _duration,
                _ease,
                useUnscaledTime: true);

        if (_tween == null)
        {
            _materialRuntime.SetReveal(_toReveal);
            ClearRuntimeRefs();
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
        {
            ClearRuntimeRefs();
            return;
        }

        ResolveTweenValues();

        _materialRuntime.SetReveal(_toReveal);

        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_canCommitFinalState || !HasValidRefs())
            return;

        ResolveTweenValues();

        _tween?.Kill(false);
        _materialRuntime.KillTween(false);
        _materialRuntime.SetReveal(_toReveal);

        ClearRuntimeRefs();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        if (_rigRefs == null)
        {
            Debug.LogWarning(
                $"[RevealCharacterEmojiCommandCharR] Failed to resolve CharacterRigRefs. " +
                $"targetKey='{_spec.slotKey}', imageTarget='{_spec.imageTarget}'.");
            return;
        }

        _image = _rigRefs.GetImage(_spec.imageTarget);
        _materialRuntime = _rigRefs.GetEmojiMaterialRuntime(_spec.imageTarget);
    }

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
        return _image != null
               && _materialRuntime != null;
    }

    private void ClearRuntimeRefs()
    {
        _canCommitFinalState = false;

        _rigRefs = null;
        _image = null;
        _materialRuntime = null;
        _tween = null;

        _fromReveal = 0f;
        _toReveal = 1f;
        _duration = 0f;
        _ease = Ease.OutCubic;
    }
}