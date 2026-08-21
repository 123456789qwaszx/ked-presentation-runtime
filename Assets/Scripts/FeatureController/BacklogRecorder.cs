using System;
using System.Collections.Generic;

[Serializable]
public struct DialogueLogEntry
{
    public string lineId;
    public int lineSerial;
    public string nodeName;
    public string rawText;
    public double timestamp;
}

// 백로그는 세션 스코프다 — 장면(리플레이) 단위로 지우지 않는다.
// 새 판 시작(EpisodePlayer.StartGameAsync)에서만 비우고, 롤백은 표적 뒤 꼬리만 걷는다.
public sealed class BacklogRecorder
{
    // 세션 연속 로그의 상한. 넘치면 앞(오래된 쪽)에서 밀려난다 —
    // 뒤(최신 쪽)는 롤백 truncate의 좌표라 건드리면 안 된다.
    private const int MAXLOGCOUNT = 300;
    private readonly List<DialogueLogEntry> _entries = new(MAXLOGCOUNT);
    
    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public void Record(YarnLineMeta meta)
    {
        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
        });
    }

    public void ClearBacklog()
    {
        _entries.Clear();
    }

    // 롤백용 — 표적 라인 뒤에 커밋된 라인 수만큼 최신 쪽에서 걷어낸다.
    // 백로그와 롤백 포인트는 같은 자리(CommitLineEntered)에서 쌓이므로 꼬리가 1:1로 정렬된다.
    // (시크 패스스루 동안은 양쪽 규칙이 갈리지만 — 포인트는 다시 쌓고 백로그는 안 쌓는다 —
    //  롤백은 시크 중에 걸 수 없으므로(RequestRollbackOneStep 가드) 이 정렬은 깨지지 않는다.)
    public void TruncateFromEnd(int count)
    {
        if (count <= 0)
            return;

        if (count >= _entries.Count)
        {
            _entries.Clear();
            return;
        }

        _entries.RemoveRange(_entries.Count - count, count);
    }

    
    private void Add(in DialogueLogEntry entry)
    {
        _entries.Add(entry);

        if (_entries.Count > MAXLOGCOUNT)
        {
            _entries.RemoveAt(0);
        }
    }
}