using System.Collections.Generic;
using System.Threading;
using Ked.Presentation.Core;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

// 대사 줄의 앵커 — 글의 표지 [beat=a/]와 줄 앞의 <<at a 명령>>을 이어, 그 줄의 앵커를 열리는 차례로 든다.
// docs/PLAN_line-anchor.md §3 · §4 (앞선 계획: docs/PLAN_line-beat.md).
//
//   <<at in fade_in s2>>               앞 앵커 in — 첫 글자 앞의 표지. 대사창이 열리기 전(줄이 차례로 연다)
//   <<face s2 05>>                     머리 — at 없는 명령(옛 표기). start와 같은 때다(StepHead)
//   <<at start emoji s2 note>>         start — 머리 바로 뒤
//   <<at a face_swap s2 03 0.3s>>      글 표지 a — 타자기가 그 글자를 드러내기 직전
//   <<at end emoji s2 sweat>>          타자기 끝
//   세이나: [beat=in/]아, 음…… [beat=a/]이건 비밀인데.
//
// - 자리는 언어마다(글), 명령은 모든 언어에 하나(<<at>>). 차례는 코어 LineBeatSchedule.BuildAnchors가 정한다 — 접힘과 같은 함수다.
// - 실행기의 새 스텝이 아니다 — 그 줄의 스텝에 덧붙는다(YarnPlaybackDriver.PlayInStep). 좌표도 아니다.
// - 줄 하나의 앵커 목록이다: 줄에 들어갈 때 Begin, 앞 앵커는 TryPlayNextPreAnchor, 머리 뒤에 PlayStart,
//   글자마다 PlayUpTo, 줄이 끝나거나 시크가 지나가면 PlayRemaining.
//   끊긴 줄(run 무효)의 남은 것은 실행하지 않는다 — 다음 Begin이 지운다.
// - 명령 글은 닿았을 때 푼다 — 대상 슬롯은 그때의 무대로 판정한다.
public sealed class InlineCommandTrack : IActionMarkupHandler
{
    private readonly YarnCommandBridge _bridge;
    private readonly YarnPlaybackDriver _playbackDriver;

    // 이 줄의 앵커 — 열리는 차례: 앞 앵커 → start → 글 표지 → end. start · end는 언제나 있다.
    private List<LineAnchor> _anchors = new();

    // start의 자리 — 그 앞이 앞 앵커다.
    private int _startIndex;

    // 커서 둘, 앞으로만 간다 — 앞 앵커의 것과 start 뒤(글 표지 · end)의 것.
    private int _nextPre;
    private int _next;

    private readonly List<BeatMarker> _markers = new();
    private readonly List<string> _problems = new();
    private readonly List<CommandSpecBase> _batch = new();

    public InlineCommandTrack(YarnCommandBridge bridge, YarnPlaybackDriver playbackDriver)
    {
        _bridge = bridge;
        _playbackDriver = playbackDriver;
    }

    // 줄에 들어갈 때 — 이 줄의 표지와 쌓인 <<at>>을 이어 앵커를 짓는다. 앞 줄의 남은 것은 지운다.
    public void Begin(MarkupParseResult text)
    {
        _markers.Clear();
        _problems.Clear();

        CollectMarkers(text);

        _anchors = LineBeatSchedule.BuildAnchors(_markers, _playbackDriver.TakeCollectedAt(), null, _problems);

        _startIndex = 0;

        while (_startIndex < _anchors.Count && _anchors[_startIndex].Kind == LineAnchorKind.Pre)
            _startIndex++;

        _nextPre = 0;
        _next = _startIndex + 1;

        // 번역문이 표지를 빠뜨렸거나(AR9) 짝이 맞지 않는다(AR7) — 명령은 사라지지 않게 줄 끝에 남았다.
        foreach (string problem in _problems)
            Debug.LogWarning($"[줄의 자리] {problem}");
    }

    // 아직 열지 않은 앞 앵커가 있나 — 첫 글자 앞의 표지(A5).
    public bool HasPreAnchors => _nextPre < _startIndex;

    // 다음 앞 앵커를 연다 — 글의 표지 순서. 더 없으면 false.
    // 앞 앵커에서는 기다려도 된다(pause · screen_cover) — 대사창이 열리기 전이라 붙잡을 글이 없다.
    public bool TryPlayNextPreAnchor()
    {
        if (_nextPre >= _startIndex)
            return false;

        Collect(_anchors[_nextPre++], allowWait: true);
        PlayBatch();
        return true;
    }

    // start — 머리 명령(at 없는 것) 바로 뒤, 대사창이 열리기 전에.
    public void PlayStart()
    {
        if (_startIndex >= _anchors.Count)
            return;

        Collect(_anchors[_startIndex], allowWait: false);
        PlayBatch();
    }

    // 이 글자 자리까지의 앵커를 연다 — 그 글자가 드러나기 직전에.
    public void PlayUpTo(int characterIndex)
    {
        while (_next < _anchors.Count && _anchors[_next].Position <= characterIndex)
            Collect(_anchors[_next++], allowWait: false);

        PlayBatch();
    }

    // 남은 앵커를 모두 연다 — 타자기 끝(end · 자리를 잃은 것), 다 보이기 전에 넘어간 줄, 시크가 지나가는 줄.
    public void PlayRemaining()
    {
        while (_next < _anchors.Count)
            Collect(_anchors[_next++], allowWait: false);

        PlayBatch();
    }

    // 노드 끝의 스텝, 머리 뒤 — <<at>> 다음에 대사 줄이 없었다(AR10). 사라지지 않게, 머리보다 앞서지 않게 쓴 순서로 덧붙인다.
    public void PlayStrayAt()
    {
        if (_playbackDriver.CollectedAt.Count == 0)
            return;

        List<AtCommand> ats = _playbackDriver.TakeCollectedAt();

        Debug.LogWarning(
            $"[줄의 자리] <<at>> {ats.Count}개 다음에 대사 줄이 없다(노드 끝) — " +
            $"붙을 줄이 없어 남은 명령 뒤에 실행한다: {ats[0]}{(ats.Count > 1 ? " …" : "")}");

        foreach (AtCommand at in ats)
            _bridge.TryParseInline(at.Command, at.ToString(), _batch);

        PlayBatch();
    }

    // 글 순서의 자리 표지 [beat=a/]. Yarn이 마커를 글 순서로 준다. 자리는 화자 이름을 뺀 글의 것이다 — 자리 0이 첫 글자 앞이다.
    private void CollectMarkers(MarkupParseResult text)
    {
        List<MarkupAttribute> attributes = text.Attributes;

        if (attributes == null)
            return;

        for (int i = 0; i < attributes.Count; i++)
        {
            MarkupAttribute attribute = attributes[i];

            if (attribute.Name != LineBeatSchedule.MarkerName)
                continue;

            if (TryReadName(attribute, out string beat))
                _markers.Add(new BeatMarker(beat, attribute.Position));
            else
                Debug.LogWarning("[줄의 자리] [beat=이름/]에 이름이 없다 — 무시한다.");
        }
    }

    // 따옴표 없는 이름은 Yarn이 숫자 · 참거짓으로 읽기도 한다 — 글로 돌려 받는다(표지 이름 12 · a).
    private static bool TryReadName(MarkupAttribute attribute, out string name)
    {
        name = null;

        if (attribute.Properties == null || !attribute.Properties.TryGetValue(LineBeatSchedule.MarkerName, out MarkupValue value))
            return false;

        name = value.Type == MarkupValueType.String ? value.StringValue : value.ToString();
        return !string.IsNullOrWhiteSpace(name);
    }

    private void Collect(LineAnchor anchor, bool allowWait)
    {
        foreach (AtCommand at in anchor.Commands)
            _bridge.TryParseInline(at.Command, at.ToString(), _batch, allowWait);
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
