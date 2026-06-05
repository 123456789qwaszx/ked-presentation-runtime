using UnityEngine;

public sealed class AdvanceGate
{
    private readonly VnUxState _vnUxState;
    private readonly VnPlaybackSettings _vnPlaybackSettings;
    private readonly VNLinePresentationState _lineState;
    private ICommandRunScopeProvider _scopeProvider;
    private readonly VNTraceStream _trace;

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;
    
    private float UserAdvanceCooldownSec => _vnPlaybackSettings.userAdvanceCooldownSec;
    private float AutoPulseCooldownSec => _vnPlaybackSettings.autoAdvanceRateLimitSec;
    public float CooldownAfterHurryUpSec => _vnPlaybackSettings.cooldownAfterHurryUpSec;
    public float CooldownAfterNextLineSec => _vnPlaybackSettings.cooldownAfterNextLineSec;

    private double _lastAcceptedUserAdvanceAt = double.NegativeInfinity;
    private double _lastAcceptedAutoPulseAt = double.NegativeInfinity;
    private double _cooldownUntilUnscaled = double.NegativeInfinity;

    private string _lastRejectTraceKey;
    private int _lastRejectTraceFrame = -1;

    public AdvanceGate(
        VnUxState uxState,
        VnPlaybackSettings vnPlaybackSettings,
        VNLinePresentationState lineState,
        ICommandRunScopeProvider scopeProvider,
        VNTraceStream trace)
    {
        _vnUxState = uxState;
        _vnPlaybackSettings = vnPlaybackSettings;
        _lineState = lineState;
        _scopeProvider = scopeProvider;
        _trace = trace;
    }

    public bool IsLineFullyShown
    {
        get
        {
            if (_lineState == null)
                return true;

            return _lineState.IsLineFullyShown;
        }
    }

    public void AddCooldownSeconds(float seconds)
    {
        if (seconds <= 0f)
            return;

        double until = Time.unscaledTimeAsDouble + seconds;
        if (until > _cooldownUntilUnscaled)
            _cooldownUntilUnscaled = until;

        Trace("AddCooldown", $"seconds={seconds:0.###}, until={_cooldownUntilUnscaled:0.000}");
    }

    public void Reset()
    {
        _lastAcceptedUserAdvanceAt = double.NegativeInfinity;
        _lastAcceptedAutoPulseAt = double.NegativeInfinity;
        _cooldownUntilUnscaled = double.NegativeInfinity;

        _lastRejectTraceKey = null;
        _lastRejectTraceFrame = -1;

        Trace("Reset");
    }

    public bool TryAcceptUserAdvance()
    {
        return TryEnter(isUser: true);
    }

    public bool TryAcceptAutoPulse()
    {
        return TryEnter(isUser: false);
    }

    private bool TryEnter(bool isUser)
    {
        if (_lineState.IsSeekingActive)
            return Reject(isUser, "seek_active");

        if (_vnUxState.BacklogVisible)
            return Reject(isUser, "backlog_visible");

        if (_vnUxState.ChoicesVisible)
            return Reject(isUser, "choices_visible");

        if (CurrentScope != null && CurrentScope.IsNodeBusy)
        {
            // Debug.Log("executorDispose");
            // _commandExecutor.CancelAndDisposeToken();
            return Reject(isUser, "cps_node_busy");
        }

        double unscaledNow = Time.unscaledTimeAsDouble;

        if (unscaledNow < _cooldownUntilUnscaled)
            return Reject(
                isUser,
                "cooldown",
                $"now={unscaledNow:0.000}, until={_cooldownUntilUnscaled:0.000}");

        if (!PassRateLimit(isUser, unscaledNow))
            return Reject(isUser, "rate_limit", $"now={unscaledNow:0.000}");

        Trace(
            isUser ? "AcceptUserAdvance" : "AcceptAutoPulse",
            $"now={unscaledNow:0.000}");
        
        return true;
    }

    private bool PassRateLimit(bool isUserInitiated, double currentTime)
    {
        ref double lastAcceptedAt = ref isUserInitiated
            ? ref _lastAcceptedUserAdvanceAt
            : ref _lastAcceptedAutoPulseAt;

        float cooldown = isUserInitiated
            ? UserAdvanceCooldownSec
            : AutoPulseCooldownSec;

        if (currentTime - lastAcceptedAt < cooldown)
            return false;

        lastAcceptedAt = currentTime;
        return true;
    }

    private bool Reject(bool isUser, string reason, string note = null)
    {
        // User input rejection is useful.
        // Auto rejection can spam, so trace it only when the reason changes or enough frames pass.
        bool shouldTrace = isUser || ShouldTraceAutoReject(reason);

        if (shouldTrace)
        {
            string evt = isUser
                ? "RejectUserAdvance"
                : "RejectAutoPulse";

            string detail = string.IsNullOrWhiteSpace(note)
                ? $"reason={reason}"
                : $"reason={reason}, {note}";

            Trace(evt, detail);
        }

        return false;
    }

    private bool ShouldTraceAutoReject(string reason)
    {
        string key = "auto:" + reason;

        if (_lastRejectTraceKey != key)
        {
            _lastRejectTraceKey = key;
            _lastRejectTraceFrame = Time.frameCount;
            return true;
        }

        // Prevent per-frame spam while still showing long stalls.
        if (Time.frameCount - _lastRejectTraceFrame >= 60)
        {
            _lastRejectTraceFrame = Time.frameCount;
            return true;
        }

        return false;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        string state =
            $"lineFullyShown={IsLineFullyShown}, " +
            $"ux=backlog:{_vnUxState.BacklogVisible},choices:{_vnUxState.ChoicesVisible},hudHidden:{_vnUxState.HudHidden}, " +
            $"cooldownUntil={_cooldownUntilUnscaled:0.000}";

        _trace.Trace(nameof(AdvanceGate), evt, state, note);
    }
}


// using System;
// using UnityEngine;
//
// public sealed class AdvanceGate
// {
//     private readonly VnUxState _vnUxState;
//     private readonly VnPlaybackSettings _vnPlaybackSettings;
//     private readonly LinePresentationAdvanceState _lineState;
//     private readonly Func<bool> _isCpsNodeBusy;
//     private readonly VNTraceStream _trace;
//
//     private float UserAdvanceCooldownSec => _vnPlaybackSettings.userAdvanceCooldownSec;
//     private float AutoPulseCooldownSec => _vnPlaybackSettings.autoAdvanceRateLimitSec;
//     public float CooldownAfterHurryUpSec => _vnPlaybackSettings.cooldownAfterHurryUpSec;
//     public float CooldownAfterNextLineSec => _vnPlaybackSettings.cooldownAfterNextLineSec;
//
//     private double _lastAcceptedUserAdvanceAt = double.NegativeInfinity;
//     private double _lastAcceptedAutoPulseAt = double.NegativeInfinity;
//
//     private double _cooldownUntilUnscaled = double.NegativeInfinity;
//
//     public AdvanceGate(VnUxState uxState, VnPlaybackSettings vnPlaybackSettings, LinePresentationAdvanceState lineState, Func<bool> isCpsNodeBusy, VNTraceStream trace = null)
//     {
//         _vnUxState = uxState;
//         _vnPlaybackSettings = vnPlaybackSettings;
//         _lineState = lineState;
//         _isCpsNodeBusy = isCpsNodeBusy;
//         _trace = trace;
//     }
//
//     public bool IsLineFullyShown
//     {
//         get
//         {
//             if (_lineState == null)
//                 return true;
//
//             return _lineState.IsLineFullyShown;
//         }
//     }
//
//     public void AddCooldownSeconds(float seconds)
//     {
//         if (seconds <= 0f)
//             return;
//
//         double until = Time.unscaledTimeAsDouble + seconds;
//         if (until > _cooldownUntilUnscaled)
//             _cooldownUntilUnscaled = until;
//     }
//
//     public bool TryAcceptUserAdvance()
//     {
//         return TryEnter(isUser: true);
//     }
//
//     public bool TryAcceptAutoPulse()
//     {
//         return TryEnter(isUser: false);
//     }
//
//     private bool TryEnter(bool isUser)
//     {
//         // if (_lineState != null && _lineState.IsSeeking)
//         //     return false;
//         if (_lineState != null && _lineState.IsSeekingActive)
//             return false;
//         
//         if (_vnUxState.BacklogVisible)
//             return false;
//
//         if (_vnUxState.ChoicesVisible)
//             return false;
//
//         if (_isCpsNodeBusy != null && _isCpsNodeBusy())
//             return false;
//
//         double unscaledNow = Time.unscaledTimeAsDouble;
//         if (unscaledNow < _cooldownUntilUnscaled)
//             return false;
//
//         if (!PassRateLimit(isUser, unscaledNow))
//             return false;
//
//         return true;
//     }
//
//     private bool PassRateLimit(bool isUserInitiated, double currentTime)
//     {
//         ref double lastAcceptedAt = ref isUserInitiated
//             ? ref _lastAcceptedUserAdvanceAt
//             : ref _lastAcceptedAutoPulseAt;
//
//         float cooldown = isUserInitiated
//             ? UserAdvanceCooldownSec
//             : AutoPulseCooldownSec;
//
//         if (currentTime - lastAcceptedAt < cooldown)
//             return false;
//
//         lastAcceptedAt = currentTime;
//         return true;
//     }
// }