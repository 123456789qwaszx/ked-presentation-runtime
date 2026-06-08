using UnityEngine;
using UnityEngine.UI;

public sealed class UIBlurredTextureBinder : MonoBehaviour
{
    [SerializeField] private UIStageBlurController blurController;
    [SerializeField] private RawImage targetRawImage;

    private void Reset()
    {
        targetRawImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        if (blurController == null || targetRawImage == null)
            return;

        RenderTexture texture = blurController.BlurredTexture;

        if (texture == null)
            return;

        if (targetRawImage.texture == texture)
            return;

        targetRawImage.texture = texture;
    }
}