using System;

namespace Ked.Progression
{
    // 챕터 하나가 끝났을 때 무엇이 다음인가. 엔딩키는 지금 노드에서 곧장 읽는다.
    //
    // 상태를 만들지도 옮기지도 않는다 — "엔딩" 또는 "막다른 곳"만 낸다.
    public static class ScenarioTransition
    {
        public static ScenarioAdvance Resolve(ChapterProgression chapter, ProgressionState state)
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode node))
            {
                throw new ArgumentException(
                    $"지금 에피소드 '{state.CurrentEpisodeId}'가 챕터 '{chapter.ChapterId}'에 없다.",
                    nameof(state));
            }

            return node.IsEndingCandidate
                ? ScenarioAdvance.Ended(node.EndingKey)
                : ScenarioAdvance.DeadEnd();
        }
    }
}
