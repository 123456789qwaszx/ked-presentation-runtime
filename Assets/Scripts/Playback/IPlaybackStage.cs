using System.Threading.Tasks;

/// <summary>
/// 재생 단위 하나에 필요한 무대 쪽 동작 넷. ScenePresentation이 구현한다.
///
/// 이 넷을 부르는 순서는 PlaybackUnit이 소유한다. 호스트는 이 계약을 직접 부르지 않는다.
/// 순서를 Unity 없이 검증할 수 있도록 무대(Yarn·Stage·Scope)를 이 계약 뒤에 둔다.
/// </summary>
public interface IPlaybackStage
{
    /// <summary>단위를 연다 — 선택 기록을 비우고 무대·스코프를 새로 시작한다.</summary>
    void BeginScene();

    /// <summary>Yarn 노드 하나를 끝까지(또는 정지까지) 재생한다.</summary>
    Task PlayEpisodeAsync(string nodeName);

    /// <summary>같은 단위를 루트부터 다시 달리기 직전 — 무대를 비우고 스코프를 다시 연다. 선택 기록은 지우지 않는다.</summary>
    void PrepareReplay();

    /// <summary>재생을 멈추고 무대를 정리한다. 롤백 기록도 여기서 비워진다.</summary>
    Task StopAsync();
}
