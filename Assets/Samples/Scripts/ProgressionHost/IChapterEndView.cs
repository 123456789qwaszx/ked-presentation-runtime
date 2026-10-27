using Ked.Progression;

/// <summary>
/// 진행 예제의 회차 끝 — 챕터가 끝났을 때 SceneRunner가 부른다(끝 카드). 마지막 장면을 확정(저장)한 뒤다.
/// 선택지 화면(IChapterOptionsView)과 같이 장면 실행이 판단한 자리에서 직접 부른다.
/// 진행 런타임(ProgressionDriver)은 끝을 알리지 않으므로 사본을 고치지 않고 여기서 연다.
///
/// 받는 것: 챕터 정의 · 마지막으로 본 에피소드 · 끝난 상태(확정된 스탯).
/// 화면에 잇는 것은 호스트 어댑터다(KedHostPorts의 KedPlaythroughEndView).
/// </summary>
public interface IChapterEndView
{
    void Show(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state);
}
