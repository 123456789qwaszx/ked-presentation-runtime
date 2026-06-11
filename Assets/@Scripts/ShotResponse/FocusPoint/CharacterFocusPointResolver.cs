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

        // 1. 현재 라이브 transform 기준 focusWorld.
        Vector3 focusWorld =
            measureRect.TransformPoint(
                new Vector3(focusOffset.x, focusOffset.y, 0f));

        // 2. 현재 움직이는 placement 조상들이 있다면,
        //    그 조상들이 settled target에 도착했을 때의 focusWorld로 보정한다.
        if (useSettledPlacementTargets && rigRefs.PlacementTargets != null)
        {
            focusWorld += rigRefs.PlacementTargets.AccumulateResidualWorldDisplacement(
                measureRect,
                stageRoot);
        }

        Vector3 focusInStage =
            stageRoot.InverseTransformPoint(focusWorld);

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