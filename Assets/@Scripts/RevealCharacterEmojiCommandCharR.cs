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
    [Tooltip("이미 runtime material이 없을 때만 baseMaterial 확보용으로 사용합니다.")]
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
    private Tween _revealTween;

    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private float _targetReveal;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public RevealCharacterEmojiCommandCharR(RevealCharacterEmojiCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            Resolve(scope);

        if (!HasValidRefs() || !PrepareMaterial())
        {
            ClearRuntimeRefs();
            yield break;
        }

        if (_spec.killTween)
            KillTween(true);

        _canCommitFinalState = true;

        float from = ResolveStartReveal();
        _targetReveal = ResolveTargetReveal();
        float duration = ResolveDuration();
        Ease ease = ResolveEase();

        if (scope.ShouldCompressTime || duration <= 0f)
        {
            CommitFinalState();
            ClearRuntimeRefs();
            yield break;
        }

        _revealTween = _materialRuntime.TweenReveal(
            from,
            _targetReveal,
            duration,
            ease,
            useUnscaledTime: true);

        if (_revealTween == null)
        {
            CommitFinalState();
            ClearRuntimeRefs();
            yield break;
        }

        if (_spec.wait)
        {
            yield return _revealTween.WaitForCompletion();

            CommitFinalState();
            ClearRuntimeRefs();
            yield break;
        }

        // wait=false인 경우에는 runtime이 tween을 소유한다.
        // command는 entry를 닫되, tween을 즉시 final commit하지 않는다.
        _canCommitFinalState = false;
        ClearRuntimeRefs();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            Resolve(scope);

        if (!HasValidRefs() || !PrepareMaterial())
        {
            ClearRuntimeRefs();
            return;
        }

        _canCommitFinalState = true;
        _targetReveal = ResolveTargetReveal();

        CommitFinalState();
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        // wait=false reveal은 runtime tween이 소유하므로 여기서 final commit하면
        // 같은 프레임에 바로 스냅되어 reveal이 보이지 않는다.
        if (!_canCommitFinalState)
            return;

        CommitFinalState();
        ClearRuntimeRefs();
    }

    private void Resolve(CommandRunScope scope)
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

    private float ResolveStartReveal()
    {
        if (_spec.usePresetReveal && _spec.visualPreset != null)
            return _spec.visualPreset.startReveal;

        return _spec.fromReveal;
    }

    private float ResolveTargetReveal()
    {
        if (_spec.usePresetReveal && _spec.visualPreset != null)
            return _spec.visualPreset.endReveal;

        return _spec.toReveal;
    }

    private float ResolveDuration()
    {
        if (_spec.usePresetReveal && _spec.visualPreset != null)
            return _spec.visualPreset.revealDuration;

        return _spec.duration;
    }

    private Ease ResolveEase()
    {
        if (_spec.usePresetReveal && _spec.visualPreset != null)
            return _spec.visualPreset.revealEase;

        return _spec.ease;
    }

    private void CommitFinalState()
    {
        KillTween(false);

        if (!HasValidRefs())
        {
            _canCommitFinalState = false;
            return;
        }

        if (!PrepareMaterial())
        {
            _canCommitFinalState = false;
            return;
        }

        _materialRuntime.SetReveal(_targetReveal);

        _canCommitFinalState = false;
    }

    private void KillTween(bool complete)
    {
        if (_revealTween != null)
        {
            _revealTween.Kill(complete);
            _revealTween = null;
        }

        _materialRuntime?.KillTween(complete);
    }

    private bool HasValidRefs()
    {
        return _image != null
               && _materialRuntime != null;
    }

    private void ClearRuntimeRefs()
    {
        _revealTween = null;

        _rigRefs = null;
        _image = null;
        _materialRuntime = null;

        _resolveAttempted = false;
        _canCommitFinalState = false;
    }
}