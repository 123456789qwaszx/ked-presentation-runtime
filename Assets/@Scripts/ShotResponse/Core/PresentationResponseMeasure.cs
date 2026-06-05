using UnityEngine;

public readonly struct PresentationResponseMeasure
{
    public readonly Vector2 basePositionInRigSpace;
    public readonly Vector2 baseLocalScale;

    public PresentationResponseMeasure(
        Vector2 basePositionInRigSpace,
        Vector2 baseLocalScale)
    {
        this.basePositionInRigSpace = basePositionInRigSpace;
        this.baseLocalScale = baseLocalScale;
    }
}