/// <summary>
/// 연출 단독 예제의 회차 끝 — 노드를 끝까지 봤을 때 NodePlaythrough가 부른다(끝 카드).
/// 끝내기 요청(타이틀로)으로 끊긴 것은 끝이 아니라 부르지 않는다.
///
/// 끝은 대본 명령이 아니라 회차를 연 쪽이 정한다 — 같은 대본이 진행 예제에서는 에피소드 하나일 뿐이다.
/// 화면에 잇는 것은 호스트 어댑터다(KedHostPorts의 KedPlaythroughEndView).
/// </summary>
public interface IEpisodeEndView
{
    void Show();
}
