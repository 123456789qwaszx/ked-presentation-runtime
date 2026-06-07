using UnityEngine;
using UnityEngine.UI;

public enum UIStageBlurDownsample
{
    Full = 1,
    Half = 2,
    Quarter = 4,
    Eighth = 8
}

public sealed class UIStageBlurController : MonoBehaviour
{
    private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");
    private static readonly int BlurTexelSizeId = Shader.PropertyToID("_BlurTexelSize");

    [Header("Input")]
    [SerializeField] private Camera captureCamera;

    [Header("Output Preview")]
    [SerializeField] private RawImage previewRawImage;

    [Header("Material")]
    [SerializeField] private Material blurMaterial;

    [Header("Blur Settings")]
    [SerializeField] private UIStageBlurDownsample downsample = UIStageBlurDownsample.Quarter;
    [SerializeField, Range(1, 6)] private int iterations = 2;
    [SerializeField, Range(0f, 8f)] private float blurRadius = 3f;

    [Header("Runtime")]
    [SerializeField] private bool renderEveryFrame = true;

    private RenderTexture _blurA;
    private RenderTexture _blurB;
    private int _width;
    private int _height;
    private UIStageBlurDownsample _allocatedDownsample;

    public RenderTexture SourceTexture => GetSourceTexture();
    public RenderTexture BlurredTexture => _blurA;

    public bool IsReady =>
        captureCamera != null &&
        captureCamera.targetTexture != null &&
        blurMaterial != null &&
        _blurA != null &&
        _blurB != null;

    private void OnEnable()
    {
        EnsureRenderTextures();
        ApplyPreviewTexture();
    }

    private void OnDisable()
    {
        ReleaseRenderTextures();
    }

    private void OnDestroy()
    {
        ReleaseRenderTextures();
    }

    private void LateUpdate()
    {
        if (!renderEveryFrame)
            return;

        RenderBlur();
    }

    public void SetCaptureCamera(Camera camera)
    {
        if (captureCamera == camera)
            return;

        captureCamera = camera;
        RecreateRenderTextures();
    }

    public void SetPreview(RawImage rawImage)
    {
        previewRawImage = rawImage;
        ApplyPreviewTexture();
    }

    public void SetBlur(float radius, int iterationCount)
    {
        blurRadius = Mathf.Max(0f, radius);
        iterations = Mathf.Clamp(iterationCount, 1, 6);
    }

    public void SetDownsample(UIStageBlurDownsample value)
    {
        if (downsample == value)
            return;

        downsample = value;
        RecreateRenderTextures();
    }

    public void ForceRenderBlur()
    {
        RenderBlur();
    }

    public void ClearPreview()
    {
        if (previewRawImage != null)
            previewRawImage.texture = null;
    }

    private void RenderBlur()
    {
        RenderTexture source = GetSourceTexture();

        if (source == null || blurMaterial == null)
            return;

        EnsureRenderTextures();

        if (_blurA == null || _blurB == null)
            return;

        if (blurRadius <= 0f)
        {
            Graphics.Blit(source, _blurA);
            return;
        }

        blurMaterial.SetFloat(BlurRadiusId, blurRadius);
        blurMaterial.SetVector(
            BlurTexelSizeId,
            new Vector4(1f / _width, 1f / _height, _width, _height));

        Graphics.Blit(source, _blurA);

        for (int i = 0; i < iterations; i++)
        {
            Graphics.Blit(_blurA, _blurB, blurMaterial, 0);
            Graphics.Blit(_blurB, _blurA, blurMaterial, 1);
        }
    }

    private RenderTexture GetSourceTexture()
    {
        if (captureCamera == null)
            return null;

        return captureCamera.targetTexture;
    }

    private void EnsureRenderTextures()
    {
        RenderTexture source = GetSourceTexture();

        if (source == null)
            return;

        int downsampleValue = Mathf.Max(1, (int)downsample);

        int nextWidth = Mathf.Max(1, source.width / downsampleValue);
        int nextHeight = Mathf.Max(1, source.height / downsampleValue);

        bool sizeMatches =
            _blurA != null &&
            _blurB != null &&
            _width == nextWidth &&
            _height == nextHeight &&
            _allocatedDownsample == downsample;

        if (sizeMatches)
            return;

        ReleaseRenderTextures();

        _width = nextWidth;
        _height = nextHeight;
        _allocatedDownsample = downsample;

        _blurA = CreateRT(_width, _height, "RT_UIStageBlur_A");
        _blurB = CreateRT(_width, _height, "RT_UIStageBlur_B");

        ApplyPreviewTexture();
    }

    private RenderTexture CreateRT(int width, int height, string textureName)
    {
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        rt.Create();
        return rt;
    }

    private void ApplyPreviewTexture()
    {
        if (previewRawImage == null)
            return;

        previewRawImage.texture = _blurA;
    }

    private void RecreateRenderTextures()
    {
        ReleaseRenderTextures();
        EnsureRenderTextures();
        ApplyPreviewTexture();
    }

    private void ReleaseRenderTextures()
    {
        ReleaseRT(ref _blurA);
        ReleaseRT(ref _blurB);

        _width = 0;
        _height = 0;
    }

    private void ReleaseRT(ref RenderTexture rt)
    {
        if (rt == null)
            return;

        if (rt.IsCreated())
            rt.Release();

        if (Application.isPlaying)
            Destroy(rt);
        else
            DestroyImmediate(rt);

        rt = null;
    }
}