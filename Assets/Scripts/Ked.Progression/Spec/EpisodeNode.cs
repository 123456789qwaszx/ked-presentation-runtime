using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class EpisodeNode
    {
        public string EpisodeId { get; }
        public string Title { get; }
        public string DialogueEntryId { get; } // 호스트가 재생할 대본 키.
        public IReadOnlyList<EpisodeOption> NextOptions { get; } // 간선

        public string EndingKey { get; }

        public EpisodeNode(
            string episodeId,
            string title,
            string dialogueEntryId,
            IReadOnlyList<EpisodeOption> nextOptions = null,
            string endingKey = null)
        {
            EpisodeId = episodeId;
            Title = title ?? string.Empty;
            DialogueEntryId = dialogueEntryId ?? string.Empty;
            NextOptions = nextOptions ?? Array.Empty<EpisodeOption>();
            EndingKey = endingKey ?? string.Empty;
        }
    }
}