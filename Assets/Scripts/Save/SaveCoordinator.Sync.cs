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


    // 409
    // HandleConflict Transaction 계약
    // [1]현재 Save 읽기
    // [2]충돌 대상 검증
    // [3]Pending 캡처
    // [4]Fork metadata 생성
    // [5]Save를 새 Playthrough로 변경
    // [6]Save 저장
    // [7]기존 Queue pending 제거
    // [8]새 Queue로 Switch
    // [9]Pending을 새 Queue에 Reset
    // [10]Runtime state 변경
    // [11]Event 발행
    // [12]TrySync
    private void HandleConflict()
    {
        LocalSaveFile current = _localStore.LoadActive();

        // Active Save가 없을 경우 아무것도 하지 않음.
        if (current == null)
            return;

        // 메모리 PlaythroughId != Active Save PlaythroughId라면,
        // 지나간 회차의 Conflict이므로 아무것도 하지 않음.
        if (_playthroughId != null 
            && !string.Equals(_playthroughId, current.PlaythroughId, StringComparison.Ordinal))
            return;

        // 정상 Conflict
        // - 현재 pending은 새 회차로 이전.
        // - 새 Playthrough가 active가 됨.
        // - pending seq는 다시 시작.
        // - runtime이 존재하면 runtime도 fork를 따라감.
        // - ConflictForked 발생.
        // - 새 회차 sync 재시도.
        SyncBatch pending = _queue.CaptureBatch();
        
        int sceneIndex = _queue.SyncedSceneCount;
        string fromId = current.PlaythroughId;
        string newId = NewPlaythroughId();

        var origin = new ForkOrigin { PlaythroughId = fromId, SceneIndex = sceneIndex, Target = null };

        current.PlaythroughId = newId;
        current.ForkedFrom = origin;
        current.SavedAtUtc = NowUtc();

        // 시간도 다른 갈라지기처럼 나눈다 — 출처 장면 진입까지가 물려받은 것, 나머지가 이 회차 것.
        if (current.Scenes != null && sceneIndex < current.Scenes.Count)
        {
            current.InheritedPlaySeconds = current.Scenes[sceneIndex].Checkpoint.PlaySecondsAtEntry;
            current.OwnPlaySeconds = Math.Max(0, current.PlaySeconds - current.InheritedPlaySeconds);
        }

        _localStore.Save(current);

        _queue.Discard(pending);
        _queue.SwitchTo(_localStore.QueuePathOf(newId));
        _queue.Reset(pending.Choices, pending.Events);

        // 재개 전(시작 시 동기화)이면 메모리는 비어 있다 — 재개가 새 파일을 읽으며 채운다.
        if (_playthroughId != null)
        {
            _playthroughId = newId;
            _forkedFrom = origin;
            _inheritedSeconds = current.InheritedPlaySeconds;
            _ownSecondsBase = current.OwnPlaySeconds;
            _startedAt = Time.realtimeSinceStartup;
        }

        Debug.LogWarning(
            $"[저장] 충돌(409) — 다른 기기가 회차 {fromId}를 먼저 저장했다. 이 기기의 진행은 새 회차 {newId}로 갈라 이어 간다 " +
            $"(출처 장면 {sceneIndex}, 미전송 선택 {pending.Choices.Count}건 → seq 1부터, 이벤트 {pending.Events.Count}건).");

        ConflictForked?.Invoke(origin);

        _ = _server.TrySyncAsync();
    }

    // ── 잔손 ────────────────────────────────────────────────────────────────

    private async Task FlushBeforeForkAsync()
    {
        if (_server == null)
            return;

        await _server.FlushAsync();

        int left = _queue.PendingCount;

        if (left > 0)
            Debug.LogWarning($"[저장] 갈라지기 전 동기화 못 함 — 옛 회차 큐에 {left}건 남김. 다음 시작에 다시 보낸다.");
    }
}
