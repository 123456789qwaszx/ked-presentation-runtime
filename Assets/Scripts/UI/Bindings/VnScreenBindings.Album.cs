public sealed partial class VNScreenBindings
{
    private void GoToAlbum()
    {
        UI.SwitchRoot<AlbumUIRoot>(
            afterPatched: root =>
            {
                BindView(root, ApplyBindings);

                RefreshAlbum(root);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(
        AlbumUIRoot root)
    {
        AddBinding(
            root,
            r => r.BackClicked += HandleAlbumBackClicked,
            r => r.BackClicked -= HandleAlbumBackClicked);
    }

    private void RefreshAlbum(
        AlbumUIRoot root)
    {
        if (_albumController == null)
        {
            root.Present(
                System.Array.Empty<AlbumEntryViewModel>());

            return;
        }

        root.Present(
            _albumController.BuildEntries());
    }

    #region Handlers

    private void HandleAlbumBackClicked()
    {
        GoToTitle();
    }

    #endregion
}