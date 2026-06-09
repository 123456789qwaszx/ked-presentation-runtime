using UnityEngine;

public interface IPresentationRigSpaceRootProvider
{
    RectTransform RigSpaceRoot { get; }
}

public sealed partial class PresentationUIRoot : IPresentationRigSpaceRootProvider
{
    public RectTransform RigSpaceRoot => View.Rect(Refs.StageShot_Root);
}

public sealed class PresentationResponseCoordinateMapper
{
    private IPresentationRigSpaceRootProvider _stageRootProvider;

    public PresentationResponseMeasure CaptureCurrentMeasure(IResponseTarget target)
    {
        RectTransform rigSpaceRoot = GetRigSpaceRoot();

        Vector2 basePositionInRigSpace =
            PresentationCoordinateMath.CaptureNeutralPivotInRigSpace(
                target.MeasureRect,
                rigSpaceRoot);

        Vector2 baseLocalScale =
            PresentationCoordinateMath.CaptureNeutralScale(
                target.MeasureRect);

        return new PresentationResponseMeasure(
            basePositionInRigSpace,
            baseLocalScale);
    }

    public Vector2 ConvertPositionFromRigSpaceToTargetParentSpace(
        Vector2 positionInRigSpace,
        IResponseTarget target)
    {
        return PresentationCoordinateMath.ConvertPointFromRigSpaceToTargetPositionParentSpace(
            positionInRigSpace,
            GetRigSpaceRoot(),
            target.PositionRect.parent as RectTransform);
    }

    private RectTransform GetRigSpaceRoot()
    {
        if (_stageRootProvider == null)
            _stageRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();

        return _stageRootProvider.RigSpaceRoot;
    }
}

// “계산 좌표계”와 “실제 적용 좌표계” 연결 어댑터
// rigSpaceRoot = PresentationResponseMath가 결과를 계산하는 공통 기준 좌표계
// targetParent = 실제 Target.PositionRect.anchoredPosition을 적용할 때 필요한 부모 좌표계
// neutralPivotSource = target의 현재 중립 기준 위치를 캡처하기 위한 측정용 RectTransform
public static class PresentationCoordinateMath
{
    public static Vector2 CaptureNeutralPivotInRigSpace(
        RectTransform neutralPivotSource,
        RectTransform rigSpaceRoot)
    {
        Vector3 worldPivot = neutralPivotSource.TransformPoint(Vector3.zero);
        Vector3 localPivot = rigSpaceRoot.InverseTransformPoint(worldPivot);

        return new Vector2(localPivot.x, localPivot.y);
    }
    
    public static Vector2 CaptureNeutralScale(
        RectTransform neutralScaleSource)
    {
        Vector3 scale = neutralScaleSource.localScale;
        return new Vector2(scale.x, scale.y);
    }
    
    public static Vector2 ConvertPointFromRigSpaceToTargetPositionParentSpace(
        Vector2 pointInRigSpace,
        RectTransform rigSpaceRoot,
        RectTransform targetParent)
    {
        Vector3 worldPosition = rigSpaceRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
        Vector3 positionInParentSpace = targetParent.InverseTransformPoint(worldPosition);
        
        return new Vector2(positionInParentSpace.x, positionInParentSpace.y);
    }
}