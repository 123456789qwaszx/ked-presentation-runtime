using UnityEngine;

/// <summary>
/// Stage_Root 기준 Rig 공간 ↔ world / parent local 변환 유틸.
/// </summary>
public static class PresentationSpaceUtil
{
    public static Vector3 SpaceToWorldPoint(RectTransform stageRoot, Vector2 pointInRigSpace)
    {
        if (stageRoot == null)
            return new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f);

        return stageRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
    }

    public static Vector2 WorldToSpacePoint(RectTransform stageRoot, Vector3 worldPoint)
    {
        if (stageRoot == null)
            return new Vector2(worldPoint.x, worldPoint.y);

        Vector3 local = stageRoot.InverseTransformPoint(worldPoint);
        return new Vector2(local.x, local.y);
    }

    public static Vector2 SpaceToParentLocal(
        RectTransform stageRoot,
        RectTransform parent,
        Vector2 pointInRigSpace)
    {
        if (parent == null)
            return pointInRigSpace;

        Vector3 worldPoint = SpaceToWorldPoint(stageRoot, pointInRigSpace);
        Vector3 parentLocal = parent.InverseTransformPoint(worldPoint);
        return new Vector2(parentLocal.x, parentLocal.y);
    }
}