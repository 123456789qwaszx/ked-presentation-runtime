using System.Collections.Generic;
using System.Text;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 JSON이 부르는 노드 이름
public static class ProgressionContentPreflight
{
    /// <summary>저작 이미터가 Yarn 노드 이름에 붙이는 접두(<c>YarnBundleEmitter.StoryPrefix</c>).</summary>
    private const string AuthoringStoryPrefix = "Story_";

    public sealed class Report
    {
        public readonly List<string> Missing = new();
        public bool IsClear => Missing.Count == 0;
    }

    // 시나리오가 부르는 대사 노드와 연출 노드를 전부 모아 project와 대조.
    public static Report Check(ScenarioProgression scenario, YarnProject project)
    {
        var report = new Report();

        if (scenario == null)
            return report;

        if (project == null)
        {
            report.Missing.Add("YarnProject가 DialogueRunner에 물려 있지 않다 — 대조할 대상이 없다.");
            return report;
        }

        var available = new HashSet<string>(project.NodeNames, System.StringComparer.Ordinal);

        foreach (ChapterProgression chapter in scenario.Chapters)
        {
            foreach (EpisodeNode node in chapter.Nodes)
            {
                Verify(report, available, node.DialogueEntryId, "대사", chapter.ChapterId, node.EpisodeId);

                IReadOnlyList<EpisodeOption> options = node.NextOptions;

                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].HasVia)
                    {
                        Verify(
                            report, available, options[i].ViaNodeId,
                            "연출", chapter.ChapterId, $"{node.EpisodeId}→{options[i].TargetEpisodeId}");
                    }
                }
            }
        }

        return report;
    }

    public static bool CheckAndLog(ScenarioProgression scenario, YarnProject project)
    {
        Report report = Check(scenario, project);

        if (report.IsClear)
        {
            Debug.Log("[진행] 사전 대조 통과 — 부르는 노드가 전부 YarnProject에 있다.");
            return true;
        }

        var text = new StringBuilder();

        text.Append("[진행] 사전 대조 실패 — 진행 JSON이 부르는 노드가 YarnProject에 없다. ")
            .Append("재생을 시작하지 않는다.\n")
            .Append("진행 JSON과 .yarn 은 저작 도구의 산출물 둘이고, 이름이 어긋나는 것을 ")
            .Append("호스트만 볼 수 있다.\n");

        for (int i = 0; i < report.Missing.Count; i++)
            text.Append("\n  ").Append(report.Missing[i]);

        Debug.LogError(text.ToString());

        return false;
    }

    private static void Verify(
        Report report,
        HashSet<string> available,
        string nodeName,
        string what,
        string chapterId,
        string where)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            report.Missing.Add($"{chapterId}/{where} — {what} 노드 이름이 비어 있다.");
            return;
        }

        if (available.Contains(nodeName))
            return;

        report.Missing.Add(
            $"{chapterId}/{where} — {what} 노드 \"{nodeName}\"이 없다.{Hint(available, nodeName)}");
    }

    // 왜 없는지 짚는다. 체크.
    private static string Hint(HashSet<string> available, string nodeName)
    {
        if (available.Contains(AuthoringStoryPrefix + nodeName))
        {
            return $"  → \"{AuthoringStoryPrefix}{nodeName}\"은 있다. " +
                   "저작 이미터가 붙이는 접두이고 진행 내보내기는 안 붙인다 — 둘 중 한쪽을 맞춰야 한다.";
        }

        if (nodeName.StartsWith(AuthoringStoryPrefix, System.StringComparison.Ordinal) &&
            available.Contains(nodeName.Substring(AuthoringStoryPrefix.Length)))
        {
            return $"  → 접두 없는 \"{nodeName.Substring(AuthoringStoryPrefix.Length)}\"은 있다.";
        }

        return $"  (YarnProject에 있는 노드: {Join(available)})";
    }

    private static string Join(HashSet<string> names)
    {
        var text = new StringBuilder();
        int count = 0;

        foreach (string name in names)
        {
            if (count++ > 0)
                text.Append(", ");

            if (count > 12)
            {
                text.Append('…');
                break;
            }

            text.Append(name);
        }

        return text.Length == 0 ? "(없음)" : text.ToString();
    }
}