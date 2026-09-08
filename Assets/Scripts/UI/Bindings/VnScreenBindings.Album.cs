public sealed partial class VNScreenBindings
{
    private AlbumController _albumController;

    public void ConfigureAlbum(
        AlbumController albumController)
    {
        _albumController = albumController;
    }

    private void GoToAlbum()
    {
        UI.SwitchRoot<AlbumUIRoot>(root =>
        {
            BindMain(root, ApplyBindings);

            RefreshAlbum(root);
        });
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