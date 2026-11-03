/// <summary>
/// 타이틀 화면이 회차를 여는 두 길. 진행 호스트(ProgressionLauncher)가 구현한다 —
/// 챕터 처음부터 / 마지막으로 본 줄부터(진행 저장).
///
/// 화면은 어느 쪽인지 모른다. 이어할 것이 없으면 처음부터 시작하는 것까지 구현의 몫이다.
/// </summary>
public interface IPlaythroughEntry
{
    /// <summary>처음부터.</summary>
    void StartNew();

    /// <summary>마지막으로 본 줄부터. 이어할 것이 없거나 읽을 수 없으면 처음부터.</summary>
    void Resume();
}
