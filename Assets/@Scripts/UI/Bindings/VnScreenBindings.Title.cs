using UnityEngine;

public sealed partial class VnScreenBindings
{
    public void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(root =>
        {
            BindMain(root, BindTitleRoot);
        });
    }

    private void BindTitleRoot(TitleUIRoot titleRoot)
    {
        BindEvent(
            titleRoot,
            t => t.OnStart += OnNewGamePressed,
            t => t.OnStart -= OnNewGamePressed);

        BindEvent(
            titleRoot,
            t => t.OnContinue += OnContinuePressed,
            t => t.OnContinue -= OnContinuePressed);

        BindEvent(
            titleRoot,
            t => t.OnOpenLoad += OnOpenLoadPressed,
            t => t.OnOpenLoad -= OnOpenLoadPressed);

        BindEvent(
            titleRoot,
            t => t.OnOpenAlbum += OnOpenAlbumPressed,
            t => t.OnOpenAlbum -= OnOpenAlbumPressed);

        BindEvent(
            titleRoot,
            t => t.OnOpenSettings += OnOpenSettingsPressed,
            t => t.OnOpenSettings -= OnOpenSettingsPressed);

        BindEvent(
            titleRoot,
            t => t.OnQuit += OnQuitPressed,
            t => t.OnQuit -= OnQuitPressed);

        RefreshTitleState(titleRoot);
    }

    private void RefreshTitleState(TitleUIRoot titleRoot)
    {
        if (titleRoot == null)
            return;

        bool hasSystem = _vnSaveLoadSystem != null;
        bool persistentReady = hasSystem && _vnSaveLoadSystem.IsInitialized;

        bool canContinue =
            hasSystem &&
            _vnSaveLoadSystem.CanContinue();

        titleRoot.SetContinueEnabled(canContinue);
        titleRoot.SetLoadEnabled(persistentReady);
        titleRoot.SetAlbumEnabled(persistentReady);
    }

    private void OnNewGamePressed()
    {
        if (_episodePlayer == null)
        {
            Debug.LogWarning("[VnScreenBindings] EpisodePlayer is null.");
            return;
        }

        _episodePlayer.StartGame(_episodePlayer.YarnEntryKey);
    }

    private void OnContinuePressed()
    {
        if (_vnSaveLoadSystem == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNSaveLoadSystem is null.");
            return;
        }

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
}