using UnityEngine;

public sealed partial class VnScreenBindings
{
    private EpisodePlayer _episodePlayer;
    
    public void ConfigureTitleView(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }
    
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(titleRoot =>
        {
            BindMain(titleRoot, ApplyBindings);
            RefreshTitleState(titleRoot);
        });
    }
    
    private void ApplyBindings(TitleUIRoot titleRoot)
    {
        AddBinding(titleRoot, 
            t => t.StartClicked += HandleStartClicked,
            t => t.StartClicked -= HandleStartClicked);

        AddBinding(titleRoot,
            t => t.ContinueClicked += HandleContinueClicked,
            t => t.ContinueClicked -= HandleContinueClicked);

        AddBinding(titleRoot,
            t => t.LoadClicked += HandleLoadClicked,
            t => t.LoadClicked -= HandleLoadClicked);

        AddBinding(titleRoot,
            t => t.AlbumClicked += HandleAlbumClicked,
            t => t.AlbumClicked -= HandleAlbumClicked);

        AddBinding(titleRoot,
            t => t.SettingsClicked += HandleSettingsClicked,
            t => t.SettingsClicked -= HandleSettingsClicked);

        AddBinding(titleRoot,
            t => t.QuitClicked += OnQuitPressed,
            t => t.QuitClicked -= OnQuitPressed);

    }
    
    private void HandleStartClicked()
    {
        _episodePlayer.StartGame(_episodePlayer.YarnEntryKey);
    }

    private void HandleContinueClicked()
    {
        if (!_vnSaveLoadSystem.TryContinue())
            Debug.LogWarning("[VnScreenBindings] Continue failed.");
    }

    private void HandleLoadClicked()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void HandleAlbumClicked()
    {
        OpenAlbumMenuPanel();
    }

    private void HandleSettingsClicked()
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
    
    private void RefreshTitleState(TitleUIRoot titleRoot)
    {
        bool canContinue = _vnSaveLoadSystem.CanContinue();

        titleRoot.SetContinueEnabled(canContinue);
        titleRoot.SetLoadEnabled(true);
        titleRoot.SetAlbumEnabled(true);
    }
}