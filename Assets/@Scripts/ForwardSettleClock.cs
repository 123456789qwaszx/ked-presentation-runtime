/// <summary>
/// Main forward flow의 settle 회계.
///
/// Epoch은 monotonic이다.
/// 하지만 아무 side settle이나 Epoch을 올리지는 않는다.
/// Scripted advance가 실제 dispatch되었을 때 BeginForwardSettle이 호출되고,
/// 그 in-flight settle에 대응되는 NotifySettled만 Epoch을 증가시킨다.
/// </summary>
public sealed class ForwardSettleClock
{
    private int _inFlightForwardSettles;

    public int Epoch { get; private set; }

    public void BeginForwardSettle()
    {
        _inFlightForwardSettles++;
    }

    public void ClearInFlightSettles()
    {
        _inFlightForwardSettles = 0;
    }

    public void NotifySettled()
    {
        if (_inFlightForwardSettles <= 0)
            return;

        _inFlightForwardSettles--;

        unchecked
        {
            Epoch++;
        }
    }
}