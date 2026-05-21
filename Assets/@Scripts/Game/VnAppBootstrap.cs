using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] UIManager uiManager;

    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    private readonly VnUxState _vnUxState = new();
    private readonly VnPlaybackSettings _vnPlaybackSettings = new();
    private readonly EpisodePlayState _episodePlayState = new();
    private readonly PresentationSessionContext _presentationSessionContext = new();
    private readonly LinePresentationAdvanceState _linePresentationAdvanceState = new();

    [Header("Sound")] 
    [SerializeField] private AudioSystem audioSystem;
    [SerializeField] private InlineSfxPlaybackHost inlineSfxHost;

    [Header("PresentationView")]
    [SerializeField] private BGHost bgHost;

    [SerializeField] private DialogueBoxHost dialogueBoxHost;

    [SerializeField] private PresentationResponseRig presentationResponseRig;

    [Tooltip("CharacterRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    [SerializeField] private RectTransform rigPrefab;
    
    [Header("Presentation")] 
    [SerializeField] private CharStageTuningSO globalTuning;
    [SerializeField] private RoleAnchorTuningDBSO roleTuningDb;
    
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;

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
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
    [SerializeField] private VnRuntimeBridge vnRuntimeBridge;
    [SerializeField] private InlineEmojiHost inlineEmojiHost;
    [SerializeField] private InlineEventMarkupHandler inlineEventMarkupHandler;
    [SerializeField] private CustomLinePresenter customLinePresenter;
    [SerializeField] private YarnLineSideEffectPresenter yarnLineSideEffectPresenter;


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
    [SerializeField] private CharacterEmojiLibrarySO characterEmojiLibrarySO;

    private PresentationSessionBridge _presentationSessionBridge;

    private PresentationViewUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    private RollbackHistory _rollbackHistory;
    private UIPatchService _uiPatchService;
    
    private VNSaveLoadSystem _vnSaveLoadSystem;
    
    

    private void Awake()
    {
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
        
        BootstrapUIBindings();
        InitializeEpisodePlayer();
        
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
        StepGateAdvancer gateAdvancer = new(_unityInputSource, _unityTimeSource, unitySignalBus, signalLatch);

        // SignalFactory
        SignalCommandFactory signalFactory = new(_unityTimeSource, unitySignalBus, signalLatch);
        
        // CharRigFactory
        CharRigSlotResolver charRigSlotResolver = new ();
        CharacterRigBuilder charRigAccess = new();
        PortraitResolver portraitResolver = new(portraitGeneratedDbSo);
        CharacterEmojiResolver emojiResolver = new(characterEmojiLibrarySO);
        CharRigCommandFactory charRigFactory = new(charRigSlotResolver, charRigAccess, portraitResolver, emojiResolver, globalTuning, roleTuningDb);

        // TransitionFactory
        TransitionCommandFactory transitionCommandFactory = new(_uiPatchService);

        // SoundFactory
        ResourcesAudioClipResolver audioClipResolver = new();
        SoundCommandFactory soundCommandFactory = new SoundCommandFactory(audioSystem, audioClipResolver);

        //PresentationViewCommandFactory
        PresentationViewCommandFactory presentationViewCommandFactory = new(presentationResponseRig, bgHost, bgHost, dialogueBoxHost);
        
        BackgroundRigSlotResolver backgroundRigSlotResolver = new();
        BackgroundRigBuilder backgroundRigBuilder = new();
        BackgroundSpriteResolver backgroundSpriteResolver = new();
        BackgroundRigCommandFactory backgroundRigCommandFactory = new(backgroundRigSlotResolver, backgroundRigBuilder, backgroundSpriteResolver);

        CompositeCommandFactory factory = new(
            signalFactory, 
            charRigFactory,
            transitionCommandFactory,
            soundCommandFactory,
            presentationViewCommandFactory,
            backgroundRigCommandFactory);
        
        commandExecutor.Initialize(factory);
        

        PresentationSession presentationSession =
            new PresentationSession(gatePlanner, gateAdvancer, commandExecutor, _presentationSessionContext, _linePresentationAdvanceState);

        presentationSessionEntry.Initialize(presentationSession, routeCatalogSo);
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
            yarnBridgePlaybackDriver,
            rigPrefab);

        customLinePresenter.Initialize(
            dialogueRunner,
            dialogueBoxRoutePolicy,
            dialogueBoxHost,
            dialogueTextRouter,
            ellipsisBreathTypewriter,
            _presentationSessionContext,
            _linePresentationAdvanceState);

        yarnLineSideEffectPresenter.Initialize(
            dialogueRunner,
            _presentationSessionContext,
            _linePresentationAdvanceState,
            yarnBridgePlaybackDriver,
            audioSystem);

        inlineEmojiHost.Initialize(yarnCommandBridge);

        inlineEventMarkupHandler.Initialize(
            yarnLineLifecycleBridge,
            _presentationSessionBridge,
            inlineSfxHost,
            inlineEmojiHost);
    }

    private void SetupYarnLifecycleBridge()
    {
        yarnLineLifecycleBridge.Initialize(dialogueRunner, customLinePresenter);
    }

    private void BootstrapDialogueAdvanceInput()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;

        AdvanceGate advanceGategate = new(
            _vnUxState,
            _vnPlaybackSettings,
            _linePresentationAdvanceState,
            () => session != null && session.IsNodeBusy()
        );

        dialogueAdvanceDispatcher.Initialize(advanceGategate, dialogueRunner, inlineEventMarkupHandler, _linePresentationAdvanceState);
        vnAdvanceInputPoller.Initialize(dialogueAdvanceDispatcher);
    }


    private void BootstrapPlaybackControls()
    {
        BacklogRecorder backlogRecorder = new(yarnLineLifecycleBridge, _vnPlaybackSettings);

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

        RollbackController rollbackController = new(
            _rollbackHistory,
            yarnLineLifecycleBridge,
            dialogueAdvanceDispatcher,
            _linePresentationAdvanceState
        );

        vnFeatureController.Initialize(
            _vnUxState,
            _vnPlaybackSettings,
            _presentationSessionContext,
            _linePresentationAdvanceState,
            ellipsisBreathTypewriter,
            inlineEventMarkupHandler,
            backlogRecorder,
            autoAdvanceScheduler,
            holdSkipController,
            rollbackController);
    }
    
    private void BootstrapVnSaveLoadRuntime()
    {

        VNRuntimeStateProvider vnRuntimeStateProvider = new (yarnLineLifecycleBridge, _rollbackHistory, vnPlaytimeTracker);
        VNLoadSeekDriver vnLoadSeekDriver = new (
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
    }
    
    private void BootstrapUIBindings()
    {
        _dialogueUIBindings = new PresentationViewUIBindings(
            _episodePlayState, 
            vnFeatureController,
            _vnUxState,
            vnRuntimeBridge, 
            dialogueAdvanceDispatcher,
            _vnSaveLoadSystem,
            episodePlayer,
            _linePresentationAdvanceState
            );
        
        _episodeFlowController = new EpisodeFlowController(
            _dialogueUIBindings,
            episodePlayer,
            _episodePlayState);
        
        _screenBindings = new VnScreenBindings(_episodeFlowController, _vnSaveLoadSystem);
    }

    private void InitializeEpisodePlayer()
    {
        episodePlayer.Initialize(_screenBindings, _dialogueUIBindings, _rollbackHistory, customLinePresenter);

        _screenBindings.AttachEpisodePlayer(episodePlayer);
    }

    private void Start()
    {
        _screenBindings.GoToTitle();
    }
}