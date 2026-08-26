using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class EpisodeNodeDto
    {
        public string EpisodeId { get; set; }
        public string Title { get; set; }

        /// <summary>v5에서 폐지됐다. 언제나 빈 문자열 — 통과값이다.</summary>
        public string IndexText { get; set; }

        public string DialogueEntryId { get; set; }

        /// <summary>v8에서 간선으로 내려갔다. 언제나 빈 배열이어야 한다 — 로더가 확인한다.</summary>
        public List<ConditionDto> VisibleConditions { get; set; }

        /// <summary>v8에서 간선으로 내려갔다. 언제나 빈 배열이어야 한다 — 로더가 확인한다.</summary>
        public List<ConditionDto> UnlockConditions { get; set; }

        public List<EpisodeOptionDto> NextOptions { get; set; }

        /// <summary>
        /// ⚠ 모델에는 없다. <see cref="EndingKey"/> 하나로 판별하고, 이 값과 어긋나면
        /// 로더가 오류를 낸다 — 어느 쪽이 이기는지 추측하지 않는다.
        /// </summary>
        public bool IsChapterEndingCandidate { get; set; }

        public string EndingKey { get; set; }
        public string DesignerNote { get; set; }

        /// <summary>저작 레이아웃(G-2 확장). 평가 입력이 아니다 — 통과값이다.</summary>
        public PositionDto Position { get; set; }
    }
}