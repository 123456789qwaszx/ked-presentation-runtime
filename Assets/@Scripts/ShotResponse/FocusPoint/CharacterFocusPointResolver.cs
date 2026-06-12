using UnityEngine;

public static class CharacterFocusPointResolver
{
    public static bool TryResolve(
        CommandRunScope scope,
        string roleKey,
        CharacterFocusPreset preset,
        string customPointKey,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        out CharacterFocusPointResult result)
    {
        return TryResolve(
            scope,
            roleKey,
            preset,
            customPointKey,
            commandOffset,
            tuningDb,
            useSettledPlacementTargets: true,
            out result);
    }

    public static bool TryResolve(
        CommandRunScope scope,
        string roleKey,
        CharacterFocusPreset preset,
        string customPointKey,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        out CharacterFocusPointResult result)
    {
        result = default;

        if (scope == null)
            return false;

        string resolvedRigKey =
            CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, roleKey);

        if (!scope.CharacterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs))
            return false;

        string tuningKey =
            CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, roleKey);

        // Focus는 "캐릭터의 논리적 위치"를 가리켜야 한다.
        // 측정 기준은 framing response 출력보다 위,
        // 즉 placement 축 마지막 노드인 CharSlot_Size를 사용한다.
        RectTransform measureRect = rigRefs.CharSlot_Size;

        IPresentationRigSpaceRootProvider stageRootProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (stageRootProvider == null || stageRootProvider.RigSpaceRoot == null)
            return false;

        RectTransform stageRoot = stageRootProvider.RigSpaceRoot;

        Vector2 focusOffset =
            CharacterFocusTuningResolver.ResolveOffset(
                tuningDb,
                tuningKey,
                preset,
                customPointKey,
                commandOffset);

        Vector3 focusLocalOffset = new(focusOffset.x, focusOffset.y, 0f);

        // 움직이는 placement 조상(translation/scale/rotation)이 있으면,
        // 그들이 settled target에 도착했을 때의 focusWorld로 보정해 측정한다.
        // 아무도 안 움직이면 라이브 측정과 동일하다.
        Vector3 focusWorld =
            useSettledPlacementTargets && rigRefs.PlacementTargets != null
                ? rigRefs.PlacementTargets.MeasureSettledWorldPoint(
                    measureRect,
                    focusLocalOffset,
                    stageRoot)
                : measureRect.TransformPoint(focusLocalOffset);

        Vector3 focusInStage = stageRoot.InverseTransformPoint(focusWorld);

        result = new CharacterFocusPointResult
        {
            StageRoot = stageRoot,
            FocusRect = measureRect,
            FocusOffsetInFocusRectSpace = focusOffset,
            FocusPointInStageSpace = new Vector2(focusInStage.x, focusInStage.y),
        };

        return true;
    }
}