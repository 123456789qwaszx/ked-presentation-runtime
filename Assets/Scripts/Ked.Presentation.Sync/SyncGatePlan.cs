using System.Collections.Generic;

namespace Ked.Presentation.Sync
{

    public sealed class SyncGatePlan
    {
        private static readonly SyncGatePlan EmptyPlan = new(System.Array.Empty<SyncGateToken>());

        private readonly SyncGateToken[] _tokens;

        public IReadOnlyList<SyncGateToken> Tokens => _tokens;

        public bool IsEmpty => _tokens.Length == 0;

        public SyncGatePlan(IReadOnlyList<SyncGateToken> tokens)
        {
            _tokens = new SyncGateToken[tokens.Count];

            for (int i = 0; i < tokens.Count; i++)
                _tokens[i] = tokens[i];
        }

        public static SyncGatePlan Empty()
        {
            return EmptyPlan;
        }
    }
}