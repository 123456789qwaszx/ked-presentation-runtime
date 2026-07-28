using System.Collections.Generic;

/// <summary>
/// 메이드가 감당하는 부담의 세 축.
/// 몬스터의 부하 성향, 메이드의 대응력, 누적 붕괴도, 업무 숙련도가 모두 이 축을 공유한다.
/// 새 축을 추가할 계획이 없다면 인덱스는 0..Count-1 로 고정 유지한다.
/// </summary>
public enum BurdenAxis
{
    // 압박, 충돌, 구속, 중량, 독성 같은 물리적 위험. 누적 부담 표기는 '상처'.
    Physical = 0,

    // 공포, 환각, 죄책감, 기억 침식, 언어적 공격. 누적 부담 표기는 '스트레스'.
    Mental = 1,

    // 유혹, 흥분, 집착, 욕망 증폭. 누적 부담 표기는 '충동'.
    Empathic = 2,
}

public static class BurdenAxes
{
    public const int Count = 3;

    private static readonly BurdenAxis[] AllAxes =
    {
        BurdenAxis.Physical,
        BurdenAxis.Mental,
        BurdenAxis.Empathic,
    };

    public static IReadOnlyList<BurdenAxis> All => AllAxes;

    public static int ToIndex(BurdenAxis axis) => (int)axis;

    public static BurdenAxis FromIndex(int index)
    {
        if (index < 0 || index >= Count)
            return BurdenAxis.Physical;

        return AllAxes[index];
    }

    /// <summary>메이드 상태창에 노출되는 누적 부담 이름.</summary>
    public static string ToBurdenLabel(BurdenAxis axis) => axis switch
    {
        BurdenAxis.Physical => "상처",
        BurdenAxis.Mental => "스트레스",
        BurdenAxis.Empathic => "충동",
        _ => axis.ToString(),
    };

    /// <summary>메이드 상태창에 노출되는 영구 대응력 이름.</summary>
    public static string ToAptitudeLabel(BurdenAxis axis) => axis switch
    {
        BurdenAxis.Physical => "육체 대응력",
        BurdenAxis.Mental => "정신 대응력",
        BurdenAxis.Empathic => "감응 대응력",
        _ => axis.ToString(),
    };

    /// <summary>업무 수첩과 숙련 이벤트에 노출되는 숙련 트랙 이름.</summary>
    public static string ToMasteryLabel(BurdenAxis axis) => axis switch
    {
        BurdenAxis.Physical => "육체 업무 경험",
        BurdenAxis.Mental => "정신 업무 경험",
        BurdenAxis.Empathic => "감응 업무 경험",
        _ => axis.ToString(),
    };
}