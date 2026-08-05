using System.Collections.Generic;
using UnityEngine;

public readonly struct CharacterDepthResult
{
    public readonly Vector2 RawDepthYAnchoredPosition;
    public readonly Vector2 DepthScale;
    public readonly CharacterFocusPreset PreserveFocusPreset;
    public readonly Vector2 PreserveFocusOffset;

    public CharacterDepthResult(
        Vector2 rawDepthYAnchoredPosition,
        Vector2 depthScale,
        CharacterFocusPreset preserveFocusPreset,
        Vector2 preserveFocusOffset)
    {
        RawDepthYAnchoredPosition = rawDepthYAnchoredPosition;
        DepthScale = depthScale;
        PreserveFocusPreset = preserveFocusPreset;
        PreserveFocusOffset = preserveFocusOffset;
    }
}

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

    // baseline은 targetDepthY가 아니라 현재/settled FocusPoint다.
    // 따라서 place_focus가 이미 잡아둔 위치를 기준으로 보존한다.
    //
    // U13-b-5: 종전에는 목표 depth를 실제 트랜스폼에 잠깐 적용해 측정하고 되돌렸다
    // (마지막 "적용→측정→복원" 트릭 — 예외가 나면 리그가 더러워졌다).
    // 이제 정착 체인을 한 번 캡처해 코어 수학(SettledFocusMath)으로 직접 푼다.
    // 유니티에는 아무것도 쓰지 않는다.
    public static void CalculateDepthYThatPreservesCurrentFocus(
        CommandRunScope scope,
        IShotResponseStageProvider stageProvider,
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

        RectTransform rigSpaceRoot = stageProvider?.RigSpaceRoot;

        if (rigSpaceRoot == null || depthYRect == null || depthScaleRect == null)
            return;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, roleKey);
        scope.CharacterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs);
        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, roleKey);

        if (!CharacterFocusPointResolver.TryResolveFocusMeasureInputs(
                rigRefs,
                tuningKey,
                preserveFocusPreset,
                preserveFocusOffset,
                focusTuningDb,
                CharacterFocusPointResolver.ResolveFacing(scope, roleKey),
                mirrorCommandOffset: true,
                out RectTransform measureRect,
                out Vector3 focusLocalOffset))
        {
            return;
        }

        List<RectTransform> chainRects = new();

        Ked.Presentation.Core.RectNodeState[] chain =
            rigRefs.PlacementTargets.CaptureSettledChain(measureRect, rigSpaceRoot, chainRects);

        int depthYIndex = chainRects.IndexOf(depthYRect);
        int depthScaleIndex = chainRects.IndexOf(depthScaleRect);

        if (depthYIndex < 0 || depthScaleIndex < 0)
        {
            // depth 축이 측정 체인의 조상이 아니다 — 리그 구성이 스키마와 다르다는 뜻이라
            // 조용히 보정을 생략하지 않고 알린다. (rawDepthY 그대로 반환)
            Debug.LogWarning(
                $"[CharacterDepthResolver] depth 축이 focus 측정 체인에 없다. " +
                $"depthY={depthYRect.name}({depthYIndex}), depthScale={depthScaleRect.name}({depthScaleIndex}). " +
                "focus 보존 보정을 건너뛴다 — 리그 구성을 확인할 것.");
            return;
        }

        Ked.Presentation.Core.Vec2 solved =
            Ked.Presentation.Core.SettledFocusMath.SolveDepthYPreservingFocus(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(rigSpaceRoot),
                depthYIndex,
                depthScaleIndex,
                new Ked.Presentation.Core.Vec2(focusLocalOffset.x, focusLocalOffset.y),
                new Ked.Presentation.Core.Vec2(rawDepthY.x, rawDepthY.y),
                new Ked.Presentation.Core.Vec2(targetDepthScale.x, targetDepthScale.y));

        finalDepthY = new Vector2(solved.X, solved.Y);
    }
}