using UnityEngine;

// target + profile + coordinate mapper.
// Translates PresentationIntentState into target response and applies it.
public sealed class PresentationResponseBinding
{
    public struct Response
    {
        // Calculated in rig space first.
        // Converted to PositionRect.parent local space immediately before apply.
        public Vector2 anchoredPosition;
        public Vector2 scale;
    }

    private readonly PresentationResponseCoordinateMapper _coordinateMapper;

    public string Key { get; }
    public PresentationResponseProfile Profile { get; }
    public IResponseTarget Target { get; }

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        IResponseTarget target,
        PresentationResponseCoordinateMapper coordinateMapper)
    {
        Key = key;
        Profile = profile;
        Target = target;
        _coordinateMapper = coordinateMapper;
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

        Response response = PresentationResponseMath.CalculateTargetTransformResponseFromShotIntent(in state, Profile);
        response.anchoredPosition = _coordinateMapper.ConvertPositionFromRigSpaceToTargetParentSpace(response.anchoredPosition);

        Target.ApplyResponse(in response);
    }
}