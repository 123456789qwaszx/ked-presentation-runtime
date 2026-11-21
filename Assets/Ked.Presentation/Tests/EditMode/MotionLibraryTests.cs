using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// motions.json 로더 — 스키마 수용, end·kind 낱말, 성분 생략, 모션 단위 검증(어긋난 것은 로그 + 무시). 채널에 mode는 없다(motions/2).
public class MotionLibraryTests
{
    private const string Nod = @"{ ""name"": ""nod"", ""rig"": ""character"", ""duration"": 0.6, ""end"": ""commit"",
        ""channels"": [
          { ""node"": ""CharacterPortrait_Track_Move"", ""kind"": ""position"",
            ""y"": [ { ""t"": 0, ""v"": 0, ""inTangent"": 0, ""outTangent"": -120 },
                     { ""t"": 0.3, ""v"": -18, ""inTangent"": 0, ""outTangent"": 0 },
                     { ""t"": 0.6, ""v"": 0, ""inTangent"": 60, ""outTangent"": 0 } ] },
          { ""node"": ""CharacterPortrait_SwayPivot"", ""kind"": ""Euler"",
            ""z"": [ { ""t"": 0, ""v"": 3 }, { ""t"": 0.6, ""v"": 0 } ] }
        ] }";

    private const string Head = @"{ ""name"": ""x"", ""duration"": 1, ";
    private const string TrackX = @"""channels"": [ { ""node"": ""CharSlot_Track"", ""kind"": ""position"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }";

    private static string Doc(params string[] motions)
        => @"{ ""schema"": ""motions/2"", ""motions"": [" + string.Join(",", motions) + "] }";

    [Test]
    public void 유효한_문서를_읽고_채널과_키가_그대로_나온다()
    {
        MotionLibrary library = MotionLibrary.Parse(Doc(Nod), "test");

        Assert.AreEqual(1, library.Count);
        Assert.IsTrue(library.TryGet("nod", out MotionClip clip));
        Assert.AreEqual("character", clip.Rig);
        Assert.AreEqual(0.6f, clip.Duration, 0f);
        Assert.AreEqual(MotionEnd.Commit, clip.End);
        Assert.AreEqual(2, clip.Channels.Length);

        MotionChannel y = clip.Channels[0];
        Assert.AreEqual(MotionChannelKind.Position, y.Kind);
        Assert.IsFalse(y.HasX, "생략한 성분은 null이다 — JsonUtility의 빈 리스트를 null로 읽는다");
        Assert.AreEqual(3, y.Y.Length);
        Assert.AreEqual(-120f, y.Y[0].OutTangent, 0f);
        Assert.AreEqual(-18f, y.Y[1].Value, 0f);

        MotionChannel z = clip.Channels[1];
        Assert.AreEqual(MotionChannelKind.Euler, z.Kind, "kind는 대소문자를 가리지 않는다");
        Assert.IsTrue(z.HasZ);

        Assert.IsTrue(library.Clips.ContainsKey("nod"), "리듀서 튜닝에 그대로 넘기는 사전");
    }

    [TestCase(Head + TrackX, "end")]
    [TestCase(Head + @"""end"": ""hold"", " + TrackX, "end")]
    [TestCase(Head + @"""end"": ""commit"", ""channels"": [ { ""node"": ""CharSlot_Track"", ""kind"": ""slide"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }", "kind")]
    [TestCase(Head + @"""end"": ""commit"", ""channels"": [ { ""node"": ""CharFocus_Track"", ""kind"": ""position"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }", "포커스 계층")]
    [TestCase(Head + @"""end"": ""commit"", ""channels"": [ { ""node"": ""CharSlot_Track"", ""kind"": ""position"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 0.5, ""v"": 1 } ] } ] }", "길이")]
    public void 어긋난_모션은_경고하고_무시한다(string motionJson, string expectInWarning)
    {
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*무시.*" + Regex.Escape(expectInWarning)));

        MotionLibrary library = MotionLibrary.Parse(Doc(motionJson), "test");

        Assert.AreEqual(0, library.Count);
        Assert.IsFalse(library.TryGet("x", out _));
    }

    [Test]
    public void 스키마_노드_목록을_주면_없는_노드를_거른다()
    {
        // 리그를 밝힌다 — 리그가 없는 모션은 노드를 보기 전에 "모르는 리그"로 걸러진다(C5 이후). 여기서 보려는 것은 노드다.
        string stray = Head + @"""rig"": ""character"", ""end"": ""commit"", ""channels"": [ { ""node"": ""NoSuchNode"", ""kind"": ""position"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }";

        Assert.AreEqual(1, MotionLibrary.Parse(Doc(stray), "test").Count, "목록이 없으면 노드는 보지 않는다");

        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*스키마"));
        Assert.AreEqual(0, MotionLibrary.Parse(Doc(stray), "test", MotionLibrary.StageRigNodeIds).Count);

        Assert.AreEqual(1, MotionLibrary.Parse(Doc(Nod), "test", MotionLibrary.StageRigNodeIds).Count, "실제 스키마의 노드는 통과한다");
    }

    // ── 카메라 리그의 모션 = 화면 흔들림 ─────────────────────────────────

    private static string Shake(string node, string end, string rig = "camera")
        => "{ \"name\": \"shake\", \"rig\": \"" + rig + "\", \"duration\": 0.2, \"end\": \"" + end + "\", \"channels\": [ { \"node\": \"" + node
           + "\", \"kind\": \"position\", \"x\": [ { \"t\": 0, \"v\": 0 }, { \"t\": 0.1, \"v\": 12 }, { \"t\": 0.2, \"v\": 0 } ] } ] }";

    [Test]
    public void 카메라_리그의_모션은_제_스키마로_검증된다()
    {
        MotionLibrary library = MotionLibrary.Parse(Doc(Nod, Shake("Camera_Shake", "restore")), "test", MotionLibrary.StageRigNodeIds);

        Assert.AreEqual(2, library.Count, "캐릭터 모션과 흔들림이 한 파일에 산다");
        Assert.IsTrue(library.TryGet("shake", out MotionClip shake));
        Assert.AreEqual(MotionRules.CameraRig, shake.Rig);
    }

    [Test]
    public void 흔들림은_Camera_Shake에만_키를_주고_restore여야_한다()
    {
        // 나머지 카메라 노드는 샷 식의 해다 — 키를 주면 샷이 쓰는 값과 싸운다.
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*Camera_Shake 하나다"));
        Assert.AreEqual(0, MotionLibrary.Parse(Doc(Shake("Camera_Pan", "restore")), "test", MotionLibrary.StageRigNodeIds).Count);

        // commit이면 흔들림 노드에 값이 남는다 — 폴드가 모르는 상태다.
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*restore여야 한다"));
        Assert.AreEqual(0, MotionLibrary.Parse(Doc(Shake("Camera_Shake", "commit")), "test", MotionLibrary.StageRigNodeIds).Count);
    }

    [Test]
    public void 무대가_모르는_리그의_모션은_거른다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*모르는 리그"));
        Assert.AreEqual(0, MotionLibrary.Parse(Doc(Shake("Window", "restore", "stageWindow")), "test", MotionLibrary.StageRigNodeIds).Count);
    }

    [Test]
    public void restore인데_마지막_키가_남아도_읽힌다_값_규칙은_없고_재생이_시작값을_되쓴다()
    {
        // motions/1에서는 "restore 마지막 키 중립값" 규칙에 걸렸다. motions/2는 mode를 모르므로 값 규칙이 없다.
        MotionLibrary library = MotionLibrary.Parse(Doc(Head + @"""end"": ""restore"", " + TrackX), "test");

        Assert.AreEqual(1, library.Count);
        Assert.IsTrue(library.TryGet("x", out MotionClip clip));
        Assert.AreEqual(MotionEnd.Restore, clip.End);
        Assert.AreEqual(1f, clip.Channels[0].X[1].Value, 0f);
    }

    [Test]
    public void 같은_이름이_둘이면_뒤의_것을_무시한다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*둘"));

        MotionLibrary library = MotionLibrary.Parse(Doc(Nod, Nod), "test");

        Assert.AreEqual(1, library.Count);
    }

    [Test]
    public void 깨진_JSON과_빈_문서는_오류를_내고_빈_저장소다()
    {
        LogAssert.Expect(LogType.Error, new Regex("MotionLibrary"));
        MotionLibrary broken = MotionLibrary.Parse("이건 JSON이 아니다 {", "test");

        Assert.AreEqual(0, broken.Count);
        Assert.AreSame(MotionLibrary.Empty, broken);

        Assert.AreEqual(0, MotionLibrary.Parse(@"{ ""schema"": ""motions/2"" }", "test").Count);
    }

    [Test]
    public void motions_1_문서는_스키마_경고와_함께_읽히고_채널의_mode는_무시된다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*schema 'motions/1'"));

        const string v1 = @"{ ""schema"": ""motions/1"", ""motions"": [ { ""name"": ""x"", ""rig"": ""character"", ""duration"": 1, ""end"": ""commit"",
            ""channels"": [ { ""node"": ""CharSlot_Track"", ""kind"": ""position"", ""mode"": ""absolute"", ""x"": [ { ""t"": 0, ""v"": 5 }, { ""t"": 1, ""v"": 1 } ] } ] } ] }";

        MotionLibrary library = MotionLibrary.Parse(v1, "test");

        Assert.AreEqual(1, library.Count, "값은 그대로 읽힌다 — mode는 커맨드가 정한다");
        Assert.IsTrue(library.TryGet("x", out MotionClip clip));
        Assert.AreEqual(5f, clip.Channels[0].X[0].Value, 0f);
    }
}
