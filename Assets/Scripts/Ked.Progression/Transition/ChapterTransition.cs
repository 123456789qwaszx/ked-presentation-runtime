using System;
using System.Collections.Generic;

namespace Ked.Progression
{
    public enum OptionVisibility
    {
        Shown = 0,
        Locked = 1,
    }

    // 판정이 끝난 선택지 하나. 해석 결과이지 상태가 아니므로 저장하지 않음.
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

    public enum ChapterAdvanceKind
    {
        AwaitPlayerChoice = 0, // 고를 수 있는 것이 하나 이상 있다. 플레이어의 입력을 기다림.
        AutoAdvance = 1, // 고를 수 있는 것이 없을 시, 자동으로 진행.
        ChapterEnded = 2,
    }

    // 지금 에피소드에서 발생해야 할 일. 해석 결과이지 상태가 아니므로 저장하지 않음.
    public readonly struct ChapterAdvance
    {
        public ChapterAdvanceKind Kind { get; }

        // 화면에 그릴 목록.
        // "ChapterAdvanceKind.AwaitPlayerChoice"일 때만 사용.
        public IReadOnlyList<ResolvedOption> Options { get; }

        // "ChapterAdvanceKind.AutoAdvance일 때만 사용.
        public EpisodeOption AutoOption { get; }

        // "ChapterAdvanceKind.ChapterEnded"일 때 지금 노드의 엔딩키.
        // 엔딩 후보가 아닌 노드에서 끝났으면 빈 문자열.
        public string EndingKey { get; }

        // 표시조건 미달로 목록에서 빠진 개수. 그리는 데는 안 쓰인다.
        // (에디터 및 디버깅 용)
        public int HiddenCount { get; }

        internal ChapterAdvance(
            ChapterAdvanceKind kind,
            IReadOnlyList<ResolvedOption> options,
            EpisodeOption autoOption,
            string endingKey,
            int hiddenCount)
        {
            Kind = kind;
            Options = options;
            AutoOption = autoOption;
            EndingKey = endingKey;
            HiddenCount = hiddenCount;
        }

        public override string ToString() => $"{Kind}(보임 {Options.Count}, 숨김 {HiddenCount})";
    }

    // 지금 에피소드에서 갈 수 있는 길을 확정.
    // - 계획을 먼저 확정하고 그 뒤엔 고르기만 함.
    // - 여기서 한 번 판정하면 그 결과를 화면에 표시.
    // - 플레이어가 고른 뒤에야 값 변경.
    public static class ChapterTransition
    {
        private static readonly ResolvedOption[] None = new ResolvedOption[0];

        public static ChapterAdvance Resolve(ChapterProgression chapter, ProgressionState state)
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!chapter.TryGetNode(state.CurrentEpisodeId, out EpisodeNode node))
            {
                throw new ArgumentException(
                    $"지금 에피소드 '{state.CurrentEpisodeId}'가 챕터 '{chapter.ChapterId}'에 없다.",
                    nameof(state));
            }

            var shownOrLocked = new List<ResolvedOption>();
            int hidden = 0;
            bool anySelectable = false;

            IReadOnlyList<EpisodeOption> options = node.NextOptions;

            for (int i = 0; i < options.Count; i++)
            {
                EpisodeOption option = options[i];

                if (option.Kind == OptionKind.AutoAdvance)
                    continue;
                
                if (FirstUnmet(option.VisibleConditions, state).IsConstructed)
                {
                    // 표시조건 미달이면 목록에 만들지 않는다.
                    hidden++;
                    continue;
                }

                ProgressionCondition blocking = FirstUnmet(option.Conditions, state);

                if (!blocking.IsConstructed)
                {
                    shownOrLocked.Add(ResolvedOption.Shown(option));
                    anySelectable = true;
                    continue;
                }

                shownOrLocked.Add(ResolvedOption.Locked(option, blocking));
            }

            if (anySelectable)
            {
                return new ChapterAdvance(
                    ChapterAdvanceKind.AwaitPlayerChoice,
                    shownOrLocked, null, string.Empty, hidden);
            }

            // 고를 수 있는 것이 하나도 없다. 잠긴 것들은 화면에 세우지 않는다.
            if (node.TryGetAutoOption(out EpisodeOption auto))
            {
                return new ChapterAdvance(
                    ChapterAdvanceKind.AutoAdvance, None, auto, string.Empty, hidden);
            }

            return new ChapterAdvance(
                ChapterAdvanceKind.ChapterEnded, None, null, node.EndingKey, hidden);
        }

        // 미달 조건 중 첫번째 것.
        private static ProgressionCondition FirstUnmet(
            IReadOnlyList<ProgressionCondition> conditions, ProgressionState state)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (!ConditionEvaluator.IsMet(conditions[i], state))
                    return conditions[i];
            }

            return default;
        }
    }
}