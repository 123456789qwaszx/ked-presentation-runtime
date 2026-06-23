using System.Threading;
using Yarn.Unity;

// ─────────────────────────────────────────────────────────────────────────────
// InlineAdvanceDispatchHost
//
// inline [advance/]의 실제 sub lane 디스패치를 담당하는 per-line, run-scoped host.
//
// 존재 이유(책임 분리):
//  VNLinePresentationFlow는 "여러 라인에 걸친 라인 트랜잭션 시퀀서"(장수명)인 반면,
//  inline advance host는 "현재 *이* 라인 run에 묶인 단발 디스패치 프리미티브"다.
//  유효 창은 정확히 타입라이터 phase(Begin/End)뿐이다. 두 granularity를 한 클래스에
//  동거시키면 장수명 객체에 per-line mutable 상태가 swap-in/swap-out으로 얹힌다.
//  그래서 per-line 상태를 "그게 전부 일인" 이 클래스로 격리한다.
//
// 와이어링 모델:
//  - 핸들러(InlineAdvanceMarkupHandler)는 Initialize 시점에 이 host를 안정 facade로
//    한 번 주입받는다(참조는 영속). 라인마다 바뀌는 건 host 내부의 현재 run 타겟뿐이다.
//  - flow가 이 host를 소유하고, 타입라이터 phase 진입 시 Begin(run),
//    종료(finally) 시 End()로 구동한다.
//
// 취소 토큰 계약(IInlinePresentationAdvanceHost):
//  inline advance 디스패치의 cancel은 핸들러의 ct(hurry/next-line)가 아니라
//  현재 라인의 hard-cancel run token(LinePresentationRun.VisualToken)이다.
//  이 토큰은 rollback/load(AbortCurrentVnLine)와 presenter lifetime 종료에서만
//  fire한다. 그래야:
//   - hurry는 inline advance를 중도 취소하지 않고 settle까지 완주시켜
//     ForwardSettleClock 회계 leak(대기자 없는 Epoch 증가)을 막고,
//   - next-line 클릭은 inline [advance] 스톨을 깨지 못하며,
//   - rollback/load만 in-flight 핸드셰이크를 무효화한다(이 경로는 lane 회계를
//     ClearInFlightSettles로 리셋하므로 대칭이 유지된다).
//  cancel을 run에서 직접 파생시켜, run과 cancel이 어긋날 여지를 제거한다.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class InlineAdvanceDispatchHost : IInlinePresentationAdvanceHost
{
    private readonly VNSideRunnerSyncHub _sideRunnerSyncHub;

    private LinePresentationRun _run;
    private bool _runValid;

    public InlineAdvanceDispatchHost(VNSideRunnerSyncHub sideRunnerSyncHub)
    {
        _sideRunnerSyncHub = sideRunnerSyncHub;
    }

    // 타입라이터 phase 진입 시 현재 라인 run으로 디스패치 창을 연다.
    public void Begin(LinePresentationRun run)
    {
        _run = run;
        _runValid = true;
    }

    // 타입라이터 phase 종료(finally) 시 디스패치 창을 닫는다.
    public void End()
    {
        _runValid = false;
        _run = default;
    }

    public async YarnTask AdvanceOneAsync(int ordinal)
    {
        if (!_runValid)
            return;

        LinePresentationRun run = _run;

        if (run == null)
            return;

        if (!run.IsValid)
            return;

        CancellationToken cancel = run.VisualToken;

        if (cancel.IsCancellationRequested)
            return;

        await _sideRunnerSyncHub.RunInlineScriptedAdvanceAsync(1, cancel);
    }
}