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

    public void Apply(in PresentationIntentState state)
    {
        if (!TryEnsureCameraRootProvider())
            return;

        float scale = PresentationShotIntentMath.EvaluateCameraScale(state.zoom);

        _cameraRootProvider.StageZoomRoot.localScale = new Vector3(scale, scale, 1f);
        _cameraRootProvider.StagePanRoot.anchoredPosition = state.panInRigSpace;
    }

    private bool TryEnsureCameraRootProvider()
    {
        if (_cameraRootProvider != null)
            return true;

        _cameraRootProvider = UIManager.Instance.GetUI<PresentationUIRoot>();

        if (_cameraRootProvider == null)
            return false;

        return true;
    }
}