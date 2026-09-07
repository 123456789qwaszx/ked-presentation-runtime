using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    private Task _startupRestore = Task.CompletedTask;
    private bool _startupStarted;

    // 이어하기만 초기 복구를 기다린다. 기존 로컬이 있으면 즉시 완료된다.
    public Task WaitForStartupSyncAsync() => _startupRestore;

    public Task SyncPendingAsync()
    {
        if (_startupStarted) return _startupRestore;
        _startupStarted = true;
        _startupRestore = RestoreThenSyncAsync();
        return _startupRestore;
    }

    private async Task RestoreThenSyncAsync()
    {
        try
        {
            if (_restore != null) await _restore.RestoreAsync();
        }
        catch (Exception error)
        {
            Debug.LogError($"[복구] 초기 복구 실패. 다음 시작에 재시도\n{error}");
        }
        // 이 작업들은 startup 장벽에 포함하지 않는다.
        if (_server != null) _ = _server.TrySyncAsync();
        if (_bookmarkSync != null) _ = SyncBookmarksSafelyAsync();
    }

    private async Task SyncBookmarksSafelyAsync()
    {
        try { await _bookmarkSync.SyncAllAsync(); }
        catch (Exception error) { Debug.LogError($"[즐겨찾기] 동기화 보류\n{error}"); }
    }

    private void HandleConflictForked(string sourceId, LocalSaveFile fork)
    {
        // 네트워크 작업이 시작된 뒤 사용자가 다른 회차를 선택했을 수 있다.
        if (_newPrepared || _playthroughId != sourceId || _localStore.ActiveId != fork.PlaythroughId) return;
        // 진행 중인 Scene의 체크포인트와 경로는 그대로 두고 회차 소유권만 바꾼다.
        _playthroughId = fork.PlaythroughId;
        _forkedFrom = fork.ForkedFrom;
        _active = _localStore.Open(fork.PlaythroughId);
        int ownNow = OwnSeconds;
        _ownSecondsBase = Math.Max(0, _inheritedSeconds + ownNow - fork.InheritedPlaySeconds);
        _inheritedSeconds = fork.InheritedPlaySeconds;
        _startedAt = Time.realtimeSinceStartup;
        ConflictForked?.Invoke(fork.ForkedFrom);
    }
}
