using UnityEngine;
using UnityEngine.UI;

public partial class PresentationUIRoot
{
    private ScreenNoiseEffectController _screenNoiseEffect;

    public ScreenNoiseEffectController GetScreenNoiseEffect()
    {
        if (_screenNoiseEffect != null)
            return _screenNoiseEffect;

        Image image = View.Image(Refs.ScreenNoiseOverlay_Image);

        if (image == null)
        {
            Debug.LogWarning(
                "[PresentationUIRoot] ScreenNoiseOverlay_Image is missing.",
                this);
            return null;
        }

        _screenNoiseEffect = image.GetComponent<ScreenNoiseEffectController>();

        if (_screenNoiseEffect == null)
            _screenNoiseEffect = image.gameObject.AddComponent<ScreenNoiseEffectController>();

        return _screenNoiseEffect;
    }
}