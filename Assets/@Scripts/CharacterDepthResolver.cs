using UnityEngine;

public static class CharacterDepthResolver
{
    public static bool TryResolveRawDepth(
        CommandRunScope scope,
        string roleKey,
        CharacterDepthPreset preset,
        bool useLevel,
        float level,
        CharacterDepthTuningSO globalTuning,
        RoleDepthTuningDBSO roleTuningDb,
        bool overridePreserveFocus,
        CharacterFocusPreset preserveFocusPresetOverride,
        string preserveCustomFocusKeyOverride,
        Vector2 preserveFocusOffsetOverride,
        Vector2 commandYOffsetAdd,
        float commandScaleMultiplier,
        out CharacterDepthResult result)
    {
        result = default;

        if (scope == null)
            return false;

        CharacterDepthPresetValue value =
            ResolveBaseValue(
                preset,
                useLevel,
                level,
                globalTuning);

        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(
                scope,
                roleKey);

        ApplyRoleCorrection(
            ref value,
            preset,
            roleTuningDb,
            tuningKey);

        if (overridePreserveFocus)
        {
            value.preserveFocusPreset = preserveFocusPresetOverride;
            value.preserveCustomFocusKey = preserveCustomFocusKeyOverride ?? "";
            value.preserveFocusOffset = preserveFocusOffsetOverride;
        }

        value.depthY += commandYOffsetAdd;

        if (commandScaleMultiplier > 0f)
            value.depthScale *= commandScaleMultiplier;

        value.depthScale = Mathf.Max(0.0001f, value.depthScale);

        result = new CharacterDepthResult(
            value.depthY,
            new Vector2(value.depthScale, value.depthScale),
            value.preserveFocusPreset,
            value.preserveCustomFocusKey ?? "",
            value.preserveFocusOffset);

        return true;
    }

    public static bool CalculateDepthYThatPreservesCurrentFocus(
        CommandRunScope scope,
        string roleKey,
        RectTransform depthYRect,
        RectTransform depthScaleRect,
        Vector2 rawDepthY,
        Vector2 targetDepthScale,
        CharacterFocusPreset preserveFocusPreset,
        string preserveCustomFocusKey,
        Vector2 preserveFocusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        out Vector2 finalDepthY)
    {
        finalDepthY = rawDepthY;

        if (scope == null || depthYRect == null || depthScaleRect == null)
            return false;

        if (preserveFocusPreset == CharacterFocusPreset.Custom &&
            string.IsNullOrWhiteSpace(preserveCustomFocusKey))
        {
            return false;
        }

        // 1. baseline은 targetDepthY가 아니라 현재/settled FocusPoint다.
        //    따라서 place_focus가 이미 잡아둔 위치를 기준으로 보존한다.
        if (!CharacterFocusPointResolver.TryResolve(
                scope,
                roleKey,
                preserveFocusPreset,
                preserveCustomFocusKey,
                preserveFocusOffset,
                focusTuningDb,
                useSettledPlacementTargets: true,
                out CharacterFocusPointResult currentFocus))
        {
            return false;
        }

        // 2. rawDepthY + targetDepthScale을 잠깐 실제 transform에 적용해 target focus를 측정한다.
        //    이 함수는 호출 후 즉시 원복한다.
        if (!TryMeasureFocusWithTemporaryDepthTransform(
                scope,
                roleKey,
                depthYRect,
                depthScaleRect,
                rawDepthY,
                targetDepthScale,
                preserveFocusPreset,
                preserveCustomFocusKey,
                preserveFocusOffset,
                focusTuningDb,
                out CharacterFocusPointResult targetFocus))
        {
            return false;
        }

        RectTransform stageRoot = currentFocus.StageRoot;
        RectTransform depthYParent = depthYRect.parent as RectTransform;

        if (stageRoot == null || depthYParent == null)
            return false;

        Vector2 compensationInStageSpace =
            currentFocus.FocusPointInStageSpace -
            targetFocus.FocusPointInStageSpace;

        Vector2 compensationInDepthYParentSpace =
            PresentationCoordinateMath.ConvertVectorFromRigSpaceToTargetPositionParentSpace(
                compensationInStageSpace,
                stageRoot,
                depthYParent);

        finalDepthY = rawDepthY + compensationInDepthYParentSpace;
        return true;
    }

    private static bool TryMeasureFocusWithTemporaryDepthTransform(
        CommandRunScope scope,
        string roleKey,
        RectTransform depthYRect,
        RectTransform depthScaleRect,
        Vector2 depthYTarget,
        Vector2 depthScaleTarget,
        CharacterFocusPreset preserveFocusPreset,
        string preserveCustomFocusKey,
        Vector2 preserveFocusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        out CharacterFocusPointResult result)
    {
        result = default;

        if (scope == null || depthYRect == null || depthScaleRect == null)
            return false;

        Vector2 savedDepthY = depthYRect.anchoredPosition;
        Vector3 savedDepthScale = depthScaleRect.localScale;

        depthYRect.anchoredPosition = depthYTarget;
        depthScaleRect.localScale = new Vector3(
            depthScaleTarget.x,
            depthScaleTarget.y,
            savedDepthScale.z);

        bool success =
            CharacterFocusPointResolver.TryResolve(
                scope,
                roleKey,
                preserveFocusPreset,
                preserveCustomFocusKey,
                preserveFocusOffset,
                focusTuningDb,
                useSettledPlacementTargets: true,
                out result);

        depthScaleRect.localScale = savedDepthScale;
        depthYRect.anchoredPosition = savedDepthY;

        return success;
    }

    private static CharacterDepthPresetValue ResolveBaseValue(
        CharacterDepthPreset preset,
        bool useLevel,
        float level,
        CharacterDepthTuningSO globalTuning)
    {
        if (globalTuning == null)
        {
            if (useLevel)
                return ResolveDefaultLevel(level);

            return CharacterDepthTuningSet.Default.Get(preset);
        }

        return useLevel
            ? globalTuning.ResolveLevel(level)
            : globalTuning.ResolvePreset(preset);
    }

    private static CharacterDepthPresetValue ResolveDefaultLevel(float level)
    {
        float t = Mathf.Clamp01(level / 10f);

        float y = Mathf.Lerp(120f, -440f, t);
        float scale = Mathf.Lerp(0.86f, 1.38f, t);

        CharacterFocusPreset preserveFocus =
            level <= 2.5f
                ? CharacterFocusPreset.Feet
                : level <= 8.5f
                    ? CharacterFocusPreset.Bust
                    : CharacterFocusPreset.Face;

        return new CharacterDepthPresetValue
        {
            depthY = new Vector2(0f, y),
            depthScale = scale,
            preserveFocusPreset = preserveFocus,
            preserveCustomFocusKey = "",
            preserveFocusOffset = Vector2.zero,
        };
    }

    private static void ApplyRoleCorrection(
        ref CharacterDepthPresetValue value,
        CharacterDepthPreset preset,
        RoleDepthTuningDBSO roleTuningDb,
        string tuningKey)
    {
        if (roleTuningDb == null)
            return;

        if (!roleTuningDb.TryGet(tuningKey, out RoleDepthTuningDBSO.Entry entry))
            return;

        if (entry == null)
            return;

        value.depthY += entry.defaultYOffsetAdd;

        if (entry.defaultScaleMultiplier > 0f)
            value.depthScale *= entry.defaultScaleMultiplier;

        CharacterDepthPresetCorrection correction =
            entry.corrections.Get(preset);

        value.depthY += correction.yOffsetAdd;
        value.depthScale *= correction.SafeScaleMultiplier;

        if (correction.overridePreserveFocus)
        {
            value.preserveFocusPreset = correction.preserveFocusPreset;
            value.preserveCustomFocusKey = correction.preserveCustomFocusKey ?? "";
        }

        value.preserveFocusOffset += correction.preserveFocusOffsetAdd;
    }
}