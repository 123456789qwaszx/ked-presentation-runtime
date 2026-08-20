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

    [TestCase(@"{ ""name"": ""Hop-Snappy"", ""keys"": [ { ""t"": 0 }, { ""t"": 1 } ] }", "이름 규칙")]
    [TestCase(@"{ ""name"": ""solo"", ""keys"": [ { ""t"": 0 } ] }", "키 2개 미만")]
    [TestCase(@"{ ""name"": ""late_start"", ""keys"": [ { ""t"": 0.1 }, { ""t"": 1 } ] }", "첫 키 t!=0")]
    [TestCase(@"{ ""name"": ""early_end"", ""keys"": [ { ""t"": 0 }, { ""t"": 0.9 } ] }", "마지막 키 t!=1")]
    [TestCase(@"{ ""name"": ""unsorted"", ""keys"": [ { ""t"": 0 }, { ""t"": 0.6 }, { ""t"": 0.4 }, { ""t"": 1 } ] }", "오름차순 위반")]
    public void 어긋난_커브는_경고_로그와_함께_무시된다(string curveJson, string label)
    {
        string json = @"{ ""schema"": ""ease-curves/1"", ""curves"": [" + curveJson + "] }";

        LogAssert.Expect(LogType.Warning, new Regex("EaseCurveLibrary.*무시"));

        EaseCurveLibrary library = EaseCurveLibrary.Parse(json, "test");

        Assert.AreEqual(0, library.Count, label);
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
