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
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
    public CharacterRigTarget castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform;
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Motion Reset Targets")]
    public CharacterRigTarget trackMoveTarget = CharacterRigTarget.EmojiSlot00_Track_Move;
    public CharacterRigTarget trackXTarget = CharacterRigTarget.EmojiSlot00_Track_X;
    public CharacterRigTarget trackYTarget = CharacterRigTarget.EmojiSlot00_Track_Y;
    public CharacterRigTarget scaleTarget = CharacterRigTarget.EmojiSlot00_Scale;
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

// Responsibility:
// - Emoji 표시를 위한 기본 상태를 한 번에 초기화.
// - root 표시, sprite, material preset, reveal 초기값, placement, motion axis reset 등.
public sealed class InitCharacterEmojiCommandCharR : CommandBase
{
    private const string EmojiMaterialInstanceSuffix = " (Emoji Instance)";

    private readonly InitCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;
    private readonly CharacterEmojiVisualPresetSO _visualPreset;
    private readonly CharacterFocusTuningDBSO _focusTuningDb;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private CharacterRigRefs _rigRefs;

    public InitCharacterEmojiCommandCharR(
        InitCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver,
        CharacterEmojiVisualPresetSO visualPreset,
        CharacterFocusTuningDBSO focusTuningDb)
    {
        _spec = spec;
        _resolver = resolver;
        _visualPreset = visualPreset;
        _focusTuningDb = focusTuningDb;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);

        if (!TryResolveSprite(out Sprite sprite))
            yield break;

        _resolver.TryResolvePlacement(_spec.emojiKey, out CharacterEmojiPlacement placement);

        RectTransform root = _rigRefs.GetRect(_spec.rootTarget);
        RectTransform castTransform = _rigRefs.GetRect(_spec.castTarget);
        Image image = _rigRefs.GetImage(_spec.imageTarget);

        DOTween.Kill(image, true);

        root.GetComponent<CanvasGroup>().alpha = 1f;
        
        image.sprite = sprite;
        image.preserveAspect = _spec.preserveAspect;

        if (_spec.setNativeSize)
            image.SetNativeSize();
        
        ApplyMaterial(image);
        ApplyPlacement(scope, castTransform, placement);

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
        CharacterEmojiPlacement placement)
    {
        TryResolveFocusAnchoredPosition(
            scope,
            castTransform,
            placement,
            out Vector2 anchoredPosition);

        castTransform.anchoredPosition = anchoredPosition;
        castTransform.localScale = placement.localScale;
        castTransform.localRotation = Quaternion.Euler(0f, 0f, placement.rotationZ);
    }

    private bool TryResolveFocusAnchoredPosition(
        CommandRunScope scope,
        RectTransform castTransform,
        CharacterEmojiPlacement placement,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        IShotResponseStageProvider stageProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(
                scope,
                _spec.slotKey);

        CharacterFocusPointResolver.TryResolveFromRigRefs(
            _rigRefs,
            stageProvider.RigSpaceRoot,
            tuningKey,
            placement.focusPreset,
            placement.offsetFromFocusInRigSpace,
            _focusTuningDb,
            true,
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

        ResetScale(_rigRefs.GetRect(_spec.scaleTarget));
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
}