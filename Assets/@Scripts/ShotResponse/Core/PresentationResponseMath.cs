using UnityEngine;

public static class PresentationResponseMath
{
    public static PresentationTargetResponse CalculateTargetTransformResponseFromShotIntent(
        in PresentationIntentState state,
        PresentationResponseProfile profile,
        in PresentationResponseMeasure measure)
    {
        // 이건 물리 카메라 시뮬레이션이 아니라 VN 스테이지 구도 보정기다.
        // 그래서 X축(좌우 구도)과 Y축(얼굴 높이 / 화면 여백 / 키 차이)을 절대 섞지 않는다.
        // normalized / magnitude / Vector2.Distance 는 의도적으로 사용하지 않는다.
        // (이 셋이 들어가는 순간 세로 위치가 가로 반응에 되먹임되어 구도가 흔들린다.)

        Vector2 focusSpreadOffset =
            CalculateAxisSeparatedFocusSpreadOffset(
                measure.basePositionInRigSpace,
                state.focusPointInRigSpace,
                state.zoom,
                profile.focusSpreadPixelsPerZoom);

        Vector2 panOffset =
            CalculateAxisSeparatedPanOffset(
                state.panInRigSpace,
                profile.panResponse);

        Vector2 destPos =
            measure.basePositionInRigSpace +
            focusSpreadOffset +
            panOffset;

        // 스케일은 zoom에 대한 "균일" 배율이다. 축 분리 대상이 아니다.
        // (X/Y를 다르게 주면 스프라이트가 찌그러진다.)
        //
        // 중요: zoom=0이면 무조건 base로 정확히 돌아와야 한다(shot_reset 멱등성).
        // baseLocalScale * (1 + 0 * scaleResponse) = baseLocalScale.
        // 캐릭터별 약한 스케일 반응은 ApplyResponse가 아니라 여기 profile에서 처리한다.
        Vector2 destScale =
            measure.baseLocalScale * (1f + state.zoom * profile.zoomScaleResponse);

        return new PresentationTargetResponse
        {
            anchoredPosition = destPos,
            scale = destScale,
        };
    }

    // focusPoint 기준으로 대상이 어느 "쪽"에 있는지(부호)만 본다. 거리는 보지 않는다.
    // 덕분에 캐릭터의 얼굴 높이 / 키 차이 / 프리셋 오프셋으로 세로 위치가 달라져도
    // 가로 spread 반응은 전혀 흔들리지 않는다.
    private static Vector2 CalculateAxisSeparatedFocusSpreadOffset(
        Vector2 basePositionInRigSpace,
        Vector2 focusPointInRigSpace,
        float zoom,
        Vector2 focusSpreadPixelsPerZoom)
    {
        Vector2 fromFocus = basePositionInRigSpace - focusPointInRigSpace;

        float signX = Mathf.Approximately(fromFocus.x, 0f)
            ? 0f
            : Mathf.Sign(fromFocus.x);

        float signY = Mathf.Approximately(fromFocus.y, 0f)
            ? 0f
            : Mathf.Sign(fromFocus.y);

        return new Vector2(
            signX * focusSpreadPixelsPerZoom.x * zoom,
            signY * focusSpreadPixelsPerZoom.y * zoom);
    }

    // pan도 축별로 완전 독립.
    // 세로 pan은 거의 죽이고(panResponse.y를 작게) 가로 pan만 살리는 식의 튜닝이 가능해진다.
    private static Vector2 CalculateAxisSeparatedPanOffset(
        Vector2 panInRigSpace,
        Vector2 panResponse)
    {
        return new Vector2(
            panInRigSpace.x * panResponse.x,
            panInRigSpace.y * panResponse.y);
    }
}