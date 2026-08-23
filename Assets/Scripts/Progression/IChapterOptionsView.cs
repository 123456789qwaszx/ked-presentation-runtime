using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;

/// <summary>
/// 에피소드 선택지 화면.
/// VNOptionsPresenter는 대본 안의 분기를 그림.
/// 여기 오는 것은 진행 층의 간선.
/// 고르면 스탯이 커밋되고 에피소드가 넘어간다.
/// 같은 프리팹을 재사용해도 같은 프레젠터는 아님.
///
/// 배열 순서가 곧 화면 순서. 코어가 이미 정렬 주기 때문.
/// </summary>
public interface IChapterOptionsView
{
    /// <param name="options">
    /// 보이는 것 전부. 잠긴 것도 들어 있음.
    /// HideWhenLocked가 걸린 것은 코어가 이미 뺀 상태.
    /// </param>
    /// <param name="hiddenCount">
    /// 숨겨진 개수. 로그·디버그용.
    /// </param>
    /// <returns>고른 것의 <paramref name="options"/> 안 인덱스.</returns>
    Task<int> ShowAsync(IReadOnlyList<ResolvedOption> options, int hiddenCount);

    /// <summary>
    /// 드라이버가 멈춘다. 기다리던 <see cref="ShowAsync"/>를 풀어 주고 화면을 접는다.
    /// 기다리는 중이 아니면 아무 일도 없다.
    /// </summary>
    void Cancel();
}
