using System;
using System.Collections.Generic;

/// <summary>
/// v3 세이브 DTO. [Serializable] 공개 필드만 - JsonUtility/자체 직렬화 어느 쪽에도 물린다.
/// Capture 는 저장 가능 국면(§14)에서만 성공한다.
/// </summary>
[Serializable]
public sealed class GuesthouseV3SaveModel
{
    [Serializable]
    public sealed class MaidSave
    {
        public string maidId;
        public int[] gauge = new int[3];
        public int[] peaks = new int[3];
        public int[] masteryLevel = new int[3];
        public int[] masteryXp = new int[3];
        public bool hasRescueTicket;
        public int totalCollapseCount;
        public bool isLost;
        public int relationPoints;
        public int trustCount;
        public int dependCount;
        public bool depthScar;
        public List<string> quirkIds = new();
        public List<AftereffectSave> aftereffects = new();
    }

    [Serializable]
    public sealed class AftereffectSave
    {
        public string id;
        public int cares;
        public int neglectDays;
        public int daysHeld;
        public int blockDaysLeft;
    }

    [Serializable]
    public sealed class KeyValue { public string key; public int value; }

    public int currentDay;
    public ulong rngState;
    public int commitBarrierVersion;
    public int bankruptcyCount;
    public int ledgerToday;
    public int ledgerHeld;
    public int ledgerLifetime;
    public string phase;
    public List<MaidSave> maids = new();
    public List<KeyValue> understanding = new();
    public List<string> phoneCalled = new();
    public List<string> depthWitnessed = new();
    public List<string> oneTimeFlags = new();
    public List<string> abilitiesOwned = new();
    public List<string> abilitiesEquipped = new();
    public List<KeyValue> abilityCampaignUses = new();
    public List<string> ruinedRoutes = new();
    public int[] typeServiceCounts = new int[3];
    public int[] typeWitnessCounts = new int[3];

    public static bool TryCapture(CampaignStateV3 campaign, out GuesthouseV3SaveModel save)
    {
        save = null;
        if (!campaign.CanSaveNow) return false;   // 접객/심층 중 저장 불가. (§14)

        save = new GuesthouseV3SaveModel
        {
            currentDay = campaign.CurrentDayNumber,
            rngState = campaign.Rng.State,
            commitBarrierVersion = campaign.CommitLog.RollbackBarrierVersion,
            bankruptcyCount = campaign.BankruptcyCount,
            ledgerToday = campaign.Ledger.Today,
            ledgerHeld = campaign.Ledger.Held,
            ledgerLifetime = campaign.Ledger.Lifetime,
            phase = campaign.Phase.ToString(),
        };

        for (int i = 0; i < campaign.Maids.Count; i++)
        {
            MaidStateV3 m = campaign.Maids[i];
            var ms = new MaidSave
            {
                maidId = m.MaidId,
                hasRescueTicket = m.HasRescueTicket,
                totalCollapseCount = m.TotalCollapseCount,
                isLost = m.IsLost,
                relationPoints = m.RelationPoints,
                trustCount = m.TrustCount,
                dependCount = m.DependCount,
                depthScar = m.HasDepthScar,
            };
            for (int a = 0; a < BurdenAxes.Count; a++)
            {
                BurdenAxis axis = BurdenAxes.FromIndex(a);
                ms.gauge[a] = m.Gauge.Get(axis);
                ms.peaks[a] = m.Gauge.GetPeak(axis);
                ms.masteryLevel[a] = m.GetMastery(axis).Level;
                ms.masteryXp[a] = m.GetMastery(axis).Experience;
            }
            ms.quirkIds.AddRange(m.QuirkIds);
            for (int a = 0; a < m.Aftereffects.Count; a++)
            {
                AftereffectInstance inst = m.Aftereffects[a];
                ms.aftereffects.Add(new AftereffectSave
                {
                    id = inst.Definition.Id,
                    cares = inst.CaresApplied,
                    neglectDays = inst.NeglectDaysPassed,
                    daysHeld = inst.DaysHeld,
                    blockDaysLeft = inst.BlockDaysLeft == int.MaxValue ? -1 : inst.BlockDaysLeft,
                });
            }
            save.maids.Add(ms);
        }

        foreach (KeyValuePair<string, int> kv in campaign.Understanding.AllPoints)
            save.understanding.Add(new KeyValue { key = kv.Key, value = kv.Value });
        foreach (string s in campaign.Understanding.PhoneCalled) save.phoneCalled.Add(s);
        foreach (string s in campaign.Understanding.DepthWitnessed) save.depthWitnessed.Add(s);
        foreach (string s in campaign.Understanding.OneTimeFlags) save.oneTimeFlags.Add(s);
        foreach (string s in campaign.Abilities.Owned) save.abilitiesOwned.Add(s);
        foreach (string s in campaign.Abilities.Equipped) save.abilitiesEquipped.Add(s);
        foreach (KeyValuePair<string, int> kv in campaign.Abilities.CampaignUses)
            save.abilityCampaignUses.Add(new KeyValue { key = kv.Key, value = kv.Value });
        save.ruinedRoutes.AddRange(campaign.RuinedRouteMaidIds);

        (int[] svc, int[] wit) = campaign.SnapshotCounters();
        save.typeServiceCounts = svc;
        save.typeWitnessCounts = wit;
        return true;
    }

    public CampaignStateV3 Restore(GuesthouseV3ContentDB content, GuesthouseTuningV3 tuning)
    {
        var campaign = new CampaignStateV3(content, tuning, 0UL);
        campaign.Rng.RestoreState(rngState);
        campaign.CommitLog.RestoreBarrier(commitBarrierVersion);
        campaign.CurrentDayNumber = currentDay;
        campaign.BankruptcyCount = bankruptcyCount;

        var ledger = new DesireLedger(ledgerToday, ledgerHeld, ledgerLifetime);
        // DesireLedger 는 캠페인 생성 시 고정 - 값 이식.
        campaign.Ledger.Earn(0);
        RestoreLedger(campaign.Ledger, ledger);

        if (Enum.TryParse(phase, out CampaignPhaseV3 parsed)) campaign.Phase = parsed;

        for (int i = 0; i < maids.Count; i++)
        {
            MaidSave ms = maids[i];
            MaidStateV3 m = campaign.GetMaid(ms.maidId);
            if (m == null) continue;

            m.RestoreCore(ms.hasRescueTicket, ms.totalCollapseCount, ms.isLost,
                ms.relationPoints, ms.trustCount, ms.dependCount, ms.depthScar);
            m.RestoreQuirks(ms.quirkIds);

            for (int a = 0; a < BurdenAxes.Count; a++)
            {
                BurdenAxis axis = BurdenAxes.FromIndex(a);
                m.Gauge.SetValue(axis, ms.gauge[a]);
                m.GetMastery(axis).Restore(ms.masteryLevel[a], ms.masteryXp[a]);
            }

            for (int a = 0; a < ms.aftereffects.Count; a++)
            {
                AftereffectSave es = ms.aftereffects[a];
                AftereffectDefinition def = content.GetAftereffect(es.id);
                if (def == null) continue;
                m.AddAftereffect(def);
                m.FindAftereffect(es.id)?.Restore(es.cares, es.neglectDays, es.daysHeld,
                    es.blockDaysLeft < 0 ? int.MaxValue : es.blockDaysLeft);
            }
        }

        for (int i = 0; i < understanding.Count; i++)
            campaign.Understanding.RestorePoint(understanding[i].key, understanding[i].value);
        for (int i = 0; i < phoneCalled.Count; i++) campaign.Understanding.RestorePhone(phoneCalled[i]);
        for (int i = 0; i < depthWitnessed.Count; i++) campaign.Understanding.RestoreWitness(depthWitnessed[i]);
        for (int i = 0; i < oneTimeFlags.Count; i++) campaign.Understanding.RestoreFlag(oneTimeFlags[i]);
        for (int i = 0; i < abilitiesOwned.Count; i++) campaign.Abilities.Grant(abilitiesOwned[i]);
        for (int i = 0; i < abilitiesEquipped.Count; i++)
            campaign.Abilities.Equip(abilitiesEquipped[i], tuning.GetAbilitySlots(ShopLevelRule.Resolve(ledgerLifetime, tuning)));
        for (int i = 0; i < abilityCampaignUses.Count; i++)
            campaign.Abilities.RestoreCampaignUse(abilityCampaignUses[i].key, abilityCampaignUses[i].value);
        campaign.RuinedRouteMaidIds.AddRange(ruinedRoutes);
        campaign.RestoreCounters(typeServiceCounts, typeWitnessCounts);
        return campaign;
    }

    private static void RestoreLedger(DesireLedger target, DesireLedger source)
    {
        // DesireLedger 는 개별 setter 를 두지 않으므로(계약 §7.1) 리플렉션 대신 재구성이 정석이지만,
        // CampaignStateV3.Ledger 가 readonly 라 값 이식 헬퍼를 여기 둔다.
        target.RestoreFrom(source.Today, source.Held, source.Lifetime);
    }
}
