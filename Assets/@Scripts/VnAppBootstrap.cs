using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    private readonly PresentationPlaybackSettings _presentationContextSettings = new ();
    private readonly VnUxState _vnUxState = new ();
    private readonly VnPlaybackSettings _vnPlaybackSettings = new ();
    private readonly EpisodePlayState _episodePlayState = new (); 
    
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
    [SerializeField] private YarnLineRuntimePresenter yarnLineRuntimePresenter;
    
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private DialogueTextRouter dialogueTextRouter;
    [SerializeField] private VnRuntimeBridge vnRuntimeBridge;
    [SerializeField] private YarnUIBridge yarnUIBridge;
    [SerializeField] private InlineEventMarkupHandler inlineEventMarkupHandler;
    
    [Header("VnAdvanceGate")]
    [SerializeField] private EllipsisBreathTypewriter ellipsisBreathTypewriter;
    [SerializeField] private VnAdvanceInputPoller vnAdvanceInputPoller;
    [SerializeField] private DialogueAdvanceDispatcher dialogueAdvanceDispatcher;
    
    [Header("YarnVnInputFeature")]
    [SerializeField] private YarnLineLifecycleBridge yarnLineLifecycleBridge;
    [SerializeField] private VnFeatureController vnFeatureController;
    
    [Header("UI")]
    [SerializeField] private EpisodePlayer episodePlayer;
    
    private PresentationSessionBridge _presentationSessionBridge;
    
    private DialogueUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    
    private void Awake()
    {
        BootstrapPresentationSession();
        ConnectPresentationSessionToYarn();
        
        BootstrapYarn();
        SetupYarnLifecycleBridge();
        
        BootstrapDialogueAdvanceInput();
        BootstrapPlaybackControls();
        
        BootstrapUIBindings();
        InitializeEpisodePlayer();
        
    }
    
    private void BootstrapPresentationSession()
    {
        SignalLatch signalLatch = new();
        unitySignalBus.OnSignal += signalLatch.Latch;
        
        StepGatePlanBuilder gatePlanner = new ();
        StepGateAdvancer gateAdvancer = new (_unityInputSource, _unityTimeSource, unitySignalBus, signalLatch);
        
        SignalCommandFactory signalFactory = new(_unityTimeSource, unitySignalBus, signalLatch);
        CharRigSlotResolver charRigSlotResolver = new();
        CharacterRigAccess charRigAccess = new(charRigSlotResolver);
        PortraitResolver portraitResolver = new (portraitGeneratedDbSo);
        CharRigCommandFactory charRigFactory = new(charRigAccess, portraitResolver);
        commandExecutor.Initialize(signalFactory, charRigFactory);
        
        PresentationSession presentationSession = new PresentationSession(gatePlanner, gateAdvancer, commandExecutor, _presentationContextSettings);
        
        presentationSessionEntry.Initialize(presentationSession, routeCatalogSo, _presentationContextSettings);
    }

    
    private void ConnectPresentationSessionToYarn()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;
        
        _presentationSessionBridge = new(session, unitySignalBus);
    }
    
    private void BootstrapYarn()
    {
        inlineEventMarkupHandler.Initialize(_presentationSessionBridge);
        vnRuntimeBridge.Initialize(dialogueRunner, presentationSessionEntry, _presentationSessionBridge);
        yarnUIBridge.Initialize(linePresenter, ellipsisBreathTypewriter, dialogueTextRouter);
        
        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(dialogueRunner, yarnUIBridge, vnRuntimeBridge);
        yarnCommandRegistry.Initialize();
        
        yarnCommandBridge.Initialize(dialogueRunner);
        yarnBridgePlaybackDriver.Initialize(yarnCommandBridge, commandExecutor, _presentationContextSettings);
        yarnLineRuntimePresenter.Initialize(dialogueRunner, yarnCommandBridge, yarnBridgePlaybackDriver);
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
            yarnLineLifecycleBridge,
            () => session != null && session.IsNodeBusy()
        );

        dialogueAdvanceDispatcher.Initialize(advanceGategate, dialogueRunner, inlineEventMarkupHandler);
        vnAdvanceInputPoller.Initialize(dialogueAdvanceDispatcher);
    }

    
    [Header("RollbackHistoryDebugOverlay")]
    public RollbackHistoryDebugOverlay overlay;
    private void CreateRollbackHistoryDebugTool(NodeRollbackHistory history, RollbackRuntimeState runtimeState)
    {
        overlay.Initialize(history, runtimeState);
    }
    
    private void BootstrapPlaybackControls()
    {
        BacklogRecorder backlogRecorder = new BacklogRecorder(yarnLineLifecycleBridge, _vnPlaybackSettings);
        AutoAdvanceScheduler autoAdvanceScheduler = new AutoAdvanceScheduler(
            yarnLineLifecycleBridge,
            _vnPlaybackSettings,
            dialogueAdvanceDispatcher,
            () => Time.unscaledTimeAsDouble);
        HoldSpeedUpController holdSkipController = new(
            _vnPlaybackSettings,
            ellipsisBreathTypewriter,
            dialogueAdvanceDispatcher,
            () => yarnLineLifecycleBridge.IsLineFullyShown);
        
        RollbackRuntimeState rollbackState = new RollbackRuntimeState();
        NodeRollbackHistory rollbackHistory = new NodeRollbackHistory(yarnLineLifecycleBridge, rollbackState, _presentationSessionBridge);
        
        RollbackController rollbackController = new RollbackController(
            state: rollbackState,
            history: rollbackHistory,
            bridge: yarnLineLifecycleBridge,
            episodePlayer,
            dispatcher: dialogueAdvanceDispatcher,
            inlineMarkupHandler: inlineEventMarkupHandler,
            typewriter: ellipsisBreathTypewriter,
            playbackSettings: _vnPlaybackSettings,
            _presentationSessionBridge,
            yarnBridgePlaybackDriver,
            commandExecutor
        );
        
        CreateRollbackHistoryDebugTool(rollbackHistory, rollbackState);
        
        vnFeatureController.Initialize(
            _vnUxState,
            _vnPlaybackSettings,
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
        _dialogueUIBindings = new DialogueUIBindings(_episodePlayState, vnFeatureController, _vnUxState, vnRuntimeBridge, dialogueAdvanceDispatcher);
        _episodeFlowController = new EpisodeFlowController(_dialogueUIBindings, episodePlayer, _episodePlayState);
        _screenBindings = new VnScreenBindings(_episodeFlowController);
    }

    private void InitializeEpisodePlayer()
    {
        episodePlayer.Initialize(_screenBindings, _dialogueUIBindings);
    }
    
    private void Start()
    {
        _screenBindings.GoToTitle();
    }
}