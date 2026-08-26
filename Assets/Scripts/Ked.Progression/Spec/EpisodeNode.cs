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

        // 시청 완료 시 이벤트·보상 트리거. 해석 없이 실어 나르기만 함.
        public string EventKey { get; }

        public EpisodeNode(
            string episodeId,
            string title,
            string dialogueEntryId,
            IReadOnlyList<EpisodeOption> nextOptions = null,
            string eventKey = null)
        {
            EpisodeId = episodeId;
            Title = title ?? string.Empty;
            DialogueEntryId = dialogueEntryId ?? string.Empty;
            NextOptions = nextOptions ?? Array.Empty<EpisodeOption>();
            EventKey = eventKey ?? string.Empty;
        }
    }
}