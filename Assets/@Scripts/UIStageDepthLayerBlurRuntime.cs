using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// UIStageDepthLayerBlurRuntime  (Baker / 메커니즘)
//
// 역할:
//   PresentationStage (Stage00~02) x (Far/Back/Mid/Front/Close) depth layer 별
//   defocus 블러를 매 프레임 굽고 결과를 각 layer의 FrostedGlass overlay(RawImage)에 쓴다.
//     - 원본 rig를 직접 블러하지 않는다. captureRoot 아래 proxy Image를 구성한다.
//     - 공유 UICaptureCamera로 source RT에 렌더하고, UIStageBlurController로 블러한다.
//     - 결과를 layer 전용 BakedTexture로 스냅샷해 overlay RawImage에 표시한다.
//
// 소유권 경계 (StageDepthDefocusCommand와의 계약):
//   Baker   : RawImage(texture/uvRect/enabled), coverage padding 기하, 캡처·블러·스냅샷, 추적 재bake.
//   Command : OverlayCanvasGroup.alpha + 캐릭터 edge hide 전이/최종값.
//   → 이 클래스는 alpha/visibility tween을 더 이상 소유하지 않는다(과거 SetOverlayVisible 제거).
//   "이 layer가 defocus 상태"는 BeginLayer~EndLayer 사이 IsTracking으로 표현되는 지속 상태이며,
//   Command tween이 끝나도 LateUpdate가 계속 추적 재bake한다.
//
// 좌표계 계약(BG 경로와 동일):
//   source rig image world corners → WorldToScreenPoint → ScreenPointToLocalPointInRectangle(captureRoot)
//   → proxy 배치 → captureCamera가 captureRoot(풀스크린)를 source RT에 1:1 렌더
//   → overlay RawImage가 현재 화면 rect 기준 uvRect로 표시.
//
// 유지되는 핵심 수정:
//   (1) 런타임 생성 캡처 오브젝트의 layer를 captureRoot.layer로 강제(컬링 방지).
//   (2) 캐릭터 runtime effect material을 캡처에 끌고 오지 않는다(plain 스프라이트를 블러).
//   (3) captureRoot 풀스크린 강제 + source RT 종횡비 1:1 검증.
//   (3-1) overlay는 depth 렌더 순서 안에 두고 screen-space RT 샘플링은 uvRect로 보정.
//   (3-3) coverage padding으로 layer 경계 잘림 완화.
//   (4) 공유 blurController/RT: bake 동안 외부 콘텐츠 격리 + layer 전용 BakedTexture 스냅샷.
//
// SoC: source image 수집 책임은 UIStageDepthLayerSourceCollector로 분리.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class UIStageDepthLayerBlurRuntime : MonoBehaviour, IStageDepthLayerBlurRuntime
{
    [Header("Capture Canvas")]
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RectTransform captureRoot;

    [Header("Blur (BG 경로와 공유)")]
    [SerializeField] private UIStageBlurController blurController;

    [Header("Debug")]
    [SerializeField] private bool warnMissingProxyRoot = true;

    private readonly UIStageDepthBlurCaptureBuilder _captureBuilder = new();
    private UIStageDepthBlurCaptureRefs _captureRefs;

    private readonly UIStageDepthLayerSourceCollector _sourceCollector = new();

    private readonly Dictionary<LayerKey, LayerState> _states = new();
    private readonly Dictionary<LayerKey, ProxyPool> _proxyPools = new();

    // 수집된 source(그리기 순서) 버퍼.
    private readonly List<SourceImageEntry> _sourceImageBuffer = new();

    // 좌표 매핑용 코너 버퍼.
    private readonly Vector3[] _sourceWorldCorners = new Vector3[4];
    private readonly Vector2[] _captureLocalCorners = new Vector2[4];
    private readonly Vector3[] _overlayWorldCorners = new Vector3[4];

    // 이번 bake에서 켠 depth proxy 집합(공유 캡처 격리 시 "유지 대상" 판정).
    private readonly HashSet<Image> _currentBakeProxies = new();

    // 공유 캡처 격리용 스크래치.
    private readonly List<Image> _captureImageScan = new();
    private readonly List<Image> _foreignDisabledBuffer = new();

    private IPresentationDepthDefocusOverlayProvider _overlayProvider;

    private bool _captureGraphBuilt;
    private bool _captureFramingValidated;

    // 캡처 카메라에 보이도록, 런타임 생성 캡처 오브젝트에 강제할 layer.
    private int _captureLayer;

    // ── lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureCaptureGraph();
        DisableAllProxyPools();
    }

    private void OnEnable()
    {
        EnsureCaptureGraph();
        DisableAllProxyPools();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Capture Proxy Graph")]
    private void ContextRebuildCaptureProxyGraph()
    {
        _captureGraphBuilt = false;
        _captureFramingValidated = false;
        EnsureCaptureGraph();
        DisableAllProxyPools();
    }
#endif

    private void LateUpdate()
    {
        // 추적 중인 layer만 매 프레임 다시 굽는다(rig 이동/스케일/회전 추종).
        // bake가 생략된 프레임에도 overlay는 StagePan/StageZoom/depth root 아래에서 움직일 수 있으므로,
        // screen-space RT 샘플링용 uvRect는 매 프레임 갱신한다.
        foreach (KeyValuePair<LayerKey, LayerState> pair in _states)
        {
            LayerState state = pair.Value;

            if (!state.IsTracking)
                continue;

            ApplyOverlayCoveragePadding(state);

            bool baked = BakeLayerBlur(state, force: false);

            if (baked)
                ApplyBlurTextureToOverlay(state);
            else if (state.Target.IsValid)
                SyncOverlayUvRectToScreen(state.Target.OverlayRawImage);
        }

        DisableAllProxyPools();
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<LayerKey, LayerState> pair in _states)
        {
            LayerState state = pair.Value;

            ResetOverlayCoveragePadding(state);
            ReleaseBakedTexture(state);
        }

        _states.Clear();

        foreach (KeyValuePair<LayerKey, ProxyPool> pair in _proxyPools)
            pair.Value?.DisableAll();

        _proxyPools.Clear();

        _sourceImageBuffer.Clear();
        _currentBakeProxies.Clear();
        _captureImageScan.Clear();
        _foreignDisabledBuffer.Clear();

        _captureGraphBuilt = false;
        _captureFramingValidated = false;
    }

    // ── public API (IStageDepthLayerBlurRuntime) ───────────────────────────────

    public bool TryResolveTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target)
    {
        target = default;

        EnsureOverlayProvider();

        if (_overlayProvider == null)
            return false;

        return _overlayProvider.TryGetDepthDefocusTarget(stage, layer, out target)
               && target.IsValid;
    }

    public void BeginLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        in PresentationDepthDefocusTarget target,
        CommandRunScope scope,
        in StageDepthBlurParams blurParams)
    {
        EnsureCaptureGraph();

        if (scope == null || !target.IsValid)
            return;

        LayerKey key = new(stage, layer);

        if (!_states.TryGetValue(key, out LayerState state))
        {
            state = new LayerState(key);
            _states.Add(key, state);
        }

        state.Target = target;
        state.CharacterRigs = scope.CharacterRigs;
        state.BackgroundRigs = scope.BackgroundRigs;

        state.BlurRadius = blurParams.BlurRadius;
        state.Iterations = blurParams.Iterations;
        state.Downsample = blurParams.Downsample;
        state.CoveragePaddingPixels = blurParams.CoveragePaddingPixels;

        state.IsTracking = true;

        ApplyOverlayCoveragePadding(state);

        // 즉시 force-bake로 텍스처를 선준비(Command가 alpha를 올리기 전에 내용이 있어야 한다).
        bool baked = BakeLayerBlur(state, force: true);

        if (baked)
            ApplyBlurTextureToOverlay(state);

        DisableAllProxyPools();
    }

    public void EndLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer)
    {
        LayerKey key = new(stage, layer);

        if (!_states.TryGetValue(key, out LayerState state))
            return;

        state.IsTracking = false;

        ResetOverlayCoveragePadding(state);

        if (state.Target.IsValid)
            state.Target.OverlayRawImage.enabled = false;
    }

    // ── capture graph 구성 ─────────────────────────────────────────────────────

    private void EnsureOverlayProvider()
    {
        if (_overlayProvider != null)
            return;

        _overlayProvider = UIManager.Instance.GetUI<PresentationUIRoot>();
    }

    private void EnsureCaptureGraph()
    {
        if (_captureGraphBuilt)
            return;

        if (captureRoot == null)
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
        if (captureRoot == null)
            return;

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
            if (warnMissingProxyRoot)
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

    // ── bake ───────────────────────────────────────────────────────────────────

    private bool BakeLayerBlur(LayerState state, bool force)
    {
        if (state == null)
            return false;

        if (blurController == null)
            return false;

        ValidateCaptureFramingOnce();

        if (captureCanvas == null || captureRoot == null)
            return false;

        if (!state.Target.IsValid)
            return false;

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

    // 스프라이트/색/fill 등 표시 속성만 복사. material은 복사하지 않는다
    // (캐릭터 runtime effect material을 캡처에 끌고 오면 블러 파이프라인과 충돌). plain 스프라이트를 블러.
    private static bool SyncGraphicState(SourceImageEntry source, Image proxy)
    {
        Image src = source.Image;
        bool changed = false;

        if (proxy.material != null)
        {
            proxy.material = null;
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

    // ── overlay ────────────────────────────────────────────────────────────────

    // RawImage의 texture/uvRect/enabled는 Baker가 소유한다. alpha는 Command가 소유한다.
    private void ApplyBlurTextureToOverlay(LayerState state)
    {
        if (!state.Target.IsValid)
            return;

        RawImage rawImage = state.Target.OverlayRawImage;

        if (rawImage.texture != state.BakedTexture)
            rawImage.texture = state.BakedTexture;

        // 텍스처가 준비된 시점에만 켠다(빈 RawImage 흰색 번쩍임 방지).
        if (!rawImage.enabled)
            rawImage.enabled = true;

        rawImage.raycastTarget = false;

        SyncOverlayUvRectToScreen(rawImage);
    }

    // BakedTexture는 화면 전체 기준 screen-space RT다. RawImage는 depth layer 안쪽 렌더 순서를 지키되,
    // 현재 화면에서 차지하는 영역만 RT에서 샘플링하도록 uvRect를 맞춘다.
    private void SyncOverlayUvRectToScreen(RawImage rawImage)
    {
        if (rawImage == null)
            return;

        RectTransform rt = rawImage.rectTransform;

        if (rt == null)
            return;

        rt.GetWorldCorners(_overlayWorldCorners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < _overlayWorldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, _overlayWorldCorners[i]);

            minX = Mathf.Min(minX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxX = Mathf.Max(maxX, screen.x);
            maxY = Mathf.Max(maxY, screen.y);
        }

        float invScreenWidth = 1f / Mathf.Max(1, Screen.width);
        float invScreenHeight = 1f / Mathf.Max(1, Screen.height);

        rawImage.uvRect = new Rect(
            minX * invScreenWidth,
            minY * invScreenHeight,
            (maxX - minX) * invScreenWidth,
            (maxY - minY) * invScreenHeight);
    }

    private void ApplyOverlayCoveragePadding(LayerState state)
    {
        if (state == null || !state.Target.IsValid)
            return;

        RectTransform overlayRect = state.Target.OverlayCanvasGroup.transform as RectTransform;
        RectTransform rawImageRect = state.Target.OverlayRawImage.rectTransform;

        if (overlayRect == null || rawImageRect == null)
            return;

        if (!state.OverlayPaddingCaptured)
        {
            state.BaseOverlayOffsetMin = overlayRect.offsetMin;
            state.BaseOverlayOffsetMax = overlayRect.offsetMax;
            state.BaseRawImageOffsetMin = rawImageRect.offsetMin;
            state.BaseRawImageOffsetMax = rawImageRect.offsetMax;
            state.OverlayPaddingCaptured = true;
        }

        float padding = Mathf.Max(0f, state.CoveragePaddingPixels);

        Vector2 overlayPadding = ConvertScreenPixelsToParentLocalPadding(overlayRect, padding);
        Vector2 rawPadding = ConvertScreenPixelsToParentLocalPadding(rawImageRect, padding);

        overlayRect.offsetMin = state.BaseOverlayOffsetMin - overlayPadding;
        overlayRect.offsetMax = state.BaseOverlayOffsetMax + overlayPadding;

        rawImageRect.offsetMin = state.BaseRawImageOffsetMin - rawPadding;
        rawImageRect.offsetMax = state.BaseRawImageOffsetMax + rawPadding;
    }

    private void ResetOverlayCoveragePadding(LayerState state)
    {
        if (state == null || !state.Target.IsValid)
            return;

        if (!state.OverlayPaddingCaptured)
            return;

        RectTransform overlayRect = state.Target.OverlayCanvasGroup.transform as RectTransform;
        RectTransform rawImageRect = state.Target.OverlayRawImage.rectTransform;

        if (overlayRect != null)
        {
            overlayRect.offsetMin = state.BaseOverlayOffsetMin;
            overlayRect.offsetMax = state.BaseOverlayOffsetMax;
        }

        if (rawImageRect != null)
        {
            rawImageRect.offsetMin = state.BaseRawImageOffsetMin;
            rawImageRect.offsetMax = state.BaseRawImageOffsetMax;
        }
    }

    private static Vector2 ConvertScreenPixelsToParentLocalPadding(RectTransform rect, float pixels)
    {
        if (rect == null || pixels <= 0f)
            return Vector2.zero;

        RectTransform parent = rect.parent as RectTransform;

        if (parent == null)
            return new Vector2(pixels, pixels);

        Camera camera = null;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            Vector2.zero,
            camera,
            out Vector2 localA);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            new Vector2(pixels, pixels),
            camera,
            out Vector2 localB);

        Vector2 delta = localB - localA;

        return new Vector2(
            Mathf.Abs(delta.x),
            Mathf.Abs(delta.y));
    }

    // source가 비어 추적할 게 없으면 추적 중단 + overlay 끔.
    // (alpha는 Command 소유이므로 여기서 건드리지 않는다. RawImage만 끈다.)
    private void StopTrackingAndHideImmediate(LayerState state)
    {
        state.IsTracking = false;

        ResetOverlayCoveragePadding(state);

        if (state.Target.IsValid)
            state.Target.OverlayRawImage.enabled = false;
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
        if (state.BakedTexture == null)
            return;

        if (state.BakedTexture.IsCreated())
            state.BakedTexture.Release();

        if (Application.isPlaying)
            Destroy(state.BakedTexture);
        else
            DestroyImmediate(state.BakedTexture);

        state.BakedTexture = null;
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static void SetLayerRecursive(Transform root, int layer)
    {
        if (root == null)
            return;

        root.gameObject.layer = layer;

        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursive(root.GetChild(i), layer);
    }

    private static readonly PresentationStageKey[] StageKeys =
    {
        PresentationStageKey.Stage00,
        PresentationStageKey.Stage01,
        PresentationStageKey.Stage02,
    };

    private static readonly PresentationDepthLayerKey[] LayerKeys =
    {
        PresentationDepthLayerKey.Far,
        PresentationDepthLayerKey.Back,
        PresentationDepthLayerKey.Mid,
        PresentationDepthLayerKey.Front,
        PresentationDepthLayerKey.Close,
    };

    private static int StageToIndex(PresentationStageKey stage)
    {
        return stage switch
        {
            PresentationStageKey.Stage00 => 0,
            PresentationStageKey.Stage01 => 1,
            PresentationStageKey.Stage02 => 2,
            _ => 0
        };
    }

    private static string LayerToKey(PresentationDepthLayerKey layer)
    {
        return layer switch
        {
            PresentationDepthLayerKey.Far => "far",
            PresentationDepthLayerKey.Back => "back",
            PresentationDepthLayerKey.Mid => "mid",
            PresentationDepthLayerKey.Front => "front",
            PresentationDepthLayerKey.Close => "close",
            _ => "mid"
        };
    }

    private static string BuildProxyImagePrefix(PresentationStageKey stage, PresentationDepthLayerKey layer)
    {
        return $"Slot{StageToIndex(stage):00}_{LayerToKey(layer)}_";
    }

    // ── nested types ───────────────────────────────────────────────────────────

    private readonly struct LayerKey : IEquatable<LayerKey>
    {
        public readonly PresentationStageKey Stage;
        public readonly PresentationDepthLayerKey Layer;

        public LayerKey(PresentationStageKey stage, PresentationDepthLayerKey layer)
        {
            Stage = stage;
            Layer = layer;
        }

        public bool Equals(LayerKey other) => Stage == other.Stage && Layer == other.Layer;
        public override bool Equals(object obj) => obj is LayerKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Stage * 397) ^ (int)Layer;
            }
        }
    }

    // 한 layer의 지속 bake 상태. alpha/tween은 Command 소유이므로 여기 없다.
    private sealed class LayerState
    {
        public readonly LayerKey Key;

        public PresentationDepthDefocusTarget Target;

        public CharacterRigRegistry CharacterRigs;
        public BackgroundRigRegistry BackgroundRigs;

        public bool IsTracking;

        public float BlurRadius;
        public int Iterations;
        public UIStageBlurDownsample Downsample;
        public float CoveragePaddingPixels;

        public bool OverlayPaddingCaptured;
        public Vector2 BaseOverlayOffsetMin;
        public Vector2 BaseOverlayOffsetMax;
        public Vector2 BaseRawImageOffsetMin;
        public Vector2 BaseRawImageOffsetMax;

        public RenderTexture BakedTexture;

        public LayerState(LayerKey key) => Key = key;
    }

    // layer root 아래 proxy Image를 필요한 만큼 늘려 재사용(매 프레임 생성/파괴 금지).
    private sealed class ProxyPool
    {
        private readonly PresentationStageKey _stage;
        private readonly PresentationDepthLayerKey _layer;
        private readonly RectTransform _root;
        private readonly UIStageDepthBlurCaptureBuilder _builder;
        private readonly List<Image> _images = new();

        public ProxyPool(
            PresentationStageKey stage,
            PresentationDepthLayerKey layer,
            RectTransform root,
            UIStageDepthBlurCaptureBuilder builder)
        {
            _stage = stage;
            _layer = layer;
            _root = root;
            _builder = builder;

            CollectExistingImages();
        }

        public Image Acquire(int index)
        {
            if (index < 0)
                return null;

            while (_images.Count <= index)
                _images.Add(CreateImage(_images.Count));

            Image image = _images[index];

            if (image == null)
                _images[index] = image = CreateImage(index);

            return image;
        }

        public void DisableAll()
        {
            for (int i = 0; i < _images.Count; i++)
            {
                if (_images[i] != null)
                    _images[i].enabled = false;
            }
        }

        private void CollectExistingImages()
        {
            _images.Clear();

            if (_root == null)
                return;

            Image[] existing = _root.GetComponentsInChildren<Image>(true);

            Array.Sort(existing, (a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < existing.Length; i++)
            {
                Image image = existing[i];

                if (image == null)
                    continue;

                image.raycastTarget = false;
                image.enabled = false;
                _images.Add(image);
            }
        }

        private Image CreateImage(int index)
        {
            string imageName = $"{BuildProxyImagePrefix(_stage, _layer)}{index:00}_Image";

            Image image = _builder.EnsureProxyImage(_root, imageName);

            if (image == null)
                return null;

            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }
    }
}