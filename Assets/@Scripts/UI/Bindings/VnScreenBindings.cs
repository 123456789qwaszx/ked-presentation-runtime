using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VnScreenBindings : IDisposable
{
    private static UIManager UI => UIManager.Instance;

    private readonly Dictionary<UIBase, List<Action>> _cleanupByOwner = new();

    private readonly VNSaveLoadSystem _vnSaveLoadSystem;

    private UIBase _boundMain;

    private EpisodePlayer _episodePlayer;

    public VnScreenBindings(VNSaveLoadSystem vnSaveLoadSystem)
    {
        _vnSaveLoadSystem = vnSaveLoadSystem;
    }

    public void AttachEpisodePlayer(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }

    private void BindMain<T>(T owner, Action<T> bind)
        where T : UIBase
    {
        if (!owner)
            return;

        if (_boundMain != null && _boundMain != owner)
            Unbind(_boundMain);

        Unbind(owner);

        _boundMain = owner;
        bind(owner);
    }

    private void Bind<T>(T owner, Action<T> bind)
        where T : UIBase
    {
        if (!owner)
            return;

        Unbind(owner);
        bind(owner);
    }

    private void BindEvent<T>(T owner, Action<T> bind, Action<T> unbind)
        where T : UIBase
    {
        if (!owner || bind == null || unbind == null)
            return;

        bind(owner);
        AddCleanup(owner, () => unbind(owner));
    }

    private void AddCleanup(UIBase owner, Action cleanup)
    {
        if (!owner || cleanup == null)
            return;

        if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
        {
            cleanups = new List<Action>();
            _cleanupByOwner[owner] = cleanups;
        }

        cleanups.Add(cleanup);
    }

    private void Unbind(UIBase owner)
    {
        if (!owner)
            return;

        if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
            return;

        RunCleanups(cleanups);
        _cleanupByOwner.Remove(owner);

        if (_boundMain == owner)
            _boundMain = null;
    }

    private void UnbindMain()
    {
        if (_boundMain == null)
            return;

        Unbind(_boundMain);
        _boundMain = null;
    }

    private void UnbindAll()
    {
        foreach (var kv in _cleanupByOwner)
            RunCleanups(kv.Value);

        _cleanupByOwner.Clear();
        _boundMain = null;
    }

    private static void RunCleanups(List<Action> cleanups)
    {
        for (int i = cleanups.Count - 1; i >= 0; i--)
        {
            try
            {
                cleanups[i]?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public void Dispose()
    {
        UnbindAll();
    }
}