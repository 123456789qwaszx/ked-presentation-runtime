using System;
using System.Collections.Generic;

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

    // Determines what happens when a chapter run ends.
    // The ending key is read directly from the current node.
    public static class ScenarioTransition
    {
        public static ScenarioAdvance Resolve(ScenarioProgression scenario, ProgressionState state)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!scenario.TryGetChapter(state.CurrentChapterId, out ChapterProgression chapter))
            {
                throw new ArgumentException(
                    $"지금 챕터 '{state.CurrentChapterId}'가 시나리오 '{scenario.ScenarioId}'에 없다.",
                    nameof(state));
            }

            if (!chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode node))
            {
                throw new ArgumentException(
                    $"지금 에피소드 '{state.CurrentEpisodeId}'가 챕터 '{chapter.ChapterId}'에 없다.",
                    nameof(state));
            }

            if (!node.IsEndingCandidate)
                return ScenarioAdvance.DeadEnd();

            string endingKey = node.EndingKey;
            IReadOnlyList<EndingRule> rules = chapter.EndingRules;
            bool sawKey = false;

            for (int i = 0; i < rules.Count; i++)
            {
                EndingRule rule = rules[i];

                if (!string.Equals(rule.EndingKey, endingKey, StringComparison.Ordinal))
                    continue;

                sawKey = true;

                if (!AreMet(rule.Conditions, state))
                    continue;

                return rule.Outcome == EndingOutcome.ScenarioEnd
                    ? ScenarioAdvance.Ended(endingKey, rule)
                    : ScenarioAdvance.ToChapter(endingKey, rule);
            }

            if (!sawKey)
                return ScenarioAdvance.Ended(endingKey, null);
            
            // 여기 도달할 수 없다 — 같은 키의 마지막 규칙은 조건이 없어야 하고(무조건 성립),
            // 생성자가 그것을 강제한다. 도달했다면 불변식이 깨진 것이므로 조용히 끝내지 않는다.
            throw new InvalidOperationException(
                $"엔딩 '{endingKey}'의 규칙이 모두 미달이다. 같은 키의 마지막 규칙은 조건이 " +
                "없어야 한다(ChapterInvariants가 강제). 불변식을 우회해 만든 챕터가 아닌지 확인할 것.");
        }

        private static bool AreMet(IReadOnlyList<ProgressionCondition> conditions, ProgressionState state)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (!ConditionEvaluator.IsMet(conditions[i], state))
                    return false;
            }

            return true;
        }
    }
}