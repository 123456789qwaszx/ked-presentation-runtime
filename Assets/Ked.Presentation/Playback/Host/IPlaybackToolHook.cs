/// <summary>
/// 조립이 끝난 재생에 붙는 개발 도구(녹화기 · 대조 장치)의 계약.
///
/// 조립 루트(예제: SampleBootstrap)가 AssemblePresentation 끝에서 한 번 부른다 — 첫 줄이 서기 전이다.
/// 도구는 재생을 막지 않는다: 못 서면 이유를 남기고 빠진다(StageEquivalenceHarness와 같은 규약).
/// 그 규약은 구현이 지킨다 — 부르는 쪽은 감싸지 않는다.
/// </summary>
public interface IPlaybackToolHook
{
    void OnPlaybackAssembled(PlaybackToolServices services);
}
