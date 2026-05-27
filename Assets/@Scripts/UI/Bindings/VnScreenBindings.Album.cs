using UnityEngine;

public sealed partial class VnScreenBindings
{
    public void GoToAlbum()
    {
        UI.SwitchRoot<AlbumUIRoot>(root =>
        {
            BindMain(root, BindAlbumRoot);
        });
    }

    private void BindAlbumRoot(AlbumUIRoot albumRoot)
    {
        BindEvent(
            albumRoot,
            a => a.OnCloseRequested += OnAlbumCloseRequested,
            a => a.OnCloseRequested -= OnAlbumCloseRequested);

        RefreshAlbumRoot(albumRoot);
    }

    private void RefreshAlbumRoot(AlbumUIRoot albumRoot)
    {
        if (albumRoot == null)
            return;

        if (_vnSaveLoadSystem == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNSaveLoadSystem is null.");
            return;
        }

        VNAlbumUnlockService albumService = _vnSaveLoadSystem.AlbumService;

        if (albumService == null)
        {
            Debug.LogWarning("[VnScreenBindings] AlbumService is null.");
            return;
        }

        albumRoot.Rebuild(
            albumService.GetAllItems(),
            albumService.IsUnlocked);
    }

    private void OnAlbumCloseRequested()
    {
        GoToTitle();
    }
}