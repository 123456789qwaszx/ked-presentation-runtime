using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class EpisodeNodeDto
    {
        public string EpisodeId { get; set; }
        public string Title { get; set; }
        public string DialogueEntryId { get; set; }

        // "이 에피소드를 다 시청했을 때"의 이벤트·보상 트리거.
        // (툴은 해석하지 않음.)
        public string EventKey { get; set; }

        public List<EpisodeOptionDto> NextOptions { get; set; }
    }
}