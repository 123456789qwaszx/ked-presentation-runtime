using System;
using UnityEngine;

// 로컬 저장소의 파일 정리. 회차 진행과 무관하므로 저장 세션을 알지 않는다.
public sealed class LocalSaveMaintenance
{
    private readonly ILocalSaveStore _store;

    private float _nextMaintenance;

    public LocalSaveMaintenance(ILocalSaveStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    // 로컬에서 참조가 끝난 회차만 정리한다. 네트워크 상태와 무관하다.
    public void Tick(float realtime)
    {
        if (realtime < _nextMaintenance)
            return;

        _nextMaintenance = realtime + 5;

        try
        {
            _store.CollectUnusedPlaythroughs();
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[저장] 로컬 정리 보류: {error.Message}");
        }
    }
}
