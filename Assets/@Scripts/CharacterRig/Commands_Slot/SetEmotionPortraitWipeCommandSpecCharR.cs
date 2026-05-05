using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig", "Set Emotion Portrait (Overlay Wipe)", Order = -960)]
public sealed class SetEmotionPortraitWipeCommandSpec : CommandSpecBase
{
    [Header("Emotion")]
    [Tooltip("표정 키. 예: 1, 01, 6, 06")]
    public string emotion;

    [Header("Override Identity (optional)")]
    [Tooltip("비우면 CastRegistry의 characterKey를 사용합니다.")]
    public string characterOverride;

    [Tooltip("비우면 CastRegistry의 variantKey를 사용합니다.")]
    public string variantOverride;

    [Header("Tween")]
    [Range(0f, 2f)]
    public float duration = 0.38f;

    public Ease ease = Ease.OutCubic;
    public bool snapOnSkip = true;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;

    [Header("Validation")]
    public bool strict = true;
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

        if (!HasRequiredRefs())
            yield break;

        Sprite targetSprite = ResolveSprite(scope);
        if (targetSprite == null)
        {
            if (_spec.strict)
            {
                Debug.LogWarning(
                    $"[SetEmotionPortraitWipeCommand] Failed to resolve portrait. " +
                    $"roleKey={SafeTrim(_spec.roleKey)}, emotion={SafeTrim(_spec.emotion)}");
            }

            yield break;
        }

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
                if (!_canCommitFinalState || !HasRequiredRefs())
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

        if (!HasRequiredRefs())
            return;

        Sprite targetSprite = ResolveSprite(scope);
        if (targetSprite == null)
            return;

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

        if (scope == null)
            return;

        string roleKey = SafeTrim(_spec.roleKey);
        if (string.IsNullOrEmpty(roleKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetEmotionPortraitWipeCommand] roleKey is null or empty.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning($"[SetEmotionPortraitWipeCommand] Rig refs not found. roleKey='{roleKey}'.");
            return;
        }

        _portraitRoot = rig.CharacterPortrait_Root;
        _overlayRoot = rig.CharacterPortraitOverlay_Root;
        _portraitImage = rig.CharacterPortrait_Image;
        _overlayImage = rig.CharacterPortraitOverlay_Image;

        if (_portraitRoot != null)
            _portraitCanvasGroup = GetRootCanvasGroup(_portraitRoot, "CharacterPortrait_Root");

        if (_overlayRoot != null)
            _overlayCanvasGroup = GetRootCanvasGroup(_overlayRoot, "CharacterPortraitOverlay_Root");
    }

    private Sprite ResolveSprite(CommandRunScope scope)
    {
        if (scope == null)
            return null;

        string roleKey = SafeTrim(_spec.roleKey);
        if (string.IsNullOrEmpty(roleKey))
            return null;

        if (!scope.CastRegistry.TryGetBinding(roleKey, out CastBinding binding))
        {
            if (_spec.strict)
                Debug.LogWarning($"[SetEmotionPortraitWipeCommand] No cast binding found. roleKey='{roleKey}'.");
            return null;
        }

        string character = string.IsNullOrWhiteSpace(_spec.characterOverride)
            ? binding.CharacterKey
            : SafeTrim(_spec.characterOverride);

        string variant = string.IsNullOrWhiteSpace(_spec.variantOverride)
            ? binding.VariantKey
            : SafeTrim(_spec.variantOverride);

        if (string.IsNullOrWhiteSpace(character))
        {
            if (_spec.strict)
                Debug.LogWarning($"[SetEmotionPortraitWipeCommand] Character is empty. roleKey='{roleKey}'.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(variant))
            variant = "a";

        string resolvedVariant = ResolveVariantKey(character, variant);
        return _resolver.Resolve(character, resolvedVariant, _spec.emotion);
    }

    private bool HasRequiredRefs()
    {
        return _portraitRoot != null &&
               _overlayRoot != null &&
               _portraitImage != null &&
               _overlayImage != null &&
               _portraitCanvasGroup != null &&
               _overlayCanvasGroup != null;
    }

    private void EnsureRootsVisible()
    {
        if (_portraitRoot != null && !_portraitRoot.gameObject.activeSelf)
            _portraitRoot.gameObject.SetActive(true);

        if (_overlayRoot != null && !_overlayRoot.gameObject.activeSelf)
            _overlayRoot.gameObject.SetActive(true);
    }

    private void CommitFinalState(Sprite targetSprite)
    {
        if (!HasRequiredRefs())
            return;

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