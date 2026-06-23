using Yarn.Unity;

// inline [advance/]의 실제 sub lane 디스패치를 담당하는 host.
//
// 설계 원칙:
//  - 마크업 핸들러는 sub runner / hub / lane을 절대 직접 만지지 않는다.
//    핸들러는 manifest 위치 감지와 ordinal 순서만 안다. 디스패치는 host가 한다.
//  - 취소 토큰은 핸들러의 ct(hurry/next-line)가 아니라, host가 보유한 현재 라인의
//    hard-cancel run token이다. 그래야 hurry가 inline advance를 중도 취소하지 않고
//    settle까지 완주시켜 회계 leak을 막는다. rollback/load만 run을 무효화해 취소한다.
public interface IInlinePresentationAdvanceHost
{
    // 현재 라인의 ordinal번째 inline advance를 sub lane으로 정확히 1칸 디스패치하고,
    // 그 settle(또는 main_free 즉시 해제)까지 대기한다.
    // 현재 라인 run이 유효하지 않으면 아무것도 하지 않고 즉시 반환한다.
    YarnTask AdvanceOneAsync(int ordinal);
}