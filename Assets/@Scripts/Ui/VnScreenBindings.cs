using System;
using UnityEngine;

public sealed class VnScreenBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();
    private static UIManager UI => UIManager.Instance;
    
    private EpisodeFlowController _episodeFlowController;
    
    private EpisodePlayer _episodePlayer;

    private UIBase _boundRoot;

    public VnScreenBindings(EpisodeFlowController episodeFlowController)
    {
        _episodeFlowController = episodeFlowController;
    }

    public void AttachEpisodePlayer(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }
    
    #region Title
    public void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(root =>
        {
            RebindRoot(root, BindTitleRoot);
        });
    }
    
    private void BindTitleRoot(TitleUIRoot titleRoot)
    {
        _ctx.Bind(titleRoot, t => t.OnStart += OnNewGamePressed, t => t.OnStart -= OnNewGamePressed);
        _ctx.Bind(titleRoot, t => t.OnContinue += OnContinuePressed, t => t.OnContinue -= OnContinuePressed);
    }
    
    private void OnNewGamePressed()
    {
        _episodePlayer.StartGame();
        //GoToLobby();
    }
    
    private void OnContinuePressed()
    { }
    
    #endregion
    
    #region Lobby
    
    public void GoToLobby()
    {
        UI.SwitchRoot<LobbyUIRoot>(root =>
        {
            RebindRoot(root, BindLobbyRoot);
        });
    }
    
    
    private void BindLobbyRoot(LobbyUIRoot lobbyRoot)
    {
        _ctx.Bind(lobbyRoot, l => l.OnOpenStory += OpenStorySelectFlow, l => l.OnOpenStory -= OpenStorySelectFlow);
        _ctx.Bind(lobbyRoot, l => l.OnNextBroadcastRequested += OnNextBroadcastRequested, l => l.OnNextBroadcastRequested -= OnNextBroadcastRequested);
    }
    
    private void OnNextBroadcastRequested()
    { }

    private void OpenStorySelectFlow()
    {
        _episodeFlowController.OpenSelectChapterPanel();
    }
    
    #endregion
    
    private void RebindRoot<T>(T root, Action<T> bind)
        where T : UIBase
    {
        if (!root) return;

        if (_boundRoot != null && _boundRoot != root)
            _ctx.Unbind(_boundRoot);

        _ctx.Unbind(root);
        _boundRoot = root;
        bind(root);
    }
    
    public void Dispose()
    {
        _ctx.Dispose();
    }
}
