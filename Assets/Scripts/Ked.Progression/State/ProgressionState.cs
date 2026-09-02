using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    // 챕터 진행 상태.
    public sealed class ProgressionState
    {
        private readonly Dictionary<string, int> _stats;

        public string CurrentEpisodeId { get; }

        public IReadOnlyDictionary<string, int> Stats => _stats;

        private ProgressionState(string currentEpisodeId, Dictionary<string, int> stats)
        {
            CurrentEpisodeId = currentEpisodeId;
            _stats = stats;
        }

        public static ProgressionState CreateInitial(IEnumerable<StatDefinition> stats, string startEpisodeId)
        {
            var values = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (StatDefinition stat in stats)
            {
                if (values.ContainsKey(stat.Key))
                    throw new ArgumentException($"스탯 키 '{stat.Key}'가 중복 정의됐다.", nameof(stats));

                values[stat.Key] = stat.Initial;
            }

            return new ProgressionState(startEpisodeId, values);
        }
        
        public int GetStat(string key)
        {
            if (_stats.TryGetValue(key, out int value))
                return value;

            // - 정의되지 않은 키는 0으로 떨어뜨리지 않고 던짐.
            // - 작가가 오타 낸 조건이 언제나 true로써,
            // - "언제나 통과하는 관문"으로 조용히 바뀌기에 찾기 힘든 버그를 유발하기 때문.
            throw new KeyNotFoundException(
                $"정의되지 않은 스탯 '{key}'. 정의된 것: {string.Join(", ", _stats.Keys)}");
        }
        
        // 장면 내, 현재까지의 pending choice를 적용한 작업 상태를 계산
        // - 이후 선택지의 조건 판정에 사용.
        // - 롤백 시 pending을 줄여 다시 계산.
        // - Scene이 끝나면 최종 상태로 확정.
        public ProgressionState FoldChoices(ChapterProgression chapter, IReadOnlyList<EpisodeOption> choices)
        {
            ProgressionState state = this;

            for (int i = 0; i < choices.Count; i++)
                state = state.ApplyChoice(chapter, choices[i]);

            return state;
        }
        
        // 유일한 스탯 입력 자리.
        // 간선의 StatChange를 순서대로 반영하고,
        // 도착 에피소드로 이동한 새 ProgressionState를 반환.
        public ProgressionState ApplyChoice(ChapterProgression chapter, EpisodeOption choices)
        {
            var stats = new Dictionary<string, int>(_stats, StringComparer.Ordinal);

            IReadOnlyList<StatChange> changes = choices.StatChanges;

            for (int i = 0; i < changes.Count; i++)
            {
                StatChange change = changes[i];
                StatDefinition definition = chapter.StatsByKey[change.Key];

                if (!stats.TryGetValue(change.Key, out int current))
                    throw new InvalidOperationException($"{chapter.ChapterId} 상태에 스탯 '{change.Key}'가 없다.");

                stats[change.Key] = definition.Clamp(change.ApplyTo(current));
            }

            return new ProgressionState(choices.TargetEpisodeId, stats);
        }
        
        public static ProgressionState Restore(
            ChapterProgression chapter, 
            string currentEpisodeId, 
            IReadOnlyDictionary<string, int> savedStats)
        {
            var values = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (StatDefinition stat in chapter.Stats)
            {
                values[stat.Key] = savedStats.TryGetValue(stat.Key, out int saved)
                    ? stat.Clamp(saved)
                    : stat.Initial;
            }

            return new ProgressionState(currentEpisodeId, values);
        }
    }
}