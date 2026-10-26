using UnityEngine;

/// <summary>
/// 연출 런타임 조립. 씬 참조를 받아 무대·명령·Yarn·대사 표시·재생 제어를 잇는다.
///
/// 진행·저장을 모른다. 결과(PresentationRuntime)에 재생 호스트를 하나 붙이면 돈다.
/// - 연출 단독: NodePlaybackHost
/// - 진행·저장: ProgressionLauncher
///
/// 조립 순서와 내용은 VNAppBootstrap.Awake의 연출 쪽을 그대로 옮긴 것이다.
/// </summary>
public sealed class PresentationAssembler
{
    /// <summary>예제 대본 번들 폴더(Assets/Samples/Dialogue). 커브(curves.json)도 대본 곁에 산다.</summary>
    private static string DialogueFolder =>
        System.IO.Path.Combine(Application.dataPath, "Samples", "Dialogue");

    private readonly PresentationSceneRefs _refs;
    private readonly IPresentationView _presentationView;

    private readonly VNPlaybackRuntimeState _playbackState = new();

    private readonly RollbackHistory _rollbackHistory = new();
    private readonly ChoiceHistory _choiceHistory = new();
    private readonly VNLinePresentationState _linePresentationAdvanceState = new();

    private readonly PresentationStage _presentationStage = new();
    private readonly BacklogRecorder _backlogRecorder = new();

    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher = new();
    private readonly EpisodeSkipController _episodeSkipController = new();

    private IUIThemePatchPort _uiThemePatch;

    private VNRuntimeStateProvider _vnRuntimeStateProvider;

    private PresentationUIRoot _presentationUIRoot;
    private PresentationShotResponseSystem _presentationResponseRig;
    private ScreenEffectRig _screenEffectRig;

    private PresentationScopeSession _presentationScopeSession;
    private VNFeatureController _vnFeatureController;
    private VNLinePresentationFlow _linePresentationFlow;
    private ScenePresentation _scenePresentation;

    private PresentationAssembler(
        PresentationSceneRefs refs,
        IPresentationView presentationView)
    {
        _refs = refs;
        _presentationView = presentationView;
    }

    /// <param name="presentationView">재생을 시작할 때 무대 화면을 앞으로 가져올 자리.</param>
    public static PresentationRuntime Build(
        PresentationSceneRefs refs,
        IPresentationView presentationView)
    {
        return Build(refs, presentationView, out _);
    }

    /// <param name="internals">예제 UI가 읽는 조립 안쪽 상태(재생 모드·지금 장면). 연출 런타임은 내놓지 않는 것이라 여기서 건넨다.</param>
    public static PresentationRuntime Build(
        PresentationSceneRefs refs,
        IPresentationView presentationView,
        out PresentationInternals internals)
    {
        var assembler = new PresentationAssembler(refs, presentationView);
        PresentationRuntime runtime = assembler.Assemble();

        internals = new PresentationInternals(
            assembler._playbackState,
            assembler._vnRuntimeStateProvider);

        return runtime;
    }

    private PresentationRuntime Assemble()
    {
        BootstrapUIManager();

        BootstrapPresentationRoots();

        BootstrapAudioSystem();

        BootstrapPresentationSession();

        BootstrapYarn();

        CreateScenePresentation();

        BootstrapPlaybackControls();

        BootstrapEquivalenceHarness();

        return new PresentationRuntime(
            _scenePresentation,
            _vnFeatureController,
            _dialogueAdvanceDispatcher,
            _linePresentationFlow,
            _rollbackHistory,
            _choiceHistory,
            _backlogRecorder,
            _refs.InputPoller);
    }

    private void BootstrapUIManager()
    {
        SpritePortAssignmentBuilder spritePortAssignmentBuilder = new();
        ResourcesUISpriteLoader resourcesUISpriteLoader = new();
        UISpritePatcher uiSpritePatcher = new(resourcesUISpriteLoader);
        UIPatchService uiPatchService = new(spritePortAssignmentBuilder, uiSpritePatcher);

        _refs.UIManager.Init();
        _refs.UIManager.AttachUIPatchService(uiPatchService);

        _uiThemePatch = new UIThemePatchAdapter(_refs.UIManager, uiPatchService);
    }

    private void BootstrapPresentationRoots()
    {
        _vnRuntimeStateProvider = new VNRuntimeStateProvider(_rollbackHistory, _choiceHistory);

        _presentationUIRoot = _refs.UIManager.GetUI<PresentationUIRoot>();

        IShotResponseStageProvider shotResponseStageProvider = _presentationUIRoot;

        _presentationResponseRig = new PresentationShotResponseSystem(shotResponseStageProvider);
        _screenEffectRig = EnsureScreenEffectRig();

        _refs.CharacterFocusDebugView.Initialize(
            _presentationStage,
            shotResponseStageProvider,
            _refs.CharacterFocusTuningDb);
    }

    private ScreenEffectRig EnsureScreenEffectRig()
    {
        ScreenEffectRig screenEffectRig = _refs.ScreenEffectRigMount.GetComponentInChildren<ScreenEffectRig>(true);

        if (screenEffectRig == null)
        {
            ScreenEffectRigBuilder screenEffectRigBuilder = new();

            RectTransform rigRoot = screenEffectRigBuilder.BuildRigRoot(
                _refs.ScreenEffectRigPrefab);

            rigRoot.SetParent(_refs.ScreenEffectRigMount, false);

            if (!rigRoot.TryGetComponent(out screenEffectRig))
                screenEffectRig = rigRoot.gameObject.AddComponent<ScreenEffectRig>();
        }

        screenEffectRig.Initialize();

        return screenEffectRig;
    }

    private void BootstrapAudioSystem()
    {
        _refs.AudioSystem.Initialize();
    }

    private void BootstrapPresentationSession()
    {
        SignalLatch signalLatch = new();
        UnitySignalBus unitySignalBus = new();
        unitySignalBus.OnSignal += signalLatch.Latch;

        // Character Rig
        CharRigSlotResolver charRigSlotResolver = new(_presentationUIRoot);
        CharacterRigBuilder characterRigBuilder = new();
        PortraitResolver portraitResolver = new(_refs.PortraitGeneratedDb);

        CharacterRigCommandFactory charRigFactory = new(
            charRigSlotResolver,
            characterRigBuilder,
            portraitResolver,
            _refs.RoleTuningDb,
            _refs.CharacterFocusTuningDb,
            _refs.CharacterVisualFocusPresetDb,
            _refs.CharacterDepthTuning,
            _presentationUIRoot);

        // Background Rig
        BackgroundRigBuilder backgroundRigBuilder = new();
        BackgroundRigSlotResolver backgroundRigSlotResolver = new(_presentationUIRoot);

        BackgroundRigCommandFactory backgroundRigFactory = new(
            backgroundRigBuilder,
            backgroundRigSlotResolver);

        ShotResponseCommandFactory presentationShotFactory = new(
            _presentationResponseRig, _refs.CharacterFocusTuningDb, _presentationUIRoot);

        // Presentation Control
        UnityTimeSource unityTimeSource = new();

        PresentationControlCommandFactory presentationControlFactory = new(
            _uiThemePatch,
            unityTimeSource,
            unitySignalBus,
            signalLatch);

        // Audio
        AudioCommandFactory audioFactory = new(_refs.AudioSystem);

        ScreenEffectCommandFactory screenEffectFactory = new(
            _screenEffectRig,
            _refs.ScreenFlashPresetDb,
            _refs.ScreenNoisePresetDb,
            _refs.ScreenVignettePresetDb,
            _refs.StageMaskMotionPresetDb,
            _presentationUIRoot,
            _presentationUIRoot);

        CompositeCommandFactory factory = new(
            charRigFactory,
            backgroundRigFactory,
            presentationShotFactory,
            presentationControlFactory,
            audioFactory,
            screenEffectFactory);

        _refs.CommandExecutor.Initialize(factory);

        PresentationSessionContext presentationSessionContext = new(_playbackState);

        _presentationScopeSession = new(
            _refs.CommandExecutor,
            presentationSessionContext,
            _linePresentationAdvanceState,
            _presentationStage);
    }

    private void BootstrapYarn()
    {
        YarnPlaybackDriver yarnPlaybackDriver = new(_refs.CommandExecutor, _presentationScopeSession);

        // 대사창: 새 예제 UI가 넘긴 것이 있으면 그것, 없으면 옛 UI의 박스.
        IPresentationDialogueBoxView dialogueSurfaceBox =
            _refs.DialogueBoxView ?? _refs.UIManager.GetUI<DialogueSurfaceBox>();

        DialogueBoxCurrentState dialogueBoxState = new();
        DialogueSurfaceState dialogueSurfaceState = new();

        DialogueBoxPresentationController dialogueBoxPresentationController = new(
            dialogueBoxState,
            dialogueSurfaceBox,
            dialogueSurfaceState,
            _refs.SurfaceLayoutPresetDb,
            _refs.SpeakerPolicyDb);

        EaseCurveLibrary easeCurves = EaseCurveLibrary.LoadFrom(
            System.IO.Path.Combine(DialogueFolder, EaseCurveLibrary.BundleFileName));

        _ = new YarnCommandBridge(
            _refs.DialogueRunner,
            yarnPlaybackDriver,
            _refs.CharacterRigPrefab,
            _refs.BackgroundRigPrefab,
            dialogueBoxPresentationController,
            easeCurves);

        VNYarnLineBoundary vnYarnLineBoundary = new(
            _backlogRecorder,
            _rollbackHistory,
            _vnRuntimeStateProvider,
            _linePresentationAdvanceState);

        LineHurrySpeedController lineHurrySpeed = new(_refs.EllipsisBreathTypewriter);

        _linePresentationFlow = new VNLinePresentationFlow(
            vnYarnLineBoundary,
            _linePresentationAdvanceState,
            dialogueBoxPresentationController,
            _refs.EllipsisBreathTypewriter,
            yarnPlaybackDriver,
            lineHurrySpeed);

        _refs.CustomLinePresenter.Initialize(
            _refs.DialogueRunner,
            _linePresentationFlow,
            _refs.EllipsisBreathTypewriter,
            _playbackState);

        VNChoiceBoundary vnChoiceBoundary = new(
            _choiceHistory,
            _rollbackHistory);

        IPresentationOptionsBoxView optionsBox =
            _refs.OptionsBoxView ?? _refs.UIManager.GetUI<VNDefaultOptionsPanel>();

        OptionsBoxPresentationController optionsBoxPresentation = new(optionsBox);

        VNOptionsPresentationFlow optionsPresentationFlow = new(
            optionsBoxPresentation,
            vnChoiceBoundary,
            _linePresentationAdvanceState);

        _refs.OptionsPresenter.Initialize(
            _refs.DialogueRunner,
            optionsPresentationFlow,
            _refs.OptionItem);
    }

    private void CreateScenePresentation()
    {
        IEpisodeNodeRunner nodeRunner =
            new YarnEpisodeNodeRunner(_refs.DialogueRunner);

        _scenePresentation = new ScenePresentation(
            nodeRunner,
            _presentationView,
            _rollbackHistory,
            _refs.CustomLinePresenter,
            _presentationResponseRig,
            _presentationStage,
            _presentationScopeSession,
            _choiceHistory,
            _episodeSkipController,
            _linePresentationAdvanceState);
    }

    // 입력 폴러의 연결은 여기서 하지 않는다 — 재생 호스트가 정해진 뒤 PresentationRuntime.AttachHost가 한다.
    private void BootstrapPlaybackControls()
    {
        _refs.AutoAdvanceScheduler.Initialize(
            _playbackState,
            _dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);

        RapidSkipController rapidSkipController = new(
            _dialogueAdvanceDispatcher);

        _episodeSkipController.Initialize(
            _dialogueAdvanceDispatcher);

        _vnFeatureController = new VNFeatureController(
            _playbackState,
            _linePresentationAdvanceState,
            _refs.EllipsisBreathTypewriter,
            _backlogRecorder,
            _refs.AutoAdvanceScheduler,
            rapidSkipController,
            _episodeSkipController,
            _rollbackHistory,
            _choiceHistory);

        AdvanceGate advanceGate = new(
            _playbackState,
            _linePresentationAdvanceState,
            _presentationScopeSession);

        _dialogueAdvanceDispatcher.Initialize(
            advanceGate,
            _refs.DialogueRunner,
            _linePresentationAdvanceState);
    }

    private void BootstrapEquivalenceHarness()
    {
        if (!_refs.EnableEquivalenceHarness)
            return;

        GameObject harnessGo = new("StageEquivalenceHarness");
        harnessGo.transform.SetParent(_refs.HarnessParent, false);

        StageEquivalenceHarness harness = harnessGo.AddComponent<StageEquivalenceHarness>();

        harness.Initialize(
            _vnRuntimeStateProvider,
            _presentationScopeSession,
            _presentationResponseRig,
            _dialogueAdvanceDispatcher,
            DialogueFolder);
    }
}

/// <summary>
/// 조립 안쪽에서 예제 UI(KedUI)가 읽는 것 둘.
/// - PlaybackState: 자동·빨리 넘기기의 켜짐(퀵메뉴 표시)과 재생 설정(자동 대기·빠르게 재생 배속).
/// - RuntimeState: 지금 줄의 장면(노드) 이름(대사창 위 꼬리표).
/// 연출 런타임(PresentationRuntime)의 공개 면을 넓히지 않으려고 예제 조립이 예제 층 안에서만 건넨다.
/// </summary>
public sealed class PresentationInternals
{
    public VNPlaybackRuntimeState PlaybackState { get; }
    public VNRuntimeStateProvider RuntimeState { get; }

    public PresentationInternals(
        VNPlaybackRuntimeState playbackState,
        VNRuntimeStateProvider runtimeState)
    {
        PlaybackState = playbackState;
        RuntimeState = runtimeState;
    }
}
