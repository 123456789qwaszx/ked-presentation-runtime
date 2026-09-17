using UnityEngine;

public sealed partial class VNScreenBindings
{
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(
            afterPatched: root =>
            {
                BindView(root, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(TitleUIRoot root)
    {
        AddBinding(root,
            r => r.ContinueClicked += HandleContinueClicked,
            r => r.ContinueClicked -= HandleContinueClicked);

        AddBinding(root,
            r => r.StartClicked += HandleStartClicked,
            r => r.StartClicked -= HandleStartClicked);

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
            r => r.QuitClicked += HandleQuitClicked,
            r => r.QuitClicked -= HandleQuitClicked);
    }

    #region Handlers

    private void HandleContinueClicked()
    {
        _progressionLauncher.Resume();
    }

    private void HandleStartClicked()
    {
        _progressionLauncher.StartNewGame();
    }

    private void HandleLoadClicked()
    {
        OpenLoadMenu();
    }

    private void HandleAlbumClicked()
    {
        GoToAlbum();
    }

    private void HandleSettingsClicked()
    {
        Debug.Log("설정 화면.");
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}