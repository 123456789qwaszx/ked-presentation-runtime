using System.Collections.Generic;
using UnityEngine;

// 보이스는 줄 소속 이벤트다 — 새 줄에 들어오면 앞 줄의 보이스를 짧게 줄이며 끝낸다(2026-11-03 소유자 결정).
// 서두르기·선택지는 줄 진입이 아니라 끊지 않는다. 새 줄의 보이스는 그 뒤 PlayCollected에서 나온다.
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

    // 줄의 보이스 = 그 줄 머리에서 실행할 마지막 voice. 뒤에 stop_voice가 있으면 없다.
    // 키 없이 클립만 든 스펙은 기록할 수 없어 빠진다.
    public static string FindLineVoiceKey(IReadOnlyList<CommandSpecBase> lineSpecs)
    {
        string key = null;

        if (lineSpecs == null)
            return null;

        for (int i = 0; i < lineSpecs.Count; i++)
        {
            switch (lineSpecs[i])
            {
                case PlayVoiceCommandSpec play:
                    key = string.IsNullOrWhiteSpace(play.clipKey) ? null : play.clipKey;
                    break;
                case StopVoiceCommandSpec:
                    key = null;
                    break;
            }
        }

        return key;
    }

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
