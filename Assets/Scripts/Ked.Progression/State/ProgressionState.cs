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

        // 저장에서 복원한 [2] 상태 (M7).
        //
        // CreateInitial과 같은 자리에서 출발하되, 저장된 값이 있는 스탯만 그 값으로 덮는다.
        // 기준이 "저장본"이 아니라 "챕터 정의"인 이유:
        // - Commit은 상태가 챕터의 스탯을 전부 들고 있다고 가정한다(없으면 던진다) —
        //   저장 후 챕터에 스탯이 추가됐다면, 저장본만 믿고 만들면 그 가정이 깨진다.
        // - 반대로 저장본에만 있는 키(챕터에서 빠진 스탯)는 조용히 버린다 —
        //   서버가 개편 뒤의 이벤트 재도달을 흡수하는 것과 같은 결이다(D-011).
        // 저장된 값은 Clamp를 다시 지난다 — 저장 후 스탯의 min/max가 바뀌었어도 범위 안이다.
        public static ProgressionState Restore(
            ChapterProgression chapter,
            string currentEpisodeId,
            IReadOnlyDictionary<string, int> savedStats)
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));

            // 에피소드가 챕터에 없으면 복원 자체가 성립하지 않는다. 여기서 던지고,
            // "그럼 처음부터"라는 결정은 호출자(드라이버)가 한다 — 정책은 상태의 일이 아니다.
            if (!chapter.TryGetNode(currentEpisodeId, out _))
            {
                throw new ArgumentException(
                    $"복원할 에피소드 '{currentEpisodeId}'가 챕터 '{chapter.ChapterId}'에 없다.",
                    nameof(currentEpisodeId));
            }

            var values = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (StatDefinition stat in chapter.Stats)
            {
                int value = stat.Initial;

                if (savedStats != null && savedStats.TryGetValue(stat.Key, out int saved))
                    value = stat.Clamp(saved);

                values[stat.Key] = value;
            }

            return new ProgressionState(currentEpisodeId, values);
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
            VerifyReachableFromHere(chapter, chosen);

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

        // 고른 선택지가 지금 있는 에피소드에서 나가는 길인지 확인한다.
        //
        // 챕터 생성자는 "모든 간선이 실재하는 노드에 착지한다"까지만 보장하지, 호출자가
        // 엉뚱한 노드의 선택지를 넘기는 것은 막지 못한다. 그대로 통과시키면 그래프에 없는
        // 경로로 이동한 상태가 만들어지고, 도달성 증명이 보증한 것과 실제 플레이가 갈린다.
        //
        // 상태가 챕터 ID를 들지 않으므로 챕터가 짝이 맞는지도 여기서 함께 본다 —
        // 다른 챕터의 것이면 지금 에피소드가 그 챕터에 없다.
        private void VerifyReachableFromHere(ChapterProgression chapter, EpisodeOption chosen)
        {
            if (!chapter.TryGetNode(CurrentEpisodeId, out EpisodeNode node))
            {
                throw new ArgumentException(
                    $"지금 에피소드 '{CurrentEpisodeId}'가 챕터 '{chapter.ChapterId}'에 없다.",
                    nameof(chapter));
            }

            IReadOnlyList<EpisodeOption> options = node.NextOptions;

            for (int i = 0; i < options.Count; i++)
            {
                if (ReferenceEquals(options[i], chosen))
                    return;
            }

            throw new ArgumentException(
                $"고른 선택지({chosen})가 에피소드 '{CurrentEpisodeId}'에서 나가는 길이 아니다.",
                nameof(chosen));
        }
    }
}