using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class ScenarioProgression
    {
        private readonly Dictionary<string, ChapterProgression> _chaptersById;
        private readonly Dictionary<string, StatDefinition> _statsByKey;

        public string ScenarioId { get; }
        public string DisplayName { get; }

        // 새 게임이 시작하는 챕터.
        public string StartChapterId { get; }

        public IReadOnlyList<StatDefinition> Stats { get; } // 스탯 정의의 유일한 원천

        public IReadOnlyList<ChapterProgression> Chapters { get; }

        public IReadOnlyDictionary<string, StatDefinition> StatsByKey => _statsByKey;

        public ScenarioProgression(
            string scenarioId,
            string displayName,
            string startChapterId,
            IReadOnlyList<StatDefinition> stats,
            IReadOnlyList<ChapterProgression> chapters)
        {
            if (string.IsNullOrEmpty(scenarioId))
                throw new ArgumentException("시나리오 ID가 비어 있다.", nameof(scenarioId));
            
            ScenarioId = scenarioId;
            DisplayName = displayName ?? string.Empty;
            StartChapterId = startChapterId ?? string.Empty;
            Stats = stats ?? Array.Empty<StatDefinition>();
            Chapters = chapters ?? Array.Empty<ChapterProgression>();

            var diagnostics = new List<ProgressionDiagnostic>();

            ScenarioInvariants.Collect(
                Stats, Chapters, StartChapterId,
                diagnostics, out _statsByKey, out _chaptersById);

            if (diagnostics.Count > 0)
                throw new ArgumentException(diagnostics[0].ToString());
        }

        public bool TryGetChapter(string chapterId, out ChapterProgression chapter)
        {
            if (chapterId == null)
            {
                chapter = null;
                return false;
            }

            return _chaptersById.TryGetValue(chapterId, out chapter);
        }

        // 시작 챕터. 생성자에서 존재 보장.
        public ChapterProgression StartChapter => _chaptersById[StartChapterId];

        /// 새 게임의 상태. 시나리오의 초기값으로 한 번만 세움.
        /// 챕터단위가 아님.
        public ProgressionState CreateInitialState()
        {
            ChapterProgression start = StartChapter;

            return ProgressionState.CreateInitial(
                Stats, start.ChapterId, start.StartEpisodeId);
        }

        public override string ToString() =>
            $"{ScenarioId}(챕터 {Chapters.Count}, 스탯 {Stats.Count})";
    }
}