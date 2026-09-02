using System.Collections.Generic;

// 서버로 보낼 것을 쌓는 큐. (sync_queue.json)
//
// 규칙:
// - seq 발급과 적재는 한 번의 파일 쓰기.
// - 비우는 조건은 "200을 받았다"뿐. (accepted*와 보낸 수를 비교하지 않음.)
// - 큐는 뒤에만 붙는다(append-only).
//   그래서 전송 시점의 사본(배치)은 언제나 현재 큐의 접두사고,
//   성공하면 그 길이만큼 앞에서 지움. 전송 중에 쌓인 것은 뒤에 남는다.
public sealed class SyncQueue
{
    private string _path;
    private SyncQueueFile _file;

    public SyncQueue(string path)
    {
        SwitchTo(path);
    }

    public string Path => _path;

    // 큐는 회차마다 하나다. 활성 회차가 바뀌면(재개·새 게임·갈라지기) 그 회차의 큐 파일로 옮겨 탄다.
    // 인스턴스는 그대로라 동기화 쪽이 쥔 참조가 안 깨진다. 전송 중에 바꾸지 않는 것은 호출자의 몫.
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

    // 새 회차. PlaythroughId 없음/seq 1부터/revision 없음.
    // 남아 있던 항목은 이전 회차 것이라 함께 버려짐
    public void Reset()
    {
        _file = new SyncQueueFile();
        Persist();
    }

    public void SetPlaythroughId(long playthroughId)
    {
        _file.PlaythroughId = playthroughId;
        Persist();
    }

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
    public void Acknowledge(SyncBatch batch, long newRevision)
    {
        _file.PendingChoices.RemoveRange(0, batch.Choices.Count);
        _file.PendingEvents.RemoveRange(0, batch.Events.Count);
        _file.BaseRevision = newRevision;

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