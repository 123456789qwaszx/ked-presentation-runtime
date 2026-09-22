using System;
using System.Collections.Generic;
using UnityEngine;

// 현재 Playthrough의 저장 세션을 소유한다.
// Scene 진입 시 checkpoint를 만들고, 진행 중 resume point를 갱신하며,
// 정상 Scene 종료 시 snapshot과 Scene history를 확정한다.
//
// 실행 중인 Backlog/Yarn 상태는 직접 읽지 않는다 — 캡처된 값만 받는다.
// 수동 슬롯(SaveSlotService), 갈라지기(PlaythroughForkService),
// 파일 정리(LocalSaveMaintenance)는 여기 없다.
public sealed partial class SaveCoordinator
{
    private readonly ILocalSaveStore _localStore;
    private readonly string _contentVersion;

    private PlaythroughSession _playthroughSession;

    private float _startedAt = Time.realtimeSinceStartup;
    private int _playSecondsBase;

    private string _playthroughId;
    private readonly List<SceneRecord> _scenes = new(); // 확정된 Scene 기록(이미 끝난 장면들)
    private SceneCheckpoint _currentEntry; // 현재 플레이 중인 Scene의 "진입 당시 상태"

    public SaveCoordinator(
        ILocalSaveStore localStore,
        string contentVersion)
    {
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));

        if (string.IsNullOrWhiteSpace(contentVersion))
            throw new ArgumentException("저장 콘텐츠 버전이 비어 있다.", nameof(contentVersion));

        _contentVersion = contentVersion;
        _localStore.Initialize();
    }

    public string PlaythroughId => _playthroughId;

    // 밖에서 쓰는 저장 유스케이스가 현재 상태를 보는 유일한 창.
    // _scenes 목록과 _currentEntry 원본은 넘기지 않는다.
    public PlaythroughSaveSnapshot Capture() =>
        new(
            _playthroughId,
            new List<SceneRecord>(_scenes),
            CopyEntry(_currentEntry),
            TotalSeconds);

    // 백로그 한 줄마다 물어보는 자리가 있어 값 복사로 둔다.
    private static SceneCheckpoint CopyEntry(SceneCheckpoint entry) =>
        entry == null
            ? null
            : new SceneCheckpoint
            {
                ChapterId = entry.ChapterId,
                EpisodeId = entry.EpisodeId,
                Stats = new Dictionary<string, int>(entry.Stats, StringComparer.Ordinal),
                BacklogSerialStart = entry.BacklogSerialStart,
                PlaySecondsAtEntry = entry.PlaySecondsAtEntry,
                EnteredAtUtc = entry.EnteredAtUtc,
            };

    private int TotalSeconds => _playSecondsBase + (int)(Time.realtimeSinceStartup - _startedAt);

    // SaveCoordinator 전체를 특정 회차에 접속.
    // - 메모리 상태, Scene 이력, 플레이 시간, 고정 회차 session
    private void BecomePlaythrough(
        string id,
        int playSeconds,
        List<SceneRecord> scenes)
    {
        _playthroughId = id;
        _playSecondsBase = playSeconds;
        _startedAt = Time.realtimeSinceStartup;

        _scenes.Clear();

        if (scenes != null)
            _scenes.AddRange(scenes);

        _currentEntry = null;

        _playthroughSession = _localStore.Open(id);
    }
}
