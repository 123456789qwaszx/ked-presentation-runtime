using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig", "Set Emotion Portrait (Overlay Wipe)", Order = -960)]
public sealed class SetEmotionPortraitWipeCommandSpec : CharacterRigCommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;

    [Header("Tween")]
    [Range(0f, 2f)]
    public float duration = 0.38f;

    public Ease ease = Ease.OutCubic;
    public bool snapOnSkip = true;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetEmotionPortraitWipeCommand : CommandBase, IStepScopedCommand
{
    private readonly SetEmotionPortraitWipeCommandSpec _spec;
    private readonly PortraitResolver _resolver;

    private RectTransform _portraitRoot;
    private RectTransform _overlayRoot;
    private Image _portraitImage;
    private Image _overlayImage;
    private CanvasGroup _portraitCanvasGroup;
    private CanvasGroup _overlayCanvasGroup;

    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private Sequence _seq;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetEmotionPortraitWipeCommand(
        SetEmotionPortraitWipeCommandSpec spec,
        PortraitResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Sprite targetSprite = ResolveSprite(scope);

        _overlayCanvasGroup.DOKill(true);
        _portraitCanvasGroup.DOKill(true);
        _seq?.Kill(false);
        _seq = null;
        _canCommitFinalState = true;

        EnsureRootsVisible();

        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;

        _overlayImage.sprite = targetSprite;
        ApplySizing(_overlayImage, targetSprite);

        if (_spec.duration <= 0f)
        {
            CommitFinalState(targetSprite);
            _canCommitFinalState = false;
            _seq = null;
            yield break;
        }

        _seq = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_overlayCanvasGroup
                .DOFade(1f, _spec.duration)
                .SetEase(_spec.ease))
            .AppendCallback(() =>
            {
                if (!_canCommitFinalState)
                    return;

                _portraitImage.sprite = targetSprite;
                ApplySizing(_portraitImage, targetSprite);
                _portraitCanvasGroup.alpha = 1f;
            })
            .Append(_overlayCanvasGroup
                .DOFade(0f, _spec.duration)
                .SetEase(_spec.ease))
            .OnComplete(() =>
            {
                if (!_canCommitFinalState)
                    return;

                CommitFinalState(targetSprite);
                _canCommitFinalState = false;
                _seq = null;
            });

        if (_spec.wait)
            yield return _seq.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Sprite targetSprite = ResolveSprite(scope);

        CommitFinalState(targetSprite);
        _canCommitFinalState = false;
        _seq = null;
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        if (_seq == null)
            return;

        if (_spec.snapOnSkip)
            _seq.Complete(true);
        else
            _seq.Kill(false);

        _canCommitFinalState = false;
        _seq = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.targetKey);

        _portraitRoot = rigRefs.CharacterPortrait_Root;
        _overlayRoot = rigRefs.CharacterPortraitOverlay_Root;
        _portraitImage = rigRefs.CharacterPortrait_Image;
        _overlayImage = rigRefs.CharacterPortraitOverlay_Image;

        _portraitCanvasGroup = GetRootCanvasGroup(_portraitRoot, "CharacterPortrait_Root");
        _overlayCanvasGroup = GetRootCanvasGroup(_overlayRoot, "CharacterPortraitOverlay_Root");
    }

    private Sprite ResolveSprite(CommandRunScope scope)
    {
        PortraitIdentity id = _spec.portrait;
        if (id == null)
            throw new InvalidOperationException("[SetEmotionPortraitWipeCommand] PortraitIdentity is null.");

        string roleKey =
            CharacterRigTargetResolver.ResolveRoleKeyFromTargetKey(
                scope,
                _spec.targetKey);

        if (!scope.CastRegistry.TryGetBinding(roleKey, out CastBinding binding))
        {
            throw new InvalidOperationException(
                $"[SetEmotionPortraitWipeCommand] No cast binding found. targetKey='{_spec.targetKey}', roleKey='{roleKey}'.");
        }

        string character = SafeTrim(id.character);
        if (string.IsNullOrEmpty(character))
            character = SafeTrim(binding.CharacterKey);

        if (string.IsNullOrEmpty(character))
        {
            throw new InvalidOperationException(
                $"[SetEmotionPortraitWipeCommand] Character is empty. targetKey='{_spec.targetKey}', roleKey='{roleKey}'.");
        }

        string variant = SafeTrim(id.variant);
        if (string.IsNullOrEmpty(variant))
            variant = SafeTrim(binding.VariantKey);

        if (string.IsNullOrEmpty(variant))
            variant = "a";

        string emotion = SafeTrim(id.emotion);
        if (string.IsNullOrEmpty(emotion))
        {
            throw new InvalidOperationException(
                $"[SetEmotionPortraitWipeCommand] Emotion is empty. targetKey='{_spec.targetKey}', roleKey='{roleKey}', character='{character}'.");
        }

        string resolvedVariant = ResolveVariantKey(character, variant);
        Sprite sprite = _resolver.Resolve(character, resolvedVariant, emotion);

        if (sprite == null)
        {
            throw new InvalidOperationException(
                $"[SetEmotionPortraitWipeCommand] Failed to resolve portrait. character='{character}', variant='{resolvedVariant}', emotion='{emotion}'.");
        }

        return sprite;
    }

    private void EnsureRootsVisible()
    {
        if (!_portraitRoot.gameObject.activeSelf)
            _portraitRoot.gameObject.SetActive(true);

        if (!_overlayRoot.gameObject.activeSelf)
            _overlayRoot.gameObject.SetActive(true);
    }

    private void CommitFinalState(Sprite targetSprite)
    {
        _seq?.Kill(false);
        _seq = null;

        _portraitImage.sprite = targetSprite;
        ApplySizing(_portraitImage, targetSprite);

        _overlayCanvasGroup.alpha = 0f;
        _portraitCanvasGroup.alpha = 1f;
    }

    private static CanvasGroup GetRootCanvasGroup(RectTransform root, string debugName)
    {
        if (root.TryGetComponent(out CanvasGroup canvasGroup))
            return canvasGroup;

        throw new InvalidOperationException(
            $"[SetEmotionPortraitWipeCommand] CanvasGroup missing on Root: {debugName} ({root.name})");
    }

    private void ApplySizing(Image image, Sprite sprite)
    {
        CharRigImageSizingPolicy.Apply(
            image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }

    private static string ResolveVariantKey(string character, string variant)
    {
        if (string.IsNullOrEmpty(variant))
            return "";

        variant = variant.Trim();

        if (variant.StartsWith(character + "_", StringComparison.Ordinal))
            return variant;

        return $"{character}_{variant}";
    }
}