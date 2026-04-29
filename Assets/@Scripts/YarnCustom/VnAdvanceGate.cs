using System;
using UnityEngine;

public sealed class AdvanceGate
{
    private readonly VnUxState _vnUxState;
    private readonly VnPlaybackSettings _vnPlaybackSettings;
    private readonly EllipsisBreathTypewriter _ellipsisBreathTypewriter;
    private readonly Func<bool> _isCpsNodeBusy;

    // ---- Rate limiting ----
    private float UserAdvanceCooldownSec => _vnPlaybackSettings.userAdvanceCooldownSec;
    private float AutoPulseCooldownSec => _vnPlaybackSettings.autoAdvanceRateLimitSec;
    public float CooldownAfterHurryUpSec => _vnPlaybackSettings.cooldownAfterHurryUpSec;
    public float CooldownAfterNextLineSec => _vnPlaybackSettings.cooldownAfterNextLineSec;

    private double _lastAcceptedUserAdvanceAt = double.NegativeInfinity;
    private double _lastAcceptedAutoPulseAt   = double.NegativeInfinity;

    // ---- Event cooldown ----
    private double _cooldownUntilUnscaled = double.NegativeInfinity;

    public AdvanceGate(
        VnUxState uxState,
        VnPlaybackSettings vnPlaybackSettings,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        Func<bool> isCpsNodeBusy)
    {
        _vnUxState = uxState;
        _vnPlaybackSettings = vnPlaybackSettings;
        _ellipsisBreathTypewriter = ellipsisBreathTypewriter;
        _isCpsNodeBusy = isCpsNodeBusy;
    }
    
    public bool IsLineFullyShown => _ellipsisBreathTypewriter.IsComplete;
    
    public void AddCooldownSeconds(float seconds)
    {
        if (seconds <= 0f) return;
        
        double until = Time.unscaledTimeAsDouble + seconds;
        if (until > _cooldownUntilUnscaled)
            _cooldownUntilUnscaled = until;
    }

    public bool TryAcceptUserAdvance() => TryEnter(isUser: true);
    public bool TryAcceptAutoPulse()   => TryEnter(isUser: false);


    private bool TryEnter(bool isUser)
    {
        if (_vnUxState.BacklogVisible)
            return false;
        
        if (_vnUxState.ChoicesVisible)
            return false;

        if (_isCpsNodeBusy())
            return false;

        // cooldown
        double unscaledNow = Time.unscaledTimeAsDouble;
        if (unscaledNow < _cooldownUntilUnscaled)
            return false;

        // Rate limit
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