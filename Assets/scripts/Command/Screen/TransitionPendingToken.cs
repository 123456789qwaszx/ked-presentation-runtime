using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TransitionPendingToken : IDisposable
{
    private readonly TransitionContext _context;
    private bool _released;

    public string Owner { get; }
    public string Reason { get; }

    internal TransitionPendingToken(TransitionContext context, string owner, string reason)
    {
        _context = context;
        Owner = owner;
        Reason = reason;
    }

    public void Release()
    {
        if (_released) return;
        _released = true;
        _context.InternalRelease(this);
    }

    public void Dispose() => Release();
}

public sealed class TransitionContext
{
    private readonly List<TransitionPendingToken> _pending = new();

    public TransitionCommandSpec Spec { get; }
    public TransitionTargetHandle Target { get; }
    public CommandRunScope Scope { get; }

    public bool CoverCompleted  { get; private set; }
    public bool SwapEntered     { get; private set; }
    public bool ReadyCompleted  { get; private set; }
    public bool Finished        { get; private set; }
    public bool SkipRequested   { get; private set; }
    public bool CancelRequested { get; private set; }

    public int PendingCount => _pending.Count;
    public float StartedRealtime { get; }

    public TransitionContext(
        TransitionCommandSpec spec,
        TransitionTargetHandle target,
        CommandRunScope scope)
    {
        Spec   = spec;
        Target = target;
        Scope  = scope;
        StartedRealtime = Time.unscaledTime;
    }

    // 준비 작업을 등록. 반환된 token을 Release() 하면 pending 해제.
    public TransitionPendingToken RegisterPending(string owner, string reason)
    {
        var token = new TransitionPendingToken(this, owner, reason);
        _pending.Add(token);
        return token;
    }

    internal void InternalRelease(TransitionPendingToken token)
    {
        _pending.Remove(token);
    }

    public void MarkCoverCompleted()  => CoverCompleted  = true;
    public void MarkSwapEntered()     => SwapEntered     = true;
    public void MarkReadyCompleted()  => ReadyCompleted  = true;
    public void MarkFinished()        => Finished        = true;

    public void RequestSkip()   => SkipRequested   = true;
    public void RequestCancel() => CancelRequested = true;

    public bool IsReadyToUncover()
    {
        if (CancelRequested || SkipRequested) return true;
        return PendingCount == 0;
    }

    public bool IsTimedOut()
    {
        if (Spec.readyTimeoutSeconds <= 0f) return false;
        return Time.unscaledTime - StartedRealtime >= Spec.readyTimeoutSeconds;
    }
}