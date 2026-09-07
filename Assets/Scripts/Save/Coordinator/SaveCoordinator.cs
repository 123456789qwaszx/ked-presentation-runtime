using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Bridges progression reports into the save layer.
// This is the save layer's only dependency on progression-specific types.
//
// 저장 단위는 장면이다 (G4). 장면 끝 fold 한 번에:
// 1) 현재 상태를 먼저 보존    / localStore.Save(...)  — 장면 진입 스냅샷 또는 챕터 완료
// 2) 서버용 이력을 남김       / _queue.EnqueueChoice/Event(...)  — 확정 순서대로
// 3) 서버 전송은 기다리지 않음 / _server.TrySyncAsync(...)
//
// 회차 파일은 이력(Scenes)을 든다 (F1). 장면 진입에서 스냅샷을 받아 두고, 장면 끝에 경로를 붙여
// 장면 기록 하나로 접는다. 이력은 현재 챕터 안에서만 쌓이고 챕터가 바뀌면 비운다.
// 시간은 둘로 센다 — 물려받은 것(Inherited)과 이 회차에서 새로 플레이한 것(Own).
//
// 갈라지기 (F2): 이력의 장면 기록 하나를 물려받아 새 회차 파일을 쓰고 활성으로 세운다.
// 옛 회차 파일은 그대로 남는다. 그 뒤 런처가 다시 띄우면 재개 경로가 새 회차를 연다.
public sealed partial class SaveCoordinator : IProgressionReporter
{
    private readonly ISaveStore _localStore;
    private readonly SyncQueue _queue; // 서버에 아직 보내지 못한 변경사항들.
    private readonly ServerSyncSaveStore _server;

    private float _startedAt = Time.realtimeSinceStartup;
    private int _inheritedSeconds;
    private int _ownSecondsBase;

    private string _playthroughId;
    private ForkOrigin _forkedFrom;

    private readonly List<SceneRecord> _scenes = new(); // 확정된 Scene 기록(이미 끝난 장면들)
    private SceneCheckpoint _currentEntry; // 현재 플레이 중인 Scene의 "진입 당시 상태"

    // 서버 사본 쪽. 셋 다 서버가 없으면 null.
    private readonly ServerBookmarkSync _bookmarkSync;
    private readonly ServerRestore _restore;

    // 409로 갈라졌다 — UI가 한 줄 알릴 재료(출처). 사용자가 시키지 않았는데 회차가 둘이 된 경우다.
    public event Action<ForkOrigin> ConflictForked;

    public SaveCoordinator(
        ISaveStore localStore,
        SyncQueue queue,
        ServerSyncSaveStore server,
        ServerBookmarkSync bookmarkSync = null,
        ServerRestore restore = null)
    {
        _localStore = localStore;
        _queue = queue;
        _server = server;
        _bookmarkSync = bookmarkSync;
        _restore = restore;

        if (_server != null)
            _server.ConflictDetected += HandleConflict;
    }

    public IReadOnlyList<SceneRecord> Scenes => _scenes;
    public string PlaythroughId => _playthroughId;

    private int OwnSeconds => _ownSecondsBase + (int)(Time.realtimeSinceStartup - _startedAt);
    private int TotalSeconds => _inheritedSeconds + OwnSeconds;

    // SaveCoordinator 전체를 특정 회차에 접속.
    // - 메모리 상태, Scene이력, 플레이 시간, SyncQueue
    private void BecomePlaythrough(
        string id, 
        ForkOrigin forkedFrom,
        int inheritedSeconds,
        int ownSeconds,
        List<SceneRecord> scenes)
    {
        _playthroughId = id;
        _forkedFrom = forkedFrom;
        _inheritedSeconds = inheritedSeconds;
        _ownSecondsBase = ownSeconds;
        _startedAt = Time.realtimeSinceStartup;

        _scenes.Clear();

        if (scenes != null)
            _scenes.AddRange(scenes);

        _currentEntry = null;

        _queue.SwitchTo(_localStore.QueuePathOf(id));
    }

    private static string NewPlaythroughId() => Guid.NewGuid().ToString("N");

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}