using UnityEngine;

public sealed class VNAutoSaveService
{
    private readonly VNSaveService _saveService;

    public float MinIntervalSeconds = 60f;
    public bool IsEnabled = true;

    private float _lastAutoSaveRealtime = -9999f;

    public VNAutoSaveService(VNSaveService saveService)
    {
        _saveService = saveService;
    }

    public void TryAutoSave()
    {
        if (!IsEnabled)
            return;

        float now = Time.realtimeSinceStartup;

        if (now - _lastAutoSaveRealtime < MinIntervalSeconds)
            return;

        if (_saveService.SaveAuto())
            _lastAutoSaveRealtime = now;
    }

    public void ForceAutoSave()
    {
        if (!IsEnabled)
            return;

        if (_saveService.SaveAuto())
            _lastAutoSaveRealtime = Time.realtimeSinceStartup;
    }

    public void ResetInterval()
    {
        _lastAutoSaveRealtime = -9999f;
    }
}