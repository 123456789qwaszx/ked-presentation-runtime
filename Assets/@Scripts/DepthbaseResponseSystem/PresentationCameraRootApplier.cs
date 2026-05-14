using UnityEngine;

public sealed class PresentationCameraRootApplier
{
    private readonly RectTransform _stagePanRoot;
    private readonly RectTransform _stageZoomRoot;

    private readonly float _maxScale;
    private readonly float _panMultiplier;

    public PresentationCameraRootApplier(
        RectTransform stagePanRoot,
        RectTransform stageZoomRoot,
        float maxScale = 5.0f,
        float panMultiplier = 1.0f)
    {
        _stagePanRoot = stagePanRoot;
        _stageZoomRoot = stageZoomRoot;
        _maxScale = Mathf.Max(1.0001f, maxScale);
        _panMultiplier = panMultiplier;
    }

    public void Apply(in PresentationIntentState state)
    {
        if (_stageZoomRoot != null)
        {
            float scale = EvaluateScale(state.zoom);
            _stageZoomRoot.localScale = new Vector3(scale, scale, 1f);
        }

        if (_stagePanRoot != null)
            _stagePanRoot.anchoredPosition = state.pan * _panMultiplier;
    }

    public float EvaluateScale(float zoom)
    {
        float t = Mathf.Clamp(zoom, -10f, 10f) / 10f;
        return Mathf.Pow(_maxScale, t);
    }
}