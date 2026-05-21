using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public sealed class FocusBlurFadeOverlay : MonoBehaviour
{
    [Header("Optional Zoom Root")]
    [SerializeField] private RectTransform _zoomRoot;

    [Header("Input")]
    [SerializeField] private bool _blockRaycastWhenVisible;

    private CanvasGroup _canvasGroup;
    private Image _image;
    private Vector3 _zoomRootBaseScale = Vector3.one;
    private bool _hasBaseScale;

    public CanvasGroup CanvasGroup
    {
        get
        {
            EnsureRefs();
            return _canvasGroup;
        }
    }

    public Image Image
    {
        get
        {
            EnsureRefs();
            return _image;
        }
    }

    public RectTransform ZoomRoot => _zoomRoot;

    public bool BlockRaycastWhenVisible
    {
        get => _blockRaycastWhenVisible;
        set
        {
            _blockRaycastWhenVisible = value;
            ApplyRaycastState();
        }
    }

    private void Awake()
    {
        EnsureRefs();
        CaptureBaseScaleIfNeeded();
        ApplyRaycastState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRefs();
        ApplyRaycastState();
    }
#endif

    public void SetZoomRoot(RectTransform zoomRoot)
    {
        _zoomRoot = zoomRoot;
        _hasBaseScale = false;
        CaptureBaseScaleIfNeeded();
    }

    public void CaptureBaseScaleIfNeeded()
    {
        if (_hasBaseScale)
            return;

        if (_zoomRoot == null)
            return;

        _zoomRootBaseScale = _zoomRoot.localScale;
        _hasBaseScale = true;
    }

    public Vector3 GetBaseScale()
    {
        CaptureBaseScaleIfNeeded();
        return _zoomRootBaseScale;
    }

    public void SetAlpha(float alpha)
    {
        EnsureRefs();

        alpha = Mathf.Clamp01(alpha);
        _canvasGroup.alpha = alpha;

        ApplyRaycastState();
    }

    public void SetColor(Color color)
    {
        EnsureRefs();
        _image.color = color;
    }

    public void SetZoomAmount(float zoomAmount)
    {
        if (_zoomRoot == null)
            return;

        CaptureBaseScaleIfNeeded();

        float scale = Mathf.Max(0.01f, 1f + zoomAmount);
        _zoomRoot.localScale = _zoomRootBaseScale * scale;
    }

    public void ResetZoom()
    {
        if (_zoomRoot == null)
            return;

        CaptureBaseScaleIfNeeded();
        _zoomRoot.localScale = _zoomRootBaseScale;
    }

    public void ClearImmediate()
    {
        SetAlpha(0f);
        ResetZoom();
        gameObject.SetActive(false);
    }

    public void CoverImmediate(float alpha, float zoomAmount)
    {
        gameObject.SetActive(true);
        SetAlpha(alpha);
        SetZoomAmount(zoomAmount);
    }

    private void EnsureRefs()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_image == null)
            _image = GetComponent<Image>();

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
        }

        if (_image != null)
        {
            _image.raycastTarget = false;
        }
    }

    private void ApplyRaycastState()
    {
        if (_canvasGroup == null)
            return;

        bool shouldBlock =
            _blockRaycastWhenVisible &&
            _canvasGroup.alpha > 0.001f;

        _canvasGroup.blocksRaycasts = shouldBlock;
    }
}