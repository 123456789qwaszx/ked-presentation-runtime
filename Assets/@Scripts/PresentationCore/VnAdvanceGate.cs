using System;
using UnityEngine;

public sealed class AdvanceGate
{
    private const int AdvanceRequestKindCount = 4;

    private readonly VnPlaybackSettings _vnPlaybackSettings;
    private readonly VNLinePresentationState _lineState;
    private ICommandRunScopeProvider _scopeProvider;
    private readonly VNTraceStream _trace;

    private CommandRunScope CurrentScope => _scopeProvider?.CurrentScope;

    private readonly double[] _lastAcceptedAt = new double[AdvanceRequestKindCount];
    private double _cooldownUntilUnscaled = double.NegativeInfinity;

    private string _lastRejectTraceKey;
    private int _lastRejectTraceFrame = -1;

    public AdvanceGate(
        VnPlaybackSettings vnPlaybackSettings,
        VNLinePresentationState lineState,
        ICommandRunScopeProvider scopeProvider,
        VNTraceStream trace)
    {
        _vnPlaybackSettings = vnPlaybackSettings;
        _lineState = lineState;
        _scopeProvider = scopeProvider;
        _trace = trace;

        ResetAcceptedTimes();
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
        ResetAcceptedTimes();

        _cooldownUntilUnscaled = double.NegativeInfinity;

        _lastRejectTraceKey = null;
        _lastRejectTraceFrame = -1;

        Trace("Reset");
    }

    private void ResetAcceptedTimes()
    {
        for (int i = 0; i < _lastAcceptedAt.Length; i++)
            _lastAcceptedAt[i] = double.NegativeInfinity;
    }

    public bool TryAcceptUserAdvance()
    {
        return TryAccept(AdvanceRequestKind.User);
    }

    public bool TryAcceptAutoPulse()
    {
        return TryAccept(AdvanceRequestKind.Auto);
    }

    public bool TryAcceptSpeedUpModePulse()
    {
        return TryAccept(AdvanceRequestKind.SpeedUpMode);
    }

    public bool TryAcceptRapidSkipPulse()
    {
        return TryAccept(AdvanceRequestKind.RapidSkip);
    }

    public bool TryAccept(AdvanceRequestKind kind)
    {
        if (_lineState.IsSeekingActive)
            return Reject(kind, "seek_active");

        if (ShouldRejectNodeBusy(kind) &&
            CurrentScope != null &&
            CurrentScope.IsNodeBusy)
        {
            return Reject(kind, "cps_node_busy");
        }

        double unscaledNow = Time.unscaledTimeAsDouble;

        if (unscaledNow < _cooldownUntilUnscaled)
        {
            return Reject(
                kind,
                "cooldown",
                $"now={unscaledNow:0.000}, until={_cooldownUntilUnscaled:0.000}");
        }

        if (!PassRateLimit(kind, unscaledNow))
            return Reject(kind, "rate_limit", $"now={unscaledNow:0.000}");

        Trace($"Accept{kind}", $"now={unscaledNow:0.000}");
        return true;
    }

    private bool ShouldRejectNodeBusy(AdvanceRequestKind kind)
    {
        return kind != AdvanceRequestKind.RapidSkip;
    }

    private bool PassRateLimit(AdvanceRequestKind kind, double currentTime)
    {
        int index = (int)kind;
        float cooldown = GetRateLimitSeconds(kind);

        if (currentTime - _lastAcceptedAt[index] < cooldown)
            return false;

        _lastAcceptedAt[index] = currentTime;
        return true;
    }

    private float GetRateLimitSeconds(AdvanceRequestKind kind)
    {
        switch (kind)
        {
            case AdvanceRequestKind.Auto:
                return _vnPlaybackSettings.autoAdvanceRateLimitSec;

            case AdvanceRequestKind.SpeedUpMode:
                return _vnPlaybackSettings.speedupAdvanceRateLimitSec;

            case AdvanceRequestKind.RapidSkip:
                return _vnPlaybackSettings.rapidSkipAdvanceRateLimitSec;

            case AdvanceRequestKind.User:
            default:
                return _vnPlaybackSettings.userAdvanceCooldownSec;
        }
    }

    public float GetCooldownAfterHurryUp(AdvanceRequestKind kind)
    {
        switch (kind)
        {
            case AdvanceRequestKind.SpeedUpMode:
                return _vnPlaybackSettings.speedupCooldownAfterHurryUpSec;

            case AdvanceRequestKind.RapidSkip:
                return _vnPlaybackSettings.rapidSkipCooldownAfterHurryUpSec;

            case AdvanceRequestKind.Auto:
            case AdvanceRequestKind.User:
            default:
                return _vnPlaybackSettings.cooldownAfterHurryUpSec;
        }
    }

    public float GetCooldownAfterNextLine(AdvanceRequestKind kind)
    {
        switch (kind)
        {
            case AdvanceRequestKind.SpeedUpMode:
                return _vnPlaybackSettings.speedupCooldownAfterNextLineSec;

            case AdvanceRequestKind.RapidSkip:
                return _vnPlaybackSettings.rapidSkipCooldownAfterNextLineSec;

            case AdvanceRequestKind.Auto:
            case AdvanceRequestKind.User:
            default:
                return _vnPlaybackSettings.cooldownAfterNextLineSec;
        }
    }

    private bool Reject(AdvanceRequestKind kind, string reason, string note = null)
    {
        bool shouldTrace = kind == AdvanceRequestKind.User ||
                           kind == AdvanceRequestKind.RapidSkip ||
                           ShouldTraceRepeatedReject(kind, reason);

        if (shouldTrace)
        {
            string detail = string.IsNullOrWhiteSpace(note)
                ? $"kind={kind}, reason={reason}"
                : $"kind={kind}, reason={reason}, {note}";

            Debug.Log(detail);
            Trace("RejectAdvance", detail);
        }

        return false;
    }

    private bool ShouldTraceRepeatedReject(AdvanceRequestKind kind, string reason)
    {
        string key = kind + ":" + reason;

        if (_lastRejectTraceKey != key)
        {
            _lastRejectTraceKey = key;
            _lastRejectTraceFrame = Time.frameCount;
            return true;
        }

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
            $"cooldownUntil={_cooldownUntilUnscaled:0.000}";

        _trace.Trace(nameof(AdvanceGate), evt, state, note);
    }
}