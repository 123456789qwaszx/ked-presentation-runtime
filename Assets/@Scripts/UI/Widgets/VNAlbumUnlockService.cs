using System.Collections.Generic;
using UnityEngine;

public sealed class VNAlbumUnlockService
{
    private readonly VNGlobalProgressData _globalData;
    private readonly JsonVNGlobalProgressRepository _globalRepo;
    private readonly VNAlbumDatabaseSO _vnAlbumData;

    private readonly HashSet<string> _unlockedCgSet;
    private readonly HashSet<string> _unlockedEndingSet;

    public VNAlbumUnlockService(
        VNGlobalProgressData globalData,
        JsonVNGlobalProgressRepository globalRepo,
        VNAlbumDatabaseSO database)
    {
        _globalData = globalData;
        _globalRepo = globalRepo;
        _vnAlbumData = database;
        
        _unlockedCgSet = new HashSet<string>(_globalData.unlockedCgKeys);
        _unlockedEndingSet = new HashSet<string>(_globalData.unlockedEndingKeys);
    }

    public IReadOnlyList<VNAlbumItemSO> GetAllItems() => _vnAlbumData.Items;
    
    public bool IsUnlocked(string key) => _unlockedCgSet.Contains(key);
    public bool IsEndingUnlocked(string key) => _unlockedEndingSet.Contains(key);

    public bool Unlock(string key)
    {
        if (_vnAlbumData.FindByKey(key) == null)
            Debug.LogWarning($"[VNAlbumUnlockService] Key '{key}' not found in album database. Unlocking anyway.");

        if (!_unlockedCgSet.Add(key))
            return false;

        _globalData.unlockedCgKeys.Add(key);
        
        return _globalRepo.Save(_globalData);
    }
    
    public bool UnlockEnding(string key)
    {
        if (!_unlockedEndingSet.Add(key))
            return false;

        _globalData.unlockedEndingKeys.Add(key);
        
        return _globalRepo.Save(_globalData);
    }

    public List<VNAlbumItemSO> GetUnlockedItems()
    {
        var result = new List<VNAlbumItemSO>();

        for (int i = 0; i < _vnAlbumData.Items.Count; i++)
        {
            VNAlbumItemSO item = _vnAlbumData.Items[i];
            if(item == null) 
                continue;

            if (_unlockedCgSet.Contains(item.key))
                result.Add(item);
        }

        return result;
    }
    
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public bool LockCgForDebug(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        bool removedFromSet = _unlockedCgSet.Remove(key);
        int removedFromList = _globalData.unlockedCgKeys.RemoveAll(k => k == key);

        if (!removedFromSet && removedFromList <= 0)
            return false;

        _globalRepo.Save(_globalData);

        Debug.Log($"[VNAlbumUnlockService] Locked CG for debug: '{key}'");
        return true;
    }

    public void ClearAllCgUnlocksForDebug()
    {
        _unlockedCgSet.Clear();
        _globalData.unlockedCgKeys.Clear();

        _globalRepo.Save(_globalData);

        Debug.Log("[VNAlbumUnlockService] Cleared all CG unlocks for debug.");
    }
#endif
}