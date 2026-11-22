using System.Collections.Generic;
using System.Threading;
using Ked.Presentation.Core;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

// 대사 줄의 앵커 — 글의 표지 [beat=a/]와 줄 앞의 <<at a 명령>> · <<gap a 길이>>를 이어, 그 줄의 앵커를 열리는 차례로 든다.
// docs/PLAN_line-anchor.md §3 · §4 (앞선 계획: docs/PLAN_line-beat.md).
//
//   <<at in fade_in s2>>               앞 앵커 in — 첫 글자 앞의 표지. 대사창이 열리기 전(줄이 차례로 연다)
//   <<gap in 1s>>                      in이 열리고 최소 1초 뒤에 다음 앵커
//   <<at start face s2 05>>            start — 대사창이 열리며
//   <<at a face_swap s2 03 0.3s>>      글 표지 a — 타자기가 그 글자를 드러내기 직전
//   <<at end emoji s2 sweat>>          타자기 끝
//   세이나: [beat=in/]아, 음…… [beat=a/]이건 비밀인데.
//
// - 자리는 언어마다(글), 명령과 간격은 모든 언어에 하나. 차례는 코어 LineBeatSchedule.BuildAnchors가 정한다 — 접힘과 같은 함수다.
// - 관문(AR3): 다음 앵커는 ① 앞 앵커의 명령이 모두 시작되고(기다리는 명령이 끝나고) ② 앞 앵커의 간격이 지난 뒤에 열린다.
//   타자기는 표지 자리에서만 선다 — 그 글자 앞에서 관문을 기다린다. 그래서 글의 길이나 입력이 앵커의 차례를 바꾸지 못한다.
// - 실행기의 새 스텝이 아니다 — 그 줄의 스텝에 덧붙는다(YarnPlaybackDriver.PlayInStep). 좌표도 아니다.
// - 줄 하나의 앵커 목록이다: 줄에 들어갈 때 Begin, 앞 앵커는 TryPlayNextPreAnchor, 대사창이 열리기 전에 PlayStart,
//   글자마다 타자기가 지나고, 줄 끝에 OpenRemainingAsync. 시크가 지나가는 줄은 PlayRemaining(기다리지 않는다).
//   끊긴 줄(run 무효)의 남은 것은 실행하지 않는다 — 다음 Begin이 지운다.
// - 명령 글은 닿았을 때 푼다 — 대상 슬롯은 그때의 무대로 판정한다.
public sealed class InlineCommandTrack : IActionMarkupHandler
{
    private readonly YarnCommandBridge _bridge;
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly ICommandRunScopeProvider _scopeProvider;
    private readonly PlaybackRuntimeState _playbackState;

    // 이 줄의 앵커 — 열리는 차례: 앞 앵커 → start → 글 표지 → end. start · end는 언제나 있다.
    private List<LineAnchor> _anchors = new();

    // start의 자리 — 그 앞이 앞 앵커다.
    private int _startIndex;

    // 커서 둘, 앞으로만 간다 — 앞 앵커의 것과 start 뒤(글 표지 · end)의 것.
    private int _nextPre;
    private int _next;

    // 줄마다 오른다 — 관문을 기다리던 앞 줄의 것이 이 줄의 앵커를 열지 않게.
    private int _lineSerial;

    // 이 줄의 글 서두르기(첫 클릭) — 남은 간격을 없애고, 아직 도는 기다림은 스텝을 서둘러 끝낸다(A12).
    private CancellationToken _hurryToken;

    // 관문의 ② — 마지막으로 연 앵커가 열린 때(실시간 — pause와 같은 시계)와 그 간격.
    private double _openedAt;
    private float _gap;

    private readonly List<BeatMarker> _markers = new();
    private readonly List<string> _problems = new();
    private readonly List<CommandSpecBase> _batch = new();

    public InlineCommandTrack(
        YarnCommandBridge bridge,
        YarnPlaybackDriver playbackDriver,
        ICommandRunScopeProvider scopeProvider,
        PlaybackRuntimeState playbackState)
    {
        _bridge = bridge;
        _playbackDriver = playbackDriver;
        _scopeProvider = scopeProvider;
        _playbackState = playbackState;
    }

    // 줄에 들어갈 때 — 이 줄의 표지와 쌓인 <<at>> · <<gap>>을 이어 앵커를 짓는다. 앞 줄의 남은 것은 지운다.
    public void Begin(MarkupParseResult text, CancellationToken hurryToken)
    {
        _lineSerial++;
        _hurryToken = hurryToken;
        _openedAt = Time.unscaledTimeAsDouble;
        _gap = 0f;

        _markers.Clear();
        _problems.Clear();

        CollectMarkers(text);

        _anchors = LineBeatSchedule.BuildAnchors(
            _markers,
            _playbackDriver.TakeCollectedAt(),
            _playbackDriver.TakeCollectedGaps(),
            _problems);

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

    // 다음 앞 앵커를 연다 — 글의 표지 순서. 더 없으면 false. 관문은 줄(StageStepFlow)이 기다린다.
    public bool TryPlayNextPreAnchor()
    {
        if (_nextPre >= _startIndex)
            return false;

        Open(_anchors[_nextPre++]);
        return true;
    }

    // 방금 연 앵커의 간격이 아직 차지 않았나(AR3 ②) — 앞 앵커를 여는 줄이 묻는다. 압축 · 글 서두르기면 간격은 없다.
    public bool IsGapPending
    {
        get
        {
            if (_gap <= 0f || _hurryToken.IsCancellationRequested)
                return false;

            CommandRunScope scope = _scopeProvider.CurrentScope;

            if (scope == null || scope.IsCancelled || scope.ShouldCompressCommandExecution)
                return false;

            return Time.unscaledTimeAsDouble - _openedAt < _gap;
        }
    }

    // start — 대사창이 열리기 전에. 그 뒤 줄이 블러 · 장면 BGM을 맞춘다(StepHead.Settle).
    public void PlayStart()
    {
        if (_startIndex < _anchors.Count)
            Open(_anchors[_startIndex]);
    }

    // 타자기가 닿지 않은 앵커를 관문(AR3)을 지나 차례로 연다 — 글 끝의 표지, end, 자리를 잃은 것(AR9), 글 칸이 없는 줄.
    // 그 뒤 end의 간격만큼 머문다(AR4 — 넘김을 받기 전의 최소 시간). end의 기다리는 명령은 넘김 문이 본다(AdvanceGate).
    // 다 보이기 전에 넘어간 줄(cancel)은 기다리지 않고 남은 것을 연다 — 끝 상태가 접힘과 같아야 한다.
    // 끊긴 줄(run 무효)은 열지 않는다.
    public async YarnTask OpenRemainingAsync(LinePresentationRun run, CancellationToken cancel)
    {
        int serial = _lineSerial;

        while (serial == _lineSerial && run.IsValid && _next < _anchors.Count)
        {
            await WaitGateAsync(serial, cancel);

            if (serial != _lineSerial || !run.IsValid)
                return;

            Open(_anchors[_next++]);
        }

        while (serial == _lineSerial && run.IsValid && !cancel.IsCancellationRequested && IsGapPending)
            await YarnTask.Yield();
    }

    // 남은 앵커를 기다리지 않고 모두 연다 — 시크가 지나가는 줄(명령이 압축된다).
    public void PlayRemaining()
    {
        while (_next < _anchors.Count)
            Open(_anchors[_next++]);
    }

    // 노드 끝의 스텝, 남은 at 없는 명령 뒤 — <<at>> 다음에 대사 줄이 없었다(AR10). 사라지지 않게, 그 명령보다 앞서지 않게 쓴 순서로 덧붙인다.
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

    // 노드 끝 — <<gap>> 다음에 대사 줄이 없었다(AR10). 붙을 앵커가 없어 버린다.
    public void DropStrayGaps()
    {
        int count = _playbackDriver.TakeCollectedGaps().Count;

        if (count > 0)
            Debug.LogWarning($"[줄의 자리] <<gap>> {count}개 다음에 대사 줄이 없다(노드 끝) — 붙을 앵커가 없어 버린다.");
    }

    // 앵커 하나를 연다 — 그 명령을 풀어 이 줄의 스텝에 덧붙이고, 관문의 시계를 여기서부터 다시 잰다.
    private void Open(LineAnchor anchor)
    {
        foreach (AtCommand at in anchor.Commands)
            _bridge.TryParseInline(at.Command, at.ToString(), _batch);

        PlayBatch();

        _openedAt = Time.unscaledTimeAsDouble;
        _gap = anchor.Gap;
    }

    private void PlayBatch()
    {
        if (_batch.Count == 0)
            return;

        _playbackDriver.PlayInStep(_batch);
        _batch.Clear();
    }

    // 관문(AR3) — 다음 앵커를 열어도 되나.
    //   ① 앞 앵커의 명령이 모두 시작됐다 — 이 줄에서 기다리는 명령(pause · screen_cover)을 실행했을 때만 본다.
    //      글을 서두른 줄(첫 클릭)은 아직 도는 기다림을 스텝 서두르기로 끝낸다(A12) — 차례는 그대로다.
    //   ② 앞 앵커의 간격이 지났다 — 압축(빨리 넘기기 · 시크 · 서두른 스텝)과 글 서두르기에서는 없다.
    // 끊기거나 넘어간 줄(cancel)은 기다리지 않는다 — 열지 말지는 부른 쪽이 정한다.
    private bool IsGateOpen(CancellationToken cancel)
    {
        if (cancel.IsCancellationRequested)
            return true;

        CommandRunScope scope = _scopeProvider.CurrentScope;

        if (scope == null || scope.IsCancelled)
            return true;

        if (_playbackDriver.UnitHasWaits && scope.CommandIsPlaying)
        {
            if (_hurryToken.IsCancellationRequested && !scope.ShouldCompressCommandExecution)
                _playbackState.HurryStep();

            return false;
        }

        return !IsGapPending;
    }

    private async YarnTask WaitGateAsync(int serial, CancellationToken cancel)
    {
        while (serial == _lineSerial && !IsGateOpen(cancel))
            await YarnTask.Yield();
    }

    // 이 글자 자리까지의 앵커를 연다 — 그 글자가 드러나기 직전에, 앵커마다 관문을 지나서.
    private async YarnTask OpenUpToAsync(int characterIndex, CancellationToken cancel)
    {
        int serial = _lineSerial;

        while (serial == _lineSerial && _next < _anchors.Count && _anchors[_next].Position <= characterIndex)
        {
            await WaitGateAsync(serial, cancel);

            // 끊기거나 넘어간 줄 — 여기서는 열지 않는다. 넘어간 줄의 남은 것은 줄 끝(OpenRemainingAsync)이 연다.
            if (serial != _lineSerial || cancel.IsCancellationRequested)
                return;

            Open(_anchors[_next++]);
        }
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

    void IActionMarkupHandler.OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

    void IActionMarkupHandler.OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }

    YarnTask IActionMarkupHandler.OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        // 이 글자에 닿은 앵커가 없다 — 거의 모든 글자가 그렇다.
        if (_next >= _anchors.Count || _anchors[_next].Position > currentCharacterIndex)
            return YarnTask.CompletedTask;

        return OpenUpToAsync(currentCharacterIndex, cancellationToken);
    }

    void IActionMarkupHandler.OnLineDisplayComplete() { }

    void IActionMarkupHandler.OnLineWillDismiss() { }
}
