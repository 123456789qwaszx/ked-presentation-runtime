using UnityEngine;

// Rig 공간 결과를 실제 RectTransform에 적용하는 기본 어댑터.
// 내부적으로 Rig 공간 → parent local 공간 변환을 수행한다.
public sealed class RectTransformResponseTarget : MonoBehaviour, IPresentationResponseTarget
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private PresentationResponseRig _presentationResponseRig;

    public RectTransform Rect => _rect;
    public CanvasGroup CanvasGroup => _canvasGroup;

    private void Reset()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _presentationResponseRig = GetComponentInParent<PresentationResponseRig>(true);
    }

    public void ApplyResponse(in PresentationResponse response)
    {
        if (_rect == null)
            return;

        if (_presentationResponseRig == null)
            _presentationResponseRig = GetComponentInParent<PresentationResponseRig>(true);

        RectTransform parent = _rect.parent as RectTransform;
        if (_presentationResponseRig != null && _presentationResponseRig.SpaceRoot != null && parent != null)
        {
            Vector3 worldPoint = _presentationResponseRig.SpaceToWorldPoint(response.positionInRigSpace);
            Vector3 parentLocal = parent.InverseTransformPoint(worldPoint);
            _rect.anchoredPosition3D = new Vector3(parentLocal.x, parentLocal.y, _rect.anchoredPosition3D.z);
        }
        else
        {
            _rect.anchoredPosition = response.positionInRigSpace;
        }

        _rect.localScale = new Vector3(response.scale.x, response.scale.y, 1f);

        if (_canvasGroup != null)
            _canvasGroup.alpha = response.alpha;
    }
}