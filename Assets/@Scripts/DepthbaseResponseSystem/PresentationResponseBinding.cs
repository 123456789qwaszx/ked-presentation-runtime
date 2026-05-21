using UnityEngine;

// target + profile + runtime 좌표계 정보를 묶고,
// state를 response로 번역해 target에 적용한다.
public sealed class PresentationResponseBinding
{
    // position은 target parent 기준이 아니라 Rig 공간 좌표다.
    // local position이 아니고 Rig 공간인 이유는 좌표계가 섞이지 않게 하기 위함.
    // 이것을 PositionRect parent local 공간으로 변환해 적용한다.
    public struct Response
    {
        // anchoredPosition은 Rig/Stage 공간 기준 좌표로 계산된다.
        // Apply 직전에 PositionRect.parent 기준 좌표로 변환된다.
        public Vector2 anchoredPosition;
        public Vector2 scale;
        public float alpha;
    }

    // 모든 연출 계산의 기준 좌표계.
    // 예:
    // "이 캐릭터 슬롯은 Stage 기준 (300, 0)에 있다"
    // "이 배경판은 Stage 기준 (-200, 0)에 있다"
    private readonly RectTransform _rigSpaceRoot;

    // PositionRect의 실제 parent.
    // RectTransform.anchoredPosition은 이 parent 기준 local 좌표로 써야 한다.
    private readonly RectTransform _targetParent;
    private readonly bool _needsCoordinateTransform;

    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IResponseTarget Target { get; }

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        IResponseTarget target,
        RectTransform stageRoot)
    {
        Key = key;
        Profile = profile;
        Target = target;

        _rigSpaceRoot = stageRoot;

        _targetParent = target != null && target.PositionRect != null
            ? target.PositionRect.parent as RectTransform
            : null;

        _needsCoordinateTransform =
            _targetParent != null &&
            _rigSpaceRoot != null &&
            !ReferenceEquals(_targetParent, _rigSpaceRoot);
    }

    public bool IsAlive =>
        Target != null &&
        Target.MeasureRect != null &&
        Target.PositionRect != null;

    public void Apply(in PresentationIntentState state)
    {
        if (!IsAlive)
            return;

        Response response = Solve(in state, Profile);

        if (_needsCoordinateTransform)
        {
            // Convert:
            // Stage_Root local space
            // -> world space
            // -> PositionRect.parent local space.
            Vector2 positionInStageSpace = response.anchoredPosition;

            Vector3 worldPosition = _rigSpaceRoot.TransformPoint(
                new Vector3(positionInStageSpace.x, positionInStageSpace.y, 0f));

            Vector3 positionInParentSpace = _targetParent.InverseTransformPoint(worldPosition);

            response.anchoredPosition = new Vector2(
                positionInParentSpace.x,
                positionInParentSpace.y);
        }

        Target.ApplyResponse(in response);
    }

    private static Response Solve(
        in PresentationIntentState state,
        PresentationResponseProfile profile)
    {
        float zoomFactor = Mathf.Clamp(state.zoom, -10f, 10f);

        float scaleMultiplier = 1f + zoomFactor * profile.maxZoomScaleDelta;
        Vector2 scaledSize = profile.baseScale * Mathf.Max(0.01f, scaleMultiplier);

        Vector2 focusToTarget = profile.basePositionInRigSpace - state.focusPoint;

        Vector2 zoomSpreadOffset = CalculateZoomSpreadOffset(
            focusToTarget,
            zoomFactor,
            profile.maxZoomSpreadPixels);

        Vector2 finalPosition =
            profile.basePositionInRigSpace +
            state.pan * profile.panResponse +
            zoomSpreadOffset;

        return new Response
        {
            anchoredPosition = finalPosition,
            scale = scaledSize,
            alpha = profile.baseAlpha,
        };
    }

    private static Vector2 CalculateZoomSpreadOffset(
        Vector2 focusToTarget,
        float zoomFactor,
        float maxZoomSpreadPixels)
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