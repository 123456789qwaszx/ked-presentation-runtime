using System;

/// <summary>낮 행동 옵션의 부하 범위. (v3 §2.2 - 약 6~10 / 중 10~16 / 강 15~23)</summary>
public readonly struct LoadRange
{
    public int Min { get; }
    public int Max { get; }

    public LoadRange(int min, int max)
    {
        Min = Math.Max(0, min);
        Max = Math.Max(Min, max);
    }

    /// <summary>v3 §2.2 표준 3계층.</summary>
    public static LoadRange Light => new(6, 10);
    public static LoadRange Medium => new(10, 16);
    public static LoadRange Heavy => new(15, 23);

    public override string ToString() => $"{Min}~{Max}";
}

/// <summary>
/// 낮 부하 판정 1회분의 내역.
/// RawLoad(완화 전)는 숙련 경험치의 기준이므로 반드시 보존한다 (기존 MasteryExperienceRule 계약).
/// </summary>
public readonly struct LoadJudgmentResult
{
    /// <summary>범위 내 굴림값.</summary>
    public int RangeRoll { get; }

    /// <summary>굴림 + 개체 부하 보정. 완화 전 원본 부하 - 숙련 경험치 기준.</summary>
    public int RawLoad { get; }

    /// <summary>완화 후 실제 누적될 부하.</summary>
    public int AppliedLoad { get; }

    /// <summary>최소 부하 바닥에 걸렸는지. HUD 의 "완화 한계" 표시용.</summary>
    public bool WasFloored { get; }

    public LoadJudgmentResult(int rangeRoll, int rawLoad, int appliedLoad, bool wasFloored)
    {
        RangeRoll = rangeRoll;
        RawLoad = rawLoad;
        AppliedLoad = appliedLoad;
        WasFloored = wasFloored;
    }

    public override string ToString()
        => $"굴림 {RangeRoll} -> 원본 {RawLoad} -> 적용 {AppliedLoad}{(WasFloored ? " (바닥)" : "")}";
}

/// <summary>
/// 낮 접객의 부하 범위 판정. (v3 §2.3)
///
///   적용 부하 = clamp( roll(범위) + 개체 보정 − 적성x2 − 숙련Lvx1, 최소 4 )
///
/// 기존 BurdenAccrualRule(고정 부하/적성x1) 을 대체할 v3 규칙이지만,
/// 이 단계에서는 나란히 존재하며 기존 코드는 수정하지 않는다.
/// 순수 계산 - 붕괴 반영은 호출부가 결과를 받아 수행한다.
/// </summary>
public static class LoadRangeJudgmentRule
{
    public static LoadJudgmentResult Judge(
        IDiceSource dice,
        in LoadRange range,
        int monsterLoadModifier,
        int demandAxisAptitude,
        int masteryLevel,
        GuesthouseTuningV3 tuning)
    {
        int roll = dice.NextInclusive(range.Min, range.Max);

        return Interpret(roll, monsterLoadModifier, demandAxisAptitude, masteryLevel, tuning);
    }

    /// <summary>확정 굴림값 해석. 커밋 로그 재생/테스트용.</summary>
    public static LoadJudgmentResult Interpret(
        int rangeRoll,
        int monsterLoadModifier,
        int demandAxisAptitude,
        int masteryLevel,
        GuesthouseTuningV3 tuning)
    {
        int rawLoad = Math.Max(0, rangeRoll + monsterLoadModifier);

        int mitigated = rawLoad
            - Math.Max(0, demandAxisAptitude) * tuning.DayAptitudeMitigationPerPoint
            - Math.Max(0, masteryLevel) * tuning.DayMasteryMitigationPerLevel;

        bool floored = mitigated < tuning.DayMinimumAppliedLoad;
        int applied = floored ? tuning.DayMinimumAppliedLoad : mitigated;

        return new LoadJudgmentResult(rangeRoll, rawLoad, applied, floored);
    }
}
