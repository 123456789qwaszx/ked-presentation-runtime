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
    private const int QualifiedBonus = 1000;
    private const int TraitBonus = 200;
    private const int AptitudeGapPenalty = 15;
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

    private static int ScoreOption(ServiceActionOption option, MaidRuntimeState maid, int declaredIndex)
    {
        int score = -declaredIndex * DeclarationOrderPenalty;

        int aptitude = maid.Aptitude[option.RequiredAptitudeAxis];
        int gap = option.RequiredAptitude - aptitude;

        if (gap <= 0)
            score += QualifiedBonus;
        else
            score -= gap * AptitudeGapPenalty;

        if (maid.Profile.HasTrait(option.PreferredTraitKey))
            score += TraitBonus;

        return score;
    }
}
