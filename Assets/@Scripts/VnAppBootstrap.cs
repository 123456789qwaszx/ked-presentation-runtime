using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    private readonly SignalLatch _signalLatch = new();
    private readonly PlaybackSettings _settings = new ();
    
    [Header("Presentation")]
    [SerializeField] private CommandExecutor commandExecutor;
    [SerializeField] private UnitySignalBus unitySignalBus;
    [SerializeField] private PortraitGeneratedDbSo portraitGeneratedDbSo;
    [SerializeField] private RouteCatalogSO routeCatalogSo;
    [SerializeField] private PresentationSessionEntry presentationSessionEntry;
    
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private YarnUIBridge yarnUIBridge;
    [SerializeField] private InlineEventMarkupHandler inlineEventMarkupHandler;
    
    [Header("VnAdvanceGate")]
    [SerializeField] private EllipsisBreathTypewriter ellipsisBreathTypewriter;
    [SerializeField] VnRawInputPoller vnRawInputPoller;
    
    [Header("YarnVnInputFeature")]
    [SerializeField] YarnLineLifecycleBridge yarnLineLifecycleBridge;
    [SerializeField] VnFeatureController vnFeatureController;
    
    
    [Header("UI")]
    [SerializeField] private EpisodePlayer episodePlayer;
    
    private DialogueUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    
    private PresentationSessionBridge _presentationSessionBridge;
    
    private void Awake()
    {
        PresentationSessionBootstrap();
        BuildBridgePresentationSessionToYarn();
        YarnBootstrap();
        VnAdvanceInputBootstrap();
        
        UIBootStrap();
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
        VnRuntimeBridge vnRuntimeBridge = new VnRuntimeBridge(dialogueRunner, presentationSessionEntry, _presentationSessionBridge);
        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(dialogueRunner, yarnUIBridge, vnRuntimeBridge);
        yarnCommandRegistry.Initialize();
        inlineEventMarkupHandler.Initiailze(_presentationSessionBridge);
    }

    private void VnAdvanceInputBootstrap()
    {
        PresentationSession session = presentationSessionEntry.PresentationSession;
        
        VnUxState vnUxState = new();
        AdvanceGate advanceGategate = new(
            vnUxState,
            ellipsisBreathTypewriter,
            () => session != null && session.IsNodeBusy()
        );
        
        DialogueAdvanceRouter advanceRouter = new(
            advanceGategate,
            dialogueRunner,
            inlineEventMarkupHandler
        );
        
        vnRawInputPoller.Initialize(advanceRouter);
        
        yarnLineLifecycleBridge.Initialize();
        
        VnFeaturePolicy vnFeaturePolicy = new ();
        BacklogRecorder backlogRecorder = new BacklogRecorder(yarnLineLifecycleBridge,
            _unityTimeSource,
            vnFeaturePolicy.maxLogCount);
        AutoAdvanceScheduler auto = new AutoAdvanceScheduler(
            yarnLineLifecycleBridge,
            vnUxState, 
            vnFeaturePolicy,
            _unityTimeSource,
            () => _unityTimeSource.UnscaledDeltaTime);
    }
    
    private void UIBootStrap()
    {
        EpisodePlayState episodePlayState = new EpisodePlayState(); 
        _dialogueUIBindings = new DialogueUIBindings(episodePlayState);
        _episodeFlowController = new EpisodeFlowController(_dialogueUIBindings, episodePlayer, episodePlayState);
        _screenBindings = new VnScreenBindings(_episodeFlowController);
    }
    
    private void Start()
    {
        _screenBindings.GoToTitle();
    }
}