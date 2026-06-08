using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBackgroundRigBlurRuntime : MonoBehaviour, IBackgroundRigBlurRuntime
{
    private const string CaptureBackProxyName = "Capture_BackLayer_Image";
    private const string CaptureFrontProxyName = "Capture_FrontLayer_Image";

    [Header("Source / Capture Canvas")]
    [SerializeField] private Canvas sourceCanvas;
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RectTransform captureRoot;

    [Header("Capture Proxy Images")]
    [SerializeField] private Image captureBackLayerImage;
    [SerializeField] private Image captureFrontLayerImage;

    [Header("Blur")]
    [SerializeField] private UIStageBlurController blurController;
    
    [Header("External Presentation Mask")]
    [SerializeField] private CanvasGroup frostedGlassMaskCanvasGroup;
    [SerializeField] private bool controlFrostedGlassMask = true;

    [Header("Copy Options")]
    [SerializeField] private bool copySprite = true;
    [SerializeField] private bool copyColor = true;
    [SerializeField] private bool multiplyInheritedCanvasGroupAlpha = true;
    [SerializeField] private bool copyMaterial = false;
    [SerializeField] private bool copyImageType = true;
    [SerializeField] private bool copyPreserveAspect = true;
    [SerializeField] private bool disableRaycastTarget = true;

    private readonly Dictionary<string, RigBinding> _bindings = new();
    private readonly Vector3[] _sourceWorldCorners = new Vector3[4];
    private readonly Vector2[] _captureLocalCorners = new Vector2[4];

    private string _activeRigKey;

    // 디포커스가 떠 있는 동안에만 소스를 미러링/재블러한다. 꺼져 있으면 파이프라인 전체 정지.
    private bool _isTracking;

    private void Awake()
    {
        EnsureCaptureProxyImages();
    }

    private void OnEnable()
    {
        EnsureCaptureProxyImages();
        SetFrostedGlassMaskVisible(false, 0f);
        Sync();
    }

    private void OnValidate()
    {
        EnsureCaptureProxyImages();
        Sync();
    }

    private void LateUpdate()
    {
        if (!_isTracking)
            return;

        RigBinding active = ResolveBinding(_activeRigKey);
        if (active == null)
            return;

        // Sync는 변경 여부를 반환한다. 정지 프레임이면 false → 캡처/블러 비용 0.
        bool changed = Sync();
        if (!changed)
            return;

        if (blurController != null)
            blurController.ForceRenderBlur();

        ApplyBlurTexture(active);
    }

    public void Bind(string rigKey, BackgroundRigRefs refs)
    {
        if (string.IsNullOrEmpty(rigKey) || refs == null)
            return;

        EnsureCaptureProxyImages();

        RigBinding binding = new RigBinding(rigKey, refs);
        SetupOverlay(binding);

        _bindings[rigKey] = binding;

        if (string.IsNullOrEmpty(_activeRigKey))
            _activeRigKey = rigKey;

        Sync();
    }

    public void ClearSources()
    {
        _isTracking = false;

        _activeRigKey = null;
        _bindings.Clear();

        ClearProxy(captureBackLayerImage);
        ClearProxy(captureFrontLayerImage);
    }

    public void ShowDefocus(
        string rigKey,
        float alpha,
        float duration,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample)
    {
        RigBinding binding = ResolveBinding(rigKey);

        if (binding == null)
            return;

        _activeRigKey = binding.RigKey;

        // 첫 블러를 굽기 전에 프록시를 현재 소스 상태로 맞춘다.
        Sync();

        if (blurController != null)
        {
            blurController.SetDownsample(downsample);
            blurController.SetBlur(blurRadius, iterations);
            blurController.ForceRenderBlur();
        }

        ApplyBlurTexture(binding);
        float targetAlpha = Mathf.Clamp01(alpha);

        ApplyBlurTexture(binding);
        FadeOverlay(binding, targetAlpha, duration);
        SetFrostedGlassMaskVisible(targetAlpha > 0.001f, duration);

        // 디포커스가 떠 있는 동안만 추적 시작. 배경이 정지면 매 프레임 비용은 Sync(코너 투영)뿐.
        _isTracking = targetAlpha > 0.001f;
    }

    public void HideDefocus(string rigKey, float duration)
    {
        RigBinding binding = ResolveBinding(rigKey);

        if (binding == null)
            return;

        // 마지막 블러 프레임을 고정. 페이드 아웃은 구워둔 RT에 알파만 애니메이션한다.
        _isTracking = false;

        FadeOverlay(binding, 0f, duration);
        SetFrostedGlassMaskVisible(false, duration);
    }

    public void ClearDefocusImmediate(string rigKey)
    {
        _isTracking = false;
        SetFrostedGlassMaskVisible(false, 0f);

        RigBinding binding = ResolveBinding(rigKey);

        if (binding == null)
            return;

        binding.FadeTween?.Kill();
        binding.FadeTween = null;

        if (binding.OverlayCanvasGroup != null)
            binding.OverlayCanvasGroup.alpha = 0f;

        if (binding.OverlayRawImage != null)
            binding.OverlayRawImage.enabled = false;
    }

    private bool Sync()
    {
        RigBinding active = ResolveBinding(_activeRigKey);

        if (active == null)
        {
            ClearProxy(captureBackLayerImage);
            ClearProxy(captureFrontLayerImage);
            return false;
        }

        bool changed = false;
        changed |= SyncPair(active.Refs.Background_BackLayer_Image, captureBackLayerImage);
        changed |= SyncPair(active.Refs.Background_FrontLayer_Image, captureFrontLayerImage);
        return changed;
    }

    private bool SyncPair(Image source, Image proxy)
    {
        if (proxy == null)
            return false;

        if (source == null)
        {
            bool wasVisible = proxy.enabled || proxy.sprite != null;
            ClearProxy(proxy);
            return wasVisible;
        }

        bool changed = SyncGraphicState(source, proxy);
        changed |= SyncProxyRectToSource(source.rectTransform, proxy.rectTransform);
        return changed;
    }

    private bool SyncGraphicState(Image source, Image proxy)
    {
        bool changed = false;

        bool sourceVisible =
            source != null &&
            source.enabled &&
            source.gameObject.activeInHierarchy &&
            source.sprite != null;

        if (proxy.enabled != sourceVisible)
        {
            proxy.enabled = sourceVisible;
            changed = true;
        }

        if (!sourceVisible)
            return changed;

        if (disableRaycastTarget)
            proxy.raycastTarget = false;

        if (copySprite && proxy.sprite != source.sprite)
        {
            proxy.sprite = source.sprite;
            changed = true;
        }

        if (copyColor)
        {
            Color color = source.color;

            if (multiplyInheritedCanvasGroupAlpha)
                color.a *= CalculateInheritedCanvasGroupAlpha(source.transform);

            if (proxy.color != color)
            {
                proxy.color = color;
                changed = true;
            }
        }

        if (copyMaterial)
            proxy.material = source.material;
        else
            proxy.material = null;

        if (copyImageType)
        {
            proxy.type = source.type;
            proxy.fillMethod = source.fillMethod;
            proxy.fillOrigin = source.fillOrigin;
            proxy.fillAmount = source.fillAmount;
            proxy.fillClockwise = source.fillClockwise;
            proxy.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }

        if (copyPreserveAspect)
            proxy.preserveAspect = source.preserveAspect;

        return changed;
    }

    private bool SyncProxyRectToSource(RectTransform sourceRect, RectTransform proxyRect)
    {
        if (sourceRect == null || proxyRect == null || captureRoot == null)
            return false;

        sourceRect.GetWorldCorners(_sourceWorldCorners);

        Camera sourceCamera = GetCanvasCamera(sourceCanvas);
        Camera captureCamera = GetCanvasCamera(captureCanvas);

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                sourceCamera,
                _sourceWorldCorners[i]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                captureRoot,
                screenPoint,
                captureCamera,
                out _captureLocalCorners[i]);
        }

        Vector2 bottomLeft = _captureLocalCorners[0];
        Vector2 topLeft = _captureLocalCorners[1];
        Vector2 topRight = _captureLocalCorners[2];
        Vector2 bottomRight = _captureLocalCorners[3];

        Vector2 center = (bottomLeft + topLeft + topRight + bottomRight) * 0.25f;

        float width = Vector2.Distance(bottomLeft, bottomRight);
        float height = Vector2.Distance(bottomLeft, topLeft);

        Vector2 rightDirection = bottomRight - bottomLeft;
        float angle = 0f;

        if (rightDirection.sqrMagnitude > 0.0001f)
        {
            rightDirection.Normalize();
            angle = Mathf.Atan2(rightDirection.y, rightDirection.x) * Mathf.Rad2Deg;
        }

        // 직전 프레임(=프록시 현재 값)과 비교해 변경 여부 판단. 임계치로 서브픽셀 지터 흡수.
        bool changed =
            (proxyRect.anchoredPosition - center).sqrMagnitude > 0.01f ||
            Mathf.Abs(proxyRect.sizeDelta.x - width) > 0.05f ||
            Mathf.Abs(proxyRect.sizeDelta.y - height) > 0.05f ||
            Mathf.Abs(Mathf.DeltaAngle(proxyRect.localEulerAngles.z, angle)) > 0.05f;

        proxyRect.anchorMin = new Vector2(0.5f, 0.5f);
        proxyRect.anchorMax = new Vector2(0.5f, 0.5f);
        proxyRect.pivot = new Vector2(0.5f, 0.5f);
        proxyRect.anchoredPosition = center;
        proxyRect.sizeDelta = new Vector2(width, height);
        proxyRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        proxyRect.localScale = Vector3.one;

        return changed;
    }

    private void SetupOverlay(RigBinding binding)
    {
        BackgroundRigRefs refs = binding.Refs;

        RawImage rawImage = refs.Background_DefocusOverlay_RawImage;
        RectTransform overlayRoot = refs.Background_DefocusOverlay_Root;

        if (rawImage == null || overlayRoot == null)
            return;

        CanvasGroup canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = overlayRoot.gameObject.AddComponent<CanvasGroup>();

        rawImage.raycastTarget = false;
        rawImage.color = Color.white;
        rawImage.enabled = false;

        canvasGroup.alpha = 0f;

        binding.OverlayRawImage = rawImage;
        binding.OverlayCanvasGroup = canvasGroup;

        ApplyBlurTexture(binding);
    }

    private void ApplyBlurTexture(RigBinding binding)
    {
        if (binding == null || binding.OverlayRawImage == null || blurController == null)
            return;

        RenderTexture texture = blurController.BlurredTexture;

        if (texture == null)
            return;

        if (binding.OverlayRawImage.texture == texture)
            return;

        binding.OverlayRawImage.texture = texture;
    }

    private void FadeOverlay(RigBinding binding, float targetAlpha, float duration)
    {
        if (binding == null || binding.OverlayCanvasGroup == null)
            return;

        binding.FadeTween?.Kill();
        binding.FadeTween = null;

        if (binding.OverlayRawImage != null)
            binding.OverlayRawImage.enabled = true;

        if (duration <= 0f)
        {
            binding.OverlayCanvasGroup.alpha = targetAlpha;

            if (binding.OverlayRawImage != null && targetAlpha <= 0.001f)
                binding.OverlayRawImage.enabled = false;

            return;
        }

        binding.FadeTween = binding.OverlayCanvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (binding.OverlayRawImage != null && targetAlpha <= 0.001f)
                    binding.OverlayRawImage.enabled = false;
            });
    }
    
    private void SetFrostedGlassMaskVisible(bool visible, float duration)
    {
        if (!controlFrostedGlassMask || frostedGlassMaskCanvasGroup == null)
            return;

        frostedGlassMaskCanvasGroup.DOKill(false);

        float targetAlpha = visible ? 1f : 0f;

        frostedGlassMaskCanvasGroup.blocksRaycasts = visible;
        frostedGlassMaskCanvasGroup.interactable = visible;

        if (duration <= 0f)
        {
            frostedGlassMaskCanvasGroup.alpha = targetAlpha;
            return;
        }

        frostedGlassMaskCanvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                bool isVisible = targetAlpha > 0.001f;
                frostedGlassMaskCanvasGroup.blocksRaycasts = isVisible;
                frostedGlassMaskCanvasGroup.interactable = isVisible;
            });
    }

    private RigBinding ResolveBinding(string rigKey)
    {
        if (!string.IsNullOrEmpty(rigKey) && _bindings.TryGetValue(rigKey, out RigBinding binding))
            return binding;

        if (!string.IsNullOrEmpty(_activeRigKey) && _bindings.TryGetValue(_activeRigKey, out binding))
            return binding;

        return null;
    }

    private void EnsureCaptureProxyImages()
    {
        if (captureRoot == null)
            return;

        if (captureBackLayerImage == null)
            captureBackLayerImage = EnsureProxyImage(CaptureBackProxyName);

        if (captureFrontLayerImage == null)
            captureFrontLayerImage = EnsureProxyImage(CaptureFrontProxyName);
    }

    private Image EnsureProxyImage(string objectName)
    {
        RectTransform existing = FindDirectChild(captureRoot, objectName);

        if (existing == null)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            existing = (RectTransform)go.transform;
            existing.SetParent(captureRoot, false);
            existing.gameObject.layer = captureRoot.gameObject.layer;
        }

        Image image = existing.GetComponent<Image>();
        if (image == null)
            image = existing.gameObject.AddComponent<Image>();

        image.raycastTarget = false;
        image.enabled = false;

        return image;
    }

    private RectTransform FindDirectChild(RectTransform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child as RectTransform;
        }

        return null;
    }

    private float CalculateInheritedCanvasGroupAlpha(Transform source)
    {
        float alpha = 1f;

        Transform current = source;
        while (current != null)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null)
                alpha *= group.alpha;

            if (sourceCanvas != null && current == sourceCanvas.transform)
                break;

            current = current.parent;
        }

        return alpha;
    }

    private Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void ClearProxy(Image proxy)
    {
        if (proxy == null)
            return;

        proxy.enabled = false;
        proxy.sprite = null;
        proxy.material = null;
    }

    private sealed class RigBinding
    {
        public readonly string RigKey;
        public readonly BackgroundRigRefs Refs;

        public RawImage OverlayRawImage;
        public CanvasGroup OverlayCanvasGroup;
        public Tween FadeTween;

        public RigBinding(string rigKey, BackgroundRigRefs refs)
        {
            RigKey = rigKey;
            Refs = refs;
        }
    }
}