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

    [Header("Sizing Policy")]
    public CharRigImageSizingMode sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect;

    public CharRigImageSizingPolicy.HorizontalAlign horizontalAlign =
        CharRigImageSizingPolicy.HorizontalAlign.Center;
}

public sealed class SetEmotionPortraitWipeCommand : CommandBase
{
    private readonly SetEmotionPortraitWipeCommandSpec _spec;
    private readonly PortraitResolver _resolver;

    private RectTransform _portraitRoot;
    private RectTransform _overlayRoot;
    private Image _portraitImage;
    private Image _overlayImage;
    private CanvasGroup _portraitCanvasGroup;
    private CanvasGroup _overlayCanvasGroup;

    private Sprite _targetSprite;
    private Sequence _seq;

    private bool _resolveAttempted;
    private bool _hasResolvedTargetSprite;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

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

        ResolveTargetSprite(scope);

        ClaimTarget();
        PrepareTransitionState();

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            yield break;
        }

        _seq = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(_overlayRoot);

        _seq.Append(
            _overlayCanvasGroup
                .DOFade(1f, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_overlayCanvasGroup));

        _seq.AppendCallback(() =>
        {
            _portraitImage.sprite = _targetSprite;
            ApplySizing(_portraitImage, _targetSprite);
            _portraitCanvasGroup.alpha = 1f;
        });

        _seq.Append(
            _overlayCanvasGroup
                .DOFade(0f, _spec.duration)
                .SetEase(_spec.ease)
                .SetUpdate(true)
                .SetTarget(_overlayCanvasGroup));

        _seq.OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _seq.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ResolveTargetSprite(scope);

        if (!HasClaimedTarget)
            ClaimTarget();
        else
            KillCurrentTween();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        _portraitRoot = rigRefs.CharacterPortraitSprite_Root;
        _overlayRoot = rigRefs.CharacterPortraitSpriteOverlay_Root;
        _portraitImage = rigRefs.CharacterPortraitSprite_Image;
        _overlayImage = rigRefs.CharacterPortraitSpriteOverlay_Image;

        _portraitCanvasGroup = _portraitRoot.GetComponent<CanvasGroup>();
        _overlayCanvasGroup = _overlayRoot.GetComponent<CanvasGroup>();
    }

    private void ResolveTargetSprite(CommandRunScope scope)
    {
        if (_hasResolvedTargetSprite)
            return;

        _targetSprite = _resolver.Resolve(
            scope,
            _spec.slotKey,
            _spec.portrait,
            nameof(SetEmotionPortraitWipeCommand));

        _hasResolvedTargetSprite = true;
    }

    private void ClaimTarget()
    {
        KillPreviousTween();

        HasClaimedTarget = true;
    }

    private void PrepareTransitionState()
    {
        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;

        _overlayImage.sprite = _targetSprite;
        ApplySizing(_overlayImage, _targetSprite);
    }

    private void CommitFinalState()
    {
        _portraitImage.sprite = _targetSprite;
        ApplySizing(_portraitImage, _targetSprite);

        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;

        HasClaimedTarget = false;
    }

    private void KillPreviousTween()
    {
        _seq?.Kill(false);
        _seq = null;

        _overlayCanvasGroup.DOKill(true);
        _portraitCanvasGroup.DOKill(true);
        _overlayRoot.DOKill(true);
        _portraitRoot.DOKill(true);
    }

    private void KillCurrentTween()
    {
        _seq?.Kill(false);
        _seq = null;

        _overlayCanvasGroup.DOKill(false);
        _portraitCanvasGroup.DOKill(false);
        _overlayRoot.DOKill(false);
        _portraitRoot.DOKill(false);
    }

    private void ApplySizing(Image image, Sprite sprite)
    {
        CharRigImageSizingPolicy.Apply(
            image,
            sprite,
            _spec.sizingMode,
            _spec.horizontalAlign);
    }
}