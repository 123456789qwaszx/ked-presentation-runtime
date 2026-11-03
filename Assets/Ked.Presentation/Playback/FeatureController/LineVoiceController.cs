// 보이스는 줄 소속 이벤트다 — 새 줄에 들어오면 앞 줄의 보이스를 짧게 줄이며 끝낸다(2026-11-03 소유자 결정).
// 서두르기·선택지는 줄 진입이 아니라 끊지 않는다. 새 줄의 보이스는 그 뒤 PlayCollected에서 나온다.
public sealed class LineVoiceController
{
    private const float LineEndFadeSeconds = 0.06f;

    private readonly VoicePlayer _voice;

    public LineVoiceController(VoicePlayer voice)
    {
        _voice = voice;
    }

    // 앞 줄 보이스는 줄 진입 때 끝나므로, 표시가 끝난 뒤 흐르는 보이스는 이 줄 것이다.
    public bool IsPlaying => _voice != null && _voice.IsPlaying;

    public void EndPreviousLine() => _voice?.FadeOut(LineEndFadeSeconds);
}
