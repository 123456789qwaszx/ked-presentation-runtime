using System.Collections.Generic;

public sealed class SyncGateState
{
    private readonly List<SyncGateToken> _tokens = new();

    private int _cursor;
    private bool _isRunning;

    public bool IsCompleted
    {
        get
        {
            if (!_isRunning)
                return true;

            return _cursor >= _tokens.Count;
        }
    }

    public SyncGateToken? CurrentToken
    {
        get
        {
            if (!_isRunning)
                return null;

            if (_cursor < 0)
                return null;

            if (_cursor >= _tokens.Count)
                return null;

            return _tokens[_cursor];
        }
    }

    public bool Begin(SyncGatePlan plan)
    {
        if (_isRunning && !IsCompleted)
            return false;

        _tokens.Clear();
        _cursor = 0;
        
        for (int i = 0; i < plan.Tokens.Count; i++)
            _tokens.Add(plan.Tokens[i]);

        _isRunning = true;

        if (_tokens.Count == 0)
            _isRunning = false;

        return true;
    }

    public void ConsumeCurrent()
    {
        if (!_isRunning)
            return;

        if (_cursor < _tokens.Count)
            _cursor++;

        if (_cursor >= _tokens.Count)
            Clear();
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
        ConsumeCurrent();
        return true;
    }

    public void Clear()
    {
        _tokens.Clear();
        _cursor = 0;
        _isRunning = false;
    }
}