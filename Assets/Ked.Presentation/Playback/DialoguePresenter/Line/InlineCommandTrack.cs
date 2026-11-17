using System.Collections.Generic;
using System.Threading;
using Ked.Presentation.Core;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

// 대사 줄의 자리 — 글의 표지 [beat=a/]에 줄 앞의 <<at a 명령>>을 잇는다. 타자기가 그 글자를 드러내기 직전에 실행한다.
// docs/PLAN_line-beat.md §3 · §5.
//
//   <<face s2 05>>                     머리 — 대사와 함께(StepHead)
//   <<at a face_swap s2 03 0.3s>>      표지 a
//   <<at end emoji s2 sweat>>          타자기 끝
//   세이나: 아, 음…… [beat=a/]이건 비밀인데.
//
// - 자리는 언어마다(글), 명령은 모든 언어에 하나(<<at>>). 순서는 코어 LineBeatSchedule이 정한다 — 폴드와 같은 함수다.
// - 옛 [cmd="명령"/]도 이행 동안 받는다(L5에서 걷는다). 같은 목록에 글 순서로 선다.
// - 새 스텝이 아니다 — 줄 머리 명령과 같은 스텝에 덧붙는다(YarnPlaybackDriver.PlayInStep).
// - 명령 글은 닿았을 때 푼다 — 대상 슬롯은 그때의 무대로 판정한다.
//
// 줄마다 새 목록(Line)이다(I5). 커서 하나가 앞으로만 간다 — 타자기(PlayUpTo) · 줄 끝(PlayRemaining) · 시크가 같은 커서를 지난다(I2).
// 목록은 그 줄을 연 실행 세대(CommandRunScope)를 든다 — 정지 · 되감기로 세대가 바뀌면 남은 것은 버린다.
// 다음 스텝의 머리는 앞 줄의 목록이 닫혔는지 본다(I4 — CloseBeforeNextStep).
public sealed class InlineCommandTrack : IActionMarkupHandler
{
    private readonly YarnCommandBridge _bridge;
    private readonly YarnPlaybackDriver _playbackDriver;

    private readonly List<LineMarker> _markers = new();
    private readonly List<string> _problems = new();
    private readonly List<CommandSpecBase> _batch = new();

    // 지금 줄의 목록 — 타자기의 글자 콜백이 지난다. 앞 줄의 것은 Begin이 갈아 끼운다.
    private Line _current;

    public InlineCommandTrack(YarnCommandBridge bridge, YarnPlaybackDriver playbackDriver)
    {
        _bridge = bridge;
        _playbackDriver = playbackDriver;
    }

    // 줄 하나의 실행 목록. 줄의 흐름(LinePresentationFlow)이 제 줄의 것만 들고 끝낸다 — 늦게 온 정리가 남의 줄을 건드리지 않는다.
    public sealed class Line
    {
        private readonly InlineCommandTrack _track;
        private readonly List<ScheduledCommand> _items;
        private readonly CommandRunScope _scope;
        private int _next;

        internal Line(InlineCommandTrack track, List<ScheduledCommand> items, CommandRunScope scope)
        {
            _track = track;
            _items = items;
            _scope = scope;
        }

        // 모두 실행했거나 버렸다.
        public bool IsClosed => _next >= _items.Count;

        // 이 글자 자리까지의 명령을 실행한다 — 그 글자가 드러나기 직전에.
        public void PlayUpTo(int characterIndex) => Play(characterIndex);

        // 남은 명령을 모두 실행한다 — 타자기 끝(end · 자리를 잃은 것), 다 보이기 전에 넘어간 줄, 시크가 지나가는 줄.
        public void PlayRemaining() => Play(LineBeatSchedule.LineEnd);

        // 끊긴 줄 — 남은 것을 실행하지 않는다. 정지가 기준으로 돌리고 루트부터 다시 접는다.
        public void Discard() => _next = _items.Count;

        internal bool IsLive => ReferenceEquals(_scope, _track._playbackDriver.CurrentScope)
                                && (_scope == null || !_scope.IsCancelled);

        internal int RemainingCount => _items.Count - _next;

        private void Play(int upTo)
        {
            if (IsClosed)
                return;

            // 세대가 바뀌었다(I5) — 앞 장면의 명령은 새 장면에 닿지 않는다.
            if (!IsLive)
            {
                Discard();
                return;
            }

            while (_next < _items.Count && _items[_next].Position <= upTo)
                _track.Collect(_items[_next++]);

            _track.PlayBatch();
        }
    }

    // 줄의 머리 뒤 — 이 줄의 표지와 쌓인 <<at>>을 이어 목록을 짓는다(StepHead.PlayLine이 부른다).
    public Line Begin(MarkupParseResult text)
    {
        _markers.Clear();
        _problems.Clear();

        CollectMarkers(text);

        List<AtCommand> ats = _playbackDriver.TakeCollectedAt();
        List<ScheduledCommand> items = LineBeatSchedule.Build(_markers, ats, _problems);

        // 번역문이 표지를 빠뜨렸거나(R5) 짝이 맞지 않는다(R6) — 명령은 사라지지 않게 줄 끝에 남았다.
        foreach (string problem in _problems)
            Debug.LogWarning($"[줄의 자리] {problem}");

        _current = new Line(this, items, _playbackDriver.CurrentScope);
        return _current;
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

    // 다음 스텝의 머리 전(I4) — 앞 줄의 목록은 "실행됨" 또는 "버려짐"이어야 한다.
    // 열려 있으면: 같은 세대면 오류를 남기고 새 머리보다 먼저 실행한다(늦게 튀어나오지도, 조용히 사라지지도 않게).
    // 세대가 바뀌었으면 버린다 — 정지가 장면을 기준으로 돌렸다.
    public void CloseBeforeNextStep()
    {
        Line previous = _current;
        _current = null;

        if (previous == null || previous.IsClosed)
            return;

        if (!previous.IsLive)
        {
            previous.Discard();
            return;
        }

        Debug.LogError(
            $"[줄의 자리] 앞 줄의 표지 명령 {previous.RemainingCount}개가 줄 끝을 지나지 않았다 — 다음 스텝의 머리보다 먼저 실행한다. 줄의 흐름을 확인할 것.");
        previous.PlayRemaining();
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

    // 타자기의 글자 — 지금 줄의 목록만 지난다. 타자기는 제 실행이 바뀌면 콜백을 멈춘다(EllipsisBreathTypewriter의 runId).
    YarnTask IActionMarkupHandler.OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        _current?.PlayUpTo(currentCharacterIndex);
        return YarnTask.CompletedTask;
    }

    void IActionMarkupHandler.OnLineDisplayComplete() { }

    void IActionMarkupHandler.OnLineWillDismiss() { }
}
