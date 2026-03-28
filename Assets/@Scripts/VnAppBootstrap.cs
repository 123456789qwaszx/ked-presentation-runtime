using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    public PlaybackSettings settings = new ();
    private readonly UnityInputSource _unityInputSource = new();
    private readonly UnityTimeSource _unityTimeSource = new();
    private readonly SignalLatch _signalLatch = new();
    
    [Header("UI")]
    [SerializeField] private EpisodePlayer episodePlayer;
    
    private DialogueUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    
    [Header("Presentation")]
    [SerializeField] private CommandExecutor commandExecutor;
    [SerializeField] private UnitySignalBus unitySignalBus;
    [SerializeField] private PortraitGeneratedDBSO portraitGeneratedDbSo;
    
    public PresentationSession Session { get; private set; }

    private PresentationSessionBridge _presentationSessionBridge;
    
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private YarnUIBridge yarnUIBridge;
    [SerializeField] private PresentationRouteEntry presentationRouteEntry;
    
    private void Awake()
    {
        UIBootStrap();
        PresentationSessionBootstrap();
        BuildBridgePresentationSessionToYarn();
        YarnBootstrap();
    }

    private void UIBootStrap()
    {
        EpisodePlayState episodePlayState = new EpisodePlayState(); 
        _dialogueUIBindings = new DialogueUIBindings(episodePlayState);
        _episodeFlowController = new EpisodeFlowController(_dialogueUIBindings, episodePlayer, episodePlayState);
        _screenBindings = new VnScreenBindings(_episodeFlowController);
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
        
        Session = new PresentationSession(gatePlanner, gateAdvancer, commandExecutor, settings);
    }

    private void BuildBridgePresentationSessionToYarn()
    {
        _presentationSessionBridge = new(Session, unitySignalBus);
    }
    
    private void YarnBootstrap()
    {
        VnRuntimeBridge vnRuntimeBridge = new VnRuntimeBridge(dialogueRunner, presentationRouteEntry, _presentationSessionBridge);
        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(dialogueRunner, yarnUIBridge, vnRuntimeBridge);
        yarnCommandRegistry.Initialize();
    }
    
    private void Start()
    {
        _screenBindings.GoToTitle();
    }
}