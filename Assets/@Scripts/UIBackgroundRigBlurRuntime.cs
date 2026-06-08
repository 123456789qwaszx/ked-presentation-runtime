using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public interface IPresentationDefocusOverlayProvider
{
    CanvasGroup FrostedGlassMaskCanvasGroup { get; }
    RawImage FrostedGlassRawImage { get; }
}

public sealed partial class PresentationUIRoot : IPresentationDefocusOverlayProvider
{
    public CanvasGroup FrostedGlassMaskCanvasGroup => View.Rect(Refs.FrostedGlassMask).GetComponent<CanvasGroup>();
    public RawImage FrostedGlassRawImage => View.Rect(Refs.FrostedGlassRawImage).GetComponent<RawImage>();
}

public sealed class UIBackgroundRigBlurRuntime : MonoBehaviour, IBackgroundRigBlurRuntime
{
    [Header("Capture Canvas")]
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RectTransform captureRoot;

    [Header("Capture Proxy Images")]
    [SerializeField] private Image captureBackLayerImage;
    [SerializeField] private Image captureFrontLayerImage;

    [Header("Blur")]
    [SerializeField] private UIStageBlurController blurController;

    private readonly Dictionary<string, RigBinding> _bindings = new();
    private readonly Vector3[] _sourceWorldCorners = new Vector3[4];
    private readonly Vector2[] _captureLocalCorners = new Vector2[4];

    private string _activeRigKey;
    private bool _isTracking;

    private Tween _defocusTween;

    private IPresentationDefocusOverlayProvider _overlay;
    private CanvasGroup _overlayCanvasGroup;
    private RawImage _overlayRawImage;

    private void LateUpdate()
    {
        if (!_isTracking)
            return;

        if (!SyncCaptureProxies())
            return;

        blurController.RenderBlur();
        ApplyBlurTextureToOverlay();
    }

    public void Bind(string rigKey, BackgroundRigRefs refs)
    {
        _bindings[rigKey] = new RigBinding(rigKey, refs);

        if (string.IsNullOrEmpty(_activeRigKey))
            _activeRigKey = rigKey;

        if (_activeRigKey == rigKey)
            SyncCaptureProxies();
    }

    public void ClearBindings()
    {
        _isTracking = false;
        _activeRigKey = null;
        _bindings.Clear();

        ResetCaptureProxies();
        SetOverlayVisible(false, 0f);

        _overlay = null;
        _overlayCanvasGroup = null;
        _overlayRawImage = null;
    }

    public void ShowDefocus(
        string rigKey,
        float alpha,
        float duration,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample)
    {
        EnsureOverlay();

        _activeRigKey = ResolveBinding(rigKey).RigKey;
        SyncCaptureProxies();

        blurController.SetDownsample(downsample);
        blurController.SetBlur(blurRadius, iterations);
        blurController.RenderBlur();

        ApplyBlurTextureToOverlay();

        float targetAlpha = Mathf.Clamp01(alpha);
        bool visible = targetAlpha > 0.001f;

        SetOverlayVisible(visible, duration, targetAlpha);
        _isTracking = visible;
    }

    public void HideDefocus(string rigKey, float duration)
    {
        EnsureOverlay();

        _isTracking = false;
        SetOverlayVisible(false, duration);
    }

    private void EnsureOverlay()
    {
        if (_overlay != null)
            return;

        _overlay = UIManager.Instance.GetUI<PresentationUIRoot>();
        _overlayCanvasGroup = _overlay.FrostedGlassMaskCanvasGroup;
        _overlayRawImage = _overlay.FrostedGlassRawImage;
    }

    private bool SyncCaptureProxies()
    {
        RigBinding active = ResolveBinding(_activeRigKey);

        Image sourceBack = active.Refs.Background_BackLayer_Image;
        Image sourceFront = active.Refs.Background_FrontLayer_Image;

        if (!IsCaptureSourceAlive(sourceBack, sourceFront))
        {
            StopTrackingAndClearActiveBinding();
            return false;
        }

        bool changed = false;

        changed |= SyncGraphicState(sourceBack, captureBackLayerImage);
        changed |= SyncProxyRectToSource(sourceBack.rectTransform, captureBackLayerImage.rectTransform);

        changed |= SyncGraphicState(sourceFront, captureFrontLayerImage);
        changed |= SyncProxyRectToSource(sourceFront.rectTransform, captureFrontLayerImage.rectTransform);

        return changed;
    }

    private bool SyncGraphicState(Image source, Image proxy)
    {
        bool sourceVisible =
            source.enabled &&
            source.gameObject.activeInHierarchy &&
            source.sprite != null;

        bool changed = false;

        if (proxy.enabled != sourceVisible)
        {
            proxy.enabled = sourceVisible;
            changed = true;
        }

        if (!sourceVisible)
            return changed;

        if (proxy.sprite != source.sprite)
        {
            proxy.sprite = source.sprite;
            changed = true;
        }

        if (proxy.color != source.color)
        {
            proxy.color = source.color;
            changed = true;
        }

        if (proxy.preserveAspect != source.preserveAspect)
        {
            proxy.preserveAspect = source.preserveAspect;
            changed = true;
        }

        return changed;
    }

    private bool SyncProxyRectToSource(RectTransform sourceRect, RectTransform proxyRect)
    {
        sourceRect.GetWorldCorners(_sourceWorldCorners);

        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                captureRoot,
                _sourceWorldCorners[i],
                captureCanvas.worldCamera,
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

    private void ApplyBlurTextureToOverlay()
    {
        if (_overlayRawImage == null)
            return;

        RenderTexture texture = blurController.BlurredTexture;
        if (texture == null) 
            return;

        if (_overlayRawImage.texture == texture) 
            return;

        _overlayRawImage.texture = texture;
    }

    private void SetOverlayVisible(bool visible, float duration, float visibleAlpha = 1f)
    {
        if (_overlayCanvasGroup == null || _overlayRawImage == null)
            return;

        _defocusTween?.Kill();
        _defocusTween = null;

        float targetAlpha = visible ? Mathf.Clamp01(visibleAlpha) : 0f;

        _overlayRawImage.raycastTarget = false;
        _overlayCanvasGroup.blocksRaycasts = false;
        _overlayCanvasGroup.interactable = false;

        if (visible)
            _overlayRawImage.enabled = true;

        if (duration <= 0f)
        {
            _overlayCanvasGroup.alpha = targetAlpha;

            if (targetAlpha <= 0.001f)
                _overlayRawImage.enabled = false;

            return;
        }

        _defocusTween = _overlayCanvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (targetAlpha <= 0.001f)
                    _overlayRawImage.enabled = false;
            });
    }
    
    private bool IsCaptureSourceAlive(Image sourceBack, Image sourceFront)
    {
        return sourceBack && sourceFront;
    }
    
    private void StopTrackingAndClearActiveBinding()
    {
        string deadRigKey = _activeRigKey;

        _isTracking = false;
        _activeRigKey = null;

        if (!string.IsNullOrEmpty(deadRigKey))
            _bindings.Remove(deadRigKey);

        ResetCaptureProxies();
        SetOverlayVisible(false, 0f);
    }

    private void ResetCaptureProxies()
    {
        captureBackLayerImage.enabled = false;
        captureBackLayerImage.sprite = null;
        captureBackLayerImage.material = null;

        captureFrontLayerImage.enabled = false;
        captureFrontLayerImage.sprite = null;
        captureFrontLayerImage.material = null;
    }

    private RigBinding ResolveBinding(string rigKey)
    {
        string key = string.IsNullOrEmpty(rigKey) ? _activeRigKey : rigKey;
        return _bindings[key];
    }

    private sealed class RigBinding
    {
        public readonly string RigKey;
        public readonly BackgroundRigRefs Refs;

        public RigBinding(string rigKey, BackgroundRigRefs refs)
        {
            RigKey = rigKey;
            Refs = refs;
        }
    }
}