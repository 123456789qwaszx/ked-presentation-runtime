using UnityEngine;

// 캐릭터의 특정 포커스 지점이,
// 현재 Presentation RigSpaceRoot 좌표계에서 어디에 있는지 측정한 값.
public struct CharacterFocusPointResult
{
    // FocusPointInRigSpace가 소속된 기준 좌표계.
    public RectTransform RigSpaceRoot;

    // Final focus estimate in RigSpaceRoot local space.
    public Vector2 FocusPointInRigSpace;
}

public static class CharacterFocusPointResolver
{
    public static void TryResolve(
        CommandRunScope scope,
        string roleKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        out CharacterFocusPointResult result)
    {
        result = default;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, roleKey);
        scope.CharacterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs);
        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, roleKey);

        IShotResponseStageProvider stageProvider = UIManager.Instance.GetUI<PresentationUIRoot>();

        TryResolveFromRigRefs(
            rigRefs,
            stageProvider.RigSpaceRoot,
            tuningKey,
            preset,
            commandOffset,
            tuningDb,
            useSettledPlacementTargets,
            out result);
    }

    public static bool TryResolveFromRigRefs(
        CharacterRigRefs rigRefs,
        RectTransform rigSpaceRoot,
        string tuningKey,
        CharacterFocusPreset preset,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        bool useSettledPlacementTargets,
        out CharacterFocusPointResult result)
    {
        result = default;

        // Focus는 "캐릭터의 논리적 위치"를 가리켜야 한다.
        // 측정 기준은 framing response 출력보다 위,
        // 즉 placement 축 마지막 노드인 CharSlot_Size를 사용한다.
        RectTransform measureRect = rigRefs.CharSlot_Size;

        Vector2 focusOffset = tuningDb.ResolveOffset(tuningKey, preset, commandOffset);
        Vector3 focusLocalOffset = new(focusOffset.x, focusOffset.y, 0f);

        // 움직이는 placement 조상(translation/scale/rotation)이 있으면,
        // 그들이 settled target에 도착했을 때의 focusWorld로 보정해 측정한다.
        // 아무도 안 움직이면 라이브 측정과 동일하다.
        Vector3 focusWorld =
            useSettledPlacementTargets && rigRefs.PlacementTargets != null
                ? rigRefs.PlacementTargets.MeasureSettledWorldPoint(
                    measureRect,
                    focusLocalOffset,
                    rigSpaceRoot)
                : measureRect.TransformPoint(focusLocalOffset);

        Vector3 focusInRigSpace =
            rigSpaceRoot.InverseTransformPoint(focusWorld);

        result = new CharacterFocusPointResult
        {
            RigSpaceRoot = rigSpaceRoot,
            FocusPointInRigSpace = new Vector2(
                focusInRigSpace.x,
                focusInRigSpace.y),
        };

        return true;
    }
}