using System.Collections.Generic;
using UnityEngine;

public sealed class VNAlbumUnlockService
{
    private readonly VNGlobalProgressData _globalData;
    private readonly IVNGlobalProgressRepository _globalRepo;
    private readonly VNAlbumDatabaseSO _database;

    private HashSet<string> _unlockedCgSet;
    private HashSet<string> _unlockedEndingSet;

    public VNAlbumUnlockService(
        VNGlobalProgressData globalData,
        IVNGlobalProgressRepository globalRepo,
        VNAlbumDatabaseSO database)
    {
        _globalData = globalData;
        _globalRepo = globalRepo;
        _database = database;

        _globalData.Normalize();
        RebuildCache();
    }

    public bool IsUnlocked(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _unlockedCgSet.Contains(key);
    }

    public bool Unlock(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[VNAlbumUnlockService] Unlock called with empty key.");
            return false;
        }

        if (_database != null && _database.FindByKey(key) == null)
            Debug.LogWarning($"[VNAlbumUnlockService] Key '{key}' not found in album database. Unlocking anyway.");

        if (_unlockedCgSet.Contains(key))
            return false;

        _unlockedCgSet.Add(key);
        _globalData.unlockedCgKeys.Add(key);

        _globalRepo.Save(_globalData);

        Debug.Log($"[VNAlbumUnlockService] Unlocked CG: '{key}'");
        return true;
    }

    public bool IsEndingUnlocked(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _unlockedEndingSet.Contains(key);
    }

    public bool UnlockEnding(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_unlockedEndingSet.Contains(key))
            return false;

        _unlockedEndingSet.Add(key);
        _globalData.unlockedEndingKeys.Add(key);

        _globalRepo.Save(_globalData);

        Debug.Log($"[VNAlbumUnlockService] Unlocked ending: '{key}'");
        return true;
    }

    public int GetTotalCount()
    {
        return _database != null ? _database.Items.Count : 0;
    }

    public int GetUnlockedCount()
    {
        if (_database == null)
            return _globalData.unlockedCgKeys.Count;

        int count = 0;

        for (int i = 0; i < _database.Items.Count; i++)
        {
            VNAlbumItemSO item = _database.Items[i];

            if (item != null && _unlockedCgSet.Contains(item.key))
                count++;
        }

        return count;
    }

    public List<VNAlbumItemSO> GetUnlockedItems()
    {
        var result = new List<VNAlbumItemSO>();

        if (_database == null)
            return result;

        for (int i = 0; i < _database.Items.Count; i++)
        {
            VNAlbumItemSO item = _database.Items[i];

            if (item != null && _unlockedCgSet.Contains(item.key))
                result.Add(item);
        }

        return result;
    }

    public IReadOnlyList<VNAlbumItemSO> GetAllItems()
    {
        return _database != null ? _database.Items : new List<VNAlbumItemSO>();
    }

    public void RebuildCache()
    {
        _globalData.Normalize();

        _unlockedCgSet = new HashSet<string>(_globalData.unlockedCgKeys);
        _unlockedEndingSet = new HashSet<string>(_globalData.unlockedEndingKeys);
    }
}