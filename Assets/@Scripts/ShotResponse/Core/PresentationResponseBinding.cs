using UnityEngine;

// target + profile + runtime coordinate info.
// Translates PresentationIntentState into target response and applies it.
public sealed class PresentationResponseBinding
{
    public struct Response
    {
        // Calculated in rig/stage space first.
        // Converted to PositionRect.parent local space immediately before apply.
        public Vector2 anchoredPosition;
        public Vector2 scale;
    }

    private readonly RectTransform _rigSpaceRoot;
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
        Target.PositionRect != null &&
        Target.ScaleRect != null;

    public void Apply(in PresentationIntentState state)
    {
        if (!IsAlive)
            return;

        Response response = PresentationResponseMath.SolveResponse(in state, Profile);

        if (_needsCoordinateTransform)
        {
            response.anchoredPosition =
                PresentationCoordinateMath.ConvertPointFromRootToParentSpace(
                    response.anchoredPosition,
                    _rigSpaceRoot,
                    _targetParent);
        }

        Target.ApplyResponse(in response);
    }
}