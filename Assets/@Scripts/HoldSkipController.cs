using System;

public sealed class HoldSpeedUpController
{
    private readonly VnPlaybackSettings _settings;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly Func<bool> _isLineFullyShown;

    private bool _isHeld;
    private bool _wasHeld;

    public HoldSpeedUpController(
        VnPlaybackSettings settings,
        EllipsisBreathTypewriter typewriter,
        DialogueAdvanceDispatcher dispatcher,
        Func<bool> isLineFullyShown)
    {
        _settings = settings;
        _typewriter = typewriter;
        _dispatcher = dispatcher;
        _isLineFullyShown = isLineFullyShown;
    }

    public void SetHeld(bool held)
    {
        _isHeld = held;
    }

    public void Tick()
    {
        if (_isHeld && !_wasHeld)
            OnHoldBegin();

        if (_isHeld)
            OnHolding();

        if (!_isHeld && _wasHeld)
            OnHoldEnd();

        _wasHeld = _isHeld;
    }

    private void OnHoldBegin()
    {
        _typewriter.SetSpeedMultiplier(_settings.speedupModeMultiplier);
    }

    private void OnHolding()
    {
        if (_isLineFullyShown())
            _dispatcher.DispatchAdvance();
    }

    private void OnHoldEnd()
    {
        _typewriter.SetSpeedMultiplier(1f);
    }
}