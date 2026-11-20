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

    /// <summary>
    /// 챕터 JSON 여럿을 시나리오 하나로 — 스토리 폴더(챕터 카드 화면)의 길.
    /// 폴더에는 무엇이든 들어올 수 있으므로 챕터마다 따로 싣는다: 깨진 챕터 · ID가 겹친 챕터는 소리 내고 빼고,
    /// 나머지로 시나리오를 세운다. 순서는 ChapterId(숫자는 수로 견준다 — ch2 < ch10). 하나도 없으면 null.
    /// </summary>
    public static ScenarioDefinition LoadChapters(
        IReadOnlyList<TextAsset> chapterJsons,
        string scenarioId,
        string displayName)
    {
        var dtos = new List<ChapterProgressionDto>();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);

        int count = chapterJsons == null ? 0 : chapterJsons.Count;

        for (int i = 0; i < count; i++)
        {
            TextAsset chapterJson = chapterJsons[i];

            if (chapterJson == null)
                continue;

            ChapterProgressionDto dto;

            try
            {
                dto = JsonConvert.DeserializeObject<ChapterProgressionDto>(chapterJson.text);
            }
            catch (JsonException error)
            {
                Debug.LogError($"[진행] 챕터 JSON 역직렬화 실패 — 뺀다: {chapterJson.name}\n{error.Message}");
                continue;
            }

            ProgressionLoadResult chapter = ProgressionLoader.Load(dto);

            LogDiagnostics(chapter.Diagnostics, chapterJson.name);

            if (chapter.Chapter == null)
            {
                Debug.LogError($"[진행] 챕터를 싣지 못했다 — 뺀다: {chapterJson.name} — 위 진단을 볼 것.");
                continue;
            }

            if (!seen.Add(dto.ChapterId))
            {
                Debug.LogError($"[진행] 챕터 ID '{dto.ChapterId}'가 겹친다 — 뒤의 것을 뺀다: {chapterJson.name}");
                continue;
            }

            dtos.Add(dto);
        }

        if (dtos.Count == 0)
        {
            Debug.LogError($"[진행] 실을 챕터가 하나도 없다: {scenarioId}");
            return null;
        }

        dtos.Sort((a, b) => CompareNatural(a.ChapterId, b.ChapterId));

        ScenarioLoadResult result = ProgressionLoader.LoadScenario(
            scenarioId, displayName, dtos[0].ChapterId, dtos);

        if (!result.IsValid)
        {
            // 챕터마다의 진단은 위에서 이미 냈다 — 여기 남는 것은 시나리오 수준의 것이다.
            LogDiagnostics(result.Diagnostics, scenarioId);
            Debug.LogError($"[진행] 시나리오를 세우지 못했다: {scenarioId} — 위 진단을 볼 것.");
            return null;
        }

        return result.Scenario;
    }

    // 숫자 토막은 수로, 나머지는 글자로 견준다 — "ch2" < "ch10".
    private static int CompareNatural(string a, string b)
    {
        int i = 0, j = 0;

        while (i < a.Length && j < b.Length)
        {
            if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
            {
                int si = i, sj = j;

                while (i < a.Length && char.IsDigit(a[i])) i++;
                while (j < b.Length && char.IsDigit(b[j])) j++;

                string na = a.Substring(si, i - si).TrimStart('0');
                string nb = b.Substring(sj, j - sj).TrimStart('0');

                int byNumber = na.Length != nb.Length
                    ? na.Length.CompareTo(nb.Length)
                    : string.CompareOrdinal(na, nb);

                if (byNumber != 0)
                    return byNumber;

                continue;
            }

            if (a[i] != b[j])
                return a[i].CompareTo(b[j]);

            i++;
            j++;
        }

        int rest = (a.Length - i).CompareTo(b.Length - j);

        // 수로는 같고 표기만 다른 것("ch1" · "ch01")도 순서가 정해지게.
        return rest != 0 ? rest : string.CompareOrdinal(a, b);
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