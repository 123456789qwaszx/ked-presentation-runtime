using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
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
    [Tooltip("배치 직후 Emoji slot Root의 CanvasGroup alpha입니다. Fade 연출이 아니라 즉시 상태값입니다.")]
    [Range(0f, 1f)]
    public float alpha = 1f;

    [Header("Visual")]
    public CharacterEmojiVisualPresetSO visualPreset;
    public bool useResolvedVisualPreset = true;
    public bool overrideVisualPreset = false;

    [Header("Reveal Initial State")]
    [Tooltip("배치 직후 머터리얼 _Reveal 값입니다. 일반 표시=1, reveal/pop 합성 준비=0.")]
    [Range(0f, 1f)]
    public float initialReveal = 1f;

    [Header("Reset")]
    public bool resetCastTransform = true;

    [Header("Tween")]
    [Tooltip("true면 이모지를 새로 세팅하기 전에 관련 target의 기존 tween을 committed state로 정리합니다.")]
    public bool killTween = true;
}

public sealed class SetCharacterEmojiCommandCharR : CommandBase
{
    private readonly SetCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private CharacterRigRefs _rigRefs;

    private RectTransform _root;
    private RectTransform _castTransform;
    private Image _image;
    private CanvasGroup _rootCanvasGroup;

    private CharacterEmojiMaterialRuntime _materialRuntime;

    private Sprite _resolvedSprite;
    private CharacterEmojiLayout _resolvedLayout;
    private CharacterEmojiVisualPresetSO _resolvedVisualPreset;

    private bool _resolveAttempted;
    private bool _canCommitFinalState;
    private bool _isHideRequest;

    public override bool WaitForCompletion => _spec.wait;

    public SetCharacterEmojiCommandCharR(
        SetCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasValidTargets())
        {
            ClearRuntimeRefs();
            yield break;
        }

        if (_spec.killTween)
            KillTween(true);

        _canCommitFinalState = true;

        CommitFinalState();
        ClearRuntimeRefs();

        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

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
            ResolveRefs(scope);

        if (!_canCommitFinalState)
            return;

        CommitFinalState();
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
                $"[SetCharacterEmojiCommandCharR] Failed to resolve CharacterRigRefs. " +
                $"targetKey='{_spec.slotKey}', emojiKey='{_spec.emojiKey}'.");
            return;
        }

        _root = _rigRefs.GetRect(_spec.rootTarget);
        _castTransform = _rigRefs.GetRect(_spec.castTarget);
        _image = _rigRefs.GetImage(_spec.imageTarget);
        _materialRuntime = _rigRefs.GetEmojiMaterialRuntime(_spec.imageTarget);

        if (_root != null)
            _root.TryGetComponent(out _rootCanvasGroup);

        ResolveEmoji();
    }

    private void ResolveEmoji()
    {
        _resolvedSprite = null;
        _resolvedLayout = CharacterEmojiLayout.Default;
        _resolvedVisualPreset = null;
        _isHideRequest = false;

        if (_spec.directSprite == null && string.IsNullOrWhiteSpace(_spec.emojiKey))
        {
            _isHideRequest = true;
            return;
        }

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
                out CharacterEmojiLayout resolvedLayout,
                out _resolvedVisualPreset))
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

    private void CommitFinalState()
    {
        if (!HasValidTargets())
        {
            _canCommitFinalState = false;
            return;
        }

        if (_isHideRequest)
        {
            ApplyHiddenState();
            ClearCommitFlag();
            return;
        }

        if (_resolvedSprite == null)
        {
            ClearCommitFlag();
            return;
        }

        ApplySpriteAndLayout();
        ApplyEmojiMaterialInitialState();

        _rootCanvasGroup.alpha = _spec.alpha;

        ClearCommitFlag();
    }

    private void ApplySpriteAndLayout()
    {
        if (!HasValidTargets() || _resolvedSprite == null)
            return;

        if (!_root.gameObject.activeSelf)
            _root.gameObject.SetActive(true);

        _image.gameObject.SetActive(true);
        _image.enabled = true;
        _image.sprite = _resolvedSprite;
        _image.preserveAspect = _resolvedLayout.preserveAspect;

        if (_spec.resetCastTransform)
            ResetCastTransform();

        ApplyLayout();

        if (_resolvedLayout.setNativeSize && _image.sprite != null)
            _image.SetNativeSize();
    }

    private void ApplyEmojiMaterialInitialState()
    {
        CharacterEmojiVisualPresetSO preset = ResolveVisualPreset();

        if (preset == null)
            return;

        if (preset.baseMaterial == null)
        {
            Debug.LogWarning(
                $"[SetCharacterEmojiCommandCharR] Emoji visual preset has no baseMaterial. " +
                $"targetKey='{_spec.slotKey}', emojiKey='{_spec.emojiKey}', preset='{preset.name}'.");
            return;
        }

        if (_materialRuntime == null)
        {
            Debug.LogWarning(
                $"[SetCharacterEmojiCommandCharR] Failed to resolve emoji material runtime. " +
                $"targetKey='{_spec.slotKey}', imageTarget='{_spec.imageTarget}'.");
            return;
        }

        if (!_materialRuntime.EnsureMaterial(preset.baseMaterial))
            return;

        _materialRuntime.ApplyPresetStatic(preset, _spec.initialReveal);
    }

    private CharacterEmojiVisualPresetSO ResolveVisualPreset()
    {
        if (_spec.overrideVisualPreset && _spec.visualPreset != null)
            return _spec.visualPreset;

        if (_spec.useResolvedVisualPreset && _resolvedVisualPreset != null)
            return _resolvedVisualPreset;

        if (_spec.visualPreset != null)
            return _spec.visualPreset;

        return null;
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

    private void ApplyHiddenState()
    {
        if (!HasValidTargets())
            return;

        _image.sprite = null;
        _image.enabled = false;
        _rootCanvasGroup.alpha = 0f;

        _materialRuntime?.KillTween(false);
    }

    private void KillTween(bool complete)
    {
        // SetCharacterEmoji는 reveal/motion/fade 연출을 소유하지 않는다.
        // 다만 새 emoji를 배치할 때 같은 material runtime의 이전 reveal tween은 committed state로 정리한다.
        _materialRuntime?.KillTween(complete);

        if (_rootCanvasGroup != null)
            _rootCanvasGroup.DOKill(complete);

        if (_castTransform != null)
            _castTransform.DOKill(complete);
    }

    private bool HasValidTargets()
    {
        return _root != null
               && _rootCanvasGroup != null
               && _castTransform != null
               && _image != null;
    }

    private void ClearCommitFlag()
    {
        _canCommitFinalState = false;
    }

    private void ClearRuntimeRefs()
    {
        _rigRefs = null;

        _root = null;
        _castTransform = null;
        _image = null;
        _rootCanvasGroup = null;

        _materialRuntime = null;

        _resolvedSprite = null;
        _resolvedLayout = CharacterEmojiLayout.Default;
        _resolvedVisualPreset = null;

        _isHideRequest = false;
        _resolveAttempted = false;
        _canCommitFinalState = false;
    }
}