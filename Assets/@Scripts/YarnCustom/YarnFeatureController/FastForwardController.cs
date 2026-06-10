using System;

public sealed class FastForwardController
{
    private readonly VnPlaybackSettings _settings;
    private readonly EllipsisBreathTypewriter _typewriter;
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly PresentationSessionContext _presentationSessionContext;
    private readonly Func<bool> _isLineFullyShown;

    private bool _isHeld;
    private bool _wasHeld;

    public FastForwardController(
        VnPlaybackSettings settings,
        EllipsisBreathTypewriter typewriter,
        DialogueAdvanceDispatcher dispatcher,
        PresentationSessionContext presentationSessionContext,
        Func<bool> isLineFullyShown)
    {
        _settings = settings;
        _typewriter = typewriter;
        _dispatcher = dispatcher;
        _presentationSessionContext = presentationSessionContext;
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
        // _typewriter.SetSpeedMultiplier(_settings.speedupModeMultiplier);
        // _presentationSessionContext.EnterSpeedUpHeld();
    }

    private void OnHolding()
    {
        // if(_isLineFullyShown())
        //     _dispatcher.DispatchAdvance();
    }

    private void OnHoldEnd()
    {
        // _typewriter.SetSpeedMultiplier(1f);
        // _presentationSessionContext.ExitSpeedUpHeld();
    }
}