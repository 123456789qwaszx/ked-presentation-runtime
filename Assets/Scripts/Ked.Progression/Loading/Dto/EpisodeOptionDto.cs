using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class EpisodeOptionDto
    {
        public string TargetEpisodeId { get; set; }
        public List<StatChangeDto> StatChanges { get; set; }
        public string ChoiceLabel { get; set; }
        
        public List<ConditionDto> VisibleConditions { get; set; }
        public List<ConditionDto> Conditions { get; set; }
        
        public string LockedReasonText { get; set; }

        /// <summary>
        /// 이 길을 지나며 먼저 거쳐 가는 <b>Custom Yarn 노드</b>(optional)
        /// 비어 있으면 곧장 감.
        /// </summary>
        public string ViaNodeId { get; set; }
    }
}