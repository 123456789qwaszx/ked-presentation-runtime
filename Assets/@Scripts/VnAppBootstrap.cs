using UnityEngine;
using Yarn.Unity;

public class VnAppBootstrap : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private EpisodePlayer episodePlayer;
    
    private DialogueUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private YarnUIBridge yarnUIBridge;
    
    private void Awake()
    {
        UIBootStrap();
        YarnBootstrap();
    }

    private void UIBootStrap()
    {
        EpisodePlayState episodePlayState = new EpisodePlayState(); 
        _dialogueUIBindings = new DialogueUIBindings(episodePlayState);
        _episodeFlowController = new EpisodeFlowController(_dialogueUIBindings, episodePlayer, episodePlayState);
        _screenBindings = new VnScreenBindings(_episodeFlowController);
    }

    private void YarnBootstrap()
    {
        YarnCommandRegistry yarnCommandRegistry = new YarnCommandRegistry(dialogueRunner, yarnUIBridge);
        yarnCommandRegistry.Initialize();
    }
    
    private void Start()
    {
        _screenBindings.GoToTitle();
    }
}