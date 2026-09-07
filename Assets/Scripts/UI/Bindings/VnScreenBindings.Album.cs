using System;

public sealed partial class VNScreenBindings
{
    private void GoToAlbum()
    {
        UI.SwitchRoot<AlbumUIRoot>(root =>
        {
            BindMain(root, ApplyBindings);

            // 실제 Album 시스템은 다음 단계에서 연결한다.
            root.Present(
                Array.Empty<AlbumEntryViewModel>());
        });
    }

    private void ApplyBindings(AlbumUIRoot root)
    {
        AddBinding(
            root,
            r => r.BackClicked += HandleAlbumBackClicked,
            r => r.BackClicked -= HandleAlbumBackClicked);
    }

    #region Handlers

    private void HandleAlbumBackClicked()
    {
        GoToTitle();
    }

    #endregion
}