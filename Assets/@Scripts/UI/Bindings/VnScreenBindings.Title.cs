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
        UI.SwitchRoot<TitleUIRoot>(root =>
        {
            BindMain(root, ApplyBindings);
            Refresh(root);
        });
    }
    
    private void ApplyBindings(TitleUIRoot root)
    {
        AddBinding(root, 
            r => r.StartClicked += HandleStartClicked,
            r => r.StartClicked -= HandleStartClicked);

        AddBinding(root,
            r => r.ContinueClicked += HandleContinueClicked,
            r => r.ContinueClicked -= HandleContinueClicked);

        AddBinding(root,
            r => r.LoadClicked += HandleLoadClicked,
            r => r.LoadClicked -= HandleLoadClicked);

        AddBinding(root,
            r => r.AlbumClicked += HandleAlbumClicked,
            r => r.AlbumClicked -= HandleAlbumClicked);

        AddBinding(root,
            r => r.SettingsClicked += HandleSettingsClicked,
            r => r.SettingsClicked -= HandleSettingsClicked);

        AddBinding(root,
            r => r.QuitClicked += OnQuitPressed,
            r => r.QuitClicked -= OnQuitPressed);
    }
    
    private void HandleStartClicked()
    {
        _episodePlayer.StartGame(_episodePlayer.YarnEntryKey);
    }

    private void HandleContinueClicked()
    {
        // if (!_vnSaveLoadSystem.TryContinue())
        //     Debug.LogWarning("[VnScreenBindings] Continue failed.");
    }

    private void HandleLoadClicked()
    {
        //OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void HandleAlbumClicked()
    {
        //OpenAlbumMenuPanel();
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
    
    private void Refresh(TitleUIRoot titleRoot)
    {
       // bool canContinue = _vnSaveLoadSystem.CanContinue();

        // titleRoot.SetContinueEnabled(canContinue);
        // titleRoot.SetLoadEnabled(true);
        // titleRoot.SetAlbumEnabled(true);
    }
}