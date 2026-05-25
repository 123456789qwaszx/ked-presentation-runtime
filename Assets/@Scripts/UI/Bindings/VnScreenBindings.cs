using System;
using UnityEngine;

public sealed partial class VnScreenBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();
    private static UIManager UI => UIManager.Instance;

    private readonly EpisodeFlowController _episodeFlowController;
    private readonly VNSaveLoadSystem _vnSaveLoadSystem;

    private EpisodePlayer _episodePlayer;

    private SaveLoadMenuMode _currentSaveLoadMode;

    private UIBase _boundRoot;

    public VnScreenBindings(EpisodeFlowController episodeFlowController, VNSaveLoadSystem vnSaveLoadSystem)
    {
        _episodeFlowController = episodeFlowController;
        _vnSaveLoadSystem = vnSaveLoadSystem;
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
        _ctx.Bind(
            titleRoot,
            t => t.OnStart += OnNewGamePressed,
            t => t.OnStart -= OnNewGamePressed);

        _ctx.Bind(
            titleRoot,
            t => t.OnContinue += OnContinuePressed,
            t => t.OnContinue -= OnContinuePressed);

        _ctx.Bind(
            titleRoot,
            t => t.OnOpenLoad += OnOpenLoadPressed,
            t => t.OnOpenLoad -= OnOpenLoadPressed);

        _ctx.Bind(
            titleRoot,
            t => t.OnOpenAlbum += OnOpenAlbumPressed,
            t => t.OnOpenAlbum -= OnOpenAlbumPressed);

        _ctx.Bind(
            titleRoot,
            t => t.OnOpenSettings += OnOpenSettingsPressed,
            t => t.OnOpenSettings -= OnOpenSettingsPressed);

        _ctx.Bind(
            titleRoot,
            t => t.OnQuit += OnQuitPressed,
            t => t.OnQuit -= OnQuitPressed);

        RefreshTitleState(titleRoot);

        // if (_vnServiceContainer != null)
        // {
        //     Action refresh = () => RefreshTitleState(titleRoot);
        //
        //     _ctx.Assign(
        //         titleRoot,
        //         _ =>
        //         {
        //             _vnServiceContainer.PersistentInitialized += refresh;
        //             _vnServiceContainer.RuntimeBound += refresh;
        //         },
        //         _ =>
        //         {
        //             _vnServiceContainer.PersistentInitialized -= refresh;
        //             _vnServiceContainer.RuntimeBound -= refresh;
        //         });
        // }
    }

    private void RefreshTitleState(TitleUIRoot titleRoot)
    {
        if (titleRoot == null)
            return;

        bool hasContainer = _vnSaveLoadSystem != null;
        bool persistentReady = hasContainer && _vnSaveLoadSystem.IsInitialized;

        bool canContinue =
            hasContainer &&
            _vnSaveLoadSystem.CanContinue();

        titleRoot.SetContinueEnabled(canContinue);
        titleRoot.SetLoadEnabled(persistentReady);
        titleRoot.SetAlbumEnabled(persistentReady);
    }

    private void OnNewGamePressed()
    {
        _episodePlayer.StartGame(_episodePlayer.YarnEntryKey);
    }

    private void OnContinuePressed()
    {
        if (!_vnSaveLoadSystem.TryContinue())
            Debug.LogWarning("[VnScreenBindings] Continue failed.");
    }

    private void OnOpenLoadPressed()
    {
        GoToLoadMenu();
    }

    private void OnOpenAlbumPressed()
    {
        GoToAlbum();
    }

    private void OnOpenSettingsPressed()
    {
        Debug.Log("[VnScreenBindings] Settings requested.");
    }

    private void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region SaveLoad

    public void GoToSaveMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    public void GoToLoadMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        _currentSaveLoadMode = mode;

        UI.PushPanel<SaveLoadMenuUIPanel>(root =>
        {
            RebindRoot(root, BindSaveLoadRoot);
        });
    }

    private void BindSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        _ctx.Bind(
            saveLoadRoot,
            r => r.OnSlotSelected += OnSaveLoadSlotSelected,
            r => r.OnSlotSelected -= OnSaveLoadSlotSelected);

        _ctx.Bind(
            saveLoadRoot,
            r => r.OnCloseRequested += OnSaveLoadCloseRequested,
            r => r.OnCloseRequested -= OnSaveLoadCloseRequested);

        RefreshSaveLoadRoot(saveLoadRoot);
    }

    private void RefreshSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        VNSaveSlotMeta[] metas = _vnSaveLoadSystem.GetAllSaveSlotMetas();

        saveLoadRoot.Rebuild(
            _currentSaveLoadMode,
            metas);
    }

    private void OnSaveLoadSlotSelected(int slotIndex)
    {
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            HandleSaveSlotSelected(slotIndex);
            return;
        }

        HandleLoadSlotSelected(slotIndex);
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (!_vnSaveLoadSystem.SaveService.SaveManual(slotIndex))
        {
            Debug.LogWarning($"[VnScreenBindings] Save failed. slotIndex={slotIndex}");
            return;
        }

        UIBase currentRoot = _boundRoot;

        if (currentRoot is SaveLoadMenuUIPanel saveLoadRoot)
            RefreshSaveLoadRoot(saveLoadRoot);
    }

    private void HandleLoadSlotSelected(int slotIndex)
    {
        if (!_vnSaveLoadSystem.LoadService.Load(slotIndex))
        {
            Debug.LogWarning($"[VnScreenBindings] Load failed. slotIndex={slotIndex}");
            return;
        }
    }

    private void OnSaveLoadCloseRequested()
    {
        GoToTitle();
    }

    #endregion

    #region Album

    public void GoToAlbum()
    {
        UI.SwitchRoot<AlbumUIRoot>(root =>
        {
            RebindRoot(root, BindAlbumRoot);
        });
    }

    private void BindAlbumRoot(AlbumUIRoot albumRoot)
    {
        _ctx.Bind(
            albumRoot,
            a => a.OnCloseRequested += OnAlbumCloseRequested,
            a => a.OnCloseRequested -= OnAlbumCloseRequested);

        RefreshAlbumRoot(albumRoot);
    }

    private void RefreshAlbumRoot(AlbumUIRoot albumRoot)
    {
        VNAlbumUnlockService albumService = _vnSaveLoadSystem.AlbumService;

        albumRoot.Rebuild(
            albumService.GetAllItems(),
            albumService.IsUnlocked);
    }

    private void OnAlbumCloseRequested()
    {
        GoToTitle();
    }

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
        _ctx.Bind(
            lobbyRoot,
            l => l.OnOpenStory += OpenStorySelectFlow,
            l => l.OnOpenStory -= OpenStorySelectFlow);

        _ctx.Bind(
            lobbyRoot,
            l => l.OnNextBroadcastRequested += OnNextBroadcastRequested,
            l => l.OnNextBroadcastRequested -= OnNextBroadcastRequested);
    }

    private void OnNextBroadcastRequested()
    {
    }

    private void OpenStorySelectFlow()
    {
        _episodeFlowController.OpenSelectChapterPanel();
    }

    #endregion

    private void RebindRoot<T>(T root, Action<T> bind)
        where T : UIBase
    {
        if (!root)
            return;

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