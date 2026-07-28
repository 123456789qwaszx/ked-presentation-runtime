using System;

/// <summary>심층 주사위 결과 구간. </summary>
public enum DepthBand
{
    /// <summary>1~회수상한. 메이드가 잠깐 정신을 차린다. 붕괴 가산 0.</summary>
    Recovery = 0,

    /// <summary>위험 행동.</summary>
    Risky = 1,

    /// <summary>치명 행동.</summary>
    Fatal = 2,

    /// <summary>개체 고유 특수 행동. [brand 후유증] 부여.</summary>
    Special = 3,
}

/// <summary>
/// 심층 결과 구간의 경계 3개 (회수 상한 / 위험 상한 / 치명 상한).
/// 특수 구간은 치명 상한+1 ~ 99 로 항상 존재한다.
///
/// 메이드 성향/기벽/능력은 이 레이아웃을 변형해서 개입.
/// 변형이 아무리 겹쳐도 4구간 구조는 유지된다:
///   구간 소멸 금지, 각 구간 최소 폭 보장 (기본 4).
/// 변형은 항상 새 인스턴스를 반환한다.
/// </summary>
public readonly struct DepthBandLayout : IEquatable<DepthBandLayout>
{
    public const int DieMin = 1;
    public const int DieMax = 99;

    /// <summary>회수 구간 상한 (포함).</summary>
    public int RecoveryMax { get; }

    /// <summary>위험 구간 상한 (포함).</summary>
    public int RiskyMax { get; }

    /// <summary>치명 구간 상한 (포함). 특수 구간은 이 값+1 부터.</summary>
    public int FatalMax { get; }

    /// <summary> 기본형: 1~20 회수 / 21~60 위험 / 61~94 치명 / 95~99 특수.</summary>
    public static DepthBandLayout Standard => new(20, 60, 94);

    public DepthBandLayout(int recoveryMax, int riskyMax, int fatalMax)
    {
        RecoveryMax = recoveryMax;
        RiskyMax = riskyMax;
        FatalMax = fatalMax;
    }

    public DepthBand Resolve(int finalValue)
    {
        if (finalValue <= RecoveryMax)
            return DepthBand.Recovery;

        if (finalValue <= RiskyMax)
            return DepthBand.Risky;

        if (finalValue <= FatalMax)
            return DepthBand.Fatal;

        return DepthBand.Special;
    }

    /// <summary>
    /// 회수 상한을 delta 만큼 이동한다. (시온 성향 +8, 루이 [이름을 부른다] +8,
    /// [공동의 흔적] +4, 침면 [침하] -4, [침묵 훈련] +4)
    /// </summary>
    public DepthBandLayout ShiftRecoveryMax(int delta, int minBandWidth)
        => Normalized(RecoveryMax + delta, RiskyMax, FatalMax, minBandWidth);

    /// <summary>위험 상한을 delta 만큼 이동한다. (루이 성향: 치명 하한 61->58 은 -3)</summary>
    public DepthBandLayout ShiftRiskyMax(int delta, int minBandWidth)
        => Normalized(RecoveryMax, RiskyMax + delta, FatalMax, minBandWidth);

    /// <summary>치명 상한을 delta 만큼 이동한다. 특수 구간 폭이 함께 변한다.</summary>
    public DepthBandLayout ShiftFatalMax(int delta, int minBandWidth)
        => Normalized(RecoveryMax, RiskyMax, FatalMax + delta, minBandWidth);

    /// <summary>
    /// 최소 폭 불변식을 강제한다.
    /// 앞 구간의 확장이 뒤 구간을 침식할 때, 뒤 구간은 최소 폭까지만 밀린다.
    /// 순서: 회수부터 확정하고 위험/치명을 차례로 민다.
    /// </summary>
    private static DepthBandLayout Normalized(
        int recoveryMax,
        int riskyMax,
        int fatalMax,
        int minBandWidth)
    {
        int width = Math.Max(1, minBandWidth);

        // 각 구간이 최소 폭을 갖기 위한 상한/하한.
        //   회수:   width .. 99 - width*3
        //   위험:   회수+width .. 99 - width*2
        //   치명:   위험+width .. 99 - width
        int recovery = Math.Clamp(recoveryMax, width, DieMax - width * 3);
        int risky = Math.Clamp(riskyMax, recovery + width, DieMax - width * 2);
        int fatal = Math.Clamp(fatalMax, risky + width, DieMax - width);

        return new DepthBandLayout(recovery, risky, fatal);
    }

    public bool Equals(DepthBandLayout other)
        => RecoveryMax == other.RecoveryMax
           && RiskyMax == other.RiskyMax
           && FatalMax == other.FatalMax;

    public override bool Equals(object obj) => obj is DepthBandLayout other && Equals(other);

    public override int GetHashCode() => (RecoveryMax * 397 ^ RiskyMax) * 397 ^ FatalMax;

    public override string ToString()
        => $"회수 1~{RecoveryMax} / 위험 {RecoveryMax + 1}~{RiskyMax} / 치명 {RiskyMax + 1}~{FatalMax} / 특수 {FatalMax + 1}~99";
}