namespace Ked.Progression
{
    public readonly struct ScenarioAdvance
    {
        // 챕터가 끝난 노드의 이벤트키. (Optional)
        public string EventKey { get; }

        internal ScenarioAdvance(string eventKey)
        {
            EventKey = eventKey ?? string.Empty;
        }

        public override string ToString() => $"Ended({EventKey})";
    }
}