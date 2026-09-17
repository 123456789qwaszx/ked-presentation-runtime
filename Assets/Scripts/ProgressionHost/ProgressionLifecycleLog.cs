using System.Collections.Generic;
using Ked.Progression;
using UnityEngine;

// 진행이 실제로 어떤 경계를 지났는지 관찰만 한다.
//
// ⚠ 여기서 저장하지 않는다. 저장은 ProgressionSaveBridge의 일이다.
//   둘을 다시 하나로 합치지 않는다 — 실패의 의미가 다르다.
//   관찰자가 던지는 예외는 진행을 멈출 이유가 되지 못하므로, 여기서는 아무것도 판정하지 않는다.
//
// SceneRunner는 Enter/Commit 양쪽에서 저장을 먼저, 이 관찰자를 나중에 부른다.
// 따라서 저장이 실패하면 아래 Commit/Exit 로그는 아예 나오지 않는다.
public sealed class ProgressionLifecycleLog : IProgressionReporter
{
    public void ReportChapterEntered(string chapterId, ChapterState state) =>
        Debug.Log($"[진행][챕터] 진입 — {chapterId} @ {state.CurrentEpisodeId}");

    public void ReportChapterExited(string chapterId, ChapterState state) =>
        Debug.Log($"[진행][챕터] 종료 — {chapterId} @ {state.CurrentEpisodeId}");

    public void ReportSceneEntered(string chapterId, string sceneId, ChapterState entryState) =>
        Debug.Log($"[진행][장면] 진입 — {sceneId} @ {entryState.CurrentEpisodeId}");

    public void ReportSceneCommitted(
        string chapterId,
        string sceneId,
        IReadOnlyList<CommittedChoice> choices,
        IReadOnlyList<string> watchedEpisodeIds,
        ChapterState state) =>
        Debug.Log(
            $"[진행][장면] 확정 — {sceneId}, 선택 {choices.Count}개, " +
            $"시청 {watchedEpisodeIds.Count}개 → {state.CurrentEpisodeId}");

    public void ReportSceneExited(string chapterId, string sceneId, ChapterState committedState) =>
        Debug.Log($"[진행][장면] 종료 — {sceneId} → {committedState.CurrentEpisodeId}");

    // 에피소드 경계는 한 장면에서도 여러 번 지나므로 기본 로그로 남기지 않는다.
    // 필요하면 여기에서만 켠다 — 진행 판정에는 영향이 없다.
    public void ReportEpisodeEntered(string chapterId, string sceneId, EpisodeNode episode)
    {
    }

    public void ReportEpisodeExited(string chapterId, string sceneId, EpisodeNode episode)
    {
    }
}
