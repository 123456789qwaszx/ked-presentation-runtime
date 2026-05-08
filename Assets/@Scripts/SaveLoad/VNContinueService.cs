using UnityEngine;

public sealed class VNContinueService
{
    private readonly VNGlobalProgressData _globalData;
    private readonly VNLoadService _loadService;
    private readonly IVNSaveRepository _saveRepo;

    public VNContinueService(
        VNGlobalProgressData globalData,
        VNLoadService loadService,
        IVNSaveRepository saveRepo)
    {
        _globalData = globalData;
        _loadService = loadService;
        _saveRepo = saveRepo;
    }

    public bool CanContinue()
    {
        if (_globalData == null)
            return false;

        _globalData.Normalize();

        if (string.IsNullOrWhiteSpace(_globalData.continueSlotId))
            return false;

        return _saveRepo.Exists(_globalData.continueSlotId);
    }

    public bool Continue()
    {
        if (!CanContinue())
        {
            Debug.LogWarning("[VNContinueService] No continue target available.");
            return false;
        }

        Debug.Log($"[VNContinueService] Continue → slot='{_globalData.continueSlotId}'");
        return _loadService.Load(_globalData.continueSlotId);
    }
}