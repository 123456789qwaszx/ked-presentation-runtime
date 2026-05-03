using UnityEngine;

/// <summary>
/// 최종 local 적용만 담당하는 기본 어댑터.
/// rig나 Stage_Root를 알지 않는다.
/// </summary>
public sealed class RectTransformResponseTarget : MonoBehaviour, PresentationResponseBinding.IResponseTarget
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private CanvasGroup _canvasGroup;

    public RectTransform Rect => _rect;
    public CanvasGroup CanvasGroup => _canvasGroup;

    public void ApplyResponse(in PresentationResponseBinding.Response response)
    {
        if (_rect == null)
            return;
        
        //Debug.Log($"[RectTransformResponseTarget] Apply. " + $"target={name}, scale={response.scale}, pos={response.anchoredPosition}");
        _rect.anchoredPosition = response.anchoredPosition;
        _rect.localScale = new Vector3(response.scale.x, response.scale.y, 1f);

        if (_canvasGroup != null)
            _canvasGroup.alpha = response.alpha;
    }
}