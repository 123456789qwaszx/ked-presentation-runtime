// // ============================================================
// // EpisodeCondition.cs
// // 경로: Assets/_Project/Scripts/VN/Episode/Progression/
// //
// // 노드 표시 조건 / 해금 조건 / 선택지 조건 / Attachment 조건 /
// // 챕터 엔딩 조건에서 공통으로 사용하는 조건 단위.
// // ============================================================
//
// using System;
//
// [Serializable]
// public sealed class EpisodeCondition
// {
//     /// <summary>
//     /// 조건 종류. Flag(bool), Stat(int), EpisodeCleared, ChapterCleared, Token
//     /// </summary>
//     public EpisodeConditionKind Kind;
//
//     /// <summary>
//     /// 플래그 키, 스탯 키, 에피소드 ID, 챕터 ID, 토큰 ID 등.
//     /// Kind에 따라 의미가 달라진다.
//     /// </summary>
//     public string Key;
//
//     /// <summary>
//     /// 비교 연산자. Exists/NotExists는 Key만 확인하고 값은 무시한다.
//     /// </summary>
//     public EpisodeCompareOp Op;
//
//     /// <summary>Op가 수치 비교일 때 사용.</summary>
//     public int IntValue;
//
//     /// <summary>Kind == Flag이고 Op == Equal/NotEqual일 때 사용.</summary>
//     public bool BoolValue;
//
//     /// <summary>Kind == Token처럼 문자열 일치가 필요한 경우 사용.</summary>
//     public string StringValue;
// }
//
// public enum EpisodeConditionKind
// {
//     /// <summary>bool 플래그. BoolValue와 비교한다.</summary>
//     Flag,
//
//     /// <summary>int 스탯. IntValue와 비교한다.</summary>
//     Stat,
//
//     /// <summary>특정 에피소드가 클리어되었는지. Key = EpisodeId.</summary>
//     EpisodeCleared,
//
//     /// <summary>특정 챕터가 클리어되었는지. Key = ChapterId.</summary>
//     ChapterCleared,
//
//     /// <summary>특정 토큰(아이템/키)을 보유 중인지. Key = TokenId.</summary>
//     Token
// }
//
// public enum EpisodeCompareOp
// {
//     /// <summary>Key가 존재하면 참.</summary>
//     Exists,
//
//     /// <summary>Key가 존재하지 않으면 참.</summary>
//     NotExists,
//
//     Equal,
//     NotEqual,
//
//     /// <summary>Stat 전용. 실제값 >= IntValue.</summary>
//     GreaterOrEqual,
//
//     /// <summary>Stat 전용. 실제값 <= IntValue.</summary>
//     LessOrEqual
// }
