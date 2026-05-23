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

        ICameraFocusStageRootProvider stageRootProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();

        if (stageRootProvider == null || stageRootProvider.StageRoot == null)
            return false;

        RectTransform focusRect = rigRefs.Character_CastTransform;
        RectTransform previewRoot = rigRefs.Character_ExtensionsRoot != null
            ? rigRefs.Character_ExtensionsRoot
            : rigRefs.Character_CastTransform;

        RectTransform stageRoot = stageRootProvider.StageRoot;

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

        Vector3 focusWorld = focusRect.TransformPoint(
            new Vector3(focusOffset.x, focusOffset.y, 0f));

        Vector3 focusInStage = stageRoot.InverseTransformPoint(focusWorld);

        result = new CharacterFocusPointResult
        {
            StageRoot = stageRoot,
            FocusRect = focusRect,
            PreviewRoot = previewRoot,
            FocusOffsetInFocusRectSpace = focusOffset,
            FocusPointInStageSpace = new Vector2(focusInStage.x, focusInStage.y)
        };

        return true;
    }
}