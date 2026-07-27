using System;

/// <summary>3장부 스냅숏. HUD 와 세이브 DTO 가 이 형태로 읽는다.</summary>
public readonly struct DesireLedgerSnapshot
{
    public int Today { get; }
    public int Held { get; }
    public int Lifetime { get; }

    public DesireLedgerSnapshot(int today, int held, int lifetime)
    {
        Today = today;
        Held = held;
        Lifetime = lifetime;
    }

    public override string ToString() => $"오늘 {Today} / 보유 {Held} / 누적 {Lifetime}";
}

/// <summary>
/// 욕구의 3장부. (v3 §7.1)
///
///   Today    - 오늘의 영업 목표(할당) 판정. 소비로 내려가지 않는다. (매출)
///   Held     - 능력/시설에 실제 소비. (현금)
///   Lifetime - 가게 레벨과 장기 진행도. 소비로 내려가지 않는다. (명성)
///
/// 계약: Earn 만이 세 장부를 동시에 올린다. EarnNight 는 Today 를 제외한다.
/// Spend 는 Held 만 내린다. 개별 장부를 직접 수정하는 경로는 존재하지 않는다.
/// 하나의 매출을 서로 다른 장부로 기록하는 것이지, 복제가 아니다.
/// </summary>
public sealed class DesireLedger
{
    public int Today { get; private set; }
    public int Held { get; private set; }
    public int Lifetime { get; private set; }

    public DesireLedger()
    {
    }

    /// <summary>세이브 복원용.</summary>
    public DesireLedger(int today, int held, int lifetime)
    {
        Today = Math.Max(0, today);
        Held = Math.Max(0, held);
        Lifetime = Math.Max(0, lifetime);
    }

    /// <summary>낮 결산 수입. 세 장부 동시 기입.</summary>
    public void Earn(int amount)
    {
        if (amount <= 0)
            return;

        Today += amount;
        Held += amount;
        Lifetime += amount;
    }

    /// <summary>
    /// 밤 수입 (관리 붕괴 +40 등). 오늘 장부 제외 - 당일 할당은 이미 판정 종료이며,
    /// 밤 수입이 할당을 소급 구제하면 낮의 긴장이 죽는다. (v3 §6.1)
    /// </summary>
    public void EarnNight(int amount)
    {
        if (amount <= 0)
            return;

        Held += amount;
        Lifetime += amount;
    }

    /// <summary>능력 구매 등 소비. 보유가 부족하면 아무것도 하지 않고 false.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;

        if (Held < amount)
            return false;

        Held -= amount;
        return true;
    }

    /// <summary>세이브 복원 전용. 게임 규칙 코드에서 호출 금지.</summary>
    public void RestoreFrom(int today, int held, int lifetime)
    {
        Today = System.Math.Max(0, today);
        Held = System.Math.Max(0, held);
        Lifetime = System.Math.Max(0, lifetime);
    }

    /// <summary>할당 판정 직후, 다음 날 개시 시점에 호출한다.</summary>
    public void StartNewDay()
    {
        Today = 0;
    }

    /// <summary>오늘 장부가 할당을 채웠는지.</summary>
    public bool MeetsQuota(int quota) => Today >= quota;

    public DesireLedgerSnapshot Snapshot() => new(Today, Held, Lifetime);

    public override string ToString() => Snapshot().ToString();
}
