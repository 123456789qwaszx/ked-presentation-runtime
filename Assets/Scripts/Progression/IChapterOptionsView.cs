using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;

/// <summary>
/// 에피소드 진행 선택지 화면.
/// Yarn 내부 분기와 달리 Progression 계층의 간선을 표시한다.
/// </summary>
public interface IChapterOptionsView
{
    /// <param name="options">
    /// 화면에 표시할 선택지.
    /// 잠긴 선택지도 포함하며, 표시 조건에서 제외된 항목은 이미 빠져 있다.
    /// </param>
    /// <param name="hiddenCount">
    /// 표시 조건으로 숨겨진 선택지 수. 로그·디버그용.
    /// </param>
    /// <returns>
    /// 선택된 항목의 <paramref name="options"/> 내 인덱스.
    /// </returns>
    Task<int> ShowAsync(
        IReadOnlyList<ResolvedOption> options,
        int hiddenCount);

    /// <summary>
    /// 현재 진행 선택 대기를 취소하고 화면을 닫는다.
    /// 기다리는 중이 아니면 아무 일도 하지 않는다.
    /// </summary>
    void Cancel();
}