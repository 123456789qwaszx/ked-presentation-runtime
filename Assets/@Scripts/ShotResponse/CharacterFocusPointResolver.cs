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

        if (rigRefs == null || rigRefs.Character_CastTransform == null)
            return false;

        RectTransform focusRect = rigRefs.Character_CastTransform;

        ICameraFocusStageRootProvider cameraFocusStageRootProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (cameraFocusStageRootProvider == null || cameraFocusStageRootProvider.StageRoot == null)
            return false;

        RectTransform stageRoot = cameraFocusStageRootProvider.StageRoot;

        string tuningKey = CharacterFocusTuningResolver.BuildTuningKey(roleKey, poseKey);

        Vector2 focusOffset =
            CharacterFocusTuningResolver.ResolveOffset(
                tuningDb,
                tuningKey,
                preset,
                customPointKey,
                commandOffset);

        Vector3 world = focusRect.TransformPoint(
            new Vector3(focusOffset.x, focusOffset.y, 0f));

        Vector3 local = stageRoot.InverseTransformPoint(world);

        result = new CharacterFocusPointResult
        {
            StageRoot = stageRoot,
            FocusRect = focusRect,
            FocusOffsetInFocusRectSpace = focusOffset,
            FocusPointInStageSpace = new Vector2(local.x, local.y)
        };

        return true;
    }
}