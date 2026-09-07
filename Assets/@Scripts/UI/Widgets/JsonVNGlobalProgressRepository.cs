using System;
using System.IO;
using UnityEngine;

public sealed class JsonVNGlobalProgressRepository
{
    private const string FileName = "global.json";

    private readonly string _filePath;

    public JsonVNGlobalProgressRepository()
    {
        _filePath = Path.Combine(Application.persistentDataPath, FileName);
        // Debug.Log($"[GlobalProgress] filePath: {_filePath}");
    }

    public VNGlobalProgressData LoadOrCreate()
    {
        if (TryLoadFromPath(_filePath, out VNGlobalProgressData data))
            return data;

        if (TryLoadFromPath(GetBackupPath(), out data))
        {
            Debug.LogWarning("[JsonVNGlobalProgressRepository] Loaded global data from backup.");
            return data;
        }

        Debug.LogWarning("[JsonVNGlobalProgressRepository] Failed to load global data. Using default.");

        data = new VNGlobalProgressData();
        data.Normalize();
        Save(data);
        return data;
    }

    public bool Save(VNGlobalProgressData data)
    {
        if (data == null)
            return false;

        data.Normalize();

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            WriteTextWithBackup(_filePath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JsonVNGlobalProgressRepository] Save failed. path='{_filePath}', error='{e.Message}'");
            return false;
        }
    }

    private bool TryLoadFromPath(string path, out VNGlobalProgressData data)
    {
        data = null;

        if (!File.Exists(path))
            return false;

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
            data = null;
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

        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }

    private string GetBackupPath()
    {
        return _filePath + ".bak";
    }
}