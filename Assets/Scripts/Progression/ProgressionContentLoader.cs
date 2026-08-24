using System.Collections.Generic;
using System.Text;
using Ked.Progression;
using Ked.Progression.Dto;
using Newtonsoft.Json;
using UnityEngine;

// 챕터 JSON을 읽어 시나리오로 감쌈.
public static class ProgressionContentLoader
{
    // 챕터 하나를 단일 챕터 시나리오로 싣는다. 
    public static ScenarioProgression LoadSingleChapter(TextAsset chapterJson)
    {
        if (chapterJson == null)
        {
            Debug.LogError(
                "[진행] 챕터 JSON이 물려 있지 않다. " +
                "VNAppBootstrap의 \"진행 층 › Progression Chapter Json\"에 " +
                "Assets/@Dialogue/ChapterProgression/ 아래의 .json 을 넣을 것.");

            return null;
        }

        ChapterProgressionDto dto;

        try
        {
            dto = JsonConvert.DeserializeObject<ChapterProgressionDto>(chapterJson.text);
        }
        catch (JsonException error)
        {
            Debug.LogError($"[진행] 챕터 JSON 역직렬화 실패: {chapterJson.name}\n{error.Message}");
            return null;
        }

        if (dto == null)
        {
            Debug.LogError($"[진행] 챕터 JSON이 비어 있다: {chapterJson.name}");
            return null;
        }

        // 챕터 하나를 시나리오로 감싸는 것은 코어가 직접.
        ScenarioLoadResult result = ProgressionLoader.LoadAsSingleChapterScenario(dto);

        LogDiagnostics(result.Diagnostics, chapterJson.name);

        if (!result.IsValid)
        {
            Debug.LogError($"[진행] 챕터를 싣지 못했다: {chapterJson.name} — 위 진단을 볼 것.");
            return null;
        }

        Debug.Log(
            $"[진행] 실었다 — {result.Scenario.ScenarioId} " +
            $"(에피소드 {result.Scenario.StartChapter.Nodes.Count}, " +
            $"스탯 {result.Scenario.StartChapter.Stats.Count})");

        return result.Scenario;
    }

    private static void LogDiagnostics(IReadOnlyList<ProgressionDiagnostic> diagnostics, string what)
    {
        if (diagnostics.Count == 0)
            return;

        var errors = new StringBuilder();
        var warnings = new StringBuilder();

        for (int i = 0; i < diagnostics.Count; i++)
        {
            ProgressionDiagnostic diagnostic = diagnostics[i];

            StringBuilder into =
                diagnostic.Severity == ProgressionDiagnosticSeverity.Error ? errors : warnings;

            into.Append("\n  ").Append(diagnostic);
        }

        if (warnings.Length > 0)
            Debug.LogWarning($"[진행] 경고 — {what}{warnings}");

        if (errors.Length > 0)
            Debug.LogError($"[진행] 오류 — {what}{errors}");
    }
}