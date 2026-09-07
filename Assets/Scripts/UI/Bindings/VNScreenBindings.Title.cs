using System.Threading.Tasks;
using UnityEngine;

public sealed partial class VNScreenBindings
{
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(root =>
        {
            BindMain(root, ApplyBindings);
        });
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

    private async void HandleContinueClicked()
    {
        if (_progressionLauncher == null)
            return;

        await _progressionLauncher.ResumeAfterAsync(
            _saveCoordinator?.WaitForStartupSyncAsync() ?? Task.CompletedTask);
    }

    private async void HandleStartClicked()
    {
        if (_progressionLauncher == null ||
            _saveCoordinator == null)
        {
            return;
        }

        await _progressionLauncher.TransitionAsync(
            _saveCoordinator.PrepareNewPlaythroughAsync);
    }

    private void HandleLoadClicked()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void HandleAlbumClicked()
    {
        Debug.Log("앨범 화면.");
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