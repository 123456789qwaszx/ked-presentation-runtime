// // 컷인 등 "포인터" 시퀀스는 사용자의 advance 입력을 직접 받지 않는다.
// // GateTokenType.Input을 잘못 써도 조용히 다음으로 넘어가지 않고 영원히 멈춰 있는 것이
// // (메인 입력을 가로채는 것보다) 옳은 동작이다.
// public sealed class NullInputSource : IInputSource
// {
//     public bool ConsumeAdvancePressed() => false;
// }