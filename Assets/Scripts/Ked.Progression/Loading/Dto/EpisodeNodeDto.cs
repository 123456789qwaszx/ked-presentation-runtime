using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class EpisodeNodeDto
    {
        public string EpisodeId { get; set; }
        public string Title { get; set; }
        public string DialogueEntryId { get; set; }

        public List<EpisodeOptionDto> NextOptions { get; set; }
    }
}