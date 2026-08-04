using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// - (원본 rig를 직접 블러하지 않는다)
// - captureRoot 아래 proxy Image를 구성.
// - 공유 UICaptureCamera로 source RT에 렌더하고, UIStageBlurController로 블러.
// - 결과를 layer 전용 BakedTexture로 스냅샷해 overlay RawImage에 표시.
//
// 좌표계 계약:
// -> source rig image world corners -> WorldToScreenPoint
// -> ScreenPointToLocalPointInRectangle(captureRoot)
// -> proxy 배치                      -> captureCamera가 captureRoot(풀스크린)를 source RT에 1:1 렌더
// -> overlay RawImage가 현재 화면 rect 기준 uvRect로 표시.
public sealed partial class UIStageDepthLayerBlurRuntime : MonoBehaviour, IStageDepthLayerBlurRuntime
{
    [Header("Capture Canvas")]
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RectTransform captureRoot;

    [Header("Blur (BG 경로와 공유)")]
    [SerializeField] private UIStageBlurController blurController;

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

    public void Initialize(IPresentationDepthDefocusOverlayProvider overlayProvider)
    {
        _overlayProvider = overlayProvider;
    }

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
            else
                SyncOverlayUvRectToScreen(state.Target.OverlayRawImage);
        }

        DisableAllProxyPools();
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<LayerKey, LayerState> pair in _states)
            ReleaseBakedTexture(pair.Value);
        
        _states.Clear();
    }

    // ── public API (IStageDepthLayerBlurRuntime) ───────────────────────────────

    public void ResolveTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target)
    {
        _overlayProvider.GetDepthDefocusTarget(stage, layer, out target);
    }

    public void BeginLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        in PresentationDepthDefocusTarget target,
        CommandRunScope scope,
        in StageDepthBlurParams blurParams)
    {
        EnsureCaptureGraph();
        
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

        state.Target.OverlayRawImage.enabled = false;
    }
    
    
}