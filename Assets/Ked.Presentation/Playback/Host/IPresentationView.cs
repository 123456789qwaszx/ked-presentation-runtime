/// <summary>
/// 재생을 시작할 때 무대 화면을 앞으로 가져오는 자리.
///
/// 재생 본체(Playback 어셈블리)는 UI를 모른다 — 그래서 화면 쪽(예제는 KedUIFlow)이 이 계약을 구현해 조립에 넘긴다.
/// </summary>
public interface IPresentationView
{
    void GoToPresentationView();
}
