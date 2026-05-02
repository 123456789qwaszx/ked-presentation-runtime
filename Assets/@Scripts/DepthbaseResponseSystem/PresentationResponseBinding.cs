using System;
using UnityEngine;

/// <summary>
/// Response target 하나와 profile을 묶는 단위.
/// </summary>
[Serializable]
public sealed class PresentationResponseBinding
{
    public string key;

    [SerializeField] private MonoBehaviour _targetBehaviour;
    public PresentationResponseProfile profile = new PresentationResponseProfile();

    private IPresentationResponseTarget _cachedTarget;

    public IPresentationResponseTarget Target
    {
        get
        {
            if (_cachedTarget == null && _targetBehaviour != null)
                _cachedTarget = _targetBehaviour as IPresentationResponseTarget;
            return _cachedTarget;
        }
    }

    public void SetTarget(MonoBehaviour behaviour)
    {
        _targetBehaviour = behaviour;
        _cachedTarget = behaviour as IPresentationResponseTarget;
    }

    public void Apply(in PresentationIntentState state)
    {
        IPresentationResponseTarget target = Target;
        if (target == null)
            return;

        PresentationResponse response = PresentationResponseSolver.Solve(state, profile);
        target.ApplyResponse(in response);
    }

    public void CaptureBasePose(PresentationResponseRig rig)
    {
        if (rig == null)
            return;

        RectTransformResponseTarget rectTarget = _targetBehaviour as RectTransformResponseTarget;
        if (rectTarget == null || rectTarget.Rect == null)
            return;

        Vector3 worldPivot = rectTarget.Rect.TransformPoint(Vector3.zero);
        profile.basePositionInRigSpace = rig.WorldToSpacePoint(worldPivot);
        profile.baseScale = new Vector2(rectTarget.Rect.localScale.x, rectTarget.Rect.localScale.y);

        if (rectTarget.CanvasGroup != null)
            profile.baseAlpha = rectTarget.CanvasGroup.alpha;
    }
}
