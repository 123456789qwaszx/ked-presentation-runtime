using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// UI 없이 루프 전체를 굴리기 위한 자동 응답 구현.
///
/// 밸런스 확인과 회귀 테스트용이다. 무작위성을 쓰지 않으므로 같은 콘텐츠에서는 항상 같은 결과가 나온다.
/// 선택 정책은 "한계를 넘기지 않는 선에서 가장 강한 반응을 고른다"로, 이상적인 플레이를 근사한다.
/// </summary>
public sealed class HeadlessGuesthouseScreens : IGuesthouseScreenBindings
{
    private readonly bool _verbose;

    public HeadlessGuesthouseScreens(bool verbose = true)
    {
        _verbose = verbose;
    }

    public async YarnTask PresentReservationBoardAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        if (_verbose)
        {
            for (int i = 0; i < bookings.Count; i++)
                Debug.Log($"[Guesthouse] Day{dayNumber} 예약{i}: {bookings[i].Monster.DisplayName}");
        }

        await YarnTask.Yield();
    }

    public async YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        await YarnTask.Yield();

        BurdenAxis demandAxis = request.Monster.DemandAxis;

        MaidRuntimeState best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < request.Candidates.Count; i++)
        {
            MaidRuntimeState candidate = request.Candidates[i];

            // 요구 축 대응력이 높고, 그 축의 잔여 여유가 큰 메이드를 우선한다.
            int headroom = candidate.Burden.GetLimit(demandAxis) - candidate.Burden.Get(demandAxis);
            int score = candidate.Aptitude[demandAxis] * 10 + headroom;

            if (score <= bestScore)
                continue;

            best = candidate;
            bestScore = score;
        }

        return best?.MaidId;
    }

    public async YarnTask<int> RequestActionApprovalAsync(ServiceApprovalRequest request)
    {
        await YarnTask.Yield();

        int bestIndex = 0;
        int bestScore = int.MinValue;

        for (int i = 0; i < request.Options.Count; i++)
        {
            int score = request.Options[i].Reaction.ToReactionScore() * 10;

            if (request.WouldBreachLimit(i))
                score -= 100;

            if (request.IsBeyondAptitude(i))
                score -= 20;

            if (score <= bestScore)
                continue;

            bestIndex = i;
            bestScore = score;
        }

        return bestIndex;
    }

    public void NotifyControlLost(ServiceSessionState session)
    {
        Debug.LogWarning(
            $"[Guesthouse] 통제 신호 거부. maid={session.Maid.DisplayName}, " +
            $"axis={BurdenAxes.ToBurdenLabel(session.ControlLossAxis)}");
    }

    public async YarnTask PresentSettlementAsync(ServiceSettlementResult result)
    {
        if (_verbose)
            Debug.Log($"[Guesthouse] {result.ToSummaryLine()}");

        await YarnTask.Yield();
    }

    public async YarnTask PresentDayReportAsync(DayCycleState day)
    {
        if (_verbose)
        {
            Debug.Log(
                $"[Guesthouse] Day{day.DayNumber} 종료. 에너지 {day.EnergyEarned}, " +
                $"실패 {day.CountFailedBookings()}, 사고 {day.CountIncidents()}");
        }

        await YarnTask.Yield();
    }

    public async YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request)
    {
        await YarnTask.Yield();

        MaidRuntimeState target = null;
        BurdenAxis targetAxis = BurdenAxis.Physical;
        int highest = -1;

        for (int i = 0; i < request.Maids.Count; i++)
        {
            MaidRuntimeState maid = request.Maids[i];

            if (maid.IsLost)
                continue;

            for (int a = 0; a < BurdenAxes.Count; a++)
            {
                BurdenAxis axis = BurdenAxes.FromIndex(a);
                int value = maid.Burden.Get(axis);

                if (value <= highest)
                    continue;

                target = maid;
                targetAxis = axis;
                highest = value;
            }
        }

        if (target == null)
            return NightPlan.None;

        NightProgramKind kind = request.CanRunManagedRelease(target, targetAxis)
            ? NightProgramKind.ManagedRelease
            : NightProgramKind.Care;

        return new NightPlan(kind, target.MaidId, targetAxis);
    }
}
