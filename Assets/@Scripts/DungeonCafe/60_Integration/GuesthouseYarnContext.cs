using Yarn.Unity;

/// <summary>
/// 노드를 재생하기 직전에 Yarn 변수 저장소로 현재 상황을 밀어 넣는다.
///
/// 접객 노드는 배정된 메이드와 무관하게 공유되므로, 대본이 배정 결과를 알려면
/// 변수를 통하는 수밖에 없다. 노드를 메이드별로 복제하지 않기 위한 장치다.
///
/// 여기서 넣는 값은 전부 '표시용'이다.
/// 판정과 분기는 모두 C# 규칙 레이어가 담당하고, Yarn 쪽에서 이 값을 고쳐도 게임 상태는 바뀌지 않는다.
/// </summary>
public sealed class GuesthouseYarnContext
{
    private readonly VariableStorageBehaviour _storage;

    public GuesthouseYarnContext(VariableStorageBehaviour storage)
    {
        _storage = storage;
    }

    /// <summary>접객 세션 진입 시점에 한 번 갱신한다.</summary>
    public void PushSession(ServiceSessionState session)
    {
        if (_storage == null || session == null)
            return;

        MaidRuntimeState maid = session.Maid;
        MonsterProfile monster = session.Encounter.Monster;

        SetString("$maid_id", maid.Profile.MaidId);
        SetString("$maid_name", maid.Profile.DisplayName);
        SetString("$maid_style", maid.Profile.ProposalStyleKey);

        SetString("$monster_id", monster.MonsterId);
        SetString("$monster_name", monster.DisplayName);
        SetString("$monster_species", monster.Species.ToString());

        PushVolatile(session);
    }

    /// <summary>비트마다 바뀌는 값. 상황 노드와 승인 노드 직전에 갱신한다.</summary>
    public void PushVolatile(ServiceSessionState session)
    {
        if (_storage == null || session == null)
            return;

        MaidRuntimeState maid = session.Maid;
        BurdenAxis demand = session.Encounter.Monster.DemandAxis;

        SetNumber("$collapse", maid.Burden.Get(demand));
        SetNumber("$collapse_percent", maid.Burden.GetPercentOfLimit(demand));
        SetNumber("$satisfaction", session.Encounter.Satisfaction);
        SetNumber("$beat_index", session.ConsumedBeatCount);

        SetString("$control", session.ControlStatus.ToString());
        SetBool("$control_lost", session.ControlStatus == ControlAuthorityStatus.Lost);
        SetBool("$strained", session.ControlStatus == ControlAuthorityStatus.Strained);
    }

    /// <summary>밤 구간에서 대상 메이드를 알린다.</summary>
    public void PushMaid(MaidRuntimeState maid, BurdenAxis axis)
    {
        if (_storage == null || maid == null)
            return;

        SetString("$maid_id", maid.Profile.MaidId);
        SetString("$maid_name", maid.Profile.DisplayName);
        SetString("$axis", axis.ToString());
        SetString("$axis_label", BurdenAxes.ToBurdenLabel(axis));
        SetNumber("$collapse", maid.Burden.Get(axis));
        SetNumber("$collapse_percent", maid.Burden.GetPercentOfLimit(axis));
    }

    public void PushDay(int dayNumber, int energyEarned, int energyQuota)
    {
        if (_storage == null)
            return;

        SetNumber("$day", dayNumber);
        SetNumber("$energy", energyEarned);
        SetNumber("$energy_quota", energyQuota);
    }

    // ------------------------------------------------------------
    // 저장소 접근
    // ------------------------------------------------------------
    private void SetString(string name, string value)
    {
        _storage.SetValue(name, value ?? string.Empty);
    }

    private void SetNumber(string name, int value)
    {
        _storage.SetValue(name, (float)value);
    }

    private void SetBool(string name, bool value)
    {
        _storage.SetValue(name, value);
    }
}
