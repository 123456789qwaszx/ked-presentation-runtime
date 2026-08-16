// // ============================================================
// // EpisodeProgressionValidator.cs
// // 경로: Assets/_Project/Scripts/VN/Episode/Progression/
// //
// // ChapterEpisodeProgressionSO의 무결성을 검사하는 순수 정적 클래스.
// // 데이터 수정은 최소화하며, 자동 보정이 필요한 경우 별도 메서드로 분리한다.
// //
// // 사용 예 (EditorWindow):
// //   EpisodeProgressionValidationResult result =
// //       EpisodeProgressionValidator.Validate(progressionSO);
// //
// //   foreach (var issue in result.Issues)
// //       Debug.Log($"[{issue.Severity}] {issue.Message}");
// // ============================================================
//
// using System;
// using System.Collections.Generic;
//
// public static class EpisodeProgressionValidator
// {
//     // ─────────────────────────────────────────────────────────
//     // 진입점
//     // ─────────────────────────────────────────────────────────
//
//     public static EpisodeProgressionValidationResult Validate(
//         ChapterEpisodeProgressionSO progression)
//     {
//         EpisodeProgressionValidationResult result = new EpisodeProgressionValidationResult();
//
//         if (progression == null)
//         {
//             result.Add(EpisodeProgressionIssueSeverity.Error, "Progression asset is null.");
//             return result;
//         }
//
//         ValidateChapterHeader(progression, result);
//
//         HashSet<string> nodeIds = CollectNodeIds(progression, result);
//
//         ValidateNodes(progression, nodeIds, result);
//         ValidateNextOptions(progression, nodeIds, result);
//         ValidateAttachments(progression, nodeIds, result);
//         ValidateEndingRules(progression, result);
//         ValidateEndingCandidateConsistency(progression, result);
//
//         return result;
//     }
//
//     // ─────────────────────────────────────────────────────────
//     // 자동 보정 (EditorWindow의 "Fix" 버튼에서 호출 권장)
//     // ─────────────────────────────────────────────────────────
//
//     /// <summary>
//     /// Attachment.ParentEpisodeId가 비어있을 때 소속 노드 EpisodeId로 채워준다.
//     /// Validator 자체는 수정하지 않으므로, 이 메서드를 먼저 호출 후 Validate한다.
//     /// </summary>
//     public static void AutoFillAttachmentParents(ChapterEpisodeProgressionSO progression)
//     {
//         if (progression == null || progression.Nodes == null)
//             return;
//
//         for (int i = 0; i < progression.Nodes.Count; i++)
//         {
//             EpisodeNodeDefinition node = progression.Nodes[i];
//
//             if (node == null || node.Attachments == null)
//                 continue;
//
//             for (int j = 0; j < node.Attachments.Count; j++)
//             {
//                 EpisodeAttachmentDefinition att = node.Attachments[j];
//
//                 if (att == null)
//                     continue;
//
//                 if (string.IsNullOrWhiteSpace(att.ParentEpisodeId))
//                     att.ParentEpisodeId = node.EpisodeId;
//             }
//         }
//     }
//
//     // ─────────────────────────────────────────────────────────
//     // 내부 검증 단계들
//     // ─────────────────────────────────────────────────────────
//
//     private static void ValidateChapterHeader(
//         ChapterEpisodeProgressionSO progression,
//         EpisodeProgressionValidationResult result)
//     {
//         if (string.IsNullOrWhiteSpace(progression.ChapterId))
//             result.Add(EpisodeProgressionIssueSeverity.Error, "ChapterId is empty.");
//
//         if (progression.Nodes == null || progression.Nodes.Count == 0)
//             result.Add(EpisodeProgressionIssueSeverity.Error, "No episode nodes defined.");
//
//         if (string.IsNullOrWhiteSpace(progression.StartEpisodeId))
//             result.Add(EpisodeProgressionIssueSeverity.Warning, "StartEpisodeId is empty.");
//     }
//
//     /// <summary>nodeId 수집과 동시에 기본 노드 유효성 검사를 수행한다.</summary>
//     private static HashSet<string> CollectNodeIds(
//         ChapterEpisodeProgressionSO progression,
//         EpisodeProgressionValidationResult result)
//     {
//         HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
//
//         if (progression.Nodes == null)
//             return ids;
//
//         for (int i = 0; i < progression.Nodes.Count; i++)
//         {
//             EpisodeNodeDefinition node = progression.Nodes[i];
//
//             if (node == null)
//             {
//                 result.Add(EpisodeProgressionIssueSeverity.Error, $"Nodes[{i}] is null.");
//                 continue;
//             }
//
//             if (string.IsNullOrWhiteSpace(node.EpisodeId))
//             {
//                 result.Add(EpisodeProgressionIssueSeverity.Error, $"Nodes[{i}] EpisodeId is empty.");
//                 continue;
//             }
//
//             if (!ids.Add(node.EpisodeId))
//                 result.Add(EpisodeProgressionIssueSeverity.Error, $"Duplicate EpisodeId '{node.EpisodeId}'.", node.EpisodeId);
//         }
//
//         return ids;
//     }
//
//     private static void ValidateNodes(
//         ChapterEpisodeProgressionSO progression,
//         HashSet<string> nodeIds,
//         EpisodeProgressionValidationResult result)
//     {
//         if (progression.Nodes == null)
//             return;
//
//         // StartEpisodeId 존재 확인
//         if (!string.IsNullOrWhiteSpace(progression.StartEpisodeId)
//             && !nodeIds.Contains(progression.StartEpisodeId))
//         {
//             result.Add(
//                 EpisodeProgressionIssueSeverity.Error,
//                 $"StartEpisodeId '{progression.StartEpisodeId}' does not exist in Nodes.",
//                 progression.StartEpisodeId);
//         }
//
//         for (int i = 0; i < progression.Nodes.Count; i++)
//         {
//             EpisodeNodeDefinition node = progression.Nodes[i];
//
//             if (node == null || string.IsNullOrWhiteSpace(node.EpisodeId))
//                 continue;
//
//             if (string.IsNullOrWhiteSpace(node.Title))
//                 result.Add(EpisodeProgressionIssueSeverity.Warning,
//                     $"Title is empty. episodeId='{node.EpisodeId}'.", node.EpisodeId);
//
//             if (string.IsNullOrWhiteSpace(node.DialogueEntryId))
//                 result.Add(EpisodeProgressionIssueSeverity.Error,
//                     $"DialogueEntryId is empty. episodeId='{node.EpisodeId}'.", node.EpisodeId);
//
//             // IsChapterEndingCandidate이지만 EndingKey가 비어있는 경우
//             if (node.IsChapterEndingCandidate && string.IsNullOrWhiteSpace(node.EndingKey))
//                 result.Add(EpisodeProgressionIssueSeverity.Warning,
//                     $"IsChapterEndingCandidate is true but EndingKey is empty. episodeId='{node.EpisodeId}'.",
//                     node.EpisodeId);
//
//             // EndingKey가 있지만 EndingRules에 없는 경우
//             if (!string.IsNullOrWhiteSpace(node.EndingKey))
//             {
//                 bool foundInRules = false;
//
//                 if (progression.EndingRules != null)
//                 {
//                     for (int r = 0; r < progression.EndingRules.Count; r++)
//                     {
//                         ChapterEndingRule rule = progression.EndingRules[r];
//
//                         if (rule != null && string.Equals(rule.EndingKey, node.EndingKey, StringComparison.Ordinal))
//                         {
//                             foundInRules = true;
//                             break;
//                         }
//                     }
//                 }
//
//                 if (!foundInRules)
//                     result.Add(EpisodeProgressionIssueSeverity.Warning,
//                         $"EndingKey '{node.EndingKey}' not found in EndingRules. episodeId='{node.EpisodeId}'.",
//                         node.EpisodeId);
//             }
//         }
//     }
//
//     private static void ValidateNextOptions(
//         ChapterEpisodeProgressionSO progression,
//         HashSet<string> nodeIds,
//         EpisodeProgressionValidationResult result)
//     {
//         if (progression.Nodes == null)
//             return;
//
//         for (int i = 0; i < progression.Nodes.Count; i++)
//         {
//             EpisodeNodeDefinition node = progression.Nodes[i];
//
//             if (node == null || string.IsNullOrWhiteSpace(node.EpisodeId))
//                 continue;
//
//             if (node.NextOptions == null)
//                 continue;
//
//             HashSet<string> seenTargets = new HashSet<string>(StringComparer.Ordinal);
//
//             for (int j = 0; j < node.NextOptions.Count; j++)
//             {
//                 EpisodeNextOption option = node.NextOptions[j];
//
//                 if (option == null)
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"NextOptions[{j}] is null. episodeId='{node.EpisodeId}'.", node.EpisodeId);
//                     continue;
//                 }
//
//                 if (string.IsNullOrWhiteSpace(option.TargetEpisodeId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"NextOptions[{j}] TargetEpisodeId is empty. episodeId='{node.EpisodeId}'.", node.EpisodeId);
//                     continue;
//                 }
//
//                 if (!nodeIds.Contains(option.TargetEpisodeId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"NextOptions target '{option.TargetEpisodeId}' does not exist. from='{node.EpisodeId}'.",
//                         node.EpisodeId);
//                 }
//
//                 if (!seenTargets.Add(option.TargetEpisodeId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Warning,
//                         $"Duplicate NextOption target '{option.TargetEpisodeId}'. from='{node.EpisodeId}'.",
//                         node.EpisodeId);
//                 }
//
//                 // 자기 자신으로 돌아오는 경우
//                 if (string.Equals(option.TargetEpisodeId, node.EpisodeId, StringComparison.Ordinal))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Warning,
//                         $"NextOption self-loop detected. episodeId='{node.EpisodeId}'.", node.EpisodeId);
//                 }
//             }
//         }
//     }
//
//     private static void ValidateAttachments(
//         ChapterEpisodeProgressionSO progression,
//         HashSet<string> nodeIds,
//         EpisodeProgressionValidationResult result)
//     {
//         if (progression.Nodes == null)
//             return;
//
//         HashSet<string> attachmentIds = new HashSet<string>(StringComparer.Ordinal);
//
//         for (int i = 0; i < progression.Nodes.Count; i++)
//         {
//             EpisodeNodeDefinition node = progression.Nodes[i];
//
//             if (node == null || string.IsNullOrWhiteSpace(node.EpisodeId))
//                 continue;
//
//             if (node.Attachments == null)
//                 continue;
//
//             for (int j = 0; j < node.Attachments.Count; j++)
//             {
//                 EpisodeAttachmentDefinition att = node.Attachments[j];
//
//                 if (att == null)
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"Attachments[{j}] is null. episodeId='{node.EpisodeId}'.", node.EpisodeId);
//                     continue;
//                 }
//
//                 if (string.IsNullOrWhiteSpace(att.AttachmentId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"AttachmentId is empty. parent='{node.EpisodeId}', index={j}.", node.EpisodeId);
//                     continue;
//                 }
//
//                 if (!attachmentIds.Add(att.AttachmentId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"Duplicate AttachmentId '{att.AttachmentId}'.", att.AttachmentId);
//                 }
//
//                 // ParentEpisodeId 검증
//                 if (string.IsNullOrWhiteSpace(att.ParentEpisodeId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Warning,
//                         $"Attachment ParentEpisodeId is empty. attachment='{att.AttachmentId}'. " +
//                         "Run Auto-Fill to fix.", att.AttachmentId);
//                 }
//                 else if (!nodeIds.Contains(att.ParentEpisodeId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"Attachment parent '{att.ParentEpisodeId}' does not exist. attachment='{att.AttachmentId}'.",
//                         att.AttachmentId);
//                 }
//
//                 if (string.IsNullOrWhiteSpace(att.DialogueEntryId))
//                 {
//                     result.Add(EpisodeProgressionIssueSeverity.Error,
//                         $"Attachment DialogueEntryId is empty. attachment='{att.AttachmentId}'.",
//                         att.AttachmentId);
//                 }
//             }
//         }
//     }
//
//     private static void ValidateEndingRules(
//         ChapterEpisodeProgressionSO progression,
//         EpisodeProgressionValidationResult result)
//     {
//         if (progression.EndingRules == null || progression.EndingRules.Count == 0)
//         {
//             result.Add(EpisodeProgressionIssueSeverity.Warning, "No chapter ending rules defined.");
//             return;
//         }
//
//         HashSet<string> endingKeys = new HashSet<string>(StringComparer.Ordinal);
//
//         for (int i = 0; i < progression.EndingRules.Count; i++)
//         {
//             ChapterEndingRule rule = progression.EndingRules[i];
//
//             if (rule == null)
//             {
//                 result.Add(EpisodeProgressionIssueSeverity.Error, $"EndingRules[{i}] is null.");
//                 continue;
//             }
//
//             if (string.IsNullOrWhiteSpace(rule.EndingKey))
//             {
//                 result.Add(EpisodeProgressionIssueSeverity.Error,
//                     $"EndingRules[{i}] EndingKey is empty.");
//                 continue;
//             }
//
//             if (!endingKeys.Add(rule.EndingKey))
//             {
//                 result.Add(EpisodeProgressionIssueSeverity.Error,
//                     $"Duplicate EndingKey '{rule.EndingKey}'.", rule.EndingKey);
//             }
//
//             if (rule.UnlockNextChapter && string.IsNullOrWhiteSpace(rule.NextChapterId))
//             {
//                 result.Add(EpisodeProgressionIssueSeverity.Error,
//                     $"UnlockNextChapter is true but NextChapterId is empty. ending='{rule.EndingKey}'.",
//                     rule.EndingKey);
//             }
//         }
//     }
//
//     /// <summary>
//     /// 노드에서 참조하는 EndingKey가 EndingRules에 존재하는지 교차 검증한다.
//     /// (ValidateNodes에서도 일부 체크하지만, EndingRules 검증 후 재확인한다.)
//     /// </summary>
//     private static void ValidateEndingCandidateConsistency(
//         ChapterEpisodeProgressionSO progression,
//         EpisodeProgressionValidationResult result)
//     {
//         if (progression.Nodes == null)
//             return;
//
//         bool anyEndingCandidate = false;
//
//         for (int i = 0; i < progression.Nodes.Count; i++)
//         {
//             EpisodeNodeDefinition node = progression.Nodes[i];
//
//             if (node != null && node.IsChapterEndingCandidate)
//             {
//                 anyEndingCandidate = true;
//                 break;
//             }
//         }
//
//         if (!anyEndingCandidate && progression.EndingRules != null && progression.EndingRules.Count > 0)
//         {
//             result.Add(EpisodeProgressionIssueSeverity.Warning,
//                 "EndingRules exist but no node has IsChapterEndingCandidate set to true.");
//         }
//     }
// }
