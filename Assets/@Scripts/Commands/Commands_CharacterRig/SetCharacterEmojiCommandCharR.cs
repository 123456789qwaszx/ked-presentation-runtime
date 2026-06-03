using System;
using DG.Tweening;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Set Character Emoji", Order = -700)]
public sealed class SetCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Emoji Identity")]
    public string emojiKey;

    [Tooltip("Resolver를 거치지 않고 직접 스프라이트를 넣고 싶을 때 사용합니다.")]
    public Sprite directSprite;

    [Header("Rig Targets")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;

    public CharacterRigTarget castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform;

    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Layout")]
    public bool useResolvedLayout = true;
    public bool overrideLayout = false;
    public CharacterEmojiLayout layout = CharacterEmojiLayout.Default;

    [Header("Visibility")]
    [Range(0f, 1f)]
    public float alpha = 1f;

    [Min(0f)]
    public float fadeIn = 0.08f;

    public Ease fadeEase = Ease.OutCubic;

    [Header("Reset")]
    public bool resetCastTransform = true;

    [Header("Tween")]
    public bool killTween = true;
}


public sealed class SetCharacterEmojiCommandCharR : CommandBase
{
    private readonly SetCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _root;
    private RectTransform _castTransform;
    private Image _image;
    private CanvasGroup _rootCanvasGroup;

    private Sprite _resolvedSprite;
    private CharacterEmojiLayout _resolvedLayout;

    private Tween _fadeTween;

    private bool _resolveAttempted;
    private bool _canCommitFinalState;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetCharacterEmojiCommandCharR(SetCharacterEmojiCommandSpecCharR spec, CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            Resolve(scope);

        if (!HasValidRefs())
        {
            ClearRuntimeRefs();
            yield break;
        }

        if (_spec.killTween)
            KillTween(true);

        _canCommitFinalState = true;

        ApplySpriteAndLayout();

        if (_spec.fadeIn <= 0f || scope.ShouldCompressTime)
        {
            CommitFinalState();
            ClearRuntimeRefs();
            yield break;
        }

        _rootCanvasGroup.alpha = 0f;

        _fadeTween = _rootCanvasGroup
            .DOFade(_spec.alpha, _spec.fadeIn)
            .SetEase(_spec.fadeEase)
            .SetUpdate(true)
            .SetTarget(_rootCanvasGroup)
            .OnComplete(() =>
            {
                if (!_canCommitFinalState || !HasValidRefs())
                    return;

                CommitFinalState();
                ClearRuntimeRefs();
            });

        if (_spec.wait)
            yield return _fadeTween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            Resolve(scope);

        CommitFinalState();
        ClearRuntimeRefs();
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            Resolve(scope);

        if (!_canCommitFinalState)
            return;

        CommitFinalState();
        ClearRuntimeRefs();
    }

    private void Resolve(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        if (rigRefs == null)
        {
            Debug.LogWarning(
                $"[SetCharacterEmojiCommandCharR] Failed to resolve CharacterRigRefs. " +
                $"targetKey='{_spec.slotKey}', emojiKey='{_spec.emojiKey}'.");
            return;
        }

        _root = rigRefs.GetRect(_spec.rootTarget);
        _castTransform = rigRefs.GetRect(_spec.castTarget);
        _image = rigRefs.GetImage(_spec.imageTarget);

        if (_root != null)
            _root.TryGetComponent(out _rootCanvasGroup);

        ResolveEmoji();
    }

    private void ResolveEmoji()
    {
        _resolvedSprite = null;
        _resolvedLayout = CharacterEmojiLayout.Default;

        if (_spec.directSprite != null)
        {
            _resolvedSprite = _spec.directSprite;
            _resolvedLayout = _spec.overrideLayout
                ? _spec.layout
                : CharacterEmojiLayout.Default;

            return;
        }

        if (_resolver != null &&
            _resolver.TryResolve(
                _spec.emojiKey,
                out Sprite sprite,
                out CharacterEmojiLayout resolvedLayout))
        {
            _resolvedSprite = sprite;

            if (_spec.overrideLayout)
                _resolvedLayout = _spec.layout;
            else if (_spec.useResolvedLayout)
                _resolvedLayout = resolvedLayout;
            else
                _resolvedLayout = CharacterEmojiLayout.Default;

            return;
        }

        Debug.LogWarning(
            $"[SetCharacterEmojiCommandCharR] Failed to resolve emoji sprite. " +
            $"emojiKey='{_spec.emojiKey}', targetKey='{_spec.slotKey}'.");
    }

    private void ApplySpriteAndLayout()
    {
        if (!HasValidRefs())
            return;

        if (!_root.gameObject.activeSelf)
            _root.gameObject.SetActive(true);

        _image.gameObject.SetActive(true);
        _image.enabled = _resolvedSprite != null;
        _image.sprite = _resolvedSprite;
        _image.preserveAspect = _resolvedLayout.preserveAspect;

        if (_spec.resetCastTransform)
            ResetCastTransform();

        ApplyLayout();

        if (_resolvedLayout.setNativeSize && _image.sprite != null)
            _image.SetNativeSize();
    }

    private void ApplyLayout()
    {
        if (_castTransform == null)
            return;

        _castTransform.anchoredPosition = _resolvedLayout.anchoredPosition;
        _castTransform.localScale = _resolvedLayout.localScale;
        _castTransform.localRotation = Quaternion.Euler(0f, 0f, _resolvedLayout.rotationZ);
    }

    private void ResetCastTransform()
    {
        if (_castTransform == null)
            return;

        _castTransform.anchoredPosition = Vector2.zero;
        _castTransform.localScale = Vector3.one;
        _castTransform.localRotation = Quaternion.identity;
    }

    private void CommitFinalState()
    {
        KillTween(false);

        if (!HasValidRefs())
        {
            _canCommitFinalState = false;
            return;
        }

        ApplySpriteAndLayout();

        _rootCanvasGroup.alpha = _spec.alpha;

        _canCommitFinalState = false;
    }

    private void KillTween(bool complete)
    {
        if (_fadeTween != null)
        {
            _fadeTween.Kill(false);
            _fadeTween = null;
        }

        if (_rootCanvasGroup != null)
            _rootCanvasGroup.DOKill(complete);

        if (_root != null)
            _root.DOKill(complete);

        if (_castTransform != null)
            _castTransform.DOKill(complete);

        if (_image != null)
            _image.DOKill(complete);
    }

    private bool HasValidRefs()
    {
        return _root != null
               && _rootCanvasGroup != null
               && _castTransform != null
               && _image != null
               && _resolvedSprite != null;
    }

    private void ClearRuntimeRefs()
    {
        _fadeTween = null;

        _root = null;
        _castTransform = null;
        _image = null;
        _rootCanvasGroup = null;

        _resolvedSprite = null;
        _resolvedLayout = CharacterEmojiLayout.Default;

        _resolveAttempted = false;
        _canCommitFinalState = false;
    }
}