using UnityEngine.UI;

public partial class PresentationUIRoot
{
    private ScreenFlashEffectController _screenFlashEffect;

    public ScreenFlashEffectController GetScreenFlashEffect()
    {
        if (_screenFlashEffect != null)
            return _screenFlashEffect;

        Image image = View.Image(Refs.ScreenFlashOverlay_Image);

        if (image == null)
        {
            UnityEngine.Debug.LogWarning(
                "[PresentationUIRoot] ScreenFlashOverlay_Image is missing.",
                this);
            return null;
        }

        _screenFlashEffect = image.GetComponent<ScreenFlashEffectController>();

        if (_screenFlashEffect == null)
            _screenFlashEffect = image.gameObject.AddComponent<ScreenFlashEffectController>();

        return _screenFlashEffect;
    }
}