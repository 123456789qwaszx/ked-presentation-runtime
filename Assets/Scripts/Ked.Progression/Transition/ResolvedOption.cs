namespace Ked.Progression
{
    public enum OptionVisibility
    {
        Shown = 0,
        Locked = 1,
    }

    // 선택지 하나의 판정 결과
    public readonly struct ResolvedOption
    {
        public EpisodeOption Option { get; }
        public OptionVisibility Visibility { get; }

        // 저작자가 쓴 잠금 안내문.
        public string LockedReason { get; }

        // 미달 조건 중 첫번째 것만 반환.
        public ProgressionCondition BlockingCondition { get; }

        public bool IsSelectable => Visibility == OptionVisibility.Shown;

        private ResolvedOption(
            EpisodeOption option,
            OptionVisibility visibility,
            string lockedReason,
            ProgressionCondition blockingCondition)
        {
            Option = option;
            Visibility = visibility;
            LockedReason = lockedReason;
            BlockingCondition = blockingCondition;
        }

        internal static ResolvedOption Shown(EpisodeOption option) =>
            new(option, OptionVisibility.Shown, string.Empty, default);

        internal static ResolvedOption Locked(EpisodeOption option, ProgressionCondition blocking) =>
            new(option, OptionVisibility.Locked, option.LockedReasonText, blocking);

        public override string ToString() =>
            Visibility == OptionVisibility.Shown
                ? $"{Option}"
                : $"{Option} [잠김: {BlockingCondition}]";
    }
}