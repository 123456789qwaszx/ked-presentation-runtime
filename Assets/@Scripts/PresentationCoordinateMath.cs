using UnityEngine;

public interface IPresentationRigSpaceRootProvider
{
    RectTransform RigSpaceRoot  { get; }
}

public sealed partial class PresentationUIRoot : IPresentationRigSpaceRootProvider
{
    public RectTransform RigSpaceRoot  => View.Rect(Refs.StageShot_Root);
}

public sealed class PresentationResponseCoordinateMapper
{
    IPresentationRigSpaceRootProvider _stageRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
    
    private readonly RectTransform _rigSpaceRoot;
    private readonly RectTransform _targetPositionParent;
    private readonly bool _needsCoordinateTransform;

    public PresentationResponseCoordinateMapper(
        IResponseTarget target)
    {
        _rigSpaceRoot = _stageRootProvider.RigSpaceRoot;

        _targetPositionParent =
            target != null && target.PositionRect != null
                ? target.PositionRect.parent as RectTransform
                : null;

        _needsCoordinateTransform =
            _rigSpaceRoot != null &&
            _targetPositionParent != null &&
            !ReferenceEquals(_rigSpaceRoot, _targetPositionParent);
    }

    public bool IsValid => _rigSpaceRoot != null && _targetPositionParent != null;

    public Vector2 CaptureNeutralPivotInRigSpace(RectTransform neutralPivotSource)
    {
        if (_rigSpaceRoot == null || neutralPivotSource == null)
            return Vector2.zero;

        return PresentationCoordinateMath.CaptureNeutralPivotInRigSpace(
            neutralPivotSource,
            _rigSpaceRoot);
    }

    public Vector2 ConvertPositionFromRigSpaceToTargetParentSpace(Vector2 positionInRigSpace)
    {
        if (!_needsCoordinateTransform)
            return positionInRigSpace;

        return PresentationCoordinateMath.ConvertPointFromRigSpaceToTargetPositionParentSpace(
            positionInRigSpace,
            _rigSpaceRoot,
            _targetPositionParent);
    }
}

// “계산 좌표계”와 “실제 적용 좌표계” 연결 어댑터
// rigSpaceRoot / stageRoot = PresentationResponseMath가 결과를 계산하는 공통 기준 좌표계
// targetParent = 실제 Target.PositionRect.anchoredPosition을 적용할 때 필요한 부모 좌표계
// measureRect = target의 현재 중립 기준 위치를 캡처하기 위한 측정용 RectTransform
// StageRootProvider = Command가 stageRoot를 직접 알지 않도록, UIRoot에서 공통 기준 좌표계를 꺼내오는 통로
public static class PresentationCoordinateMath
{
    public static Vector2 ConvertPointFromRigSpaceToTargetPositionParentSpace(
        Vector2 pointInRigSpace,
        RectTransform rigSpaceRoot,
        RectTransform targetParent)
    {
        Vector3 worldPosition = rigSpaceRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
        Vector3 positionInParentSpace = targetParent.InverseTransformPoint(worldPosition);
        
        return new Vector2(positionInParentSpace.x, positionInParentSpace.y);
    }

    public static Vector2 CaptureNeutralPivotInRigSpace(
        RectTransform measureRect,
        RectTransform rigSpaceRoot)
    {
        Vector3 worldPivot = measureRect.TransformPoint(Vector3.zero);
        Vector3 localPivot = rigSpaceRoot.InverseTransformPoint(worldPivot);

        return new Vector2(localPivot.x, localPivot.y);
    }
}