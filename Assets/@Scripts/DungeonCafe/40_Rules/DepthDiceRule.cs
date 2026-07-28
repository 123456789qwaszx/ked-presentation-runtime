using System;

/// <summary>
/// 심층 주사위 1회분의 입력. (v3 §4.2)
///
/// 각 보정의 산출 책임은 호출부(플로우/상태)에 있고,
/// 이 규칙은 합산/클램프/구간 해석/가산만 담당한다.
/// 값은 전부 "주사위에 더해질 최종 정수"로 넘긴다 (적성만 예외 - 포인트를 넘기면 규칙이 -3/점을 적용).
/// </summary>
public readonly struct DepthRollInput
{
    /// <summary>요구축 대응력 포인트. 규칙이 -3/점으로 환산한다.</summary>
    public int DemandAxisAptitude { get; }

    /// <summary>상태이상 합산 보정. ([각인] +7 등, 양수 = 불리)</summary>
    public int StatusEffectModifier { get; }

    /// <summary>이해도 보정. (완전 파악 장착 시 -5)</summary>
    public int UnderstandingModifier { get; }

    /// <summary>플레이어 개입 합산 보정. (진정 신호 -10, 감응 차단 -8 등)</summary>
    public int InterventionModifier { get; }

    /// <summary>
    /// 상한형 효과. ([제압 유도] 최대값 50)
    /// 보정 클램프와 별개로, 클램프 이후에 적용된다. null 이면 미적용.
    /// </summary>
    public int? MaxValueCap { get; }

    public DepthRollInput(
        int demandAxisAptitude,
        int statusEffectModifier = 0,
        int understandingModifier = 0,
        int interventionModifier = 0,
        int? maxValueCap = null)
    {
        DemandAxisAptitude = demandAxisAptitude;
        StatusEffectModifier = statusEffectModifier;
        UnderstandingModifier = understandingModifier;
        InterventionModifier = interventionModifier;
        MaxValueCap = maxValueCap;
    }
}

/// <summary>
/// 심층 주사위 1회분의 판정 내역.
/// 전 항목을 보존한다 - UI 의 판정 연출(기본 78 -> 적성 -8 -> ...)과 커밋 로그가 이 구조체를 그대로 쓴다.
/// </summary>
public readonly struct DepthRollResult
{
    public int BaseRoll { get; }

    /// <summary>클램프 전 보정 합.</summary>
    public int RawModifierSum { get; }

    /// <summary>±클램프 적용 후 보정 합.</summary>
    public int ClampedModifierSum { get; }

    public bool WasModifierClamped => RawModifierSum != ClampedModifierSum;

    /// <summary>상한형 효과 적용 여부.</summary>
    public bool WasCapped { get; }

    /// <summary>구간 해석에 쓰인 최종값 (1~99).</summary>
    public int FinalValue { get; }

    public DepthBand Band { get; }

    /// <summary>붕괴 가산. 회수 구간은 0, 그 외 ⌈최종값/2⌉. (v3 §4.3)</summary>
    public int CollapseGain { get; }

    /// <summary>특수 구간 -> [각인] 부여 신호. 상태 반영은 호출부 책임.</summary>
    public bool InflictsBrand => Band == DepthBand.Special;

    /// <summary>회수 구간 -> 탈출/잔류 선택 개방 신호.</summary>
    public bool OpensRecoveryWindow => Band == DepthBand.Recovery;

    public DepthRollResult(
        int baseRoll,
        int rawModifierSum,
        int clampedModifierSum,
        bool wasCapped,
        int finalValue,
        DepthBand band,
        int collapseGain)
    {
        BaseRoll = baseRoll;
        RawModifierSum = rawModifierSum;
        ClampedModifierSum = clampedModifierSum;
        WasCapped = wasCapped;
        FinalValue = finalValue;
        Band = band;
        CollapseGain = collapseGain;
    }

    public override string ToString()
        => $"기본 {BaseRoll} 보정 {ClampedModifierSum:+0;-0;+0} -> 최종 {FinalValue} [{Band}] 가산 +{CollapseGain}";
}

/// <summary>
/// 붕괴심층 주사위 해석. (v3 §4)
///
/// 순수 계산이다. 굴림 소비 외에는 아무 상태도 건드리지 않는다.
/// 붕괴 반영/200 판정/회수 선택은 3단계의 DepthDiceFlow 가 이 결과를 받아 수행한다.
/// </summary>
public static class DepthDiceRule
{
    /// <summary>난수원에서 굴려 해석한다. 굴림 직전에 rng.State 를 커밋 로그에 남길 것.</summary>
    public static DepthRollResult Roll(
        IDiceSource dice,
        in DepthRollInput input,
        in DepthBandLayout layout,
        DungeonCafeTuning tuning)
    {
        return Interpret(dice.RollDie99(), input, layout, tuning);
    }

    /// <summary>이미 확정된 기본 굴림값을 해석한다. 커밋 로그 재생/테스트용.</summary>
    public static DepthRollResult Interpret(
        int baseRoll,
        in DepthRollInput input,
        in DepthBandLayout layout,
        DungeonCafeTuning tuning)
    {
        baseRoll = Math.Clamp(baseRoll, DepthBandLayout.DieMin, DepthBandLayout.DieMax);

        int rawModifierSum =
            input.DemandAxisAptitude * -tuning.DepthAptitudeDiePerPoint
            + input.StatusEffectModifier
            + input.UnderstandingModifier
            + input.InterventionModifier;

        int clampAbs = tuning.DepthModifierClampAbs;
        int clampedModifierSum = Math.Clamp(rawModifierSum, -clampAbs, clampAbs);

        int finalValue = Math.Clamp(
            baseRoll + clampedModifierSum,
            DepthBandLayout.DieMin,
            DepthBandLayout.DieMax);

        bool wasCapped = false;

        if (input.MaxValueCap.HasValue && finalValue > input.MaxValueCap.Value)
        {
            finalValue = Math.Max(DepthBandLayout.DieMin, input.MaxValueCap.Value);
            wasCapped = true;
        }

        DepthBand band = layout.Resolve(finalValue);
        int collapseGain = CalculateCollapseGain(finalValue, band, tuning);

        return new DepthRollResult(
            baseRoll,
            rawModifierSum,
            clampedModifierSum,
            wasCapped,
            finalValue,
            band,
            collapseGain);
    }

    /// <summary>회수 0, 그 외 ⌈최종값 / divisor⌉.</summary>
    public static int CalculateCollapseGain(int finalValue, DepthBand band, DungeonCafeTuning tuning)
    {
        if (band == DepthBand.Recovery)
            return 0;

        int divisor = tuning.DepthCollapseGainDivisor;

        return (finalValue + divisor - 1) / divisor;
    }
}
