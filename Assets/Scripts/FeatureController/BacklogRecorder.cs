using System;
using System.Collections.Generic;

[Serializable]
public struct DialogueLogEntry
{
    public string lineId;

    // 회차 안에서 단조 증가. 장면 시작 순번을 빼면 그 장면의 롤백 포인트 historyIndex가 된다.
    public int lineSerial;

    public string nodeName;
    public string rawText;
    public double timestamp;
}

// 백로그는 세션 스코프다 — 장면(리플레이) 단위로 지우지 않는다.
// 새 판 시작(EpisodePlayer.EnterSceneAsync의 isNewSession)에서만 비우고, 롤백은 표적 뒤 꼬리만 걷는다.
//
// 백점프: 항목이 현재 장면 것이면 라인 단위로 되돌아간다(롤백과 같은 기전). 항목의 장면 소속은
// "장면 진입 시점의 순번" 하나로 판정한다 — 항목마다 태그를 싣지 않는다.
public sealed class BacklogRecorder
{
    // 세션 연속 로그의 상한. 넘치면 앞(오래된 쪽)에서 밀려난다 —
    // 뒤(최신 쪽)는 롤백 truncate의 좌표라 건드리면 안 된다.
    private const int MAXLOGCOUNT = 300;
    private readonly List<DialogueLogEntry> _entries = new(MAXLOGCOUNT);

    private int _nextSerial;

    // 현재 장면의 첫 라인이 받을(받은) 순번. 장면 진입에서 찍는다.
    private int _sceneStartSerial;

    public IReadOnlyList<DialogueLogEntry> Entries => _entries;

    public void Record(YarnLineMeta meta)
    {
        Add(new DialogueLogEntry
        {
            lineId = meta.lineId,
            lineSerial = _nextSerial++,
            nodeName = meta.nodeName,
            rawText = meta.rawText,
        });
    }

    public void ClearBacklog()
    {
        _entries.Clear();
        _nextSerial = 0;
        _sceneStartSerial = 0;
    }

    // 다음 라인이 받을 순번. 세이브가 "이 장면의 첫 라인 순번"으로 적어 둔다 — 되감기의 자르는 자리.
    public int NextSerial => _nextSerial;

    // 세이브에서 되살린다 — 이전 장면들의 항목. 순번은 저장된 것을 그대로 쓰고, 다음 순번은 그 뒤를 잇는다.
    // 이 뒤에 장면 진입이 MarkSceneStart를 찍고, 현재 장면의 라인은 Load 시크가 다시 적는다.
    public void Restore(IReadOnlyList<DialogueLogEntry> entries)
    {
        _entries.Clear();

        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
                Add(entries[i]);
        }

        _nextSerial = _entries.Count == 0 ? 0 : _entries[^1].lineSerial + 1;
        _sceneStartSerial = _nextSerial;
    }

    // 되감기 — 순번 경계 뒤의 항목을 걷어낸다(이전 장면 루트 점프).
    public void TruncateFromSerial(int serial)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].lineSerial >= serial)
                _entries.RemoveAt(i);
        }

        _nextSerial = serial;
        _sceneStartSerial = serial;
    }

    // 장면 진입. 이 뒤에 적히는 라인의 순번 - 시작 순번 = 그 장면의 롤백 포인트 historyIndex.
    // 롤백 포인트가 같은 순간에 비워지므로 두 좌표계가 0에서 나란히 출발한다.
    public void MarkSceneStart()
    {
        _sceneStartSerial = _nextSerial;
    }

    public bool IsInCurrentScene(in DialogueLogEntry entry) =>
        entry.lineSerial >= _sceneStartSerial;

    // 현재 장면 항목의 롤백 포인트 historyIndex. 장면 밖 항목이면 -1.
    public int HistoryIndexOf(in DialogueLogEntry entry) =>
        IsInCurrentScene(entry) ? entry.lineSerial - _sceneStartSerial : -1;

    // 롤백용 — 표적 라인 뒤에 커밋된 라인 수만큼 최신 쪽에서 걷어낸다.
    // 백로그와 롤백 포인트는 같은 자리(CommitLineEntered)에서 쌓이므로 꼬리가 1:1로 정렬된다.
    // (시크 패스스루 동안은 양쪽 규칙이 갈리지만 — 포인트는 다시 쌓고 백로그는 안 쌓는다 —
    //  롤백은 시크 중에 걸 수 없으므로(RequestRollbackOneStep 가드) 이 정렬은 깨지지 않는다.)
    //
    // 순번도 같이 되감는다 — 리플레이가 표적을 지나 새로 적는 라인이 표적 다음 순번을 받아야
    // "순번 - 장면 시작 = historyIndex"가 계속 성립한다.
    public void TruncateFromEnd(int count)
    {
        if (count <= 0)
            return;

        int removed = Math.Min(count, _entries.Count);

        if (removed >= _entries.Count)
            _entries.Clear();
        else
            _entries.RemoveRange(_entries.Count - removed, removed);

        _nextSerial -= removed;
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
