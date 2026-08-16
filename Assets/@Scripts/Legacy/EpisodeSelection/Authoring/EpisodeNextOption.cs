// // ============================================================
// // EpisodeNextOption.cs
// // 경로: Assets/_Project/Scripts/VN/Episode/Progression/
// //
// // 에피소드 클리어 후 제시될 수 있는 다음 선택지 후보.
// // 하나의 EpisodeNodeDefinition은 복수의 NextOption을 가질 수 있다.
// //
// // 예시:
// //   main05.02 이후:
// //     - main05.03 (조건 없음)
// //     - branch05.02A (HarinTrust >= 3)
// //     - hidden05.02X (flag.saw_forbidden_stream == true)
// // ============================================================
//
// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class EpisodeNextOption
// {
//     /// <summary>이 선택지가 이동할 대상 EpisodeId. 동일 SO 내에 반드시 존재해야 한다.</summary>
//     public string TargetEpisodeId;
//
//     /// <summary>
//     /// UI에 표시할 선택지 텍스트.
//     /// 비어 있으면 일반 진행(분기 없는 다음 에피소드)으로 간주한다.
//     /// </summary>
//     public string ChoiceLabel;
//
//     /// <summary>
//     /// 이 선택지가 표시/활성화되기 위한 조건 목록.
//     /// 모든 조건이 충족되어야 활성화된다 (AND 논리).
//     /// </summary>
//     public List<EpisodeCondition> Conditions = new List<EpisodeCondition>();
//
//     /// <summary>조건 불충족 시 선택지 자체를 숨길지 여부. false면 비활성화 상태로 표시한다.</summary>
//     public bool HideWhenLocked;
//
//     /// <summary>잠금 상태일 때 표시할 안내 텍스트. HideWhenLocked == false일 때 유효하다.</summary>
//     public string LockedReasonText;
// }
