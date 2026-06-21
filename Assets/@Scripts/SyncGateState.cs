/// <summary>
/// 현재 in-flight 중인 SyncGatePlan의 runtime cursor.
/// 
/// StepGateState의 Tokens + Cursor와 같은 역할이다.
/// </summary>
public sealed class SyncGateState
{
    private readonly SyncGatePlan _plan = new();

    public bool IsCompleted
    {
        get { return _plan.IsCompleted; }
    }

    public SyncGateToken? CurrentToken
    {
        get { return _plan.CurrentToken; }
    }

    public void Enqueue(SyncGatePlan plan)
    {
        if (_plan.IsCompleted)
            _plan.Clear();

        _plan.AppendRemaining(plan);
    }

    public void ConsumeCurrent()
    {
        _plan.ConsumeCurrent();
    }

    public bool TryConsumeCurrent(out SyncGateToken token)
    {
        return _plan.TryConsumeCurrent(out token);
    }

    public void Clear()
    {
        _plan.Clear();
    }
}