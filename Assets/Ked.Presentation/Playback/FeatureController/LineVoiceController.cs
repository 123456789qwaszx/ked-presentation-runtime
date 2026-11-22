using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// 보이스는 줄 소속 이벤트다 — 새 줄에 들어오면 앞 줄의 보이스를 짧게 줄이며 끝낸다.
// 서두르기·선택지는 줄 진입이 아니라 끊지 않는다. 새 줄의 보이스는 그 뒤 start의 명령에서 나온다.
public sealed class LineVoiceController
{
    private const float LineEndFadeSeconds = 0.06f;

    private readonly VoicePlayer _voice;

    private AudioClip _replayClip;

    public LineVoiceController(VoicePlayer voice)
    {
        _voice = voice;
    }

    // 앞 줄 보이스는 줄 진입 때 끝나므로, 표시가 끝난 뒤 흐르는 보이스는 이 줄 것이다.
    public bool IsPlaying => _voice != null && _voice.IsPlaying;

    // 백로그에서 다시 튼 보이스가 아직 흐르는가. 끝나거나, 멈추거나, 다른 보이스가 덮거나, 줄이 바뀌면 false.
    public bool IsReplaying =>
        _replayClip != null && IsPlaying && _voice.CurrentClip == _replayClip;

    public void EndPreviousLine()
    {
        _replayClip = null;
        _voice?.FadeOut(LineEndFadeSeconds);
    }

    // 줄의 보이스 = 그 줄의 start에서 실행할 마지막 voice(<<at start voice 키>>). 뒤에 stop_voice가 있으면 없다.
    // 다른 앵커의 voice는 줄의 보이스가 아니다 — 줄 가운데서 트는 소리다.
    public static string FindLineVoiceKey(IReadOnlyList<AtCommand> lineAts)
    {
        string key = null;

        if (lineAts == null)
            return null;

        for (int i = 0; i < lineAts.Count; i++)
        {
            if (lineAts[i].Beat != LineBeatSchedule.StartBeat)
                continue;

            StageCommand command = lineAts[i].Command;

            if (command.Name == VoiceCommand)
                key = command.Arg(0);
            else if (command.Name == StopVoiceCommand)
                key = null;
        }

        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    private const string VoiceCommand = "voice";
    private const string StopVoiceCommand = "stop_voice";

    // 백로그 다시 듣기. 같은 채널이라 지금 흐르는 보이스를 덮는다.
    public bool Replay(string voiceKey)
    {
        if (string.IsNullOrWhiteSpace(voiceKey))
            return false;

        if (!ResourcesAudioClipResolver.TryResolve(voiceKey, out AudioClip clip))
        {
            Debug.LogWarning($"[백로그] 보이스 '{voiceKey}'를 Resources에서 찾지 못했다.");
            return false;
        }

        _voice.Play(clip);
        _replayClip = clip;
        return true;
    }

    public void StopReplay()
    {
        if (IsReplaying)
            _voice.FadeOut(LineEndFadeSeconds);

        _replayClip = null;
    }
}
