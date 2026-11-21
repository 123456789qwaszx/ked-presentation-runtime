// 스텝의 머리 — 쌓인 명령을 실행하고 그 뒤를 맞춘다. 줄과 노드 끝의 암묵 스텝이 같이 쓴다.
// 한 단위(줄 · 노드 끝의 스텝)의 차례: Enter → (앞 앵커 TryPlayNextPreBatch …) → Play(머리).
public sealed class StepHead
{
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly PlaybackRuntimeState _playbackState;
    private readonly LineVoiceController _lineVoice;
    private readonly SceneBgmController _sceneBgm;
    private readonly RigBlurIntentSystem _rigBlur;

    public StepHead(
        YarnPlaybackDriver playbackDriver,
        PlaybackRuntimeState playbackState,
        LineVoiceController lineVoice,
        SceneBgmController sceneBgm,
        RigBlurIntentSystem rigBlur)
    {
        _playbackDriver = playbackDriver;
        _playbackState = playbackState;
        _lineVoice = lineVoice;
        _sceneBgm = sceneBgm;
        _rigBlur = rigBlur;
    }

    // 다음 단위를 기다리는 명령이 있나 — 머리 · <<step>>이 닫은 구간 · <<at>>.
    public bool HasPending => _playbackDriver.HasPending;

    // <<step>>이 닫은 구간(옛 표기)이 있나 — 다음 대사 줄의 앞 앵커다(docs/PLAN_line-anchor.md A6).
    public bool HasPreBatch => _playbackDriver.PreBatchCount > 0;

    // 이 줄이 낼 보이스의 키 — 머리의 명령에서 읽는다. 명령을 실행하기 전에 읽는다 — 실행하면 쌓인 것이 비워진다.
    public string FindVoiceKey() => LineVoiceController.FindLineVoiceKey(_playbackDriver.Collected);

    // 단위(줄 · 노드 끝의 스텝)에 들어선다 — 앞 스텝의 서두르기와 앞 줄의 보이스가 여기서 끝난다.
    // 이 단위가 처음 실행하는 묶음이 실행기의 스텝을 연다 — 앞 단위의 남은 전환을 마무리하는 경계는 여기 하나다(A13).
    public void Enter()
    {
        // 앞 스텝의 서두르기는 여기서 끝난다 — 이 단위의 명령은 제 속도로 돈다.
        _playbackState.EndStepHurry();

        // 앞 줄의 보이스는 여기서 끝난다. 이 줄의 보이스는 머리의 PlayCollected가 낸다.
        _lineVoice.EndPreviousLine();

        _playbackDriver.BeginUnit();
    }

    // 다음 앞 앵커(<<step>>이 닫은 구간)를 실행하고 그 뒤를 맞춘다. 더 없으면 false.
    public bool TryPlayNextPreBatch(bool snap, bool seekPassThrough)
    {
        if (!_playbackDriver.TryPlayNextPreBatch())
            return false;

        Settle(snap, seekPassThrough);
        return true;
    }

    // 머리 — 쌓인 명령을 실행하고 그 뒤를 맞춘다.
    // snap: 명령이 끝값으로 놓이는 스텝(시크 통과 · 빠른 넘김).
    // seekPassThrough: 시크가 지나가는 스텝 — 장면 BGM은 의도만 쌓는다.
    public void Play(bool snap, bool seekPassThrough)
    {
        _playbackDriver.PlayCollected();
        Settle(snap, seekPassThrough);
    }

    // 묶음 하나를 실행한 뒤 — 줄의 앵커(<<at>>)가 앞 앵커로 열릴 때도 부른다.
    public void Settle(bool snap, bool seekPassThrough)
    {
        // 블러 의도는 명령이 다 돈 뒤 살아 있는 리그에 다시 맞춘다 — 그 사이 등장한 리그, size로 바뀐 깊이, cast가 따라간다.
        // 커맨드가 끝값으로 놓인 스텝은 블러도 Snap한다. 시크 통과도 건너뛰지 않는다 —
        // 도착 줄에서 밀린 변화가 한꺼번에 트윈으로 보이지 않게.
        _rigBlur.Reconcile(snap);

        // 장면 BGM은 압축이 아닌 스텝의 명령이 다 돈 뒤에 맞춘다.
        if (!seekPassThrough)
            _sceneBgm.SettleAfterLineHead();
    }
}
