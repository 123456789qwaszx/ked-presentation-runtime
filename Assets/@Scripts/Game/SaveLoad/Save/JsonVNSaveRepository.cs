using System;
using System.IO;
using UnityEngine;

public sealed class JsonVNSaveRepository : IVNSaveRepository
{
    private const string SlotPrefix = "slot_";
    private const string AutoSlotId = "auto";
    private const string SaveSubDir = "saves";

    private readonly string _saveDir;
    private readonly int _slotCount;

    public int SlotCount => _slotCount;
    public string AutoSlot => AutoSlotId;

    public JsonVNSaveRepository(int slotCount = 10)
    {
        _slotCount = Mathf.Max(1, slotCount);
        _saveDir = Path.Combine(Application.persistentDataPath, SaveSubDir);

        EnsureDirectory();
    }

    public string GetSlotId(int slotIndex)
    {
        int safeIndex = Mathf.Clamp(slotIndex, 1, _slotCount);
        return $"{SlotPrefix}{safeIndex:D3}";
    }

    public bool TryLoad(string slotId, out VNSaveData data)
    {
        data = null;

        if (!IsValidSlotId(slotId))
        {
            Debug.LogWarning($"[JsonVNSaveRepository] Invalid slotId: '{slotId}'");
            return false;
        }

        string path = GetPath(slotId);

        if (!File.Exists(path))
            return TryLoadBackup(path, out data);

        if (TryLoadFromPath(path, out data))
            return true;

        Debug.LogWarning($"[JsonVNSaveRepository] Main save failed. Trying backup. slot='{slotId}'");
        return TryLoadBackup(path, out data);
    }

    public bool Save(VNSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[JsonVNSaveRepository] Cannot save null VNSaveData.");
            return false;
        }

        data.Normalize();

        if (!IsValidSlotId(data.slotId))
        {
            Debug.LogError($"[JsonVNSaveRepository] Invalid slotId: '{data.slotId}'");
            return false;
        }

        try
        {
            EnsureDirectory();

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            string path = GetPath(data.slotId);

            WriteTextWithBackup(path, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonVNSaveRepository] Save failed. slot='{data.slotId}', error='{e.Message}'");
            return false;
        }
    }

    public bool Delete(string slotId)
    {
        if (!IsValidSlotId(slotId))
        {
            Debug.LogWarning($"[JsonVNSaveRepository] Invalid slotId: '{slotId}'");
            return false;
        }

        string path = GetPath(slotId);
        string backupPath = GetBackupPath(path);

        bool deleted = false;

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                deleted = true;
            }

            if (File.Exists(backupPath))
                File.Delete(backupPath);

            return deleted;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonVNSaveRepository] Delete failed. slot='{slotId}', error='{e.Message}'");
            return false;
        }
    }

    public bool Exists(string slotId)
    {
        if (!IsValidSlotId(slotId))
            return false;

        string path = GetPath(slotId);
        return File.Exists(path) || File.Exists(GetBackupPath(path));
    }

    public VNSaveSlotMeta GetMeta(int slotIndex)
    {
        //Debug.Log($"{GetSlotId(slotIndex)}");
        return GetMeta(GetSlotId(slotIndex));
    }

    public VNSaveSlotMeta GetMeta(string slotId)
    {
        if (!TryLoad(slotId, out VNSaveData data))
            return VNSaveSlotMeta.Empty(slotId);

        return VNSaveSlotMeta.FromSaveData(data);
    }

    public VNSaveSlotMeta[] GetAllMetas()
    {
        var result = new VNSaveSlotMeta[_slotCount];

        for (int i = 0; i < _slotCount; i++)
            result[i] = GetMeta(i + 1);

        return result;
    }

    private bool TryLoadBackup(string mainPath, out VNSaveData data)
    {
        string backupPath = GetBackupPath(mainPath);

        if (!File.Exists(backupPath))
        {
            data = null;
            return false;
        }

        return TryLoadFromPath(backupPath, out data);
    }

    private bool TryLoadFromPath(string path, out VNSaveData data)
    {
        data = null;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<VNSaveData>(json);

            if (data == null)
                return false;

            data.Normalize();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JsonVNSaveRepository] Load failed. path='{path}', error='{e.Message}'");
            return false;
        }
    }

    private void WriteTextWithBackup(string path, string text)
    {
        string tempPath = path + ".tmp";
        string backupPath = GetBackupPath(path);

        File.WriteAllText(tempPath, text);

        if (File.Exists(path))
            File.Copy(path, backupPath, true);

        File.Copy(tempPath, path, true);
        File.Delete(tempPath);
    }

    private void EnsureDirectory()
    {
        if (!Directory.Exists(_saveDir))
            Directory.CreateDirectory(_saveDir);
    }

    private string GetPath(string slotId)
    {
        return Path.Combine(_saveDir, $"{slotId}.json");
    }

    private string GetBackupPath(string mainPath)
    {
        return mainPath + ".bak";
    }

    private bool IsValidSlotId(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return false;

        if (slotId == AutoSlotId)
            return true;

        if (!slotId.StartsWith(SlotPrefix))
            return false;

        if (slotId.Length != "slot_001".Length)
            return false;

        for (int i = SlotPrefix.Length; i < slotId.Length; i++)
        {
            if (!char.IsDigit(slotId[i]))
                return false;
        }

        return true;
    }
}