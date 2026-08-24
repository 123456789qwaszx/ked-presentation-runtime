using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    // 진행상태:
    // - 세이브가 담을 내용. 여기 없는 것은 저장되지 않음.
    // - (잠김/표시/도달 가능 집합은 ChapterTransition.Resolve의 출력)
    // - 기존 객체를 수정하지 않고 선택/챕터 종료 시, 새로운 객체를 생성하여 커밋함.
    public sealed class ProgressionState
    {
        private readonly Dictionary<string, int> _stats;

        public string CurrentChapterId { get; }
        public string CurrentEpisodeId { get; }

        public IReadOnlyDictionary<string, int> Stats => _stats;

        private ProgressionState(
            string currentChapterId,
            string currentEpisodeId,
            Dictionary<string, int> stats)
        {
            CurrentChapterId = currentChapterId;
            CurrentEpisodeId = currentEpisodeId;
            _stats = stats;
        }

        public static ProgressionState CreateInitial(
            IEnumerable<StatDefinition> stats,
            string startChapterId,
            string startEpisodeId)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            var values = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (StatDefinition stat in stats)
            {
                if (values.ContainsKey(stat.Key))
                    throw new ArgumentException($"스탯 키 '{stat.Key}'가 중복 정의됐다.", nameof(stats));

                values[stat.Key] = stat.Initial;
            }

            return new ProgressionState(
                startChapterId ?? string.Empty, startEpisodeId, values);
        }

        internal static ProgressionState FromSave(
            string chapterId, string episodeId, Dictionary<string, int> stats)
        {
            return new ProgressionState(chapterId, episodeId, stats);
        }

        // 스탯 값:
        // - 정의되지 않은 키는 0으로 떨어뜨리지 않고 던짐.
        // - 작가가 오타 낸 조건이 언제나 true로써,
        // - "언제나 통과하는 관문"으로 조용히 바뀌기에 찾기 힘든 버그됨.
        public int GetStat(string key)
        {
            if (_stats.TryGetValue(key, out int value))
                return value;

            throw new KeyNotFoundException(
                $"정의되지 않은 스탯 '{key}'. 정의된 것: {string.Join(", ", _stats.Keys)}");
        }

        // 선택지 커밋:
        // - 스탯 증감을 원자적으로 1회 반영,
        // - 도착 에피소드로 옮긴 새 상태 반환.
        public ProgressionState Commit(ChapterProgression chapter, EpisodeOption chosen)
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));

            if (chosen == null)
                throw new ArgumentNullException(nameof(chosen));

            var stats = new Dictionary<string, int>(_stats, StringComparer.Ordinal);

            IReadOnlyList<StatChange> changes = chosen.StatChanges;

            for (int i = 0; i < changes.Count; i++)
            {
                StatChange change = changes[i];

                if (!chapter.StatsByKey.TryGetValue(change.Key, out StatDefinition definition))
                {
                    throw new KeyNotFoundException(
                        $"정의되지 않은 스탯 '{change.Key}'을(를) 변경하려 한다. " +
                        $"정의된 것: {string.Join(", ", chapter.StatsByKey.Keys)}");
                }

                if (!stats.TryGetValue(change.Key, out int current))
                    current = definition.Initial;

                stats[change.Key] = definition.Clamp(change.ApplyTo(current));
            }

            return new ProgressionState(CurrentChapterId, chosen.TargetEpisodeId, stats);
        }

        // 챕터 하나를 끝낼 시 발생하는 트랙잭션:
        // - 다음 챕터의 시작 에피소드로 옮김.
        //
        // 무엇이 다음인지는 "ScenarioTransition.Resolve"가 먼저 정하고,
        // 여기서는 그 결정을 적용만 함.
        public ProgressionState CommitChapterEnding(
            ScenarioProgression scenario, in ScenarioAdvance advance)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));

            if (string.IsNullOrEmpty(CurrentChapterId))
                throw new InvalidOperationException(
                    "지금 챕터가 비어 있다. 챕터를 끝낼 수 없다.");

            string nextChapterId = CurrentChapterId;
            string nextEpisodeId = CurrentEpisodeId;

            if (advance.Kind == ScenarioAdvanceKind.NextChapter)
            {
                if (!scenario.TryGetChapter(advance.NextChapterId, out ChapterProgression next))
                    throw new ArgumentException(
                        $"다음 챕터 '{advance.NextChapterId}'가 시나리오 " + $"'{scenario.ScenarioId}'에 없다.", 
                        nameof(advance));

                nextChapterId = next.ChapterId;
                nextEpisodeId = next.StartEpisodeId;
            }

            return new ProgressionState(
                nextChapterId,
                nextEpisodeId,
                new Dictionary<string, int>(_stats, StringComparer.Ordinal));
        }
    }
}