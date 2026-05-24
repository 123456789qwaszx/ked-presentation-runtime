using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIBindingContext : IDisposable
{
    private readonly Dictionary<UIBase, List<Action>> _cleanupByOwner = new();
    
    public void Assign<T>(T owner, Action<T> apply, Action<T> reset)
        where T : UIBase
    {
        apply(owner);

        if (!_cleanupByOwner.TryGetValue(owner!, out var list))
        {
            list = new List<Action>();
            _cleanupByOwner[owner!] = list;
        }

        list.Add(() => reset(owner));
    }
    
    public void BindScreen<T>(Func<T> resolve, Action<T> bind)
        where T : UIBase
    {
        var ui = resolve();
        if (ui == null)
        {
            Debug.LogError($"[VnFlow] EnterScreen<{typeof(T).Name}>: ui is null.");
            return;
        }

        Unbind(ui);

        bind(ui);
    }

    public void Bind<T>(T owner, Action<T> add, Action<T> remove)
        where T : UIBase
    {
        add(owner);

        if (!_cleanupByOwner.TryGetValue(owner!, out var list))
        {
            list = new List<Action>();
            _cleanupByOwner[owner!] = list;
        }

        list.Add(() => remove(owner));
    }

    public void Unbind(UIBase owner)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out var list))
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            try { list[i]?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        _cleanupByOwner.Remove(owner);
    }

    public void UnbindAll()
    {
        foreach (var kv in _cleanupByOwner)
        {
            List<Action> list = kv.Value;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                try { list[i]?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        _cleanupByOwner.Clear();
    }
    
    public void Dispose() => UnbindAll();
}