using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig", "Set Emotion Portrait (Overlay Wipe)", Order = -961)]
public sealed class SetEmotionPortraitWipeCommandSpecCharR : CharRigCommandSpecBase
{
    [Header("Portrait Identity")]
    public PortraitIdentity portrait;

    [Header("Tween")]
    [Range(0f, 2f)]
    public float duration = 0.38f;
    public Ease ease = Ease.OutCubic;
    public bool wait = false;
    public bool snapOnSkip = true;

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;
    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign = CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetEmotionPortraitWipeCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly SetEmotionPortraitWipeCommandSpecCharR _spec;
    private readonly PortraitResolver _resolver;
    private Sequence _seq;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetEmotionPortraitWipeCommandCharR(
        SetEmotionPortraitWipeCommandSpecCharR spec,
        PortraitResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            yield break;

        PortraitIdentity id = _spec.portrait;

        Sprite targetSprite = ResolveSprite(_spec);
        if (targetSprite == null)
        {
            Debug.LogWarning(
                $"[SetEmotionPortraitWipe] Failed to resolve portrait:\n" +
                $"  Character: {SafeTrim(id?.character)}\n" +
                $"  Variant: {SafeTrim(id?.variant)}\n" +
                $"  Emotion: {SafeTrim(id?.emotion)}"
            );
            yield break;
        }

        RectTransform portraitRoot = rig.CharacterPortrait_Root;
        RectTransform overlayRoot  = rig.CharacterPortraitOverlay_Root;
        Image portraitImg          = rig.CharacterPortrait_Image;
        Image overlayImg           = rig.CharacterPortraitOverlay_Image;

        if (portraitRoot == null || overlayRoot == null || portraitImg == null || overlayImg == null)
            yield break;

        CanvasGroup portraitCg = GetRootCanvasGroup(portraitRoot, "CharacterPortrait_Root");
        CanvasGroup overlayCg  = GetRootCanvasGroup(overlayRoot,  "CharacterPortraitOverlay_Root");

        if (!portraitRoot.gameObject.activeSelf) portraitRoot.gameObject.SetActive(true);
        if (!overlayRoot.gameObject.activeSelf)  overlayRoot.gameObject.SetActive(true);

        portraitCg.DOKill(false);
        overlayCg.DOKill(false);

        portraitCg.alpha = 1f;
        overlayCg.alpha  = 0f;

        overlayImg.sprite = targetSprite;
        ApplySizing(overlayImg, targetSprite);

        if (_spec.duration <= 0f)
        {
            portraitImg.sprite = targetSprite;
            ApplySizing(portraitImg, targetSprite);

            overlayCg.alpha = 0f;
            portraitCg.alpha = 1f;
            yield break;
        }

        _seq = DOTween.Sequence().SetUpdate(true);

        _seq.Append(overlayCg.DOFade(1f, _spec.duration).SetEase(_spec.ease));

        _seq.AppendCallback(() =>
        {
            portraitImg.sprite = targetSprite;
            ApplySizing(portraitImg, targetSprite);
            portraitCg.alpha = 1f;
        });

        _seq.Append(overlayCg.DOFade(0f, _spec.duration).SetEase(_spec.ease));

        _seq.OnComplete(() =>
        {
            overlayCg.alpha = 0f;
            portraitCg.alpha = 1f;
        });

        if (!_spec.wait)
            yield break;

        while (_seq != null && _seq.IsActive() && _seq.IsPlaying())
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (_seq == null) return;

        if (_spec.snapOnSkip)
            _seq.Complete(true);
        else
            _seq.Kill(false);

        _seq = null;
    }

    private CanvasGroup GetRootCanvasGroup(RectTransform root, string debugName)
    {
        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup != null) return canvasGroup;

        throw new InvalidOperationException(
            $"[SetEmotionPortraitWipe] CanvasGroup missing on Root: {debugName} ({root.name})"
        );
    }

    private Sprite ResolveSprite(SetEmotionPortraitWipeCommandSpecCharR spec)
    {
        PortraitIdentity id = spec.portrait;
        if (id == null)
            return null;

        string character = SafeTrim(id.character);
        if (string.IsNullOrEmpty(character))
            return null;

        // 1️Variant 해석
        string variant = ResolveVariantKey(character, id.variant);

        // 2️Emotion 해석
        return _resolver.Resolve(character, variant, id.emotion);
    }

    private void ApplySizing(Image img, Sprite sprite)
    {
        CharRigImageSizingPolicy.Apply(img, sprite, _spec.sizingMode, _spec.horizontalAlign);
    }

    private static string SafeTrim(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim();
    
    
    private static string ResolveVariantKey(string character, string variant)
    {
        if (string.IsNullOrEmpty(variant))
            return ""; // resolver가 defaultVariant 처리

        variant = variant.Trim();

        // 이미 풀 키면 그대로
        if (variant.StartsWith(character + "_", StringComparison.Ordinal))
            return variant;

        // shorthand: "a" → "Amber_a"
        return $"{character}_{variant}";
    }
}