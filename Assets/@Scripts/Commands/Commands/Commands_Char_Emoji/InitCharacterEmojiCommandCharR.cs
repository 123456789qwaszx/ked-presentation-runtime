using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Init Character Emoji", Order = -701)]
public sealed class InitCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Emoji Identity")]
    public string emojiKey;

    [Header("Rig Targets")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.EmojiSlot00_Root;
    public CharacterRigTarget castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset;
    public CharacterRigTarget baseSizeTarget = CharacterRigTarget.EmojiSlot00_Size;
    public CharacterRigTarget baseRotationTarget = CharacterRigTarget.EmojiSlot00_BaseRotation;
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Motion Reset Targets")]
    public CharacterRigTarget trackMoveTarget = CharacterRigTarget.EmojiSlot00_Track_Move;
    public CharacterRigTarget trackXTarget = CharacterRigTarget.EmojiSlot00_Track_Move_X;
    public CharacterRigTarget trackYTarget = CharacterRigTarget.EmojiSlot00_Track_Move_Y;
    public CharacterRigTarget effectTarget = CharacterRigTarget.EmojiSlot00_Effect;
    public CharacterRigTarget scaleTarget = CharacterRigTarget.EmojiSlot00_Scale;
    public CharacterRigTarget swayPivotTarget = CharacterRigTarget.EmojiSlot00_SwayPivot;
    public CharacterRigTarget rotationTarget = CharacterRigTarget.EmojiSlot00_Rotation;

    [Header("Image")]
    public bool preserveAspect = true;
    public bool setNativeSize = false;

    [Header("Reveal Initial State")]
    [Range(0f, 1f)]
    public float initialReveal = 0f;

    [Header("Reset")]
    public bool resetMotionAxes = true;
}

public sealed class InitCharacterEmojiCommandCharR : CharacterEmojiCommandBase
{
    private const string EmojiMaterialInstanceSuffix = " (Emoji Instance)";

    private readonly InitCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;
    private readonly CharacterEmojiVisualPresetSO _visualPreset;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;
    private readonly IShotResponseStageProvider _stageProvider;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private CharacterRigRefs _rigRefs;

    public InitCharacterEmojiCommandCharR(
        InitCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver,
        CharacterEmojiVisualPresetSO visualPreset,
        CharacterFocusTuningDBSO focusTuningDb,
        IShotResponseStageProvider stageProvider)
    {
        _spec = spec;
        _resolver = resolver;
        _visualPreset = visualPreset;
        _focusTuningDb = focusTuningDb;
        _stageProvider = stageProvider;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        if (!TryResolveSprite(out Sprite sprite))
            yield break;

        _resolver.TryResolvePlacement(_spec.emojiKey, out CharacterEmojiPlacement placement);
        CharacterEmojiMirrorContext mirrorContext = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        RectTransform root = _rigRefs.GetRect(_spec.rootTarget);
        RectTransform castTransform = _rigRefs.GetRect(_spec.castTarget);
        RectTransform baseSizeTransform = _rigRefs.GetRect(_spec.baseSizeTarget);
        RectTransform baseRotationTransform = _rigRefs.GetRect(_spec.baseRotationTarget);
        Image image = _rigRefs.GetImage(_spec.imageTarget);

        DOTween.Kill(image, true);

        root.GetComponent<CanvasGroup>().alpha = 1f;

        image.sprite = sprite;
        image.preserveAspect = _spec.preserveAspect;

        if (_spec.setNativeSize)
            image.SetNativeSize();

        ApplySpriteMirror(image, mirrorContext);
        ApplyMaterial(image);
        ApplyPlacement(
            scope,
            castTransform,
            baseSizeTransform,
            baseRotationTransform,
            placement,
            mirrorContext);

        if (_spec.resetMotionAxes)
            ResetMotionAxes();

        yield break;
    }

    private bool TryResolveSprite(out Sprite sprite)
    {
        sprite = null;

        if (_resolver.TryResolveSprite(_spec.emojiKey, out sprite))
            return true;

        Debug.LogWarning(
            $"[InitCharacterEmojiCommandCharR] Failed to resolve emoji sprite. " +
            $"emojiKey='{_spec.emojiKey}', targetKey='{_spec.slotKey}'.");

        return false;
    }

    private void ApplyMaterial(Image image)
    {
        Material material = EnsureEmojiMaterial(image, _visualPreset.baseMaterial);

        material.SetFloat(CharacterEmojiShaderIds.Reveal, _spec.initialReveal);
        material.SetFloat(CharacterEmojiShaderIds.RevealSoftness, _visualPreset.revealSoftness);
        material.SetFloat(CharacterEmojiShaderIds.RevealDirection, GetDirectionValue(_visualPreset));

        material.SetFloat(CharacterEmojiShaderIds.EdgeRimAmount, _visualPreset.edgeRimAmount);
        material.SetFloat(CharacterEmojiShaderIds.EdgeRimWidth, _visualPreset.edgeRimWidth);
        material.SetColor(CharacterEmojiShaderIds.EdgeRimColor, _visualPreset.edgeRimColor);

        material.SetFloat(CharacterEmojiShaderIds.GlowAmount, _visualPreset.glowAmount);
        material.SetColor(CharacterEmojiShaderIds.GlowColor, _visualPreset.glowColor);
    }

    private static Material EnsureEmojiMaterial(Image image, Material baseMaterial)
    {
        Material currentMaterial = image.material;

        if (IsEmojiMaterialInstance(currentMaterial) && currentMaterial.shader == baseMaterial.shader)
            return currentMaterial;

        if (IsEmojiMaterialInstance(currentMaterial))
            Object.Destroy(currentMaterial);

        Material material = Object.Instantiate(baseMaterial);
        material.name = baseMaterial.name + EmojiMaterialInstanceSuffix;

        image.material = material;

        return material;
    }

    private static bool IsEmojiMaterialInstance(Material material)
    {
        return material != null &&
               material.name.Contains(EmojiMaterialInstanceSuffix);
    }

    private static float GetDirectionValue(CharacterEmojiVisualPresetSO preset)
    {
        return preset.revealDirection == CharacterEmojiRevealDirection.BottomToTop
            ? 1f
            : 0f;
    }

    private void ApplyPlacement(
        CommandRunScope scope,
        RectTransform castTransform,
        RectTransform baseSizeTransform,
        RectTransform baseRotationTransform,
        CharacterEmojiPlacement placement,
        CharacterEmojiMirrorContext mirrorContext)
    {
        TryResolveFocusAnchoredPosition(
            scope,
            castTransform,
            placement,
            mirrorContext,
            out Vector2 anchoredPosition);

        // CastTransform은 이제 "어디에 뜨는가"만 담당한다.
        castTransform.DOKill(true);
        castTransform.anchoredPosition = anchoredPosition;
        castTransform.localScale = Vector3.one;
        castTransform.localRotation = Quaternion.identity;

        // emoji별 기본 크기는 BaseSize에만 적용한다.
        baseSizeTransform.DOKill(true);
        baseSizeTransform.anchoredPosition = Vector2.zero;
        baseSizeTransform.localScale = placement.localScale;
        baseSizeTransform.localRotation = Quaternion.identity;

        // emoji별 기본 기울기는 BaseRotation에만 적용한다.
        // placement mirror가 켜져 있으면 위치 offset과 같은 계약으로 rotationZ도 좌우 대칭한다.
        // 예: Right-facing 기준 +16도는 Left-facing에서 -16도가 된다.
        float rotationZ = mirrorContext.MirrorPlacementRotationZ(placement.rotationZ);

        baseRotationTransform.DOKill(true);
        baseRotationTransform.anchoredPosition = Vector2.zero;
        baseRotationTransform.localScale = Vector3.one;
        baseRotationTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    private bool TryResolveFocusAnchoredPosition(
        CommandRunScope scope,
        RectTransform castTransform,
        CharacterEmojiPlacement placement,
        CharacterEmojiMirrorContext mirrorContext,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        IShotResponseStageProvider stageProvider = _stageProvider;

        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(
                scope,
                _spec.slotKey);

        bool mirrorPlacementOffset =
            mirrorContext.profile != null &&
            mirrorContext.profile.placementMirror == CharacterEmojiPlacementMirrorPolicy.MirrorWithCharacterFacing;

        CharacterFocusPointResolver.TryResolveFromRigRefs(
            _rigRefs,
            stageProvider.RigSpaceRoot,
            tuningKey,
            placement.focusPreset,
            placement.offsetFromFocusInRigSpace,
            _focusTuningDb,
            true,
            mirrorContext.facing,
            mirrorPlacementOffset,
            out CharacterFocusPointResult focusResult);

        Vector3 targetWorld =
            focusResult.RigSpaceRoot.TransformPoint(
                new Vector3(
                    focusResult.FocusPointInRigSpace.x,
                    focusResult.FocusPointInRigSpace.y,
                    0f));

        RectTransform parent = castTransform.parent as RectTransform;

        anchoredPosition =
            _rigRefs.PlacementTargets.WorldPointToSettledParentLocalPoint(
                parent,
                targetWorld,
                focusResult.RigSpaceRoot);

        return true;
    }

    private void ResetMotionAxes()
    {
        ResetAnchoredPosition(_rigRefs.GetRect(_spec.trackMoveTarget));
        ResetAnchoredPosition(_rigRefs.GetRect(_spec.trackXTarget));
        ResetAnchoredPosition(_rigRefs.GetRect(_spec.trackYTarget));

        ResetLocalTransform(_rigRefs.GetRect(_spec.effectTarget));

        ResetScale(_rigRefs.GetRect(_spec.scaleTarget));
        ResetRotation(_rigRefs.GetRect(_spec.swayPivotTarget));
        ResetRotation(_rigRefs.GetRect(_spec.rotationTarget));
    }

    private static void ResetAnchoredPosition(RectTransform rect)
    {
        rect.DOKill(true);
        rect.anchoredPosition = Vector2.zero;
    }

    private static void ResetScale(RectTransform rect)
    {
        rect.DOKill(true);
        rect.localScale = Vector3.one;
    }

    private static void ResetRotation(RectTransform rect)
    {
        rect.DOKill(true);
        rect.localRotation = Quaternion.identity;
    }

    private static void ResetLocalTransform(RectTransform rect)
    {
        rect.DOKill(true);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}