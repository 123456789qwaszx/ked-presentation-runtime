using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;


[Serializable]
[CommandMenuHint("Char Rig", "Set Portrait (Crossfade)", Order = -960)]
public sealed class SetPortraitCrossfadeCommandSpecCharR : CommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;

    [Header("Tween")]
    [Range(0f, 2f)]
    public float duration = 0.28f;

    public Ease ease = Ease.OutCubic;
    public bool snapOnSkip = true;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetPortraitCrossfadeCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SetPortraitCrossfadeCommandSpecCharR _spec;
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

    public SetPortraitCrossfadeCommandCharR(
        SetPortraitCrossfadeCommandSpecCharR spec,
        PortraitResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Sprite targetSprite = ResolveSprite(_spec.portrait);
        if (targetSprite == null)
        {
            Debug.LogWarning(
                $"[SetPortraitCrossfadeCommandCharR] Failed to resolve portrait:\n" +
                $"  Character: {SafeTrim(_spec.portrait?.character)}\n" +
                $"  Variant: {SafeTrim(_spec.portrait?.variant)}\n" +
                $"  Emotion: {SafeTrim(_spec.portrait?.emotion)}");
            yield break;
        }

        _overlayCanvasGroup.DOKill(true); // Finish previous motion so this command starts from a committed state.
        _portraitCanvasGroup.DOKill(true); // Finish previous motion so this command starts from a committed state.
        _seq?.Kill(false);
        _seq = null;
        _canCommitFinalState = true;

        EnsureRootsVisible();

        _overlayImage.sprite = targetSprite;
        ApplySizing(_overlayImage, targetSprite);

        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;

        if (_spec.duration <= 0f)
        {
            CommitFinalState(targetSprite);
            _canCommitFinalState = false;
            _seq = null;
            yield break;
        }

        _seq = DOTween.Sequence()
            .SetUpdate(true)
            .Join(_portraitCanvasGroup.DOFade(0f, _spec.duration).SetEase(_spec.ease))
            .Join(_overlayCanvasGroup.DOFade(1f, _spec.duration).SetEase(_spec.ease))
            .AppendCallback(() =>
            {
                if (!_canCommitFinalState)
                    return;

                CommitFinalState(targetSprite);
            })
            .OnComplete(() =>
            {
                if (!_canCommitFinalState)
                    return;

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

        Sprite targetSprite = ResolveSprite(_spec.portrait);
        if (targetSprite == null)
            return;

        CommitFinalState(targetSprite);
        _canCommitFinalState = false;
        _seq = null;
    }

    
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState || _portraitRoot == null || _overlayRoot == null)
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

        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig))
            return;

        _portraitRoot = rig.CharacterPortrait_Root;
        _overlayRoot = rig.CharacterPortraitOverlay_Root;
        _portraitImage = rig.CharacterPortrait_Image;
        _overlayImage = rig.CharacterPortraitOverlay_Image;
        _portraitCanvasGroup = GetRootCanvasGroup(_portraitRoot, "CharacterPortrait_Root");
        _overlayCanvasGroup = GetRootCanvasGroup(_overlayRoot, "CharacterPortraitOverlay_Root");
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
        _portraitImage.sprite = targetSprite;
        ApplySizing(_portraitImage, targetSprite);

        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;
    }

    private CanvasGroup GetRootCanvasGroup(RectTransform root, string debugName)
    {
        if (root.TryGetComponent(out CanvasGroup canvasGroup))
            return canvasGroup;

        throw new InvalidOperationException(
            $"[SetPortraitCrossfadeCommandCharR] CanvasGroup missing on Root: {debugName} ({root.name})");
    }

    private Sprite ResolveSprite(PortraitIdentity id)
    {
        if (id == null)
            return null;

        string character = SafeTrim(id.character);
        if (string.IsNullOrEmpty(character))
            return null;

        string variant = ResolveVariantKey(character, id.variant);
        return _resolver.Resolve(character, variant, id.emotion);
    }

    private void ApplySizing(Image image, Sprite sprite)
    {
        CharRigImageSizingPolicy.Apply(image, sprite, _spec.sizingMode, _spec.horizontalAlign);
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