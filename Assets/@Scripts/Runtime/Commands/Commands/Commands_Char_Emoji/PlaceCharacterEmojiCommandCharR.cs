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
    [Tooltip("Library의 기본 placement를 사용할 때 참조할 emojiKey.")]
    public string emojiKey;

    [Header("Rig Targets")]
    public CharacterRigTarget castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset;
}

public sealed class PlaceCharacterEmojiCommandCharR : CharacterEmojiCommandBase
{
    private readonly PlaceCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;
    private readonly IShotResponseStageProvider _stageProvider;

    private CharacterRigRefs _rigRefs;
    private RectTransform _castTransform;
    private Image _image;

    private CharacterEmojiPlacement _resolvedPlacement;
    private CharacterEmojiMirrorContext _mirrorContext;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public PlaceCharacterEmojiCommandCharR(
        PlaceCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver,
        CharacterFocusTuningDBSO focusTuningDb,
        IShotResponseStageProvider stageProvider)
    {
        _spec = spec;
        _resolver = resolver;
        _focusTuningDb = focusTuningDb;
        _stageProvider = stageProvider;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        ClaimTarget();
        CommitFinalState(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState(scope);
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _castTransform = _rigRefs.GetRect(_spec.castTarget);
        
        _resolveAttempted = true;
    }
    
    private void ClaimTarget()
    {
        _castTransform?.DOKill(true);
        
        if(_resolver.TryResolvePlacement(_spec.emojiKey, out CharacterEmojiPlacement resolvedPlacement))
            _resolvedPlacement = resolvedPlacement;

        HasClaimedTarget = true;
    }

    private void CommitFinalState(CommandRunScope scope)
    {
        _mirrorContext = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        TryResolveFocusAnchoredPosition(scope, out Vector2 anchoredPosition);
        _castTransform.anchoredPosition = anchoredPosition;

        _castTransform.localScale = _resolvedPlacement.localScale;

        // placement mirror가 켜져 있으면 static placement rotation도 좌우 대칭한다.
        // InitCharacterEmojiCommandCharR의 BaseRotation 처리와 같은 계약이다.
        float rotationZ = _mirrorContext.MirrorPlacementRotationZ(_resolvedPlacement.rotationZ);
        _castTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        HasClaimedTarget = false;
    }

    private bool TryResolveFocusAnchoredPosition(CommandRunScope scope, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        IShotResponseStageProvider stageProvider = _stageProvider;
        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, _spec.slotKey);

        bool mirrorPlacementOffset =
            _mirrorContext.profile != null &&
            _mirrorContext.profile.placementMirror == CharacterEmojiPlacementMirrorPolicy.MirrorWithCharacterFacing;

        CharacterFocusPointResolver.TryResolveFromRigRefs(
                _rigRefs,
                stageProvider.RigSpaceRoot,
                tuningKey,
                _resolvedPlacement.focusPreset,
                _resolvedPlacement.offsetFromFocusInRigSpace,
                _focusTuningDb,
                true,
                _mirrorContext.facing,
                mirrorPlacementOffset,
                out CharacterFocusPointResult focusResult);

        Vector3 targetWorld = focusResult.RigSpaceRoot.TransformPoint(
            new Vector3(
                focusResult.FocusPointInRigSpace.x,
                focusResult.FocusPointInRigSpace.y,
                0f));

        RectTransform parent = _castTransform.parent as RectTransform;
        anchoredPosition =
            _rigRefs.PlacementTargets.WorldPointToSettledParentLocalPoint(
                parent,
                targetWorld,
                focusResult.RigSpaceRoot);

        return true;
    }
}
