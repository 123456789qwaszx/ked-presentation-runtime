#if VN_USE_ES3
using UnityEngine;

public sealed class EasySave3VNGlobalProgressRepository : IVNGlobalProgressRepository
{
    private const string FilePath = "global.es3";
    private const string Key = "global";

    public VNGlobalProgressData LoadOrCreate()
    {
        if (!ES3.FileExists(FilePath))
        {
            var created = new VNGlobalProgressData();
            created.Normalize();
            Save(created);
            return created;
        }

        try
        {
            VNGlobalProgressData data = ES3.Load<VNGlobalProgressData>(Key, FilePath);

            if (data == null)
                data = new VNGlobalProgressData();

            data.Normalize();
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EasySave3VNGlobalProgressRepository] Load failed: {e.Message}");
            var fallback = new VNGlobalProgressData();
            fallback.Normalize();
            return fallback;
        }
    }

    public bool Save(VNGlobalProgressData data)
    {
        if (data == null)
        {
            Debug.LogError("[EasySave3VNGlobalProgressRepository] Cannot save null data.");
            return false;
        }

        try
        {
            data.Normalize();
            ES3.Save(Key, data, FilePath);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EasySave3VNGlobalProgressRepository] Save failed: {e.Message}");
            return false;
        }
    }
}
#endif