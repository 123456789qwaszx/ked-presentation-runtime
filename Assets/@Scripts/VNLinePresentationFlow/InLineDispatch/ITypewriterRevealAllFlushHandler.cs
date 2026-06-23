using System.Threading;
using Yarn.Markup;
using Yarn.Unity;

// 타입라이터가 hurry/fast-forward로 reveal-all(남은 글자 일괄 표시)을 하기 '직전'
// 호출하는 opt-in hook.
//
// 존재 이유:
//  현재 EllipsisBreathTypewriter는 cancellation 감지 시 남은 character 루프를
//  돌지 않고 곧장 RevealAllAndComplete로 빠진다. 따라서 아직 도달하지 못한
//  [advance/] 마커의 OnCharacterWillAppear는 호출되지 않는다.
//  inline advance를 "fast-forward에서도 생략하지 않는다"는 계약으로 만들려면,
//  reveal-all 직전에 남은 advance를 async로 flush해야 한다. void인 OnLineDisplayComplete
//  로는 await가 불가능하므로 별도 인터페이스로 분리한다.
//
// 토큰 계약:
//  hardCancel은 'line visual run 자체가 무효화됐다'는 신호다(hurry가 아니다).
//  flush 루프는 hardCancel에서만 조기 종료하고, 실제 advance 디스패치의 취소는
//  host가 보유한 line run token으로 처리한다.
public interface ITypewriterRevealAllFlushHandler
{
    YarnTask OnTypewriterWillRevealAll(MarkupParseResult line, CancellationToken hardCancel);
}