using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// 스텝의 머리 — 단위(줄 · 노드 끝의 암묵 스텝)에 들어서고, 묶음을 실행한 뒤를 맞춘다.
//   줄                Enter → DropUnanchored → (앵커마다 InlineCommandTrack → Settle)
//   노드 끝의 스텝    Enter → Play(남은 at 없는 명령)
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

    // 다음 단위를 기다리는 명령이 있나 — at 없는 명령 · <<at>>.
    public bool HasPending => _playbackDriver.HasPending;

    // 이 줄이 낼 보이스의 키 — start의 <<at>>에서 읽는다. 줄에 들어서기 전에 읽는다 — 들어서면 앵커가 가져간다.
    public string FindVoiceKey() => LineVoiceController.FindLineVoiceKey(_playbackDriver.CollectedAt);

    // 단위(줄 · 노드 끝의 스텝)에 들어선다 — 앞 스텝의 서두르기와 앞 줄의 보이스가 여기서 끝난다.
    // 이 단위가 처음 실행하는 묶음이 실행기의 스텝을 연다 — 앞 단위의 남은 전환을 마무리하는 경계는 여기 하나다(A13).
    public void Enter()
    {
        // 앞 스텝의 서두르기는 여기서 끝난다 — 이 단위의 명령은 제 속도로 돈다.
        _playbackState.EndStepHurry();

        // 앞 줄의 보이스는 여기서 끝난다. 이 줄의 보이스는 start의 명령이 낸다.
        _lineVoice.EndPreviousLine();

        _playbackDriver.BeginUnit();
    }

    // 줄에 들어선 뒤 — 쌓인 at 없는 명령은 이 줄의 것이 아니다. 원문의 오류로 적고 실행하지 않는다(docs/PLAN_line-anchor.md §9 N5).
    // 줄의 명령은 <<at 앵커 명령 …>>이고, at 없이 쓰는 것은 노드 끝(암묵 스텝)뿐이다.
    public void DropUnanchored()
    {
        if (!_playbackDriver.TryDropUnanchored(out List<StageCommand> dropped))
            return;

        foreach (StageCommand command in dropped)
            Debug.LogError($"[줄의 자리] {YarnCommandTextExtractor.UnanchoredProblem(command)}.");
    }

    // 노드 끝의 암묵 스텝 — 남은 at 없는 명령을 실행하고 그 뒤를 맞춘다.
    // snap: 명령이 끝값으로 놓이는 스텝(시크 통과 · 빠른 넘김).
    // seekPassThrough: 시크가 지나가는 스텝 — 장면 BGM은 의도만 쌓는다.
    public void Play(bool snap, bool seekPassThrough)
    {
        _playbackDriver.PlayCollected();
        Settle(snap, seekPassThrough);
    }

    // 묶음 하나를 실행한 뒤 — 줄의 앞 앵커 · start가 열린 뒤, 노드 끝의 스텝 뒤.
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
