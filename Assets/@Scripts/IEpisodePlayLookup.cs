using System.Collections.Generic;

public interface IEpisodePlayLookup
{
    bool TryGetEpisode(string episodeId, out EpisodeSpec episode);
    bool TryGetChapter(int chapterId, out ChapterSpec chapter);
    IReadOnlyList<int> GetChapterIds();
}