using System;
using UnityEngine;

public sealed class EpisodeProgressRuntime : MonoBehaviour, IEpisodeProgress
{
    [Header("Initial State")]
    [SerializeField] private string[] initialUnlockedEpisodes =
    {
        "main05.01",
        "main05.02",
        "branch05.02U",
        "sub05.02A",
        "ending05.good"
    };

    [SerializeField] private string[] initialCompletedEpisodes =
    {
        "main05.01"
    };

    [Header("Initial Metrics")]
    [SerializeField] private int intuition = 30;
    [SerializeField] private int analysis = 35;
    [SerializeField] private int chaos = 30;

    [Header("Policy")]
    [SerializeField] private bool unlockBranchesOnClear = true;
    [SerializeField] private bool enableLog = true;

    private EpisodeProgressManager _manager;

    public PlayerState State
    {
        get
        {
            EnsureInitialized(null);
            return _manager.State;
        }
    }

    public void Initialize(IEpisodePlayLookup lookup)
    {
        EnsureInitialized(lookup);
    }

    public bool IsEpisodeUnlocked(string episodeId)
    {
        EnsureInitialized(null);
        return _manager.IsEpisodeUnlocked(episodeId);
    }

    public bool IsEpisodeCompleted(string episodeId)
    {
        EnsureInitialized(null);
        return _manager.IsEpisodeCompleted(episodeId);
    }

    public PlayerStateSnapshot GetPlayerStateSnapshot()
    {
        EnsureInitialized(null);
        return _manager.GetPlayerStateSnapshot();
    }

    public void MarkEpisodeCleared(string episodeId)
    {
        EnsureInitialized(null);
        _manager.MarkEpisodeCleared(episodeId);
    }

    public void MarkEpisodeEnding(string ownerEpisodeId, string endingEpisodeId)
    {
        EnsureInitialized(null);
        _manager.MarkEpisodeEnding(ownerEpisodeId, endingEpisodeId);
    }

    public void Unlock(string episodeId)
    {
        EnsureInitialized(null);
        _manager.Unlock(episodeId);
    }

    private void EnsureInitialized(IEpisodePlayLookup lookup)
    {
        if (_manager != null)
            return;

        PlayerState initialState = PlayerState.CreateNew();

        initialState.Intuition = intuition;
        initialState.Analysis = analysis;
        initialState.Chaos = chaos;

        _manager = new EpisodeProgressManager(
            lookup,
            initialState,
            initialUnlockedEpisodes,
            unlockBranchesOnClear,
            enableLog
        );

        ApplyCompletedEpisodes();
    }

    private void ApplyCompletedEpisodes()
    {
        if (initialCompletedEpisodes == null)
            return;

        for (int i = 0; i < initialCompletedEpisodes.Length; i++)
        {
            string id = initialCompletedEpisodes[i];

            if (string.IsNullOrEmpty(id))
                continue;

            _manager.State.ClearedEpisodes.Add(id.Trim());
        }
    }
}