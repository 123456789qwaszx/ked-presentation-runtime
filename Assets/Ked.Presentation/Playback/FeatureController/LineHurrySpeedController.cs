// 서두르기는 타자기만 빠르게 한다. 연출은 줄 넘김(스텝 경계)에서 가속·확정된다(2026-11-03 소유자 결정).
public sealed class LineHurrySpeedController
{
    private const float HurrySpeedMultiplier = 30f;

    private readonly EllipsisBreathTypewriter _typewriter;

    private bool _active;
    private float _restoreTypewriterMultiplier = 1f;

    public LineHurrySpeedController(EllipsisBreathTypewriter typewriter)
    {
        _typewriter = typewriter;
    }

    public void Enter()
    {
        if (_active)
            return;

        _restoreTypewriterMultiplier = _typewriter.SpeedMultiplier;

        _typewriter.SetSpeedMultiplier(
            _restoreTypewriterMultiplier * HurrySpeedMultiplier);

        _active = true;
    }

    public void Exit()
    {
        if (!_active)
            return;

        _typewriter.SetSpeedMultiplier(_restoreTypewriterMultiplier);

        _active = false;
    }
}
