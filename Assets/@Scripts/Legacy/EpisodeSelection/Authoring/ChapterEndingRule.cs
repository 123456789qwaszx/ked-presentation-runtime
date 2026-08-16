// // ============================================================
// // ChapterEndingRule.cs
// // 경로: Assets/_Project/Scripts/VN/Episode/Progression/
// //
// // 챕터 엔딩 조건 + 다음 챕터 개방 조건.
// // ChapterEpisodeProgressionSO.EndingRules 목록에 담긴다.
// // ============================================================
//
// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class ChapterEndingRule
// {
//     /// <summary>
//     /// 챕터 엔딩 고유 키. SO 내에서 중복 불가.
//     /// 예: "ch05_normal_end", "ch05_bad_end", "ch05_true_end"
//     /// </summary>
//     public string EndingKey;
//
//     /// <summary>에디터 / UI에 표시될 엔딩 이름.</summary>
//     public string DisplayName;
//
//     /// <summary>이 엔딩이 트리거되기 위한 조건 목록 (AND).</summary>
//     public List<EpisodeCondition> Conditions = new List<EpisodeCondition>();
//
//     /// <summary>
//     /// 이 엔딩 달성 시 다음 챕터를 해금할지 여부.
//     /// true이면 NextChapterId가 반드시 입력되어야 한다.
//     /// </summary>
//     public bool UnlockNextChapter;
//
//     /// <summary>
//     /// 해금할 다음 챕터 ID.
//     /// UnlockNextChapter == true일 때 Validator가 비어있음을 Error로 처리한다.
//     /// </summary>
//     public string NextChapterId;
//
//     /// <summary>기획자용 메모. 빌드에 영향 없음.</summary>
//     public string DesignerNote;
// }
