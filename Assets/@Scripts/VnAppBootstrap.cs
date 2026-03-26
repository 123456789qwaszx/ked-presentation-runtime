using UnityEngine;

public class VnAppBootstrap : MonoBehaviour
{
    [SerializeField] private EpisodePlayer episodePlayer;
    
    private DialogueUIBindings _dialogueUIBindings;
    private EpisodeFlowController _episodeFlowController;
    private VnScreenBindings _screenBindings;
    
    private void Awake()
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