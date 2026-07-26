/// <summary>
/// 밤에 메이드 한 명에게 적용한 처리의 결과.
/// </summary>
public sealed class NightProgramResult
{
    public NightProgramKind Kind { get; private set; }
    public string MaidId { get; private set; }
    public BurdenAxis Axis { get; private set; }

    public int CollapseBefore { get; private set; }

    /// <summary>관리 붕괴에서 일시적으로 도달한 최고치. 회복 처리에서는 Before 와 같다.</summary>
    public int CollapsePeak { get; private set; }

    public int CollapseAfter { get; private set; }

    public int ExperienceGain { get; private set; }

    public bool IsSuccess { get; private set; }
    public string FailureReason { get; private set; }

    /// <summary>이 처리 직후 숙련 이벤트가 준비되었는지 여부.</summary>
    public bool IsMasteryEventReady { get; private set; }

    public static NightProgramResult Failed(
        NightProgramKind kind,
        string maidId,
        BurdenAxis axis,
        string reason)
    {
        return new NightProgramResult
        {
            Kind = kind,
            MaidId = maidId,
            Axis = axis,
            IsSuccess = false,
            FailureReason = reason,
        };
    }

    public static NightProgramResult Succeeded(
        NightProgramKind kind,
        string maidId,
        BurdenAxis axis,
        int collapseBefore,
        int collapsePeak,
        int collapseAfter,
        int experienceGain,
        bool isMasteryEventReady)
    {
        return new NightProgramResult
        {
            Kind = kind,
            MaidId = maidId,
            Axis = axis,
            CollapseBefore = collapseBefore,
            CollapsePeak = collapsePeak,
            CollapseAfter = collapseAfter,
            ExperienceGain = experienceGain,
            IsMasteryEventReady = isMasteryEventReady,
            IsSuccess = true,
        };
    }

    public string ToSummaryLine()
        => IsSuccess
            ? $"{BurdenAxes.ToBurdenLabel(Axis)} {CollapseBefore} → {CollapseAfter} / 경험 +{ExperienceGain}"
            : $"실패: {FailureReason}";
}
