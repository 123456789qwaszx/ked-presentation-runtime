namespace Ked.Progression
{
    public enum ScenarioAdvanceKind
    {
        ScenarioEnded = 0, // Reached an ending(intentional terminal point).

        /// <summary>
        /// The chapter stopped at a node with no ending key and no outgoing path.
        /// This is not an intentional ending.
        ///
        /// If this were treated as <see cref="ScenarioEnded"/>,
        /// an unfinished node would appear identical to an intentionally authored ending in the UI.
        /// </summary>
        DeadEnd = 1,
    }

    // Determines what happens after a chapter ends.
    public readonly struct ScenarioAdvance
    {
        public ScenarioAdvanceKind Kind { get; }

        // Which ending was reached.
        // Empty when "Kind == ScenarioAdvanceKind.DeadEnd".
        public string EndingKey { get; }

        private ScenarioAdvance(ScenarioAdvanceKind kind, string endingKey)
        {
            Kind = kind;
            EndingKey = endingKey;
        }

        internal static ScenarioAdvance Ended(string endingKey) =>
            new(ScenarioAdvanceKind.ScenarioEnded, endingKey);

        internal static ScenarioAdvance DeadEnd() =>
            new(ScenarioAdvanceKind.DeadEnd, string.Empty);

        public override string ToString() => $"{Kind}({EndingKey})";
    }
}