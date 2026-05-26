using System.Collections.Generic;
using UnityEngine;

public interface IEpisodePlayLookup
{
    bool TryGetEpisode(string episodeId, out EpisodeSpec episode);
    bool TryGetChapter(int chapterId, out ChapterSpec chapter);
    IReadOnlyList<int> GetChapterIds();
}

public interface IEpisodeProgress
{
    bool IsEpisodeUnlocked(string episodeId);
    bool IsEpisodeCompleted(string episodeId);

    PlayerStateSnapshot GetPlayerStateSnapshot();

    PlayerState State { get; }

    void MarkEpisodeCleared(string episodeId);
    void MarkEpisodeEnding(string ownerEpisodeId, string endingEpisodeId);

    void Unlock(string episodeId);
}

public sealed class EpisodeProgressManager : IEpisodeProgress
{
    public PlayerState State { get; }

    private readonly IEpisodePlayLookup _lookup;

    private readonly bool _unlockBranchesOnClear;
    private readonly bool _log;

    public EpisodeProgressManager(
        IEpisodePlayLookup lookup,
        PlayerState initialState = null,
        IEnumerable<string> initialUnlocked = null,
        bool unlockBranchesOnClear = true,
        bool enableLog = true)
    {
        _lookup = lookup;
        _unlockBranchesOnClear = unlockBranchesOnClear;
        _log = enableLog;

        State = initialState ?? PlayerState.CreateNew();

        if (initialUnlocked != null)
        {
            foreach (string id in initialUnlocked)
                Unlock(id);
        }
    }

    public bool IsEpisodeUnlocked(string episodeId)
    {
        string id = Normalize(episodeId);

        if (string.IsNullOrEmpty(id))
            return false;

        return State.UnlockedEpisodes.Contains(id);
    }

    public bool IsEpisodeCompleted(string episodeId)
    {
        string id = Normalize(episodeId);

        if (string.IsNullOrEmpty(id))
            return false;

        return State.ClearedEpisodes.Contains(id);
    }

    public PlayerStateSnapshot GetPlayerStateSnapshot()
    {
        return State.Snapshot();
    }

    public void Unlock(string episodeId)
    {
        string id = Normalize(episodeId);

        if (string.IsNullOrEmpty(id))
            return;

        bool wasNew = State.UnlockedEpisodes.Add(id);

        if (wasNew && _log)
            Debug.Log($"[Progress] Unlocked: {id}");
    }

    public void MarkEpisodeCleared(string episodeId)
    {
        string id = Normalize(episodeId);

        if (string.IsNullOrEmpty(id))
            return;

        bool firstClear = State.ClearedEpisodes.Add(id);

        if (firstClear && _log)
            Debug.Log($"[Progress] Cleared: {id}");

        if (_lookup == null)
        {
            Debug.LogWarning($"[Progress] Lookup is null. Cannot unlock next/branches for '{id}'.");
            return;
        }

        if (!_lookup.TryGetEpisode(id, out EpisodeSpec episode) || episode == null)
        {
            Debug.LogError(
                $"[Progress] Invalid episodeId '{id}'. " +
                "Expected an exact catalog key."
            );
            return;
        }

        UnlockNextGuaranteed(id, episode);

        if (_unlockBranchesOnClear)
            UnlockBranches(episode);
    }

    public void MarkEpisodeEnding(string ownerEpisodeId, string endingEpisodeId)
    {
        string endingId = Normalize(endingEpisodeId);

        if (string.IsNullOrEmpty(endingId))
            return;

        State.SeenEpisodeEndings.Add(endingId);
        State.ClearedEpisodes.Add(endingId);

        string ownerId = Normalize(ownerEpisodeId);

        if (!string.IsNullOrEmpty(ownerId))
            State.ClearedEpisodes.Add(ownerId);

        if (_log)
            Debug.Log($"[Progress] Ending seen: {endingId} (owner={ownerId})");
    }

    private void UnlockNextGuaranteed(string clearedId, EpisodeSpec episode)
    {
        if (episode == null)
            return;

        if (string.IsNullOrEmpty(episode.next))
            return;

        string nextId = Normalize(episode.next);

        if (string.IsNullOrEmpty(nextId) || nextId == clearedId)
        {
            Debug.LogWarning($"[Progress] Invalid next for '{clearedId}': '{episode.next}'");
            return;
        }

        Unlock(nextId);

        if (_log)
            Debug.Log($"[Progress] {clearedId} -> unlocked next: {nextId}");
    }

    private void UnlockBranches(EpisodeSpec episode)
    {
        if (episode == null)
            return;

        if (!string.IsNullOrEmpty(episode.branchUpperTo))
            Unlock(episode.branchUpperTo);

        if (!string.IsNullOrEmpty(episode.branchMiddleTo))
            Unlock(episode.branchMiddleTo);

        if (!string.IsNullOrEmpty(episode.branchLowerTo))
            Unlock(episode.branchLowerTo);
    }

    private static string Normalize(string id)
    {
        return string.IsNullOrEmpty(id) ? "" : id.Trim();
    }
}