using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Place Character Emoji", Order = -699)]
public sealed class PlaceCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Emoji Identity")]
    [Tooltip("Library의 기본 placement를 사용할 때 참조할 emojiKey입니다. overridePlacement=true면 비워도 됩니다.")]
    public string emojiKey;

    [Header("Rig Targets")]
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

    [Header("Apply")]
    public bool resetCastTransform = true;
    public bool applyScaleAndRotation = true;
    public bool applyImageSettings = true;
}

// Responsibility:
// - CharacterFocusPoint 기준으로 Emoji CastTransform의 base 위치를 잡는다.
// - placement의 base scale/rotation/image setting을 적용한다.
// - sprite/material/reveal/fade/motion은 다른 command가 담당한다.
public sealed class PlaceCharacterEmojiCommandCharR : CommandBase
{
    private readonly PlaceCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    private CharacterRigRefs _rigRefs;
    private RectTransform _castTransform;
    private Image _image;

    private CharacterEmojiPlacement _resolvedPlacement;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public PlaceCharacterEmojiCommandCharR(
        PlaceCharacterEmojiCommandSpecCharR spec,
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

        if (!HasValidRefs())
            yield break;

        ClaimTarget();
        CommitFinalState(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasValidRefs())
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

        _castTransform = _rigRefs.GetRect(_spec.castTarget);
        _image = _rigRefs.GetImage(_spec.imageTarget);

        ResolvePlacement();
    }

    private void ResolvePlacement()
    {
        _resolvedPlacement = CharacterEmojiPlacement.Default;

        if (_spec.overridePlacement)
        {
            _resolvedPlacement = _spec.placement;
        }
        else if (_spec.useResolvedPlacement &&
                 _resolver != null &&
                 _resolver.TryResolve(
                     _spec.emojiKey,
                     out Sprite _,
                     out CharacterEmojiPlacement resolvedPlacement,
                     out CharacterEmojiVisualPresetSO _))
        {
            _resolvedPlacement = resolvedPlacement;
        }
        else
        {
            _resolvedPlacement = _spec.placement;
        }

        _resolvedPlacement.offsetFromFocusInRigSpace += _spec.commandOffsetInRigSpace;
    }

    private void ClaimTarget()
    {
        _castTransform?.DOKill(true);
        HasClaimedTarget = true;
    }

    private void CommitFinalState(CommandRunScope scope)
    {
        if (_spec.resetCastTransform)
            ResetCastTransform();

        ApplyPlacement(scope);
        ApplyImageSettings();

        HasClaimedTarget = false;
    }

    private void ApplyPlacement(CommandRunScope scope)
    {
        if (TryResolveFocusAnchoredPosition(scope, out Vector2 anchoredPosition))
            _castTransform.anchoredPosition = anchoredPosition;
        else
            _castTransform.anchoredPosition = Vector2.zero;

        if (!_spec.applyScaleAndRotation)
            return;

        _castTransform.localScale = _resolvedPlacement.localScale;
        _castTransform.localRotation = Quaternion.Euler(0f, 0f, _resolvedPlacement.rotationZ);
    }

    private void ApplyImageSettings()
    {
        if (!_spec.applyImageSettings || _image == null)
            return;

        _image.preserveAspect = _resolvedPlacement.preserveAspect;

        if (_resolvedPlacement.setNativeSize && _image.sprite != null)
            _image.SetNativeSize();
    }

    private bool TryResolveFocusAnchoredPosition(CommandRunScope scope, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (_focusTuningDb == null || _rigRefs == null || _castTransform == null || _castTransform.parent == null)
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

        if (parent == null)
            return false;

        if (_spec.useSettledPlacementTargets &&
            _rigRefs.PlacementTargets != null)
        {
            anchoredPosition =
                _rigRefs.PlacementTargets.WorldPointToSettledParentLocalPoint(
                    parent,
                    targetWorld,
                    focusResult.RigSpaceRoot);
        }
        else
        {
            Vector3 targetInParentLocal = parent.InverseTransformPoint(targetWorld);

            anchoredPosition = new Vector2(
                targetInParentLocal.x,
                targetInParentLocal.y);
        }

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
        return _rigRefs != null && _castTransform != null;
    }
}
