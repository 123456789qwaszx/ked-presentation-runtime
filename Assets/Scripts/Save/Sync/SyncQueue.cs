using System.Collections.Generic;

// 서버로 보낼 것을 쌓는 큐. (sync_queue.json)
// - 메모리 큐가 아님.
// - 변경 시 마다 Persist()
// - playthrough/A.queue.json에 계속 저장.
// - 큐파일 하나가 회차 하나에 대응.
public sealed class SyncQueue
{
    private string _path;
    private SyncQueueFile _file;

    public SyncQueue(string path)
    {
        SwitchTo(path);
    }

    // 동기화 쪽의 참조가 안 깨지도록 인스턴스 유지.
    public void SwitchTo(string path)
    {
        _path = path;

        string json = AtomicFile.ReadAllTextOrNull(path);
        _file = json == null
            ? new SyncQueueFile()
            : SaveJson.Deserialize<SyncQueueFile>(json);
    }

    public long? PlaythroughId => _file.PlaythroughId;

    // 다음 선택이 받을 seq. 체크포인트가 "여기까지의 마지막 seq"를 적어 두는 데 쓴다(되감기 표식 재료).
    public int NextSeq => _file.NextSeq;
    public long? BaseRevision => _file.BaseRevision;
    public int PendingCount => _file.PendingChoices.Count + _file.PendingEvents.Count;

    // 서버가 마지막으로 받아 준 시점의 장면 기록 수. 409로 갈라질 때 "어디까지 같은가"의 답.
    public int SyncedSceneCount => _file.SyncedSceneCount;

    public string ConflictedAtUtc => _file.ConflictedAtUtc;

    // 다음 startup에서 이 queue를 계속 재 전송하지 않도록 '409 처리 기록'
    public void MarkConflicted(string nowUtc)
    {
        _file.ConflictedAtUtc = nowUtc;
        Persist();
    }

    // 새 회차. PlaythroughId 없음/seq 1부터/revision 없음.
    // 남아 있던 항목은 이전 회차 것이라 함께 버려짐
    public void Reset()
    {
        _file = new SyncQueueFile();
        Persist();
    }

    // 409 fork용. (새 회차인데 미전송을 들고 시작, seq는 1부터 다시 매김.)
    public void Reset(IReadOnlyList<PendingChoice> choices, IReadOnlyList<PendingEvent> events)
    {
        _file = new SyncQueueFile();

        for (int i = 0; i < choices.Count; i++)
        {
            _file.PendingChoices.Add(new PendingChoice
            {
                Seq = _file.NextSeq++,
                EpisodeId = choices[i].EpisodeId,
                OptionIndex = choices[i].OptionIndex,
                ChosenAt = choices[i].ChosenAt,
            });
        }

        for (int i = 0; i < events.Count; i++)
            _file.PendingEvents.Add(new PendingEvent { EpisodeId = events[i].EpisodeId, OccurredAt = events[i].OccurredAt });

        Persist();
    }

    // 서버에서 복구한 회차의 큐
    // 미전송 데이터는 없음, 서버 상태는 이미 암.
    public void Restore(long playthroughId, long baseRevision, int nextSeq, int syncedSceneCount)
    {
        _file = new SyncQueueFile
        {
            PlaythroughId = playthroughId,
            BaseRevision = baseRevision,
            NextSeq = nextSeq,
            SyncedSceneCount = syncedSceneCount,
        };

        Persist();
    }

    public void SetPlaythroughId(long playthroughId)
    {
        _file.PlaythroughId = playthroughId;
        Persist();
    }

    // - seq 발급과 적재는 한 번의 파일 쓰기.
    // - 큐는 뒤에만 붙는다(append-only).
    //   그래서 전송 시점의 사본(배치)은 언제나 현재 큐의 접두사고,
    //   성공하면 그 길이만큼 앞에서 지움. 전송 중에 쌓인 것은 뒤에 남는다.
    public void EnqueueChoice(string episodeId, int optionIndex, string chosenAtUtc)
    {
        _file.PendingChoices.Add(new PendingChoice
        {
            Seq = _file.NextSeq,
            EpisodeId = episodeId,
            OptionIndex = optionIndex,
            ChosenAt = chosenAtUtc,
        });

        _file.NextSeq++;

        Persist();
    }

    public void EnqueueEvent(string episodeId, string occurredAtUtc)
    {
        _file.PendingEvents.Add(new PendingEvent
        {
            EpisodeId = episodeId,
            OccurredAt = occurredAtUtc,
        });

        Persist();
    }

    // "CaptureBatch() -> 서버 전송 -> 성공 -> Acknowledge()"
    // Captures the current queue contents for one sync attempt.
    public SyncBatch CaptureBatch() =>
        new SyncBatch(
            new List<PendingChoice>(_file.PendingChoices),
            new List<PendingEvent>(_file.PendingEvents));

    // '200 수신 확인'
    // Acknowledges a successfully synced batch.
    public void Acknowledge(SyncBatch batch, long newRevision, int syncedSceneCount)
    {
        _file.PendingChoices.RemoveRange(0, batch.Choices.Count);
        _file.PendingEvents.RemoveRange(0, batch.Events.Count);
        _file.BaseRevision = newRevision;
        _file.SyncedSceneCount = syncedSceneCount;

        Persist();
    }

    // '서버가 받은 게 아님(200이 아님)'
    // 다른 회차 queue로 옮겼으니, 이 source queue에서만 제거.
    // 서버 id·revision은 그대로.
    public void Discard(SyncBatch batch)
    {
        _file.PendingChoices.RemoveRange(0, batch.Choices.Count);
        _file.PendingEvents.RemoveRange(0, batch.Events.Count);

        Persist();
    }

    private void Persist() =>
        AtomicFile.WriteAllText(_path, SaveJson.SerializePretty(_file));
}

public sealed class SyncBatch
{
    public List<PendingChoice> Choices { get; }
    public List<PendingEvent> Events { get; }

    public SyncBatch(List<PendingChoice> choices, List<PendingEvent> events)
    {
        Choices = choices;
        Events = events;
    }
}