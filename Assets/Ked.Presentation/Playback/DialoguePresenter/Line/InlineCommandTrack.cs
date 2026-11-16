using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

// 인라인 명령 — 대사 글 안의 [cmd="명령 한 줄"/]. 타자기가 그 자리의 글자를 드러내기 직전에 실행한다.
// PLAN_stage-step-inline-markup.md §4.
//
//   세이나: 아, 음…… [cmd="face_swap s2 03 0.3s"/]이건 비밀인데.
//
// - 따옴표 안은 대본 명령(<<…>>)과 같은 글이다. 뜻은 무대 파서 한 곳에 있다(YarnCommandBridge.TryParseInline).
// - 새 스텝이 아니다 — 줄 머리 명령과 같은 스텝에 덧붙는다(YarnPlaybackDriver.PlayInStep). 좌표도 아니다.
// - 줄 하나의 명령 열이다: 줄에 들어갈 때 Begin, 글자마다 PlayUpTo, 줄이 끝나거나 시크가 지나가면 PlayRemaining.
//   끊긴 줄(run 무효)의 남은 것은 실행하지 않는다 — 다음 Begin이 지운다.
// - 명령 글은 닿았을 때 푼다 — 대상 슬롯은 그때의 무대로 판정한다(줄 머리의 pause 뒤에 서는 슬롯도 받는다).
public sealed class InlineCommandTrack : IActionMarkupHandler
{
    public const string MarkerName = "cmd";

    private readonly struct Entry
    {
        public readonly int Position;
        public readonly string CommandText;

        public Entry(int position, string commandText)
        {
            Position = position;
            CommandText = commandText;
        }
    }

    private readonly YarnCommandBridge _bridge;
    private readonly YarnPlaybackDriver _playbackDriver;

    // 글 순서대로. Yarn이 마커를 글 순서로 주고, 같은 자리의 것은 쓴 순서다.
    private readonly List<Entry> _entries = new();
    private readonly List<CommandSpecBase> _batch = new();
    private int _next;

    public InlineCommandTrack(YarnCommandBridge bridge, YarnPlaybackDriver playbackDriver)
    {
        _bridge = bridge;
        _playbackDriver = playbackDriver;
    }

    // 줄에 들어갈 때 — 이 줄의 인라인 명령을 모은다. 앞 줄의 남은 것은 지운다.
    public void Begin(MarkupParseResult text)
    {
        _entries.Clear();
        _next = 0;

        List<MarkupAttribute> attributes = text.Attributes;

        if (attributes == null)
            return;

        for (int i = 0; i < attributes.Count; i++)
        {
            MarkupAttribute attribute = attributes[i];

            if (attribute.Name != MarkerName)
                continue;

            if (attribute.Properties == null
                || !attribute.Properties.TryGetValue(MarkerName, out MarkupValue value)
                || string.IsNullOrWhiteSpace(value.StringValue))
            {
                Debug.LogError("[인라인 명령] [cmd=\"…\"/]에 명령 글이 없다 — 실행하지 않는다.");
                continue;
            }

            InsertInTextOrder(new Entry(attribute.Position, value.StringValue));
        }
    }

    // 이 글자 자리까지의 명령을 실행한다 — 그 글자가 드러나기 직전에.
    public void PlayUpTo(int characterIndex)
    {
        while (_next < _entries.Count && _entries[_next].Position <= characterIndex)
            Collect(_entries[_next++]);

        PlayBatch();
    }

    // 남은 명령을 모두 실행한다 — 글 끝의 것, 다 보이기 전에 넘어간 줄, 시크가 지나가는 줄.
    public void PlayRemaining()
    {
        while (_next < _entries.Count)
            Collect(_entries[_next++]);

        PlayBatch();
    }

    private void Collect(Entry entry)
    {
        _bridge.TryParseInline(entry.CommandText, _batch);
    }

    private void PlayBatch()
    {
        if (_batch.Count == 0)
            return;

        _playbackDriver.PlayInStep(_batch);
        _batch.Clear();
    }

    private void InsertInTextOrder(Entry entry)
    {
        int index = _entries.Count;

        while (index > 0 && _entries[index - 1].Position > entry.Position)
            index--;

        _entries.Insert(index, entry);
    }

    void IActionMarkupHandler.OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

    void IActionMarkupHandler.OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }

    YarnTask IActionMarkupHandler.OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        PlayUpTo(currentCharacterIndex);
        return YarnTask.CompletedTask;
    }

    void IActionMarkupHandler.OnLineDisplayComplete() { }

    void IActionMarkupHandler.OnLineWillDismiss() { }
}
