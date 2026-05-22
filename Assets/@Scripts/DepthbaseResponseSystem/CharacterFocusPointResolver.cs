using UnityEngine;

public struct CharacterFocusPointResult
{
    public RectTransform StageRoot;
    public RectTransform FocusRect;
    public Vector2 FocusPointInStageSpace;
}

public static class CharacterFocusPointResolver
{
    public static bool TryResolve(
        CommandRunScope scope,
        string roleKey,
        Vector2 localOffset,
        out CharacterFocusPointResult result)
    {
        result = default;

        scope.characterRigs.TryGetRig(roleKey, out CharacterRigRefs rigRefs);
        
        //***
        RectTransform focusRect = rigRefs.Character_CastTransform;

        ICameraFocusStageRootProvider cameraFocusStageRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
        RectTransform stageRoot = cameraFocusStageRootProvider.StageRoot ;

        Vector3 world = focusRect.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0f));
        Vector3 local = stageRoot.InverseTransformPoint(world);

        result = new CharacterFocusPointResult
        {
            StageRoot = stageRoot,
            FocusRect = focusRect,
            FocusPointInStageSpace = new Vector2(local.x, local.y)
        };

        return true;
    }

    // private static RectTransform ResolveFocusRect(RectTransform fallbackRect, CharacterFocusAnchor anchor)
    // {
    //     if (fallbackRect == null)
    //         return null;
    //
    //     CharacterFocusAnchorView view = fallbackRect.GetComponentInParent<CharacterFocusAnchorView>();
    //     if (view != null && view.TryGetAnchor(anchor, out RectTransform rect))
    //         return rect;
    //
    //     view = fallbackRect.GetComponentInChildren<CharacterFocusAnchorView>(true);
    //     if (view != null && view.TryGetAnchor(anchor, out rect))
    //         return rect;
    //
    //     return null;
    // }
}