using UnityEngine;
using UnityEngine.UI;

public sealed class UIBackgroundRigBlurCaptureProxy : MonoBehaviour
{
    [Header("Source Canvas")]
    [SerializeField] private Canvas sourceCanvas;

    [Header("Capture Canvas")]
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RectTransform captureRoot;

    [Header("Source Images")]
    [SerializeField] private Image sourceBackLayerImage;
    [SerializeField] private Image sourceFrontLayerImage;

    [Header("Capture Proxy Images")]
    [SerializeField] private Image captureBackLayerImage;
    [SerializeField] private Image captureFrontLayerImage;

    [Header("Copy Options")]
    [SerializeField] private bool copySprite = true;
    [SerializeField] private bool copyColor = true;
    [SerializeField] private bool multiplyInheritedCanvasGroupAlpha = true;
    [SerializeField] private bool copyMaterial = false;
    [SerializeField] private bool copyImageType = true;
    [SerializeField] private bool copyPreserveAspect = true;
    [SerializeField] private bool disableRaycastTarget = true;

    [Header("Runtime")]
    [SerializeField] private bool syncEveryFrame = true;

    private readonly ImageProxyPair _backPair = new();
    private readonly ImageProxyPair _frontPair = new();

    private void Awake()
    {
        RebuildPairs();
    }

    private void OnEnable()
    {
        RebuildPairs();
        Sync();
    }

    private void OnValidate()
    {
        RebuildPairs();
        Sync();
    }

    private void LateUpdate()
    {
        if (!syncEveryFrame)
            return;

        Sync();
    }

    public void Bind(BackgroundRigRefs sourceRefs)
    {
        if (sourceRefs == null)
        {
            sourceBackLayerImage = null;
            sourceFrontLayerImage = null;
        }
        else
        {
            sourceBackLayerImage = sourceRefs.Background_BackLayer_Image;
            sourceFrontLayerImage = sourceRefs.Background_FrontLayer_Image;
        }

        RebuildPairs();
        Sync();
    }

    public void Bind(
        Image backLayerImage,
        Image frontLayerImage)
    {
        sourceBackLayerImage = backLayerImage;
        sourceFrontLayerImage = frontLayerImage;

        RebuildPairs();
        Sync();
    }

    public void SetSourceCanvas(Canvas canvas)
    {
        sourceCanvas = canvas;
    }

    public void SetCaptureCanvas(Canvas canvas)
    {
        captureCanvas = canvas;
    }

    public void SetCaptureRoot(RectTransform root)
    {
        captureRoot = root;
    }

    public void SetCaptureProxyImages(Image backProxy, Image frontProxy)
    {
        captureBackLayerImage = backProxy;
        captureFrontLayerImage = frontProxy;

        RebuildPairs();
        Sync();
    }

    public void Sync()
    {
        if (captureRoot == null)
            return;

        SyncPair(_backPair);
        SyncPair(_frontPair);
    }

    public void ClearSources()
    {
        sourceBackLayerImage = null;
        sourceFrontLayerImage = null;

        RebuildPairs();

        ClearProxy(captureBackLayerImage);
        ClearProxy(captureFrontLayerImage);
    }

    private void RebuildPairs()
    {
        _backPair.Source = sourceBackLayerImage;
        _backPair.Proxy = captureBackLayerImage;

        _frontPair.Source = sourceFrontLayerImage;
        _frontPair.Proxy = captureFrontLayerImage;
    }

    private void SyncPair(ImageProxyPair pair)
    {
        if (pair.Proxy == null)
            return;

        if (pair.Source == null)
        {
            ClearProxy(pair.Proxy);
            return;
        }

        SyncGraphicState(pair.Source, pair.Proxy);
        SyncProxyRectToSource(pair.Source.rectTransform, pair.Proxy.rectTransform);
    }

    private void SyncGraphicState(Image source, Image proxy)
    {
        bool sourceVisible =
            source != null &&
            source.enabled &&
            source.gameObject.activeInHierarchy &&
            source.sprite != null;

        proxy.enabled = sourceVisible;

        if (!sourceVisible)
            return;

        if (disableRaycastTarget)
            proxy.raycastTarget = false;

        if (copySprite)
            proxy.sprite = source.sprite;

        if (copyColor)
        {
            Color color = source.color;

            if (multiplyInheritedCanvasGroupAlpha)
                color.a *= CalculateInheritedCanvasGroupAlpha(source.transform);

            proxy.color = color;
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
    }

    private void SyncProxyRectToSource(RectTransform sourceRect, RectTransform proxyRect)
    {
        if (sourceRect == null || proxyRect == null || captureRoot == null)
            return;

        Vector3[] worldCorners = RectCornerCache.WorldCorners;
        Vector2[] localCorners = RectCornerCache.LocalCorners;

        sourceRect.GetWorldCorners(worldCorners);

        Camera sourceCamera = GetCanvasCamera(sourceCanvas);
        Camera captureCamera = GetCanvasCamera(captureCanvas);

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                sourceCamera,
                worldCorners[i]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                captureRoot,
                screenPoint,
                captureCamera,
                out localCorners[i]);
        }

        Vector2 bottomLeft = localCorners[0];
        Vector2 topLeft = localCorners[1];
        Vector2 topRight = localCorners[2];
        Vector2 bottomRight = localCorners[3];

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

        proxyRect.anchorMin = new Vector2(0.5f, 0.5f);
        proxyRect.anchorMax = new Vector2(0.5f, 0.5f);
        proxyRect.pivot = new Vector2(0.5f, 0.5f);
        proxyRect.anchoredPosition = center;
        proxyRect.sizeDelta = new Vector2(width, height);
        proxyRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        proxyRect.localScale = Vector3.one;
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

    private sealed class ImageProxyPair
    {
        public Image Source;
        public Image Proxy;
    }

    private static class RectCornerCache
    {
        public static readonly Vector3[] WorldCorners = new Vector3[4];
        public static readonly Vector2[] LocalCorners = new Vector2[4];
    }
}