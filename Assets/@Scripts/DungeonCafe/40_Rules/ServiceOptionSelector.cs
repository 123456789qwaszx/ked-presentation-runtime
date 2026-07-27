using System.Collections.Generic;

/// <summary>
/// 비트의 행동 후보 풀에서 메이드가 실제로 제안할 행동을 추린다.
///
/// 능력치와 성향이 제안 목록을 만들고, 최종 실행은 관리자의 승인을 받는다.
/// 요구 대응력에 미달하는 행동도 후보에 남을 수 있다.
/// 메이드가 위험한 행동을 제안하는 것 자체가 캐릭터 묘사이기 때문이다.
///
/// 무작위성을 쓰지 않는다. 같은 상태에서는 항상 같은 목록이 나와야
/// 세이브/로드와 롤백에서 제안이 흔들리지 않는다.
/// </summary>
public sealed class ServiceOptionSelector
{
    /// <summary>
    /// 자격 충족 보너스. 나머지 항목을 전부 합쳐도 넘볼 수 없는 크기다.
    /// 덕분에 '할 수 있는 행동'이 '못 하는 행동'보다 항상 먼저 제안된다.
    /// </summary>
    private const int QualifiedBonus = 1000;

    /// <summary>
    /// 성향이 맞는 행동에 붙는 가산. 미달 약 13점어치를 상쇄한다(200 ÷ 15).
    /// 자격선을 넘겨주지는 못하므로, 못 하는 행동들 사이의 우선순위만 바꾼다.
    /// </summary>
    private const int TraitBonus = 200;

    /// <summary>
    /// 요구 대응력에 모자란 1점당 감점. 모자랄수록 선형으로 밀려난다.
    /// 넘치는 쪽은 세지 않는다. 대응력은 자원이 아니라 문턱이다.
    /// 
    /// 올리면 못 하는 행동이 후보에서 빨리 사라져 메이드별 개성이 뚜렷해지고,
    /// 내리면 위험한 제안이 자주 올라와 승인 판단이 매번 무거워진다.
    /// </summary>
    private const int AptitudeGapPenalty = 15;

    /// <summary>
    /// 작가가 적어둔 순서를 지키기 위한 미세 감점. 능력치 차이를 뒤집지 못한다.
    /// 다른 상수가 전부 5의 배수라 이 값(1~4)으로는 동점이 생기지 않는다.
    /// List.Sort 는 불안정 정렬이므로, 동점이 생기면 제안 순서가 흔들릴 수 있다.
    /// 상수를 조정할 때 이 조건이 깨지지 않는지 확인할 것.
    /// </summary>
    private const int DeclarationOrderPenalty = 1;

    private readonly List<Scored> _scratch = new();

    private readonly struct Scored
    {
        public readonly ServiceActionOption Option;
        public readonly int Score;

        public Scored(ServiceActionOption option, int score)
        {
            Option = option;
            Score = score;
        }
    }

    public IReadOnlyList<ServiceActionOption> Select(
        ServiceBeat beat,
        MaidRuntimeState maid,
        List<ServiceActionOption> buffer)
    {
        buffer.Clear();
        _scratch.Clear();

        IReadOnlyList<ServiceActionOption> pool = beat.OptionPool;

        for (int i = 0; i < pool.Count; i++)
            _scratch.Add(new Scored(pool[i], ScoreOption(pool[i], maid, i)));

        _scratch.Sort(static (a, b) => b.Score.CompareTo(a.Score));

        int offerCount = beat.OfferCount < _scratch.Count
            ? beat.OfferCount
            : _scratch.Count;

        for (int i = 0; i < offerCount; i++)
            buffer.Add(_scratch[i].Option);

        _scratch.Clear();

        return buffer;
    }

    /// <summary>승인 UI에서 위험 표시를 붙일지 판단한다.</summary>
    public static bool IsWithinAptitude(ServiceActionOption option, MaidRuntimeState maid)
        => maid.Aptitude[option.RequiredAptitudeAxis] >= option.RequiredAptitude;

    /// <summary>완화 후 부하가 남은 한계 여유를 넘어서는지. 승인 전 경고에 사용한다.</summary>
    public static bool WouldBreachLimit(
        ServiceActionOption option,
        MaidRuntimeState maid,
        ProgressionTuning tuning)
    {
        AxisTriple mitigated = BurdenAccrualRule.Mitigate(option.Load, maid.Aptitude, tuning);

        for (int i = 0; i < BurdenAxes.Count; i++)
        {
            BurdenAxis axis = BurdenAxes.FromIndex(i);

            if (maid.Burden.Get(axis) + mitigated[axis] >= maid.Burden.GetLimit(axis))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 행동 하나의 제안 점수. 높을수록 먼저 제안된다.
    ///
    ///   자격 충족 -> +1000, 미달 -> 모자란 만큼 감점
    ///   성향 일치 -> +200
    ///   선언 순서 -> 뒤일수록 미세 감점
    ///
    /// 부담 누적과 현재 붕괴도는 일부러 보지 않는다.
    /// 메이드는 자기가 무너질 행동을 걸러내지 못하고, 그걸 막는 것이 관리자의 역할이다.
    /// </summary>
    private static int ScoreOption(ServiceActionOption option, MaidRuntimeState maid, int declaredIndex)
    {
        // 동점일 때 위에 적힌 것이 앞서게 한다
        int score = -declaredIndex * DeclarationOrderPenalty;

        int aptitude = maid.Aptitude[option.RequiredAptitudeAxis];
        int gap = option.RequiredAptitude - aptitude;

        // 문턱을 넘었으면 얼마나 넘었는지는 따지지 않는다
        if (gap <= 0)
            score += QualifiedBonus;
        else
            score -= gap * AptitudeGapPenalty;

        // 성향은 '무엇에 끌리는가'를 정한다. 능력을 대신하지는 못한다
        if (maid.Profile.HasTrait(option.PreferredTraitKey))
            score += TraitBonus;

        return score;
    }
}
