using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// curves.json — 커스텀 이징 곡선 저장소 (ease-curve-orders.md §3).
//
// 커브는 작가 자산이라 번들(@Dialogue) 옆에 산다. VnTool 내보내기가 이 파일을
// 함께 내고, 여기서는 읽기만 한다. 파일이 없으면 커브 0개로 조용히 동작한다 —
// 커브를 안 쓰는 프로젝트가 정상 경로다.
//
// 스키마 (ease-curves/1) — curves는 배열이다(원안의 딕셔너리를 수정 회신:
// 런타임 파서 JsonUtility가 딕셔너리를 못 읽는다):
// {
//   "schema": "ease-curves/1",
//   "curves": [
//     { "name": "hop_snappy",
//       "keys": [ { "t": 0, "v": 0, "inTangent": 0, "outTangent": 2.6 }, … ] }
//   ]
// }
//
// 검증(어긋난 커브는 로그 + 무시 — 조용히 빠뜨리지 않는다):
// 이름 [a-z0-9_]+ · 키 2개 이상 · t 오름차순 · 첫 키 (t,v)=(0,0) · 마지막 키 (t,v)=(1,1).
//
// 끝값을 못 박는 이유: 곡선은 "경로의 모양"만 정하고 종점은 언제나 명목 목표값이다
// (호스트 커밋이 명목값을 쓴다). 중간이 1을 넘는 오버슛은 정상 — OutBack이 그렇게 논다.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class EaseCurveLibrary
{
    public const string BundleFileName = "curves.json";
    private const string ExpectedSchema = "ease-curves/1";

    private static readonly Regex NameRule = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    /// <summary>끝값 판정 허용 오차. JSON 왕복의 부동소수 잡음만 흡수한다.</summary>
    private const float EndpointTolerance = 1e-4f;

    private readonly Dictionary<string, CurveKey[]> _curves;

    public static EaseCurveLibrary Empty { get; } = new(new Dictionary<string, CurveKey[]>());

    private EaseCurveLibrary(Dictionary<string, CurveKey[]> curves)
    {
        _curves = curves;
    }

    public int Count => _curves.Count;

    public bool TryGet(string name, out CurveKey[] keys)
        => _curves.TryGetValue(name, out keys);

    /// <summary>파일이 없으면 무음으로 Empty — 커브를 안 쓰는 프로젝트가 정상 경로다.</summary>
    public static EaseCurveLibrary LoadFrom(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return Empty;

        return Parse(File.ReadAllText(jsonPath), jsonPath);
    }

    public static EaseCurveLibrary Parse(string json, string sourceLabel)
    {
        FileDto file;

        try
        {
            file = JsonUtility.FromJson<FileDto>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EaseCurveLibrary] {sourceLabel}: JSON을 읽지 못했다 — {e.Message}. 커브 0개로 동작한다.");
            return Empty;
        }

        if (file == null)
        {
            Debug.LogError($"[EaseCurveLibrary] {sourceLabel}: 빈 문서다. 커브 0개로 동작한다.");
            return Empty;
        }

        if (file.schema != ExpectedSchema)
        {
            Debug.LogWarning(
                $"[EaseCurveLibrary] {sourceLabel}: schema '{file.schema}' (기대 '{ExpectedSchema}'). " +
                "읽기는 시도한다 — 어긋나면 커브 단위 검증이 거른다.");
        }

        Dictionary<string, CurveKey[]> curves = new();

        if (file.curves != null)
        {
            foreach (CurveDto curve in file.curves)
            {
                if (!TryValidate(curve, sourceLabel, out CurveKey[] keys))
                    continue;

                curves[curve.name] = keys;
            }
        }

        return new EaseCurveLibrary(curves);
    }

    private static bool TryValidate(CurveDto curve, string sourceLabel, out CurveKey[] keys)
    {
        keys = null;

        string name = curve?.name ?? "(null)";

        if (curve == null || string.IsNullOrEmpty(curve.name) || !NameRule.IsMatch(curve.name))
        {
            Warn(sourceLabel, name, "이름은 [a-z0-9_]+ 여야 한다 (커맨드 토큰에 실린다)");
            return false;
        }

        if (curve.keys == null || curve.keys.Count < 2)
        {
            Warn(sourceLabel, name, "키가 2개 이상이어야 한다");
            return false;
        }

        if (curve.keys[0].t != 0f)
        {
            Warn(sourceLabel, name, $"첫 키 t가 0이 아니다 ({curve.keys[0].t})");
            return false;
        }

        if (curve.keys[curve.keys.Count - 1].t != 1f)
        {
            Warn(sourceLabel, name, $"마지막 키 t가 1이 아니다 ({curve.keys[curve.keys.Count - 1].t})");
            return false;
        }

        // ── 끝값 규약 ────────────────────────────────────────────────
        // 곡선은 "경로의 모양"만 정한다. 종점은 언제나 명목 목표값이다 —
        // 호스트 커밋(OnCommitFinalState)이 명목값을 쓰기 때문이다.
        // 그래서 v가 0이 아닌 데서 시작하거나 1이 아닌 데서 끝나는 곡선은
        // 재생에서 양 끝 한 프레임이 튀고, 툴 프리뷰와도 어긋난다.
        // (중간이 1을 넘는 것은 정상이다 — OutBack·OutElastic이 그렇게 논다.
        //  "1.2배 지점에 착지"는 곡선이 아니라 delta를 1.2배로 쓸 일이다.)
        if (Math.Abs(curve.keys[0].v) > EndpointTolerance)
        {
            Warn(sourceLabel, name,
                $"첫 키 v가 0이 아니다 ({curve.keys[0].v}) — 곡선은 모양만 정한다");
            return false;
        }

        if (Math.Abs(curve.keys[curve.keys.Count - 1].v - 1f) > EndpointTolerance)
        {
            Warn(sourceLabel, name,
                $"마지막 키 v가 1이 아니다 ({curve.keys[curve.keys.Count - 1].v}) — " +
                "종점은 명목 목표값이다. 더 멀리 보내려면 delta를 키워라");
            return false;
        }

        for (int i = 1; i < curve.keys.Count; i++)
        {
            if (curve.keys[i].t <= curve.keys[i - 1].t)
            {
                Warn(sourceLabel, name, $"키 t가 오름차순이 아니다 (index {i})");
                return false;
            }
        }

        keys = new CurveKey[curve.keys.Count];

        for (int i = 0; i < curve.keys.Count; i++)
        {
            KeyDto k = curve.keys[i];
            keys[i] = new CurveKey(k.t, k.v, k.inTangent, k.outTangent);
        }

        return true;
    }

    private static void Warn(string sourceLabel, string curveName, string why)
        => Debug.LogWarning($"[EaseCurveLibrary] {sourceLabel}: 커브 '{curveName}' 무시 — {why}.");

    // ── DTO (JsonUtility 직렬화 대상 — 필드명이 곧 스키마다) ─────────

    [Serializable]
    private sealed class FileDto
    {
        public string schema = "";
        public List<CurveDto> curves = new();
    }

    [Serializable]
    private sealed class CurveDto
    {
        public string name = "";
        public List<KeyDto> keys = new();
    }

    [Serializable]
    private sealed class KeyDto
    {
        public float t = 0f;
        public float v = 0f;
        public float inTangent = 0f;
        public float outTangent = 0f;
    }
}
