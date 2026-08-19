using System.Collections.Generic;

namespace Ked.Presentation.Sync
{

    public sealed class SyncGateState
    {
        private readonly List<SyncGateToken> _tokens = new();

        private int _cursor;
        private bool _isRunning;

        public bool IsCompleted => !_isRunning;

        public SyncGateToken? CurrentToken => _tokens[_cursor];

        public bool Begin(SyncGatePlan plan)
        {
            if (_isRunning)
                return false;

            _tokens.Clear();
            _cursor = 0;

            for (int i = 0; i < plan.Tokens.Count; i++)
                _tokens.Add(plan.Tokens[i]);

            _isRunning = true;
            return true;
        }

        public void ConsumeCurrent()
        {
            _cursor++;

            if (_cursor >= _tokens.Count)
                Clear();
        }

        public void Clear()
        {
            _tokens.Clear();
            _cursor = 0;
            _isRunning = false;
        }
    }
}