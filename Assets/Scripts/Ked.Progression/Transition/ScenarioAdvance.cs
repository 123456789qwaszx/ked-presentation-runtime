namespace Ked.Progression
{
    public enum ScenarioAdvanceKind
    {
        NextChapter = 0,   // Continues to the next chapter
        ScenarioEnded = 1, // Reached an ending(intentional terminal point).

        /// <summary>
        /// The chapter stopped at a node with no ending key and no outgoing path.
        /// This is not an intentional ending.
        ///
        /// If this were treated as <see cref="ScenarioEnded"/>,
        /// an unfinished node would appear identical to an intentionally authored ending in the UI.
        /// </summary>
        DeadEnd = 2,
    }
    
    // Determines what happens after a chapter ends.
    public readonly struct ScenarioAdvance
    {
        public ScenarioAdvanceKind Kind { get; }

        // Which ending was reached.
        // Empty when "Kind == ScenarioAdvanceKind.DeadEnd".
        public string EndingKey { get; }

        // The matched ending rule.
        // Null when the chapter has no ending rules at all (single-chapter scenario).
        public EndingRule MatchedRule { get; }

        /// Populated only when <see cref="ScenarioAdvanceKind.NextChapter"/>.
        public string NextChapterId { get; }

        private ScenarioAdvance(
            ScenarioAdvanceKind kind, string endingKey, EndingRule matchedRule, string nextChapterId)
        {
            Kind = kind;
            EndingKey = endingKey;
            MatchedRule = matchedRule;
            NextChapterId = nextChapterId;
        }

        internal static ScenarioAdvance ToChapter(string endingKey, EndingRule rule) =>
            new(ScenarioAdvanceKind.NextChapter, endingKey, rule, rule.NextChapterId);

        internal static ScenarioAdvance Ended(string endingKey, EndingRule rule) => 
            new(ScenarioAdvanceKind.ScenarioEnded, endingKey, rule, string.Empty);

        internal static ScenarioAdvance DeadEnd() =>
            new(ScenarioAdvanceKind.DeadEnd, string.Empty, null, string.Empty);

        public override string ToString() => 
            Kind == ScenarioAdvanceKind.NextChapter 
                ? $"{Kind}({EndingKey} → {NextChapterId})"
                : $"{Kind}({EndingKey})";
    }
}