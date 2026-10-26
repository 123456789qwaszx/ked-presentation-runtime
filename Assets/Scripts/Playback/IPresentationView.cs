/// <summary>
/// 재생을 시작할 때 무대 화면을 앞으로 가져오는 자리.
///
/// ScenePresentation은 두 호스트(노드 하나 / 챕터 진행)가 함께 쓴다.
/// 그래서 특정 화면 바인딩 구현을 알지 않고 이 계약만 안다.
/// </summary>
public interface IPresentationView
{
    void GoToPresentationView();
}
