using UnityEngine;

public static class CharacterFocusPointResolver
{
    public static bool TryResolve(
        CommandRunScope scope,
        string roleKey,
        CharacterFocusPreset preset,
        string poseKey,
        string customPointKey,
        Vector2 commandOffset,
        CharacterFocusTuningDBSO tuningDb,
        out CharacterFocusPointResult result)
    {
        result = default;

        if (scope == null)
            return false;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, roleKey);

        if (!scope.characterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs))
            return false;

        if (rigRefs == null)
            return false;

        // Focus는 "캐릭터의 논리적 위치"를 가리켜야 한다.
        // 측정 기준은 반드시 framing response 출력(FramingTransform / FramingScale)보다 위,
        // 즉 placement 축 마지막 노드인 CharSlot_Scale 이어야 한다.
        //
        // 이 노드는 bake가 basePositionInRigSpace를 뜨는 MeasureRect와 동일하다.
        // 덕분에 측정값에는 placement / 캐릭터 스케일만 들어가고, 카메라 자기 반응(pan*panResponse,
        // zoomSpread, framing scale)은 섞이지 않는다.
        // CastTransform처럼 response 아래에서 재면 pan/zoom 결과가 측정에 되먹임되어
        // 롤백 재시크마다 값이 누적으로 흘러간다(비멱등).
        RectTransform measureRect = rigRefs.CharSlot_Scale;

        if (measureRect == null)
            return false;

        ICameraFocusStageRootProvider stageRootProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (stageRootProvider == null || stageRootProvider.StageRoot == null)
            return false;

        RectTransform stageRoot = stageRootProvider.StageRoot;

        // 마커 표시용 부모. focus 계산과는 무관하므로 기존대로 둔다.
        RectTransform previewRoot = rigRefs.Character_ExtensionsRoot != null
            ? rigRefs.Character_ExtensionsRoot
            : rigRefs.Character_CastTransform;

        string tuningKey = CharacterFocusTuningResolver.BuildTuningKey(
            roleKey,
            poseKey);

        Vector2 focusOffset =
            CharacterFocusTuningResolver.ResolveOffset(
                tuningDb,
                tuningKey,
                preset,
                customPointKey,
                commandOffset);

        // offset은 CharSlot_Scale 로컬 공간 기준.
        // 캐릭터 placement 스케일에는 반응하지만 framing response에는 반응하지 않는다.
        Vector3 focusWorld = measureRect.TransformPoint(
            new Vector3(focusOffset.x, focusOffset.y, 0f));

        Vector3 focusInStage = stageRoot.InverseTransformPoint(focusWorld);

        result = new CharacterFocusPointResult
        {
            StageRoot = stageRoot,
            FocusRect = measureRect,
            PreviewRoot = previewRoot,
            FocusOffsetInFocusRectSpace = focusOffset,
            FocusPointInStageSpace = new Vector2(focusInStage.x, focusInStage.y)
        };

        return true;
    }
}