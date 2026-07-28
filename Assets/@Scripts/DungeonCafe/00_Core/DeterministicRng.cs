using System;

/// <summary>
/// 판정용 난수의 공급 접점.
///
/// 규칙 클래스는 System.Random 을 직접 잡지 않고 반드시 이 인터페이스를 통해 굴린다.
/// 테스트에서는 FixedDiceSource 로 원하는 눈을 강제 주입할 수 있고,
/// 런타임에서는 DeterministicRng 가 시드/상태 기반으로 재현 가능한 값을 공급한다.
/// </summary>
public interface IDiceSource
{
    /// <summary>min..max 양끝 포함 정수.</summary>
    int NextInclusive(int minInclusive, int maxInclusive);
}

/// <summary>
/// splitmix64 기반 결정론 난수원.
///
/// v3 §0.1 판정 불가역 원칙의 구현 기반이다.
///   - 같은 시드는 언제나 같은 수열을 낸다 (플랫폼/버전 무관 - System.Random 은 이 보장이 없다).
///   - State 를 세이브에 그대로 실으면, 로드 후에도 "다음 굴림"이 동일하다.
///     판정을 되감기로 재추첨하는 경로가 구조적으로 막힌다.
///
/// 판정 커밋 로그(4단계)는 굴림 직전의 State 를 함께 기록한다.
/// </summary>
public sealed class DeterministicRng : IDiceSource
{
    private ulong _state;

    public DeterministicRng(ulong seed)
    {
        // seed 0 도 유효하다. splitmix64 는 고정점이 없다.
        _state = seed;
    }

    /// <summary>세이브에 싣는 현재 상태. 복원은 RestoreState 로 한다.</summary>
    public ulong State => _state;

    public void RestoreState(ulong state) => _state = state;

    private ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;

        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

        return z ^ (z >> 31);
    }

    public int NextInclusive(int minInclusive, int maxInclusive)
    {
        if (maxInclusive <= minInclusive)
            return minInclusive;

        ulong span = (ulong)((long)maxInclusive - minInclusive + 1);

        // 판정 주사위의 span 은 최대 99 수준이라 모듈로 편향은 1/2^57 이하 - 무시한다.
        return minInclusive + (int)(NextUInt64() % span);
    }
}

/// <summary>확률 판정 등 IDiceSource 공통 확장.</summary>
public static class DiceSourceExtensions
{
    /// <summary>심층 주사위 1~99.</summary>
    public static int RollDie99(this IDiceSource dice) => dice.NextInclusive(1, 99);

    /// <summary>1~100 을 굴려 chancePercent 이하이면 성공.</summary>
    public static bool RollPercent(this IDiceSource dice, int chancePercent)
    {
        if (chancePercent <= 0)
            return false;

        if (chancePercent >= 100)
            return true;

        return dice.NextInclusive(1, 100) <= chancePercent;
    }
}

/// <summary>
/// 테스트/연출 리허설용. 지정한 눈을 순서대로 반환하고, 소진되면 마지막 값을 반복한다.
/// </summary>
public sealed class FixedDiceSource : IDiceSource
{
    private readonly int[] _values;
    private int _cursor;

    public FixedDiceSource(params int[] values)
    {
        _values = values == null || values.Length == 0 ? new[] { 1 } : values;
    }

    public int NextInclusive(int minInclusive, int maxInclusive)
    {
        int value = _values[Math.Min(_cursor, _values.Length - 1)];
        _cursor++;

        return Math.Clamp(value, minInclusive, maxInclusive);
    }
}