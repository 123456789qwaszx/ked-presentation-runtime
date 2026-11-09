using System;
using System.Collections.Generic;

public enum MessengerSide
{
    Left = 0,   // 내가 아닌 화자
    Right = 1,  // 나
}

public readonly struct MessengerEntry
{
    // 대본의 화자 이름(없으면 빈 문자열). 아바타 · 묶음의 키.
    public readonly string Speaker;
    public readonly string Text;
    public readonly MessengerSide Side;

    public MessengerEntry(string speaker, string text, MessengerSide side)
    {
        Speaker = speaker;
        Text = text;
        Side = side;
    }
}

// 메신저 구간 하나의 기록 — 구간이 열린 뒤 지나온 줄의 목록 (docs/PLAN_messenger.md §3).
//
// 화면이 아니라 데이터다. 메신저 뷰는 이 목록을 그리기만 한다.
// 줄 진입(YarnLineBoundary.CommitLineEntered)이 적는데, 그 자리는 시크 통과 줄도 지나간다 —
// 그래서 되감기 · 로드가 루트부터 다시 달리면 커맨드가 다시 열고 줄이 다시 적혀, 표적 줄에서 기록이 일반 재생과 같다.
// 저장하지 않는다. 장면 경계에서 비운다(ScenePresentation).
public sealed class MessengerLog
{
    private readonly List<MessengerEntry> _entries = new();
    private string _me = string.Empty;

    public bool IsOpen { get; private set; }

    // 살아 있는 목록이다 — 받은 쪽은 그 자리에서 읽고, 들고 있지 않는다.
    public IReadOnlyList<MessengerEntry> Entries => _entries;

    // 구간을 연다. me = "나"(오른쪽)의 화자 이름. 열린 채 다시 부르면 새 대화다.
    public void Open(string me)
    {
        _entries.Clear();
        _me = Normalize(me);
        IsOpen = true;
    }

    public void Close()
    {
        Reset();
    }

    // 줄 하나. 구간이 열려 있을 때만 적는다.
    public void RecordLine(string speaker, string text)
    {
        if (!IsOpen)
            return;

        string name = Normalize(speaker);

        MessengerSide side = name.Length > 0 && string.Equals(name, _me, StringComparison.Ordinal)
            ? MessengerSide.Right
            : MessengerSide.Left;

        _entries.Add(new MessengerEntry(name, text ?? string.Empty, side));
    }

    public void Reset()
    {
        _entries.Clear();
        _me = string.Empty;
        IsOpen = false;
    }

    private static string Normalize(string name)
        => string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
}
