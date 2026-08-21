using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// curves.json 로더 — 스키마 수용과 커브 단위 검증(어긋난 커브는 로그 + 무시).
public class EaseCurveLibraryTests
{
    private const string ValidJson = @"{
        ""schema"": ""ease-curves/1"",
        ""curves"": [
            { ""name"": ""hop_snappy"",
              ""keys"": [
                { ""t"": 0.0, ""v"": 0.0, ""inTangent"": 0.0, ""outTangent"": 2.6 },
                { ""t"": 0.4, ""v"": 0.9, ""inTangent"": 0.8, ""outTangent"": 0.3 },
                { ""t"": 1.0, ""v"": 1.0, ""inTangent"": 0.1, ""outTangent"": 0.0 }
              ] }
        ]
    }";

    private static string Doc(string curveJson)
        => @"{ ""schema"": ""ease-curves/1"", ""curves"": [" + curveJson + "] }";

    [Test]
    public void 유효한_문서를_읽고_키가_그대로_나온다()
    {
        EaseCurveLibrary library = EaseCurveLibrary.Parse(ValidJson, "test");

        Assert.AreEqual(1, library.Count);
        Assert.IsTrue(library.TryGet("hop_snappy", out CurveKey[] keys));
        Assert.AreEqual(3, keys.Length);
        Assert.AreEqual(0.4f, keys[1].Time, 0f);
        Assert.AreEqual(0.9f, keys[1].Value, 0f);
        Assert.AreEqual(0.8f, keys[1].InTangent, 0f);
        Assert.AreEqual(0.3f, keys[1].OutTangent, 0f);
    }

    [TestCase(@"{ ""name"": ""Hop-Snappy"", ""keys"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] }", "이름 규칙")]
    [TestCase(@"{ ""name"": ""solo"", ""keys"": [ { ""t"": 0, ""v"": 0 } ] }", "키 2개 미만")]
    [TestCase(@"{ ""name"": ""late_start"", ""keys"": [ { ""t"": 0.1, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] }", "첫 키 t!=0")]
    [TestCase(@"{ ""name"": ""early_end"", ""keys"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 0.9, ""v"": 1 } ] }", "마지막 키 t!=1")]
    [TestCase(@"{ ""name"": ""unsorted"", ""keys"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 0.6, ""v"": 0.5 }, { ""t"": 0.4, ""v"": 0.7 }, { ""t"": 1, ""v"": 1 } ] }", "오름차순 위반")]
    public void 어긋난_커브는_경고_로그와_함께_무시된다(string curveJson, string label)
    {
        LogAssert.Expect(LogType.Warning, new Regex("EaseCurveLibrary.*무시"));

        EaseCurveLibrary library = EaseCurveLibrary.Parse(Doc(curveJson), "test");

        Assert.AreEqual(0, library.Count, label);
    }

    // 끝값 규약: 곡선은 모양만 정하고 종점은 명목 목표값이다.
    // v≠0/1로 끝나는 곡선은 재생에서 양 끝이 한 프레임 튀고 툴 프리뷰와도 어긋나므로
    // 저작 단계에서 막는다 — "1.2배 지점에 착지"는 delta를 키울 일이다.
    [TestCase(@"{ ""name"": ""overshoot_end"", ""keys"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1.2 } ] }", "끝값 1.2")]
    [TestCase(@"{ ""name"": ""undershoot_end"", ""keys"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 0.8 } ] }", "끝값 0.8")]
    [TestCase(@"{ ""name"": ""jump_start"", ""keys"": [ { ""t"": 0, ""v"": 0.1 }, { ""t"": 1, ""v"": 1 } ] }", "시작값 0.1")]
    public void 끝값이_0과_1이_아닌_커브는_거부된다(string curveJson, string label)
    {
        LogAssert.Expect(LogType.Warning, new Regex("EaseCurveLibrary.*무시"));

        EaseCurveLibrary library = EaseCurveLibrary.Parse(Doc(curveJson), "test");

        Assert.AreEqual(0, library.Count, label);
    }

    [Test]
    public void 중간이_1을_넘는_오버슛_커브는_받아들인다()
    {
        // OutBack·OutElastic이 노는 방식이다 — 비행 중에만 넘고 종점은 1로 돌아온다.
        string json = Doc(@"{ ""name"": ""back_like"",
            ""keys"": [
                { ""t"": 0, ""v"": 0, ""outTangent"": 3 },
                { ""t"": 0.6, ""v"": 1.15 },
                { ""t"": 1, ""v"": 1 }
            ] }");

        EaseCurveLibrary library = EaseCurveLibrary.Parse(json, "test");

        Assert.AreEqual(1, library.Count);
        Assert.IsTrue(library.TryGet("back_like", out CurveKey[] keys));
        Assert.Greater(CurveFunctions.Evaluate(keys, 0.6f), 1f, "비행 중 오버슛이 살아 있어야 한다");
        Assert.AreEqual(1f, CurveFunctions.Evaluate(keys, 1f), 1e-4f, "종점은 1이다");
    }

    [Test]
    public void 깨진_JSON은_오류_로그와_함께_커브_0개로_동작한다()
    {
        LogAssert.Expect(LogType.Error, new Regex("EaseCurveLibrary"));

        EaseCurveLibrary library = EaseCurveLibrary.Parse("이건 JSON이 아니다 {", "test");

        Assert.AreEqual(0, library.Count);
    }

    [Test]
    public void 없는_파일은_무음으로_빈_라이브러리다()
    {
        EaseCurveLibrary library = EaseCurveLibrary.LoadFrom(
            System.IO.Path.Combine(Application.temporaryCachePath, "no-such-curves.json"));

        Assert.AreEqual(0, library.Count);
        Assert.IsFalse(library.TryGet("anything", out _));
    }
}
