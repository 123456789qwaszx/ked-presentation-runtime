using UnityEngine;
using UnityEngine.UI;

public partial class PresentationUIRoot
{
    private ScreenVignetteEffectController _screenVignetteEffect;

    public ScreenVignetteEffectController GetScreenVignetteEffect()
    {
        if (_screenVignetteEffect != null)
            return _screenVignetteEffect;

        Image image = View.Image(Refs.ScreenVignetteOverlay_Image);

        if (image == null)
        {
            Debug.LogWarning(
                "[PresentationUIRoot] ScreenVignetteOverlay_Image is missing.",
                this);
            return null;
        }

        _screenVignetteEffect = image.GetComponent<ScreenVignetteEffectController>();

        if (_screenVignetteEffect == null)
            _screenVignetteEffect = image.gameObject.AddComponent<ScreenVignetteEffectController>();

        return _screenVignetteEffect;
    }
}