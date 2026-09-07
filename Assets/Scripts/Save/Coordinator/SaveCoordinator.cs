using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 장면 보고를 회차 snapshot과 outbox로 함께 확정한다.
// 진행은 active session을 사용하고, 동기화는 회차 ID가 고정된 작업을 사용한다.
public sealed partial class SaveCoordinator : IProgressionReporter
{
    private readonly ILocalSaveStore _localStore;
    private PlaythroughSession _active;
    private bool _newPrepared;
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
        ILocalSaveStore localStore,
        ServerSyncSaveStore server,
        ServerBookmarkSync bookmarkSync = null,
        ServerRestore restore = null)
    {
        _localStore = localStore;
        _localStore.Initialize();
        _server = server;
        _bookmarkSync = bookmarkSync;
        _restore = restore;

        if (_server != null)
            _server.ConflictForked += HandleConflictForked;
    }

    public IReadOnlyList<SceneRecord> Scenes => _scenes;
    public string PlaythroughId => _playthroughId;

    private int OwnSeconds => _ownSecondsBase + (int)(Time.realtimeSinceStartup - _startedAt);
    private int TotalSeconds => _inheritedSeconds + OwnSeconds;

    // SaveCoordinator 전체를 특정 회차에 접속.
    // - 메모리 상태, Scene 이력, 플레이 시간, 고정 회차 session
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

        _active = _localStore.Open(id);
    }

    private static string NewPlaythroughId() => Guid.NewGuid().ToString("N");

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}