using UnityEngine;
using UnityEngine.Serialization;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    [SerializeField] UIManager uiManager;

    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    
    [Header("VN Trace")]
    [SerializeField] private VNTraceStream vnTrace = new VNTraceStream();

    private readonly VnUxState _vnUxState = new();
    private readonly VnPlaybackSettings _vnPlaybackSettings = new();
    private readonly PresentationSessionContext _presentationSessionContext = new();
    private readonly VnScreenBindings _screenBindings = new();

    private EpisodeSelectionStateData _episodeSelectionStateData = new ();
    
    private LinePresentationAdvanceState _linePresentationAdvanceState;
    

    [Header("Sound")] 
    [SerializeField] private AudioSystem audioSystem;
    [SerializeField] private InlineSfxPlaybackHost inlineSfxHost;

    [SerializeField] private DialogueBoxHost dialogueBoxHost;

    [SerializeField] private PresentationResponseRig presentationResponseRig;
    
    [Header("Presentation")] 
    [SerializeField] private CharStageTuningSO globalTuning;
    [SerializeField] private RoleAnchorTuningDBSO roleTuningDb;
    
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;
    [SerializeField] private CharacterFocusTuningDBSO characterFocusTuningDb;

    [SerializeField] private UnitySignalBus unitySignalBus;
    [SerializeField] private CommandExecutor commandExecutor;

    [Header("PresentationEntry")] 
    [SerializeField] private RouteCatalogSO routeCatalogSo;

    [SerializeField] private PresentationSessionEntry presentationSessionEntry;

    [Header("ImmediateCommandRunner")] 
    [SerializeField] private YarnBridgePlaybackDriver yarnBridgePlaybackDriver;
    
    //[SerializeField] private YarnLineSetupPresenter yarnLineRuntimePresenter;

    [Header("Yarn")] 
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueRunner subPresentationRunner;
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
    [SerializeField] private VnRuntimeBridge vnRuntimeBridge;
    [SerializeField] private InlineEventMarkupHandler inlineEventMarkupHandler;
    [SerializeField] private CustomLinePresenter customLinePresenter;
    [SerializeField] private SubPresentationPresenter subPresentationPresenter;


    [Header("VnAdvanceGate")]
    [SerializeField] private EllipsisBreathTypewriter ellipsisBreathTypewriter;

    [SerializeField] private VnAdvanceInputPoller vnAdvanceInputPoller;
    [SerializeField] private DialogueAdvanceDispatcher dialogueAdvanceDispatcher;

    [Header("YarnVnInputFeature")] 
    [SerializeField] private YarnLineLifecycleBridge yarnLineLifecycleBridge;

    [SerializeField] private VnFeatureController vnFeatureController;
    
    [Header("VN Save / Load")]
    [SerializeField] private int saveSlotCount = 10;
    [SerializeField] private VNAlbumDatabaseSO albumDatabase;
    [SerializeField] private VNPlaytimeTracker vnPlaytimeTracker;
    [SerializeField] private VNAlbumUnlockDebugList vnAlbumUnlockDebugList;

    [Header("UI")] 
    [SerializeField] private EpisodePlayer episodePlayer;
    
    [Header("Emoji")] 
    [SerializeField] private CharacterEmojiLibrarySO characterEmojiLibrarySo;
    
    [Header("RigPrefab")] 
    [Tooltip("CharacterRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    [SerializeField] private RectTransform rigPrefab;
    
    
    [Header("ChapterButtonCard")] 
    [SerializeField] private RectTransform chapterCardPrefab;
    [SerializeField] private ChapterCardFactory chapterCardFactory;
    
    
    [SerializeField] private RectTransform nodeRigPrefab;

    private PresentationSessionBridge _presentationSessionBridge;

    private RollbackHistory _rollbackHistory;
    private BacklogRecorder _backlogRecorder;
    private UIPatchService _uiPatchService;
    
    private VNSaveLoadSystem _vnSaveLoadSystem;
    
    [Header("Episode Selection")]
    private EpisodeSelectionSystem _episodeSelectionSystem;
    [SerializeField] private ChapterEpisodeProgressionCatalogSO chapterEpisodeProgressionCatalog;

    [SerializeField] private ChapterEpisodeProgressionSO episodeProgressionSo;
    
    
    [SerializeField] private RollbackHistoryDebugView rollbackHistoryDebugView;
    
    private void Awake()
    {
        InitializeTrace();

        BootstrapAudioSystem();
        ConnectAudioSystemToYarn();

        BootstrapUIManager();

        BootstrapPresentationSession();
        ConnectPresentationSessionToYarn();

        BootstrapYarn();
        SetupYarnLifecycleBridge();

        BootstrapDialogueAdvanceInput();
        BootstrapPlaybackControls();

        BootstrapVnSaveLoadRuntime();
        
        
        
        BootstrapEpisodeSelectionRuntime();
        

        InitializeEpisodePlayer();
        BootstrapScreenBindings();
    }
    
    private void InitializeTrace()
    {
        _linePresentationAdvanceState = new LinePresentationAdvanceState(vnTrace);

        vnTrace.Clear(this);
        vnTrace.Trace(nameof(VnAppBootstrap), "AwakeBegin", note: "VN bootstrap started", context: this);
    }

    private void BootstrapAudioSystem()
    {
        audioSystem.Initialize();
    }

    private void ConnectAudioSystemToYarn()
    {
        ResourcesAudioClipResolver audioClipResolver = new();
        inlineSfxHost.Initialize(audioSystem, audioClipResolver);
    }

    private void BootstrapUIManager()
    {
        SpritePortAssignmentBuilder spritePortAssignmentBuilder = new();
        ResourcesUISpriteLoader resourcesUISpriteLoader = new();
        UISpritePatcher uiSpritePatcher = new(resourcesUISpriteLoader);
        _uiPatchService = new UIPatchService(spritePortAssignmentBuilder, uiSpritePatcher);

        uiManager.Init();
        uiManager.AttachUIPatchService(_uiPatchService);
    }

    private void BootstrapPresentationSession()
    {
        SignalLatch signalLatch = new();
        unitySignalBus.OnSignal += signalLatch.Latch;

        StepGatePlanBuilder gatePlanner = new();
        StepGateAdvancer gateAdvancer = new(
            _unityInputSource,
            _unityTimeSource,
            unitySignalBus,
            signalLatch);

        // Signal / Timing
        SignalCommandFactory signalFactory = new(
            _unityTimeSource,
            unitySignalBus,
            signalLatch);

        // Character Rig
        CharRigSlotResolver charRigSlotResolver = new();
        CharacterRigBuilder characterRigBuilder = new();
        PortraitResolver portraitResolver = new(portraitGeneratedDbSo);
        CharacterEmojiResolver emojiResolver = new(characterEmojiLibrarySo);

        CharRigCommandFactory charRigFactory = new(
            charRigSlotResolver,
            characterRigBuilder,
            portraitResolver,
            emojiResolver,
            globalTuning,
            roleTuningDb);

        // Background Rig
        BackgroundRigSlotResolver backgroundRigSlotResolver = new();
        BackgroundRigBuilder backgroundRigBuilder = new();
        BackgroundSpriteResolver backgroundSpriteResolver = new();

        BackgroundRigCommandFactory backgroundRigFactory = new(
            backgroundRigSlotResolver,
            backgroundRigBuilder,
            backgroundSpriteResolver);

        // Presentation Shot / Response Rig
        PresentationCameraRootApplier cameraRootApplier = new();
        presentationResponseRig.Initialize(cameraRootApplier);

        ShotResponseCommandFactory presentationShotFactory = new(
            presentationResponseRig, characterFocusTuningDb);

        // Presentation Transition
        PresentationTransitionCommandFactory presentationTransitionFactory = new();

        // Presentation Control
        PresentationControlCommandFactory presentationControlFactory = new(
            _uiPatchService,
            dialogueBoxHost);

        // Audio
        ResourcesAudioClipResolver audioClipResolver = new();

        AudioCommandFactory audioFactory = new(
            audioSystem,
            audioClipResolver);

        CompositeCommandFactory factory = new(
            charRigFactory,
            backgroundRigFactory,
            presentationShotFactory,
            presentationTransitionFactory,
            presentationControlFactory,
            audioFactory,
            signalFactory);

        commandExecutor.Initialize(factory);

        PresentationSession presentationSession = new(
            gatePlanner,
            gateAdvancer,
            commandExecutor,
            _presentationSessionContext,
            _linePresentationAdvanceState);

        presentationSessionEntry.Initialize(
            presentationSession,
            routeCatalogSo);
    }

    private void ConnectPresentationSessionToYarn()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;

        _presentationSessionBridge = new(session, unitySignalBus);
    }

    private void BootstrapYarn()
    {
        DialogueBoxLineRoutingPolicy dialogueBoxRoutePolicy = new();

        vnRuntimeBridge.Initialize(
            dialogueRunner,
            presentationSessionEntry,
            _presentationSessionBridge);

        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(
            dialogueRunner,
            vnRuntimeBridge,
            dialogueBoxRoutePolicy);

        yarnCommandRegistry.Initialize();

        yarnBridgePlaybackDriver.Initialize(
            commandExecutor,
            presentationSessionEntry);

        YarnCommandBridge yarnCommandBridge = new(
            dialogueRunner,
            subPresentationRunner,
            dialogueAdvanceDispatcher,
            yarnBridgePlaybackDriver,
            rigPrefab);

        customLinePresenter.Initialize(
            dialogueRunner,
            dialogueBoxRoutePolicy,
            dialogueBoxHost,
            dialogueTextRouter,
            ellipsisBreathTypewriter,
            _presentationSessionContext,
            _linePresentationAdvanceState,
            vnTrace);

        subPresentationPresenter.Initialize(
            _presentationSessionContext,
            _linePresentationAdvanceState,
            yarnBridgePlaybackDriver,
            yarnLineLifecycleBridge,
            vnTrace);

        inlineEventMarkupHandler.Initialize(
            yarnLineLifecycleBridge,
            _presentationSessionBridge,
            inlineSfxHost,
            yarnCommandBridge);
    }

    private void SetupYarnLifecycleBridge()
    {
        yarnLineLifecycleBridge.Initialize(dialogueRunner, customLinePresenter, vnTrace);
    }

    private void BootstrapDialogueAdvanceInput()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;

        AdvanceGate advanceGate = new(
            _vnUxState,
            _vnPlaybackSettings,
            _linePresentationAdvanceState,
            () => session != null && session.IsNodeBusy(),
            vnTrace
        );

        dialogueAdvanceDispatcher.Initialize(advanceGate, dialogueRunner, subPresentationRunner, inlineEventMarkupHandler, _linePresentationAdvanceState);
        vnAdvanceInputPoller.Initialize(dialogueAdvanceDispatcher);
    }


    private void BootstrapPlaybackControls()
    {
        _backlogRecorder = new(
            yarnLineLifecycleBridge,
            _vnPlaybackSettings,
            _linePresentationAdvanceState,
            vnTrace);

        AutoAdvanceScheduler autoAdvanceScheduler = new(
            yarnLineLifecycleBridge,
            _vnPlaybackSettings,
            dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);

        HoldSpeedUpController holdSkipController = new(
            _vnPlaybackSettings,
            ellipsisBreathTypewriter,
            dialogueAdvanceDispatcher,
            _presentationSessionContext,
            () => _linePresentationAdvanceState.IsLineFullyShown);

        _rollbackHistory = new RollbackHistory();
        
        if (rollbackHistoryDebugView != null)
            rollbackHistoryDebugView.Bind(_rollbackHistory);

        RollbackController rollbackController = new(
            _rollbackHistory,
            yarnLineLifecycleBridge,
            dialogueAdvanceDispatcher,
            _linePresentationAdvanceState,
            vnTrace
        );

        vnFeatureController.Initialize(
            _vnUxState,
            _vnPlaybackSettings,
            _presentationSessionContext,
            _linePresentationAdvanceState,
            ellipsisBreathTypewriter,
            inlineEventMarkupHandler,
            _backlogRecorder,
            autoAdvanceScheduler,
            holdSkipController,
            rollbackController);
    }
    
    private void BootstrapVnSaveLoadRuntime()
    {
        VNRuntimeStateProvider vnRuntimeStateProvider = new (yarnLineLifecycleBridge, _rollbackHistory, vnPlaytimeTracker);
        VNLoadSeekDriver vnLoadSeekDriver = new(
            yarnLineLifecycleBridge,
            episodePlayer,
            dialogueAdvanceDispatcher,
            customLinePresenter,
            _linePresentationAdvanceState,
            _rollbackHistory,
            vnPlaytimeTracker);

        // 아직 게임 플래그 저장/복원이 없기에 임시로 Empty 사용.
        // 선택지/분기가 들어가면 실제 구현체로 교체.
        EmptyVNFlagStore vnFlagStore = new ();
        AlwaysAllowVNSaveSafetyPolicy vnSaveSafetyPolicy = new ();

        _vnSaveLoadSystem = new (saveSlotCount);
        _vnSaveLoadSystem.AttachRuntime(
            vnRuntimeStateProvider,
            vnLoadSeekDriver,
            vnFlagStore,
            vnSaveSafetyPolicy,
            albumDatabase);
        
        vnAlbumUnlockDebugList.Initialize(_vnSaveLoadSystem);
        _screenBindings.ConfigureAlbumView(_vnSaveLoadSystem);
    }

    private void BootstrapEpisodeSelectionRuntime()
    {
        EpisodeYarnEntryMapBuilder yarnMapBuilder = new();
        EpisodeProgressionGraphDataBuilder graphDataBuilder = new();
        EpisodeProgressionRuleDataBuilder ruleDataBuilder = new();
        EpisodeGraphLayoutOptions layoutOptions = EpisodeGraphLayoutOptions.Compact();
        EpisodeGraphRenderer episodeGraphRenderer = new(nodeRigPrefab);
        
        EpisodeGraphScrollController scrollController = new();

        _episodeSelectionSystem = new EpisodeSelectionSystem(
            chapterEpisodeProgressionCatalog,
            layoutOptions,
            yarnMapBuilder,
            graphDataBuilder,
            ruleDataBuilder,
            _episodeSelectionStateData,
            episodeGraphRenderer);
    }
    
    private void InitializeEpisodePlayer()
    {
        episodePlayer.Initialize(_screenBindings, _rollbackHistory, customLinePresenter, _backlogRecorder);
    }
    
    private void BootstrapScreenBindings()
    {
        _screenBindings.ConfigurePresentationView(
            vnFeatureController,
            _vnUxState,
            vnRuntimeBridge,
            dialogueAdvanceDispatcher,
            _linePresentationAdvanceState);

        _screenBindings.ConfigureEpisodeSelection(_episodeSelectionSystem);
        _screenBindings.ConfigureChapterSelection(chapterCardFactory, chapterCardPrefab);
        _screenBindings.ConfigureAlbumView(_vnSaveLoadSystem);
        _screenBindings.ConfigureTitleView(episodePlayer);
    }
    
    private void Start()
    {
        OpenInitialScreen();
    }
    
    private void OpenInitialScreen()
    {
        _screenBindings.OpenTitleMenu();
    }
    
    #region Helper
    [ContextMenu("VN Trace/Dump To Console")]
    public void DumpVNTraceToConsole()
    {
        if (vnTrace == null)
            return;

        vnTrace.DumpToConsole("VN TRACE MANUAL DUMP", this);
    }

    [ContextMenu("VN Trace/Dump Preview To Console")]
    public void DumpVNTracePreviewToConsole()
    {
        if (vnTrace == null)
            return;

        vnTrace.DumpPreviewToConsole("VN TRACE PREVIEW MANUAL DUMP", this);
    }

    [ContextMenu("VN Trace/Clear")]
    public void ClearVNTrace()
    {
        if (vnTrace == null)
            return;

        vnTrace.Clear(this);
    }

    [ContextMenu("VN Trace/Dump And Clear")]
    public void DumpAndClearVNTrace()
    {
        if (vnTrace == null)
            return;

        vnTrace.DumpAndClear("VN TRACE MANUAL DUMP AND CLEAR", this);
    }
    #endregion
}