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

    [Tooltip("체크하면 기존 Portrait/Overlay Tween을 끝내고 committed state에서 시작합니다.")]
    public bool killTween = true;

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

    private Sequence _seq;

    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    private bool _hasResolvedTargetSprite;
    private Sprite _targetSprite;

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

        ResolveTargetSprite(scope);

        if (_spec.killTween)
            KillTween(true); // Finish previous portrait/overlay tween so this command starts from a committed state.

        _canCommitFinalState = true;

        if (!HasValidRefs())
        {
            ClearRuntimeRefs();
            yield break;
        }

        EnsureRootsVisible();

        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;

        _overlayImage.sprite = _targetSprite;
        ApplySizing(_overlayImage, _targetSprite);

        if (_spec.duration <= 0f)
        {
            CommitFinalState();
            ClearRuntimeRefs();
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
            if (!_canCommitFinalState || !HasValidRefs())
                return;

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

        _seq.OnComplete(() =>
        {
            if (!_canCommitFinalState || !HasValidRefs())
                return;

            CommitFinalState();
            ClearRuntimeRefs();
        });

        if (_spec.wait)
            yield return _seq.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ResolveTargetSprite(scope);

        CommitFinalState();
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_canCommitFinalState)
            return;

        if (!_resolveAttempted)
            ResolveRefs(scope);

        ResolveTargetSprite(scope);

        CommitFinalState();
        ClearRuntimeRefs();
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

    private void ResolveTargetSprite(CommandRunScope scope)
    {
        if (_hasResolvedTargetSprite)
            return;

        _targetSprite =
            PortraitIdentityResolveUtility.ResolveSprite(
                scope,
                _resolver,
                _spec.targetKey,
                _spec.portrait,
                nameof(SetEmotionPortraitWipeCommand));

        _hasResolvedTargetSprite = true;
    }

    private void EnsureRootsVisible()
    {
        if (_portraitRoot != null && !_portraitRoot.gameObject.activeSelf)
            _portraitRoot.gameObject.SetActive(true);

        if (_overlayRoot != null && !_overlayRoot.gameObject.activeSelf)
            _overlayRoot.gameObject.SetActive(true);
    }

    private void CommitFinalState()
    {
        KillTween(false);

        if (!HasValidRefs())
        {
            _canCommitFinalState = false;
            return;
        }

        _portraitImage.sprite = _targetSprite;
        ApplySizing(_portraitImage, _targetSprite);

        _portraitCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.alpha = 0f;

        _canCommitFinalState = false;
    }

    private void KillTween(bool completePreviousTweens)
    {
        if (_seq != null)
        {
            _seq.Kill(false);
            _seq = null;
        }

        if (_overlayCanvasGroup != null)
            _overlayCanvasGroup.DOKill(completePreviousTweens);

        if (_portraitCanvasGroup != null)
            _portraitCanvasGroup.DOKill(completePreviousTweens);

        if (_overlayRoot != null)
            _overlayRoot.DOKill(completePreviousTweens);

        if (_portraitRoot != null)
            _portraitRoot.DOKill(completePreviousTweens);
    }

    private bool HasValidRefs()
    {
        return _portraitRoot != null
               && _overlayRoot != null
               && _portraitImage != null
               && _overlayImage != null
               && _portraitCanvasGroup != null
               && _overlayCanvasGroup != null;
    }

    private void ClearRuntimeRefs()
    {
        _seq = null;

        _portraitRoot = null;
        _overlayRoot = null;
        _portraitImage = null;
        _overlayImage = null;
        _portraitCanvasGroup = null;
        _overlayCanvasGroup = null;

        _targetSprite = null;
        _hasResolvedTargetSprite = false;
        _resolveAttempted = false;
        _canCommitFinalState = false;
    }

    private static CanvasGroup GetRootCanvasGroup(RectTransform root, string debugName)
    {
        if (root == null)
            throw new InvalidOperationException(
                $"[SetEmotionPortraitWipeCommand] Root is null: {debugName}");

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
}