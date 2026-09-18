using System;
using System.Collections.Generic;
using System.Globalization;
using Ked.Progression;
using UnityEngine;

// Progression의 Scene 경계 저장 계약을 로컬 회차 snapshot으로 확정한다.
// Backlog와 Yarn 선택 기록은 Host 상태이므로 이 구현이 직접 캡처한다.
public sealed partial class SaveCoordinator : IScenePersistence
{
    private readonly ILocalSaveStore _localStore;
    private readonly string _contentVersion;
    private readonly BacklogRecorder _backlog;
    private readonly ChoiceHistory _choiceHistory;

    private PlaythroughSession _active;

    private float _startedAt = Time.realtimeSinceStartup;
    private int _playSecondsBase;

    private string _playthroughId;
    private readonly List<SceneRecord> _scenes = new(); // 확정된 Scene 기록(이미 끝난 장면들)
    private SceneCheckpoint _currentEntry; // 현재 플레이 중인 Scene의 "진입 당시 상태"

    public SaveCoordinator(
        ILocalSaveStore localStore,
        string contentVersion,
        BacklogRecorder backlog,
        ChoiceHistory choiceHistory)
    {
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));

        if (string.IsNullOrWhiteSpace(contentVersion))
            throw new ArgumentException("저장 콘텐츠 버전이 비어 있다.", nameof(contentVersion));

        _backlog = backlog ?? throw new ArgumentNullException(nameof(backlog));
        _choiceHistory = choiceHistory ?? throw new ArgumentNullException(nameof(choiceHistory));
        _contentVersion = contentVersion;

        _localStore.Initialize();
    }

    public IReadOnlyList<SceneRecord> Scenes => _scenes;
    public string PlaythroughId => _playthroughId;

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

        _active = _localStore.Open(id);
    }

    private static string NewPlaythroughId() => Guid.NewGuid().ToString("N");

    private static string NowUtc() =>
        DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
}
