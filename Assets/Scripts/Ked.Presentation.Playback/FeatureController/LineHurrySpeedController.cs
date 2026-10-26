using DG.Tweening;

public sealed class LineHurrySpeedController
{
    private const float HurrySpeedMultiplier = 30f;

    private readonly EllipsisBreathTypewriter _typewriter;

    private bool _active;
    private float _restoreTypewriterMultiplier = 1f;
    private float _restoreDotweenUnscaledTimeScale = 1f;

    public LineHurrySpeedController(EllipsisBreathTypewriter typewriter)
    {
        _typewriter = typewriter;
    }

    public void Enter()
    {
        if (_active)
            return;

        _restoreTypewriterMultiplier = _typewriter.SpeedMultiplier;
        _restoreDotweenUnscaledTimeScale = DOTween.unscaledTimeScale;

        _typewriter.SetSpeedMultiplier(
            _restoreTypewriterMultiplier * HurrySpeedMultiplier);

        DOTween.unscaledTimeScale = HurrySpeedMultiplier;

        _active = true;
    }

    public void Exit()
    {
        if (!_active)
            return;

        _typewriter.SetSpeedMultiplier(_restoreTypewriterMultiplier);
        DOTween.unscaledTimeScale = _restoreDotweenUnscaledTimeScale;

        _active = false;
    }
}