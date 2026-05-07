using System;
using UnityEngine;

public sealed class AdvanceGate
{
    private readonly VnUxState _vnUxState;
    private readonly VnPlaybackSettings _vnPlaybackSettings;
    private readonly LinePresentationAdvanceState _lineState;
    private readonly Func<bool> _isCpsNodeBusy;

    private float UserAdvanceCooldownSec => _vnPlaybackSettings.userAdvanceCooldownSec;
    private float AutoPulseCooldownSec => _vnPlaybackSettings.autoAdvanceRateLimitSec;
    public float CooldownAfterHurryUpSec => _vnPlaybackSettings.cooldownAfterHurryUpSec;
    public float CooldownAfterNextLineSec => _vnPlaybackSettings.cooldownAfterNextLineSec;

    private double _lastAcceptedUserAdvanceAt = double.NegativeInfinity;
    private double _lastAcceptedAutoPulseAt = double.NegativeInfinity;

    private double _cooldownUntilUnscaled = double.NegativeInfinity;

    public AdvanceGate(
        VnUxState uxState,
        VnPlaybackSettings vnPlaybackSettings,
        LinePresentationAdvanceState lineState,
        Func<bool> isCpsNodeBusy)
    {
        _vnUxState = uxState;
        _vnPlaybackSettings = vnPlaybackSettings;
        _lineState = lineState;
        _isCpsNodeBusy = isCpsNodeBusy;
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
        if (_lineState != null && _lineState.IsRollbackSeeking)
            return false;

        if (_vnUxState.BacklogVisible)
            return false;

        if (_vnUxState.ChoicesVisible)
            return false;

        if (_isCpsNodeBusy != null && _isCpsNodeBusy())
            return false;

        double unscaledNow = Time.unscaledTimeAsDouble;
        if (unscaledNow < _cooldownUntilUnscaled)
            return false;

        if (!PassRateLimit(isUser, unscaledNow))
            return false;

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
}