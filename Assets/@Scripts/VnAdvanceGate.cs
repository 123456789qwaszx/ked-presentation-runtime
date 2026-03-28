using System;
using UnityEngine;

public sealed class AdvanceGate
{
    private readonly VnUxState _vnUxState;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly Func<bool> _isCpsNodeBusy;

    // ---- Rate limiting ----
    public float UserAdvanceCooldownSec { get; set; } = 0.13f; // 130ms
    public float AutoPulseCooldownSec { get; set; } = 0.13f; // 130ms

    private double _lastAcceptedUserAdvanceAt = double.NegativeInfinity;
    private double _lastAcceptedAutoPulseAt   = double.NegativeInfinity;

    // ---- Event cooldown ----
    private double _cooldownUntilUnscaled = double.NegativeInfinity;

    public AdvanceGate(
        VnUxState uxState,
        EllipsisBreathTypewriter typewriter,
        Func<bool> isCpsNodeBusy)
    {
        _vnUxState = uxState;
        _typewriter = typewriter;
        _isCpsNodeBusy = isCpsNodeBusy;
    }
    
    public bool IsLineFullyShown() => _typewriter.IsComplete;
    
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