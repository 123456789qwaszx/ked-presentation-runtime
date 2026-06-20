using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class UIStageDepthLayerBlurRuntime
{
    private bool BakeLayerBlur(LayerState state, bool force)
    {
        ValidateCaptureFramingOnce();

        if (state.CharacterRigs == null && state.BackgroundRigs == null)
            return false;

        if (!_proxyPools.TryGetValue(state.Key, out ProxyPool proxyPool) || proxyPool == null)
            return false;

        _sourceCollector.Collect(
            state.Target.SourceContentRoot,
            state.CharacterRigs,
            state.BackgroundRigs,
            _sourceImageBuffer);

        if (_sourceImageBuffer.Count <= 0)
        {
            StopTrackingAndHideImmediate(state);
            return false;
        }

        DisableAllProxyPools();
        _currentBakeProxies.Clear();

        bool changed = false;

        for (int i = 0; i < _sourceImageBuffer.Count; i++)
        {
            SourceImageEntry source = _sourceImageBuffer[i];

            Image proxy = proxyPool.Acquire(i);

            if (proxy == null)
                continue;

            if (proxy.gameObject.layer != _captureLayer)
                proxy.gameObject.layer = _captureLayer;

            changed |= SyncGraphicState(source, proxy);
            changed |= SyncProxyRectToSource(source.Image.rectTransform, proxy.rectTransform);

            proxy.enabled = true;
            proxy.raycastTarget = false;

            _currentBakeProxies.Add(proxy);
        }

        if (!force && !changed)
            return false;

        blurController.SetDownsample(state.Downsample);
        blurController.SetBlur(state.BlurRadius, state.Iterations);

        IsolateForeignCaptureContent(_currentBakeProxies);

        Canvas.ForceUpdateCanvases();
        blurController.RenderBlur();

        RenderTexture blurredTexture = blurController.BlurredTexture;

        if (blurredTexture == null)
        {
            RestoreForeignCaptureContent();
            return false;
        }

        EnsureBakedTexture(state, blurredTexture);
        Graphics.Blit(blurredTexture, state.BakedTexture);

        RestoreForeignCaptureContent();

        return true;
    }

    // ── proxy 동기화 ────────────────────────────────────────────────────────────
    private static bool SyncGraphicState(SourceImageEntry source, Image proxy)
    {
        Image src = source.Image;
        bool changed = false;

        if (proxy.material != src.material)
        {
            proxy.material = src.material;
            changed = true;
        }

        if (proxy.sprite != src.sprite)
        {
            proxy.sprite = src.sprite;
            changed = true;
        }

        if (proxy.color != source.EffectiveColor)
        {
            proxy.color = source.EffectiveColor;
            changed = true;
        }

        if (proxy.type != src.type)
        {
            proxy.type = src.type;
            changed = true;
        }

        if (proxy.preserveAspect != src.preserveAspect)
        {
            proxy.preserveAspect = src.preserveAspect;
            changed = true;
        }

        if (proxy.fillCenter != src.fillCenter)
        {
            proxy.fillCenter = src.fillCenter;
            changed = true;
        }

        if (proxy.fillMethod != src.fillMethod)
        {
            proxy.fillMethod = src.fillMethod;
            changed = true;
        }

        if (proxy.fillOrigin != src.fillOrigin)
        {
            proxy.fillOrigin = src.fillOrigin;
            changed = true;
        }

        if (!Mathf.Approximately(proxy.fillAmount, src.fillAmount))
        {
            proxy.fillAmount = src.fillAmount;
            changed = true;
        }

        if (proxy.fillClockwise != src.fillClockwise)
        {
            proxy.fillClockwise = src.fillClockwise;
            changed = true;
        }

        if (!Mathf.Approximately(proxy.pixelsPerUnitMultiplier, src.pixelsPerUnitMultiplier))
        {
            proxy.pixelsPerUnitMultiplier = src.pixelsPerUnitMultiplier;
            changed = true;
        }

        return changed;
    }

    // source의 최종 화면 footprint(4 corners)를 captureRoot local로 옮겨 proxy에 그대로 적용.
    private bool SyncProxyRectToSource(RectTransform sourceRect, RectTransform proxyRect)
    {
        if (sourceRect == null || proxyRect == null)
            return false;

        sourceRect.GetWorldCorners(_sourceWorldCorners);

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _sourceWorldCorners[i]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                captureRoot,
                screenPoint,
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

    // ── 공유 캡처 격리 ──────────────────────────────────────────────────────────
    private void IsolateForeignCaptureContent(HashSet<Image> keepEnabled)
    {
        _foreignDisabledBuffer.Clear();

        if (captureRoot == null)
            return;

        captureRoot.GetComponentsInChildren(true, _captureImageScan);

        for (int i = 0; i < _captureImageScan.Count; i++)
        {
            Image image = _captureImageScan[i];

            if (image == null || !image.enabled || keepEnabled.Contains(image))
                continue;

            image.enabled = false;
            _foreignDisabledBuffer.Add(image);
        }
    }

    private void RestoreForeignCaptureContent()
    {
        for (int i = 0; i < _foreignDisabledBuffer.Count; i++)
        {
            if (_foreignDisabledBuffer[i] != null)
                _foreignDisabledBuffer[i].enabled = true;
        }

        _foreignDisabledBuffer.Clear();
    }

    // ── proxy pool 비활성 ──────────────────────────────────────────────────────
    private void DisableAllProxyPools()
    {
        foreach (KeyValuePair<LayerKey, ProxyPool> pair in _proxyPools)
            pair.Value?.DisableAll();
    }

    // ── baked texture(layer 전용 스냅샷) ───────────────────────────────────────
    private static void EnsureBakedTexture(LayerState state, RenderTexture source)
    {
        bool valid =
            state.BakedTexture != null &&
            state.BakedTexture.width == source.width &&
            state.BakedTexture.height == source.height &&
            state.BakedTexture.format == source.format;

        if (valid)
            return;

        ReleaseBakedTexture(state);

        state.BakedTexture = new RenderTexture(source.width, source.height, 0, source.format)
        {
            name = $"RT_{state.Key.Stage}_{state.Key.Layer}_BakedBlur",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };

        state.BakedTexture.Create();
    }

    private static void ReleaseBakedTexture(LayerState state)
    {
        RenderTexture bakedTexture = state.BakedTexture;

        if (bakedTexture == null)
            return;

        state.BakedTexture = null;

        if (bakedTexture.IsCreated())
            bakedTexture.Release();

        if (Application.isPlaying)
            Destroy(bakedTexture);
        else
            DestroyImmediate(bakedTexture);
    }
}