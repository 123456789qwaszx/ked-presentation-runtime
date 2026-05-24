using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MainStoryCatalogLookup : IEpisodePlayLookup
{
    private readonly MainStoryCatalogSO _catalog;

    private readonly Dictionary<int, ChapterSpec> _chapterById = new();
    private readonly Dictionary<string, EpisodeSpec> _episodeById = new(StringComparer.Ordinal);
    private readonly List<int> _chapterIds = new();

    private bool _built;

    public MainStoryCatalogLookup(MainStoryCatalogSO catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<int> GetChapterIds()
    {
        BuildIfNeeded();
        return _chapterIds;
    }

    public bool TryGetChapter(int chapterId, out ChapterSpec chapter)
    {
        BuildIfNeeded();

        if (_chapterById.TryGetValue(chapterId, out ChapterSpec found) && found != null)
        {
            chapter = found;
            return true;
        }

        chapter = null;
        return false;
    }

    public bool TryGetEpisode(string episodeId, out EpisodeSpec episode)
    {
        BuildIfNeeded();

        if (string.IsNullOrEmpty(episodeId))
        {
            episode = null;
            return false;
        }

        if (_episodeById.TryGetValue(episodeId, out EpisodeSpec found) && found != null)
        {
            episode = found;
            return true;
        }

        episode = null;
        return false;
    }

    public void BuildIfNeeded()
    {
        if (_built)
            return;

        _chapterById.Clear();
        _episodeById.Clear();
        _chapterIds.Clear();

        if (_catalog == null)
        {
            Debug.LogError("[MainStoryCatalogLookup] Catalog is null.");
            _built = true;
            return;
        }

        if (_catalog.chapters == null)
        {
            Debug.LogError("[MainStoryCatalogLookup] Catalog chapters are null.");
            _built = true;
            return;
        }

        for (int i = 0; i < _catalog.chapters.Length; i++)
        {
            ChapterSpec chapter = _catalog.chapters[i];
            if (chapter == null)
                continue;

            NormalizeChapter(chapter);

            if (_chapterById.TryAdd(chapter.chapterId, chapter))
            {
                _chapterIds.Add(chapter.chapterId);
            }
            else
            {
                Debug.LogWarning(
                    $"[MainStoryCatalogLookup] Duplicate chapterId: {chapter.chapterId}");
            }

            if (chapter.episodes == null)
                continue;

            for (int e = 0; e < chapter.episodes.Length; e++)
            {
                EpisodeSpec episode = chapter.episodes[e];
                if (episode == null)
                    continue;

                NormalizeEpisode(episode);

                if (string.IsNullOrEmpty(episode.episodeId))
                {
                    Debug.LogWarning(
                        $"[MainStoryCatalogLookup] Empty episodeId in chapterId={chapter.chapterId}");
                    continue;
                }

                if (!_episodeById.TryAdd(episode.episodeId, episode))
                {
                    Debug.LogWarning(
                        $"[MainStoryCatalogLookup] Duplicate episodeId: {episode.episodeId}");
                }
            }
        }

        _chapterIds.Sort();
        ValidateLinks();

        _built = true;
    }

    private void ValidateLinks()
    {
        foreach (KeyValuePair<string, EpisodeSpec> kv in _episodeById)
        {
            EpisodeSpec episode = kv.Value;
            if (episode == null)
                continue;

            CheckTarget(episode.episodeId, episode.next, nameof(EpisodeSpec.next));
            CheckTarget(episode.episodeId, episode.branchUpperTo, nameof(EpisodeSpec.branchUpperTo));
            CheckTarget(episode.episodeId, episode.branchMiddleTo, nameof(EpisodeSpec.branchMiddleTo));
            CheckTarget(episode.episodeId, episode.branchLowerTo, nameof(EpisodeSpec.branchLowerTo));
            CheckTarget(episode.episodeId, episode.attachmentLowerTo, nameof(EpisodeSpec.attachmentLowerTo));

            if (!episode.isEnding)
                continue;

            bool hasOutgoing =
                !string.IsNullOrEmpty(episode.next) ||
                !string.IsNullOrEmpty(episode.branchUpperTo) ||
                !string.IsNullOrEmpty(episode.branchMiddleTo) ||
                !string.IsNullOrEmpty(episode.branchLowerTo);

            if (hasOutgoing)
            {
                Debug.LogWarning(
                    $"[MainStoryCatalogLookup] Ending episode should not have outgoing links: {episode.episodeId}");
            }

            if (string.IsNullOrEmpty(episode.endingTitle))
            {
                Debug.LogWarning(
                    $"[MainStoryCatalogLookup] Ending episode missing endingTitle: {episode.episodeId}");
            }
        }
    }

    private void CheckTarget(string fromId, string toId, string fieldName)
    {
        if (string.IsNullOrEmpty(toId))
            return;

        if (!_episodeById.ContainsKey(toId))
        {
            Debug.LogWarning(
                $"[MainStoryCatalogLookup] Broken link: {fromId}.{fieldName} -> '{toId}' not found.");
        }
    }

    private static void NormalizeChapter(ChapterSpec chapter)
    {
        chapter.displayName ??= "";
        chapter.eraText ??= "";
        chapter.episodes ??= Array.Empty<EpisodeSpec>();
    }

    private static void NormalizeEpisode(EpisodeSpec episode)
    {
        episode.episodeId ??= "";
        episode.displayName ??= "";
        episode.yarnStartNode ??= "";
        episode.entryKey ??= "";

        episode.next ??= "";
        episode.branchUpperTo ??= "";
        episode.branchMiddleTo ??= "";
        episode.branchLowerTo ??= "";
        episode.attachmentLowerTo ??= "";

        episode.endingTitle ??= "";
    }
}