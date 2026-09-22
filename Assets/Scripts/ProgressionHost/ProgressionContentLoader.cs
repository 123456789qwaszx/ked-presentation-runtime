using System.Collections.Generic;
using System.Text;
using Ked.Progression;
using Newtonsoft.Json;
using UnityEngine;

// Unity의 챕터 JSON 에셋을 Progression 시나리오 정의로 변환한다.
public static class ProgressionContentLoader
{
    public static ScenarioDefinition LoadSingleChapter(TextAsset chapterJson)
    {
        if (chapterJson == null)
        {
            Debug.LogError("[진행] 챕터 JSON이 지정되지 않았다.");
            return null;
        }

        ChapterProgressionDto dto;

        try
        {
            dto = JsonConvert.DeserializeObject<ChapterProgressionDto>(
                chapterJson.text);
        }
        catch (JsonException error)
        {
            Debug.LogError(
                $"[진행] 챕터 JSON 역직렬화 실패: " +
                $"{chapterJson.name}\n{error.Message}");

            return null;
        }

        if (dto == null)
        {
            Debug.LogError(
                $"[진행] 챕터 JSON이 비어 있다: {chapterJson.name}");

            return null;
        }

        ScenarioLoadResult result =
            ProgressionLoader.LoadAsSingleChapterScenario(dto);

        LogDiagnostics(result.Diagnostics, chapterJson.name);

        if (!result.IsValid)
        {
            Debug.LogError(
                $"[진행] 챕터를 싣지 못했다: " +
                $"{chapterJson.name} — 위 진단을 볼 것.");

            return null;
        }

        return result.Scenario;
    }

    private static void LogDiagnostics(
        IReadOnlyList<ProgressionDiagnostic> diagnostics,
        string what)
    {
        if (diagnostics.Count == 0)
            return;

        var errors = new StringBuilder();
        var warnings = new StringBuilder();

        for (int i = 0; i < diagnostics.Count; i++)
        {
            ProgressionDiagnostic diagnostic = diagnostics[i];

            StringBuilder into =
                diagnostic.Severity == ProgressionDiagnosticSeverity.Error
                    ? errors
                    : warnings;

            into.Append("\n  ").Append(diagnostic);
        }

        if (warnings.Length > 0)
            Debug.LogWarning($"[진행] 경고 — {what}{warnings}");

        if (errors.Length > 0)
            Debug.LogError($"[진행] 오류 — {what}{errors}");
    }
}