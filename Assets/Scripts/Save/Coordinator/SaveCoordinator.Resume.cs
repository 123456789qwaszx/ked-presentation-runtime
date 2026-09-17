using System;
using System.Collections.Generic;
using Ked.Progression;

public sealed partial class SaveCoordinator
{
    // 현재 Scene을 확정하지 않고, Scene 진입점부터 표시된 Line까지의 재생 계획만 갱신한다.
    public void UpdateResumePoint(
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target)
    {
        if (_playthroughId == null || _active == null)
            throw new InvalidOperationException("활성 회차 없이 이어하기 지점을 저장할 수 없다.");

        if (_currentEntry == null)
            throw new InvalidOperationException("장면 진입 보고 없이 이어하기 지점이 호출됐다.");

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        LocalSaveFile snapshot = _active.Read().Snapshot;

        snapshot.ChapterId = _currentEntry.ChapterId;
        snapshot.CurrentEpisodeId = _currentEntry.EpisodeId;
        snapshot.Stats = new Dictionary<string, int>(
            _currentEntry.Stats,
            StringComparer.Ordinal);
        snapshot.ChapterCompleted = false;
        snapshot.PendingLoad = CreateLoadPlan(path, yarnChoices, target);
        snapshot.PlaySeconds = TotalSeconds;
        snapshot.SavedAtUtc = NowUtc();

        _active.Commit(snapshot);
    }

    private static SavedLoadPlan CreateLoadPlan(
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target)
    {
        var savedPath = new List<SavedChoice>(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            savedPath.Add(new SavedChoice
            {
                FromEpisodeId = path[i].FromEpisodeId,
                OptionIndex = path[i].OptionIndex,
            });
        }

        return new SavedLoadPlan
        {
            Path = savedPath,
            YarnChoices = new List<VNChoiceRecord>(yarnChoices),
            Target = PlaythroughSession.Copy(target),
        };
    }
}
