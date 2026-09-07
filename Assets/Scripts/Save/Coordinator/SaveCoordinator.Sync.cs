using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    private Task _startupSync = Task.CompletedTask;

    public Task SyncPendingAsync()
    {
        _startupSync = SyncPendingCoreAsync();
        return _startupSync;
    }

    public Task WaitForStartupSyncAsync()
    {
        return _startupSync;
    }


    // ---- Startup Sync ----

    // 서버 없으면 끝
    // -> 필요하면 복구
    // -> active queue 선택
    // -> 이전 회차 queue 동기화
    // -> bookmark 동기화
    // -> active 회차 동기화
    private async Task SyncPendingCoreAsync()
    {
        if (_server == null)
            return;

        await RestoreIfNeededAsync();

        string activeId = _localStore.ActiveId;

        if (activeId != null)
            _queue.SwitchTo(_localStore.QueuePathOf(activeId));

        await _server.SyncStaleQueuesAsync(
            _localStore.ListPlaythroughIds(),
            activeId);

        if (_bookmarkSync != null)
            await _bookmarkSync.SyncAllAsync();

        await _server.TrySyncAsync();
    }

    private async Task RestoreIfNeededAsync()
    {
        if (_restore == null)
            return;

        bool hasActiveSave = _localStore.LoadActive() != null;
        bool hasAnyPlaythrough =
            _localStore.ListPlaythroughIds().Count > 0;

        if (hasActiveSave || hasAnyPlaythrough)
            return;

        await _restore.RestoreAsync();
    }


    // ---- "409 Conflict" ----

    // 다른 기기가 현재 회차를 먼저 저장했다.
    //
    // 이미 확정된 서버 기록은 되돌리거나 덮어쓰지 않는다.
    // 이 기기에서 아직 서버에 전달하지 못한 진행만 새 회차로 갈라 이어 간다.
    private void HandleConflict()
    {
        // Phase: ContextPrepared
        LocalSaveFile current = _localStore.LoadActive();

        // Active Save가 없을 경우 아무것도 하지 않음.
        if (current == null)
            return;
        
        // 지나간 회차의 Conflict일 경우 아무것도 하지 않음.
        if (_playthroughId != null
            && !string.Equals(_playthroughId, current.PlaythroughId, StringComparison.Ordinal)) 
            return;

        SyncBatch pending = _queue.CaptureBatch();

        int sceneIndex = _queue.SyncedSceneCount;
        string sourcePlaythroughId = current.PlaythroughId;
        string forkPlaythroughId = NewPlaythroughId();

        var origin = new ForkOrigin
        {
            PlaythroughId = sourcePlaythroughId,
            SceneIndex = sceneIndex,
            Target = null,
        };

        var ctx = new ConflictForkContext(
            current,
            pending,
            sceneIndex,
            sourcePlaythroughId,
            forkPlaythroughId,
            origin);
        
        SetPhase(ctx, ConflictForkPhase.ContextPrepared);
        
        // Phase: SavePrepared
        ctx.Save.PlaythroughId = ctx.ForkPlaythroughId;
        ctx.Save.ForkedFrom = ctx.Origin;
        ctx.Save.SavedAtUtc = NowUtc();

        if (ctx.Save.Scenes != null
            && ctx.SceneIndex < ctx.Save.Scenes.Count)
        {
            ctx.Save.InheritedPlaySeconds =
                ctx.Save.Scenes[ctx.SceneIndex].Checkpoint.PlaySecondsAtEntry;

            ctx.Save.OwnPlaySeconds =
                Math.Max(0, ctx.Save.PlaySeconds - ctx.Save.InheritedPlaySeconds);
        }
        
        SetPhase(ctx, ConflictForkPhase.SavePrepared);
        
        // Phase: SavePersisted
        _localStore.SaveAndSetActive(ctx.Save);
        
        SetPhase(ctx, ConflictForkPhase.SavePersisted);
        
        // Phase: SourceQueueReleased
        _queue.Discard(ctx.Pending);
        
        SetPhase(ctx, ConflictForkPhase.SourceQueueReleased);
        
        // Phase: ForkQueueSelected
        _queue.SwitchTo(
            _localStore.QueuePathOf(ctx.ForkPlaythroughId));
        
        SetPhase(ctx, ConflictForkPhase.ForkQueueSelected);
        
        // Phase: PendingRequeued
        _queue.Reset(ctx.Pending.Choices, ctx.Pending.Events);
        
        SetPhase(ctx, ConflictForkPhase.PendingRequeued);
        
        // Phase: RuntimeStateResolved
        if (_playthroughId != null)
        {
            _playthroughId = ctx.ForkPlaythroughId;
            _forkedFrom = ctx.Origin;

            _inheritedSeconds = ctx.Save.InheritedPlaySeconds;
            _ownSecondsBase = ctx.Save.OwnPlaySeconds;

            _startedAt = Time.realtimeSinceStartup;
        }

        SetPhase(ctx, ConflictForkPhase.RuntimeStateResolved);
        
        // Phase: ConflictPublished
        Debug.LogWarning(
            $"[저장] 충돌(409) — 다른 기기가 회차 {ctx.SourcePlaythroughId}를 먼저 저장했다. " +
            $"이 기기의 진행은 새 회차 {ctx.ForkPlaythroughId}로 갈라 이어 간다 " +
            $"(출처 장면 {ctx.SceneIndex}, " +
            $"미전송 선택 {ctx.Pending.Choices.Count}건 → seq 1부터, " +
            $"이벤트 {ctx.Pending.Events.Count}건).");

        ConflictForked?.Invoke(ctx.Origin);

        SetPhase(ctx, ConflictForkPhase.ConflictPublished);
        
        // Phase: ResyncRequested
        _ = _server.TrySyncAsync();

        SetPhase(ctx, ConflictForkPhase.ResyncRequested);
        
        // Phase: Completed
        SetPhase(ctx, ConflictForkPhase.Completed);
    }

    private void SetPhase(ConflictForkContext ctx, ConflictForkPhase phase)
    {
        ctx.Phase = phase;
    }
}