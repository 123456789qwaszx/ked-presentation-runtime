using System.Collections.Generic;
using UnityEngine;

public interface IAlbumUnlockPort
{
    bool Unlock(string albumId);
}

public sealed class AlbumUnlockService
    : IAlbumUnlockPort
{
    private readonly VNAlbumDatabaseSO _catalog;
    private readonly IAlbumProgressStore _store;

    private readonly AlbumProgress _progress;
    private readonly HashSet<string> _unlocked;

    public AlbumUnlockService(
        VNAlbumDatabaseSO catalog,
        IAlbumProgressStore store)
    {
        _catalog = catalog;
        _store = store;

        _progress =
            _store.Load()
            ?? new AlbumProgress();

        _progress.UnlockedIds ??=
            new List<string>();

        _unlocked =
            new HashSet<string>(
                _progress.UnlockedIds);
    }

    public bool IsUnlocked(string albumId)
    {
        if (string.IsNullOrWhiteSpace(albumId))
            return false;

        return _unlocked.Contains(albumId);
    }

    public bool Unlock(string albumId)
    {
        if (string.IsNullOrWhiteSpace(albumId))
            return false;

        if (!_catalog.TryGet(
                albumId,
                out _))
        {
            Debug.LogWarning(
                $"[앨범] 등록되지 않은 CG는 해금할 수 없다. id={albumId}");

            return false;
        }

        // 이미 해금된 항목.
        if (!_unlocked.Add(albumId))
            return false;

        _progress.UnlockedIds.Add(albumId);

        _store.Save(_progress);

        Debug.Log(
            $"[앨범] CG 해금 - {albumId}");

        return true;
    }
}