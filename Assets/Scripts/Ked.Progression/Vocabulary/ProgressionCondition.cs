using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    public enum ConditionKind
    {
        Stat = 0,           // Compares a stat value using "ComparisonOp".
        EpisodeCleared = 1, // Checks whether an episode has been cleared. Uses "ComparisonOp.Exists".
        ChapterCleared = 2, // Checks whether a chapter has been cleared. Uses "ComparisonOp.Exists".
    }

    public readonly struct ProgressionCondition
    {
        public ConditionKind Kind { get; }
        public string Key { get; }
        public ComparisonOp Op { get; }

        // Value to compare against.
        // { "Kind": "Stat", "Key": "trust", "Op": "GreaterOrEqual" }
        public int Value { get; }

        private ProgressionCondition(ConditionKind kind, string key, ComparisonOp op, int value)
        {
            Kind = kind;
            Key = key;
            Op = op;
            Value = value;
        }

        public static ProgressionCondition Stat(string key, ComparisonOp op, int value = 0) =>
            new(ConditionKind.Stat, Require(key, nameof(key)), op, value);

        public static ProgressionCondition EpisodeCleared(string episodeId) =>
            new(ConditionKind.EpisodeCleared, Require(episodeId, nameof(episodeId)), ComparisonOp.Exists, 0);

        public static ProgressionCondition ChapterCleared(string chapterId) =>
            new(ConditionKind.ChapterCleared, Require(chapterId, nameof(chapterId)), ComparisonOp.Exists, 0);

        // Whether this value was created through one of the factory methods.
        // C# can always create default(ProgressionCondition).
        // If such a value appears in an array, the owning type rejects it;
        // the evaluator does not need to check it again.
        public bool IsConstructed => Key != null;

        // Ensures that no default values are present in the condition list.
        internal static void RequireAllConstructed(IReadOnlyList<ProgressionCondition> conditions, string paramName)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].IsConstructed)
                    continue;
                
                throw new ArgumentException(
                    $"{paramName}[{i}] is an unconstructed condition (default value). " +
                    "Create conditions using ProgressionCondition.Stat/EpisodeCleared/ChapterCleared.",
                    paramName);
            }
        }

        public override string ToString()
        {
            return Kind == ConditionKind.Stat
                ? $"{Key} {Op} {Value}"
                : $"{Kind}({Key})";
        }

        private static string Require(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("The condition target key cannot be empty.", paramName);

            return value;
        }
    }
}