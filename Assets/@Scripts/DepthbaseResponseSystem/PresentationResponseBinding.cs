using UnityEngine;

public interface IPresentationResponseTarget
{
    RectTransform Rect { get; }
    CanvasGroup CanvasGroup { get; }

    void ApplyResponse(in PresentationResponse response);
}

// target + profile + runtime 좌표계 정보를 묶고,
// state를 response로 번역해 target에 적용한다.
public sealed class PresentationResponseBinding
{
    // 모든 연출 계산의 기준 좌표계
    // "이 캐릭터는 Stage 기준 (300, 0)에 있다"
    // "이 배경은 Stage 기준 (-200, 0)에 있다"
    private readonly RectTransform _rigSpaceRoot;
    
    // The actual parent of Target.Rect.
    // RectTransform.anchoredPosition must be expressed in this parent's local space.
    private readonly RectTransform _targetParent;
    private readonly bool _needsCoordinateTransform;

    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IPresentationResponseTarget Target { get; }

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        IPresentationResponseTarget target,
        RectTransform stageRoot)
    {
        Key = key;
        Profile = profile;
        Target = target;

        _rigSpaceRoot = stageRoot;
        _targetParent = target != null && target.Rect != null
            ? target.Rect.parent as RectTransform
            : null;

        _needsCoordinateTransform =
            _targetParent != null &&
            _rigSpaceRoot != null &&
            !ReferenceEquals(_targetParent, _rigSpaceRoot);
    }

    public bool IsAlive => Target?.Rect != null;

    public void Apply(in PresentationIntentState state)
    {
        if (!IsAlive)
            return;

        PresentationResponse response = Solve(in state, Profile);

        if (_needsCoordinateTransform)
        { // Convert: Stage_Root space -> world space -> target parent local space.
            Vector2 positionInStageSpace = response.anchoredPosition;

            Vector3 worldPosition = _rigSpaceRoot.TransformPoint(new Vector3(positionInStageSpace.x, positionInStageSpace.y, 0f));
            Vector3 positionInParentSpace = _targetParent.InverseTransformPoint(worldPosition);

            response.anchoredPosition = new Vector2(positionInParentSpace.x, positionInParentSpace.y);
        }

        Target.ApplyResponse(in response);
    }

    private static PresentationResponse Solve(in PresentationIntentState state, PresentationResponseProfile profile)
    {
        float zoomFactor = Mathf.Clamp(state.zoom, -10f, 10f);

        float scaleMultiplier = 1f + zoomFactor * profile.maxZoomScaleDelta;
        Vector2 scaledSize = profile.baseScale * Mathf.Max(0.01f, scaleMultiplier);

        Vector2 focusToTarget = profile.basePositionInRigSpace - state.focusPoint;
        Vector2 zoomSpreadOffset = CalculateZoomSpreadOffset(focusToTarget, zoomFactor, profile.maxZoomSpreadPixels);

        Vector2 finalPosition = profile.basePositionInRigSpace + state.pan * profile.panResponse + zoomSpreadOffset;

        return new PresentationResponse
        {
            anchoredPosition = finalPosition,
            scale = scaledSize,
            alpha = profile.baseAlpha,
        };
    }

    private static Vector2 CalculateZoomSpreadOffset(Vector2 focusToTarget, float zoomFactor, float maxZoomSpreadPixels)
    {
        if (maxZoomSpreadPixels <= 0f)
            return Vector2.zero;

        if (focusToTarget.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        Vector2 spreadDirection = focusToTarget.normalized;
        float spreadDistance = zoomFactor * maxZoomSpreadPixels;

        return spreadDirection * spreadDistance;
    }
}