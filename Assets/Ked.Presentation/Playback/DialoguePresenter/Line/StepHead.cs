using Yarn.Markup;

// 스텝의 머리 — 쌓인 명령을 실행하고 그 뒤를 맞춘다. 줄과 무대 스텝이 같이 쓴다(PLAN_stage-step-inline-markup.md §3.1).
// 쌓인 것은 둘로 갈려 온다(docs/PLAN_line-beat.md I1): at 없는 명령은 머리, <<at>>은 그 줄의 표지 목록.
public sealed class StepHead
{
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly PlaybackRuntimeState _playbackState;
    private readonly LineVoiceController _lineVoice;
    private readonly SceneBgmController _sceneBgm;
    private readonly RigBlurIntentSystem _rigBlur;
    private readonly InlineCommandTrack _lineBeats;

    public StepHead(
        YarnPlaybackDriver playbackDriver,
        PlaybackRuntimeState playbackState,
        LineVoiceController lineVoice,
        SceneBgmController sceneBgm,
        RigBlurIntentSystem rigBlur,
        InlineCommandTrack lineBeats)
    {
        _playbackDriver = playbackDriver;
        _playbackState = playbackState;
        _lineVoice = lineVoice;
        _sceneBgm = sceneBgm;
        _rigBlur = rigBlur;
        _lineBeats = lineBeats;
    }

    // 다음 스텝을 기다리는 명령이 있나 — <<at>>만 남아도 스텝이다(붙을 줄이 없으면 무대 스텝이 실행한다).
    public bool HasPending => _playbackDriver.Collected.Count > 0 || _playbackDriver.CollectedAt.Count > 0;

    // 이 스텝이 낼 보이스의 키. 명령을 실행하기 전에 읽는다 — 실행하면 쌓인 것이 비워진다.
    public string FindVoiceKey() => LineVoiceController.FindLineVoiceKey(_playbackDriver.Collected);

    // 대사 줄의 머리 — 머리 명령을 실행하고, 이 줄의 표지 목록을 지어 돌려준다. 목록은 줄의 흐름이 끝까지 들고 간다.
    // snap: 명령이 끝값으로 놓이는 스텝(시크 통과 · 빠른 넘김).
    // seekPassThrough: 시크가 지나가는 스텝 — 장면 BGM은 의도만 쌓는다.
    public InlineCommandTrack.Line PlayLine(MarkupParseResult text, bool snap, bool seekPassThrough)
    {
        PlayHead(snap, seekPassThrough, strayAt: false);
        return _lineBeats.Begin(text);
    }

    // 무대 스텝의 머리(<<step>> · 노드 끝) — 대사가 없다. 남은 <<at>>은 머리 뒤에(R1).
    public void PlayStageStep(bool snap, bool seekPassThrough)
    {
        PlayHead(snap, seekPassThrough, strayAt: true);
    }

    private void PlayHead(bool snap, bool seekPassThrough, bool strayAt)
    {
        // 앞 줄의 표지 목록은 여기서 닫혀 있어야 한다(I4) — 열려 있으면 이 머리보다 먼저 실행하거나(같은 세대) 버린다.
        _lineBeats.CloseBeforeNextStep();

        // 앞 스텝의 서두르기는 여기서 끝난다 — 이 스텝의 명령은 제 속도로 돈다.
        _playbackState.EndStepHurry();

        // 앞 줄의 보이스는 여기서 끝난다. 이 스텝의 보이스는 PlayCollected가 낸다.
        _lineVoice.EndPreviousLine();
        _playbackDriver.PlayCollected();

        if (strayAt)
            _lineBeats.PlayStrayAt();

        // 블러 의도는 머리 커맨드가 다 돈 뒤 살아 있는 리그에 다시 맞춘다 — 그 사이 등장한 리그, size로 바뀐 깊이, cast가 따라간다.
        // 커맨드가 끝값으로 놓인 스텝은 블러도 Snap한다. 시크 통과도 건너뛰지 않는다 —
        // 도착 줄에서 밀린 변화가 한꺼번에 트윈으로 보이지 않게.
        _rigBlur.Reconcile(snap);

        // 장면 BGM은 압축이 아닌 스텝의 머리 커맨드가 다 돈 뒤에 맞춘다.
        if (!seekPassThrough)
            _sceneBgm.SettleAfterLineHead();
    }
}
