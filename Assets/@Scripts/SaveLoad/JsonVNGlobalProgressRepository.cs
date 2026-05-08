using System;
using System.IO;
using UnityEngine;

public sealed class JsonVNGlobalProgressRepository : IVNGlobalProgressRepository
{
    private const string FileName = "global.json";

    private readonly string _filePath;

    public JsonVNGlobalProgressRepository()
    {
        _filePath = Path.Combine(Application.persistentDataPath, FileName);
    }

    public VNGlobalProgressData LoadOrCreate()
    {
        if (!File.Exists(_filePath))
        {
            var created = new VNGlobalProgressData();
            created.Normalize();
            Save(created);
            return created;
        }

        if (TryLoadFromPath(_filePath, out VNGlobalProgressData data))
            return data;

        string backupPath = GetBackupPath();

        if (File.Exists(backupPath) && TryLoadFromPath(backupPath, out data))
        {
            Debug.LogWarning("[JsonVNGlobalProgressRepository] Loaded global data from backup.");
            return data;
        }

        Debug.LogWarning("[JsonVNGlobalProgressRepository] Failed to load global data. Using default.");
        data = new VNGlobalProgressData();
        data.Normalize();
        return data;
    }

    public bool Save(VNGlobalProgressData data)
    {
        if (data == null)
        {
            Debug.LogError("[JsonVNGlobalProgressRepository] Cannot save null global data.");
            return false;
        }

        try
        {
            data.Normalize();

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            WriteTextWithBackup(_filePath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonVNGlobalProgressRepository] Save failed: {e.Message}");
            return false;
        }
    }

    private bool TryLoadFromPath(string path, out VNGlobalProgressData data)
    {
        data = null;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<VNGlobalProgressData>(json);

            if (data == null)
                return false;

            data.Normalize();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JsonVNGlobalProgressRepository] Load failed. path='{path}', error='{e.Message}'");
            return false;
        }
    }

    private void WriteTextWithBackup(string path, string text)
    {
        string tempPath = path + ".tmp";
        string backupPath = GetBackupPath();

        File.WriteAllText(tempPath, text);

        if (File.Exists(path))
            File.Copy(path, backupPath, true);

        File.Copy(tempPath, path, true);
        File.Delete(tempPath);
    }

    private string GetBackupPath()
    {
        return _filePath + ".bak";
    }
}