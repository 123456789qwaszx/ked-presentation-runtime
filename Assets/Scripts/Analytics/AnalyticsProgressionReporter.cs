using System;
using UnityEngine;

// 저장은 SaveCoordinator가 끝낸다. 관찰자 실패는 성공한 저장을 되돌리지 않는다.
public sealed class AnalyticsProgressionReporter : IProgressionReporter
{
    private readonly SaveCoordinator _save;
    private readonly ILocalSaveStore _store;
    private readonly Action<LocalSaveFile> _observe;

    public AnalyticsProgressionReporter(SaveCoordinator save, ILocalSaveStore store,
        Action<LocalSaveFile> observe)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _observe = observe ?? throw new ArgumentNullException(nameof(observe));
    }

    public void ReportSceneEntered(SceneEntryReport report)
    {
        _save.ReportSceneEntered(report);
        Observe(); // 이어하기 및 사용자 fork에 상속된 확정 경로도 전달한다.
    }

    public void ReportSceneCommitted(SceneCommitReport report)
    {
        _save.ReportSceneCommitted(report);
        Observe();
    }

    private void Observe()
    {
        try
        {
            LocalSaveFile snapshot = _store.LoadPlaythrough(_save.PlaythroughId);
            if (snapshot != null) _observe(snapshot);
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[통계] 로컬 저장 성공, 통계 관찰 보류: {error.Message}");
        }
    }
}
