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
        CharacterFocusAnchor anchor,
        CharacterRigTarget fallbackTarget,
        Vector2 localOffset,
        out CharacterFocusPointResult result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(roleKey))
            return false;

        if (scope == null || scope.Refs == null || scope.Presentation == null)
            return false;

        if (!scope.Refs.TryGetCharRigRefs(roleKey.Trim(), out CharacterRigRefs rigRefs) || rigRefs == null)
            return false;

        RectTransform fallbackRect = rigRefs.GetRect(fallbackTarget);
        if (fallbackRect == null)
            return false;

        RectTransform focusRect = ResolveFocusRect(fallbackRect, anchor);
        if (focusRect == null)
            focusRect = fallbackRect;

        RectTransform stageRoot = ResolveStageRootForRect(scope, focusRect);
        if (stageRoot == null)
            return false;

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

    private static RectTransform ResolveFocusRect(RectTransform fallbackRect, CharacterFocusAnchor anchor)
    {
        if (fallbackRect == null)
            return null;

        CharacterFocusAnchorView view = fallbackRect.GetComponentInParent<CharacterFocusAnchorView>();
        if (view != null && view.TryGetAnchor(anchor, out RectTransform rect))
            return rect;

        view = fallbackRect.GetComponentInChildren<CharacterFocusAnchorView>(true);
        if (view != null && view.TryGetAnchor(anchor, out rect))
            return rect;

        return null;
    }

    private static RectTransform ResolveStageRootForRect(CommandRunScope scope, RectTransform rect)
    {
        if (scope == null || scope.Presentation == null || rect == null)
            return null;

        RectTransform stage00 = scope.Presentation.GetRect(PresentationTarget.Stage00_Root);
        RectTransform stage01 = scope.Presentation.GetRect(PresentationTarget.Stage01_Root);
        RectTransform stage02 = scope.Presentation.GetRect(PresentationTarget.Stage02_Root);

        if (IsChildOf(rect, stage00))
            return stage00;

        if (IsChildOf(rect, stage01))
            return stage01;

        if (IsChildOf(rect, stage02))
            return stage02;

        return stage00;
    }

    private static bool IsChildOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform t = child;
        while (t != null)
        {
            if (ReferenceEquals(t, parent))
                return true;

            t = t.parent;
        }

        return false;
    }
}