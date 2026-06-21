using System.Collections.Generic;

public sealed class SyncGatePlan
{
    private readonly SyncGateToken[] _tokens;

    public IReadOnlyList<SyncGateToken> Tokens => _tokens;

    public int Count => _tokens.Length;

    public bool IsEmpty => _tokens.Length == 0;

    public int DispatchAdvanceCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < _tokens.Length; i++)
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

            for (int i = 0; i < _tokens.Length; i++)
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

    public SyncGatePlan(IReadOnlyList<SyncGateToken> tokens)
    {
        if (tokens == null || tokens.Count == 0)
        {
            _tokens = System.Array.Empty<SyncGateToken>();
            return;
        }

        _tokens = new SyncGateToken[tokens.Count];

        for (int i = 0; i < tokens.Count; i++)
            _tokens[i] = tokens[i];
    }

    public static SyncGatePlan Empty()
    {
        return new SyncGatePlan(System.Array.Empty<SyncGateToken>());
    }

    public static SyncGatePlan Single(SyncGateToken token)
    {
        return new SyncGatePlan(new[] { token });
    }
}