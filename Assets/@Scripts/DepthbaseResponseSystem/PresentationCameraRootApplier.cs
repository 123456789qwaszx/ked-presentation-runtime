using UnityEngine;

public interface IPresentationCameraRootProvider
{
    RectTransform StagePanRoot { get; }
    RectTransform StageZoomRoot { get; }
}

public sealed partial class PresentationUIRoot : IPresentationCameraRootProvider
{
    public RectTransform StagePanRoot => View.Rect(Refs.StagePan_Root);
    public RectTransform StageZoomRoot => View.Rect(Refs.StageZoom_Root);
}

public sealed class PresentationCameraRootApplier
{
    private IPresentationCameraRootProvider _cameraRootProvider;

    private readonly float _maxScale;
    private readonly float _panMultiplier;

    private bool _init;

    public PresentationCameraRootApplier(
        float maxScale = 5.0f,
        float panMultiplier = 1.0f)
    {
        _maxScale = Mathf.Max(1.0001f, maxScale);
        _panMultiplier = panMultiplier;
    }

    public void Apply(in PresentationIntentState state)
    {
        if (!_init)
            EnsureProvider();

        RectTransform stageZoomRoot = _cameraRootProvider?.StageZoomRoot;
        RectTransform stagePanRoot = _cameraRootProvider?.StagePanRoot;

        if (stageZoomRoot != null)
        {
            float scale = EvaluateScale(state.zoom);
            stageZoomRoot.localScale = new Vector3(scale, scale, 1f);
        }

        if (stagePanRoot != null)
            stagePanRoot.anchoredPosition = state.pan * _panMultiplier;
    }

    private float EvaluateScale(float zoom)
    {
        float t = Mathf.Clamp(zoom, -10f, 10f) / 10f;
        return Mathf.Pow(_maxScale, t);
    }

    private void EnsureProvider()
    {
        _cameraRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
        if (_cameraRootProvider != null)
            _init = true;
    }
}