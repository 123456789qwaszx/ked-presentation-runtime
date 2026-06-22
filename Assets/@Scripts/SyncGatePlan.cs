using System.Collections.Generic;

public sealed class SyncGatePlan
{
    private readonly SyncGateToken[] _tokens;

    public IReadOnlyList<SyncGateToken> Tokens => _tokens;

    public bool IsEmpty => _tokens.Length == 0;

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