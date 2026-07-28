using System;

/// <summary>방치된 메이드에게 일어나는 일의 종류. </summary>
public enum NeglectCollapseOutcome
{
    /// <summary>0~79: 자연 회복 −10.</summary>
    NaturalRecovery = 0,

    /// <summary>80~99: 유지 (60%).</summary>
    DangerHold = 1,

    /// <summary>80~99: 자기해소 (25%) - 붕괴 60으로, 사고성 기벽 판정 1회.</summary>
    SelfRelease = 2,

    /// <summary>80~99: 심야 사건 (15%) - 자동 심층 2비트, 개입 불가.</summary>
    NightIncident = 3,
}

/// <summary>
/// 방치 판정 1인분의 결과.
/// 수치 반영(붕괴 변경, 후유증 일수, 기벽 부여, 이벤트 예약)은 전부 호출부 책임 -
/// 이 구조체는 "무엇이 일어나는가"만 확정.
/// </summary>
public readonly struct NeglectJudgment
{
    public NeglectCollapseOutcome Outcome { get; }

    /// <summary>판정에 쓰인 붕괴값 (최고치 축).</summary>
    public int CollapseBefore { get; }

    /// <summary>이 판정이 지시하는 붕괴 도착값. NightIncident 는 심층 결과가 정하므로 변경 없음(-1).</summary>
    public int CollapseAfter { get; }

    /// <summary>SelfRelease 에서 사고성 기벽 획득 판정(50%)에 성공했는지.</summary>
    public bool GainsAccidentQuirk { get; }

    /// <summary>특수 기벽의 "먼저 요구하는 이벤트" 예약 여부 (보유 시 25%).</summary>
    public bool SchedulesQuirkRequest { get; }

    /// <summary>NightIncident 시 자동 심층 비트 수. 그 외 0.</summary>
    public int IncidentDepthBeats { get; }

    public NeglectJudgment(
        NeglectCollapseOutcome outcome,
        int collapseBefore,
        int collapseAfter,
        bool gainsAccidentQuirk,
        bool schedulesQuirkRequest,
        int incidentDepthBeats)
    {
        Outcome = outcome;
        CollapseBefore = collapseBefore;
        CollapseAfter = collapseAfter;
        GainsAccidentQuirk = gainsAccidentQuirk;
        SchedulesQuirkRequest = schedulesQuirkRequest;
        IncidentDepthBeats = incidentDepthBeats;
    }
}

/// <summary>
/// 방치(선택받지 않은 메이드) 자동 판정.
///
/// 판정 순서와 굴림 소비 순서를 고정한다 - 커밋 재현성의 전제:
///   1) 붕괴 구간 판정 (80~99 이면 1d100 소비)
///   2) 자기해소 시 기벽 판정 (1d100 소비)
///   3) 특수 기벽 보유 시 요구 이벤트 판정 (1d100 소비)
/// 조건이 거짓이면 해당 굴림은 소비하지 않는다.
///
/// 확률 재정의(qk_acc_nightowl 유지 60->50 등)는 호출부가 NeglectChances 로 넘긴다.
/// </summary>
public static class NeglectRule
{
    /// <summary>80~99 방치 확률 세트. 기벽이 이 값을 변형한다.</summary>
    public readonly struct NeglectChances
    {
        public int HoldPercent { get; }
        public int SelfReleasePercent { get; }

        public NeglectChances(int holdPercent, int selfReleasePercent)
        {
            HoldPercent = Math.Clamp(holdPercent, 0, 100);
            SelfReleasePercent = Math.Clamp(selfReleasePercent, 0, 100 - HoldPercent);
        }

        public static NeglectChances From(DungeonCafeTuning tuning)
            => new(tuning.NeglectHoldPercent, tuning.NeglectSelfReleasePercent);
    }

    public static NeglectJudgment Judge(
        IDiceSource dice,
        int highestAxisCollapse,
        bool hasSpecialQuirk,
        in NeglectChances chances,
        DungeonCafeTuning tuning)
    {
        NeglectCollapseOutcome outcome;
        int after;
        bool gainsQuirk = false;
        int incidentBeats = 0;

        bool inDangerBand =
            highestAxisCollapse >= tuning.ManagedReleaseMinimumCollapse
            && highestAxisCollapse < tuning.ControlLossThreshold;

        if (!inDangerBand)
        {
            outcome = NeglectCollapseOutcome.NaturalRecovery;
            after = Math.Max(0, highestAxisCollapse - tuning.NeglectNaturalRecovery);
        }
        else
        {
            int roll = dice.NextInclusive(1, 100);

            if (roll <= chances.HoldPercent)
            {
                outcome = NeglectCollapseOutcome.DangerHold;
                after = highestAxisCollapse;
            }
            else if (roll <= chances.HoldPercent + chances.SelfReleasePercent)
            {
                outcome = NeglectCollapseOutcome.SelfRelease;
                after = Math.Min(highestAxisCollapse, tuning.NeglectSelfReleaseTargetCollapse);
                gainsQuirk = dice.RollPercent(tuning.NeglectSelfReleaseQuirkChancePercent);
            }
            else
            {
                outcome = NeglectCollapseOutcome.NightIncident;
                after = -1; // 심층 결과가 결정한다.
                incidentBeats = tuning.NightIncidentDepthBeats;
            }
        }

        bool schedulesRequest =
            hasSpecialQuirk && dice.RollPercent(tuning.NeglectQuirkRequestChancePercent);

        return new NeglectJudgment(
            outcome,
            collapseBefore: highestAxisCollapse,
            collapseAfter: after,
            gainsAccidentQuirk: gainsQuirk,
            schedulesQuirkRequest: schedulesRequest,
            incidentDepthBeats: incidentBeats);
    }
}