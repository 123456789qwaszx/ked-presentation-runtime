using System;
using UnityEngine;

public sealed class VnScreenBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();
    private static UIManager UI => UIManager.Instance;

    private readonly EpisodeFlowController _episodeFlowController;

    private EpisodePlayer _episodePlayer;
    private VNServiceContainer _vnServiceContainer;

    private SaveLoadMenuMode _currentSaveLoadMode;

    private UIBase _boundRoot;

    public VnScreenBindings(EpisodeFlowController episodeFlowController)
    {
        _episodeFlowController = episodeFlowController;
    }

    public void AttachEpisodePlayer(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }

    public void AttachVNServiceContainer(VNServiceContainer serviceContainer)
    {
        _vnServiceContainer = serviceContainer;
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

        if (_vnServiceContainer != null)
        {
            Action refresh = () => RefreshTitleState(titleRoot);

            _ctx.Assign(
                titleRoot,
                _ =>
                {
                    _vnServiceContainer.PersistentInitialized += refresh;
                    _vnServiceContainer.RuntimeBound += refresh;
                },
                _ =>
                {
                    _vnServiceContainer.PersistentInitialized -= refresh;
                    _vnServiceContainer.RuntimeBound -= refresh;
                });
        }
    }

    private void RefreshTitleState(TitleUIRoot titleRoot)
    {
        if (titleRoot == null)
            return;

        bool hasContainer = _vnServiceContainer != null;
        bool persistentReady = hasContainer && _vnServiceContainer.IsPersistentInitialized;

        bool canContinue =
            hasContainer &&
            _vnServiceContainer.CanContinue();

        titleRoot.SetContinueEnabled(canContinue);
        titleRoot.SetLoadEnabled(persistentReady);
        titleRoot.SetAlbumEnabled(persistentReady);
    }

    private void OnNewGamePressed()
    {
        if (_episodePlayer == null)
        {
            Debug.LogError("[VnScreenBindings] EpisodePlayer is null. Cannot start new game.");
            return;
        }

        _episodePlayer.StartGame();
    }

    private void OnContinuePressed()
    {
        if (_vnServiceContainer == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNServiceContainer is null. Cannot continue.");
            return;
        }

        if (!_vnServiceContainer.CanContinue())
        {
            Debug.LogWarning("[VnScreenBindings] Continue requested, but no continue data exists.");
            return;
        }

        if (!_vnServiceContainer.TryContinue())
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
        if (_vnServiceContainer == null || !_vnServiceContainer.IsPersistentInitialized)
        {
            Debug.LogWarning("[VnScreenBindings] Cannot open Save/Load menu. VN services are not ready.");
            return;
        }

        if (_vnServiceContainer.SaveRepository == null)
        {
            Debug.LogWarning("[VnScreenBindings] SaveRepository is null.");
            return;
        }

        _currentSaveLoadMode = mode;

        UI.SwitchRoot<SaveLoadMenuUIRoot>(root =>
        {
            RebindRoot(root, BindSaveLoadRoot);
        });
    }

    private void BindSaveLoadRoot(SaveLoadMenuUIRoot saveLoadRoot)
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

    private void RefreshSaveLoadRoot(SaveLoadMenuUIRoot saveLoadRoot)
    {
        if (saveLoadRoot == null)
            return;

        if (_vnServiceContainer == null || _vnServiceContainer.SaveRepository == null)
            return;

        VNSaveSlotMeta[] metas = _vnServiceContainer.SaveRepository.GetAllMetas();

        saveLoadRoot.Rebuild(
            _currentSaveLoadMode,
            metas);
    }

    private void OnSaveLoadSlotSelected(int slotIndex)
    {
        if (_vnServiceContainer == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNServiceContainer is null.");
            return;
        }

        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            HandleSaveSlotSelected(slotIndex);
            return;
        }

        HandleLoadSlotSelected(slotIndex);
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (!_vnServiceContainer.IsRuntimeBound || _vnServiceContainer.SaveService == null)
        {
            Debug.LogWarning("[VnScreenBindings] Runtime is not bound. Cannot save.");
            return;
        }

        if (!_vnServiceContainer.SaveService.SaveManual(slotIndex))
        {
            Debug.LogWarning($"[VnScreenBindings] Save failed. slotIndex={slotIndex}");
            return;
        }

        UIBase currentRoot = _boundRoot;

        if (currentRoot is SaveLoadMenuUIRoot saveLoadRoot)
            RefreshSaveLoadRoot(saveLoadRoot);
    }

    private void HandleLoadSlotSelected(int slotIndex)
    {
        if (!_vnServiceContainer.IsRuntimeBound || _vnServiceContainer.LoadService == null)
        {
            Debug.LogWarning("[VnScreenBindings] Runtime is not bound. Cannot load.");
            return;
        }

        if (!_vnServiceContainer.LoadService.Load(slotIndex))
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
        if (_vnServiceContainer == null || !_vnServiceContainer.IsPersistentInitialized)
        {
            Debug.LogWarning("[VnScreenBindings] Cannot open Album. VN services are not ready.");
            return;
        }

        if (_vnServiceContainer.AlbumService == null)
        {
            Debug.LogWarning("[VnScreenBindings] AlbumService is null.");
            return;
        }

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
        if (albumRoot == null)
            return;

        if (_vnServiceContainer == null || _vnServiceContainer.AlbumService == null)
            return;

        VNAlbumUnlockService albumService = _vnServiceContainer.AlbumService;

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