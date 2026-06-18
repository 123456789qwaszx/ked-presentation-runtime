using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    
    private readonly VnPlaybackSettings _vnPlaybackSettings = new();
    
    private readonly VnUxState _vnUxState = new();
    private readonly EpisodeSelectionStateData _episodeSelectionStateData = new();
    private readonly RollbackHistory _rollbackHistory = new();
    private readonly ChoiceHistory _choiceHistory = new();
    private readonly VNLinePresentationState _linePresentationAdvanceState = new();
    private readonly PresentationSessionContext _presentationSessionContext = new();
    
    private readonly PresentationStage _presentationStage = new();
    
    private readonly VNSideRunnerSyncHub _vnSideRunnerSyncHub = new();
    private readonly BacklogRecorder _backlogRecorder = new ();
    
    private readonly VnScreenBindings _screenBindings = new();
    
    [Header("VN Trace")]
    [SerializeField] private VNTraceStream vnTrace = new ();

    [Header("UIManager")]
    [SerializeField] private UIManager uiManager;
    
    [Header("Sound")] 
    [SerializeField] private AudioSystem audioSystem;
    [SerializeField] private InlineSfxPlaybackHost inlineSfxHost;

    [SerializeField] private DialogueBoxHost dialogueBoxHost;

    
    [Header("Presentation")] 
    [SerializeField] private CharStageTuningSO globalTuning;
    [SerializeField] private RoleAnchorTuningDBSO roleTuningDb;
    
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;
    [SerializeField] private CharacterFocusTuningDBSO characterFocusTuningDb;
    [SerializeField] private CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb;
    
    [SerializeField] private CharacterDepthTuningSO characterDepthTuning;
    [SerializeField] private RoleDepthTuningDBSO roleDepthTuningDb;

    [SerializeField] private UnitySignalBus unitySignalBus;
    
    [Header("MainExecutor")]
    [SerializeField] private CommandExecutor commandExecutor;
    [SerializeField] private YarnBridgePlaybackDriver mainYarnBridgePlaybackDriver;
    
    [Header("SubExecutor")]
    [SerializeField] private CommandExecutor subCommandExecutor;
    [SerializeField] private YarnBridgePlaybackDriver subYarnBridgePlaybackDriver;
    
    [Header("OneShotExecutor")]
    [SerializeField] private CommandExecutor oneShotCommandExecutor;
    [SerializeField] private YarnBridgePlaybackDriver oneShotYarnBridgePlaybackDriver;
    

    [Header("PresentationEntry")] 
    [SerializeField] private RouteCatalogSO routeCatalogSo;
    [SerializeField] private PresentationSessionEntry presentationSessionEntry;


    [Header("Yarn")] 
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueRunner subPresentationRunner;
    [SerializeField] private DialogueRunner subOneShotRunner;
    
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private VnRuntimeBridge vnRuntimeBridge;
    [SerializeField] private InlineEventMarkupHandler inlineEventMarkupHandler;
    [SerializeField] private CustomLinePresenter customLinePresenter;
    [SerializeField] private SubPresentationPresenter subPresentationPresenter;
    [SerializeField] private OneShotPresentationPresenter oneShotPresentationPresenter;
    [SerializeField] private AutoAdvanceScheduler autoAdvanceScheduler;
    [SerializeField] private VNOptionsPresenter vnOptionsPresenter;
    [SerializeField] private VNOptionsBoxPresentationController vnOptionsBoxPresentationController;
    [SerializeField] private EllipsisBreathTypewriter ellipsisBreathTypewriter;
    
    [Header("VnAdvanceGate")]
    [SerializeField] private VnAdvanceInputPoller vnAdvanceInputPoller;
    [SerializeField] private DialogueAdvanceDispatcher dialogueAdvanceDispatcher;
    
    [Header("FeatureController")]
    [SerializeField] private VnFeatureController vnFeatureController;
    
    
    [Header("RigPrefab")] [Tooltip("CharacterRig prefab used for command presentation. " +
                                   "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
                                   "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")] 
    [SerializeField] private RectTransform rigPrefab;
    [SerializeField] private RectTransform backgroundRigPrefab;
    
    [Header("ChapterButtonCard")] 
    [SerializeField] private RectTransform chapterCardPrefab;
    [SerializeField] private RectTransform nodeRigPrefab;
    [SerializeField] private ChapterCardFactory chapterCardFactory;
    
    [Header("VN Save / Load")]
    [SerializeField] private VNAlbumDatabaseSO albumDatabase;
    [SerializeField] private VNPlaytimeTracker vnPlaytimeTracker;
    [SerializeField] private VNAlbumUnlockDebugList vnAlbumUnlockDebugList;
    
    [Header("Episode Selection")]
    [SerializeField] private ChapterEpisodeProgressionCatalogSO chapterEpisodeProgressionCatalog;
    
    [SerializeField] private ChapterEpisodeProgressionSO episodeProgressionSo;
    [SerializeField] private RollbackHistoryDebugView rollbackHistoryDebugView;
    
    [Header("Emoji")] 
    [SerializeField] private CharacterEmojiLibrarySO characterEmojiLibrarySo;
    [SerializeField] private CharacterEmojiVisualPresetSO characterEmojiVisualPresetSo;
    
    [Header("UI")] 
    [SerializeField] private EpisodePlayer episodePlayer;
    
    [SerializeField] private UIBackgroundRigBlurRuntime uiBackgroundRigBlurRuntime;
    [SerializeField] private ScreenNoisePresetDBSO screenNoisePresetDbso;
    [SerializeField] private ScreenVignettePresetDBSO screenVignettePresetDbso;
    [SerializeField] private ScreenFlashPresetDBSO screenFlashPresetDbso;
    
    [Header("NodeDebug")] 
    [SerializeField] private YarnLaneDebugView yarnLaneDebugView;
    [SerializeField] private CharacterFocusDebugView characterFocusDebugView;
    
    private PresentationSessionBridge _presentationSessionBridge;
    private UIPatchService _uiPatchService;
    private VNLoadSeekDriver _vnLoadSeekDriver;
    private VNSaveLoadSystem _vnSaveLoadSystem;
    private EpisodeSelectionSystem _episodeSelectionSystem;
    private DialogueBoxPresentationController  _dialogueBoxPresentationController;
    
    private VNRuntimeStateProvider _vnRuntimeStateProvider;
    private PresentationShotResponseSystem _presentationResponseRig;
    
    
    private void Awake()
    {
        BootstrapUIManager();
        
        _vnRuntimeStateProvider = new VNRuntimeStateProvider(_rollbackHistory, _choiceHistory, vnPlaytimeTracker);
        rollbackHistoryDebugView.Bind(_rollbackHistory);
        _presentationResponseRig = new PresentationShotResponseSystem();
        
        IShotResponseStageProvider shotResponseStageProvider =
            UIManager.Instance.GetUI<PresentationUIRoot>();
        
        characterFocusDebugView.Initialize(
            _presentationStage,
            shotResponseStageProvider,
            characterFocusTuningDb);

        BootstrapAudioSystem();
        ConnectAudioSystemToYarn();


        BootstrapPresentationSession();
        ConnectPresentationSessionToYarn();
        
        BootstrapYarn();
        BootstrapDialogueAdvanceInput();
        
        BootstrapVnSaveLoadRuntime();
        BootstrapLinePresentationRuntime();
        
        BootstrapPlaybackControls();
        BootstrapEpisodeSelectionRuntime();
        InitializeEpisodePlayer();
        BootstrapScreenBindings();
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

        CharacterRigCommandFactory charRigFactory = new(
            charRigSlotResolver,
            characterRigBuilder,
            portraitResolver,
            emojiResolver,
            globalTuning,
            roleTuningDb,
            characterFocusTuningDb,
            characterVisualFocusPresetDb,
            characterDepthTuning,
            roleDepthTuningDb,
            characterEmojiVisualPresetSo);

        // Background Rig
        BackgroundRigSlotResolver backgroundRigSlotResolver = new();
        BackgroundRigBuilder backgroundRigBuilder = new();
        BackgroundSpriteResolver backgroundSpriteResolver = new();
        
        BackgroundRigCommandFactory backgroundRigFactory = new(
            backgroundRigSlotResolver,
            backgroundRigBuilder,
            backgroundSpriteResolver,
            uiBackgroundRigBlurRuntime);

        ShotResponseCommandFactory presentationShotFactory = new(
            _presentationResponseRig, characterFocusTuningDb);

        // Presentation Transition
        PresentationTransitionCommandFactory presentationTransitionFactory = new();

        // Presentation Control
        PresentationControlCommandFactory presentationControlFactory = new(
            _uiPatchService,
            _vnSideRunnerSyncHub);

        // Audio
        ResourcesAudioClipResolver audioClipResolver = new();

        AudioCommandFactory audioFactory = new(
            audioSystem,
            audioClipResolver);

        ScreenEffectCommandFactory screenEffectFactory = new(
            screenFlashPresetDbso,
            screenNoisePresetDbso, 
            screenVignettePresetDbso);

        CompositeCommandFactory factory = new(
            charRigFactory,
            backgroundRigFactory,
            presentationShotFactory,
            presentationTransitionFactory,
            presentationControlFactory,
            audioFactory,
            signalFactory,
            screenEffectFactory);

        commandExecutor.Initialize(factory);
        subCommandExecutor.Initialize(factory);
        oneShotCommandExecutor.Initialize(factory);
        
        PresentationSession presentationSession = new(
            gatePlanner,
            gateAdvancer,
            commandExecutor,
            subCommandExecutor,
            oneShotCommandExecutor,
            _presentationSessionContext,
            _linePresentationAdvanceState,
            _presentationStage);

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
        vnRuntimeBridge.Initialize(dialogueRunner, presentationSessionEntry, _presentationSessionBridge);

        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(dialogueRunner, vnRuntimeBridge);

        yarnCommandRegistry.Initialize();
        mainYarnBridgePlaybackDriver.Initialize(commandExecutor, presentationSessionEntry);
        subYarnBridgePlaybackDriver.Initialize(subCommandExecutor, new SubPresentationScopeProvider(presentationSessionEntry));
        oneShotYarnBridgePlaybackDriver.Initialize(oneShotCommandExecutor, presentationSessionEntry);

        _vnSideRunnerSyncHub.RegisterPresentationLane(subPresentationRunner);

        OneShotPresentationLane oneShotPresentationLane = new(subOneShotRunner, oneShotYarnBridgePlaybackDriver);
        
        DialogueBoxMetadataResolver metadataResolver = new();
        _dialogueBoxPresentationController = new(dialogueBoxHost, metadataResolver);
        
        YarnCommandBridge yarnCommandBridge = new(
            dialogueRunner,
            mainYarnBridgePlaybackDriver,
            _vnRuntimeStateProvider,
            _vnSideRunnerSyncHub,
            rigPrefab,
            backgroundRigPrefab,
            oneShotPresentationLane,
            _dialogueBoxPresentationController,
            bindMainLaneCommands: true);
        
        YarnCommandBridge subYarnCommandBridge = new YarnCommandBridge(
            subPresentationRunner, 
            subYarnBridgePlaybackDriver,
            _vnRuntimeStateProvider,
            _vnSideRunnerSyncHub, 
            rigPrefab, 
            backgroundRigPrefab,
            oneShotPresentationLane,
            _dialogueBoxPresentationController,
            bindMainLaneCommands: false);
        
        YarnCommandBridge subOneShotYarnCommandBridge = new YarnCommandBridge(
            subOneShotRunner, 
            oneShotYarnBridgePlaybackDriver,
            _vnRuntimeStateProvider,
            _vnSideRunnerSyncHub, 
            rigPrefab, 
            backgroundRigPrefab,
            oneShotPresentationLane,
            _dialogueBoxPresentationController,
            bindMainLaneCommands: false);
        
        subPresentationPresenter.Initialize(subPresentationRunner, subYarnBridgePlaybackDriver, _vnSideRunnerSyncHub, yarnLaneDebugView);
        
        oneShotPresentationPresenter.Initialize(subOneShotRunner, yarnLaneDebugView);
        
        inlineEventMarkupHandler.Initialize(_presentationSessionBridge, inlineSfxHost, yarnCommandBridge);
    }
    
    private void BootstrapDialogueAdvanceInput()
    {
        AdvanceGate advanceGate = new(
            _vnUxState,
            _vnPlaybackSettings,
            _linePresentationAdvanceState,
            presentationSessionEntry,
            vnTrace
        );

        dialogueAdvanceDispatcher.Initialize(advanceGate, dialogueRunner, inlineEventMarkupHandler, _linePresentationAdvanceState);
        vnAdvanceInputPoller.Initialize(dialogueAdvanceDispatcher);
    }
    
    private void BootstrapLinePresentationRuntime()
    {
        VNYarnLineBoundary vnYarnLineBoundary = new (
            _backlogRecorder,
            _rollbackHistory,
            _vnRuntimeStateProvider);

        
        VNLinePresentationFlow vnLinePresentationFlow = new(
            vnYarnLineBoundary,
            _linePresentationAdvanceState,
            _dialogueBoxPresentationController,
            ellipsisBreathTypewriter,
            _vnLoadSeekDriver,
            _vnSideRunnerSyncHub,
            mainYarnBridgePlaybackDriver);

        customLinePresenter.Initialize(
            dialogueRunner,
            vnLinePresentationFlow,
            ellipsisBreathTypewriter,
            _linePresentationAdvanceState,
            _presentationSessionContext,
            trace: null,
            yarnLaneDebugView);
        
        VNChoiceBoundary vnChoiceBoundary = new(_choiceHistory, _rollbackHistory);

        VNOptionsPresentationFlow flow = new VNOptionsPresentationFlow(
            vnOptionsBoxPresentationController,
            _linePresentationAdvanceState,
            vnChoiceBoundary,
            _vnUxState);

        vnOptionsPresenter.Initialize(dialogueRunner, flow);
        
        vnOptionsPresenter.AttachDialogueRunner(dialogueRunner);
    }
    
    private void BootstrapPlaybackControls()
    {
        autoAdvanceScheduler.Initialize(
            _vnPlaybackSettings,
            dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);

        FastForwardController holdSkipController = new(
            _vnPlaybackSettings,
            ellipsisBreathTypewriter,
            dialogueAdvanceDispatcher,
            _presentationSessionContext,
            () => _linePresentationAdvanceState.IsLineFullyShown);

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
            _rollbackHistory,
            _linePresentationAdvanceState,
            _choiceHistory);
    }
    
    private void BootstrapVnSaveLoadRuntime()
    {
        _vnLoadSeekDriver = new VNLoadSeekDriver(
            episodePlayer,
            _linePresentationAdvanceState,
            vnPlaytimeTracker,
            _rollbackHistory,
            _choiceHistory,
            _vnSideRunnerSyncHub,
            vnTrace);

        // 아직 게임 플래그 저장/복원이 없기에 임시로 Empty 사용.
        // 선택지/분기가 들어가면 실제 구현체로 교체.
        EmptyVNFlagStore vnFlagStore = new ();

        _vnSaveLoadSystem = new ();
        _vnSaveLoadSystem.AttachRuntime(
            _vnRuntimeStateProvider,
            _vnLoadSeekDriver,
            vnFlagStore,
            albumDatabase,
            vnTrace);
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
        episodePlayer.Initialize(
            _screenBindings, 
            _rollbackHistory, 
            customLinePresenter,
            _backlogRecorder, 
            _choiceHistory,
            _vnSideRunnerSyncHub,
            _presentationResponseRig
            );
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
        vnAlbumUnlockDebugList.Initialize(_vnSaveLoadSystem);
        _screenBindings.ConfigureAlbumView(_vnSaveLoadSystem);
        
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