using System;
using UnityEngine;

public interface IPresentationResponseTarget
{
    void ApplyResponse(in PresentationResponse response);
}

public interface IRectTransformPresentationResponseTarget : IPresentationResponseTarget
{
    RectTransform Rect { get; }
    CanvasGroup CanvasGroup { get; }
}

/// <summary>
/// target + profile + runtime 좌표계 정보를 묶고,
/// state를 response로 번역해 target에 적용한다.
/// </summary>
public sealed class PresentationResponseBinding
{
    private const float ZoomIntentScale = 0.1f;

    private readonly RectTransform _stageRoot;
    private readonly RectTransform _parent;
    private readonly bool _skipCoordinateTransform;

    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IRectTransformPresentationResponseTarget Target { get; }

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        IRectTransformPresentationResponseTarget target,
        RectTransform stageRoot)
    {
        Key = key;
        Profile = profile;
        Target = target;

        _stageRoot = stageRoot;
        _parent = target != null && target.Rect != null
            ? target.Rect.parent as RectTransform
            : null;

        _skipCoordinateTransform =
            _parent == null ||
            _stageRoot == null ||
            ReferenceEquals(_parent, _stageRoot);
    }

    public bool IsAlive =>
        Target != null &&
        Target.Rect != null;

    public void Apply(in PresentationIntentState state)
    {
        if (!IsAlive)
            return;

        PresentationResponse rigResponse = Solve(in state, Profile);

        Vector2 localPosition = ToParentLocal(rigResponse.anchoredPosition);

        PresentationResponse response = new PresentationResponse
        {
            anchoredPosition = localPosition,
            scale = rigResponse.scale,
            alpha = rigResponse.alpha
        };

        Target.ApplyResponse(in response);
    }

    private Vector2 ToParentLocal(Vector2 pointInRigSpace)
    {
        if (_skipCoordinateTransform)
            return pointInRigSpace;

        Vector3 worldPoint = _stageRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
        Vector3 parentLocal = _parent.InverseTransformPoint(worldPoint);
        return new Vector2(parentLocal.x, parentLocal.y);
    }

    public static PresentationResponse Solve(
        in PresentationIntentState state,
        PresentationResponseProfile profile)
    {
        float zoom01 = Mathf.Clamp(state.zoom * ZoomIntentScale, -1f, 1f);

        float scaleMul = 1f + zoom01 * profile.maxZoomScaleDelta;
        Vector2 finalScale = profile.baseScale * Mathf.Max(0.01f, scaleMul);

        Vector2 spreadOffset = Vector2.zero;
        Vector2 fromFocus = profile.basePositionInRigSpace - state.focusPoint;

        if (profile.maxZoomSpreadPixels > 0f && fromFocus.sqrMagnitude > 0.0001f)
            spreadOffset = fromFocus.normalized * (zoom01 * profile.maxZoomSpreadPixels);

        Vector2 finalPosition =
            profile.basePositionInRigSpace
            + state.pan * profile.panResponse
            + spreadOffset;

        return new PresentationResponse
        {
            anchoredPosition = finalPosition,
            scale = finalScale,
            alpha = profile.baseAlpha,
        };
    }
}