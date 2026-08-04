using UnityEngine;

public enum UIStageBlurDownsample
{
    Full = 1,    // 1920x1080
    Half = 2,    // 960x540
    Quarter = 4, // 480x270
    Eighth = 8   // 240x135
}

// UICapture 카메라가 찍은 RenderTexture를 받아서,
// separable blur shader로 블러 처리한 결과를 _blurA에 만들어두는 컨트롤러
public sealed class UIStageBlurController : MonoBehaviour
{
    private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");
    private static readonly int BlurTexelSizeId = Shader.PropertyToID("_BlurTexelSize");

    [Header("Input")]
    [SerializeField] private Camera captureCamera;
    [SerializeField] private Material M_UISeparableBlur;

    [Header("Blur Settings")]
    [SerializeField] private UIStageBlurDownsample downsample = UIStageBlurDownsample.Quarter;
    [SerializeField, Range(1, 6)] private int iterations = 2;
    [SerializeField, Range(0f, 8f)] private float blurRadius = 3f;
    
    [SerializeField] private Color transparentClearColor = new(0f, 0f, 0f, 0f);

    [Header("Runtime")]
    // When true, the capture camera is disabled and rendered manually only during RenderBlur().
    [SerializeField] private bool controlCaptureCamera = true;

    private RenderTexture _blurA;
    private RenderTexture _blurB;
    private int _width;
    private int _height;
    private UIStageBlurDownsample _allocatedDownsample;

    public RenderTexture BlurredTexture => _blurA;

    // Exposed only for framing validation, such as checking source aspect ratio.
    public RenderTexture SourceTexture => captureCamera.targetTexture;

    private void OnEnable()
    {
        EnsureRenderTextures();
        ApplyCaptureCameraControl();
    }

    private void OnDisable() => ReleaseRenderTextures();
    private void OnDestroy() => ReleaseRenderTextures();

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
        ReleaseRenderTextures();
        EnsureRenderTextures();
    }

    private void ClearCaptureTarget()
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = captureCamera.targetTexture;

        GL.Clear(
            clearDepth: true,
            clearColor: true,
            backgroundColor: transparentClearColor);

        RenderTexture.active = prev;
    }

    // Renders the capture camera on demand and writes the blurred output to BlurredTexture.
    // Final output is stored in _blurA.
    public void RenderBlur()
    {
        RenderTexture source = captureCamera.targetTexture;

        if (controlCaptureCamera)
        {
            ClearCaptureTarget();
            captureCamera.Render();
        }

        EnsureRenderTextures();

        if (_blurA == null || _blurB == null)
            return;

        if (blurRadius <= 0f)
        {
            Graphics.Blit(source, _blurA);
            return;
        }

        M_UISeparableBlur.SetFloat(BlurRadiusId, blurRadius);
        M_UISeparableBlur.SetVector(
            BlurTexelSizeId,
            new Vector4(1f / _width, 1f / _height, _width, _height));

        Graphics.Blit(source, _blurA);

        for (int i = 0; i < iterations; i++)
        {
            Graphics.Blit(_blurA, _blurB, M_UISeparableBlur, 0);
            Graphics.Blit(_blurB, _blurA, M_UISeparableBlur, 1);
        }
    }

    private void ApplyCaptureCameraControl()
    {
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = transparentClearColor;

        if (controlCaptureCamera)
            captureCamera.enabled = false;
    }

    private void EnsureRenderTextures()
    {
        RenderTexture source = captureCamera.targetTexture;

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