using System.Collections.Generic;

public sealed class AlbumController
{
    private readonly VNAlbumDatabaseSO _catalog;
    private readonly AlbumUnlockService _unlocks;

    public AlbumController(
        VNAlbumDatabaseSO catalog,
        AlbumUnlockService unlocks)
    {
        _catalog = catalog;
        _unlocks = unlocks;
    }

    public IReadOnlyList<AlbumEntryViewModel> BuildEntries()
    {
        var result =
            new List<AlbumEntryViewModel>(
                _catalog.Items.Count);

        for (int i = 0; i < _catalog.Items.Count; i++)
        {
            VNAlbumItemSO item =
                _catalog.Items[i];

            if (item == null)
                continue;

            result.Add(
                CreateEntry(item));
        }

        return result;
    }

    private AlbumEntryViewModel CreateEntry(
        VNAlbumItemSO item)
    {
        bool unlocked =
            _unlocks.IsUnlocked(item.Key);

        return new AlbumEntryViewModel
        {
            Id = item.Key,
            Title = item.Title,

            Thumbnail = item.Thumbnail,
            FullImage = item.CgSprite,

            IsUnlocked = unlocked,
        };
    }
}