// using System;
// using System.IO;
// using UnityEngine;
//
// public sealed class JsonVNSaveRepository
// {
//     private const string SlotPrefix = "slot_";
//     private const string AutoSlotId = "auto";
//     private const string SaveSubDir = "saves";
//     private const string SlotIdFormatSample = "slot_001";
//
//     private readonly string _saveDir;
//     private readonly int _slotCount;
//
//     public int SlotCount => _slotCount;
//     public string AutoSlot => AutoSlotId;
//
//     public JsonVNSaveRepository(int slotCount = 10)
//     {
//         _slotCount = Mathf.Max(1, slotCount);
//         _saveDir = Path.Combine(Application.persistentDataPath, SaveSubDir);
//
//         EnsureSaveDirectory();
//     }
//
//     // Slot Id
//     public string GetSlotId(int slotIndex)
//     {
//         int safeIndex = Mathf.Clamp(slotIndex, 1, _slotCount);
//         return $"{SlotPrefix}{safeIndex:D3}";
//     }
//
//     // Save / Load
//     public bool Save(VNSaveData data)
//     {
//         if (!IsValidSlotId(data.slotId))
//             return false;
//
//         EnsureSaveDirectory();
//
//         string json = JsonUtility.ToJson(data, prettyPrint: true);
//         string path = GetPath(data.slotId);
//
//         WriteTextWithBackup(path, json);
//         return true;
//     }
//
//     public bool TryLoad(string slotId, out VNSaveData data)
//     {
//         data = null;
//
//         if (!IsValidSlotId(slotId))
//             return false;
//
//         string path = GetPath(slotId);
//
//         if (TryLoadFromPath(path, out data))
//             return true;
//
//         return TryLoadBackup(path, out data);
//     }
//
//     public bool Delete(string slotId)
//     {
//         if (!IsValidSlotId(slotId))
//             return false;
//
//         string path = GetPath(slotId);
//         string backupPath = GetBackupPath(path);
//
//         bool deleted = false;
//
//         if (File.Exists(path))
//         {
//             File.Delete(path);
//             deleted = true;
//         }
//
//         if (File.Exists(backupPath))
//             File.Delete(backupPath);
//
//         return deleted;
//     }
//
//     public bool Exists(string slotId)
//     {
//         if (!IsValidSlotId(slotId))
//             return false;
//
//         string path = GetPath(slotId);
//         return File.Exists(path) || File.Exists(GetBackupPath(path));
//     }
//
//     // Metadata
//     public VNSaveSlotMeta GetMeta(int slotIndex)
//     {
//         return GetMeta(GetSlotId(slotIndex));
//     }
//
//     public VNSaveSlotMeta GetMeta(string slotId)
//     {
//         if (!TryLoad(slotId, out VNSaveData data))
//             return VNSaveSlotMeta.Empty(slotId);
//
//         return VNSaveSlotMeta.FromSaveData(data);
//     }
//
//     public VNSaveSlotMeta[] GetAllMetas()
//     {
//         var result = new VNSaveSlotMeta[_slotCount];
//
//         for (int i = 0; i < _slotCount; i++)
//             result[i] = GetMeta(i + 1);
//
//         return result;
//     }
//
//     // Load Internals
//     private bool TryLoadBackup(string mainPath, out VNSaveData data)
//     {
//         string backupPath = GetBackupPath(mainPath);
//         return TryLoadFromPath(backupPath, out data);
//     }
//
//     private bool TryLoadFromPath(string path, out VNSaveData data)
//     {
//         data = null;
//
//         if (!File.Exists(path))
//             return false;
//
//         try
//         {
//             string json = File.ReadAllText(path);
//             data = JsonUtility.FromJson<VNSaveData>(json);
//
//             if (data == null)
//                 return false;
//
//             data.Normalize();
//             return true;
//         }
//         catch (Exception e)
//         {
//             Debug.LogWarning($"[JsonVNSaveRepository] Load failed. path='{path}', error='{e.Message}'");
//             data = null;
//             return false;
//         }
//     }
//
//     // Save Internals
//     private void WriteTextWithBackup(string path, string text)
//     {
//         string tempPath = path + ".tmp";
//         string backupPath = GetBackupPath(path);
//
//         File.WriteAllText(tempPath, text);
//
//         if (File.Exists(path))
//             File.Copy(path, backupPath, true);
//
//         File.Copy(tempPath, path, true);
//         File.Delete(tempPath);
//     }
//
//     // Path
//     private void EnsureSaveDirectory()
//     {
//         if (!Directory.Exists(_saveDir))
//             Directory.CreateDirectory(_saveDir);
//     }
//
//     private string GetPath(string slotId)
//     {
//         return Path.Combine(_saveDir, $"{slotId}.json");
//     }
//
//     private string GetBackupPath(string mainPath)
//     {
//         return mainPath + ".bak";
//     }
//
//     // Validation
//     private bool IsValidSlotId(string slotId)
//     {
//         if (string.IsNullOrWhiteSpace(slotId))
//             return false;
//
//         if (slotId == AutoSlotId)
//             return true;
//
//         if (!slotId.StartsWith(SlotPrefix))
//             return false;
//
//         if (slotId.Length != SlotIdFormatSample.Length)
//             return false;
//
//         string numberText = slotId.Substring(SlotPrefix.Length);
//
//         if (!int.TryParse(numberText, out int slotNumber))
//             return false;
//
//         return slotNumber >= 1 && slotNumber <= _slotCount;
//     }
// }