using System;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    private float _nextMaintenance;

    // 로컬에서 참조가 끝난 회차만 정리한다. 네트워크 상태와 무관하다.
    public void TickMaintenance(float realtime)
    {
        if (realtime < _nextMaintenance) return;
        _nextMaintenance = realtime + 5;
        try { _localStore.CollectUnusedPlaythroughs(); }
        catch (Exception error)
        {
            Debug.LogWarning($"[저장] 로컬 정리 보류: {error.Message}");
        }
    }
}
