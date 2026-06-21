using UnityEngine;

public sealed partial class UIStageDepthLayerBlurRuntime
{
    private void EnsureCaptureGraph()
    {
        if (_captureGraphBuilt)
            return;

        ForceCaptureRootFullScreen();

        _captureBuilder.EnsureAndBind(captureRoot, out _captureRefs);
        BuildProxyPools();

        // 런타임 생성 캡처 오브젝트가
        // 캡처 카메라 culling mask 밖(Default layer)으로 떨어져 컬링되는 것을 방지.
        // captureRoot의 layer로 서브트리 전체를 통일.
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
        _captureRefs.TryGetRoot(stage, layer, out RectTransform root);
        _proxyPools[new LayerKey(stage, layer)] = new ProxyPool(stage, layer, root, _captureBuilder);
    }

    private void ValidateCaptureFramingOnce()
    {
        if (_captureFramingValidated)
            return;

        RenderTexture sourceRt = blurController.SourceTexture;

        _captureFramingValidated = true;

        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float rtAspect = (float)sourceRt.width / Mathf.Max(1, sourceRt.height);

        if (Mathf.Abs(screenAspect - rtAspect) > 0.01f)
        {
            Debug.LogWarning(
                $"[UIStageDepthLayerBlurRuntime] Capture RT aspect({rtAspect:F3}) != screen aspect({screenAspect:F3}). " +
                "capture camera/RT를 화면 종횡비 1:1이 아닙니다.");
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