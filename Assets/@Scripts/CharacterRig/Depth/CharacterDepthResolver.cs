using UnityEngine;

public static class CharacterDepthResolver
{
    public static void ResolveRawDepth(
        CharacterDepthKey depthKey,
        bool useLevel,
        float level,
        CharacterDepthTuningSO globalTuning,
        CharacterFocusPreset focusPreset,
        Vector2 focusOffset,
        out CharacterDepthResult result)
    {
        CharacterDepthPresetValue value = useLevel
            ? globalTuning.ResolveLevel(level)
            : globalTuning.ResolvePreset(depthKey);

        value.preserveFocusPreset = focusPreset;
        value.preserveFocusOffset = focusOffset;

        result = new CharacterDepthResult(
            value.depthY,
            new Vector2(value.depthScale, value.depthScale),
            value.preserveFocusPreset,
            value.preserveFocusOffset);
    }

    public static bool CalculateDepthYThatPreservesCurrentFocus(
        CommandRunScope scope,
        string roleKey,
        RectTransform depthYRect,
        RectTransform depthScaleRect,
        Vector2 rawDepthY,
        Vector2 targetDepthScale,
        CharacterFocusPreset preserveFocusPreset,
        Vector2 preserveFocusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        out Vector2 finalDepthY)
    {
        finalDepthY = rawDepthY;

        // 1. baseline은 targetDepthY가 아니라 현재/settled FocusPoint다.
        //    따라서 place_focus가 이미 잡아둔 위치를 기준으로 보존한다.
        CharacterFocusPointResolver.TryResolve(
            scope,
            roleKey,
            preserveFocusPreset,
            preserveFocusOffset,
            focusTuningDb,
            useSettledPlacementTargets: true,
            out CharacterFocusPointResult currentFocus);

        // 2. rawDepthY + targetDepthScale을 잠깐 실제 transform에 적용해 target focus를 측정한다.
        //    이 함수는 호출 후 즉시 원복한다.
        TryMeasureFocusWithTemporaryDepthTransform(
            scope,
            roleKey,
            depthYRect,
            depthScaleRect,
            rawDepthY,
            targetDepthScale,
            preserveFocusPreset,
            preserveFocusOffset,
            focusTuningDb,
            out CharacterFocusPointResult targetFocus);

        RectTransform stageRoot = currentFocus.RigSpaceRoot;
        RectTransform depthYParent = depthYRect.parent as RectTransform;

        if (stageRoot == null || depthYParent == null)
            return false;

        Vector2 compensationInStageSpace =
            currentFocus.FocusPointInRigSpace -
            targetFocus.FocusPointInRigSpace;

        Vector2 compensationInDepthYParentSpace =
            PresentationCoordinateMath.ConvertVectorFromRigSpaceToTargetPositionParentSpace(
                compensationInStageSpace,
                stageRoot,
                depthYParent);

        finalDepthY = rawDepthY + compensationInDepthYParentSpace;
        return true;
    }

    private static void TryMeasureFocusWithTemporaryDepthTransform(
        CommandRunScope scope,
        string roleKey,
        RectTransform depthYRect,
        RectTransform depthScaleRect,
        Vector2 depthYTarget,
        Vector2 depthScaleTarget,
        CharacterFocusPreset preserveFocusPreset,
        Vector2 preserveFocusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        out CharacterFocusPointResult result)
    {
        result = default;

        Vector2 savedDepthY = depthYRect.anchoredPosition;
        Vector3 savedDepthScale = depthScaleRect.localScale;

        depthYRect.anchoredPosition = depthYTarget;
        depthScaleRect.localScale = new Vector3(
            depthScaleTarget.x,
            depthScaleTarget.y,
            savedDepthScale.z);

        CharacterFocusPointResolver.TryResolve(
            scope,
            roleKey,
            preserveFocusPreset,
            preserveFocusOffset,
            focusTuningDb,
            useSettledPlacementTargets: true,
            out result);

        depthScaleRect.localScale = savedDepthScale;
        depthYRect.anchoredPosition = savedDepthY;
    }
}