using UnityEngine;
using UnityEngine.Serialization;

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
    [SerializeField] private Material M_UISeparableBlur;

    [Header("Blur Settings")]
    [SerializeField] private UIStageBlurDownsample downsample = UIStageBlurDownsample.Quarter;
    [SerializeField, Range(1, 6)] private int iterations = 2;
    [SerializeField, Range(0f, 8f)] private float blurRadius = 3f;
    
    [SerializeField] private Color transparentClearColor = new(0f, 0f, 0f, 0f);

    [Header("Runtime")]
    // 캡처 카메라를 이 컨트롤러가 수동으로 구동한다(평소엔 렌더하지 않음).
    [SerializeField] private bool controlCaptureCamera = true;

    private RenderTexture _blurA;
    private RenderTexture _blurB;
    private int _width;
    private int _height;
    private UIStageBlurDownsample _allocatedDownsample;

    public RenderTexture BlurredTexture => _blurA;

    // 캡처 카메라가 렌더 대상으로 잡고 있는 source RT.
    // 프레이밍 1:1 검증(종횡비 비교) 용도로만 외부에서 읽는다. 저수준 blur 역할은 그대로다.
    public RenderTexture SourceTexture => captureCamera != null ? captureCamera.targetTexture : null;

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
        RecreateRenderTextures();
    }

    private void ClearCaptureTarget()
    {
        if (captureCamera == null || captureCamera.targetTexture == null)
            return;

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = captureCamera.targetTexture;

        GL.Clear(
            clearDepth: true,
            clearColor: true,
            backgroundColor: Color.clear);

        RenderTexture.active = prev;
    }

    // 온디맨드 구동 진입점: 캡처 카메라를 한 번 렌더한 뒤 블러를 굽는다.
    public void RenderBlur()
    {
        if (captureCamera == null)
            return;

        RenderTexture source = captureCamera.targetTexture;

        if (source == null || M_UISeparableBlur == null)
            return;

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
        if (captureCamera == null)
            return;

        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = transparentClearColor;

        if (controlCaptureCamera)
            captureCamera.enabled = false;
    }

    private void EnsureRenderTextures()
    {
        RenderTexture source = captureCamera.targetTexture;

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

    private void RecreateRenderTextures()
    {
        ReleaseRenderTextures();
        EnsureRenderTextures();
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