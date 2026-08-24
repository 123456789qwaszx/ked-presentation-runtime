using System.Collections.Generic;
using System.Text;
using Ked.Progression;
using UnityEngine;
using Yarn.Unity;

// 진행 JSON이 부르는 노드 이름
public static class ProgressionContentPreflight
{
    public sealed class Report
    {
        // 진행 JSON이 부르는데 YarnProject에 없는 노드.
        public readonly List<string> Missing = new();

        // [2] 진행 스탯과 [3] Yarn 선언의 타입이 갈린 자리.
        public readonly List<string> Mismatched = new();

        public bool IsClear => Missing.Count == 0 && Mismatched.Count == 0;
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

            VerifyStatTypes(report, chapter, project.InitialValues);
        }

        return report;
    }

    // [2] 진행 스탯과 [3] Yarn 선언은 같은 이름 칸을 쓴다 — 그래야 대사가
    // "<<if $호감도 >= 5>>"를 컴파일할 수 있다(야른은 선언 없는 변수를 오류로 잡는다).
    // 겹치는 것 자체는 정상이고, 어긋날 수 있는 것은 타입이다.
    //
    // 타입이 갈리면 조용히 깨진다. Yarn 저장소는 값을 심을 때 그 변수의 런타임 타입을
    // 함께 도장하는데, 진행 층이 숫자로 심고 작가가 bool로 선언했다면 그 뒤의
    // "<<if $깃발>>"이 읽히지 않고 다른 분기를 탄다. 재생해 봐도 안 보이는 종류다.
    private static void VerifyStatTypes(
        Report report, ChapterProgression chapter,
        Dictionary<string, System.IConvertible> declared)
    {
        if (declared == null)
            return;

        for (int i = 0; i < chapter.Stats.Count; i++)
        {
            StatDefinition stat = chapter.Stats[i];

            // 선언이 없으면 대사가 안 읽는 스탯이다 — 진행만 쓰는 것일 수 있으니 막지 않는다.
            if (!declared.TryGetValue("$" + stat.Key, out System.IConvertible value))
                continue;

            bool yarnIsBool = value is bool;

            if (yarnIsBool == (stat.Type == StatType.Bool))
                continue;

            report.Mismatched.Add(
                $"{chapter.ChapterId} - 스탯 \"{stat.Key}\": " +
                $"진행 {stat.Type} / Yarn 선언 {(yarnIsBool ? "bool" : "number")}. " +
                (yarnIsBool
                    ? $"진행 층은 숫자로 심으므로 \"${stat.Key}\"를 bool로 읽을 수 없다 — " +
                      "스탯을 Bool로 바꾸거나 <<declare>>를 숫자로 바꿀 것."
                    : $"깃발은 bool로 심으므로 숫자로 선언된 \"${stat.Key}\"와 어긋난다 — " +
                      "<<declare>>를 false/true로 바꿀 것."));
        }
    }

    public static bool CheckAndLog(ScenarioProgression scenario, YarnProject project)
    {
        Report report = Check(scenario, project);

        if (report.IsClear)
        {
            Debug.Log("[진행] 사전 대조 통과 - 부르는 노드가 전부 YarnProject에 있다.");
            return true;
        }

        var text = new StringBuilder();

        text.Append("[진행] 사전 대조 실패 - 재생을 시작하지 않는다.\n")
            .Append("진행 JSON과 .yarn 은 저작 도구의 산출물 둘이고, 둘이 어긋나는 것을 ")
            .Append("호스트만 볼 수 있다.\n");

        if (report.Missing.Count > 0)
        {
            text.Append("\n진행 JSON이 부르는 노드가 YarnProject에 없다:");

            for (int i = 0; i < report.Missing.Count; i++)
                text.Append("\n  ").Append(report.Missing[i]);
        }

        if (report.Mismatched.Count > 0)
        {
            text.Append("\n\n진행 스탯과 Yarn 선언의 타입이 갈렸다:");

            for (int i = 0; i < report.Mismatched.Count; i++)
                text.Append("\n  ").Append(report.Mismatched[i]);
        }

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
            report.Missing.Add($"{chapterId}/{where} - {what} 노드 이름이 비어 있다.");
            return;
        }

        if (available.Contains(nodeName))
            return;

        report.Missing.Add(
            $"{chapterId}/{where} - {what} 노드 \"{nodeName}\"이 없다.{Hint(available, nodeName)}");
    }

    // 왜 없는지 체크
    private static string Hint(HashSet<string> available, string nodeName)
    {
        var near = new List<string>();

        foreach (string candidate in available)
        {
            if (candidate.EndsWith(nodeName, System.StringComparison.Ordinal) ||
                nodeName.EndsWith(candidate, System.StringComparison.Ordinal))
            {
                near.Add(candidate);
            }
        }

        if (near.Count > 0)
            return $"  -> 이름이 꼬리만 같은 것이 있다: \"{string.Join("\", \"", near)}\". " +
                   "접두 규칙이 한쪽에만 반영된 자리다 - 둘 중 한쪽을 맞춰야 한다.";
        
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

        return text.Length == 0 
            ? "(없음)"
            : text.ToString();
    }
}