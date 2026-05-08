#if VN_USE_ES3
using UnityEngine;

public sealed class EasySave3VNSaveRepository : IVNSaveRepository
{
    private const string SlotPrefix = "slot_";
    private const string AutoSlotId = "auto";

    private readonly int _slotCount;

    public int SlotCount => _slotCount;
    public string AutoSlot => AutoSlotId;

    public EasySave3VNSaveRepository(int slotCount = 10)
    {
        _slotCount = Mathf.Max(1, slotCount);
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
            Debug.LogWarning($"[EasySave3VNSaveRepository] Invalid slotId: '{slotId}'");
            return false;
        }

        string filePath = GetFilePath(slotId);

        if (!ES3.FileExists(filePath))
            return false;

        try
        {
            data = ES3.Load<VNSaveData>("data", filePath);
            if (data == null)
                return false;

            data.Normalize();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EasySave3VNSaveRepository] Load failed. slot='{slotId}', error='{e.Message}'");
            return false;
        }
    }

    public bool Save(VNSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[EasySave3VNSaveRepository] Cannot save null data.");
            return false;
        }

        data.Normalize();

        if (!IsValidSlotId(data.slotId))
        {
            Debug.LogError($"[EasySave3VNSaveRepository] Invalid slotId: '{data.slotId}'");
            return false;
        }

        try
        {
            ES3.Save("data", data, GetFilePath(data.slotId));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EasySave3VNSaveRepository] Save failed. slot='{data.slotId}', error='{e.Message}'");
            return false;
        }
    }

    public bool Delete(string slotId)
    {
        if (!IsValidSlotId(slotId))
            return false;

        string filePath = GetFilePath(slotId);

        if (!ES3.FileExists(filePath))
            return false;

        try
        {
            ES3.DeleteFile(filePath);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EasySave3VNSaveRepository] Delete failed. slot='{slotId}', error='{e.Message}'");
            return false;
        }
    }

    public bool Exists(string slotId)
    {
        if (!IsValidSlotId(slotId))
            return false;

        return ES3.FileExists(GetFilePath(slotId));
    }

    public VNSaveSlotMeta GetMeta(int slotIndex)
    {
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

    private string GetFilePath(string slotId)
    {
        return $"saves/{slotId}.es3";
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
#endif