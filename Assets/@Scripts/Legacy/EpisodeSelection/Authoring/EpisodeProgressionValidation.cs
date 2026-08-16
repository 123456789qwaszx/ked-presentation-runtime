// // ============================================================
// // EpisodeProgressionValidation.cs
// // 경로: Assets/_Project/Scripts/VN/Episode/Progression/
// //
// // Validator가 반환하는 Issue 단위와 결과 컨테이너.
// // Runtime 폴더에 두어 Editor / Runtime 양쪽에서 참조 가능하게 한다.
// // ============================================================
//
// using System.Collections.Generic;
//
// // ─────────────────────────────────────────────────────────────
// // Issue 심각도
// // ─────────────────────────────────────────────────────────────
//
// public enum EpisodeProgressionIssueSeverity
// {
//     Info,
//     Warning,
//     Error
// }
//
// // ─────────────────────────────────────────────────────────────
// // 단일 Issue (값 타입)
// // ─────────────────────────────────────────────────────────────
//
// public readonly struct EpisodeProgressionValidationIssue
// {
//     public readonly EpisodeProgressionIssueSeverity Severity;
//
//     /// <summary>사람이 읽을 수 있는 설명 메시지.</summary>
//     public readonly string Message;
//
//     /// <summary>
//     /// 이 이슈가 발생한 EpisodeId / AttachmentId / EndingKey 등.
//     /// 특정 컨텍스트가 없으면 빈 문자열.
//     /// </summary>
//     public readonly string ContextId;
//
//     public EpisodeProgressionValidationIssue(
//         EpisodeProgressionIssueSeverity severity,
//         string message,
//         string contextId)
//     {
//         Severity = severity;
//         Message = message ?? "";
//         ContextId = contextId ?? "";
//     }
// }
//
// // ─────────────────────────────────────────────────────────────
// // 검증 결과 컨테이너
// // ─────────────────────────────────────────────────────────────
//
// public sealed class EpisodeProgressionValidationResult
// {
//     public readonly List<EpisodeProgressionValidationIssue> Issues =
//         new List<EpisodeProgressionValidationIssue>();
//
//     public bool HasErrors
//     {
//         get
//         {
//             for (int i = 0; i < Issues.Count; i++)
//             {
//                 if (Issues[i].Severity == EpisodeProgressionIssueSeverity.Error)
//                     return true;
//             }
//
//             return false;
//         }
//     }
//
//     public bool HasWarnings
//     {
//         get
//         {
//             for (int i = 0; i < Issues.Count; i++)
//             {
//                 if (Issues[i].Severity == EpisodeProgressionIssueSeverity.Warning)
//                     return true;
//             }
//
//             return false;
//         }
//     }
//
//     public int ErrorCount
//     {
//         get
//         {
//             int count = 0;
//
//             for (int i = 0; i < Issues.Count; i++)
//             {
//                 if (Issues[i].Severity == EpisodeProgressionIssueSeverity.Error)
//                     count++;
//             }
//
//             return count;
//         }
//     }
//
//     public int WarningCount
//     {
//         get
//         {
//             int count = 0;
//
//             for (int i = 0; i < Issues.Count; i++)
//             {
//                 if (Issues[i].Severity == EpisodeProgressionIssueSeverity.Warning)
//                     count++;
//             }
//
//             return count;
//         }
//     }
//
//     public void Add(
//         EpisodeProgressionIssueSeverity severity,
//         string message,
//         string contextId = "")
//     {
//         Issues.Add(new EpisodeProgressionValidationIssue(severity, message, contextId));
//     }
//
//     public void Clear()
//     {
//         Issues.Clear();
//     }
// }
