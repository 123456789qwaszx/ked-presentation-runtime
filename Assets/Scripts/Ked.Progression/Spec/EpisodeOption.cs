using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    // 에피소드에서 나가는 길 - 저작 쪽 `간선` 시트의 한 행에 대응.
    public sealed class EpisodeOption
    {
        public string ChoiceLabel { get; }// 화면에 뜨는 문구.
        
        public string TargetEpisodeId { get; }
        
        public IReadOnlyList<ProgressionCondition> VisibleConditions { get; } // 표기조건.
        public IReadOnlyList<ProgressionCondition> Conditions { get; } // 해금 조건.

        public string LockedReasonText { get; }
        
        // 스탯이 변하는 유일한 곳
        public IReadOnlyList<StatChange> StatChanges { get; } 
        
        public string ViaNodeId { get; } // (Optional Node)
        public bool HasVia => ViaNodeId.Length != 0; // 이 길에 연출이 달려 있는지 체크.

        private EpisodeOption(
            string choiceLabel,
            string targetEpisodeId,
            IReadOnlyList<ProgressionCondition> visibleConditions,
            IReadOnlyList<ProgressionCondition> conditions,
            string lockedReasonText,
            IReadOnlyList<StatChange> statChanges,
            string viaNodeId)
        {
            // 저작 쪽은 "없음"과 "빈 것"을 구분하지 않음.(조건 없는 걸 그냥 빈 간선으로 내보내서 기획자가 채우도록.)
            ChoiceLabel = choiceLabel ?? string.Empty;
            LockedReasonText = lockedReasonText ?? string.Empty;
            VisibleConditions = visibleConditions ?? Array.Empty<ProgressionCondition>();
            Conditions = conditions ?? Array.Empty<ProgressionCondition>();
            StatChanges = statChanges ?? Array.Empty<StatChange>();

            TargetEpisodeId = targetEpisodeId ?? string.Empty;
            ViaNodeId = viaNodeId ?? string.Empty;

            ProgressionCondition.RequireAllConstructed(VisibleConditions, nameof(visibleConditions));
            ProgressionCondition.RequireAllConstructed(Conditions, nameof(conditions));
        }

        // 플레이어가 고르는 선택지.
        public static EpisodeOption Choice(
            string choiceLabel,
            string targetEpisodeId,
            IReadOnlyList<ProgressionCondition> visibleConditions = null,
            IReadOnlyList<ProgressionCondition> conditions = null,
            string lockedReasonText = null,
            IReadOnlyList<StatChange> statChanges = null,
            string viaNodeId = null)
        {
            return new EpisodeOption(
                choiceLabel,
                targetEpisodeId,
                visibleConditions,
                conditions,
                lockedReasonText,
                statChanges,
                viaNodeId);
        }

        public override string ToString()
        {
            string via = HasVia ? $" ~{ViaNodeId}~" : string.Empty;

            return $"\"{ChoiceLabel}\"{via} → {TargetEpisodeId}";
        }
    }
}