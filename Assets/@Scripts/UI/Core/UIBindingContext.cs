using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIBindingContext : IDisposable
{
    private readonly Dictionary<UIBase, List<Action>> _cleanupByOwner = new();

    public void Bind<T>(T owner, Action<T> bind, Action<T> unbind)
        where T : UIBase
    {
        bind(owner);
        AddCleanup(owner, () => unbind(owner));
    }

    private void AddCleanup(UIBase owner, Action cleanup)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out var cleanups))
        {
            cleanups = new List<Action>();
            _cleanupByOwner[owner] = cleanups;
        }

        cleanups.Add(cleanup);
    }

    public void Unbind(UIBase owner)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out var cleanups))
            return;

        RunCleanups(cleanups);
        _cleanupByOwner.Remove(owner);
    }

    public void UnbindAll()
    {
        foreach (var kv in _cleanupByOwner)
            RunCleanups(kv.Value);

        _cleanupByOwner.Clear();
    }

    private static void RunCleanups(List<Action> cleanups)
    {
        for (int i = cleanups.Count - 1; i >= 0; i--)
        {
            try { cleanups[i]?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    public void Dispose()
    {
        UnbindAll();
    }
}