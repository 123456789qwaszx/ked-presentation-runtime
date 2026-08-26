using System.Collections.Generic;

namespace Ked.Progression
{
    // 저작 쪽 `ChapterProgressionExporter`가 내는 JSON과 필드 1:1.
    public sealed class ChapterProgressionDto
    {
        public string ChapterId { get; set; }
        public string DisplayName { get; set; }
        public string StartEpisodeId { get; set; }

        public List<StatDto> Stats { get; set; }

        public List<EpisodeNodeDto> Nodes { get; set; }

        // 시나리오 층의 간선. <b>툴은 아직 안 낸다</b>(언제나 빈 배열) — 그래서 이 모양은
        // 손으로 쓰는 시나리오 JSON이 먼저 쓰고, 툴이 나중에 맞춘다(X2).
        public List<EndingRuleDto> EndingRules { get; set; }
    }
}