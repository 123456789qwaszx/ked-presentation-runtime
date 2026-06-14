using UnityEngine;

public sealed class PresentationCameraRootApplier
{
    private IShotResponseStageProvider _cameraRootProvider;

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