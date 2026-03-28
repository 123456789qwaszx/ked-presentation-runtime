using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    private readonly SignalLatch _signalLatch = new();
    private readonly PlaybackSettings _settings = new ();
    private readonly VnUxState _vnUxState = new ();
    private readonly EpisodePlayState _episodePlayState = new (); 
    
    [Header("Presentation")]
    [SerializeField] private CommandExecutor commandExecutor;
    [SerializeField] private UnitySignalBus unitySignalBus;
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;
    [SerializeField] private RouteCatalogSO routeCatalogSo;
    [SerializeField] private PresentationSessionEntry presentationSessionEntry;
    
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private VnRuntimeBridge vnRuntimeBridge;
    [SerializeField] private YarnUIBridge yarnUIBridge;
    [SerializeField] private InlineEventMarkupHandler inlineEventMarkupHandler;
    
    [Header("VnAdvanceGate")]
    [SerializeField] private EllipsisBreathTypewriter ellipsisBreathTypewriter;
    [SerializeField] VnRawInputPoller vnRawInputPoller;
    [SerializeField] DialogueAdvanceRouter dialogueAdvanceRouter;
    
    [Header("YarnVnInputFeature")]
    [SerializeField] YarnLineLifecycleBridge yarnLineLifecycleBridge;
    [SerializeField] VnFeatureController vnFeatureController;
    
    [Header("UI")]
    [SerializeField] private EpisodePlayer episodePlayer;
    
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    private DialogueUIBindings _dialogueUIBindings;
    
    private PresentationSessionBridge _presentationSessionBridge;
    
    private void Awake()
    {
        PresentationSessionBootstrap();
        
        BuildBridgePresentationSessionToYarn();
        YarnBootstrap();
        VnAdvanceInputBootstrap();
        UIBootStrap();
        VnFeatureControllerBootstrap();
    }
    
    private void PresentationSessionBootstrap()
    {
        StepGatePlanBuilder gatePlanner = new ();
        unitySignalBus.OnSignal += _signalLatch.Latch;
        StepGateAdvancer gateAdvancer = new (_unityInputSource, _unityTimeSource, unitySignalBus, _signalLatch);
        
        SignalCommandFactory signalFactory = new(_unityTimeSource, unitySignalBus, _signalLatch);
        CharRigSlotResolver charRigSlotResolver = new();
        CharacterRigAccess charRigAccess = new(charRigSlotResolver);
        PortraitResolver portraitResolver = new (portraitGeneratedDbSo);
        CharRigCommandFactory charRigFactory = new(charRigAccess, portraitResolver);
        commandExecutor.Initialize(signalFactory, charRigFactory);
        
        PresentationSession presentationSession = new PresentationSession(gatePlanner, gateAdvancer, commandExecutor, _settings);
        presentationSessionEntry.Initialize(presentationSession, routeCatalogSo, _settings);
    }

    private void BuildBridgePresentationSessionToYarn()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;
        
        _presentationSessionBridge = new(session, unitySignalBus);
    }
    
    private void YarnBootstrap()
    {
        inlineEventMarkupHandler.Initiailze(_presentationSessionBridge);
        vnRuntimeBridge.Initialize(dialogueRunner, presentationSessionEntry, _presentationSessionBridge);
        
        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(dialogueRunner, yarnUIBridge, vnRuntimeBridge);
        yarnCommandRegistry.Initialize();
    }

    private void VnAdvanceInputBootstrap()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;
        
        AdvanceGate advanceGategate = new(
            _vnUxState,
            ellipsisBreathTypewriter,
            () => session != null && session.IsNodeBusy()
        );

        dialogueAdvanceRouter.Initialize(advanceGategate, dialogueRunner, inlineEventMarkupHandler);
        vnRawInputPoller.Initialize(dialogueAdvanceRouter);
        
    }

    private void VnFeatureControllerBootstrap()
    {
        yarnLineLifecycleBridge.Initialize();
        
        VnFeaturePolicy vnFeaturePolicy = new ();
        BacklogRecorder backlogRecorder = new BacklogRecorder(yarnLineLifecycleBridge, vnFeaturePolicy, _unityTimeSource);
        AutoAdvanceScheduler autoAdvanceScheduler = new AutoAdvanceScheduler(
            yarnLineLifecycleBridge,
            _vnUxState, 
            vnFeaturePolicy,
            dialogueAdvanceRouter,
            () => Time.unscaledTimeAsDouble);
        
        vnFeatureController.Initialize(ellipsisBreathTypewriter, _vnUxState, backlogRecorder, autoAdvanceScheduler);
    }
    
    private void UIBootStrap()
    {
        _dialogueUIBindings = new DialogueUIBindings(_episodePlayState, vnFeatureController, _vnUxState, vnRuntimeBridge, dialogueAdvanceRouter);
        _episodeFlowController = new EpisodeFlowController(_dialogueUIBindings, episodePlayer, _episodePlayState);
        _screenBindings = new VnScreenBindings(_episodeFlowController);

        TestLauncherInitialize();
    }

    private void TestLauncherInitialize()
    {
        episodePlayer.Initialize(_screenBindings, _dialogueUIBindings);
    }
    
    private void Start()
    {
        _screenBindings.GoToTitle();
    }
}