using System.Collections.Generic;
using System.Threading;
using Ked.Presentation.Core;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

// 대사 줄의 자리 — 글의 표지 [beat=a/]에 줄 앞의 <<at a 명령>>을 잇는다. 타자기가 그 글자를 드러내기 직전에 실행한다.
// docs/PLAN_line-beat.md §3 · §4.
//
//   <<face s2 05>>                     머리 — 대사와 함께(StepHead)
//   <<at a face_swap s2 03 0.3s>>      표지 a
//   <<at end emoji s2 sweat>>          타자기 끝
//   세이나: 아, 음…… [beat=a/]이건 비밀인데.
//
// - 자리는 언어마다(글), 명령은 모든 언어에 하나(<<at>>). 순서는 코어 LineBeatSchedule이 정한다 — 폴드와 같은 함수다.
// - 옛 [cmd="명령"/]도 이행 동안 받는다(L5에서 걷는다). 같은 목록에 글 순서로 선다.
// - 새 스텝이 아니다 — 줄 머리 명령과 같은 스텝에 덧붙는다(YarnPlaybackDriver.PlayInStep). 좌표도 아니다.
// - 줄 하나의 명령 열이다: 줄에 들어갈 때 Begin, 글자마다 PlayUpTo, 줄이 끝나거나 시크가 지나가면 PlayRemaining.
//   끊긴 줄(run 무효)의 남은 것은 실행하지 않는다 — 다음 Begin이 지운다.
// - 명령 글은 닿았을 때 푼다 — 대상 슬롯은 그때의 무대로 판정한다.
public sealed class InlineCommandTrack : IActionMarkupHandler
{
    private readonly YarnCommandBridge _bridge;
    private readonly YarnPlaybackDriver _playbackDriver;

    // 실행 순서대로(LineBeatSchedule). 커서 하나가 앞으로만 간다.
    private List<ScheduledCommand> _entries = new();
    private int _next;

    private readonly List<LineMarker> _markers = new();
    private readonly List<string> _problems = new();
    private readonly List<CommandSpecBase> _batch = new();

    public InlineCommandTrack(YarnCommandBridge bridge, YarnPlaybackDriver playbackDriver)
    {
        _bridge = bridge;
        _playbackDriver = playbackDriver;
    }

    // 줄에 들어갈 때 — 이 줄의 표지와 쌓인 <<at>>을 이어 목록을 짓는다. 앞 줄의 남은 것은 지운다.
    public void Begin(MarkupParseResult text)
    {
        _markers.Clear();
        _problems.Clear();
        _next = 0;

        CollectMarkers(text);

        _entries = LineBeatSchedule.Build(_markers, _playbackDriver.TakeCollectedAt(), _problems);

        // 번역문이 표지를 빠뜨렸거나(R5) 짝이 맞지 않는다(R6) — 명령은 사라지지 않게 줄 끝에 남았다.
        foreach (string problem in _problems)
            Debug.LogWarning($"[줄의 자리] {problem}");
    }

    // 이 글자 자리까지의 명령을 실행한다 — 그 글자가 드러나기 직전에.
    public void PlayUpTo(int characterIndex)
    {
        while (_next < _entries.Count && _entries[_next].Position <= characterIndex)
            Collect(_entries[_next++]);

        PlayBatch();
    }

    // 남은 명령을 모두 실행한다 — 타자기 끝(end · 자리를 잃은 것), 다 보이기 전에 넘어간 줄, 시크가 지나가는 줄.
    public void PlayRemaining()
    {
        while (_next < _entries.Count)
            Collect(_entries[_next++]);

        PlayBatch();
    }

    // 무대 스텝의 머리 뒤 — <<at>> 다음이 대사 줄이 아니었다(R1). 사라지지 않게, 머리보다 앞서지 않게 쓴 순서로 덧붙인다.
    public void PlayStrayAt()
    {
        if (_playbackDriver.CollectedAt.Count == 0)
            return;

        List<AtCommand> ats = _playbackDriver.TakeCollectedAt();

        Debug.LogWarning(
            $"[줄의 자리] <<at>> {ats.Count}개 다음이 대사 줄이 아니라 무대 스텝(<<step>> · 노드 끝)이다 — " +
            $"붙을 줄이 없어 그 스텝의 머리 뒤에 실행한다: {ats[0]}{(ats.Count > 1 ? " …" : "")}");

        foreach (AtCommand at in ats)
            Collect(new ScheduledCommand(LineBeatSchedule.LineEnd, at.Command, at.ToString()));

        PlayBatch();
    }

    // 글 순서의 표지 — [beat=a/]와 옛 [cmd="…"/]. Yarn이 마커를 글 순서로 준다.
    private void CollectMarkers(MarkupParseResult text)
    {
        List<MarkupAttribute> attributes = text.Attributes;

        if (attributes == null)
            return;

        for (int i = 0; i < attributes.Count; i++)
        {
            MarkupAttribute attribute = attributes[i];

            if (attribute.Name == LineBeatSchedule.MarkerName)
            {
                if (TryReadValue(attribute, LineBeatSchedule.MarkerName, out string beat))
                    _markers.Add(LineMarker.ForBeat(beat, attribute.Position));
                else
                    Debug.LogWarning("[줄의 자리] [beat=이름/]에 이름이 없다 — 무시한다.");

                continue;
            }

            if (attribute.Name != YarnCommandTextExtractor.InlineCommandMarker)
                continue;

            if (!TryReadValue(attribute, YarnCommandTextExtractor.InlineCommandMarker, out string commandText))
            {
                Debug.LogError("[인라인 명령] [cmd=\"…\"/]에 명령 글이 없다 — 실행하지 않는다.");
                continue;
            }

            if (!YarnCommandTextExtractor.TryParseCommand($"<<{commandText}>>", "inline", out StageCommand command))
            {
                Debug.LogError($"[인라인 명령] 읽지 못했다: [cmd=\"{commandText}\"/]. 실행하지 않는다.");
                continue;
            }

            _markers.Add(LineMarker.ForCommand(command, attribute.Position));
        }
    }

    // 값은 따옴표 없는 이름이면 Yarn이 숫자 · 참거짓으로 읽기도 한다 — 글로 돌려 받는다(표지 이름 12 · a).
    private static bool TryReadValue(MarkupAttribute attribute, string property, out string text)
    {
        text = null;

        if (attribute.Properties == null || !attribute.Properties.TryGetValue(property, out MarkupValue value))
            return false;

        text = value.Type == MarkupValueType.String ? value.StringValue : value.ToString();
        return !string.IsNullOrWhiteSpace(text);
    }

    private void Collect(in ScheduledCommand scheduled)
    {
        _bridge.TryParseInline(scheduled.Command, scheduled.Authored, _batch);
    }

    private void PlayBatch()
    {
        if (_batch.Count == 0)
            return;

        _playbackDriver.PlayInStep(_batch);
        _batch.Clear();
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
