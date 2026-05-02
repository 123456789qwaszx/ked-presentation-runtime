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
    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IRectTransformPresentationResponseTarget Target { get; }

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        IRectTransformPresentationResponseTarget target)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Binding key must not be null or empty.", nameof(key));

        Key = key;
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public void Apply(in PresentationIntentState state, PresentationViewRefs presentation)
    {
        PresentationResponse rigResponse = PresentationResponseSolver.Solve(in state, Profile);

        RectTransform parent = Target.Rect != null
            ? Target.Rect.parent as RectTransform
            : null;

        Vector2 localPosition = PresentationSpaceUtil.SpaceToParentLocal(
            presentation != null ? presentation.Stage_Root : null,
            parent,
            rigResponse.anchoredPosition);

        PresentationResponse response = new PresentationResponse
        {
            anchoredPosition = localPosition,
            scale = rigResponse.scale,
            alpha = rigResponse.alpha
        };

        Target.ApplyResponse(in response);
    }
}