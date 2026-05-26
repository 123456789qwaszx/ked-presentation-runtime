using System;
using UnityEngine;

public interface IVNLoadSeekDriver
{
    void PrepareForLoad();
    void BeginSeek(VNSaveData saveData, Action onComplete, Action onFail);
    void OnLoadComplete(VNSaveData saveData);
}
public sealed class VNLoadService
{
    private readonly IVNSaveRepository _saveRepo;
    private readonly IVNLoadSeekDriver _seekDriver;
    private readonly IVNFlagStore _flagStore;
    private readonly IVNSaveSafetyPolicy _safetyPolicy;
    private readonly VNTraceStream _trace;

    private bool _isLoading;
    
    public bool IsLoading => _isLoading;

    public VNLoadService(
        IVNSaveRepository saveRepo,
        IVNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy,
        VNTraceStream trace = null)
    {
        _saveRepo = saveRepo;
        _seekDriver = seekDriver;
        _flagStore = flagStore;
        _safetyPolicy = safetyPolicy;
        _trace = trace;
    }

    public bool Load(int slotIndex)
    {
        Trace("LoadBySlotIndex", $"slotIndex={slotIndex}");
        return Load(_saveRepo.GetSlotId(slotIndex));
    }

    public bool Load(string slotId)
    {
        if (_isLoading)
        {
            Debug.LogWarning("[VNLoadService] Already loading. Request ignored.");
            Trace("LoadRejected", "already loading");
            return false;
        }

        if (_safetyPolicy != null && !_safetyPolicy.CanLoadNow(out string reason))
        {
            Debug.LogWarning($"[VNLoadService] Load blocked. reason='{reason}'");
            Trace("LoadRejected", $"safetyPolicy={reason}");
            return false;
        }

        if (!_saveRepo.TryLoad(slotId, out VNSaveData data))
        {
            Debug.LogWarning($"[VNLoadService] No save data for slot '{slotId}'.");
            Trace("LoadRejected", $"no save data slotId={slotId}");
            return false;
        }

        data.Normalize();

        if (!data.HasValidTarget())
        {
            Debug.LogWarning($"[VNLoadService] Save data has no nodeName. slot='{slotId}'");
            Trace("LoadRejected", $"invalid target slotId={slotId}");
            return false;
        }

        BeginLoad(data);
        return true;
    }

    private void BeginLoad(VNSaveData data)
    {
        _isLoading = true;

        Trace("BeginLoad", $"slot={data.slotId}, target={data.nodeName}/{data.lineId}");

        try
        {
            _seekDriver.PrepareForLoad();
            
            _flagStore.Restore(data.flags);
            
            _seekDriver.BeginSeek(
                data,
                onComplete: () => OnSeekComplete(data),
                onFail: () => OnSeekFailed(data));
        }
        catch (Exception e)
        {
            _isLoading = false;
            Debug.LogError($"[VNLoadService] BeginLoad exception. slot='{data.slotId}', error='{e.Message}'");
            Trace("BeginLoadException", e.Message);
        }
    }

    private void OnSeekComplete(VNSaveData data)
    {
        Trace("OnSeekComplete", $"slot={data.slotId}, target={data.nodeName}/{data.lineId}");

        _isLoading = false;

        try
        {
            _seekDriver.OnLoadComplete(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VNLoadService] OnLoadComplete exception. slot='{data.slotId}', error='{e.Message}'");
            Trace("OnLoadCompleteException", e.Message);
        }
    }

    private void OnSeekFailed(VNSaveData data)
    {
        _isLoading = false;
        Debug.LogError($"[VNLoadService] Load failed. slot='{data.slotId}', node='{data.nodeName}', line='{data.lineId}'");
        Trace("OnSeekFailed", $"slot={data.slotId}, target={data.nodeName}/{data.lineId}");
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(VNLoadService), evt, $"isLoading={_isLoading}", note);
    }
}