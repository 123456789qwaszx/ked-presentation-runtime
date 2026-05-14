using UnityEngine;

public sealed class PresentationCameraRootApplier
{
    private readonly RectTransform _stagePanRoot;
    private readonly RectTransform _stageZoomRoot;

    private readonly float _zoomToScale;
    private readonly float _minScale;
    private readonly float _maxScale;
    private readonly float _panMultiplier;

    public PresentationCameraRootApplier(
        RectTransform stagePanRoot,
        RectTransform stageZoomRoot,
        float zoomToScale = 0.05f,
        float minScale = 0.25f,
        float maxScale = 3.0f,
        float panMultiplier = 1.0f)
    {
        _stagePanRoot = stagePanRoot;
        _stageZoomRoot = stageZoomRoot;
        _zoomToScale = zoomToScale;
        _minScale = minScale;
        _maxScale = maxScale;
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
        float zoomFactor = Mathf.Clamp(zoom, -10f, 10f);
        float scale = 1f + zoomFactor * _zoomToScale;
        return Mathf.Clamp(scale, _minScale, _maxScale);
    }
}