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
public sealed partial class UIStageDepthLayerBlurRuntime : MonoBehaviour, IStageDepthLayerBlurRuntime
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
}