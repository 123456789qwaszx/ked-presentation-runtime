using System.Collections.Generic;

/// <summary>
/// Main/sub 동기화 진행을 위한 gate token stream.
/// 
/// GateToken이 step progression을 표현했다면,
/// SyncGatePlan은 main/sub synchronization progression을 표현한다.
/// </summary>
public sealed class SyncGatePlan
{
    private readonly List<SyncGateToken> _tokens = new();
    private int _cursor;

    public int Count
    {
        get { return _tokens.Count; }
    }

    public int Cursor
    {
        get { return _cursor; }
    }

    public bool IsCompleted
    {
        get { return _cursor >= _tokens.Count; }
    }

    public int DispatchAdvanceCount
    {
        get
        {
            int count = 0;

            for (int i = _cursor; i < _tokens.Count; i++)
            {
                if (_tokens[i].Type == SyncGateTokenType.DispatchPresentationAdvance)
                    count++;
            }

            return count;
        }
    }

    public int ForwardSettleDispatchCount
    {
        get
        {
            int count = 0;

            for (int i = _cursor; i < _tokens.Count; i++)
            {
                SyncGateToken token = _tokens[i];

                if (token.Type == SyncGateTokenType.DispatchPresentationAdvance &&
                    token.CountsForForwardSettle)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public SyncGateToken? CurrentToken
    {
        get
        {
            if (_cursor < 0)
                return null;

            if (_cursor >= _tokens.Count)
                return null;

            return _tokens[_cursor];
        }
    }

    public static SyncGatePlan Empty()
    {
        return new SyncGatePlan();
    }

    public static SyncGatePlan Single(SyncGateToken token)
    {
        SyncGatePlan plan = new();
        plan.Add(token);
        return plan;
    }

    public void Add(SyncGateToken token)
    {
        _tokens.Add(token);
    }

    public void AddRepeated(SyncGateToken token, int count)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
            _tokens.Add(token);
    }

    public void AppendRemaining(SyncGatePlan other)
    {
        for (int i = other._cursor; i < other._tokens.Count; i++)
            _tokens.Add(other._tokens[i]);
    }

    public bool TryConsumeCurrent(out SyncGateToken token)
    {
        SyncGateToken? current = CurrentToken;

        if (!current.HasValue)
        {
            token = default;
            return false;
        }

        token = current.Value;
        _cursor++;
        return true;
    }

    public void ConsumeCurrent()
    {
        if (_cursor < _tokens.Count)
            _cursor++;
    }

    public void Clear()
    {
        _tokens.Clear();
        _cursor = 0;
    }
}