using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Unity 메인 스레드의 순차 worker. 활성·과거 회차를 같은 ID 큐로 처리한다.
// 실패한 ID는 이번 drain에서는 재시도하지 않는다. 유지보수 tick에서 backoff 이후 재시도한다.
public sealed class ServerSyncSaveStore
{
    private readonly ILocalSaveStore _localStore;
    private readonly ISaveSyncTransport _transport;
    private readonly Queue<string> _pending = new();
    private readonly HashSet<string> _queued = new(StringComparer.Ordinal);
    private Task _inFlight;
    private readonly Func<DateTime> _now;
    private string _publishedId;
    private long _publishedSelection = -1;
    private DateTime _resumeRetryAt;

    public event Action<string, LocalSaveFile> ConflictForked;

    public ServerSyncSaveStore(ILocalSaveStore localStore, ISaveSyncTransport transport, Func<DateTime> now = null)
    {
        _localStore = localStore;
        _transport = transport;
        _now = now ?? (() => DateTime.UtcNow);
    }

    public Task TrySyncAsync()
    {
        foreach (string id in _localStore.ListPlaythroughIds()) Queue(id);
        return Start();
    }

    public Task RequestSyncAsync(string id)
    {
        Queue(id);
        return Start();
    }

    // 수동 재시도 API. 409는 같은 revision 재전송으로 해결되지 않아 별도 fork API를 사용한다.
    public Task RetryAsync(string id)
    {
        _localStore.Open(id)?.ResetRetry();
        _resumeRetryAt = DateTime.MinValue;
        return RequestSyncAsync(id);
    }

    public Task ResolveConflictAsForkAsync(string id)
    {
        if (_inFlight != null) throw new InvalidOperationException("동기화 중에는 충돌 해소를 시작할 수 없다.");
        PlaythroughSession source = _localStore.Open(id);
        if (source?.Read().Sync.ConflictedAtUtc == null) throw new InvalidOperationException("중단된 충돌 회차가 아니다.");
        PlaythroughSession fork = _localStore.ForkConflict(id);
        ConflictForked?.Invoke(id, fork.Read().Snapshot);
        return RequestSyncAsync(fork.Id);
    }

    private void Queue(string id)
    {
        if (id != null && _localStore.Open(id)?.CanRetry(_now()) == true && _queued.Add(id)) _pending.Enqueue(id);
    }

    private Task Start()
    {
        if (_inFlight != null) return _inFlight;
        // 먼저 task를 게시하므로 동기 완료·재진입도 같은 drain을 관찰한다.
        var completion = new TaskCompletionSource<bool>();
        _inFlight = completion.Task;
        _ = DrainAsync(completion);
        return completion.Task;
    }

    private async Task DrainAsync(TaskCompletionSource<bool> completion)
    {
        var failed = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            do
            {
                while (_pending.Count > 0)
                {
                    string id = _pending.Dequeue();
                    _queued.Remove(id);
                    if (failed.Contains(id)) continue;
                    try
                    {
                        if (await SyncOnceAsync(id)) Queue(id);
                        else failed.Add(id);
                    }
                    catch (Exception error)
                    {
                        failed.Add(id);
                        try { _localStore.Open(id)?.RecordFailure(_now()); } catch { /* 저장 오류 원본을 아래에 기록 */ }
                        Debug.LogError($"[동기화] 회차 {id} 실패. 로컬 기록 보존\n{error}");
                    }
                }
                await PublishResumeAsync();
            } while (_pending.Count > 0);
        }
        catch (Exception error) { Debug.LogError($"[동기화] 이어하기 포인터 전송 보류\n{error}"); }
        finally
        {
            _inFlight = null;
            completion.TrySetResult(true);
        }
    }

    private async Task<bool> SyncOnceAsync(string id)
    {
        PlaythroughSession session = _localStore.Open(id);
        SyncWork work = session?.CaptureSyncWork();
        if (work == null) return true;

        long? serverId = session.Read().Sync.PlaythroughId;
        if (serverId == null)
        {
            serverId = await _transport.CreatePlaythroughAsync(work.Snapshot);
            if (serverId == null) { session.RecordFailure(_now()); return false; }
            session.SetServerId(serverId.Value);
        }
        if (work.ChapterVersion == null)
        {
            int? version = await _transport.ResolveChapterVersionAsync(work.Snapshot.ChapterId);
            if (version == null) { session.RecordFailure(_now()); return false; }
            session.SetChapterVersion(work.Id, version.Value);
            work.ChapterVersion = version;
        }

        var result = await _transport.UploadAsync(serverId.Value, work);
        if (result.Ok)
        {
            session.Acknowledge(work.Id, result.Body.Revision);
            Debug.Log($"[동기화] {id} commit {work.CommitVersion} 완료 — revision {result.Body.Revision}");
            return true;
        }
        if (result.ErrorCode == "CONFLICT")
        {
            if (work.BaseRevision == 0)
            {
                // 신규 회차까지 충돌하는 서버 오류에서 무한 fork를 만들지 않는다.
                session.MarkConflicted(DateTime.UtcNow.ToString("o"));
                return false;
            }
            // 응답의 원래 회차를 갈라 보존한다. active가 바뀌었으면 현재 진행은 건드리지 않는다.
            PlaythroughSession fork = _localStore.ForkConflict(id);
            Queue(fork.Id);
            ConflictForked?.Invoke(id, fork.Read().Snapshot);
            return true;
        }
        bool blocked = result.Status >= 400 && result.Status < 500 && result.Status != 408 && result.Status != 429;
        session.RecordFailure(_now(), blocked ? result.ErrorCode ?? ("HTTP_" + result.Status) : null);
        Debug.LogWarning($"[동기화] {id} 전송 보류 — HTTP {result.Status} {result.ErrorCode}");
        return false;
    }
    private async Task PublishResumeAsync()
    {
        string id = _localStore.ActiveId;
        long selection = _localStore.SelectionVersion;
        if (id == null || _now() < _resumeRetryAt || id == _publishedId && selection == _publishedSelection) return;
        PlaythroughFile file = _localStore.Open(id)?.Read();
        if (file == null || file.SyncedCommitVersion == 0 || file.ReleasedTo != null) return;
        _resumeRetryAt = _now().AddSeconds(30);
        if (!await _transport.SetResumeAsync(id, selection, _localStore.SelectionScopeId)) return;
        _publishedId = id;
        _publishedSelection = selection;
        _resumeRetryAt = DateTime.MinValue;
    }

}
