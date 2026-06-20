using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// UIStageDepthLayerBlurRuntime
//
// 역할:
//   PresentationStage 의 (Stage00~02) x (Far/Back/Mid/Front/Close) depth layer 별
//   defocus(블러) 오버레이를 만든다. bg_defocus 와 동일한 철학을 유지한다.
//     - 원본 rig 를 직접 블러하지 않는다.
//     - captureRoot(캡처 캔버스) 아래 proxy Image 를 구성한다.
//     - 공유 UICaptureCamera 로 source RT 에 렌더하고, UIStageBlurController 로 블러한다.
//     - 결과를 각 layer 의 FrostedGlass overlay(RawImage)에 표시한다.
//
// 좌표계 계약(BG 경로와 동일하게 보존):
//   source rig image 의 world corners
//     → WorldToScreenPoint(null)                         [스크린 px]
//     → ScreenPointToLocalPointInRectangle(captureRoot)  [captureRoot local]
//     → proxy 를 captureRoot local 에 배치
//     → captureCamera 가 captureRoot(풀스크린)를 source RT 에 1:1 렌더
//     → 각 depth layer 안쪽의 overlay RawImage 가 현재 화면 rect 기준 uvRect 로 표시
//   GetWorldCorners 는 rig 의 모든 상위 축(slot/depth/track/scale/framing 등) 합성을
//   포함하므로, command 로 무엇을 움직이든 화면 위치와 어긋나지 않는다.
//
// 이 재작성에서 바로잡은 핵심:
//   (1) 런타임 생성 캡처 오브젝트의 layer 를 captureRoot.layer 로 강제한다.
//       Builder 의 new GameObject 는 Default layer 라, 캡처 카메라 culling mask 가
//       UI layer 로 좁혀져 있으면 컬링되어 렌더되지 않는다(= 빈 캡처 = 안 보임).
//   (2) 캐릭터 runtime effect material 을 캡처에 끌고 오지 않는다(plain 스프라이트를 블러).
//   (3) captureRoot 풀스크린 강제 + source RT 종횡비 1:1 검증.
//   (3-1) overlay 는 depth 렌더 순서 안에 두고, screen-space RT 샘플링은 uvRect 로 보정한다.
//   (3-2) defocus 중에는 원본 sharp Image 를 bake 직후 숨겨 edge bleed-through 를 막는다.
//   (3-3) overlay coverage padding 으로 layer 경계에서 blur 가 잘리는 문제를 완화한다.
//   (4) 같은 blurController/RT 를 BG 경로와 공유하므로:
//         - bake 동안 외부 캡처 콘텐츠 격리(오염 방지),
//         - 결과를 layer 전용 BakedTexture 로 스냅샷(라이브 _blurA 에일리어싱 방지).
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

    private readonly Dictionary<LayerKey, LayerState> _states = new();
    private readonly Dictionary<LayerKey, ProxyPool> _proxyPools = new();

    // 레지스트리에서 살아있는 rig 를 받아오는 스크래치 버퍼.
    private readonly List<CharacterRigRefs> _characterRigBuffer = new();
    private readonly List<BackgroundRigRefs> _backgroundRigBuffer = new();

    // 해당 layer content 아래에 속한 "캡처 허용" 이미지 집합 / 계층 순서 수집 버퍼.
    private readonly HashSet<Image> _allowedSourceImages = new();
    private readonly List<SourceImageEntry> _sourceImageBuffer = new();

    // 좌표 매핑용 코너 버퍼.
    private readonly Vector3[] _sourceWorldCorners = new Vector3[4];
    private readonly Vector2[] _captureLocalCorners = new Vector2[4];

    // Overlay RawImage 는 depth layer 안쪽에서 렌더 순서를 지키되,
    // texture 는 screen-space RT 를 샘플링하므로 현재 화면 rect 에 맞춰 uvRect 를 보정한다.
    private readonly Vector3[] _overlayWorldCorners = new Vector3[4];

    // 이번 bake 에서 켠 depth proxy 집합(공유 캡처 격리 시 "유지 대상" 판정).
    private readonly HashSet<Image> _currentBakeProxies = new();

    // 공유 캡처 격리용 스크래치(비할당 GetComponentsInChildren / 복원 목록).
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
        // 추적 중인 layer 만 매 프레임 다시 굽는다(rig 이동/스케일/회전 추종).
        // bake 가 생략된 프레임에도 overlay 는 StagePan/StageZoom/depth root 아래에서
        // 움직일 수 있으므로, screen-space RT 샘플링용 uvRect 는 매 프레임 갱신한다.
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

            state.Tween?.Kill();
            state.Tween = null;

            ReleaseBakedTexture(state);
        }

        _states.Clear();

        foreach (KeyValuePair<LayerKey, ProxyPool> pair in _proxyPools)
            pair.Value?.DisableAll();

        _proxyPools.Clear();

        _characterRigBuffer.Clear();
        _backgroundRigBuffer.Clear();
        _allowedSourceImages.Clear();
        _sourceImageBuffer.Clear();
        _currentBakeProxies.Clear();
        _captureImageScan.Clear();
        _foreignDisabledBuffer.Clear();

        _captureGraphBuilt = false;
        _captureFramingValidated = false;
    }

    // ── public API (IStageDepthLayerBlurRuntime) ───────────────────────────────

    public void ShowDefocus(
        CommandRunScope scope,
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        float alpha,
        float duration,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample,
        float coveragePaddingPixels)
    {
        EnsureOverlayProvider();
        EnsureCaptureGraph();

        if (scope == null || _overlayProvider == null)
            return;

        if (!_overlayProvider.TryGetDepthDefocusTarget(stage, layer, out PresentationDepthDefocusTarget target))
            return;

        if (!target.IsValid)
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

        state.Alpha = Mathf.Clamp01(alpha);
        state.BlurRadius = Mathf.Max(0f, blurRadius);
        state.Iterations = Mathf.Clamp(iterations, 1, 6);
        state.Downsample = downsample;
        state.CoveragePaddingPixels = Mathf.Max(0f, coveragePaddingPixels);
        state.IsTracking = state.Alpha > 0.001f;

        ApplyOverlayCoveragePadding(state);

        bool baked = BakeLayerBlur(state, force: true);

        if (baked)
            ApplyBlurTextureToOverlay(state);

        DisableAllProxyPools();

        SetOverlayVisible(
            state,
            visible: baked && state.IsTracking,
            duration: duration,
            visibleAlpha: state.Alpha);
    }

    public void HideDefocus(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        float duration)
    {
        LayerKey key = new(stage, layer);

        if (!_states.TryGetValue(key, out LayerState state))
            return;

        state.IsTracking = false;

        ResetOverlayCoveragePadding(state);

        SetOverlayVisible(
            state,
            visible: false,
            duration: duration,
            visibleAlpha: 0f);
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

        // proxy 가 들고 다니는 좌표는 "스크린 좌표"다. captureRoot 가 화면 전체와 1:1 로
        // 겹쳐야 source RT 가 화면과 동일 기준이 되고 overlay(default uvRect)가 맞는다.
        ForceCaptureRootFullScreen();

        _captureBuilder.EnsureAndBind(captureRoot, out _captureRefs);
        BuildProxyPools();

        // (핵심) 런타임 생성 캡처 오브젝트가 캡처 카메라 culling mask 밖(Default layer)으로
        // 떨어져 컬링되는 것을 막는다. captureRoot 의 layer 로 서브트리 전체를 통일한다.
        _captureLayer = captureRoot.gameObject.layer;
        SetLayerRecursive(captureRoot, _captureLayer);

        _captureGraphBuilt = true;
    }

    // captureRoot 를 부모 전체를 덮는 stretch + identity 로 강제(BG 와 공유, idempotent).
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

    // 공유 source RT 의 종횡비가 화면과 어긋나면 off-center rig 가 한 축으로 거리 비례로 밀린다.
    // 조용한 드리프트 대신 1회 경고. RT 준비 전이면 래치를 올리지 않고 다음 bake 때 재시도.
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
                "capture camera/RT 를 화면 종횡비 1:1 로 맞춰라. off-center rig 가 거리 비례로 어긋난다.");
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

        CollectSourceImagesForLayer(state);

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

    // ── source 수집(레지스트리 기반) ────────────────────────────────────────────

    private void CollectSourceImagesForLayer(LayerState state)
    {
        _allowedSourceImages.Clear();
        _sourceImageBuffer.Clear();

        RectTransform contentRoot = state.Target.SourceContentRoot;

        if (contentRoot == null)
            return;

        _characterRigBuffer.Clear();
        _backgroundRigBuffer.Clear();

        state.CharacterRigs?.CollectAliveRigs(_characterRigBuffer);
        state.BackgroundRigs?.CollectAliveRigs(_backgroundRigBuffer);

        // 해당 depth content 아래에 mount 된 살아있는 rig 의 "실제 표시 Image" 만 허용집합에 넣는다.
        for (int i = 0; i < _backgroundRigBuffer.Count; i++)
        {
            BackgroundRigRefs refs = _backgroundRigBuffer[i];

            if (refs == null || refs.RigRoot == null || !IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            AddAllowedImage(refs.Background_BackLayer_Image);
            AddAllowedImage(refs.Background_FrontLayer_Image);
        }

        for (int i = 0; i < _characterRigBuffer.Count; i++)
        {
            CharacterRigRefs refs = _characterRigBuffer[i];

            if (refs == null || refs.RigRoot == null || !IsDescendantOf(refs.RigRoot, contentRoot))
                continue;

            AddAllowedImage(refs.CharacterPortraitSprite_Image);
            AddAllowedImage(refs.CharacterPortraitSpriteOverlay_Image);

            AddAllowedImage(refs.EmojiSlot00_Image);
            AddAllowedImage(refs.EmojiSlot01_Image);
            AddAllowedImage(refs.EmojiSlot02_Image);
        }

        // content 하위를 계층 순서로 훑어, 허용집합에 든 Image 만 그리기 순서대로 수집한다.
        CollectAllowedImagesInHierarchyOrder(contentRoot, contentRoot);
    }

    private void AddAllowedImage(Image image)
    {
        if (image != null)
            _allowedSourceImages.Add(image);
    }

    private void CollectAllowedImagesInHierarchyOrder(RectTransform contentRoot, Transform current)
    {
        if (current == null)
            return;

        if (current.TryGetComponent(out Image image) && _allowedSourceImages.Contains(image))
        {
            if (TryBuildSourceEntry(contentRoot, image, out SourceImageEntry entry))
                _sourceImageBuffer.Add(entry);
        }

        for (int i = 0; i < current.childCount; i++)
            CollectAllowedImagesInHierarchyOrder(contentRoot, current.GetChild(i));
    }

    private static bool TryBuildSourceEntry(RectTransform contentRoot, Image image, out SourceImageEntry entry)
    {
        entry = default;

        if (!IsSourceImageAlive(image))
            return false;

        // content 까지 누적된 CanvasGroup alpha 를 반영(스프라이트 Root 는 초기 alpha 0).
        float canvasGroupAlpha = EvaluateCanvasGroupAlpha(image.transform, contentRoot);

        if (canvasGroupAlpha <= 0.001f)
            return false;

        Color effectiveColor = image.color;
        effectiveColor.a *= canvasGroupAlpha;

        if (effectiveColor.a <= 0.001f)
            return false;

        entry = new SourceImageEntry(image, effectiveColor);
        return true;
    }

    private static float EvaluateCanvasGroupAlpha(Transform leaf, Transform stopRoot)
    {
        float alpha = 1f;
        Transform current = leaf;

        while (current != null)
        {
            if (current.TryGetComponent(out CanvasGroup canvasGroup))
                alpha *= canvasGroup.alpha;

            if (current == stopRoot)
                break;

            current = current.parent;
        }

        return alpha;
    }

    private static bool IsSourceImageAlive(Image source)
    {
        return source != null
            && source.enabled
            && source.gameObject.activeInHierarchy
            && source.sprite != null;
    }

    private static bool IsDescendantOf(RectTransform child, RectTransform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform t = child;

        while (t != null)
        {
            if (t == parent)
                return true;

            t = t.parent;
        }

        return false;
    }

    // ── proxy 동기화 ────────────────────────────────────────────────────────────

    // 스프라이트/색/fill 등 표시 속성만 복사한다.
    // material 은 복사하지 않는다: 캐릭터 portrait 에 바인딩된 runtime effect material 을
    // 캡처에 끌고 오면 블러 파이프라인과 충돌하거나 잘못 렌더된다. defocus 는 "plain 스프라이트"를 블러한다.
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

    // source 의 최종 화면 footprint(4 corners)를 captureRoot local 로 옮겨 proxy 에 그대로 적용.
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


    // ── sharp source visibility ───────────────────────────────────────────────
    // ── overlay ────────────────────────────────────────────────────────────────

    private void ApplyBlurTextureToOverlay(LayerState state)
    {
        if (!state.Target.IsValid)
            return;

        RawImage rawImage = state.Target.OverlayRawImage;

        if (rawImage.texture != state.BakedTexture)
            rawImage.texture = state.BakedTexture;

        SyncOverlayUvRectToScreen(rawImage);
    }

    // BakedTexture 는 화면 전체를 기준으로 구운 screen-space RT 다.
    // 하지만 FrostedGlassRawImage 는 각 depth layer 안쪽에 두어 렌더 순서를 지켜야 한다.
    // 따라서 RawImage geometry 가 현재 화면에서 차지하는 영역만 RT 에서 샘플링하도록 uvRect 를 맞춘다.
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
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                null,
                _overlayWorldCorners[i]);

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

    private void SetOverlayVisible(LayerState state, bool visible, float duration, float visibleAlpha)
    {
        if (!state.Target.IsValid)
            return;

        CanvasGroup canvasGroup = state.Target.OverlayCanvasGroup;
        RawImage rawImage = state.Target.OverlayRawImage;

        state.Tween?.Kill();
        state.Tween = null;

        float targetAlpha = visible ? Mathf.Clamp01(visibleAlpha) : 0f;

        rawImage.raycastTarget = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (visible)
            rawImage.enabled = true;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;

            if (targetAlpha <= 0.001f)
                rawImage.enabled = false;

            return;
        }

        state.Tween = canvasGroup
            .DOFade(targetAlpha, duration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (targetAlpha <= 0.001f)
                    rawImage.enabled = false;
            });
    }

    private void StopTrackingAndHideImmediate(LayerState state)
    {
        ResetOverlayCoveragePadding(state);

        state.IsTracking = false;
        SetOverlayVisible(state, visible: false, duration: 0f, visibleAlpha: 0f);
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
    
    private readonly struct SourceImageEntry
    {
        public readonly Image Image;
        public readonly Color EffectiveColor;

        public SourceImageEntry(Image image, Color effectiveColor)
        {
            Image = image;
            EffectiveColor = effectiveColor;
        }
    }

    private sealed class LayerState
    {
        public readonly LayerKey Key;

        public PresentationDepthDefocusTarget Target;

        public CharacterRigRegistry CharacterRigs;
        public BackgroundRigRegistry BackgroundRigs;

        public bool IsTracking;

        public float Alpha;
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
        public Tween Tween;

        public LayerState(LayerKey key) => Key = key;
    }

    // layer root 아래 proxy Image 를 필요한 만큼 늘려 재사용한다(매 프레임 생성/파괴 금지).
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