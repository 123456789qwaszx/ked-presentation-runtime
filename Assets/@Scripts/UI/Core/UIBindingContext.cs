using System;
using UnityEngine;

public sealed class UIBindingContext : IDisposable
{
    private readonly OwnerCleanupContext<UIBase> _cleanups = new(Debug.LogException);

    public void AddBinding<T>(T owner, Action<T> bind, Action<T> unbind)
        where T : UIBase
    {
        bind(owner);
        _cleanups.AddCleanup(owner, () => unbind(owner));
    }

    public void Unbind(UIBase owner)
    {
        _cleanups.Clear(owner);
    }

    public void UnbindAll()
    {
        _cleanups.ClearAll();
    }

    public void Dispose()
    {
        _cleanups.Dispose();
    }
}