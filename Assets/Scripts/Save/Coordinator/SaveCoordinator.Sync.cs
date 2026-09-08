using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    private Task _startupRestore = Task.CompletedTask;
    private bool _startupStarted;
    private Task _maintenance = Task.CompletedTask;
    private float _nextMaintenance;
    private bool _wasOnline;
    private DateTime _nextBookmarkSync;
    private int _bookmarkRetrySeconds = 5;

    // 이어하기만 초기 복구를 기다린다. 기존 로컬이 있으면 즉시 완료된다.
    public Task WaitForStartupSyncAsync() => _startupRestore;

    public Task SyncPendingAsync()
    {
        if (_startupStarted) 
            return _startupRestore;
        
        _startupStarted = true;
        _startupRestore = RestoreThenSyncAsync();
        
        return _startupRestore;
    }

    private async Task RestoreThenSyncAsync()
    {
        try
        {
            if (_restore != null) 
                await _restore.RestoreResumeAsync();
        }
        catch (Exception error)
        {
            Debug.LogError($"[복구] 초기 복구 실패. 다음 시작에 재시도\n{error}");
        }
        // 이 작업들은 startup 장벽에 포함하지 않는다.
        _maintenance = MaintainAsync(true);
    }

    // Unity Update에서 호출. reachability는 힌트이며 실제 성공 여부는 HTTP 결과로 판단한다.
    public void TickSync(float realtime, bool online)
    {
        if (!_startupStarted || !_startupRestore.IsCompleted || !_maintenance.IsCompleted) return;
        if (online && !_wasOnline) { _nextMaintenance = 0; _nextBookmarkSync = DateTime.MinValue; }
        _wasOnline = online;
        if (realtime < _nextMaintenance) return;
        _nextMaintenance = realtime + 5;
        _maintenance = MaintainAsync(online);
    }

    public Task RetryPlaythroughAsync(string id) => _server?.RetryAsync(id) ?? Task.CompletedTask;
    public Task ResolveConflictAsForkAsync(string id) => _server?.ResolveConflictAsForkAsync(id) ?? Task.CompletedTask;

    public async Task<bool> RetryBookmarkAsync(string id)
    {
        BookmarkFile file = _localStore.LoadBookmarks();
        Bookmark bookmark = file.Bookmarks.Find(b => b.Id == id);
        if (bookmark == null || _bookmarkSync == null) return false;
        bookmark.SyncError = null;
        _localStore.SaveBookmarks(file);
        return await _bookmarkSync.PushAsync(id);
    }

    private async Task MaintainAsync(bool online)
    {
        try
        {
            if (online)
            {
                if (_server != null) await _server.TrySyncAsync();
                if (DateTime.UtcNow >= _nextBookmarkSync)
                {
                    if (_restore != null) await _restore.RestoreAsync();
                    if (_bookmarkSync != null) await _bookmarkSync.SyncAllAsync();
                    BookmarkFile bookmarks = _localStore.LoadBookmarks();
                    RestoreProgress progress = _localStore.LoadRestoreProgress();
                    bool pending = bookmarks.PendingDeletes.Count > 0
                        || bookmarks.Bookmarks.Exists(b => b.SyncedAtUtc == null && b.SyncError == null)
                        || progress.Started && !progress.Completed;
                    _bookmarkRetrySeconds = pending ? Math.Min(300, _bookmarkRetrySeconds * 2) : 5;
                    _nextBookmarkSync = DateTime.UtcNow.AddSeconds(_bookmarkRetrySeconds);
                }
            }
            RestoreProgress restore = _localStore.LoadRestoreProgress();
            // 복구 목록을 전부 확인하기 전에는 미발견 수동 슬롯의 출처를 지우지 않는다.
            if (!restore.Started || restore.BookmarksCompleted)
                _localStore.CollectUnusedPlaythroughs(requireSynced: _server != null);
        }
        catch (Exception error)
        {
            _nextBookmarkSync = DateTime.UtcNow.AddSeconds(30);
            Debug.LogError($"[저장] 유지보수 보류. 로컬 기록을 남기고 재시도\n{error}");
        }
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
