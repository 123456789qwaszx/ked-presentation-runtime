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

    [Header("Rig Targets")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
    public CharacterRigTarget castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform;
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Placement")]
    public bool useResolvedPlacement = true;
    public bool overridePlacement = false;

    public CharacterEmojiPlacement placement = CharacterEmojiPlacement.Default;

    [Tooltip("Library/override placement 위에 추가로 더하는 임시 RigSpace 오프셋입니다.")]
    public Vector2 commandOffsetInRigSpace = Vector2.zero;

    [Tooltip("진행 중인 place/depth 계열 커맨드가 있으면 settled target 기준으로 focus point를 측정합니다.")]
    public bool useSettledPlacementTargets = true;

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
}

public sealed class SetCharacterEmojiCommandCharR : CommandBase
{
    private readonly SetCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _root;
    private RectTransform _castTransform;
    private Image _image;
    private CanvasGroup _rootCanvasGroup;
    private CharacterEmojiMaterialRuntime _materialRuntime;

    private Sprite _resolvedSprite;
    private CharacterEmojiPlacement _resolvedPlacement;
    private CharacterEmojiVisualPresetSO _resolvedVisualPreset;

    private bool _hasResolvedEmoji;
    private bool _resolveAttempted;

    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetCharacterEmojiCommandCharR(
        SetCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _resolver = resolver;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasValidRefs() || !_hasResolvedEmoji)
            yield break;

        ClaimTarget();
        CommitFinalState(scope);

        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasValidRefs() || !_hasResolvedEmoji)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState(scope);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        if (_rigRefs == null)
            return;

        _root = _rigRefs.GetRect(_spec.rootTarget);
        _castTransform = _rigRefs.GetRect(_spec.castTarget);
        _image = _rigRefs.GetImage(_spec.imageTarget);
        _materialRuntime = _rigRefs.GetEmojiMaterialRuntime(_spec.imageTarget);

        if (_root != null)
            _root.TryGetComponent(out _rootCanvasGroup);

        ResolveEmoji();
    }

    private void ClaimTarget()
    {
        _materialRuntime?.KillTween(true);
        _rootCanvasGroup?.DOKill(true);
        _castTransform?.DOKill(true);

        HasClaimedTarget = true;
    }

    private void CommitFinalState(CommandRunScope scope)
    {
        ApplySpriteAndPlacement(scope);
        ApplyEmojiMaterialInitialState();

        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.alpha = _spec.alpha;
            _rootCanvasGroup.interactable = true;
            _rootCanvasGroup.blocksRaycasts = true;
        }

        HasClaimedTarget = false;
    }

    private void ResolveEmoji()
    {
        _hasResolvedEmoji = false;
        _resolvedSprite = null;
        _resolvedPlacement = CharacterEmojiPlacement.Default;
        _resolvedVisualPreset = null;

        if (_resolver != null &&
            _resolver.TryResolve(
                _spec.emojiKey,
                out Sprite sprite,
                out CharacterEmojiPlacement resolvedPlacement,
                out _resolvedVisualPreset))
        {
            _hasResolvedEmoji = true;
            _resolvedSprite = sprite;

            if (_spec.overridePlacement)
                _resolvedPlacement = _spec.placement;
            else if (_spec.useResolvedPlacement)
                _resolvedPlacement = resolvedPlacement;
            else
                _resolvedPlacement = CharacterEmojiPlacement.Default;

            _resolvedPlacement.offsetFromFocusInRigSpace += _spec.commandOffsetInRigSpace;
            return;
        }

        Debug.LogWarning(
            $"[SetCharacterEmojiCommandCharR] Failed to resolve emoji sprite. " +
            $"emojiKey='{_spec.emojiKey}', targetKey='{_spec.slotKey}'.");
    }

    private void ApplySpriteAndPlacement(CommandRunScope scope)
    {
        if (_root != null && !_root.gameObject.activeSelf)
            _root.gameObject.SetActive(true);

        _image.gameObject.SetActive(true);
        _image.enabled = true;
        _image.sprite = _resolvedSprite;
        _image.preserveAspect = _resolvedPlacement.preserveAspect;

        if (_spec.resetCastTransform)
            ResetCastTransform();

        ApplyPlacement(scope);

        if (_resolvedPlacement.setNativeSize && _image.sprite != null)
            _image.SetNativeSize();
    }

    private void ApplyEmojiMaterialInitialState()
    {
        CharacterEmojiVisualPresetSO preset = ResolveVisualPreset();

        if (preset == null || preset.baseMaterial == null || _materialRuntime == null)
            return;

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

    private void ApplyPlacement(CommandRunScope scope)
    {
        if (TryResolveFocusAnchoredPosition(scope, out Vector2 anchoredPosition))
            _castTransform.anchoredPosition = anchoredPosition;
        else
            _castTransform.anchoredPosition = Vector2.zero;

        _castTransform.localScale = _resolvedPlacement.localScale;
        _castTransform.localRotation = Quaternion.Euler(0f, 0f, _resolvedPlacement.rotationZ);
    }

    private bool TryResolveFocusAnchoredPosition(CommandRunScope scope, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (_focusTuningDb == null || _castTransform == null || _castTransform.parent == null)
            return false;

        IShotResponseStageProvider stageProvider = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (stageProvider == null || stageProvider.RigSpaceRoot == null)
            return false;

        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, _spec.slotKey);

        if (!CharacterFocusPointResolver.TryResolveFromRigRefs(
                _rigRefs,
                stageProvider.RigSpaceRoot,
                tuningKey,
                _resolvedPlacement.focusPreset,
                _resolvedPlacement.offsetFromFocusInRigSpace,
                _focusTuningDb,
                _spec.useSettledPlacementTargets,
                out CharacterFocusPointResult focusResult))
        {
            return false;
        }

        if (focusResult.RigSpaceRoot == null)
            return false;

        Vector3 targetWorld = focusResult.RigSpaceRoot.TransformPoint(
            new Vector3(
                focusResult.FocusPointInRigSpace.x,
                focusResult.FocusPointInRigSpace.y,
                0f));

        RectTransform parent = _castTransform.parent as RectTransform;
        Vector3 targetInParentLocal = parent.InverseTransformPoint(targetWorld);

        anchoredPosition = new Vector2(targetInParentLocal.x, targetInParentLocal.y);
        return true;
    }

    private void ResetCastTransform()
    {
        _castTransform.anchoredPosition = Vector2.zero;
        _castTransform.localScale = Vector3.one;
        _castTransform.localRotation = Quaternion.identity;
    }

    private bool HasValidRefs()
    {
        return
            _rigRefs != null &&
            _root != null &&
            _castTransform != null &&
            _image != null;
    }
}