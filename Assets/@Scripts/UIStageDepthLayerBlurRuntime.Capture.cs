using UnityEngine;

// - EnsureCaptureGraph
// - proxy root/pool registration
// - captureRoot fullscreen 강제
// - capture RT framing 검증
// - SetLayerRecursive
public sealed partial class UIStageDepthLayerBlurRuntime
{
    // ── capture graph 구성 ─────────────────────────────────────────────────────
    private void EnsureCaptureGraph()
    {
        if (_captureGraphBuilt)
            return;

        // proxy 좌표는 "스크린 좌표"다. captureRoot가 화면 전체와 1:1로 겹쳐야
        // source RT가 화면 기준이 되고 overlay(default uvRect)가 맞는다.
        ForceCaptureRootFullScreen();

        _captureBuilder.EnsureAndBind(captureRoot, out _captureRefs);
        BuildProxyPools();

        // (핵심) 런타임 생성 캡처 오브젝트가 캡처 카메라 culling mask 밖(Default layer)으로
        // 떨어져 컬링되는 것을 막는다. captureRoot의 layer로 서브트리 전체를 통일한다.
        _captureLayer = captureRoot.gameObject.layer;
        SetLayerRecursive(captureRoot, _captureLayer);

        _captureGraphBuilt = true;
    }

    private void ForceCaptureRootFullScreen()
    {
        captureRoot.anchorMin = Vector2.zero;
        captureRoot.anchorMax = Vector2.one;
        captureRoot.pivot = new Vector2(0.5f, 0.5f);
        captureRoot.offsetMin = Vector2.zero;
        captureRoot.offsetMax = Vector2.zero;
        captureRoot.localScale = Vector3.one;
        captureRoot.localRotation = Quaternion.identity;
    }

    private void BuildProxyPools()
    {
        _proxyPools.Clear();

        foreach (PresentationStageKey stage in StageKeys)
        foreach (PresentationDepthLayerKey layer in LayerKeys)
            RegisterProxyPool(stage, layer);
    }

    private void RegisterProxyPool(PresentationStageKey stage, PresentationDepthLayerKey layer)
    {
        if (_captureRefs == null)
            return;

        if (!_captureRefs.TryGetRoot(stage, layer, out RectTransform root) || root == null)
        {
            Debug.LogWarning($"[UIStageDepthLayerBlurRuntime] Missing proxy root. stage='{stage}' layer='{layer}'.");

            return;
        }

        _proxyPools[new LayerKey(stage, layer)] = new ProxyPool(stage, layer, root, _captureBuilder);
    }

    // 공유 source RT 종횡비가 화면과 어긋나면 off-center rig가 한 축으로 거리 비례로 밀린다. 1회 경고.
    private void ValidateCaptureFramingOnce()
    {
        if (_captureFramingValidated || blurController == null)
            return;

        RenderTexture sourceRt = blurController.SourceTexture;

        if (sourceRt == null)
            return;

        _captureFramingValidated = true;

        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float rtAspect = (float)sourceRt.width / Mathf.Max(1, sourceRt.height);

        if (Mathf.Abs(screenAspect - rtAspect) > 0.01f)
        {
            Debug.LogWarning(
                $"[UIStageDepthLayerBlurRuntime] Capture RT aspect({rtAspect:F3}) != screen aspect({screenAspect:F3}). " +
                "capture camera/RT를 화면 종횡비 1:1로 맞춰라. off-center rig가 거리 비례로 어긋난다.");
        }
    }
    
    private static void SetLayerRecursive(Transform root, int layer)
    {
        if (root == null)
            return;

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursive(root.GetChild(i), layer);
    }
}