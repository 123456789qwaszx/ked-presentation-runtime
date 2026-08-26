using System.Collections.Generic;

namespace Ked.Progression
{
    /// <summary>
    /// 챕터에서 나가는 길.
    ///
    /// ⚠ <b><see cref="Outcome"/>을 명시로 둔 것이 <see cref="EpisodeOptionDto.ChoiceLabel"/>과
    /// 다른 점이다.</b> 저작 데이터의 간선 종류는 이미 sentinel로 굳어 있어 로더가 번역할 수밖에
    /// 없지만(D5), 이 모양은 아직 아무도 안 쓰므로 처음부터 명시할 수 있다.
    /// <c>NextChapterId</c>가 비었는지로 판별하면 "다음 챕터를 실수로 안 적음"과
    /// "여기서 끝남"이 같은 모양이 된다.
    /// </summary>
    public sealed class EndingRuleDto
    {
        /// <summary><c>"NextChapter"</c> 또는 <c>"ScenarioEnd"</c>.</summary>
        public string Outcome { get; set; }

        public string EndingKey { get; set; }
        public string DisplayName { get; set; }
        public List<ConditionDto> Conditions { get; set; }

        /// <summary><c>Outcome</c>이 <c>ScenarioEnd</c>면 비어 있어야 한다.</summary>
        public string NextChapterId { get; set; }

        public string DesignerNote { get; set; }
    }
}