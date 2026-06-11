using UnityEngine;

public readonly struct PresentationResponseMeasure
{
    // focus-side 판정용. MeasureRect 기준으로 잰 rig-space 위치.
    // (대상이 focusPoint의 좌/우, 위/아래 어느 쪽인지 부호를 보는 데만 쓴다.)
    public readonly Vector2 basePositionInRigSpace;

    // 적용 기준. PositionRect의 중립(bind 시점) anchoredPosition (부모 로컬 공간).
    // 실제 위치 적용은 "이 값 + offset" 으로만 한다. 절대 위치를 다시 꽂지 않는다.
    public readonly Vector2 baseAnchoredPosition;

    // ScaleRect의 중립(bind 시점) localScale.
    public readonly Vector2 baseLocalScale;

    public PresentationResponseMeasure(
        Vector2 basePositionInRigSpace,
        Vector2 baseAnchoredPosition,
        Vector2 baseLocalScale)
    {
        this.basePositionInRigSpace = basePositionInRigSpace;
        this.baseAnchoredPosition = baseAnchoredPosition;
        this.baseLocalScale = baseLocalScale;
    }
}