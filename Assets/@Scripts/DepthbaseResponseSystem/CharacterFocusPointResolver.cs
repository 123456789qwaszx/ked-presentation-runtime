using UnityEngine;

public interface ICameraFocusStageRootProvider
{
    RectTransform Stage00Root { get; }
    RectTransform Stage01Root { get; }
    RectTransform Stage02Root { get; }
}

public sealed partial class PresentationUIRoot : ICameraFocusStageRootProvider
{
    public RectTransform Stage00Root => View.Rect(Refs.Stage00_Root);
    public RectTransform Stage01Root => View.Rect(Refs.Stage01_Root);
    public RectTransform Stage02Root => View.Rect(Refs.Stage02_Root);
}

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

        if (scope == null || scope.characterRigs == null)
            return false;

        if (!scope.characterRigs.TryGetRig(roleKey.Trim(), out CharacterRigRefs rigRefs) || rigRefs == null)
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
        if (rect == null)
            return null;

        ICameraFocusStageRootProvider cameraFocusStageRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
        
        RectTransform stage00 = cameraFocusStageRootProvider.Stage00Root ;
        RectTransform stage01 = cameraFocusStageRootProvider.Stage01Root ;
        RectTransform stage02 = cameraFocusStageRootProvider.Stage02Root ;

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