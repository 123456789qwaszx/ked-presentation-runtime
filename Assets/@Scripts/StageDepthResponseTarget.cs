using UnityEngine;

// 캐릭터 리그 내부 framing 노드를 target으로 삼던 기존 방식(CharacterRigResponseTarget)을 대체.
// 카메라 반응은 캐릭터 속성이 아니라 무대 깊이 레이어의 속성으로 정의.
//
// MeasureRect(Root)는 position, scale보다 상위 계층.(Response의 영향이 없는 중립 기준점)
// PresentationResponseCoordinateMapper가 매 적용마다 MeasureRect의 pivot을 다시 측정 함.
// 따라서 만약 MeasureRect가 응답을 받으면 base가 누적 드리프트 됨.
public sealed class StageDepthResponseTarget : IResponseTarget
{
    private readonly RectTransform _measure;
    private readonly RectTransform _position;
    private readonly RectTransform _scale;

    public RectTransform MeasureRect => _measure;
    public RectTransform PositionRect => _position;
    public RectTransform ScaleRect => _scale;

    public StageDepthResponseTarget(
        RectTransform measure,
        RectTransform position,
        RectTransform scale)
    {
        _measure = measure;
        _position = position;
        _scale = scale;
    }

    public void ApplyResponse(in PresentationTargetResponse response)
    {
        if (_position != null)
            _position.anchoredPosition = response.anchoredPosition;

        if (_scale != null)
            _scale.localScale = new Vector3(response.scale.x, response.scale.y, 1f);
    }
}