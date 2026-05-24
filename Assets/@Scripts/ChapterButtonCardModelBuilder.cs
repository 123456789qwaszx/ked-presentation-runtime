using System.Collections.Generic;

public sealed class ChapterButtonCardModelBuilder
{
    public ChapterButtonCardModel[] Build(
        IEpisodePlayLookup lookup,
        IEpisodeProgress progress)
    {
        if (lookup == null)
            return new ChapterButtonCardModel[0];

        IReadOnlyList<int> chapterIds = lookup.GetChapterIds();
        ChapterButtonCardModel[] models = new ChapterButtonCardModel[chapterIds.Count];

        for (int i = 0; i < chapterIds.Count; i++)
        {
            int chapterId = chapterIds[i];

            if (!lookup.TryGetChapter(chapterId, out ChapterSpec chapter) || chapter == null)
            {
                models[i] = new ChapterButtonCardModel(
                    chapterId,
                    indexText: chapterId.ToString(),
                    chapterIndexLabel: $"챕터{chapterId}",
                    chapterTitle: $"Chapter {chapterId}",
                    episodeHeading: "",
                    interactable: false,
                    locked: true
                );

                continue;
            }

            string chapterTitle = string.IsNullOrEmpty(chapter.displayName)
                ? $"챕터 {chapterId}"
                : chapter.displayName;

            string heading = BuildChapterHeadingByProgress(chapter, progress);
            bool locked = !IsChapterUnlocked(chapter, progress);

            models[i] = new ChapterButtonCardModel(
                chapterId,
                indexText: chapterId.ToString(),
                chapterIndexLabel: $"챕터{chapterId}",
                chapterTitle: chapterTitle,
                episodeHeading: heading,
                interactable: !locked,
                locked: locked
            );
        }

        return models;
    }

    private static bool IsChapterUnlocked(ChapterSpec chapter, IEpisodeProgress progress)
    {
        if (chapter == null || chapter.episodes == null || progress == null)
            return false;

        for (int i = 0; i < chapter.episodes.Length; i++)
        {
            EpisodeSpec episode = chapter.episodes[i];
            if (episode == null)
                continue;

            if (progress.IsEpisodeUnlocked(episode.episodeId))
                return true;
        }

        return false;
    }

    private static string BuildChapterHeadingByProgress(
        ChapterSpec chapter,
        IEpisodeProgress progress)
    {
        if (chapter == null || chapter.episodes == null || chapter.episodes.Length == 0)
            return "";

        if (progress == null)
            return "";

        EpisodeSpec bestCompletedMaxOrder = null;
        EpisodeSpec firstUnlockedNotCompletedMinOrder = null;
        EpisodeSpec firstUnlockedMinOrder = null;

        for (int i = 0; i < chapter.episodes.Length; i++)
        {
            EpisodeSpec episode = chapter.episodes[i];
            if (episode == null || string.IsNullOrEmpty(episode.episodeId))
                continue;

            string id = episode.episodeId;

            bool unlocked = progress.IsEpisodeUnlocked(id);
            bool completed = unlocked && progress.IsEpisodeCompleted(id);

            if (completed)
            {
                if (bestCompletedMaxOrder == null ||
                    episode.order > bestCompletedMaxOrder.order)
                {
                    bestCompletedMaxOrder = episode;
                }
            }

            if (unlocked && !completed)
            {
                if (firstUnlockedNotCompletedMinOrder == null ||
                    episode.order < firstUnlockedNotCompletedMinOrder.order)
                {
                    firstUnlockedNotCompletedMinOrder = episode;
                }
            }

            if (unlocked)
            {
                if (firstUnlockedMinOrder == null ||
                    episode.order < firstUnlockedMinOrder.order)
                {
                    firstUnlockedMinOrder = episode;
                }
            }
        }

        EpisodeSpec pick =
            bestCompletedMaxOrder ??
            firstUnlockedNotCompletedMinOrder ??
            firstUnlockedMinOrder;

        if (pick == null)
            return "";

        string title = string.IsNullOrEmpty(pick.displayName)
            ? pick.episodeId
            : pick.displayName;

        return $"{pick.order:00} {title}";
    }
}