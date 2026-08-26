using System.Collections.Generic;

namespace Ked.Progression
{
    public sealed class EpisodeOptionDto
    {
        public string TargetEpisodeId { get; set; }

        /// <summary>
        /// 화면에 뜨는 문구. <b>비어 있으면 자동 진행이다</b> — 간선의 종류는 따로 적지 않고
        /// 문구의 유무로 가른다. 이것이 규약이다.
        ///
        /// ⚠ 그래서 문구를 실수로 지운 선택지는 조용히 자동 진행이 된다. 한 에피소드에서
        /// 둘 이상 지우면 <c>EpisodeNode</c>가 "자동 진행 간선이 둘 이상"으로 잡는다.
        /// </summary>
        public string ChoiceLabel { get; set; }

        public List<ConditionDto> VisibleConditions { get; set; }
        public List<ConditionDto> Conditions { get; set; }

        public string LockedReasonText { get; set; }
        public List<StatChangeDto> StatChanges { get; set; }

        /// <summary>
        /// 연출을 매다는 자리(계약서 §H-3). 이 길을 지나며 먼저 거쳐 가는 <b>Yarn 노드</b>
        /// 이름이고, 비어 있으면 곧장 간다. 에피소드 사이 트랜지션과 엔딩 연출이 같은 칸을 쓴다.
        ///
        /// ⚠ 이름 하나만 온다. 지속시간·이징 같은 파라미터가 여기 붙기 시작하면
        /// 경계면이 넓어진다 — 그건 연출 쪽에서 산다.
        /// </summary>
        public string ViaNodeId { get; set; }
    }
}