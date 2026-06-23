using System;

public sealed class SpeedUpModeController
{
    private readonly DialogueAdvanceDispatcher _dispatcher;
    private readonly Func<bool> _isEnabled;
    private readonly Func<bool> _isLineFullyShown;

    public SpeedUpModeController(
        DialogueAdvanceDispatcher dispatcher,
        Func<bool> isEnabled,
        Func<bool> isLineFullyShown)
    {
        _dispatcher = dispatcher;
        _isEnabled = isEnabled;
        _isLineFullyShown = isLineFullyShown;
    }

    public void Tick()
    {
        if (!_isEnabled())
            return;

        if (!_isLineFullyShown())
            return;

        _dispatcher.DispatchSpeedUpModeAdvance();
    }
}