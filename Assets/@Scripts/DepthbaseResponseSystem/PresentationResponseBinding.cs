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
/// 완성된 target + profile을 묶고, state를 response로 번역해 target에 적용한다.
/// </summary>
public sealed class PresentationResponseBinding
{
    private const float ZoomIntentScale = 0.1f;
    
    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IRectTransformPresentationResponseTarget Target { get; }

    public PresentationResponseBinding(string key, PresentationResponseProfile profile, IRectTransformPresentationResponseTarget target)
    {
        Key = key;
        Profile = profile;
        Target = target;
    }

    public void Apply(in PresentationIntentState state, PresentationViewRefs presentation)
    {
        PresentationResponse rigResponse = Solve(in state, Profile);

        RectTransform parent = Target.Rect.parent as RectTransform;

        Vector2 localPosition = SpaceToParentLocal(presentation.Stage_Root, parent, rigResponse.anchoredPosition);

        PresentationResponse response = new PresentationResponse
        {
            anchoredPosition = localPosition,
            scale = rigResponse.scale,
            alpha = rigResponse.alpha
        };

        Target.ApplyResponse(in response);
    }
    
    public Vector2 SpaceToParentLocal(RectTransform stageRoot, RectTransform parent, Vector2 pointInRigSpace)
    {
        if (parent == null)
            return pointInRigSpace;

        Vector3 worldPoint = stageRoot.TransformPoint(new Vector3(pointInRigSpace.x, pointInRigSpace.y, 0f));
        Vector3 parentLocal = parent.InverseTransformPoint(worldPoint);
        return new Vector2(parentLocal.x, parentLocal.y);
    }
    
    
    public static PresentationResponse Solve(in PresentationIntentState state, PresentationResponseProfile profile)
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