using System;
using UnityEngine;

// Solver 결과를 실제 구현체에 적용하는 대상 계약.
public interface IPresentationResponseTarget
{
    void ApplyResponse(in PresentationResponse response);
}

// Response target 하나와 profile을 묶는 단위.
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

    public PresentationResponseBinding(
        string key,
        PresentationResponseProfile profile,
        MonoBehaviour targetBehaviour)
    {
        this.key = key;
        this.profile = profile ?? new PresentationResponseProfile();
        _targetBehaviour = targetBehaviour;
        _cachedTarget = targetBehaviour as IPresentationResponseTarget;
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