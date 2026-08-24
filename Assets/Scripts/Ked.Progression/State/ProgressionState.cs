using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    // 챕터 하나를 도는 동안의 진행 상태 — [2] 계층.
    //
    // 수명이 챕터다. 챕터가 끝나면 이 객체는 버려지고, 다음 챕터는 자기 것을 새로 만든다.
    // 챕터를 넘어 사는 것은 [1] 영구 계층의 일인데 그 계층은 아직 서지 않았다 —
    // 그래서 여기에 챕터 ID가 없다. "지금 어느 챕터인가"는 이 상태를 굴리는 쪽이 안다.
    //
    // 스탯이 바뀌는 자리는 Commit 하나뿐이고, 그 입력은 간선이 든 StatChange뿐이다.
    // - 잠김/표시/도달 가능 집합은 ChapterTransition.Resolve의 출력이지 상태가 아니다.
    // - 기존 객체를 수정하지 않고 선택마다 새 객체를 만들어 커밋한다.
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

        public static ProgressionState CreateInitial(
            IEnumerable<StatDefinition> stats, string startEpisodeId)
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

            return new ProgressionState(startEpisodeId, values);
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

        // 선택지 커밋 — 스탯이 바뀌는 유일한 자리:
        // - 간선이 든 증감을 원자적으로 1회 반영,
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

                // 상태는 이 챕터의 스탯을 전부 들고 시작한다(CreateEntryState).
                // 없다는 것은 상태와 챕터가 갈렸다는 뜻이라 초기값으로 메우지 않는다 —
                // 조용히 메우면 다른 챕터의 값이 한 칸에만 섞인다.
                if (!stats.TryGetValue(change.Key, out int current))
                    throw new InvalidOperationException(
                        $"상태에 스탯 '{change.Key}'가 없다. " +
                        $"이 상태는 챕터 '{chapter.ChapterId}'의 것이 아니다.");

                stats[change.Key] = definition.Clamp(change.ApplyTo(current));
            }

            return new ProgressionState(chosen.TargetEpisodeId, stats);
        }
    }
}
