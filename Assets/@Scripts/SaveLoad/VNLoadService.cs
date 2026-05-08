using UnityEngine;

public sealed class VNLoadService
{
    private readonly IVNSaveRepository _saveRepo;
    private readonly IVNLoadSeekDriver _seekDriver;
    private readonly IVNFlagStore _flagStore;
    private readonly IVNSaveSafetyPolicy _safetyPolicy;

    private bool _isLoading;

    public bool IsLoading => _isLoading;

    public VNLoadService(
        IVNSaveRepository saveRepo,
        IVNLoadSeekDriver seekDriver,
        IVNFlagStore flagStore,
        IVNSaveSafetyPolicy safetyPolicy)
    {
        _saveRepo = saveRepo;
        _seekDriver = seekDriver;
        _flagStore = flagStore;
        _safetyPolicy = safetyPolicy;
    }

    public bool Load(int slotIndex)
    {
        return Load(_saveRepo.GetSlotId(slotIndex));
    }

    public bool Load(string slotId)
    {
        if (_isLoading)
        {
            Debug.LogWarning("[VNLoadService] Already loading. Request ignored.");
            return false;
        }

        if (_safetyPolicy != null && !_safetyPolicy.CanLoadNow(out string reason))
        {
            Debug.LogWarning($"[VNLoadService] Load blocked. reason='{reason}'");
            return false;
        }

        if (!_saveRepo.TryLoad(slotId, out VNSaveData data))
        {
            Debug.LogWarning($"[VNLoadService] No save data for slot '{slotId}'.");
            return false;
        }

        data.Normalize();

        if (!data.HasValidTarget())
        {
            Debug.LogWarning($"[VNLoadService] Save data has no nodeName. slot='{slotId}'");
            return false;
        }

        BeginLoad(data);
        return true;
    }

    private void BeginLoad(VNSaveData data)
    {
        _isLoading = true;

        Debug.Log($"[VNLoadService] Begin load. slot='{data.slotId}', node='{data.nodeName}', line='{data.lineId}', visitedIndex={data.visitedIndex}");

        try
        {
            _seekDriver.PrepareForLoad();

            // 핵심:
            // Yarn/CPS seek를 시작하기 전에 저장 당시 flags를 먼저 복원한다.
            // 이 순서가 틀리면 분기 있는 VN에서 다른 경로로 seek될 수 있다.
            _flagStore.Restore(data.flags);

            _seekDriver.BeginSeek(
                data,
                onComplete: () => OnSeekComplete(data),
                onFail: () => OnSeekFailed(data));
        }
        catch (System.Exception e)
        {
            _isLoading = false;
            Debug.LogError($"[VNLoadService] BeginLoad exception. slot='{data.slotId}', error='{e.Message}'");
        }
    }

    private void OnSeekComplete(VNSaveData data)
    {
        _isLoading = false;

        try
        {
            _seekDriver.OnLoadComplete(data);
            Debug.Log($"[VNLoadService] Load complete. slot='{data.slotId}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VNLoadService] OnLoadComplete exception. slot='{data.slotId}', error='{e.Message}'");
        }
    }

    private void OnSeekFailed(VNSaveData data)
    {
        _isLoading = false;
        Debug.LogError($"[VNLoadService] Load failed. slot='{data.slotId}', node='{data.nodeName}', line='{data.lineId}'");
    }
}