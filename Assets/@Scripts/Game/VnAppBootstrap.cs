using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] UIManager uiManager;
    
    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    private readonly VnUxState _vnUxState = new ();
    private readonly VnPlaybackSettings _vnPlaybackSettings = new ();
    private readonly EpisodePlayState _episodePlayState = new ();
    private readonly PresentationSessionContext _presentationSessionContext = new();
    private readonly LinePresentationAdvanceState  _linePresentationAdvanceState = new();
    
    [Header("Sound")]
    [SerializeField] private AudioSystem audioSystem;
    [SerializeField] private InlineSfxHost inlineSfxHost;
    
    [Header("PresentationView")]
    [SerializeField] private BGHost bgHost;
    [SerializeField] private DialogueBoxHost dialogueBoxHost;

    [SerializeField] private PresentationResponseRig presentationResponseRig;
    
    [Header("Presentation")]
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;
    [SerializeField] private UnitySignalBus unitySignalBus;
    [SerializeField] private CommandExecutor commandExecutor;
    
    [Header("PresentationEntry")]
    [SerializeField] private RouteCatalogSO routeCatalogSo;
    [SerializeField] private PresentationSessionEntry presentationSessionEntry;
    
    [Header("ImmediateCommandRunner")]
    [SerializeField] private YarnBridgePlaybackDriver yarnBridgePlaybackDriver;
    [SerializeField] private YarnCommandBridge yarnCommandBridge;
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
    
    [Header("UI")]
    [SerializeField] private EpisodePlayer episodePlayer;
    
    [Header("Transition")]
    [SerializeField] private TransitionTargetRouter transitionTargetRouter;
    
    private PresentationSessionBridge _presentationSessionBridge;
    
    private PresentationViewUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    private RollbackHistory _rollbackHistory;
    private UIPatchService _uiPatchService;
    
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
        
        BootstrapUIBindings();
        InitializeEpisodePlayer();

    }

    private void BootstrapAudioSystem()
    {
        audioSystem.Initialize();
    }
    
    private void ConnectAudioSystemToYarn()
    {
        ResourcesAudioClipResolver audioClipResolver = new ();
        inlineSfxHost.Initialize(audioSystem, audioClipResolver);
    }

    private void BootstrapUIManager()
    {
        SpritePortAssignmentBuilder spritePortAssignmentBuilder = new ();
        ResourcesUISpriteLoader resourcesUISpriteLoader = new ();
        UISpritePatcher uiSpritePatcher = new (resourcesUISpriteLoader);
        _uiPatchService = new UIPatchService(spritePortAssignmentBuilder, uiSpritePatcher);
        
        uiManager.Init();
        uiManager.AttachUIPatchService(_uiPatchService);
    }
    
    private void BootstrapPresentationSession()
    {
        SignalLatch signalLatch = new();
        unitySignalBus.OnSignal += signalLatch.Latch;
        
        StepGatePlanBuilder gatePlanner = new ();
        StepGateAdvancer gateAdvancer = new (_unityInputSource, _unityTimeSource, unitySignalBus, signalLatch);
        
        // SignalFactory
        SignalCommandFactory signalFactory = new(_unityTimeSource, unitySignalBus, signalLatch);
        
        // CharRigFactory
        CharRigSlotResolver charRigSlotResolver = new();
        CharacterRigAccess charRigAccess = new(charRigSlotResolver);
        PortraitResolver portraitResolver = new (portraitGeneratedDbSo);
        CharRigCommandFactory charRigFactory = new(charRigAccess, portraitResolver);
        
        // TransitionFactory
        TransitionCommandFactory transitionCommandFactory = new(transitionTargetRouter, _uiPatchService);
        
        // SoundFactory
        ResourcesAudioClipResolver audioClipResolver = new ();
        SoundCommandFactory soundCommandFactory = new SoundCommandFactory(audioSystem, audioClipResolver);
        
        //PresentationViewCommandFactory
        PresentationViewAccess presentationViewAccess = new ();
        PresentationViewCommandFactory presentationViewCommandFactory = new(presentationViewAccess, presentationResponseRig, bgHost, bgHost, dialogueBoxHost);
        
        CompositeCommandFactory factory = new (signalFactory, charRigFactory, transitionCommandFactory, soundCommandFactory, presentationViewCommandFactory);
        commandExecutor.Initialize(factory);
        
        PresentationSession presentationSession = new PresentationSession(gatePlanner, gateAdvancer, commandExecutor, _presentationSessionContext);
        
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

        yarnCommandBridge.Initialize(
            dialogueRunner,
            yarnBridgePlaybackDriver);

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
        yarnLineLifecycleBridge.Initialize(dialogueRunner);
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

        dialogueAdvanceDispatcher.Initialize(advanceGategate, dialogueRunner, inlineEventMarkupHandler);
        vnAdvanceInputPoller.Initialize(dialogueAdvanceDispatcher);
    }
    
    
    private void BootstrapPlaybackControls()
    {
        BacklogRecorder backlogRecorder = new (yarnLineLifecycleBridge, _vnPlaybackSettings);
        
        AutoAdvanceScheduler autoAdvanceScheduler = new (
            yarnLineLifecycleBridge,
            _vnPlaybackSettings,
            dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);
        
        HoldSpeedUpController holdSkipController = new(
            _vnPlaybackSettings,
            ellipsisBreathTypewriter,
            dialogueAdvanceDispatcher,
            _presentationSessionContext,
            () => yarnLineLifecycleBridge.IsLineFullyShown);
        
        _rollbackHistory = new RollbackHistory();
        
        RollbackController rollbackController = new (
            _rollbackHistory,
            yarnLineLifecycleBridge,
            episodePlayer,
            dialogueAdvanceDispatcher,
            _presentationSessionBridge,
            _presentationSessionContext,
            UIManager.Instance.GetUI<PresentationUIRoot>(),
            customLinePresenter,
            _linePresentationAdvanceState
            
        );
        
        vnFeatureController.Initialize(
            _vnUxState,
            _vnPlaybackSettings,
            _presentationSessionContext,
            yarnLineLifecycleBridge,
            ellipsisBreathTypewriter,
            inlineEventMarkupHandler,
            backlogRecorder,
            autoAdvanceScheduler,
            holdSkipController,
            rollbackController);
    }
    
    private void BootstrapUIBindings()
    {
        _dialogueUIBindings = new PresentationViewUIBindings(_episodePlayState, vnFeatureController, _vnUxState, vnRuntimeBridge, dialogueAdvanceDispatcher);
        _episodeFlowController = new EpisodeFlowController(_dialogueUIBindings, episodePlayer, _episodePlayState);
        _screenBindings = new VnScreenBindings(_episodeFlowController);
    }

    private void InitializeEpisodePlayer()
    {
        episodePlayer.Initialize(_screenBindings, _dialogueUIBindings, _rollbackHistory);
    }
    
    private void Start()
    {
        //_screenBindings.GoToTitle();
    }
}